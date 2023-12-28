namespace UnitTests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for tests that use the OPC PLC server NuGet.
/// </summary>
public class OpcPlcBase
{
    private const int MaxPortTries = 10;

    private readonly OpcPlcServer _opcPlcServer;
    private readonly bool _endpointUrlOverridden;
    private readonly Random _rnd = new(1234); // Seeded (deterministic) random number generator.

    private bool _isOpcPlcServerRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcPlcBase"/> class.
    /// Set the <paramref name="endpointUriOverride"/> to override spawning a server and use an existing one instead.
    /// </summary>
    public OpcPlcBase(string[] args, int port = 51234, string? endpointUriOverride = null)
    {
        if (_isOpcPlcServerRunning && _opcPlcServer is not null)
        {
            // Already running.
            return;
        }

        _opcPlcServer = new OpcPlcServer();

        if (!string.IsNullOrEmpty(endpointUriOverride))
        {
            OpcPlcEndpointUrl = endpointUriOverride;
            _endpointUrlOverridden = true;
            return;
        }

        // Try to use the specified port, otherwise find a deterministic random port.
        int usedPortRetries = MaxPortTries;
        while (OpcPlcEndpointUrl == string.Empty)
        {
            try
            {
                OpcPlcEndpointUrl = StartOpcPlcServerAsync(args, port, CancellationToken.None).GetAwaiter().GetResult();
                break;
            }
            catch (Exception)
            {
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                if (--usedPortRetries == 0)
                {
                    throw;
                }
#pragma warning restore S2589 // Boolean expressions should not be gratuitous

                port = GetRandomEphemeralPort();
            }
        }
    }

    /// <summary>
    /// Gets the OPC PLC server endpoint URL.
    /// </summary>
    public static string OpcPlcEndpointUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Restarts the OPC PLC server with the same configuration.
    /// </summary>
    public Task RestartOpcPlcServerAsync()
    {
        if (_endpointUrlOverridden)
        {
            throw new InvalidOperationException("Cannot restart OPC PLC server when the endpoint URL is overridden.");
        }

        return _opcPlcServer.RestartAsync();
    }

    private async Task<string> WaitForServerUpAsync(Task serverTask, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (serverTask.IsFaulted)
            {
                throw serverTask.Exception!;
            }

            if (serverTask.IsCompleted)
            {
                throw new Exception("The OPC PLC server failed to start.");
            }

            if (!_opcPlcServer.Ready)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                continue;
            }

            _isOpcPlcServerRunning = true;

            return _opcPlcServer.PlcServer.GetEndpoints()[0].EndpointUrl;
        }
    }

    private Task<string> StartOpcPlcServerAsync(string[] args, int port, CancellationToken cancellationToken)
    {
        // Passed args override the following defaults.
        var serverTask = Task.Run(
            async () => await _opcPlcServer.StartAsync(
                args?.Concat(
                    new[]
                    {
                        "--autoaccept",
                        $"--portnum={port}",
                    }).ToArray(),
                cancellationToken)
            .ConfigureAwait(false),
            cancellationToken);

        return WaitForServerUpAsync(serverTask, cancellationToken);
    }

    private int GetRandomEphemeralPort()
    {
        // Port range 49152–65535 (https://en.wikipedia.org/wiki/Ephemeral_port).
        return _rnd.Next(49152, 65535);
    }
}
