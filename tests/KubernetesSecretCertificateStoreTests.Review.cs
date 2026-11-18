namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class KubernetesSecretCertificateStoreTests
{
    [Test]
    public async Task Store_EnumerateAsync_MatchesSdkPemReaderBelowItsLimit()
    {
        using var first = CreateSelfSignedCertificate("first");
        using var second = CreateSelfSignedCertificate("second");
        using var unreached = CreateSelfSignedCertificate("unreached");
        using var emptyName = CreateCertificateWithEmptyName();
        using var rsa = RSA.Create(2048);
        // Covers an inline key, invalid base64, an empty name and a stray END label that stops the SDK reader.
        byte[] bundle = Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem() + "\n" + first.ExportCertificatePem() +
            "\n-----BEGIN CERTIFICATE-----\nnot-base64\n-----END CERTIFICATE-----\n" +
            emptyName.ExportCertificatePem() + "\r\n" + second.ExportCertificatePem() +
            "\n-----END CERTIFICATE-----\n" + unreached.ExportCertificatePem());
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]> { ["peers.crt"] = bundle });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");

        using CertificateCollection enumerated = await store.EnumerateAsync().ConfigureAwait(false);

        var sdk = PEMReader.ImportPublicKeysFromPEM(bundle);
        try
        {
            enumerated.Select(certificate => certificate.Thumbprint).Should()
                .Equal(sdk.Select(certificate => certificate.Thumbprint))
                .And.Equal(first.Thumbprint, second.Thumbprint);
        }
        finally
        {
            foreach (var certificate in sdk)
            {
                certificate.Dispose();
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Store_EnumerateAsync_IgnoresBlocksBeyondSdkPemReaderLimit(bool unreadableAfterLimit)
    {
        using var repeated = CreateSelfSignedCertificate("repeated");
        using var hidden = CreateSelfSignedCertificate("after-limit");
        // A base64-valid block that is not a certificate makes the SDK reader reject an entry if it reaches it.
        string afterLimit = unreadableAfterLimit
            ? "-----BEGIN CERTIFICATE-----\nAAAA\n-----END CERTIFICATE-----\n"
            : hidden.ExportCertificatePem() + "\n";
        byte[] bundle = Encoding.UTF8.GetBytes(
            string.Concat(Enumerable.Repeat(repeated.ExportCertificatePem() + "\n", 99)) + afterLimit);
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]> { ["peers.crt"] = bundle });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");

        using (CertificateCollection before = await store.EnumerateAsync().ConfigureAwait(false))
        {
            before.Select(certificate => certificate.Thumbprint).Should().Equal(repeated.Thumbprint);
        }

        // Removing the 99 visible blocks would bring the block after the limit into view, so nothing is changed.
        var applied = await ApplyTrustListAsync(client, store.StorePath, new TrustListDataType
        {
            SpecifiedLists = (uint)TrustListMasks.All,
            TrustedCertificates = []
        }).ConfigureAwait(false);

        ServiceResult.IsBad(applied).Should().BeTrue();
        client.GetSecret("opcplc-tests", "pki-trusted")["peers.crt"].Should().Equal(bundle);
    }

    [Test]
    public async Task Store_DeleteAsync_RefusesRemovalThatWouldExposeHiddenCertificate()
    {
        using var removed = CreateSelfSignedCertificate("removed");
        using var hidden = CreateSelfSignedCertificate("hidden");
        // The SDK reader stops after the first block, so the hidden certificate is never enumerated. Removing the
        // first block would join "-----BEG" and "IN CERTIFICATE-----" into a new, readable block.
        byte[] bundle = Encoding.UTF8.GetBytes("-----BEG" + removed.ExportCertificatePem() + "IN CERTIFICATE-----\n" +
            Convert.ToBase64String(hidden.RawData, Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END CERTIFICATE-----\n");
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]> { ["peers.crt"] = bundle });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeFalse();

        client.GetSecret("opcplc-tests", "pki-trusted")["peers.crt"].Should().Equal(bundle);
        using CertificateCollection enumerated = await store.EnumerateAsync().ConfigureAwait(false);
        enumerated.Select(certificate => certificate.Thumbprint).Should().Equal(removed.Thumbprint);
    }

    [Test]
    public async Task Store_DeleteAsync_RemovesCompanionKeysWithUpperCaseExtensions()
    {
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var other = CreateSelfSignedCertificateWithPrivateKey("other");
        using var removedKey = removed.GetRSAPrivateKey();
        using var otherKey = other.GetRSAPrivateKey();
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            ["client.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem()),
            ["client.PFX"] = removed.Export(X509ContentType.Pkcs12),
            ["client.KEY"] = Encoding.UTF8.GetBytes(removedKey.ExportPkcs8PrivateKeyPem()),
            ["client.PEM"] = Encoding.UTF8.GetBytes(otherKey.ExportPkcs8PrivateKeyPem())
        });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own").Keys.Should().BeEquivalentTo(["client.PEM"],
            "only keys proven to belong to the deleted certificate are removed, whatever their extension case");
    }

    [TestCase("encrypted-canonical-with-foreign-same-basename-certificate")]
    [TestCase("readable-pfx-with-survivor-sharing-its-key")]
    public async Task Store_DeleteAsync_RemovesPfxHoldingDeletedCertificate(string scenario)
    {
        var client = new InMemoryKubernetesSecretStoreClient();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=pfx-owner", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var removed = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var sharedKeySurvivor = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        using var foreign = CreateSelfSignedCertificate("foreign");
        string password = null;
        string survivorThumbprint;
        if (scenario.StartsWith("encrypted", StringComparison.Ordinal))
        {
            password = "store-password";
            using Certificate owned = Certificate.From(new X509Certificate2(removed));
            await store.AddAsync(owned, password.ToCharArray()).ConfigureAwait(false);
            var data = client.GetSecret("opcplc-tests", "pki-own");
            data[removed.Thumbprint + ".crt"] = Encoding.UTF8.GetBytes(foreign.ExportCertificatePem());
            client.SetSecret("opcplc-tests", "pki-own", data);
            survivorThumbprint = foreign.Thumbprint;
        }
        else
        {
            client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
            {
                ["chain.crt"] = Encoding.UTF8.GetBytes(
                    removed.ExportCertificatePem() + "\n" + sharedKeySurvivor.ExportCertificatePem()),
                ["chain.pfx"] = removed.Export(X509ContentType.Pkcs12)
            });
            survivorThumbprint = sharedKeySurvivor.Thumbprint;
        }

        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own").Keys.Should()
            .NotContain(entryKey => entryKey.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase));
        using var deletedIdentity = await store.LoadPrivateKeyAsync(removed.Thumbprint, null, password)
            .ConfigureAwait(false);
        deletedIdentity.Should().BeNull();
        using var survivor = await store.LoadPrivateKeyAsync(survivorThumbprint, null, password).ConfigureAwait(false);
        survivor.Should().BeNull("a PFX holding the deleted certificate must not be offered for a surviving one");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Store_DeleteAsync_JudgesPfxByItsPrivateKeyCertificates(bool deletedCertificateHasTheKey)
    {
        using var issuer = CreateSelfSignedCertificate("issuer");
        using var deleted = CreateSelfSignedCertificateWithPrivateKey("deleted");
        using var unrelated = CreateSelfSignedCertificateWithPrivateKey("unrelated");
        using var publicDeleted = X509CertificateLoader.LoadCertificate(deleted.RawData);
        // Only the certificate carrying the private key identifies the container; chain certificates do not.
        var container = deletedCertificateHasTheKey
            ? new X509Certificate2Collection { deleted, issuer }
            : new X509Certificate2Collection { unrelated, publicDeleted };
        string certificateEntry = deletedCertificateHasTheKey ? deleted.Thumbprint + ".der" : "client.crt";
        string pfxEntry = deletedCertificateHasTheKey ? deleted.Thumbprint + ".pfx" : "client.pfx";
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-own", new Dictionary<string, byte[]>
        {
            [certificateEntry] = deletedCertificateHasTheKey
                ? deleted.RawData
                : Encoding.UTF8.GetBytes(deleted.ExportCertificatePem()),
            [pfxEntry] = container.Export(X509ContentType.Pkcs12)
        });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\own", noPrivateKeys: false);

        (await store.DeleteAsync(deleted.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        client.GetSecret("opcplc-tests", "pki-own").ContainsKey(pfxEntry).Should().Be(!deletedCertificateHasTheKey,
            "a container is removed only when every private key in it belongs to the deleted certificate");
        using var deletedIdentity = await store.LoadPrivateKeyAsync(deleted.Thumbprint, null, null)
            .ConfigureAwait(false);
        deletedIdentity.Should().BeNull();
    }

    [Test]
    public async Task Store_TrustListApply_RollbackAfterDeletionRestoresTrust()
    {
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var kept = CreateSelfSignedCertificate("kept");
        using var rejected = CreateSelfSignedCertificate("rejected-by-storage");
        using var key = removed.GetRSAPrivateKey();
        var storage = new InMemoryKubernetesSecretStoreClient();
        storage.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>
        {
            ["tls.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem()),
            ["tls.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem()),
            ["peers.crt"] = Encoding.UTF8.GetBytes("# peers\r\n" + removed.ExportCertificatePem() + "\n" +
                kept.ExportCertificatePem() + "\n")
        });
        // Adding the new certificate fails after the SDK already deleted the removed one, forcing a rollback.
        var client = new FailingWriteClient(storage, data => data.ContainsKey(rejected.Thumbprint + ".der"));

        var applied = await ApplyTrustListAsync(client,
            KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted",
            new TrustListDataType
            {
                SpecifiedLists = (uint)TrustListMasks.TrustedCertificates,
                TrustedCertificates = [ByteString.From(kept.RawData), ByteString.From(rejected.RawData)]
            }).ConfigureAwait(false);

        ServiceResult.IsBad(applied).Should().BeTrue();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, storage, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        using CertificateCollection trusted = await store.EnumerateAsync().ConfigureAwait(false);
        trusted.Select(certificate => certificate.Thumbprint).Should()
            .BeEquivalentTo([removed.Thumbprint, kept.Thumbprint], "the SDK rollback re-adds the deleted certificate");
        storage.GetSecret("opcplc-tests", "pki-trusted").Should().ContainKey(removed.Thumbprint + ".der")
            .And.NotContainKey(rejected.Thumbprint + ".der");
    }
    private static async Task<ServiceResult> ApplyTrustListAsync(
        IKubernetesSecretStoreClient client, string storePath, TrustListDataType desired)
    {
        var factory = new Mock<IKubernetesSecretStoreClientFactory>();
        factory.Setup(instance => instance.Create()).Returns(client);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry,
            [new KubernetesSecretCertificateStoreType(NullLoggerFactory.Instance, factory.Object, "opcplc-tests")]);
        var context = new SessionSystemContext(telemetry)
        {
            SessionId = new NodeId(Guid.NewGuid(), 1),
            NamespaceUris = new NamespaceTable(),
            ServerUris = new StringTable(),
            EncodeableFactory = EncodeableFactory.Create()
        };
        var node = new TrustListState(null) { NodeId = new NodeId("ReviewTrustList", 1) };
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
            new CertificateStoreIdentifier(storePath, KubernetesSecretCertificateStore.StoreTypeName),
            new CertificateStoreIdentifier(KubernetesSecretCertificateStore.StoreTypePrefix + "pki-issuer",
                KubernetesSecretCertificateStore.StoreTypeName),
            (_, _) => { }, (_, _) => { }, telemetry, coordinator, 0, 1024 * 1024, manager);
        try
        {
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
            return await coordinator.ApplyChangesAsync(context.SessionId.Value).ConfigureAwait(false);
        }
        finally
        {
            coordinator.Reset();
        }
    }

    private static X509Certificate2 CreateCertificateWithEmptyName()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(new X500DistinguishedName(string.Empty), rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class FailingWriteClient(
        IKubernetesSecretStoreClient inner, Func<IReadOnlyDictionary<string, byte[]>, bool> fail)
        : IKubernetesSecretStoreClient
    {
        public Task<IReadOnlyDictionary<string, byte[]>> ReadAsync(
            string namespaceName, string secretName, CancellationToken ct = default)
            => inner.ReadAsync(namespaceName, secretName, ct);

        public Task WriteAsync(string namespaceName, string secretName, IReadOnlyDictionary<string, byte[]> data,
            CancellationToken ct = default)
            => fail(data)
                ? Task.FromException(new IOException("Injected Kubernetes write failure."))
                : inner.WriteAsync(namespaceName, secretName, data, ct);
    }
}
