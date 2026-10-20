namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using OpcPlc.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

[TestFixture]
public class ReadinessTests
{
    [TestCase(false, HttpStatusCode.NotFound)]
    [TestCase(true, HttpStatusCode.OK)]
    public async Task PublisherConfig_IsServedOnlyWhenEnabled(bool enabled, HttpStatusCode expectedStatus)
    {
        string fileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, "{}").ConfigureAwait(false);
            var server = new OpcPlcServer
            {
                Config = new OpcPlcConfiguration
                {
                    WebServerPort = 0,
                    PnJson = fileName.Replace('\\', '/'),
                    ShowPublisherConfigJsonPh = enabled,
                },
            };
            using var host = server.CreateHostBuilder([]);
            await host.StartAsync().ConfigureAwait(false);
            try
            {
                var addresses = host.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>().Addresses;
                var address = new UriBuilder(addresses.Single()) { Host = "localhost" };
                using var client = new HttpClient { BaseAddress = address.Uri };

                using var response = await client.GetAsync("/" + server.Config.PnJson.TrimStart('/')).ConfigureAwait(false);
                response.StatusCode.Should().Be(expectedStatus);
            }
            finally
            {
                await host.StopAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Test]
    public async Task HealthEndpoints_ReportReadinessIndependentlyOfLiveness()
    {
        var server = new OpcPlcServer
        {
            Config = new OpcPlcConfiguration { WebServerPort = 0 },
        };
        using var host = server.CreateHostBuilder([]);
        await host.StartAsync().ConfigureAwait(false);

        try
        {
            var addresses = host.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses;
            var address = new UriBuilder(addresses.Single()) { Host = "localhost" };
            using var client = new HttpClient { BaseAddress = address.Uri };

            using var starting = await client.GetAsync("/ready").ConfigureAwait(false);
            starting.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            using var alive = await client.GetAsync("/healthz").ConfigureAwait(false);
            alive.StatusCode.Should().Be(HttpStatusCode.OK);
            using var publisherConfig = await client.GetAsync("/pn.json").ConfigureAwait(false);
            publisherConfig.StatusCode.Should().Be(HttpStatusCode.NotFound);

            server.Ready = true;
            using var running = await client.GetAsync("/ready").ConfigureAwait(false);
            running.StatusCode.Should().Be(HttpStatusCode.OK);
            running.Headers.CacheControl.NoStore.Should().BeTrue();

            server.Ready = false;
            using var stopping = await client.GetAsync("/ready").ConfigureAwait(false);
            stopping.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            using var stillAlive = await client.GetAsync("/healthz").ConfigureAwait(false);
            stillAlive.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task OpcUaListener_RefusesConnectionsUntilAddressSpaceIsReady()
    {
        int port = GetFreeTcpPort();
        bool? acceptedWhileBuildingAddressSpace = null;

        var server = new OpcPlcServer();
        var timeService = new Mock<TimeService> { CallBase = true };
        timeService.Setup(t => t.UtcNow())
            .Callback(() => acceptedWhileBuildingAddressSpace ??= CanConnect(port))
            .CallBase();
        server.TimeService = timeService.Object;

        using var cancellation = new CancellationTokenSource();
        var serverTask = Task.Run(() => server.StartAsync(
            ["--autoaccept", $"--pn={port}", "--wp=0"], cancellation.Token));

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await WaitUntilReadyAsync(server, serverTask, timeout.Token).ConfigureAwait(false);

            acceptedWhileBuildingAddressSpace.Should().BeFalse(
                "the OPC UA listener must not accept clients before the address space exists");
            CanConnect(port).Should().BeTrue("the OPC UA listener must accept clients once the server is ready");

            await server.RestartAsync().WaitAsync(timeout.Token).ConfigureAwait(false);
            server.Ready.Should().BeTrue();
            CanConnect(port).Should().BeTrue("the OPC UA listener must be reopened after a restart");
        }
        finally
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool CanConnect(int port)
    {
        using var client = new TcpClient();
        try
        {
            return client.ConnectAsync(IPAddress.Loopback, port).Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException ex) when (ex.InnerException is SocketException)
        {
            return false;
        }
    }

    private static async Task WaitUntilReadyAsync(OpcPlcServer server, Task serverTask, CancellationToken cancellationToken)
    {
        while (!server.Ready)
        {
            if (serverTask.IsCompleted)
            {
                await serverTask.ConfigureAwait(false);
                Assert.Fail("Server exited before becoming ready");
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Ready_IsResetDuringRestartAndShutdown(bool cancelExternally)
    {
        var server = new OpcPlcServer();
        var timeService = new Mock<TimeService> { CallBase = true };
        timeService.Setup(t => t.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()))
            .Callback(() => server.Ready.Should().BeFalse("simulation must initialize before readiness"))
            .CallBase();
        server.TimeService = timeService.Object;

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        using var cancellation = new CancellationTokenSource();
        var serverTask = Task.Run(() => server.StartAsync(
            ["--autoaccept", $"--pn={port}", "--wp=0"], cancellation.Token));

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            while (!server.Ready)
            {
                if (serverTask.IsCompleted)
                {
                    await serverTask.ConfigureAwait(false);
                    Assert.Fail("Server exited before becoming ready");
                }

                await Task.Delay(50, timeout.Token).ConfigureAwait(false);
            }

            var restart = server.RestartAsync();
            await restart.WaitAsync(timeout.Token).ConfigureAwait(false);
            server.Ready.Should().BeTrue();
            timeService.Verify(t => t.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()), Times.AtLeast(2));

            if (cancelExternally)
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
            }
            else
            {
                server.Stop();
                server.Ready.Should().BeFalse();
            }
            await serverTask.WaitAsync(timeout.Token).ConfigureAwait(false);
            server.Ready.Should().BeFalse();
        }
        finally
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await serverTask.WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
        }
    }
}
