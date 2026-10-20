namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using OpcPlc.Certs;
using OpcPlc.Configuration;
using OpcPlc.Tests.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public partial class DiSecurityMigrationTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task PlcProvider_HostedCertificateCompletionCreatesAndReusesIdentityAsync(bool kubernetes, bool custom)
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "certificate-startup-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            plc.OpcUa.Hostname = "localhost";
            plc.OpcUa.OpcOwnCertStoreType = kubernetes
                ? KubernetesSecretCertificateStore.StoreTypeName : FlatDirectoryCertificateStore.StoreTypeName;
            plc.OpcUa.OpcKubernetesSecretNamespace = "startup-tests";
            if (kubernetes)
            {
                plc.OpcUa.OpcOwnCertStorePath = "startup/own";
            }
            var secrets = new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient();
            var clientFactory = new Mock<IKubernetesSecretStoreClientFactory>();
            clientFactory.Setup(factory => factory.Create()).Returns(secrets);
            using Certificate candidate = CreateApplicationReplacement(
                applicationUri: "urn:OpcPlc:localhost", domainName: Utils.GetHostName());
            if (custom)
            {
                plc.OpcUa.NewCertificateBase64String = Convert.ToBase64String(candidate.RawData);
                plc.OpcUa.PrivateKeyBase64String = Convert.ToBase64String(candidate.Export(X509ContentType.Pfx));
            }
            string thumbprint = custom ? candidate.Thumbprint : null;
            for (int restart = 0; restart < 2; restart++)
            {
                var factory = new OpcUaAppConfigFactory(plc, NullLogger.Instance, NullLoggerFactory.Instance,
                    DefaultTelemetry.Create(_ => { }), clientFactory.Object);
                var provider = new PlcApplicationConfigurationProvider(factory, deferCertificateCompletion: true);
                await using (provider.ConfigureAwait(false))
                {
                    ApplicationConfiguration configuration = await provider.GetAsync().ConfigureAwait(false);
                    if (restart == 0 && !custom)
                    {
                        using CertificateEntryCollection initial = configuration.CertificateManager
                            .SnapshotApplicationCertificates();
                        initial.Should().BeEmpty();
                    }

                    (await provider.Application.CheckApplicationInstanceCertificatesAsync(silent: true)
                        .ConfigureAwait(false)).Should().BeTrue();
                    Task firstCompletion = provider.CompleteCertificateConfigurationAsync();
                    Task repeatedCompletion = provider.CompleteCertificateConfigurationAsync();
                    repeatedCompletion.Should().BeSameAs(firstCompletion);
                    await firstCompletion.ConfigureAwait(false);
                    await repeatedCompletion.ConfigureAwait(false);
                    using CertificateEntry entry = configuration.CertificateManager.AcquireApplicationCertificateByType(
                        ObjectTypeIds.RsaSha256ApplicationCertificateType);
                    thumbprint ??= entry.Certificate.Thumbprint;
                    entry.Certificate.Thumbprint.Should().Be(thumbprint);
                    VerifyCertificateKey(entry.Certificate);
                    if (kubernetes)
                    {
                        secrets.GetSecret("startup-tests", "startup-own").Should()
                            .ContainKey(thumbprint + ".der").And.ContainKey(thumbprint + ".pfx");
                        Directory.Exists(root).Should().BeFalse();
                    }
                }
                plc.OpcUa.NewCertificateBase64String = null;
                plc.OpcUa.PrivateKeyBase64String = null;
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

    [Test]
    public async Task PlcProvider_HostedCertificateCompletionDisposalCancelsReportingAndPreservesAcquiredHandleAsync()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "certificate-disposal-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        plc.OpcUa.OpcOwnCertStoreType = KubernetesSecretCertificateStore.StoreTypeName;
        plc.OpcUa.OpcKubernetesSecretNamespace = "startup-tests";
        plc.OpcUa.OpcOwnCertStorePath = "startup/own";
        var clientFactory = new Mock<IKubernetesSecretStoreClientFactory>();
        clientFactory.Setup(factory => factory.Create())
            .Returns(new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient());
        var factory = new OpcUaAppConfigFactory(plc, NullLogger.Instance, NullLoggerFactory.Instance,
            DefaultTelemetry.Create(_ => { }), clientFactory.Object);
        var provider = new PlcApplicationConfigurationProvider(factory, deferCertificateCompletion: true);
        await using (provider.ConfigureAwait(false))
        {
            Func<Task> beforeGet = () => provider.CompleteCertificateConfigurationAsync();
            await beforeGet.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Hosted configuration must be requested before completion.").ConfigureAwait(false);
            Directory.Exists(root).Should().BeFalse();
            await provider.GetAsync().ConfigureAwait(false);
            (await provider.Application.CheckApplicationInstanceCertificatesAsync(silent: true).ConfigureAwait(false))
                .Should().BeTrue();
            using CertificateEntry retained = provider.Configuration.CertificateManager
                .AcquireApplicationCertificateByType(ObjectTypeIds.RsaSha256ApplicationCertificateType);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stalledClient = new Mock<IKubernetesSecretStoreClient>(MockBehavior.Strict);
            stalledClient.Setup(client => client.ReadAsync(
                "startup-tests", "startup-own", It.IsAny<CancellationToken>()))
                .Returns(async (string _, string _, CancellationToken ct) =>
                {
                    entered.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                    throw new InvalidOperationException("Reporting must be cancelled.");
                });
            clientFactory.Setup(client => client.Create()).Returns(stalledClient.Object);

            // Store reporting is diagnostic only and runs after completion, so it must not block startup.
            await provider.CompleteCertificateConfigurationAsync().WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            // Disposal must cancel the stalled background reporting instead of waiting for it.
            await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            VerifyCertificateKey(retained.Certificate);
            Directory.Exists(root).Should().BeFalse();
        }
    }

    [Test]
    public async Task HostedCertificateCompletion_DoesNotRepeatSdkValidationAndReportsCsrAsync()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "certificate-report-" + Guid.NewGuid().ToString("N"));
        string csrName = "certificate-startup-" + Guid.NewGuid().ToString("N");
        string csrPath = Path.Combine(Environment.CurrentDirectory, csrName + ".csr");
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            var factory = new OpcUaAppConfigFactory(plc, NullLogger.Instance, NullLoggerFactory.Instance,
                DefaultTelemetry.Create(_ => { }));
            ApplicationInstance application = factory.CreateApplication();
            await using var cleanup = application.ConfigureAwait(false);
            ApplicationConfiguration configuration = await factory.ConfigureAsync(
                application, deferCertificateCompletion: true).ConfigureAwait(false);
            (await application.CheckApplicationInstanceCertificatesAsync(silent: true).ConfigureAwait(false))
                .Should().BeTrue();
            configuration.ApplicationName = csrName;
            plc.OpcUa.ShowCreateSigningRequestInfo = true;
            var checkedApplication = new Mock<IApplicationInstance>(MockBehavior.Strict);
            checkedApplication.SetupGet(instance => instance.ApplicationConfiguration).Returns(configuration);

            await factory.CompleteCertificateConfigurationAsync(
                checkedApplication.Object, certificateAlreadyChecked: true).ConfigureAwait(false);

            checkedApplication.Verify(instance => instance.CheckApplicationInstanceCertificatesAsync(
                It.IsAny<bool>(), It.IsAny<ushort?>(), It.IsAny<CancellationToken>()), Times.Never);
            byte[] csr = await File.ReadAllBytesAsync(csrPath).ConfigureAwait(false);
            var request = CertificateRequest.LoadSigningRequest(csr, HashAlgorithmName.SHA256);
            using CertificateEntry entry = configuration.CertificateManager.AcquireApplicationCertificateByType(
                ObjectTypeIds.RsaSha256ApplicationCertificateType);
            request.SubjectName.RawData.Should().Equal(entry.Certificate.SubjectName.RawData);
            using RSA publicKey = entry.Certificate.GetRSAPublicKey();
            request.PublicKey.ExportSubjectPublicKeyInfo().Should().Equal(publicKey.ExportSubjectPublicKeyInfo());
        }
        finally
        {
            File.Delete(csrPath);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

    }

    [Test]
    public async Task HostedCertificateCompletion_RejectsPublicOnlyIdentityWithoutRepeatingSdkValidationAsync()
    {
        using Certificate certificate = CreatePeerCertificate(expired: false);
        using var chain = new CertificateCollection();
        using var original = new CertificateEntry(
            certificate, chain, ObjectTypeIds.RsaSha256ApplicationCertificateType);
        var manager = new Mock<ICertificateManager>(MockBehavior.Strict);
        manager.Setup(registry => registry.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType)).Returns(original.AddRef);
        var application = new Mock<IApplicationInstance>(MockBehavior.Strict);
        application.SetupGet(instance => instance.ApplicationConfiguration)
            .Returns(new ApplicationConfiguration { CertificateManager = manager.Object });
        var factory = new OpcUaAppConfigFactory(new OpcPlcConfiguration(), NullLogger.Instance,
            NullLoggerFactory.Instance, DefaultTelemetry.Create(_ => { }));

        Func<Task> complete = () => factory.CompleteCertificateConfigurationAsync(
            application.Object, certificateAlreadyChecked: true);

        await complete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application certificate has no private key.").ConfigureAwait(false);
        application.Verify(instance => instance.CheckApplicationInstanceCertificatesAsync(
            It.IsAny<bool>(), It.IsAny<ushort?>(), It.IsAny<CancellationToken>()), Times.Never);
        using RSA publicKey = original.Certificate.GetRSAPublicKey();
        publicKey.KeySize.Should().Be(2048);
    }

    [Test]
    public async Task PlcProvider_HostedCertificateCompletionHonorsCancellationAndNeverCreatesMissingIdentityAsync()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "certificate-missing-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            var factory = new OpcUaAppConfigFactory(plc, NullLogger.Instance, NullLoggerFactory.Instance,
                DefaultTelemetry.Create(_ => { }));
            var provider = new PlcApplicationConfigurationProvider(factory, deferCertificateCompletion: true);
            await using (provider.ConfigureAwait(false))
            {
                await provider.GetAsync().ConfigureAwait(false);
                Func<Task> canceled = () => provider.CompleteCertificateConfigurationAsync(new CancellationToken(true));
                await canceled.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                Func<Task> missing = () => provider.CompleteCertificateConfigurationAsync();
                await missing.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Application certificate could not be loaded.").ConfigureAwait(false);
                using CertificateEntryCollection certificates = provider.Configuration.CertificateManager
                    .SnapshotApplicationCertificates();
                certificates.Should().BeEmpty();
            }
            Func<Task> disposed = () => provider.CompleteCertificateConfigurationAsync();
            await disposed.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
