namespace OpcPlc.Extensions;

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
}
