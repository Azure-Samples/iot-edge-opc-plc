namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Configuration;
using OpcPlc.Certs;
using OpcPlc.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
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

    [TestCase(".key")]
    [TestCase(".pem")]
    [TestCase(".pfx")]
    public async Task Store_DeleteAsync_RemovesPemCrtEntryAndCompanionKey(string keyExtension)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(new Mock<ILogger>().Object, client, "opcplc-tests");
        using var seeded = CreateSelfSignedCertificateWithPrivateKey("seeded-peer");
        using var other = CreateSelfSignedCertificate("other-peer");
        using var rsa = seeded.GetRSAPrivateKey();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["client.crt"] = Encoding.UTF8.GetBytes(seeded.ExportCertificatePem()),
            ["client" + keyExtension] = keyExtension == ".pfx" ? seeded.Export(X509ContentType.Pkcs12)
                : Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()),
            [other.Thumbprint + ".der"] = other.Export(X509ContentType.Cert),
        });
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted", noPrivateKeys: false);

        (await store.DeleteAsync(seeded.Thumbprint, CancellationToken.None).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-trusted").Keys.Should().BeEquivalentTo([other.Thumbprint + ".der"]);
        using var remaining = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        remaining.Select(certificate => certificate.Thumbprint).Should().BeEquivalentTo([other.Thumbprint]);
        (await store.DeleteAsync(seeded.Thumbprint, CancellationToken.None).ConfigureAwait(false)).Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Store_DeleteAsync_RewritesPemBundleWithoutDeletedCertificate(bool deleteLeaf)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(new Mock<ILogger>().Object, client, "opcplc-tests");
        using var leaf = CreateSelfSignedCertificateWithPrivateKey("bundle-leaf");
        using var second = CreateSelfSignedCertificate("bundle-second");
        using var rsa = leaf.GetRSAPrivateKey();
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["chain.crt"] = Encoding.UTF8.GetBytes(leaf.ExportCertificatePem() + "\n" + second.ExportCertificatePem()),
            ["chain.key"] = Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem()),
        });
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);
        var (deleted, kept) = deleteLeaf ? (leaf, second) : (second, leaf);

        (await store.DeleteAsync(deleted.Thumbprint, CancellationToken.None).ConfigureAwait(false)).Should().BeTrue();

        using var remaining = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
        remaining.Select(certificate => certificate.Thumbprint).Should().BeEquivalentTo([kept.Thumbprint]);
        client.GetSecret("opcplc-tests", "pki-own").ContainsKey("chain.key").Should().Be(!deleteLeaf,
            "the companion key belongs to the leaf certificate");
        if (!deleteLeaf)
        {
            using var withKey = await store.LoadPrivateKeyAsync(leaf.Thumbprint, leaf.Subject, string.Empty,
                CancellationToken.None).ConfigureAwait(false);
            withKey.HasPrivateKey.Should().BeTrue();
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(100)]
    public async Task Store_DeleteAsync_PreservesOtherPemContentByteForByte(int survivorCount)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var removed = CreateSelfSignedCertificate("removed");
        using var survivor = CreateSelfSignedCertificateWithPrivateKey("survivor");
        using var key = survivor.GetRSAPrivateKey();
        string prefix = "# deployment metadata\r\n" + key.ExportPkcs8PrivateKeyPem() + "\r\n";
        string suffix = "\r\n" + string.Concat(Enumerable.Repeat(survivor.ExportCertificatePem() + "\r\n",
            survivorCount)) + "-----BEGIN CERTIFICATE-----\r\nnot-base64\r\n-----END CERTIFICATE-----\r\n";
        byte[] expected = Encoding.UTF8.GetBytes(prefix + suffix);
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            ["bundle.crt"] = Encoding.UTF8.GetBytes(prefix + removed.ExportCertificatePem() + suffix)
        });
        store.Open("KubernetesSecret:pki-own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own")["bundle.crt"].Should().Equal(expected);
        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeFalse();
    }

    [TestCase(".pfx", false)]
    [TestCase(".pfx", true)]
    [TestCase(".pem", true)]
    [TestCase(".key", true)]
    public async Task Store_DeleteAsync_PreservesSurvivorCompanionKey(string keyExtension, bool separateEntry)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var removed = CreateSelfSignedCertificate("removed");
        using var survivor = CreateSelfSignedCertificateWithPrivateKey("survivor");
        using var key = survivor.GetRSAPrivateKey();
        byte[] keyData = keyExtension == ".pfx"
            ? survivor.Export(X509ContentType.Pkcs12)
            : Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem());
        var data = new Dictionary<string, byte[]>
        {
            ["chain.crt"] = Encoding.UTF8.GetBytes(separateEntry ? survivor.ExportCertificatePem()
                : removed.ExportCertificatePem() + "\n" + survivor.ExportCertificatePem()),
            ["chain" + keyExtension] = keyData
        };
        if (separateEntry)
        {
            data["chain.der"] = removed.Export(X509ContentType.Cert);
        }
        client.SetSecret("opcplc-tests", "pki-own", data);
        store.Open("KubernetesSecret:pki-own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own")["chain" + keyExtension].Should().Equal(keyData);
        using var withKey = await store.LoadPrivateKeyAsync(survivor.Thumbprint, survivor.Subject, string.Empty)
            .ConfigureAwait(false);
        withKey.Should().NotBeNull();
        withKey.Thumbprint.Should().Be(survivor.Thumbprint);
        withKey.HasPrivateKey.Should().BeTrue();
    }

    [Test]
    public async Task Store_DeleteAsync_PreservesKeySharedByRenewedCertificate()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=shared-key", key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var removed = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var survivor = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        byte[] keyData = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem());
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            ["chain.der"] = removed.Export(X509ContentType.Cert),
            ["chain.crt"] = Encoding.UTF8.GetBytes(survivor.ExportCertificatePem()),
            ["chain.key"] = keyData
        });
        store.Open("KubernetesSecret:pki-own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own")["chain.key"].Should().Equal(keyData);
        using var withKey = await store.LoadPrivateKeyAsync(survivor.Thumbprint, survivor.Subject, string.Empty)
            .ConfigureAwait(false);
        withKey.HasPrivateKey.Should().BeTrue();
    }

    [TestCase(".pem")]
    [TestCase(".pfx")]
    public async Task Store_DeleteAsync_PreservesCompanionWhenOwnershipIsUnknown(string keyExtension)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var key = removed.GetRSAPrivateKey();
        byte[] keyData = keyExtension == ".pfx"
            ? removed.Export(X509ContentType.Pkcs12, "test-password")
            : Encoding.UTF8.GetBytes(key.ExportEncryptedPkcs8PrivateKeyPem("test-password",
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000)));
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            ["client.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem()),
            ["client" + keyExtension] = keyData
        });
        store.Open("KubernetesSecret:pki-own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own")["client" + keyExtension].Should().Equal(keyData);
    }

    [Test]
    public async Task Store_DeleteAsync_PreservesUnreadableCertificateAndItsPossibleKey()
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var key = removed.GetRSAPrivateKey();
        string unreadable = PemEncoding.WriteString("CERTIFICATE", [1, 2, 3]);
        byte[] keyData = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem());
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            ["client.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem() + "\n" + unreadable),
            ["client.key"] = keyData
        });
        store.Open("KubernetesSecret:pki-own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own")["client.crt"]
            .Should().Equal(Encoding.UTF8.GetBytes("\n" + unreadable));
        client.GetSecret("opcplc-tests", "pki-own")["client.key"].Should().Equal(keyData);
    }

    [TestCase("renewed-with-same-key")]
    [TestCase("password-protected")]
    [TestCase("unrelated-unreadable-entry")]
    public async Task Store_DeleteAsync_RemovesCanonicalPrivateKey(string scenario)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=canonical", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using Certificate removed = Certificate.From(
            request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
        using Certificate renewed = Certificate.From(
            request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2)));
        string password = scenario == "password-protected" ? "store-password" : null;
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);
        await store.AddAsync(removed, password?.ToCharArray()).ConfigureAwait(false);
        if (scenario == "renewed-with-same-key")
        {
            await store.AddAsync(renewed).ConfigureAwait(false);
        }
        byte[] unreadable = Encoding.UTF8.GetBytes(PemEncoding.WriteString("CERTIFICATE", [1, 2, 3]));
        if (scenario == "unrelated-unreadable-entry")
        {
            var data = client.GetSecret("opcplc-tests", "pki-own");
            data["seeded.crt"] = unreadable;
            client.SetSecret("opcplc-tests", "pki-own", data);
        }

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        var secret = client.GetSecret("opcplc-tests", "pki-own");
        secret.Should().NotContainKey(removed.Thumbprint + ".der")
            .And.NotContainKey(removed.Thumbprint + ".pfx", "the deleted certificate's private key must not linger");
        using var orphan = await store.LoadPrivateKeyAsync(removed.Thumbprint, removed.Subject, password)
            .ConfigureAwait(false);
        orphan.Should().BeNull();
        if (scenario == "renewed-with-same-key")
        {
            secret.Should().ContainKey(renewed.Thumbprint + ".der").And.ContainKey(renewed.Thumbprint + ".pfx");
            using var survivor = await store.LoadPrivateKeyAsync(renewed.Thumbprint, renewed.Subject, null)
                .ConfigureAwait(false);
            survivor.HasPrivateKey.Should().BeTrue();
        }
        if (scenario == "unrelated-unreadable-entry")
        {
            secret["seeded.crt"].Should().Equal(unreadable);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Store_TrustListApply_WithDuplicatesPreservesRequestedCertificatesAsync(bool keepCertificate)
    {
        var clientFactory = new InMemoryKubernetesSecretStoreClientFactory();
        using Certificate certificate = DefaultCertificateFactory.Instance.CreateApplicationCertificate(
            "urn:opcplc:duplicate-peer", "duplicate-peer", "CN=duplicate-peer", ["localhost"]).CreateForRSA();
        clientFactory.Client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>
        {
            ["client.crt"] = Encoding.ASCII.GetBytes(PemEncoding.WriteString("CERTIFICATE", certificate.RawData))
        });
        using var store = new KubernetesSecretCertificateStore(
            NullLogger.Instance, clientFactory.Client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        await store.AddAsync(certificate).ConfigureAwait(false);

        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry,
            [new KubernetesSecretCertificateStoreType(NullLoggerFactory.Instance, clientFactory, "opcplc-tests")]);
        var context = new SessionSystemContext(telemetry)
        {
            SessionId = new NodeId(Guid.NewGuid(), 1),
            NamespaceUris = new NamespaceTable(),
            ServerUris = new StringTable(),
            EncodeableFactory = EncodeableFactory.Create()
        };
        var node = new TrustListState(null) { NodeId = new NodeId("DuplicateTrustList", 1) };
        node.CreateOrReplaceOpen(context, node);
        node.CreateOrReplaceClose(context, node);
        node.CreateOrReplaceRead(context, node);
        node.CreateOrReplaceWrite(context, node);
        node.CreateOrReplaceOpenCount(context, node);
        node.CreateOrReplaceOpenWithMasks(context, node);
        node.CreateOrReplaceCloseAndUpdate(context, node);
        node.CreateOrReplaceAddCertificate(context, node);
        node.CreateOrReplaceRemoveCertificate(context, node);
        node.CreateOrReplaceLastUpdateTime(context, node);
        var coordinator = new PushConfigurationTransactionCoordinator(telemetry);
        using var trustList = new TrustList(node,
            new CertificateStoreIdentifier(store.StorePath, store.StoreType),
            new CertificateStoreIdentifier("KubernetesSecret:pki-issuer", store.StoreType),
            (_, _) => { }, (_, _) => { }, telemetry, coordinator, 0, 1024 * 1024, manager);
        try
        {
            var desired = new TrustListDataType
            {
                SpecifiedLists = (uint)TrustListMasks.All,
                TrustedCertificates = keepCertificate ? [ByteString.From(certificate.RawData)] : []
            };
            using var stream = new MemoryStream();
            using (var encoder = new BinaryEncoder(stream,
                new ServiceMessageContext(telemetry, context.EncodeableFactory)
                {
                    NamespaceUris = context.NamespaceUris,
                    ServerUris = context.ServerUris
                }, true))
            {
                encoder.WriteEncodeable(null, desired);
            }
            var opened = await node.Open.OnCallAsync(context, node.Open, node.NodeId,
                (byte)(OpenFileMode.Write | OpenFileMode.EraseExisting), CancellationToken.None).ConfigureAwait(false);
            ServiceResult.IsGood(opened.ServiceResult).Should().BeTrue();
            var written = await node.Write.OnCallAsync(context, node.Write, node.NodeId, opened.FileHandle,
                ByteString.From(stream.ToArray()), CancellationToken.None).ConfigureAwait(false);
            ServiceResult.IsGood(written.ServiceResult).Should().BeTrue();
            var staged = await node.CloseAndUpdate.OnCallAsync(context, node.CloseAndUpdate, node.NodeId,
                opened.FileHandle, CancellationToken.None).ConfigureAwait(false);
            ServiceResult.IsGood(staged.ServiceResult).Should().BeTrue();
            staged.ApplyChangesRequired.Should().BeTrue();

            var applied = await coordinator.ApplyChangesAsync(context.SessionId.Value).ConfigureAwait(false);
            ServiceResult.IsGood(applied).Should().BeTrue(applied?.ToString());
            using var remaining = await store.EnumerateAsync().ConfigureAwait(false);
            remaining.Select(value => value.Thumbprint).Should().BeEquivalentTo(
                keepCertificate ? new[] { certificate.Thumbprint } : Array.Empty<string>());
            if (!keepCertificate)
            {
                clientFactory.Client.GetSecret("opcplc-tests", "pki-trusted").Should().BeEmpty();
            }
        }
        finally
        {
            coordinator.Reset();
        }
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
