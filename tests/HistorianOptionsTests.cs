namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mono.Options;
using NUnit.Framework;
using OpcPlc.PluginNodes;
using System;
using System.Threading;
using System.Threading.Tasks;

public class HistorianOptionsTests
{
    [TestCase(new string[0], false)]
    [TestCase(new[] { "--historian" }, true)]
    [TestCase(new[] { "--hn" }, true)]
    [TestCase(new[] { "--historian", "--historian-" }, false)]
    public void HistorianOptionIsOptIn(string[] arguments, bool expected)
    {
        var plugin = new HistorianPluginNodes(new TimeService(), NullLogger.Instance);
        var options = new OptionSet();
        plugin.AddOptions(options);

        options.Parse(arguments).Should().BeEmpty();

        plugin.Enabled.Should().Be(expected);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HistorianRegistrationPropagatesCancellation(bool enabled)
    {
        var plugin = new HistorianPluginNodes(new TimeService(), NullLogger.Instance);
        var options = new OptionSet();
        plugin.AddOptions(options);
        if (enabled)
        {
            options.Parse(["--historian"]);
        }
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> register = () => plugin.AddToAddressSpaceAsync(
            null, null, null, cancellation.Token).AsTask();
        await register.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
    }
}
