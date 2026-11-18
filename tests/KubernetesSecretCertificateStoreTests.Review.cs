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

    [Test]
    public async Task Store_TrustListApply_RemovesCertificatesBeyondSdkPemReaderLimit()
    {
        using var repeated = CreateSelfSignedCertificate("repeated");
        using var hidden = CreateSelfSignedCertificate("after-limit");
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>
        {
            ["peers.crt"] = Encoding.UTF8.GetBytes(
                string.Concat(Enumerable.Repeat(repeated.ExportCertificatePem() + "\n", 99)) +
                hidden.ExportCertificatePem() + "\n")
        });
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        using (CertificateCollection before = await store.EnumerateAsync().ConfigureAwait(false))
        {
            before.Select(certificate => certificate.Thumbprint).Should()
                .BeEquivalentTo([repeated.Thumbprint, hidden.Thumbprint]);
        }

        var applied = await ApplyTrustListAsync(client, store.StorePath, new TrustListDataType
        {
            SpecifiedLists = (uint)TrustListMasks.All,
            TrustedCertificates = []
        }).ConfigureAwait(false);

        ServiceResult.IsGood(applied).Should().BeTrue(applied?.ToString());
        using CertificateCollection after = await store.EnumerateAsync().ConfigureAwait(false);
        after.Should().BeEmpty("an empty TrustList must not leave a previously unenumerated certificate trusted");
        client.GetSecret("opcplc-tests", "pki-trusted").Should().BeEmpty();
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

    [Test]
    public async Task Store_TrustListApply_RollbackRestoresDeletedSecretDataExactly()
    {
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var kept = CreateSelfSignedCertificate("kept");
        using var rejected = CreateSelfSignedCertificate("rejected-by-storage");
        using var key = removed.GetRSAPrivateKey();
        var original = new Dictionary<string, byte[]>
        {
            ["tls.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem()),
            ["tls.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem()),
            ["peers.crt"] = Encoding.UTF8.GetBytes("# peers\r\n" + removed.ExportCertificatePem() + "\n" +
                kept.ExportCertificatePem() + "\n")
        };
        var storage = new InMemoryKubernetesSecretStoreClient();
        storage.SetSecret("opcplc-tests", "pki-trusted", original);
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
        storage.GetSecret("opcplc-tests", "pki-trusted").Should().BeEquivalentTo(original,
            "a failed ApplyChanges must leave the Secret exactly as it was, including the deleted private key");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Store_AddAsync_RestoresOnlyTheReaddedCertificate(bool readdInDeletionOrder)
    {
        using var first = CreateSelfSignedCertificateWithPrivateKey("first");
        using var second = CreateSelfSignedCertificate("second");
        using var third = CreateSelfSignedCertificate("third");
        using var key = first.GetRSAPrivateKey();
        string firstPem = first.ExportCertificatePem();
        string secondPem = second.ExportCertificatePem();
        string bundle = "# head\n" + firstPem + "\n" + secondPem + "\n" + third.ExportCertificatePem() + "\n# tail\n";
        var original = new Dictionary<string, byte[]>
        {
            ["peers.crt"] = Encoding.UTF8.GetBytes(bundle),
            ["peers.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())
        };
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", original);
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        (await store.DeleteAsync(first.Thumbprint).ConfigureAwait(false)).Should().BeTrue();
        (await store.DeleteAsync(second.Thumbprint).ConfigureAwait(false)).Should().BeTrue();
        client.GetSecret("opcplc-tests", "pki-trusted").Keys.Should().BeEquivalentTo(["peers.crt"]);

        var (readdedFirst, readdedLater) = readdInDeletionOrder ? (first, second) : (second, first);
        using (Certificate publicCertificate = Certificate.FromRawData(readdedFirst.RawData))
        {
            await store.AddAsync(publicCertificate).ConfigureAwait(false);
        }

        var partial = client.GetSecret("opcplc-tests", "pki-trusted");
        partial["peers.crt"].Should().Equal(Encoding.UTF8.GetBytes(
            bundle.Replace(readdInDeletionOrder ? secondPem : firstPem, string.Empty, StringComparison.Ordinal)),
            "only the re-added certificate is restored");
        partial.ContainsKey("peers.key").Should().Be(readdInDeletionOrder);
        partial.Keys.Should().NotContain(entryKey => entryKey.EndsWith(".der", StringComparison.Ordinal));

        using (Certificate publicCertificate = Certificate.FromRawData(readdedLater.RawData))
        {
            await store.AddAsync(publicCertificate).ConfigureAwait(false);
        }

        client.GetSecret("opcplc-tests", "pki-trusted").Should().BeEquivalentTo(original);
    }

    [TestCase("expired")]
    [TestCase("changed-externally")]
    public async Task Store_AddAsync_AddsCanonicalEntryWhenRestoreIsUnsafe(string scenario)
    {
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var kept = CreateSelfSignedCertificate("kept");
        using var added = CreateSelfSignedCertificate("added-externally");
        using var key = removed.GetRSAPrivateKey();
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>
        {
            ["peers.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem() + "\n" + kept.ExportCertificatePem()),
            ["peers.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())
        });
        var time = new ManualTimeProvider();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests")
        {
            TimeProvider = time
        };
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();
        var afterDelete = client.GetSecret("opcplc-tests", "pki-trusted");
        if (scenario == "expired")
        {
            time.UtcNow += TimeSpan.FromMinutes(6);
        }
        else
        {
            afterDelete["peers.crt"] =
                [.. afterDelete["peers.crt"], .. Encoding.UTF8.GetBytes(added.ExportCertificatePem())];
            client.SetSecret("opcplc-tests", "pki-trusted", afterDelete);
        }
        var expected = afterDelete.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        expected[removed.Thumbprint + ".der"] = removed.RawData;

        using (Certificate publicCertificate = Certificate.FromRawData(removed.RawData))
        {
            await store.AddAsync(publicCertificate).ConfigureAwait(false);
        }

        client.GetSecret("opcplc-tests", "pki-trusted").Should().BeEquivalentTo(expected,
            "an expired or outdated record must not overwrite the entry or bring back the deleted key");
    }

    [Test]
    public async Task Store_DeleteAsync_ClearsDeletedEntriesWhenTheyExpireWithoutFurtherStoreCalls()
    {
        using var removed = CreateSelfSignedCertificateWithPrivateKey("removed");
        using var kept = CreateSelfSignedCertificate("kept");
        using var key = removed.GetRSAPrivateKey();
        var client = new InMemoryKubernetesSecretStoreClient();
        client.SetSecret("opcplc-tests", "pki-trusted", new Dictionary<string, byte[]>
        {
            ["peers.crt"] = Encoding.UTF8.GetBytes(removed.ExportCertificatePem() + "\n" + kept.ExportCertificatePem()),
            ["peers.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())
        });
        var time = new ManualTimeProvider();
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests")
        {
            TimeProvider = time
        };
        store.Open(KubernetesSecretCertificateStore.StoreTypePrefix + @"pki\trusted");
        (await store.DeleteAsync(removed.Thumbprint).ConfigureAwait(false)).Should().BeTrue();
        store.DeletionJournalCount.Should().Be(2, "the deleted certificate entry and its key are kept for rollback");
        var sweep = time.Timers.Should().ContainSingle().Subject;
        sweep.DueTime.Should().Be(TimeSpan.FromMinutes(5));

        time.UtcNow += TimeSpan.FromMinutes(5);
        sweep.Callback(sweep.State);

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (store.DeletionJournalCount != 0)
        {
            await Task.Delay(10, deadline.Token).ConfigureAwait(false);
        }
        time.Timers.Should().ContainSingle("nothing is left to sweep");
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

    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public List<(TimerCallback Callback, object State, TimeSpan DueTime)> Timers { get; } = [];

        public override DateTimeOffset GetUtcNow() => UtcNow;

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            Timers.Add((callback, state, dueTime));
            return new ManualTimer();
        }

        private sealed class ManualTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
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
