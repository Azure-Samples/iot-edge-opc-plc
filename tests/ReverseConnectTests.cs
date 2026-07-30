namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the reverse connect (ReverseHello) feature.
/// These start their own server instance, since reverse connect must be configured at startup.
/// </summary>
[TestFixture]
public class ReverseConnectTests
{
    /// <summary>
    /// The OPC UA TCP message type that a server sends to a reverse connect client.
    /// </summary>
    private const string ReverseHelloMessageType = "RHE";

    private const int ReverseConnectIntervalMs = 1000;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Test]
    public async Task ReverseConnect_SendsReverseHelloToConfiguredClient()
    {
        // Arrange: a plain TCP listener that stands in for the reverse connect client.
        var clientListener = new TcpListener(IPAddress.Loopback, 0);
        clientListener.Start();
        int clientPort = ((IPEndPoint)clientListener.LocalEndpoint).Port;

        var opcPlcServer = new OpcPlcServer();
        using var serverCts = new CancellationTokenSource();

        var serverTask = Task.Run(() => opcPlcServer.StartAsync(
            [
                "--autoaccept",
                $"--pn={GetFreePort()}",
                $"--rcc=opc.tcp://localhost:{clientPort}",
                $"--rci={ReverseConnectIntervalMs}",
            ],
            serverCts.Token));

        try
        {
            await WaitForServerReadyAsync(opcPlcServer, serverTask).ConfigureAwait(false);

            opcPlcServer.PlcServer.GetReverseConnections().Keys
                .Should().Contain(url => url.Port == clientPort,
                    "the configured client endpoint should be registered as a reverse connection");

            // Act: the server dials out to the client endpoint on its own.
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var connection = await clientListener.AcceptTcpClientAsync(timeoutCts.Token).ConfigureAwait(false);

            var buffer = new byte[8];
            int read = await connection.GetStream()
                .ReadAtLeastAsync(buffer, ReverseHelloMessageType.Length, throwOnEndOfStream: false, cancellationToken: timeoutCts.Token)
                .ConfigureAwait(false);

            // Assert
            read.Should().BeGreaterThanOrEqualTo(ReverseHelloMessageType.Length);
            Encoding.ASCII.GetString(buffer, 0, ReverseHelloMessageType.Length)
                .Should().Be(ReverseHelloMessageType, "the server must greet the client with a ReverseHello message");
        }
        finally
        {
            clientListener.Stop();
            await StopServerAsync(serverCts, serverTask).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task ReverseConnect_NotConfigured_HasNoReverseConnections()
    {
        // Arrange
        var opcPlcServer = new OpcPlcServer();
        using var serverCts = new CancellationTokenSource();

        var serverTask = Task.Run(() => opcPlcServer.StartAsync(
            [
                "--autoaccept",
                $"--pn={GetFreePort()}",
            ],
            serverCts.Token));

        try
        {
            // Act
            await WaitForServerReadyAsync(opcPlcServer, serverTask).ConfigureAwait(false);

            // Assert
            opcPlcServer.PlcServer.GetReverseConnections().Should().BeEmpty();
        }
        finally
        {
            await StopServerAsync(serverCts, serverTask).ConfigureAwait(false);
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForServerReadyAsync(OpcPlcServer opcPlcServer, Task serverTask)
    {
        using var timeoutCts = new CancellationTokenSource(Timeout);

        while (!opcPlcServer.Ready)
        {
            if (serverTask.IsFaulted)
            {
                throw serverTask.Exception!;
            }

            if (serverTask.IsCompleted)
            {
                throw new InvalidOperationException("The OPC PLC server failed to start.");
            }

            timeoutCts.Token.ThrowIfCancellationRequested();
            await Task.Delay(200).ConfigureAwait(false);
        }
    }

    private static async Task StopServerAsync(CancellationTokenSource serverCts, Task serverTask)
    {
        await serverCts.CancelAsync().ConfigureAwait(false);
        await Task.WhenAny(serverTask, Task.Delay(Timeout)).ConfigureAwait(false);
    }
}
