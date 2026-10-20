namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Certs;
using OpcPlc.Configuration;
using OpcPlc.Tests.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestFixture("Directory")]
[TestFixture("FlatDirectory")]
[TestFixture("KubernetesSecret")]
public class PlcPendingCertificateKeyStoreTests(string storeType)
{
    private readonly bool _kubernetes = storeType == KubernetesSecretCertificateStore.StoreTypeName;
    private string _root;
    private CertificateManager _manager;
    private PendingCertificateKeyContext _context;
    private KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient _secretClient;

    [SetUp]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "opcplc-pending-" + Guid.NewGuid().ToString("N"));
        var factory = new Mock<IKubernetesSecretStoreClientFactory>();
        _secretClient = new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient();
        factory.Setup(instance => instance.Create())
            .Returns(_secretClient);
        ICertificateStoreProvider provider = storeType switch
        {
            KubernetesSecretCertificateStore.StoreTypeName =>
                new KubernetesSecretCertificateStoreType(NullLoggerFactory.Instance, factory.Object, "pending-tests"),
            FlatDirectoryCertificateStore.StoreTypeName =>
                new FlatDirectoryCertificateStoreType(NullLoggerFactory.Instance),
            _ => null
        };
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        _manager = new CertificateManager(telemetry, provider is null ? [] : [provider]);
        string path = storeType switch
        {
            KubernetesSecretCertificateStore.StoreTypeName => "KubernetesSecret:own",
            FlatDirectoryCertificateStore.StoreTypeName => "FlatDirectory:" + Path.Combine(_root, "own"),
            _ => Path.Combine(_root, "own")
        };
        _context = new PendingCertificateKeyContext(
            new CertificateStoreIdentifier(path, storeType, noPrivateKeys: false),
            ObjectIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup,
            ObjectTypeIds.RsaSha256ApplicationCertificateType, null, telemetry);
    }

    [TearDown]
    public void TearDown()
    {
        _manager.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public async Task PendingKey_ReopensAndOnlyOneMatchingClaimConsumesItAsync()
    {
        if (!_kubernetes)
        {
            string path = Path.Combine("%LocalApplicationData%", Path.GetRelativePath(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine(_root, "own")));
            if (storeType == FlatDirectoryCertificateStore.StoreTypeName)
            {
                path = FlatDirectoryCertificateStore.StoreTypePrefix + path;
            }
            _context = _context with { BaseStore = new CertificateStoreIdentifier(path, storeType, noPrivateKeys: false) };
        }
        using Certificate key = CreateCertificate();
        using Certificate other = CreateCertificate();
        using (var first = new PlcPendingCertificateKeyStore(_manager))
        {
            (await first.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
            (await first.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        }
        if (_kubernetes)
        {
            _secretClient.GetSecret("pending-tests", "own").Keys
                .Should().Contain(keyName => keyName.StartsWith("opcplc-pending-", StringComparison.Ordinal));
        }
        using ICertificateStore own = _manager.OpenCertificateStore(
            _context.BaseStore.StorePath, _context.BaseStore.StoreType, noPrivateKeys: false);
        using CertificateCollection active = await own.EnumerateAsync().ConfigureAwait(false);
        active.Should().BeEmpty("a pending key must never become the active identity during startup");

        using var reopened = new PlcPendingCertificateKeyStore(_manager);
        using Certificate mismatch = await reopened.TryTakeMatchingAsync(_context, other).ConfigureAwait(false);
        mismatch.Should().BeNull();
        using Certificate wrongScope = await reopened.TryTakeAsync(
            _context with { CertificateTypeId = ObjectTypeIds.RsaMinApplicationCertificateType }).ConfigureAwait(false);
        wrongScope.Should().BeNull();
        using Certificate peek = await reopened.TryPeekMatchingAsync(_context, key).ConfigureAwait(false);
        peek.Should().NotBeNull();
        using RSA privateKey = peek.GetRSAPrivateKey();
        using RSA publicKey = key.GetRSAPublicKey();
        byte[] data = [1, 2, 3];
        publicKey.VerifyData(data, privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();

        Certificate[] claims = await Task.WhenAll(
            reopened.TryTakeMatchingAsync(_context, key).AsTask(),
            reopened.TryTakeMatchingAsync(_context, key).AsTask()).ConfigureAwait(false);
        try
        {
            claims.Count(certificate => certificate is not null).Should().Be(1);
        }
        finally
        {
            foreach (Certificate certificate in claims)
            {
                certificate?.Dispose();
            }
        }
    }

    [Test]
    public async Task PendingKey_RestoreDoesNotOverwriteNewerRequestAsync()
    {
        using var store = new PlcPendingCertificateKeyStore(_manager);
        using Certificate original = CreateCertificate();
        using Certificate newer = CreateCertificate();
        (await store.SaveAsync(_context, original).ConfigureAwait(false)).Should().BeTrue();
        using Certificate taken = await store.TryTakeAsync(_context).ConfigureAwait(false);
        (await store.SaveAsync(_context, newer).ConfigureAwait(false)).Should().BeTrue();
        (await store.TryRestoreAsync(_context, taken).ConfigureAwait(false)).Should().BeFalse();
        using Certificate pending = await store.TryPeekMatchingAsync(_context, newer).ConfigureAwait(false);
        pending.Thumbprint.Should().Be(newer.Thumbprint);
        await store.RemoveAsync(_context).ConfigureAwait(false);
        (await store.TryRestoreAsync(_context, taken).ConfigureAwait(false)).Should().BeTrue();
        using Certificate restored = await store.TryTakeAsync(_context).ConfigureAwait(false);
        restored.Thumbprint.Should().Be(original.Thumbprint);
    }

    [Test]
    public async Task PendingKey_InvalidOrCancelledSavePreservesExistingKeyAsync()
    {
        using var store = new PlcPendingCertificateKeyStore(_manager);
        using Certificate original = CreateCertificate();
        using var publicOnly = Certificate.FromRawData(original.RawData);
        (await store.SaveAsync(_context, original).ConfigureAwait(false)).Should().BeTrue();
        Func<Task> invalid = () => store.SaveAsync(_context, publicOnly).AsTask();
        await invalid.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
        Func<Task> cancelled = () => store.SaveAsync(_context, original, new CancellationToken(true)).AsTask();
        await cancelled.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        using Certificate pending = await store.TryTakeAsync(_context).ConfigureAwait(false);
        pending.Thumbprint.Should().Be(original.Thumbprint);
    }

    [TestCase(24)]
    [TestCase(25)]
    public async Task PendingKey_ExpiresAfterTwentyFourHoursAndIsRemovedAsync(int hours)
    {
        using Certificate key = CreateCertificate();
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
        using (var originalStore = new PlcPendingCertificateKeyStore(_manager, timeProvider))
        {
            (await originalStore.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        }
        timeProvider.Advance(TimeSpan.FromHours(hours));
        using var store = new PlcPendingCertificateKeyStore(_manager, timeProvider);

        using Certificate result = await store.TryTakeAsync(_context).ConfigureAwait(false);
        result.Should().BeNull();

        if (_kubernetes)
        {
            _secretClient.GetSecret("pending-tests", "own").Keys
                .Should().NotContain(keyName => keyName.StartsWith("opcplc-pending-", StringComparison.Ordinal));
        }
        else
        {
            using ICertificateStore pendingStore = _manager.OpenCertificateStore(
                GetPendingStorePath(),
                _context.BaseStore.StoreType, noPrivateKeys: false);
            using CertificateCollection remaining = await pendingStore.EnumerateAsync().ConfigureAwait(false);
            remaining.Should().BeEmpty();
            File.Exists(GetPendingStorePath().Replace("FlatDirectory:", string.Empty,
                StringComparison.Ordinal) + ".created").Should().BeFalse();
        }
    }

    [Test]
    public async Task PendingKey_RepeatedSaveAndRestorePreserveOriginalExpiryAsync()
    {
        using Certificate key = CreateCertificate();
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
        using var store = new PlcPendingCertificateKeyStore(_manager, timeProvider);
        (await store.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        timeProvider.Advance(TimeSpan.FromHours(23));
        (await store.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        using Certificate taken = await store.TryTakeMatchingAsync(_context, key).ConfigureAwait(false);
        taken.Should().NotBeNull();
        timeProvider.Advance(TimeSpan.FromMinutes(30));
        (await store.TryRestoreAsync(_context, taken).ConfigureAwait(false)).Should().BeTrue();

        using var reopened = new PlcPendingCertificateKeyStore(_manager, timeProvider);
        using Certificate peek = await reopened.TryPeekMatchingAsync(_context, key).ConfigureAwait(false);
        peek.Should().NotBeNull();
        timeProvider.Advance(TimeSpan.FromMinutes(30));
        using Certificate expired = await reopened.TryTakeMatchingAsync(_context, key).ConfigureAwait(false);
        expired.Should().BeNull();
    }

    [Test]
    public async Task PendingKey_RestoreDoesNotReviveExpiredClaimAsync()
    {
        using Certificate key = CreateCertificate();
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
        using var store = new PlcPendingCertificateKeyStore(_manager, timeProvider);
        (await store.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        using Certificate taken = await store.TryTakeAsync(_context).ConfigureAwait(false);
        timeProvider.Advance(TimeSpan.FromHours(24));
        (await store.TryRestoreAsync(_context, taken).ConfigureAwait(false)).Should().BeFalse();
        using Certificate pending = await store.TryTakeAsync(_context).ConfigureAwait(false);
        pending.Should().BeNull();
    }

    [Test]
    public async Task PendingKey_ReplacementGetsNewLifetimeWithoutChangingActiveCertificateAsync()
    {
        using Certificate activeKey = CreateCertificate();
        using Certificate original = CreateCertificate();
        using Certificate newer = CreateCertificate();
        using ICertificateStore own = _manager.OpenCertificateStore(
            _context.BaseStore.StorePath, storeType, noPrivateKeys: false);
        await own.AddAsync(activeKey).ConfigureAwait(false);
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UtcNow);
        using var store = new PlcPendingCertificateKeyStore(_manager, timeProvider);
        (await store.SaveAsync(_context, original).ConfigureAwait(false)).Should().BeTrue();
        timeProvider.Advance(TimeSpan.FromHours(23));
        (await store.SaveAsync(_context, newer).ConfigureAwait(false)).Should().BeTrue();
        using Certificate mismatch = await store.TryTakeMatchingAsync(_context, original).ConfigureAwait(false);
        mismatch.Should().BeNull();
        timeProvider.Advance(TimeSpan.FromHours(1));
        using Certificate current = await store.TryPeekMatchingAsync(_context, newer).ConfigureAwait(false);
        current.Should().NotBeNull();
        timeProvider.Advance(TimeSpan.FromHours(23));
        using Certificate expired = await store.TryTakeAsync(_context).ConfigureAwait(false);
        expired.Should().BeNull();

        using CertificateCollection active = await own.EnumerateAsync().ConfigureAwait(false);
        active.Should().ContainSingle().Which.Thumbprint.Should().Be(activeKey.Thumbprint);
        using Certificate persisted = await own.LoadPrivateKeyAsync(activeKey.Thumbprint,
            activeKey.Subject, null, NodeId.Null, null).ConfigureAwait(false);
        persisted.HasPrivateKey.Should().BeTrue();
        X509Utils.VerifyKeyPair(activeKey, persisted).Should().BeTrue();
        if (_kubernetes)
        {
            _secretClient.GetSecret("pending-tests", "own").Keys.Should().HaveCount(2);
        }
    }

    [Test]
    public async Task PendingKey_SdkDirectoryLayoutRemainsReadableAsync()
    {
        if (storeType != CertificateStoreType.Directory)
        {
            Assert.Ignore("Only Directory uses the SDK pending-key layout.");
        }
        using Certificate key = CreateCertificate();
        var sdkStore = new DirectoryPendingCertificateKeyStore();
        (await sdkStore.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
        using var store = new PlcPendingCertificateKeyStore(_manager);
        using Certificate pending = await store.TryTakeMatchingAsync(_context, key).ConfigureAwait(false);
        pending.Should().NotBeNull();
        pending.Thumbprint.Should().Be(key.Thumbprint);
    }

    private string GetPendingStorePath()
    {
        string scope = Utils.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            _context.CertificateGroupId + "|" + _context.CertificateTypeId)))[..16];
        return storeType == CertificateStoreType.Directory
            ? Path.Combine(_context.BaseStore.StorePath, "pending", scope)
            : _context.BaseStore.StorePath + ".pending-" + scope.ToLowerInvariant();
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow += amount;
    }

    private static Certificate CreateCertificate() => DefaultCertificateFactory.Instance
        .CreateApplicationCertificate("urn:opcplc:pending-test", "pending-test", "CN=pending-test", ["localhost"])
        .CreateForRSA();
}
