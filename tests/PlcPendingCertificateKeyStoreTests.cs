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
using System.Threading;
using System.Threading.Tasks;

[TestFixture(false)]
[TestFixture(true)]
public class PlcPendingCertificateKeyStoreTests(bool kubernetes)
{
    private string _root;
    private CertificateManager _manager;
    private PendingCertificateKeyContext _context;

    [SetUp]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "opcplc-pending-" + Guid.NewGuid().ToString("N"));
        var factory = new Mock<IKubernetesSecretStoreClientFactory>();
        factory.Setup(instance => instance.Create())
            .Returns(new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient());
        ICertificateStoreProvider provider = kubernetes
            ? new KubernetesSecretCertificateStoreType(NullLoggerFactory.Instance, factory.Object, "pending-tests")
            : new FlatDirectoryCertificateStoreType(NullLoggerFactory.Instance);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        _manager = new CertificateManager(telemetry, [provider]);
        string path = kubernetes ? "KubernetesSecret:own" : "FlatDirectory:" + Path.Combine(_root, "own");
        _context = new PendingCertificateKeyContext(
            new CertificateStoreIdentifier(path, provider.StoreTypeName, noPrivateKeys: false),
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
        using Certificate key = CreateCertificate();
        using Certificate other = CreateCertificate();
        using (var first = new PlcPendingCertificateKeyStore(_manager))
        {
            (await first.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
            (await first.SaveAsync(_context, key).ConfigureAwait(false)).Should().BeTrue();
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

    private static Certificate CreateCertificate() => DefaultCertificateFactory.Instance
        .CreateApplicationCertificate("urn:opcplc:pending-test", "pending-test", "CN=pending-test", ["localhost"])
        .CreateForRSA();
}
