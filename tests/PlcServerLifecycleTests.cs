// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Security.Certificates;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

[TestFixture]
public class PlcServerLifecycleTests
{
    [Test]
    public async Task ManagedSession_RestartReconnectsAndResumesReadsAsync()
    {
        var fixture = new PlcSimulatorFixture(["--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var session = await fixture.CreateSessionAsync("ManagedRestart", cancellationToken: deadline.Token)
                .ConfigureAwait(false);
            var managed = session.Should().BeOfType<ManagedSession>().Subject;
            await using var cleanup = managed.ConfigureAwait(false);
            managed.KeepAliveInterval = 1000;
            var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            managed.ConnectionStateChanged += (_, change) =>
            {
                if (change.NewState != ConnectionState.Connected)
                {
                    disconnected.TrySetResult();
                }
                else if (disconnected.Task.IsCompleted)
                {
                    reconnected.TrySetResult();
                }
            };
            var nodeId = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, managed.NamespaceUris);
            StatusCode.IsGood((await managed.ReadValueAsync(nodeId, deadline.Token).ConfigureAwait(false)).StatusCode)
                .Should().BeTrue();

            await fixture.RestartAsync().WaitAsync(deadline.Token).ConfigureAwait(false);
            await reconnected.Task.WaitAsync(deadline.Token).ConfigureAwait(false);

            uint before = (await managed.ReadValueAsync(nodeId, deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetUInt32();
            fixture.FireTimersWithPeriod(1000, 1);
            (await managed.ReadValueAsync(nodeId, deadline.Token).ConfigureAwait(false)).WrappedValue.GetUInt32()
                .Should().Be(before + 1);
            await managed.CloseAsync(deadline.Token).ConfigureAwait(false);
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task Restart_ReusesCertificateAndReleasesEndpointAsync()
    {
        var fixture = new PlcSimulatorFixture(["--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        int port = new Uri(fixture.EndpointUrl).Port;
        try
        {
            string thumbprint = GetThumbprint(fixture.Server);
            for (int restart = 0; restart < 2; restart++)
            {
                PlcServer previous = fixture.Server;
                using (var session = await fixture.CreateSessionAsync("BeforeRestart").ConfigureAwait(false))
                {
                    var node = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
                    StatusCode.IsGood((await session.ReadValueAsync(node).ConfigureAwait(false)).StatusCode)
                        .Should().BeTrue();
                    await session.CloseAsync().ConfigureAwait(false);
                }

                await fixture.RestartAsync().WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                fixture.Ready.Should().BeTrue();
                fixture.Server.Should().NotBeSameAs(previous);
                GetThumbprint(fixture.Server).Should().Be(thumbprint);
                using var restarted = await fixture.CreateSessionAsync("AfterRestart").ConfigureAwait(false);
                var valueId = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, restarted.NamespaceUris);
                uint before = (await restarted.ReadValueAsync(valueId).ConfigureAwait(false)).WrappedValue.GetUInt32();
                fixture.FireTimersWithPeriod(1000, 1);
                (await restarted.ReadValueAsync(valueId).ConfigureAwait(false)).WrappedValue.GetUInt32()
                    .Should().Be(before + 1);
                await restarted.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
        fixture.Ready.Should().BeFalse();
        AssertPortReleased(port);
    }

    [Test]
    public async Task HostedIdentity_UsesConfiguredCredentialsAndRejectsAnonymousAsync()
    {
        var fixture = new PlcSimulatorFixture(["--str=false", "--daa", "--du=operator", "--dc=test-password"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var session = await fixture.CreateSessionAsync("ConfiguredUser",
                new UserIdentity("operator", Encoding.UTF8.GetBytes("test-password")), deadline.Token)
                .ConfigureAwait(false);
            var node = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
            StatusCode.IsGood((await session.ReadValueAsync(node).ConfigureAwait(false)).StatusCode).Should().BeTrue();
            await session.CloseAsync().ConfigureAwait(false);

            Func<Task> anonymous = async () =>
            {
                using var rejected = await fixture.CreateSessionAsync(
                    "RejectedAnonymous", cancellationToken: deadline.Token).ConfigureAwait(false);
                await rejected.CloseAsync().ConfigureAwait(false);
            };
            await anonymous.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
            Func<Task> wrongPassword = async () =>
            {
                using var rejected = await fixture.CreateSessionAsync("RejectedPassword",
                    new UserIdentity("operator", Encoding.UTF8.GetBytes("wrong-password")), deadline.Token)
                    .ConfigureAwait(false);
                await rejected.CloseAsync().ConfigureAwait(false);
            };
            await wrongPassword.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task Startup_PreCanceled_DoesNotBindEndpointAsync()
    {
        int port = FreePort();
        var server = new OpcPlcServer();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await server.StartAsync(["--autoaccept", $"--pn={port}", "--str=false"], cancellation.Token)
            .WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        server.Ready.Should().BeFalse();
        server.PlcServer.Should().BeNull();
        server.Stop();
        AssertPortReleased(port);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StartupFailure_ReleasesEndpointAndDoesNotBecomeReadyAsync(bool afterServerStarted)
    {
        int port = FreePort();
        var server = new OpcPlcServer
        {
            TimeService = afterServerStarted ? new FailingSimulationTimeService() : new TimeService()
        };
        string[] arguments = afterServerStarted
            ? ["--autoaccept", $"--pn={port}", "--str=false"]
            : ["--autoaccept", $"--pn={port}", "--str=false", $"--dalm=missing-{Guid.NewGuid():N}.json"];
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        Func<Task> start = () => server.StartAsync(arguments, cancellation.Token);

        if (afterServerStarted)
        {
            await start.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Controlled simulation startup failure.").ConfigureAwait(false);
        }
        else
        {
            await start.Should().ThrowAsync<ServiceResultException>()
                .Where(error => error.StatusCode == StatusCodes.BadInternalError).ConfigureAwait(false);
        }

        server.Ready.Should().BeFalse();
        server.Stop();
        AssertPortReleased(port);
    }

    [Test]
    public async Task Stop_DuringSimulationStartup_DrainsAndReleasesEndpointAsync()
    {
        int port = FreePort();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new OpcPlcServer { TimeService = new BlockingSimulationTimeService(entered, release) };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        Task start = Task.Run(() => server.StartAsync(
            ["--autoaccept", $"--pn={port}", "--str=false"], cancellation.Token));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            server.Ready.Should().BeFalse();
            server.Stop();
        }
        finally
        {
            release.Set();
            await cancellation.CancelAsync().ConfigureAwait(false);
            await start.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }

        server.Ready.Should().BeFalse();
        AssertPortReleased(port);
    }

    private static string GetThumbprint(PlcServer server)
    {
        using CertificateEntry certificate = server.Config.OpcUa.ApplicationConfiguration.CertificateManager
            .AcquireApplicationCertificateByType(ObjectTypeIds.RsaSha256ApplicationCertificateType);
        return certificate.Certificate.Thumbprint;
    }

    private static int FreePort()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static void AssertPortReleased(int port)
    {
        using var listener = new TcpListener(IPAddress.Any, port);
        listener.Server.ExclusiveAddressUse = true;
        listener.Start();
    }

    private sealed class FailingSimulationTimeService : TimeService
    {
        public override OpcPlc.ITimer NewTimer(ElapsedEventHandler callback, uint intervalInMilliseconds)
        {
            throw new InvalidOperationException("Controlled simulation startup failure.");
        }
    }

    private sealed class BlockingSimulationTimeService(TaskCompletionSource entered, ManualResetEventSlim release)
        : TimeService
    {
        public override OpcPlc.ITimer NewTimer(ElapsedEventHandler callback, uint intervalInMilliseconds)
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("Simulation startup was not released.");
            }
            return base.NewTimer(callback, intervalInMilliseconds);
        }
    }
}
