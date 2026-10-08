namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mono.Options;
using NUnit.Framework;
using OpcPlc.PluginNodes;

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
}