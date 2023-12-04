namespace UnitTests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class OpcPlcBase
{
    public OpcPlcBase(string[] args, int port = 51234)
    {
        // Passed args override the following defaults.
        var serverTask = Task.Run(() => OpcPlc.Program.MainAsync(
            args.Concat(
                new[]
                {
                    "--autoaccept",
                    $"--portnum={port}",
                    "--fn=25",
                    "--fr=1",
                    "--ft=uint",
                }).ToArray(),
            CancellationToken.None)
            .GetAwaiter().GetResult());

        OpcPlcEndpointUrl = WaitForServerUpAsync(serverTask).GetAwaiter().GetResult();
    }

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
