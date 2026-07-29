namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using NUnit.Framework;
using OpcPlc.Configuration;
using System;
using System.Collections.Immutable;

[TestFixture]
public class CliOptionsReverseConnectTests
{
    private static ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes> NoPluginNodes
        => ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

    [Test]
    public void Parse_ReverseConnect_IsDisabledByDefault()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = Array.Empty<string>();

        // Act
        _ = CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        config.OpcUa.ReverseConnectClientUrls.Should().BeEmpty();
        config.OpcUa.ReverseConnectInterval.Should().Be(15_000);
        config.OpcUa.ReverseConnectTimeout.Should().Be(30_000);
        config.OpcUa.ReverseConnectRejectTimeout.Should().Be(60_000);
        config.OpcUa.ReverseConnectMaxSessionCount.Should().Be(0);
    }

    [Test]
    public void Parse_ReverseConnectClients_PopulatesSingleUrl()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--rcc=opc.tcp://client:65300" };

        // Act
        _ = CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        config.OpcUa.ReverseConnectClientUrls.Should().ContainSingle()
            .Which.Should().Be("opc.tcp://client:65300");
    }

    [Test]
    public void Parse_ReverseConnectClients_PopulatesMultipleUrls()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--reverseconnectclients=opc.tcp://client1:65300,opc.tcp://client2:65301" };

        // Act
        _ = CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        config.OpcUa.ReverseConnectClientUrls.Should()
            .Equal("opc.tcp://client1:65300", "opc.tcp://client2:65301");
    }

    [TestCase("not-a-valid-url")]
    [TestCase("http://client:65300")]
    [TestCase("opc.tcp://client1:65300,bogus")]
    [TestCase("opc.tcp://client")]
    [TestCase("opc.tcp://:65300")]
    [TestCase("opc.tcp://")]
    public void Parse_ReverseConnectClients_InvalidUrl_Throws(string value)
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { $"--rcc={value}" };

        // Act
        var act = () => CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        act.Should().Throw<Mono.Options.OptionException>()
            .WithMessage("*expected format: opc.tcp://<host>:<port>*");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Parse_ReverseConnectClients_EmptyValue_ThrowsOptionException(string value)
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { $"--rcc={value}" };

        // Act
        var act = () => CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        act.Should().Throw<Mono.Options.OptionException>("an empty value must not escape as an IndexOutOfRangeException");
    }

    [Test]
    public void Parse_ReverseConnectClients_TrimsSpacesAroundUrls()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--rcc=opc.tcp://client1:65300, opc.tcp://client2:65301" };

        // Act
        _ = CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        config.OpcUa.ReverseConnectClientUrls.Should()
            .Equal("opc.tcp://client1:65300", "opc.tcp://client2:65301");
    }

    [Test]
    public void Parse_ReverseConnectTimings_SetConfigValues()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--rci=1000", "--rctm=2000", "--rcrt=3000", "--rcms=4" };

        // Act
        _ = CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        config.OpcUa.ReverseConnectInterval.Should().Be(1000);
        config.OpcUa.ReverseConnectTimeout.Should().Be(2000);
        config.OpcUa.ReverseConnectRejectTimeout.Should().Be(3000);
        config.OpcUa.ReverseConnectMaxSessionCount.Should().Be(4);
    }

    [TestCase("--rci=0")]
    [TestCase("--rctm=0")]
    [TestCase("--rcrt=0")]
    [TestCase("--rcms=-1")]
    public void Parse_ReverseConnectTimings_OutOfRange_Throws(string arg)
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { arg };

        // Act
        var act = () => CliOptions.InitConfiguration(args, config, NoPluginNodes);

        // Assert
        act.Should().Throw<Mono.Options.OptionException>();
    }
}
