namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using OpcPlc.Certs;
using OpcPlc.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class CertificateStoreMigrationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task TrustStoreProvider_ConfigurationBuilderUsesInjectedProviderAsync(bool customProvider)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-trust-provider-" + Guid.NewGuid().ToString("N"));
        try
        {
            ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
            var provider = new Mock<ICertificateStoreProvider>();
            provider.SetupGet(instance => instance.StoreTypeName).Returns("InjectedDirectory");
            provider.Setup(instance => instance.SupportsStorePath(It.IsAny<string>())).Returns(true);
            provider.Setup(instance => instance.CreateStore(It.IsAny<ITelemetryContext>()))
                .Returns((ITelemetryContext context) => new DirectoryCertificateStore(noSubDirs: false, context));
            var application = new ApplicationInstance(telemetry)
            {
                ApplicationName = "TrustProviderControl",
                ApplicationType = ApplicationType.Server
            };
            await using (application.ConfigureAwait(false))
            {
                var builder = application.Build("urn:localhost:TrustProviderControl", "urn:opcplc:control")
                    .AsServer(["opc.tcp://localhost:4840/control"])
                    .AddSignAndEncryptPolicies()
                    .AddSecurityConfiguration([new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(root, "own"),
                        SubjectName = "CN=TrustProviderControl",
                        CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                    }], root);
                SecurityConfiguration security = application.ApplicationConfiguration.SecurityConfiguration;
                string storeType = customProvider ? "InjectedDirectory" : CertificateStoreType.Directory;
                security.TrustedIssuerCertificates.StoreType = storeType;
                security.TrustedPeerCertificates.StoreType = storeType;
                using CertificateManager manager = CertificateManagerFactory.Create(security, telemetry, options =>
                {
                    if (customProvider)
                    {
                        options.AddStoreProvider(provider.Object);
                    }
                });
                application.ApplicationConfiguration.CertificateManager = manager;
                using (ICertificateStore store = manager.OpenIssuerStore(TrustListIdentifier.Peers))
                {
                    using CertificateCollection certificates = await store.EnumerateAsync().ConfigureAwait(false);
                    certificates.Should().BeEmpty();
                }

                ApplicationConfiguration configuration = await builder.CreateAsync().ConfigureAwait(false);

                configuration.SecurityConfiguration.TrustedIssuerCertificates.StoreType.Should().Be(storeType);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationImport_ServicePersistsThroughCustomProviderAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry, [fixture.Provider]);
        var plc = new OpcPlcConfiguration();
        plc.OpcUa.ApplicationConfiguration = new ApplicationConfiguration
        {
            CertificateManager = manager,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificates =
                [
                    new CertificateIdentifier
                    {
                        StoreType = fixture.Provider.StoreTypeName,
                        StorePath = fixture.Path,
                        SubjectName = "CN=service-import",
                        CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                    }
                ]
            }
        };
        var service = new CertificateManagementService(plc, NullLogger.Instance, telemetry, [fixture.Provider]);
        using Certificate original = CreateCertificate("service-import");
        using Certificate replacement = CreateCertificate("service-import");
        foreach (Certificate certificate in new[] { original, replacement })
        {
            using RSA key = certificate.GetRSAPrivateKey();
            (await service.UpdateApplicationCertificateAsync(Convert.ToBase64String(certificate.RawData), null, null,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())), null)
                .ConfigureAwait(false)).Should().BeTrue();
        }
        using CertificateEntry active = manager.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType);
        active.Certificate.Thumbprint.Should().Be(replacement.Thumbprint);
        VerifySignature(active.Certificate);
        using CertificateCollection stored = await ApplicationCertificateLifecycle.EnumerateApplicationCertificatesAsync(
            plc.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificates[0],
            [fixture.Provider], telemetry).ConfigureAwait(false);
        stored.Should().ContainSingle().Which.Thumbprint.Should().Be(replacement.Thumbprint);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationActivation_UsesCustomProviderForPersistenceAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using var manager = new CertificateManager(telemetry, [fixture.Provider]);
        var config = new ApplicationConfiguration
        {
            CertificateManager = manager,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificates =
                [
                    new CertificateIdentifier
                    {
                        StoreType = fixture.Provider.StoreTypeName,
                        StorePath = fixture.Path,
                        SubjectName = "CN=custom-rotation",
                        CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                    }
                ]
            }
        };
        using Certificate first = CreateCertificate("custom-rotation");
        using Certificate second = CreateCertificate("custom-rotation");

        await ApplicationCertificateLifecycle.PersistAndActivateAsync(config, first, [fixture.Provider], telemetry)
            .ConfigureAwait(false);
        using CertificateEntry old = manager.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType);
        await ApplicationCertificateLifecycle.PersistAndActivateAsync(config, second, [fixture.Provider], telemetry)
            .ConfigureAwait(false);

        using CertificateEntry current = manager.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType);
        current.Certificate.Thumbprint.Should().Be(second.Thumbprint);
        VerifySignature(current.Certificate);
        VerifySignature(old.Certificate);
        using CertificateCollection stored = await ApplicationCertificateLifecycle.EnumerateApplicationCertificatesAsync(
            config.SecurityConfiguration.ApplicationCertificates[0], [fixture.Provider], telemetry).ConfigureAwait(false);
        stored.Should().ContainSingle().Which.Thumbprint.Should().Be(second.Thumbprint);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TrustMutation_UsesInjectedProviderForAddAndRemoveAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        var security = new SecurityConfiguration
        {
            TrustedPeerCertificates = new CertificateTrustList
            {
                StoreType = fixture.Provider.StoreTypeName,
                StorePath = fixture.Path
            },
            TrustedIssuerCertificates = new CertificateTrustList
            {
                StoreType = fixture.Provider.StoreTypeName,
                StorePath = fixture.Path
            }
        };
        using CertificateManager manager = CertificateManagerFactory.Create(security,
            DefaultTelemetry.Create(_ => { }), options => options.AddStoreProvider(fixture.Provider));
        var operations = new CertificateTrustListOperations(manager, NullLogger.Instance);
        using Certificate certificate = CreateCertificate("trust-write-provider");
        string base64 = Convert.ToBase64String(certificate.RawData);

        (await operations.AddAsync([base64], null, TrustListIdentifier.Peers, false).ConfigureAwait(false))
            .Should().BeTrue();
        (await operations.AddAsync([base64], null, TrustListIdentifier.Peers, false).ConfigureAwait(false))
            .Should().BeTrue();
        using (ICertificateStore store = manager.OpenTrustedStore(TrustListIdentifier.Peers))
        using (CertificateCollection certificates = await store.EnumerateAsync().ConfigureAwait(false))
        {
            certificates.Should().ContainSingle().Which.Thumbprint.Should().Be(certificate.Thumbprint);
            certificates[0].HasPrivateKey.Should().BeFalse();
        }

        (await operations.RemovePeerCertificatesAsync([certificate.Thumbprint]).ConfigureAwait(false)).Should().BeTrue();
        using ICertificateStore remainingStore = manager.OpenTrustedStore(TrustListIdentifier.Peers);
        using CertificateCollection remaining = await remainingStore.EnumerateAsync().ConfigureAwait(false);
        remaining.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationDiagnostics_EnumeratesInjectedStoreWithOwnedPublicResultsAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        using Certificate first = CreateCertificate("diagnostic-first");
        using Certificate second = CreateCertificate("diagnostic-second");
        await fixture.Store.AddAsync(first, ct: CancellationToken.None).ConfigureAwait(false);
        await fixture.Store.AddAsync(second, ct: CancellationToken.None).ConfigureAwait(false);
        var identifier = new CertificateIdentifier
        {
            StoreType = fixture.Provider.StoreTypeName,
            StorePath = fixture.Path,
            CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
        };

        using CertificateCollection certificates = await ApplicationCertificateLifecycle
            .EnumerateApplicationCertificatesAsync(identifier, [fixture.Provider], DefaultTelemetry.Create(_ => { }))
            .ConfigureAwait(false);
        fixture.Store.Dispose();

        certificates.Select(certificate => certificate.Thumbprint).Should()
            .BeEquivalentTo(first.Thumbprint, second.Thumbprint);
        certificates.Should().OnlyContain(certificate => !certificate.HasPrivateKey);
        using RSA publicKey = certificates[0].GetRSAPublicKey();
        publicKey.KeySize.Should().Be(2048);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Store_PersistsCertificateAndPrivateKeyAcrossReopenAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        string thumbprint;
        byte[] publicData;
        using (Certificate certificate = CreateCertificate("roundtrip"))
        {
            thumbprint = certificate.Thumbprint;
            publicData = certificate.RawData;
            await fixture.Store.AddAsync(certificate, "test-password".ToCharArray(), CancellationToken.None)
                .ConfigureAwait(false);
        }
        fixture.Store.Close();
        fixture.Open(noPrivateKeys: false);
        using CertificateCollection certificates = await fixture.Store.EnumerateAsync().ConfigureAwait(false);
        certificates.Should().ContainSingle().Which.RawData.Should().Equal(publicData);
        using CertificateCollection found = await fixture.Store.FindByThumbprintAsync(thumbprint).ConfigureAwait(false);
        found.Should().ContainSingle().Which.Thumbprint.Should().Be(thumbprint);
        using Certificate privateCertificate = await fixture.Store.LoadPrivateKeyAsync(
            thumbprint, null, null, ObjectTypeIds.RsaSha256ApplicationCertificateType,
            "test-password".ToCharArray(), CancellationToken.None).ConfigureAwait(false);
        privateCertificate.Should().NotBeNull();
        VerifySignature(privateCertificate);
        (await fixture.Store.DeleteAsync(thumbprint).ConfigureAwait(false)).Should().BeTrue();
        using CertificateCollection remaining = await fixture.Store.EnumerateAsync().ConfigureAwait(false);
        remaining.Should().BeEmpty();
        found[0].RawData.Should().Equal(publicData);
        VerifySignature(privateCertificate);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PemBundle_ReturnsIndependentCertificateHandlesAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        using Certificate first = CreateCertificate("bundle-first");
        using Certificate second = CreateCertificate("bundle-second");
        await fixture.SeedPemAsync(first, second).ConfigureAwait(false);

        using CertificateCollection all = await fixture.Store.EnumerateAsync().ConfigureAwait(false);
        all.Select(certificate => certificate.Thumbprint).Should().BeEquivalentTo(first.Thumbprint, second.Thumbprint);
        using CertificateCollection selected = await fixture.Store.FindByThumbprintAsync(
            second.Thumbprint.ToLowerInvariant()).ConfigureAwait(false);
        selected.Should().ContainSingle().Which.RawData.Should().Equal(second.RawData);
        all.Dispose();
        fixture.Store.Dispose();

        selected[0].RawData.Should().Equal(second.RawData);
        using RSA publicKey = selected[0].GetRSAPublicKey();
        publicKey.KeySize.Should().Be(2048);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PemPrivateKey_ReturnedHandleRemainsUsableAfterStoreClosesAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        using Certificate certificate = CreateCertificate("pem-key");
        await fixture.SeedPemAsync(certificate).ConfigureAwait(false);
        using RSA privateKey = certificate.GetRSAPrivateKey();
        await fixture.SeedAsync("tls.key", Encoding.UTF8.GetBytes(privateKey.ExportPkcs8PrivateKeyPem()))
            .ConfigureAwait(false);

        using Certificate loaded = await fixture.Store.LoadPrivateKeyAsync(
            certificate.Thumbprint, certificate.Subject, null, ObjectTypeIds.RsaSha256ApplicationCertificateType,
            password: null, ct: CancellationToken.None).ConfigureAwait(false);
        loaded.Should().NotBeNull();
        fixture.Store.Close();
        fixture.Store.Dispose();

        loaded.Thumbprint.Should().Be(certificate.Thumbprint);
        VerifySignature(loaded);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task NoPrivateKeys_DoesNotReturnPemKeyMaterialAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        using Certificate certificate = CreateCertificate("public-only");
        await fixture.SeedPemAsync(certificate).ConfigureAwait(false);
        using RSA privateKey = certificate.GetRSAPrivateKey();
        await fixture.SeedAsync("tls.key", Encoding.UTF8.GetBytes(privateKey.ExportPkcs8PrivateKeyPem()))
            .ConfigureAwait(false);
        fixture.Store.Close();
        fixture.Open(noPrivateKeys: true);

        using Certificate loaded = await fixture.Store.LoadPrivateKeyAsync(
            certificate.Thumbprint, certificate.Subject, null, ObjectTypeIds.RsaSha256ApplicationCertificateType,
            password: null, ct: CancellationToken.None).ConfigureAwait(false);

        loaded.Should().BeNull();
        using CertificateCollection publicCertificates = await fixture.Store.EnumerateAsync().ConfigureAwait(false);
        publicCertificates.Should().ContainSingle().Which.HasPrivateKey.Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PublicOnlyAdd_DoesNotPersistPrivateKeysAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        fixture.Store.Close();
        fixture.Open(noPrivateKeys: true);
        using Certificate certificate = CreateCertificate("trust-only");
        await fixture.Store.AddAsync(certificate, ct: CancellationToken.None).ConfigureAwait(false);
        fixture.Store.Close();
        fixture.Open(noPrivateKeys: false);

        using Certificate loaded = await fixture.Store.LoadPrivateKeyAsync(
            certificate.Thumbprint, certificate.Subject, null, ObjectTypeIds.RsaSha256ApplicationCertificateType,
            password: null, ct: CancellationToken.None).ConfigureAwait(false);

        loaded.Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CertificateManager_UsesInjectedProviderAndOwnedResultsAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        var security = new SecurityConfiguration
        {
            TrustedPeerCertificates = new CertificateTrustList
            {
                StoreType = fixture.Provider.StoreTypeName,
                StorePath = fixture.Path
            },
            TrustedIssuerCertificates = new CertificateTrustList
            {
                StoreType = fixture.Provider.StoreTypeName,
                StorePath = fixture.Path
            }
        };
        using CertificateManager manager = CertificateManagerFactory.Create(security,
            DefaultTelemetry.Create(_ => { }), options => options.AddStoreProvider(fixture.Provider));
        using Certificate certificate = CreateCertificate("injected-provider");
        using (ICertificateStore store = manager.OpenTrustedStore(TrustListIdentifier.Peers))
        {
            store.StoreType.Should().Be(fixture.Provider.StoreTypeName);
            store.NoPrivateKeys.Should().BeTrue();
            await store.AddAsync(certificate, null, CancellationToken.None).ConfigureAwait(false);
        }

        using ICertificateStore reopened = manager.OpenTrustedStore(TrustListIdentifier.Peers);
        using CertificateCollection found = await reopened.FindByThumbprintAsync(certificate.Thumbprint)
            .ConfigureAwait(false);
        found.Should().ContainSingle().Which.RawData.Should().Equal(certificate.RawData);
        reopened.Dispose();
        manager.Dispose();
        found[0].RawData.Should().Equal(certificate.RawData);
        fixture.Provider.SupportsStorePath(fixture.Path).Should().BeTrue();
        fixture.Provider.SupportsStorePath("Directory:unrelated").Should().BeFalse();
    }

    [Test]
    public async Task CertificateManagers_KeepKubernetesProvidersInstanceScopedAsync()
    {
        using var first = new StoreFixture(true);
        using var second = new StoreFixture(true);
        using var firstManager = new CertificateManager(DefaultTelemetry.Create(_ => { }), [first.Provider]);
        using var secondManager = new CertificateManager(DefaultTelemetry.Create(_ => { }), [second.Provider]);
        firstManager.RegisterTrustList(TrustListIdentifier.Peers, first.Path, first.Path);
        secondManager.RegisterTrustList(TrustListIdentifier.Peers, second.Path, second.Path);
        using Certificate certificate = CreateCertificate("first-manager-only");
        using ICertificateStore firstStore = firstManager.OpenTrustedStore(TrustListIdentifier.Peers);
        await firstStore.AddAsync(certificate, null, CancellationToken.None).ConfigureAwait(false);
        using ICertificateStore secondStore = secondManager.OpenTrustedStore(TrustListIdentifier.Peers);

        using CertificateCollection firstCertificates = await firstStore.EnumerateAsync().ConfigureAwait(false);
        using CertificateCollection secondCertificates = await secondStore.EnumerateAsync().ConfigureAwait(false);

        firstCertificates.Should().ContainSingle();
        secondCertificates.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CertificateManager_LoadsApplicationKeyThroughInjectedProviderAsync(bool kubernetes)
    {
        using var fixture = new StoreFixture(kubernetes);
        using Certificate certificate = CreateCertificate("manager-application-key");
        await fixture.Store.AddAsync(certificate, null, CancellationToken.None).ConfigureAwait(false);
        var security = new SecurityConfiguration
        {
            ApplicationCertificates =
            [
                new CertificateIdentifier
                {
                    StoreType = fixture.Provider.StoreTypeName,
                    StorePath = fixture.Path,
                    Thumbprint = certificate.Thumbprint,
                    SubjectName = certificate.Subject,
                    CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                }
            ]
        };
        using var manager = new CertificateManager(DefaultTelemetry.Create(_ => { }), [fixture.Provider]);

        await manager.LoadApplicationCertificatesAsync(security, ct: CancellationToken.None).ConfigureAwait(false);

        using CertificateEntry loaded = manager.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType);
        loaded.Should().NotBeNull();
        loaded.Certificate.Thumbprint.Should().Be(certificate.Thumbprint);
        VerifySignature(loaded.Certificate);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationCertificateProvider_SdkDirectoryControlAsync(bool customProvider)
    {
        string directory = Path.Combine(Path.GetTempPath(), "opcplc-provider-repro-" + Guid.NewGuid().ToString("N"));
        ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
        using Certificate certificate = CreateCertificate("sdk-provider-control");
        const string providerName = "InjectedDirectory";
        var provider = new Mock<ICertificateStoreProvider>();
        provider.SetupGet(instance => instance.StoreTypeName).Returns(providerName);
        provider.Setup(instance => instance.SupportsStorePath(directory)).Returns(true);
        provider.Setup(instance => instance.CreateStore(It.IsAny<ITelemetryContext>()))
            .Returns((ITelemetryContext context) => new DirectoryCertificateStore(noSubDirs: false, context));
        try
        {
            using (var store = new DirectoryCertificateStore(noSubDirs: false, telemetry))
            {
                store.Open(directory, noPrivateKeys: false);
                await store.AddAsync(certificate, null, CancellationToken.None).ConfigureAwait(false);
            }

            using var manager = new CertificateManager(telemetry, customProvider ? [provider.Object] : null);
            var security = new SecurityConfiguration
            {
                ApplicationCertificates =
                [
                    new CertificateIdentifier
                    {
                        StoreType = customProvider ? providerName : CertificateStoreType.Directory,
                        StorePath = directory,
                        Thumbprint = certificate.Thumbprint,
                        SubjectName = certificate.Subject,
                        CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                    }
                ]
            };
            await manager.LoadApplicationCertificatesAsync(security, ct: CancellationToken.None).ConfigureAwait(false);
            using CertificateEntry entry = manager.AcquireApplicationCertificateByType(
                ObjectTypeIds.RsaSha256ApplicationCertificateType);
            entry.Should().NotBeNull();
            entry.Certificate.Thumbprint.Should().Be(certificate.Thumbprint);
            VerifySignature(entry.Certificate);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static Certificate CreateCertificate(string name)
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=" + name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        return Certificate.From(request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static void VerifySignature(Certificate certificate)
    {
        byte[] data = Encoding.UTF8.GetBytes("ownership-test");
        using RSA privateKey = certificate.GetRSAPrivateKey();
        using RSA publicKey = certificate.GetRSAPublicKey();
        byte[] signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();
    }

    private sealed class StoreFixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "opcplc-cert-" + Guid.NewGuid().ToString("N"));
        private readonly bool _kubernetes;
        private Dictionary<string, byte[]> _data = new(StringComparer.Ordinal);
        public ICertificateStore Store { get; }
        public ICertificateStoreProvider Provider { get; }
        public string Path => _kubernetes
            ? KubernetesSecretCertificateStore.StoreTypePrefix + "store"
            : FlatDirectoryCertificateStore.StoreTypePrefix + _directory;

        public StoreFixture(bool kubernetes)
        {
            _kubernetes = kubernetes;
            if (kubernetes)
            {
                var client = new Mock<IKubernetesSecretStoreClient>();
                client.Setup(instance => instance.ReadAsync("tests", "store", It.IsAny<CancellationToken>()))
                    .Returns((string _, string _, CancellationToken ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return Task.FromResult<IReadOnlyDictionary<string, byte[]>>(
                            _data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
                    });
                client.Setup(instance => instance.WriteAsync("tests", "store",
                        It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
                    .Returns((string _, string _, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct) =>
                    {
                        ct.ThrowIfCancellationRequested();
                        _data = data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
                        return Task.CompletedTask;
                    });
                var factory = new Mock<IKubernetesSecretStoreClientFactory>();
                factory.Setup(instance => instance.Create()).Returns(client.Object);
                Provider = new KubernetesSecretCertificateStoreType(NullLoggerFactory.Instance, factory.Object, "tests");
            }
            else
            {
                Directory.CreateDirectory(_directory);
                Provider = new FlatDirectoryCertificateStoreType(NullLoggerFactory.Instance);
            }
            Store = Provider.CreateStore(DefaultTelemetry.Create(_ => { }));
            Open(noPrivateKeys: false);
        }

        public void Open(bool noPrivateKeys) => Store.Open(Path, noPrivateKeys);

        public async Task SeedPemAsync(params Certificate[] certificates)
        {
            string pem = string.Join("\n", certificates.Select(certificate =>
                PemEncoding.WriteString("CERTIFICATE", certificate.RawData)));
            await SeedAsync("tls.crt", Encoding.UTF8.GetBytes(pem)).ConfigureAwait(false);
        }

        public async Task SeedAsync(string name, byte[] data)
        {
            if (_kubernetes)
            {
                _data[name] = data;
            }
            else
            {
                await File.WriteAllBytesAsync(System.IO.Path.Combine(_directory, name), data).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Store.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}