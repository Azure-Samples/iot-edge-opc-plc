namespace OpcPlc.Extensions;

using System;
using System.Threading;
using System.Threading.Tasks;

public static class CancellationTokenExtensions
{
    /// <summary>
    /// Extension method to await a cancellation token.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task WhenCanceled(this CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
        return tcs.Task;
    }

    private static string ROLE_INSTANCE
    {
        get
        {
            return System.Environment.MachineName;
        }
    }

    private static string SIMULATION_ID
    {
        get
        {
            return Environment.GetEnvironmentVariable("SIMULATION_ID");
        }
    }

    private static string KUBERNETES_NODE
    {
        get
        {
            return Environment.GetEnvironmentVariable("KUBERNETES_NODE");
        }
    }

    private static string CLUSTER_NAME
    {
        get
        {
            return Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");
        }
    }

    private static string BUILD_NUMBER
    {
        get
        {
            return Environment.GetEnvironmentVariable("BUILD_NUMBER");
        }
    }
}
