namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using OpcPlc.Configuration;
using OpcPlc.Certs;
using OpcPlc.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class KubernetesSecretCertificateStoreTests
{
    [Test]
    public async Task Store_AddEnumerateLoadPrivateKeyAndDelete_UsesSecretData()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        var loggerMock = new Mock<ILogger>();
        using var store = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var certificate = CreateSelfSignedCertificateWithPrivateKey("k8s-store-app");

        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        await store.AddAsync(certificate, ct: CancellationToken.None).ConfigureAwait(false);

        var certificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        certificates.Should().ContainSingle(certificateInStore => certificateInStore.Thumbprint == certificate.Thumbprint);

        var matchingCertificates = await store.FindByThumbprintAsync(certificate.Thumbprint, CancellationToken.None).ConfigureAwait(false);
        matchingCertificates.Should().ContainSingle();

        using var certificateWithPrivateKey = await store.LoadPrivateKeyAsync(certificate.Thumbprint, certificate.Subject, string.Empty, CancellationToken.None).ConfigureAwait(false);
        certificateWithPrivateKey.Should().NotBeNull();
        certificateWithPrivateKey.HasPrivateKey.Should().BeTrue();

        var deleted = await store.DeleteAsync(certificate.Thumbprint, CancellationToken.None).ConfigureAwait(false);
        deleted.Should().BeTrue();

        var remainingCertificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        remainingCertificates.Should().BeEmpty();
        client.GetSecret("opcplc-tests", "pki-own").Should().NotBeNull();
    }

    [Test]
    public async Task ConfigureAsync_KubernetesSecretStore_WiresTrustedUserStore()
    {
        var fakeFactory = new InMemoryKubernetesSecretStoreClientFactory();
        var config = new OpcPlcConfiguration();
        config.OpcUa.OpcOwnCertStoreType = KubernetesSecretCertificateStore.StoreTypeName;
        config.OpcUa.OpcKubernetesSecretNamespace = "opcplc-tests";
        config.OpcUa.OpcOwnCertStorePath = @"pki\own";
        config.OpcUa.OpcTrustedCertStorePath = @"pki\trusted";
        config.OpcUa.OpcRejectedCertStorePath = @"pki\rejected";
        config.OpcUa.OpcIssuerCertStorePath = @"pki\issuer";
        config.OpcUa.OpcTrustedUserCertStorePath = @"trusted-users";
        config.OpcUa.OpcUserIssuerCertStorePath = @"user-issuers";

        using var trustedUserCertificate = CreateSelfSignedCertificate("trusted-user");
        config.OpcUa.TrustedUserCertificateBase64Strings = [Convert.ToBase64String(trustedUserCertificate.Export(X509ContentType.Cert))];

        var loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
        var telemetryContext = new OpcTelemetryContext(loggerFactoryMock.Object, "Opc.Ua", OpcTelemetryContext.ResolveOpcPlcVersion());

        var factory = new OpcUaAppConfigFactory(config, loggerMock.Object, loggerFactoryMock.Object, telemetryContext, fakeFactory);

        var appConfig = await factory.ConfigureAsync().ConfigureAwait(false);

        appConfig.SecurityConfiguration.ApplicationCertificate.StoreType.Should().Be(KubernetesSecretCertificateStore.StoreTypeName);
        appConfig.SecurityConfiguration.TrustedUserCertificates.StorePath.Should().Be(KubernetesSecretCertificateStore.StoreTypePrefix + config.OpcUa.OpcTrustedUserCertStorePath);

        using var trustedUserStore = appConfig.SecurityConfiguration.TrustedUserCertificates.OpenStore(telemetryContext);
        var trustedUserCertificates = await trustedUserStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);

        trustedUserCertificates.Should().Contain(certificate => certificate.Thumbprint == trustedUserCertificate.Thumbprint);
        fakeFactory.Client.GetSecret("opcplc-tests", "trusted-users").Should().ContainKey(trustedUserCertificate.Thumbprint.ToUpperInvariant() + ".der");
        fakeFactory.Client.GetSecret("opcplc-tests", "pki-own").Should().NotBeNull();
        fakeFactory.Client.GetSecret("opcplc-tests", "pki-own").Should().NotBeSameAs(fakeFactory.Client.GetSecret("opcplc-tests", "trusted-users"));
    }

    [Test]
    public async Task Stores_WithDifferentPaths_UseDifferentSecrets()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        var loggerMock = new Mock<ILogger>();
        using var ownStore = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var trustedStore = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var ownCertificate = CreateSelfSignedCertificateWithPrivateKey("own-app");
        using var trustedCertificate = CreateSelfSignedCertificate("trusted-peer");

        ownStore.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);
        trustedStore.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted", noPrivateKeys: true);

        await ownStore.AddAsync(ownCertificate, ct: CancellationToken.None).ConfigureAwait(false);
        await trustedStore.AddAsync(trustedCertificate, ct: CancellationToken.None).ConfigureAwait(false);

        var ownSecret = client.GetSecret("opcplc-tests", "pki-own");
        var trustedSecret = client.GetSecret("opcplc-tests", "pki-trusted");

        ownSecret.Should().NotBeNull();
        trustedSecret.Should().NotBeNull();
        ownSecret.Should().ContainKey(ownCertificate.Thumbprint.ToUpperInvariant() + ".der");
        ownSecret.Should().ContainKey(ownCertificate.Thumbprint.ToUpperInvariant() + ".pfx");
        trustedSecret.Should().ContainKey(trustedCertificate.Thumbprint.ToUpperInvariant() + ".der");
        trustedSecret.Keys.Should().OnlyContain(key => key.EndsWith(".der", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Store_LoadPrivateKeyAsync_SupportsCrtAndKeyEntries()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        var loggerMock = new Mock<ILogger>();
        using var store = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var certificate = CreateSelfSignedCertificateWithPrivateKey("pem-backed-app");
        using var rsa = certificate.GetRSAPrivateKey();

        client.SetSecret(
            "opcplc-tests",
            "pki-own",
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["tls.crt"] = Encoding.UTF8.GetBytes(certificate.ExportCertificatePem()),
                ["tls.key"] = Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()),
            });

        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        var certificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        certificates.Should().ContainSingle(certificateInStore => certificateInStore.Thumbprint == certificate.Thumbprint);

        using var certificateWithPrivateKey = await store.LoadPrivateKeyAsync(certificate.Thumbprint, certificate.Subject, string.Empty, CancellationToken.None).ConfigureAwait(false);
        certificateWithPrivateKey.Should().NotBeNull();
        certificateWithPrivateKey.HasPrivateKey.Should().BeTrue();
    }

    [Test]
    public async Task Store_LoadPrivateKeyAsync_SupportsCrtAndPemEntries()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        var loggerMock = new Mock<ILogger>();
        using var store = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var certificate = CreateSelfSignedCertificateWithPrivateKey("pem-file-app");
        using var rsa = certificate.GetRSAPrivateKey();

        client.SetSecret(
            "opcplc-tests",
            "pki-own",
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["app.crt"] = Encoding.UTF8.GetBytes(certificate.ExportCertificatePem()),
                ["app.pem"] = Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()),
            });

        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        using var certificateWithPrivateKey = await store.LoadPrivateKeyAsync(certificate.Thumbprint, certificate.Subject, string.Empty, CancellationToken.None).ConfigureAwait(false);
        certificateWithPrivateKey.Should().NotBeNull();
        certificateWithPrivateKey.HasPrivateKey.Should().BeTrue();
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
    }

    private static X509Certificate2 CreateSelfSignedCertificateWithPrivateKey(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }

    private sealed class InMemoryKubernetesSecretStoreClientFactory : IKubernetesSecretStoreClientFactory
    {
        public InMemoryKubernetesSecretStoreClient Client { get; } = new();

        public IKubernetesSecretStoreClient Create() => Client;
    }

    private sealed class InMemoryKubernetesSecretStoreClient : IKubernetesSecretStoreClient
    {
        private readonly Dictionary<(string Namespace, string SecretName), Dictionary<string, byte[]>> _secrets = [];

        public Task<IReadOnlyDictionary<string, byte[]>> ReadAsync(string namespaceName, string secretName, CancellationToken ct = default)
        {
            if (_secrets.TryGetValue((namespaceName, secretName), out var secretData))
            {
                return Task.FromResult<IReadOnlyDictionary<string, byte[]>>(secretData.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
            }

            return Task.FromResult<IReadOnlyDictionary<string, byte[]>>(new Dictionary<string, byte[]>(StringComparer.Ordinal));
        }

        public Task WriteAsync(string namespaceName, string secretName, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct = default)
        {
            _secrets[(namespaceName, secretName)] = data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
            return Task.CompletedTask;
        }

        public void SetSecret(string namespaceName, string secretName, IReadOnlyDictionary<string, byte[]> data)
        {
            _secrets[(namespaceName, secretName)] = data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
        }

        public Dictionary<string, byte[]> GetSecret(string namespaceName, string secretName)
        {
            return _secrets.TryGetValue((namespaceName, secretName), out var secretData)
                ? secretData
                : null;
        }
    }
}
