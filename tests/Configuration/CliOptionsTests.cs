namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using NUnit.Framework;
using OpcPlc.Configuration;
using System.Collections.Immutable;
using System.IO;
using System.Linq;


[TestFixture]
public class CliOptionsTests
{
    [Test]
    public void Parse_AddTrustedUserCertBase64_PopulatesConfigList()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--tub=Zm9vYmFyYmFzZTY0" }; // "foobarbase64" as a dummy base64 string
        var pluginNodes = ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

        // Act
        _ = CliOptions.InitConfiguration(args, config, pluginNodes);

        // Assert
        config.OpcUa.TrustedUserCertificateBase64Strings.Should().NotBeNull();
        config.OpcUa.TrustedUserCertificateBase64Strings.Count.Should().Be(1);
        config.OpcUa.TrustedUserCertificateBase64Strings.Single().Should().Be("Zm9vYmFyYmFzZTY0");
    }

    [Test]
    public void Parse_AddTrustedUserCertFile_PopulatesConfigFileList()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        string tmpFile = Path.GetTempFileName();
        File.WriteAllText(tmpFile, "dummy");
        try
        {
            var args = new[] { $"--tuf={tmpFile}" };
            var pluginNodes = ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

            // Act
            _ = CliOptions.InitConfiguration(args, config, pluginNodes);

            // Assert
            config.OpcUa.TrustedUserCertificateFileNames.Should().NotBeNull();
            config.OpcUa.TrustedUserCertificateFileNames.Count.Should().Be(1);
            config.OpcUa.TrustedUserCertificateFileNames.Single().Should().Be(tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Test]
    public void Parse_AddUserIssuerCertFile_PopulatesConfigFileList()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        // create a temp file to satisfy the parser helper
        string tmpFile = Path.GetTempFileName();
        File.WriteAllText(tmpFile, "dummy");
        try
        {
            var args = new[] { $"--uif={tmpFile}" };
            var pluginNodes = ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

            // Act
            _ = CliOptions.InitConfiguration(args, config, pluginNodes);

            // Assert
            config.OpcUa.UserIssuerCertificateFileNames.Should().NotBeNull();
            config.OpcUa.UserIssuerCertificateFileNames.Count.Should().Be(1);
            config.OpcUa.UserIssuerCertificateFileNames.Single().Should().Be(tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Test]
    public void Parse_AddUserIssuerCertBase64_PopulatesConfigList()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var args = new[] { "--uib=QmFzZTY0VXNlcklzc3Vlcg==" }; // "Base64UserIssuer" dummy
        var pluginNodes = ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

        // Act
        _ = CliOptions.InitConfiguration(args, config, pluginNodes);

        // Assert
        config.OpcUa.UserIssuerCertificateBase64Strings.Should().NotBeNull();
        config.OpcUa.UserIssuerCertificateBase64Strings.Count.Should().Be(1);
        config.OpcUa.UserIssuerCertificateBase64Strings.Single().Should().Be("QmFzZTY0VXNlcklzc3Vlcg==");
    }

    [Test]
    public void Parse_TrustedUserAndUserIssuerStorePaths_SetConfigPaths()
    {
        // Arrange
        var config = new OpcPlcConfiguration();
        var trustedUserPath = "pki/custom-trusted-user";
        var userIssuerPath = "pki/custom-user-issuer";
        var args = new[] { $"--tup={trustedUserPath}", $"--uip={userIssuerPath}" };
        var pluginNodes = ImmutableList<OpcPlc.PluginNodes.Models.IPluginNodes>.Empty;

        // Act
        _ = CliOptions.InitConfiguration(args, config, pluginNodes);

        // Assert
        config.OpcUa.OpcTrustedUserCertStorePath.Should().Be(trustedUserPath);
        config.OpcUa.OpcUserIssuerCertStorePath.Should().Be(userIssuerPath);
    }
}
