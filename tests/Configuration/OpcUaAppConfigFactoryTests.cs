namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OpcPlc.Configuration;
using OpcPlc.Certs;
using Opc.Ua.Security.Certificates;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

[TestFixture]
public class OpcUaAppConfigFactoryTests
{
    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        // Ensure exported cert without private key for trusted stores
        return X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
    }

    [Test]
    public async Task ConfigureAsync_AddTrustedUserCertBase64_WritesToTrustedUserStore()
    {
        // Arrange - temp pki layout
        string root = Path.Combine(Path.GetTempPath(), "opcplc_test_pki_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new OpcPlcConfiguration();
            // Use FlatDirectory store type to avoid machine store operations.
            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
            // point all stores under our temp root
            config.OpcUa.OpcOwnCertStorePath = Path.Combine(root, "own");
            config.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "trusted");
            config.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "rejected");
            config.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "issuer");
            config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "trusted-user");
            config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "issuer-user");

            // Ensure directories exist
            Directory.CreateDirectory(config.OpcUa.OpcOwnCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcTrustedCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcRejectedCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcIssuerCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcTrustedUserCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcUserIssuerCertStorePath);

            // create self-signed cert and encode as base64 DER
            var cert = CreateSelfSignedCertificate("unit-test-user");
            var der = cert.Export(X509ContentType.Cert);
            var base64 = Convert.ToBase64String(der);

            // place into config lists so Init will add them
            config.OpcUa.TrustedUserCertificateBase64Strings = new System.Collections.Generic.List<string> { base64 };

            // mocks
            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object);

            // Act
            var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

            // Assert - open the configured trusted user store and find our certificate
            using var store = appConfig.SecurityConfiguration.TrustedUserCertificates.OpenStore();
            var certs = await store.Enumerate().ConfigureAwait(false);

            certs.Should().NotBeNull();
            certs.Count.Should().BeGreaterThanOrEqualTo(1, "Trusted user store should contain at least one certificate");
            bool found = false;
            foreach (var c in certs)
            {
                if (string.Equals(c.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue("Added trusted user certificate must appear in the trusted user store");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { };
        }
    }

    [Test]
    public async Task ConfigureAsync_AddTrustedUserCertFile_WritesToTrustedUserStore()
    {
        // Arrange - temp pki layout
        string root = Path.Combine(Path.GetTempPath(), "opcplc_test_pki_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string certFile = Path.Combine(Path.GetTempPath(), "trusteduser_" + Guid.NewGuid().ToString("N") + ".der");
        try
        {
            var config = new OpcPlcConfiguration();
            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
            config.OpcUa.OpcOwnCertStorePath = Path.Combine(root, "own");
            config.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "trusted");
            config.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "rejected");
            config.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "issuer");
            config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "trusted-user");
            config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "issuer-user");

            Directory.CreateDirectory(config.OpcUa.OpcTrustedUserCertStorePath);

            // create cert and write to temp file in DER
            var cert = CreateSelfSignedCertificate("unit-test-trusted-user");
            var der = cert.Export(X509ContentType.Cert);
            await File.WriteAllBytesAsync(certFile, der).ConfigureAwait(false);

            config.OpcUa.TrustedUserCertificateFileNames = new System.Collections.Generic.List<string> { certFile };

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object);

            // Act
            var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

            // Assert - open the configured trusted user store and find our certificate
            using var store = appConfig.SecurityConfiguration.TrustedUserCertificates.OpenStore();
            var certs = await store.Enumerate().ConfigureAwait(false);

            certs.Should().NotBeNull();
            certs.Count.Should().BeGreaterThanOrEqualTo(1, "Trusted user store should contain at least one certificate");
            bool found = false;
            foreach (var c in certs)
            {
                if (string.Equals(c.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue("Added trusted user certificate must appear in the trusted user store");
        }
        finally
        {
            try { File.Delete(certFile); } catch{ };
            try { Directory.Delete(root, recursive: true); } catch { };
        }
    }

    [Test]
    public async Task ConfigureAsync_AddUserIssuerCertFile_WritesToUserIssuerStore()
    {
        // Arrange - temp pki layout
        string root = Path.Combine(Path.GetTempPath(), "opcplc_test_pki_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new OpcPlcConfiguration();
            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
            config.OpcUa.OpcOwnCertStorePath = Path.Combine(root, "own");
            config.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "trusted");
            config.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "rejected");
            config.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "issuer");
            config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "trusted-user");
            config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "issuer-user");

            Directory.CreateDirectory(config.OpcUa.OpcUserIssuerCertStorePath);

            // create cert and write to temp file in DER
            var cert = CreateSelfSignedCertificate("unit-test-issuer");
            var der = cert.Export(X509ContentType.Cert);
            string certFile = Path.Combine(Path.GetTempPath(), "userissuer_" + Guid.NewGuid().ToString("N") + ".der");
            await File.WriteAllBytesAsync(certFile, der).ConfigureAwait(false);

            config.OpcUa.UserIssuerCertificateFileNames = new System.Collections.Generic.List<string> { certFile };

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object);

            // Act
            var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

            // Assert
            using var store = appConfig.SecurityConfiguration.UserIssuerCertificates.OpenStore();
            var certs = await store.Enumerate().ConfigureAwait(false);

            certs.Should().NotBeNull();
            certs.Count.Should().BeGreaterThanOrEqualTo(1, "User issuer store should contain at least one certificate");
            bool found = false;
            foreach (var c in certs)
            {
                if (string.Equals(c.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue("Added user issuer certificate must appear in the user issuer store");

            // cleanup temp file
            File.Delete(certFile);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { };
        }
    }

    [Test]
    public async Task ConfigureAsync_AddUserIssuerCertBase64_WritesToUserIssuerStore()
    {
        // Arrange - temp pki layout
        string root = Path.Combine(Path.GetTempPath(), "opcplc_test_pki_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new OpcPlcConfiguration();
            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
            config.OpcUa.OpcOwnCertStorePath = Path.Combine(root, "own");
            config.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "trusted");
            config.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "rejected");
            config.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "issuer");
            config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "trusted-user");
            config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "issuer-user");

            Directory.CreateDirectory(config.OpcUa.OpcUserIssuerCertStorePath);

            // create self-signed cert and encode as base64 DER
            var cert = CreateSelfSignedCertificate("unit-test-user-issuer");
            var der = cert.Export(X509ContentType.Cert);
            var base64 = Convert.ToBase64String(der);

            // place into config lists so Init will add them
            config.OpcUa.UserIssuerCertificateBase64Strings = new System.Collections.Generic.List<string> { base64 };

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object);

            // Act
            var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

            // Assert - open the configured user issuer store and find our certificate
            using var store = appConfig.SecurityConfiguration.UserIssuerCertificates.OpenStore();
            var certs = await store.Enumerate().ConfigureAwait(false);

            certs.Should().NotBeNull();
            certs.Count.Should().BeGreaterThanOrEqualTo(1, "User issuer store should contain at least one certificate");
            bool found = false;
            foreach (var c in certs)
            {
                if (string.Equals(c.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue("Added user issuer certificate must appear in the user issuer store");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { };
        }
    }

    [Test]
    public async Task ConfigureAsync_UserCertStorePaths_AppliedToSecurityConfiguration()
    {
        // Arrange - custom store paths
        string root = Path.Combine(Path.GetTempPath(), "opcplc_test_pki_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new OpcPlcConfiguration();
            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;

            config.OpcUa.OpcOwnCertStorePath = Path.Combine(root, "own");
            config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "my-trusted-users");
            config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "my-user-issuers");
            config.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "trusted");
            config.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "issuer");
            config.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "rejected");

            // Ensure directories exist
            Directory.CreateDirectory(config.OpcUa.OpcOwnCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcTrustedUserCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcUserIssuerCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcTrustedCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcIssuerCertStorePath);
            Directory.CreateDirectory(config.OpcUa.OpcRejectedCertStorePath);

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object);

            // Act
            var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

            // Assert - store paths should include FlatDirectory prefix
            var expectedTrustedUserPath = FlatDirectoryCertificateStore.StoreTypePrefix + config.OpcUa.OpcTrustedUserCertStorePath;
            var expectedUserIssuerPath = FlatDirectoryCertificateStore.StoreTypePrefix + config.OpcUa.OpcUserIssuerCertStorePath;

            appConfig.SecurityConfiguration.TrustedUserCertificates.StorePath.Should().Be(expectedTrustedUserPath);
            appConfig.SecurityConfiguration.UserIssuerCertificates.StorePath.Should().Be(expectedUserIssuerPath);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { };
        }
    }
}
