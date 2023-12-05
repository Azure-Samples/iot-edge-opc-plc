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
    /// <summary>
    /// Initializes a new instance of the <see cref="OpcPlcBase"/> class.
    /// </summary>
    public OpcPlcBase(string[] args, int port = 51234)
    {
        // Passed args override the following defaults.
        var serverTask = Task.Run(() => OpcPlc.Program.MainAsync(
            args.Concat(
                new[]
                {
                    "--autoaccept",
                    $"--portnum={port}",
                }).ToArray(),
            CancellationToken.None)
            .GetAwaiter().GetResult());

        OpcPlcEndpointUrl = WaitForServerUpAsync(serverTask).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the OPC PLC server endpoint URL.
    /// </summary>
    public string OpcPlcEndpointUrl { get; }

    private static async Task<string> WaitForServerUpAsync(Task serverTask)
    {
        while (true)
        {
            if (serverTask.IsFaulted)
            {
                throw serverTask.Exception!;
            }

            if (serverTask.IsCompleted)
            {
                throw new Exception("Server failed to start.");
            }

            if (!OpcPlc.Program.Ready)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                continue;
            }

            return OpcPlc.Program.PlcServer.GetEndpoints()[0].EndpointUrl;
        }
    }
}
