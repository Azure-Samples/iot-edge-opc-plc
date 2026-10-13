namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using OpcPlc.Configuration;
using OpcPlc.Certs;
using OpcPlc.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class KubernetesSecretCertificateStoreTests
{
    [Test]
    public async Task PendingKey_ConcurrentCertificateWritesPreserveEveryEntryAsync()
    {
        var backingClient = new InMemoryKubernetesSecretStoreClient();
        var client = new Mock<IKubernetesSecretStoreClient>();
        client.Setup(instance => instance.ReadAsync("pending-tests", "own", It.IsAny<CancellationToken>()))
            .Returns(async (string ns, string name, CancellationToken ct) =>
            {
                var snapshot = await backingClient.ReadAsync(ns, name, ct).ConfigureAwait(false);
                await Task.Delay(10, ct).ConfigureAwait(false);
                return snapshot;
            });
        client.Setup(instance => instance.WriteAsync("pending-tests", "own",
            It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .Returns((string ns, string name, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct) =>
                backingClient.WriteAsync(ns, name, data, ct));
        using var first = new KubernetesSecretCertificateStore(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, client.Object, "pending-tests");
        using var second = new KubernetesSecretCertificateStore(
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, client.Object, "pending-tests");
        first.Open("KubernetesSecret:own", noPrivateKeys: false);
        second.Open("KubernetesSecret:own", noPrivateKeys: false);
        using Certificate one = DefaultCertificateFactory.Instance
            .CreateApplicationCertificate("urn:opcplc:first", "first", "CN=first", ["localhost"]).CreateForRSA();
        using Certificate two = DefaultCertificateFactory.Instance
            .CreateApplicationCertificate("urn:opcplc:second", "second", "CN=second", ["localhost"]).CreateForRSA();
        var factory = new Mock<IKubernetesSecretStoreClientFactory>();
        factory.Setup(instance => instance.Create()).Returns(client.Object);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry,
            [new KubernetesSecretCertificateStoreType(
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, factory.Object, "pending-tests")]);
        var context = new Opc.Ua.Server.PendingCertificateKeyContext(
            new CertificateStoreIdentifier("KubernetesSecret:own", "KubernetesSecret", noPrivateKeys: false),
            ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup,
            ObjectTypeIds.RsaSha256ApplicationCertificateType, null, telemetry);
        using var pendingStore = new PlcPendingCertificateKeyStore(manager);
        await Task.WhenAll(first.AddAsync(one), second.AddAsync(two),
            pendingStore.SaveAsync(context, one).AsTask()).ConfigureAwait(false);

        using CertificateCollection active = await first.EnumerateAsync().ConfigureAwait(false);
        active.Should().HaveCount(2).And.Contain(certificate => certificate.Thumbprint == one.Thumbprint)
            .And.Contain(certificate => certificate.Thumbprint == two.Thumbprint);
        using Certificate pending = await pendingStore.TryTakeMatchingAsync(context, one).ConfigureAwait(false);
        pending.Should().NotBeNull();
        backingClient.GetSecret("pending-tests", "own").Keys.Should().HaveCount(4);
    }

    [TestCase(true, HttpStatusCode.Forbidden)]
    [TestCase(false, HttpStatusCode.Forbidden)]
    [TestCase(false, HttpStatusCode.Conflict)]
    public async Task PendingKey_StorageFailureIsExplicitAndPreservesSecretAsync(
        bool denyRead, HttpStatusCode statusCode)
    {
        var backingClient = new InMemoryKubernetesSecretStoreClient();
        var client = new Mock<IKubernetesSecretStoreClient>();
        bool fail = false;
        using var response = new HttpResponseMessage(statusCode);
        var failure = new k8s.Autorest.HttpOperationException("Storage failure")
        {
            Response = new k8s.Autorest.HttpResponseMessageWrapper(response, "Storage failure")
        };
        client.Setup(instance => instance.ReadAsync("pending-tests", "own", It.IsAny<CancellationToken>()))
            .Returns((string ns, string name, CancellationToken ct) =>
                fail && denyRead
                    ? Task.FromException<IReadOnlyDictionary<string, byte[]>>(failure)
                    : backingClient.ReadAsync(ns, name, ct));
        client.Setup(instance => instance.WriteAsync("pending-tests", "own",
            It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .Returns((string ns, string name, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct) =>
                fail && !denyRead ? Task.FromException(failure) : backingClient.WriteAsync(ns, name, data, ct));
        var factory = new Mock<IKubernetesSecretStoreClientFactory>();
        factory.Setup(instance => instance.Create()).Returns(client.Object);
        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(instance => instance.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry,
            [new KubernetesSecretCertificateStoreType(loggerFactory.Object, factory.Object, "pending-tests")]);
        var context = new Opc.Ua.Server.PendingCertificateKeyContext(
            new CertificateStoreIdentifier("KubernetesSecret:own", "KubernetesSecret", noPrivateKeys: false),
            ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup,
            ObjectTypeIds.RsaSha256ApplicationCertificateType, null, telemetry);
        using var store = new PlcPendingCertificateKeyStore(manager);
        using Certificate original = DefaultCertificateFactory.Instance
            .CreateApplicationCertificate("urn:opcplc:pending-test", "pending-test", "CN=pending-test", ["localhost"])
            .CreateForRSA();
        using Certificate newer = DefaultCertificateFactory.Instance
            .CreateApplicationCertificate("urn:opcplc:pending-test", "pending-test", "CN=pending-test", ["localhost"])
            .CreateForRSA();
        (await store.SaveAsync(context, original).ConfigureAwait(false)).Should().BeTrue();
        var before = backingClient.GetSecret("pending-tests", "own")
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
        fail = true;
        Func<Task> save = () => store.SaveAsync(context, newer).AsTask();
        if (statusCode == HttpStatusCode.Forbidden)
        {
            await save.Should().ThrowAsync<ServiceResultException>()
                .Where(exception => exception.StatusCode == StatusCodes.BadUserAccessDenied &&
                    exception.InnerException == failure).ConfigureAwait(false);
            logger.Verify(instance => instance.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), failure,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
        else
        {
            await save.Should().ThrowAsync<k8s.Autorest.HttpOperationException>().ConfigureAwait(false);
        }
        backingClient.GetSecret("pending-tests", "own").Should().BeEquivalentTo(before);
        fail = false;
        using Certificate pending = await store.TryTakeMatchingAsync(context, original).ConfigureAwait(false);
        pending.Should().NotBeNull();
        client.Verify(instance => instance.WriteAsync(It.IsAny<string>(), It.Is<string>(name => name != "own"),
            It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Store_AddEnumerateLoadPrivateKeyAndDelete_UsesSecretData()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        var loggerMock = new Mock<ILogger>();
        using var store = new KubernetesSecretCertificateStore(loggerMock.Object, client, "opcplc-tests");
        using var certificate = CreateSelfSignedCertificateWithPrivateKey("k8s-store-app");

        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        using Certificate owned = Certificate.From(new X509Certificate2(certificate));
        await store.AddAsync(owned, ct: CancellationToken.None).ConfigureAwait(false);

        using var certificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        certificates.Should().ContainSingle(certificateInStore => certificateInStore.Thumbprint == certificate.Thumbprint);

        using var matchingCertificates = await store.FindByThumbprintAsync(certificate.Thumbprint, CancellationToken.None).ConfigureAwait(false);
        matchingCertificates.Should().ContainSingle();

        using var certificateWithPrivateKey = await store.LoadPrivateKeyAsync(certificate.Thumbprint, certificate.Subject, string.Empty, CancellationToken.None).ConfigureAwait(false);
        certificateWithPrivateKey.Should().NotBeNull();
        certificateWithPrivateKey.HasPrivateKey.Should().BeTrue();

        var deleted = await store.DeleteAsync(certificate.Thumbprint, CancellationToken.None).ConfigureAwait(false);
        deleted.Should().BeTrue();

        using var remainingCertificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
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
        using var manager = (CertificateManager)appConfig.CertificateManager;

        appConfig.SecurityConfiguration.ApplicationCertificate.StoreType.Should().Be(KubernetesSecretCertificateStore.StoreTypeName);
        appConfig.SecurityConfiguration.TrustedUserCertificates.StorePath.Should().Be(KubernetesSecretCertificateStore.StoreTypePrefix + config.OpcUa.OpcTrustedUserCertStorePath);

        using var trustedUserStore = appConfig.CertificateManager.OpenTrustedStore(TrustListIdentifier.Users);
        using var trustedUserCertificates = await trustedUserStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);

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

        using Certificate owned = Certificate.From(new X509Certificate2(ownCertificate));
        using Certificate trusted = Certificate.From(new X509Certificate2(trustedCertificate));
        await ownStore.AddAsync(owned, ct: CancellationToken.None).ConfigureAwait(false);
        await trustedStore.AddAsync(trusted, ct: CancellationToken.None).ConfigureAwait(false);

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

        using var certificates = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
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
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
    }

    private static X509Certificate2 CreateSelfSignedCertificateWithPrivateKey(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }

    private sealed class InMemoryKubernetesSecretStoreClientFactory : IKubernetesSecretStoreClientFactory
    {
        public InMemoryKubernetesSecretStoreClient Client { get; } = new();

        public IKubernetesSecretStoreClient Create() => Client;
    }

    internal sealed class InMemoryKubernetesSecretStoreClient : IKubernetesSecretStoreClient
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
