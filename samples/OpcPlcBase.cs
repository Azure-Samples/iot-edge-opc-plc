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
    private static bool _isOpcPlcServerRunning;
    private static string[]? _args;
    private readonly int _port;
    private readonly bool _endpointUrlOverridden;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcPlcBase"/> class.
    /// Set the <paramref name="endpointUriOverride"/> to override spawning a server and use an existing one instead.
    /// </summary>
    public OpcPlcBase(string[] args, int port = 51234, string? endpointUriOverride = null)
    {
        if (_isOpcPlcServerRunning)
        {
            if (OpcPlcEndpointUrl.Contains(port.ToString()))
            {
                // Already running at the same port.
                return;
            }

            OpcPlc.Program.Stop();
        }

        _args = args;
        _port = port;

        if (!string.IsNullOrEmpty(endpointUriOverride))
        {
            OpcPlcEndpointUrl = endpointUriOverride;
            _endpointUrlOverridden = true;
            return;
        }

        OpcPlcEndpointUrl = StartOpcPlcServerAsync(CancellationToken.None).GetAwaiter().GetResult();
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

        return OpcPlc.Program.RestartAsync();
    }

    private static async Task<string> WaitForServerUpAsync(Task serverTask, CancellationToken cancellationToken)
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

            if (!OpcPlc.Program.Ready)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                continue;
            }

            _isOpcPlcServerRunning = true;

            return OpcPlc.Program.PlcServer.GetEndpoints()[0].EndpointUrl;
        }
    }

    private Task<string> StartOpcPlcServerAsync(CancellationToken cancellationToken)
    {
        // Passed args override the following defaults.
        var serverTask = Task.Run(
            async () => await OpcPlc.Program.StartAsync(
                _args?.Concat(
                    new[]
                    {
                        "--autoaccept",
                        $"--portnum={_port}",
                    }).ToArray(),
                cancellationToken)
            .ConfigureAwait(false),
            cancellationToken);

        return WaitForServerUpAsync(serverTask, cancellationToken);
    }
}
