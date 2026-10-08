// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using Opc.Ua.Server.Hosting;
using OpcPlc.Certs;
using OpcPlc.Configuration;
using OpcPlc.Tests.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class DiSecurityMigrationTests
{
    private const string ApplicationName = "OpcPlc";
    private const string ApplicationUri = "urn:localhost:OpcPlcDiSecurityProbe";
    private const string ProductUri = "urn:opcplc:test:di-security";

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task PlcProvider_CustomStorePersistsAndReusesIdentityWithoutDirectoryFallbackAsync(
        bool kubernetes, bool existingIdentity)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-custom-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            plc.OpcUa.OpcOwnCertStoreType = kubernetes
                ? KubernetesSecretCertificateStore.StoreTypeName : FlatDirectoryCertificateStore.StoreTypeName;
            plc.OpcUa.OpcKubernetesSecretNamespace = "provider-tests";
            if (kubernetes)
            {
                plc.OpcUa.OpcOwnCertStorePath = "pki/own";
            }
            string prefix = kubernetes
                ? KubernetesSecretCertificateStore.StoreTypePrefix : FlatDirectoryCertificateStore.StoreTypePrefix;
            var clientFactory = new Mock<IKubernetesSecretStoreClientFactory>(MockBehavior.Strict);
            var secretClient = new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient();
            if (kubernetes)
            {
                clientFactory.Setup(factory => factory.Create()).Returns(secretClient);
            }
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            string thumbprint = null;
            if (existingIdentity)
            {
                ICertificateStoreProvider storeProvider = kubernetes
                    ? new KubernetesSecretCertificateStoreType(logging, clientFactory.Object, "provider-tests")
                    : new FlatDirectoryCertificateStoreType(logging);
                using ICertificateStore store = storeProvider.CreateStore(DefaultTelemetry.Create(_ => { }));
                store.Open(prefix + plc.OpcUa.OpcOwnCertStorePath, noPrivateKeys: false);
                using Certificate other = CreateApplicationReplacement(
                    applicationUri: "urn:OtherApplication:existing", applicationName: "OtherApplication");
                using Certificate original = CreateApplicationReplacement(
                    applicationUri: "urn:OpcPlc:" + plc.OpcUa.HostnameLabel,
                    subjectSuffix: ", O=ExistingDeployment, OU=PLC, DC=host",
                    domainName: plc.OpcUa.Hostname);
                await store.AddAsync(other).ConfigureAwait(false);
                await store.AddAsync(original).ConfigureAwait(false);
                thumbprint = original.Thumbprint;
            }
            for (int restart = 0; restart < 2; restart++)
            {
                var provider = new PlcApplicationConfigurationProvider(new OpcUaAppConfigFactory(plc,
                    logging.CreateLogger("provider-test"), logging,
                    DefaultTelemetry.Create(_ => { }), clientFactory.Object));
                await using (provider.ConfigureAwait(false))
                {
                    ApplicationConfiguration configuration = await provider.GetAsync().ConfigureAwait(false);
                    SecurityConfiguration security = configuration.SecurityConfiguration;
                    security.ApplicationCertificates[0].StoreType.Should().Be(plc.OpcUa.OpcOwnCertStoreType);
                    security.ApplicationCertificates[0].StorePath.Should().Be(prefix + plc.OpcUa.OpcOwnCertStorePath);
                    security.TrustedUserCertificates.StoreType.Should().Be(plc.OpcUa.OpcOwnCertStoreType);
                    security.TrustedUserCertificates.StorePath.Should().Be(prefix + plc.OpcUa.OpcTrustedUserCertStorePath);
                    security.RejectedCertificateStore.StorePath.Should().Be(prefix + plc.OpcUa.OpcRejectedCertStorePath);
                    using CertificateEntry entry = configuration.CertificateManager.AcquireApplicationCertificateByType(
                        ObjectTypeIds.RsaSha256ApplicationCertificateType);
                    entry.Should().NotBeNull();
                    thumbprint ??= entry.Certificate.Thumbprint;
                    entry.Certificate.Thumbprint.Should().Be(thumbprint);
                    VerifyCertificateKey(entry.Certificate);
                    if (kubernetes)
                    {
                        secretClient.GetSecret("provider-tests", "pki-own")
                            .Should().ContainKey(thumbprint + ".der").And.ContainKey(thumbprint + ".pfx");
                        Directory.Exists(root).Should().BeFalse();
                    }
                    else
                    {
                        int expectedCount = existingIdentity ? 2 : 1;
                        Directory.EnumerateFiles(plc.OpcUa.OpcOwnCertStorePath, "*.der")
                            .Should().HaveCount(expectedCount);
                        Directory.EnumerateFiles(plc.OpcUa.OpcOwnCertStorePath, "*.pfx")
                            .Should().HaveCount(expectedCount);
                    }
                }
            }
            clientFactory.Verify(factory => factory.Create(), kubernetes ? Times.AtLeastOnce() : Times.Never());
            clientFactory.VerifyNoOtherCalls();
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
    public async Task PlcProvider_DirectFactoryConstructionRemainsAvailableAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-direct-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        try
        {
            plc.OpcUa.Hostname = "localhost";
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("direct-test"), logging,
                DefaultTelemetry.Create(_ => { }));
            ApplicationConfiguration configuration = await factory.ConfigureAsync().ConfigureAwait(false);
            configuration.Should().BeSameAs(plc.OpcUa.ApplicationConfiguration);
            configuration.ApplicationUri.Should().Be("urn:OpcPlc:localhost");
            using CertificateEntry entry = configuration.CertificateManager.AcquireApplicationCertificateByType(
                ObjectTypeIds.RsaSha256ApplicationCertificateType);
            VerifyCertificateKey(entry.Certificate);
            ((CertificateManager)configuration.CertificateManager).Dispose();
            VerifyCertificateKey(entry.Certificate);
            configuration.CertificateManager.AcquireApplicationCertificateByType(
                ObjectTypeIds.RsaSha256ApplicationCertificateType).Should().BeNull();
            Action openAfterDispose = () =>
            {
                using ICertificateStore store = ((CertificateManager)configuration.CertificateManager)
                    .OpenCertificateStore(plc.OpcUa.OpcOwnCertStorePath, plc.OpcUa.OpcOwnCertStoreType);
            };
            openAfterDispose.Should().Throw<ObjectDisposedException>();
        }
        finally
        {
            (plc.OpcUa.ApplicationConfiguration?.CertificateManager as IDisposable)?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public async Task PlcProvider_DirectFactoryFailureDisposesCertificateManagerAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-direct-failure-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        try
        {
            plc.OpcUa.NewCertificateBase64String = "!";
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("direct-failure"), logging,
                DefaultTelemetry.Create(_ => { }));
            Func<Task> configure = () => factory.ConfigureAsync();
            await configure.Should().ThrowAsync<Exception>()
                .WithMessage("Update/Setting of the application certificate failed.").ConfigureAwait(false);
            plc.OpcUa.ApplicationConfiguration.CertificateManager.Should().NotBeNull();
            Action openAfterFailure = () =>
            {
                using ICertificateStore store = ((CertificateManager)plc.OpcUa.ApplicationConfiguration.CertificateManager)
                    .OpenCertificateStore(plc.OpcUa.OpcOwnCertStorePath, plc.OpcUa.OpcOwnCertStoreType);
            };
            openAfterFailure.Should().Throw<ObjectDisposedException>();
        }
        finally
        {
            (plc.OpcUa.ApplicationConfiguration?.CertificateManager as IDisposable)?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Test]
    public async Task PlcProvider_DirectFactoryCancellationDisposesCertificateManagerAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-direct-cancel-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        plc.OpcUa.OpcOwnCertStoreType = KubernetesSecretCertificateStore.StoreTypeName;
        plc.OpcUa.OpcKubernetesSecretNamespace = "provider-tests";
        plc.OpcUa.OpcOwnCertStorePath = @"pki\own";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new Mock<IKubernetesSecretStoreClient>(MockBehavior.Strict);
        client.Setup(instance => instance.ReadAsync(
            "provider-tests", "pki-own", It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken ct) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                throw new InvalidOperationException("The stalled read must be cancelled.");
            });
        var clientFactory = new Mock<IKubernetesSecretStoreClientFactory>();
        clientFactory.Setup(instance => instance.Create()).Returns(client.Object);
        var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("direct-cancel"), logging,
            DefaultTelemetry.Create(_ => { }), clientFactory.Object);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task<ApplicationConfiguration> pending = factory.ConfigureAsync(cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await cancellation.CancelAsync().ConfigureAwait(false);
            Func<Task> configure = () => pending;
            await configure.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            plc.OpcUa.ApplicationConfiguration.CertificateManager.Should().NotBeNull();
            Action openAfterCancellation = () =>
            {
                using ICertificateStore store = ((CertificateManager)plc.OpcUa.ApplicationConfiguration.CertificateManager)
                    .OpenCertificateStore(
                        KubernetesSecretCertificateStore.StoreTypePrefix + plc.OpcUa.OpcOwnCertStorePath,
                        plc.OpcUa.OpcOwnCertStoreType);
            };
            openAfterCancellation.Should().Throw<ObjectDisposedException>();
            Directory.Exists(root).Should().BeFalse();
        }
        finally
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await Task.WhenAny(pending).ConfigureAwait(false);
            (plc.OpcUa.ApplicationConfiguration?.CertificateManager as IDisposable)?.Dispose();
        }
    }

    [Test]
    public async Task PlcProvider_HostUsesFactoryConfigurationAndReusesIdentityAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-host-" + Guid.NewGuid().ToString("N"));
        try
        {
            int port;
            using (var reservation = new TcpListener(IPAddress.Loopback, 0))
            {
                reservation.Start();
                port = ((IPEndPoint)reservation.LocalEndpoint).Port;
            }
            string thumbprint = null;
            for (int restart = 0; restart < 2; restart++)
            {
                var plc = CreatePlcConfiguration(root, false);
                plc.OpcUa.Hostname = "localhost";
                plc.OpcUa.ServerPort = (ushort)port;
                plc.OpcUa.ServerPath = "/plc-provider";
                plc.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "cli-selected-trust");
                plc.DisableCertAuth = true;
                var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
                {
                    DisableDefaults = true
                });
                builder.Logging.AddConsole();
                builder.Services.AddOpcUa().AddServer(options =>
                {
                    options.EndpointUrls.Add("opc.tcp://localhost:1/unused-default");
                }).AddDefaultIdentityAuthenticators(options =>
                {
                    options.EnableAnonymous = true;
                    options.EnableUserNamePassword = false;
                    options.EnableX509 = false;
                    options.EnableJwt = false;
                });
                builder.Services.AddSingleton<IOpcUaApplicationConfigurationProvider>(services =>
                    new PlcApplicationConfigurationProvider(new OpcUaAppConfigFactory(plc,
                        services.GetRequiredService<ILogger<OpcUaAppConfigFactory>>(),
                        services.GetRequiredService<ILoggerFactory>(),
                        services.GetRequiredService<ITelemetryContext>())));
                var observer = new ServerStartedObserver();
                builder.Services.AddSingleton<IServerStartupTask>(observer);
                IHost host = builder.Build();
                await using (((IAsyncDisposable)host).ConfigureAwait(false))
                {
                    try
                    {
                        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        await host.StartAsync(deadline.Token).ConfigureAwait(false);
                        await observer.Started.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
                        using var connection = new TcpClient();
                        await connection.ConnectAsync(IPAddress.Loopback, port, deadline.Token).ConfigureAwait(false);
                        connection.Connected.Should().BeTrue();
                        var provider = host.Services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                        provider.Should().BeOfType<PlcApplicationConfigurationProvider>();
                        var config = await provider.GetAsync(deadline.Token).ConfigureAwait(false);
                        config.Should().BeSameAs(plc.OpcUa.ApplicationConfiguration);
                        config.ApplicationUri.Should().Be("urn:OpcPlc:localhost:plc-provider");
                        config.SecurityConfiguration.TrustedPeerCertificates.StorePath.Should()
                            .Be(plc.OpcUa.OpcTrustedCertStorePath);
                        using CertificateEntry entry = config.CertificateManager.AcquireApplicationCertificateByType(
                            ObjectTypeIds.RsaSha256ApplicationCertificateType);
                        thumbprint ??= entry.Certificate.Thumbprint;
                        entry.Certificate.Thumbprint.Should().Be(thumbprint);
                        VerifyCertificateKey(entry.Certificate);
                        using Certificate peer = CreatePeerCertificate(expired: false);
                        var service = new CertificateManagementService(plc, Mock.Of<ILogger>(),
                            host.Services.GetRequiredService<ITelemetryContext>());
                        (await service.AddCertificatesAsync([Convert.ToBase64String(peer.RawData)], null, false)
                            .ConfigureAwait(false)).Should().BeTrue();
                        using ICertificateStore store = config.CertificateManager
                            .OpenTrustedStore(TrustListIdentifier.Peers);
                        store.StorePath.Should().Be(plc.OpcUa.OpcTrustedCertStorePath);
                        using CertificateCollection stored = await store.FindByThumbprintAsync(peer.Thumbprint)
                            .ConfigureAwait(false);
                        stored.Should().ContainSingle();
                    }
                    finally
                    {
                        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        await host.StopAsync(stop.Token).ConfigureAwait(false);
                    }
                    using var released = new TcpListener(IPAddress.Loopback, port);
                    released.Server.ExclusiveAddressUse = true;
                    released.Start();
                }
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
    public async Task PlcProvider_SharesInitializationAndPreservesCancellationAndDisposalAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-life-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("provider-test"), logging,
                DefaultTelemetry.Create(_ => { }));
            var provider = new PlcApplicationConfigurationProvider(factory);
            await using (provider.ConfigureAwait(false))
            {
                Func<Task> cancelled = () => provider.GetAsync(new CancellationToken(true));
                await cancelled.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                Directory.Exists(root).Should().BeFalse();
                Task<ApplicationConfiguration>[] pending = Enumerable.Range(0, 8)
                    .Select(_ => provider.GetAsync()).ToArray();
                ApplicationConfiguration[] results = await Task.WhenAll(pending).ConfigureAwait(false);
                results.Should().OnlyContain(config => ReferenceEquals(config, results[0]));
                using CertificateEntry retained = results[0].CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                await provider.DisposeAsync().ConfigureAwait(false);
                await provider.DisposeAsync().ConfigureAwait(false);
                VerifyCertificateKey(retained.Certificate);
                Func<Task> afterDispose = () => provider.GetAsync();
                await afterDispose.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
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
    public async Task PlcProvider_DisposalCancelsStalledStoreWithoutCancellingSharedWaitersAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-cancel-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        plc.OpcUa.OpcOwnCertStoreType = KubernetesSecretCertificateStore.StoreTypeName;
        plc.OpcUa.OpcKubernetesSecretNamespace = "provider-tests";
        plc.OpcUa.OpcOwnCertStorePath = "pki/own";
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new Mock<IKubernetesSecretStoreClient>(MockBehavior.Strict);
        client.Setup(instance => instance.ReadAsync(
            "provider-tests", "pki-own", It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken ct) =>
            {
                entered.TrySetResult(ct);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                throw new InvalidOperationException("The stalled read must be cancelled.");
            });
        var clientFactory = new Mock<IKubernetesSecretStoreClientFactory>();
        clientFactory.Setup(instance => instance.Create()).Returns(client.Object);
        var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var provider = new PlcApplicationConfigurationProvider(new OpcUaAppConfigFactory(plc,
            logging.CreateLogger("provider-cancel"), logging, DefaultTelemetry.Create(_ => { }), clientFactory.Object));
        await using (provider.ConfigureAwait(false))
        {
            using var waiterCancellation = new CancellationTokenSource();
            Task<ApplicationConfiguration> cancelledWaiter = provider.GetAsync(waiterCancellation.Token);
            Task<ApplicationConfiguration> sharedWaiter = provider.GetAsync();
            CancellationToken operationToken = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
            operationToken.CanBeCanceled.Should().BeTrue();

            await waiterCancellation.CancelAsync().ConfigureAwait(false);
            Func<Task> wait = () => cancelledWaiter;
            await wait.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            operationToken.IsCancellationRequested.Should().BeFalse();
            sharedWaiter.IsCompleted.Should().BeFalse();

            await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            operationToken.IsCancellationRequested.Should().BeTrue();
            Func<Task> shared = () => sharedWaiter;
            await shared.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            await provider.DisposeAsync().ConfigureAwait(false);
            Directory.Exists(root).Should().BeFalse();
        }
    }

    [Test]
    public async Task PlcProvider_DoesNotRepeatFailedCertificateInstallationAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-fail-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            plc.OpcUa.NewCertificateBase64String = "!";
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("provider-test"), logging,
                DefaultTelemetry.Create(_ => { }));
            var provider = new PlcApplicationConfigurationProvider(factory);
            await using (provider.ConfigureAwait(false))
            {
                Task<ApplicationConfiguration> first = provider.GetAsync();
                Func<Task> failed = () => first;
                await failed.Should().ThrowAsync<Exception>()
                    .WithMessage("Update/Setting of the application certificate failed.").ConfigureAwait(false);
                plc.OpcUa.NewCertificateBase64String = null;
                provider.GetAsync().Should().BeSameAs(first);
                using CertificateEntryCollection entries = provider.Configuration.CertificateManager
                    .SnapshotApplicationCertificates();
                entries.Should().BeEmpty();
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

    [TestCase(true, false, false, false)]
    [TestCase(false, true, true, false)]
    [TestCase(false, false, true, true)]
    [TestCase(true, true, true, true)]
    public async Task PlcProvider_MapsEnabledUserTokenPoliciesAsync(
        bool disableAnonymous, bool disableCertificate, bool credentials, bool disablePassword)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-auth-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            plc.DisableAnonymousAuth = disableAnonymous;
            plc.DisableCertAuth = disableCertificate;
            plc.DisableUsernamePasswordAuth = disablePassword;
            if (credentials)
            {
                plc.DefaultUser = "operator";
                plc.DefaultPassword = "test-only";
            }
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("provider-test"), logging,
                DefaultTelemetry.Create(_ => { }));
            var provider = new PlcApplicationConfigurationProvider(factory);
            await using (provider.ConfigureAwait(false))
            {
                if (disableAnonymous && disableCertificate && (!credentials || disablePassword))
                {
                    Func<Task> create = () => provider.GetAsync();
                    await create.Should().ThrowAsync<InvalidOperationException>()
                        .WithMessage("At least one user authentication method must be enabled.").ConfigureAwait(false);
                    Directory.Exists(root).Should().BeFalse();
                    return;
                }
                ApplicationConfiguration config = await provider.GetAsync().ConfigureAwait(false);
                UserTokenType[] policies = config.ServerConfiguration.UserTokenPolicies.ToArray()
                    .Select(policy => policy.TokenType).ToArray();
                policies.Contains(UserTokenType.Anonymous).Should().Be(!disableAnonymous);
                policies.Contains(UserTokenType.Certificate).Should().Be(!disableCertificate);
                policies.Contains(UserTokenType.UserName).Should().Be(credentials && !disablePassword);
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

    [TestCase(false, 1)]
    [TestCase(false, 73)]
    [TestCase(true, 73)]
    [TestCase(false, 100)]
    [TestCase(false, int.MaxValue - 3)]
    public async Task PlcProvider_PreservesExplicitPathsIdentityAndServerSettingsAsync(bool unsecure, int maxSessionCount)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-provider-" + Guid.NewGuid().ToString("N"));
        try
        {
            var plc = CreatePlcConfiguration(root, false);
            plc.OpcUa.Hostname = "LOCALHOST";
            plc.OpcUa.ServerPort = 51234;
            plc.OpcUa.ServerPath = "/factory/line";
            plc.OpcUa.DnsNames = ["alternate.example.test"];
            plc.OpcUa.OpcTrustedCertStorePath = Path.Combine(root, "custom-peers");
            plc.OpcUa.OpcIssuerCertStorePath = Path.Combine(root, "custom-issuers");
            plc.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(root, "custom-users");
            plc.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(root, "custom-user-issuers");
            plc.OpcUa.OpcRejectedCertStorePath = Path.Combine(root, "custom-rejections");
            plc.OpcUa.EnableUnsecureTransport = unsecure;
            plc.OpcUa.MaxSessionCount = maxSessionCount;
            plc.OpcUa.MaxSubscriptionCount = 91;
            plc.OpcUa.OpcMaxStringLength = 123456;
            plc.OpcUa.ReverseConnectClientUrls = ["opc.tcp://localhost:51235/client"];
            plc.DefaultUser = "operator";
            plc.DefaultPassword = "test-only";
            var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("provider-test"), logging,
                DefaultTelemetry.Create(_ => { }));
            var provider = new PlcApplicationConfigurationProvider(factory);
            await using (provider.ConfigureAwait(false))
            {
                ApplicationConfiguration config = await provider.GetAsync().ConfigureAwait(false);
                config.Should().BeSameAs(plc.OpcUa.ApplicationConfiguration).And.BeSameAs(provider.Configuration);
                (await provider.GetAsync().ConfigureAwait(false)).Should().BeSameAs(config);
                config.ApplicationName.Should().Be("OpcPlc");
                config.ApplicationUri.Should().Be("urn:OpcPlc:localhost:factory:line");
                config.ProductUri.Should().Be(plc.OpcUa.ProductUri);
                config.ServerConfiguration.BaseAddresses.ToArray().Should()
                    .Equal(Utils.ReplaceLocalhost("opc.tcp://localhost:51234/factory/line"));
                config.ServerConfiguration.AlternateBaseAddresses.ToArray().Should()
                    .Equal("opc.tcp://alternate.example.test:51234/factory/line");
                config.ServerConfiguration.SecurityPolicies.ToArray().Should()
                    .Contain(policy => policy.SecurityMode == MessageSecurityMode.Sign)
                    .And.Contain(policy => policy.SecurityMode == MessageSecurityMode.SignAndEncrypt);
                config.ServerConfiguration.SecurityPolicies.ToArray()
                    .Any(policy => policy.SecurityMode == MessageSecurityMode.None).Should().Be(unsecure);
                config.ServerConfiguration.UserTokenPolicies.ToArray().Select(policy => policy.TokenType).Should()
                    .BeEquivalentTo(new[]
                    {
                        UserTokenType.Anonymous, UserTokenType.UserName, UserTokenType.Certificate
                    });
                config.ServerConfiguration.MaxSessionCount.Should().Be(maxSessionCount);
                config.ServerConfiguration.MaxChannelCount.Should().Be(maxSessionCount + 3);
                config.ServerConfiguration.MaxSubscriptionCount.Should().Be(91);
                config.TransportQuotas.MaxStringLength.Should().Be(123456);
                config.ServerConfiguration.OperationLimits.MaxNodesPerRead.Should().Be(2500);
                config.ServerConfiguration.ReverseConnect.Clients[0].EndpointUrl.Should()
                    .Be("opc.tcp://localhost:51235/client");
                config.SecurityConfiguration.TrustedPeerCertificates.StorePath.Should()
                    .Be(plc.OpcUa.OpcTrustedCertStorePath);
                config.SecurityConfiguration.TrustedIssuerCertificates.StorePath.Should()
                    .Be(plc.OpcUa.OpcIssuerCertStorePath);
                config.SecurityConfiguration.TrustedUserCertificates.StorePath.Should()
                    .Be(plc.OpcUa.OpcTrustedUserCertStorePath);
                config.SecurityConfiguration.UserIssuerCertificates.StorePath.Should()
                    .Be(plc.OpcUa.OpcUserIssuerCertStorePath);
                config.SecurityConfiguration.RejectedCertificateStore.StorePath.Should()
                    .Be(plc.OpcUa.OpcRejectedCertStorePath);
                using CertificateEntry entry = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                entry.Should().NotBeNull();
                VerifyCertificateKey(entry.Certificate);
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

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MaxValue - 2)]
    [TestCase(int.MaxValue - 1)]
    [TestCase(int.MaxValue)]
    public async Task PlcProvider_RejectsSessionCapacityWithoutReconnectHeadroomAsync(int maxSessionCount)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-capacity-" + Guid.NewGuid().ToString("N"));
        var plc = CreatePlcConfiguration(root, false);
        plc.OpcUa.MaxSessionCount = maxSessionCount;
        var logging = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var factory = new OpcUaAppConfigFactory(plc, logging.CreateLogger("provider-test"), logging,
            DefaultTelemetry.Create(_ => { }));
        Func<Task> configure = () => factory.ConfigureAsync();

        await configure.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(plc.OpcUa.MaxSessionCount)).ConfigureAwait(false);
        Directory.Exists(root).Should().BeFalse();
    }

    [TestCase("empty")]
    [TestCase("bad-cert-base64")]
    [TestCase("bad-key-base64")]
    [TestCase("wrong-password")]
    [TestCase("wrong-key")]
    [TestCase("missing-key")]
    [TestCase("missing-file")]
    [TestCase("wrong-subject")]
    public async Task ApplicationImport_InvalidInputPreservesOldIdentityAsync(string failure)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-import-fail-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                var config = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry old = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, false).ConfigureAwait(false);
                var plc = CreatePlcConfiguration(root, false);
                plc.OpcUa.ApplicationConfiguration = config;
                var service = new CertificateManagementService(plc, Mock.Of<ILogger>(),
                    services.GetRequiredService<ITelemetryContext>());
                using Certificate candidate = failure == "wrong-subject"
                    ? CreateCrlSigner("wrong-subject") : CreateApplicationReplacement();
                using Certificate other = CreateApplicationReplacement();
                string certBase64 = failure == "empty" ? null : failure == "bad-cert-base64" ? "!"
                    : Convert.ToBase64String(candidate.RawData);
                string keyBase64 = failure == "missing-key" ? null : failure == "bad-key-base64" ? "!"
                    : Convert.ToBase64String((failure == "wrong-key" ? other : candidate)
                        .Export(X509ContentType.Pfx, "correct-password"));
                string file = failure == "missing-file" ? Path.Combine(root, "absent.der") : null;
                string metadata = config.SecurityConfiguration.ApplicationCertificates[0].Thumbprint;

                (await service.UpdateApplicationCertificateAsync(certBase64, file,
                    failure == "wrong-password" ? "incorrect-password" : "correct-password", keyBase64, null)
                    .ConfigureAwait(false)).Should().BeFalse();

                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(old.Certificate.Thumbprint);
                config.SecurityConfiguration.ApplicationCertificates[0].Thumbprint.Should().Be(metadata);
                VerifyCertificateKey(active.Certificate);
                using CertificateCollection stored = await ApplicationCertificateLifecycle
                    .EnumerateApplicationCertificatesAsync(config.SecurityConfiguration.ApplicationCertificates[0], [],
                        services.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);
                stored.Should().ContainSingle().Which.Thumbprint.Should().Be(old.Certificate.Thumbprint);
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

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task ApplicationImport_RenewsUsingCurrentPrivateKeyAsync(bool certificateFile, bool unusableKey)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-renew-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                var config = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry old = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, false).ConfigureAwait(false);
                using RSA key = old.Certificate.GetRSAPrivateKey();
                using Certificate renewed = CreateApplicationReplacement(existingKey: key);
                using Certificate unrelated = CreateApplicationReplacement();
                var plc = CreatePlcConfiguration(root, false);
                plc.OpcUa.ApplicationConfiguration = config;
                var service = new CertificateManagementService(plc, Mock.Of<ILogger>(),
                    services.GetRequiredService<ITelemetryContext>());
                string file = Path.Combine(root, "renewed.pem");
                await File.WriteAllTextAsync(file, PemEncoding.WriteString("CERTIFICATE", renewed.RawData))
                    .ConfigureAwait(false);

                (await service.UpdateApplicationCertificateAsync(
                    certificateFile ? "ignored-when-file-is-present" : Convert.ToBase64String(renewed.RawData),
                    certificateFile ? file : null, "renew-password",
                    unusableKey
                        ? Convert.ToBase64String(unrelated.Export(X509ContentType.Pfx, "renew-password")) : null,
                    null).ConfigureAwait(false)).Should().BeTrue();

                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(renewed.Thumbprint).And.NotBe(old.Certificate.Thumbprint);
                VerifyCertificateKey(active.Certificate);
                VerifyCertificateKey(old.Certificate);
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

    [TestCase("peer", false)]
    [TestCase("peer", true)]
    [TestCase("issuer", false)]
    [TestCase("issuer", true)]
    [TestCase("unknown", true)]
    [TestCase("revoked", true)]
    [TestCase("cached-missing-crl", true)]
    [TestCase("missing-crl", true)]
    [TestCase("expired", true)]
    public async Task ApplicationImport_EnforcesIssuerValidationDespiteAutoAcceptAsync(string trust, bool autoAccept)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-import-trust-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root, autoAccept);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                var config = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry old = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, false).ConfigureAwait(false);
                var plc = CreatePlcConfiguration(root, autoAccept);
                plc.OpcUa.ApplicationConfiguration = config;
                var service = new CertificateManagementService(plc,
                    services.GetRequiredService<ILogger<CertificateManagementService>>(),
                    services.GetRequiredService<ITelemetryContext>());
                using Certificate issuer = CreateCrlSigner("import-root");
                using Certificate candidate = CreateApplicationReplacement(issuer, expired: trust == "expired");
                if (trust != "unknown")
                {
                    (await service.AddCertificatesAsync([Convert.ToBase64String(issuer.RawData)], null,
                        issuerCertificate: trust == "issuer").ConfigureAwait(false)).Should().BeTrue();
                    string revokedSerial = trust == "revoked" ? candidate.SerialNumber : "01";
                    if (trust is not ("missing-crl" or "cached-missing-crl"))
                    {
                        (await service.UpdateCrlAsync(
                            Convert.ToBase64String(CreateCrl(issuer, revokedSerial).RawData), null)
                            .ConfigureAwait(false)).Should().BeTrue();
                    }
                }
                int callbackCalls = 0;
                config.CertificateManager.AcceptError = (_, _) =>
                {
                    callbackCalls++;
                    return autoAccept;
                };
                if (trust == "cached-missing-crl")
                {
                    using var chain = new CertificateCollection { candidate };
                    var permissive = await config.CertificateManager.ValidateAsync(chain, TrustListIdentifier.Peers,
                        new Opc.Ua.Security.Certificates.CertificateValidationOptions
                        {
                            RejectUnknownRevocationStatus = false
                        }).ConfigureAwait(false);
                    permissive.IsValid.Should().BeTrue();
                    callbackCalls = 0;
                }
                using RSA key = candidate.GetRSAPrivateKey();
                bool success = await service.UpdateApplicationCertificateAsync(
                    Convert.ToBase64String(candidate.RawData), null, null,
                    Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())), null)
                    .ConfigureAwait(false);
                bool expected = trust is "peer" or "issuer";
                success.Should().Be(expected);
                callbackCalls.Should().Be(0);
                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(expected ? candidate.Thumbprint : old.Certificate.Thumbprint);
                VerifyCertificateKey(active.Certificate);
                using CertificateCollection stored = await ApplicationCertificateLifecycle
                    .EnumerateApplicationCertificatesAsync(config.SecurityConfiguration.ApplicationCertificates[0], [],
                        services.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);
                stored.Should().ContainSingle().Which.Thumbprint.Should().Be(active.Certificate.Thumbprint);
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

    [TestCase("pfx")]
    [TestCase("pem")]
    [TestCase("encrypted-pem")]
    [TestCase("key-file")]
    public async Task ApplicationImport_ServiceInstallsMatchingPrivateKeyAsync(string format)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-import-" + Guid.NewGuid().ToString("N"));
        try
        {
            string thumbprint;
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var config = await services.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                    .GetAsync(CancellationToken.None).ConfigureAwait(false);
                var plc = CreatePlcConfiguration(root, false);
                plc.OpcUa.ApplicationConfiguration = config;
                var service = new CertificateManagementService(plc,
                    services.GetRequiredService<ILogger<CertificateManagementService>>(),
                    services.GetRequiredService<ITelemetryContext>());
                using Certificate candidate = CreateApplicationReplacement();
                using RSA key = candidate.GetRSAPrivateKey();
                string password = "test-import-password";
                byte[] keyData = format switch
                {
                    "pfx" => candidate.Export(X509ContentType.Pfx, password),
                    "pem" => System.Text.Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem()),
                    _ => System.Text.Encoding.UTF8.GetBytes(key.ExportEncryptedPkcs8PrivateKeyPem(password,
                        new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1000)))
                };
                thumbprint = candidate.Thumbprint;
                string keyFile = null;
                if (format == "key-file")
                {
                    Directory.CreateDirectory(root);
                    keyFile = Path.Combine(root, "input.key");
                    await File.WriteAllBytesAsync(keyFile, keyData).ConfigureAwait(false);
                }
                (await service.UpdateApplicationCertificateAsync(Convert.ToBase64String(candidate.RawData), null,
                    password, format == "key-file" ? "AQID" : Convert.ToBase64String(keyData), keyFile)
                    .ConfigureAwait(false)).Should().BeTrue();
                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(thumbprint);
                VerifyCertificateKey(active.Certificate);
            }
            ServiceProvider restarted = CreateServices(root);
            await using (restarted.ConfigureAwait(false))
            {
                var config = await restarted.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                    .GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(thumbprint);
                VerifyCertificateKey(active.Certificate);
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

    [TestCase("add-fails")]
    [TestCase("reload-fails")]
    [TestCase("public-only")]
    [TestCase("wrong-subject")]
    [TestCase("cancelled")]
    public async Task ApplicationActivation_FailurePreservesExistingIdentityAsync(string failure)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-activate-fail-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                var config = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry old = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, false).ConfigureAwait(false);
                using Certificate valid = CreateApplicationReplacement();
                using Certificate publicOnly = new(valid.RawData);
                using Certificate wrongSubject = CreateCrlSigner("wrong-subject");
                Certificate candidate = failure == "public-only" ? publicOnly
                    : failure == "wrong-subject" ? wrongSubject : valid;
                var store = new Mock<ICertificateStore>();
                store.Setup(instance => instance.FindByThumbprintAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new CertificateCollection());
                if (failure == "add-fails")
                {
                    store.Setup(instance => instance.AddAsync(It.IsAny<Certificate>(), It.IsAny<char[]>(),
                        It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Injected add failure"));
                }
                var storeProvider = new Mock<ICertificateStoreProvider>();
                storeProvider.SetupGet(instance => instance.StoreTypeName).Returns(CertificateStoreType.Directory);
                storeProvider.Setup(instance => instance.CreateStore(It.IsAny<ITelemetryContext>()))
                    .Returns(store.Object);
                string previousThumbprint = config.SecurityConfiguration.ApplicationCertificates[0].Thumbprint;
                Func<Task> activate = () => ApplicationCertificateLifecycle.PersistAndActivateAsync(
                    config, candidate, [storeProvider.Object], services.GetRequiredService<ITelemetryContext>(),
                    new CancellationToken(failure == "cancelled"));

                if (failure == "cancelled")
                {
                    await activate.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
                else if (failure == "add-fails")
                {
                    await activate.Should().ThrowAsync<IOException>().ConfigureAwait(false);
                }
                else
                {
                    await activate.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
                }
                store.Verify(instance => instance.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                using CertificateEntry current = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                current.Certificate.Thumbprint.Should().Be(old.Certificate.Thumbprint);
                config.SecurityConfiguration.ApplicationCertificates[0].Thumbprint.Should().Be(previousThumbprint);
                VerifyCertificateKey(current.Certificate);
                using CertificateCollection stored = await ApplicationCertificateLifecycle
                    .EnumerateApplicationCertificatesAsync(config.SecurityConfiguration.ApplicationCertificates[0], [],
                        services.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);
                stored.Should().ContainSingle().Which.Thumbprint.Should().Be(old.Certificate.Thumbprint);
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

    [TestCase("empty")]
    [TestCase("malformed")]
    [TestCase("missing-file")]
    [TestCase("cancelled")]
    public async Task CrlReplacement_InvalidInputNeverWritesAsync(string input)
    {
        var manager = new Mock<ICertificateTrustListManager>(MockBehavior.Strict);
        var fileAccess = new Mock<ITrustListFileAccess>(MockBehavior.Strict);
        var operations = new CertificateTrustListOperations(manager.Object, Mock.Of<ILogger>());
        string file = input == "missing-file" ? Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".crl") : null;
        Func<Task<bool>> replace = () => operations.ReplaceCrlAsync(fileAccess.Object,
            input == "empty" ? null : "!", file, new CancellationToken(input == "cancelled"));
        if (input == "cancelled")
        {
            await replace.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        }
        else
        {
            (await replace().ConfigureAwait(false)).Should().BeFalse();
        }
        fileAccess.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ApplicationActivation_PersistsRotatedKeyAndKeepsAcquiredOldHandleAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-activate-" + Guid.NewGuid().ToString("N"));
        try
        {
            string thumbprint;
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                ApplicationConfiguration config = await provider.GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry old = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, false).ConfigureAwait(false);
                using Certificate replacement = CreateApplicationReplacement();
                thumbprint = replacement.Thumbprint;

                await ApplicationCertificateLifecycle.PersistAndActivateAsync(config, replacement, [],
                    services.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);

                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(thumbprint);
                config.SecurityConfiguration.ApplicationCertificates[0].Thumbprint.Should().Be(thumbprint);
                using CertificateCollection stored = await ApplicationCertificateLifecycle
                    .EnumerateApplicationCertificatesAsync(config.SecurityConfiguration.ApplicationCertificates[0], [],
                        services.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);
                stored.Should().ContainSingle().Which.Thumbprint.Should().Be(thumbprint);
                VerifyCertificateKey(old.Certificate);
                VerifyCertificateKey(active.Certificate);
            }
            ServiceProvider restarted = CreateServices(root);
            await using (restarted.ConfigureAwait(false))
            {
                var config = await restarted.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                    .GetAsync(CancellationToken.None).ConfigureAwait(false);
                using CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                    ObjectTypeIds.RsaSha256ApplicationCertificateType);
                active.Certificate.Thumbprint.Should().Be(thumbprint);
                VerifyCertificateKey(active.Certificate);
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

    private static Certificate CreateApplicationReplacement(Certificate issuer = null, RSA existingKey = null,
        bool expired = false, string applicationUri = ApplicationUri, string subjectSuffix = "",
        string domainName = "localhost", string applicationName = ApplicationName)
    {
        using RSA generatedKey = existingKey is null ? RSA.Create(2048) : null;
        RSA key = existingKey ?? generatedKey;
        var request = new CertificateRequest("CN=" + applicationName + subjectSuffix, key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature |
            X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1"), new("1.3.6.1.5.5.7.3.2") }, false));
        var names = new SubjectAlternativeNameBuilder();
        names.AddUri(new Uri(applicationUri));
        names.AddDnsName("localhost");
        if (!string.Equals(domainName, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            names.AddDnsName(domainName);
        }
        request.CertificateExtensions.Add(names.Build());
        if (issuer is not null)
        {
            using X509Certificate2 issuerPublic = X509CertificateLoader.LoadCertificate(issuer.RawData);
            request.CertificateExtensions.Add(System.Security.Cryptography.X509Certificates
                .X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    issuerPublic, includeKeyIdentifier: true, includeIssuerAndSerial: true));
            using RSA signingKey = issuer.GetRSAPrivateKey();
            using X509Certificate2 signed = request.Create(issuer.SubjectName,
                X509SignatureGenerator.CreateForRSA(signingKey, RSASignaturePadding.Pkcs1),
                DateTimeOffset.UtcNow.AddHours(-12), DateTimeOffset.UtcNow.AddHours(expired ? -1 : 24),
                RandomNumberGenerator.GetBytes(16));
            return Certificate.From(signed.CopyWithPrivateKey(key));
        }
        return Certificate.From(request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static void VerifyCertificateKey(Certificate certificate)
    {
        using RSA privateKey = certificate.GetRSAPrivateKey();
        using RSA publicKey = certificate.GetRSAPublicKey();
        byte[] data = [4, 5, 6];
        byte[] signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CrlReplacement_RejectsWrongKeyWithoutChangingStoreAsync(bool sameSubject)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-crl-reject-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var config = await services.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                    .GetAsync(CancellationToken.None).ConfigureAwait(false);
                ICertificateManager manager = config.CertificateManager;
                var operations = new CertificateTrustListOperations(manager, Mock.Of<ILogger>());
                using Certificate signer = CreateCrlSigner("expected-signer");
                using Certificate impostor = CreateCrlSigner(sameSubject ? "expected-signer" : "unknown-signer");
                X509CRL original = CreateCrl(signer, "01");
                using (ICertificateStore store = manager.OpenTrustedStore(TrustListIdentifier.Peers))
                {
                    await store.AddAsync(signer).ConfigureAwait(false);
                    await store.AddCRLAsync(original).ConfigureAwait(false);
                }

                (await operations.ReplaceCrlAsync(manager, Convert.ToBase64String(CreateCrl(impostor, "02").RawData),
                    null).ConfigureAwait(false)).Should().BeFalse();
                using ICertificateStore unchanged = manager.OpenTrustedStore(TrustListIdentifier.Peers);
                (await unchanged.EnumerateCRLsAsync().ConfigureAwait(false)).Should().ContainSingle()
                    .Which.RawData.Should().Equal(original.RawData);
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
    public async Task CrlReplacement_InvalidatesPreviouslyAcceptedCertificateAsync(bool issuerStore)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-crl-cache-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                var config = await services.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                    .GetAsync(CancellationToken.None).ConfigureAwait(false);
                ICertificateManager manager = config.CertificateManager;
                var operations = new CertificateTrustListOperations(manager, Mock.Of<ILogger>());
                using Certificate signer = CreateCrlSigner("revocation-root");
                using RSA leafKey = RSA.Create(2048);
                var request = new CertificateRequest("CN=RevocablePeer", leafKey, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                using X509Certificate2 issuerPublic = X509CertificateLoader.LoadCertificate(signer.RawData);
                request.CertificateExtensions.Add(System.Security.Cryptography.X509Certificates
                    .X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                        issuerPublic, includeKeyIdentifier: true, includeIssuerAndSerial: true));
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, true));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                    new OidCollection { new("1.3.6.1.5.5.7.3.1"), new("1.3.6.1.5.5.7.3.2") }, false));
                using RSA signingKey = signer.GetRSAPrivateKey();
                using Certificate leaf = Certificate.From(request.Create(signer.SubjectName,
                    X509SignatureGenerator.CreateForRSA(signingKey, RSASignaturePadding.Pkcs1),
                    DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(1), [0x12, 0x34]));
                (await operations.AddAsync([Convert.ToBase64String(signer.RawData)], null,
                    TrustListIdentifier.Peers, issuerStore).ConfigureAwait(false)).Should().BeTrue();
                (await operations.AddAsync([Convert.ToBase64String(leaf.RawData)], null,
                    TrustListIdentifier.Peers, false).ConfigureAwait(false)).Should().BeTrue();
                (await operations.ReplaceCrlAsync(manager, Convert.ToBase64String(CreateCrl(signer, "01").RawData),
                    null).ConfigureAwait(false)).Should().BeTrue();
                var accepted = await manager.ValidateAsync(leaf, TrustListIdentifier.Peers).ConfigureAwait(false);
                accepted.IsValid.Should().BeTrue("{0}", accepted.StatusCode);

                string file = Path.Combine(root, "revoked.crl");
                await File.WriteAllBytesAsync(file, CreateCrl(signer, leaf.SerialNumber).RawData).ConfigureAwait(false);
                (await operations.ReplaceCrlAsync(manager, "ignored-file-takes-precedence", file).ConfigureAwait(false))
                    .Should().BeTrue();

                var revoked = await manager.ValidateAsync(leaf, TrustListIdentifier.Peers).ConfigureAwait(false);
                revoked.IsValid.Should().BeFalse();
                revoked.StatusCode.Should().Be((StatusCode)StatusCodes.BadCertificateRevoked);
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
    public async Task CrlReplacement_PreservesDestinationAndUnrelatedDataAsync(bool issuerStore)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-crl-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                ICertificateManager manager = configuration.CertificateManager;
                var operations = new CertificateTrustListOperations(manager, Mock.Of<ILogger>());
                using Certificate signer = CreateCrlSigner("crl-signer");
                using Certificate unrelatedSigner = CreateCrlSigner("unrelated-signer");
                X509CRL oldCrl = CreateCrl(signer, "01");
                X509CRL unrelated = CreateCrl(unrelatedSigner, "02");
                X509CRL replacement = CreateCrl(signer, "03");
                (await operations.AddAsync([Convert.ToBase64String(signer.RawData)], null,
                    TrustListIdentifier.Peers, issuerStore).ConfigureAwait(false)).Should().BeTrue();
                using (ICertificateStore store = issuerStore
                    ? manager.OpenIssuerStore(TrustListIdentifier.Peers)
                    : manager.OpenTrustedStore(TrustListIdentifier.Peers))
                {
                    await store.AddAsync(unrelatedSigner).ConfigureAwait(false);
                    await store.AddCRLAsync(oldCrl).ConfigureAwait(false);
                    await store.AddCRLAsync(unrelated).ConfigureAwait(false);
                }

                (await operations.ReplaceCrlAsync(manager, Convert.ToBase64String(replacement.RawData), null)
                    .ConfigureAwait(false)).Should().BeTrue();

                using ICertificateStore selected = issuerStore
                    ? manager.OpenIssuerStore(TrustListIdentifier.Peers)
                    : manager.OpenTrustedStore(TrustListIdentifier.Peers);
                var crls = await selected.EnumerateCRLsAsync().ConfigureAwait(false);
                crls.Count.Should().Be(2);
                crls.Should().Contain(crl => crl.RawData.SequenceEqual(replacement.RawData));
                crls.Should().Contain(crl => crl.RawData.SequenceEqual(unrelated.RawData));
                using CertificateCollection certificates = await selected.EnumerateAsync().ConfigureAwait(false);
                certificates.Count.Should().Be(2);
                using ICertificateStore other = issuerStore
                    ? manager.OpenTrustedStore(TrustListIdentifier.Peers)
                    : manager.OpenIssuerStore(TrustListIdentifier.Peers);
                (await other.EnumerateCRLsAsync().ConfigureAwait(false)).Should().BeEmpty();
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

    private static Certificate CreateCrlSigner(string commonName)
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=" + commonName, key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        return Certificate.From(request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30)));
    }

    private static X509CRL CreateCrl(Certificate signer, string serialNumber)
    {
        return new X509CRL(CrlBuilder.Create(signer.SubjectName, HashAlgorithmName.SHA256)
            .SetThisUpdate(DateTime.UtcNow.AddMinutes(-1))
            .SetNextUpdate(DateTime.UtcNow.AddDays(1))
            .AddRevokedSerialNumbers([serialNumber], CRLReason.KeyCompromise)
            .CreateForRSA(signer).RawData);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReverseConnectConfiguration_RemainsDisabledWithoutClients(bool nullClients)
    {
        var config = new OpcApplicationConfiguration
        {
            ReverseConnectClientUrls = nullClients ? null : []
        };

        config.CreateReverseConnectConfiguration().Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(7)]
    public void ReverseConnectConfiguration_PreservesSettingsAndClientOrder(int maxSessions)
    {
        var config = new OpcApplicationConfiguration
        {
            ReverseConnectClientUrls = ["opc.tcp://localhost:51001/a", "opc.tcp://localhost:51002/b"],
            ReverseConnectInterval = 1500,
            ReverseConnectTimeout = 2700,
            ReverseConnectRejectTimeout = 3900,
            ReverseConnectMaxSessionCount = maxSessions
        };

        ReverseConnectServerConfiguration result = config.CreateReverseConnectConfiguration();

        result.ConnectInterval.Should().Be(1500);
        result.ConnectTimeout.Should().Be(2700);
        result.RejectTimeout.Should().Be(3900);
        result.Clients.Count.Should().Be(2);
        result.Clients[0].EndpointUrl.Should().Be("opc.tcp://localhost:51001/a");
        result.Clients[1].EndpointUrl.Should().Be("opc.tcp://localhost:51002/b");
        result.Clients.ToArray().Should().OnlyContain(client => client.Enabled &&
            client.Timeout == 2700 && client.MaxSessionCount == maxSessions);
        config.ReverseConnectClientUrls.Clear();
        result.Clients.Count.Should().Be(2);
    }

    [TestCase("null")]
    [TestCase("empty")]
    [TestCase("invalid-base64")]
    [TestCase("invalid-certificate")]
    [TestCase("missing-file")]
    [TestCase("cancelled")]
    public async Task TrustMutation_InvalidInputDoesNotWriteCertificatesAsync(string kind)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-trust-invalid-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                ICertificateManager manager = configuration.CertificateManager;
                var operations = new CertificateTrustListOperations(manager,
                    services.GetRequiredService<ILogger<CertificateTrustListOperations>>());
                using Certificate certificate = CreatePeerCertificate(expired: false);
                string valid = Convert.ToBase64String(certificate.RawData);
                if (kind == "cancelled")
                {
                    Func<Task> add = () => operations.AddAsync([valid], null, TrustListIdentifier.Peers,
                        issuerCertificate: false, new CancellationToken(true));
                    await add.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                    Func<Task> remove = () => operations.RemovePeerCertificatesAsync(
                        [certificate.Thumbprint], new CancellationToken(true));
                    await remove.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
                else
                {
                    bool result = kind switch
                    {
                        "null" => await operations.AddAsync(null, null, TrustListIdentifier.Peers, false)
                            .ConfigureAwait(false),
                        "empty" => await operations.AddAsync([], [], TrustListIdentifier.Peers, false)
                            .ConfigureAwait(false),
                        "invalid-base64" => await operations.AddAsync([valid, "!"], null,
                            TrustListIdentifier.Peers, false).ConfigureAwait(false),
                        "invalid-certificate" => await operations.AddAsync([valid, "AQID"], null,
                            TrustListIdentifier.Peers, false).ConfigureAwait(false),
                        _ => await operations.AddAsync([valid], [Path.Combine(root, "missing.der")],
                            TrustListIdentifier.Peers, false).ConfigureAwait(false)
                    };
                    result.Should().BeFalse();
                }
                using ICertificateStore store = manager.OpenTrustedStore(TrustListIdentifier.Peers);
                using CertificateCollection certificates = await store.EnumerateAsync().ConfigureAwait(false);
                certificates.Should().BeEmpty();
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
    public async Task TrustMutation_FailedCommitDoesNotReportSuccessAsync(bool remove)
    {
        var transaction = new Mock<ITrustListTransaction>();
        transaction.Setup(update => update.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Injected persistence failure"));
        var manager = new Mock<ICertificateTrustListManager>();
        manager.Setup(registry => registry.BeginUpdateAsync(TrustListIdentifier.Peers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        var store = new Mock<ICertificateStore>();
        store.Setup(instance => instance.FindByThumbprintAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CertificateCollection());
        manager.Setup(registry => registry.OpenTrustedStore(TrustListIdentifier.Peers)).Returns(store.Object);
        var operations = new CertificateTrustListOperations(manager.Object, Mock.Of<ILogger>());
        using Certificate certificate = CreatePeerCertificate(expired: false);

        bool result = remove
            ? await operations.RemovePeerCertificatesAsync([certificate.Thumbprint]).ConfigureAwait(false)
            : await operations.AddAsync([Convert.ToBase64String(certificate.RawData)], null,
                TrustListIdentifier.Peers, false).ConfigureAwait(false);

        result.Should().BeFalse();
        transaction.Verify(update => update.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(update => update.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TrustMutation_RemainingCertificateDoesNotReportSuccessfulRemovalAsync()
    {
        using Certificate certificate = CreatePeerCertificate(expired: false);
        var transaction = new Mock<ITrustListTransaction>();
        var store = new Mock<ICertificateStore>();
        store.Setup(instance => instance.FindByThumbprintAsync(certificate.Thumbprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var found = new CertificateCollection();
                found.Add(certificate);
                return found;
            });
        var manager = new Mock<ICertificateTrustListManager>();
        manager.Setup(registry => registry.BeginUpdateAsync(TrustListIdentifier.Peers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        manager.Setup(registry => registry.OpenTrustedStore(TrustListIdentifier.Peers)).Returns(store.Object);
        var operations = new CertificateTrustListOperations(manager.Object, Mock.Of<ILogger>());

        (await operations.RemovePeerCertificatesAsync([certificate.Thumbprint]).ConfigureAwait(false))
            .Should().BeFalse();
        transaction.Verify(update => update.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        using RSA key = certificate.GetRSAPublicKey();
        key.KeySize.Should().Be(2048);
    }

    [Test]
    public async Task TrustMutation_RemovesPeersAndIssuersButPreservesUserTrustAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-trust-remove-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                ICertificateManager manager = configuration.CertificateManager;
                var operations = new CertificateTrustListOperations(manager,
                    services.GetRequiredService<ILogger<CertificateTrustListOperations>>());
                using Certificate certificate = CreatePeerCertificate(expired: false);
                string base64 = Convert.ToBase64String(certificate.RawData);
                foreach (TrustListIdentifier scope in new[] { TrustListIdentifier.Peers, TrustListIdentifier.Users })
                {
                    foreach (bool issuer in new[] { false, true })
                    {
                        (await operations.AddAsync([base64], null, scope, issuer).ConfigureAwait(false))
                            .Should().BeTrue();
                    }
                    (await manager.ValidateAsync(certificate, scope).ConfigureAwait(false)).IsValid.Should().BeTrue();
                }

                (await operations.RemovePeerCertificatesAsync([]).ConfigureAwait(false)).Should().BeFalse();
                (await operations.RemovePeerCertificatesAsync([certificate.Thumbprint, certificate.Thumbprint])
                    .ConfigureAwait(false)).Should().BeTrue();
                (await operations.RemovePeerCertificatesAsync([certificate.Thumbprint]).ConfigureAwait(false))
                    .Should().BeTrue();

                foreach (TrustListIdentifier scope in new[] { TrustListIdentifier.Peers, TrustListIdentifier.Users })
                {
                    foreach (bool issuer in new[] { false, true })
                    {
                        using ICertificateStore store = issuer
                            ? manager.OpenIssuerStore(scope) : manager.OpenTrustedStore(scope);
                        using CertificateCollection found = await store.EnumerateAsync().ConfigureAwait(false);
                        found.Count.Should().Be(scope == TrustListIdentifier.Users ? 1 : 0);
                    }
                }
                (await manager.ValidateAsync(certificate, TrustListIdentifier.Peers).ConfigureAwait(false))
                    .StatusCode.Should().Be((StatusCode)StatusCodes.BadCertificateUntrusted);
                (await manager.ValidateAsync(certificate, TrustListIdentifier.Users).ConfigureAwait(false))
                    .IsValid.Should().BeTrue();
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

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task TrustMutation_AddsOnlyToSelectedStoreAndAcceptsDuplicatesAsync(bool users, bool issuer)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-trust-add-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(root);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                ICertificateManager manager = configuration.CertificateManager;
                var operations = new CertificateTrustListOperations(manager,
                    services.GetRequiredService<ILogger<CertificateTrustListOperations>>());
                using Certificate certificate = CreatePeerCertificate(expired: false);
                TrustListIdentifier selected = users ? TrustListIdentifier.Users : TrustListIdentifier.Peers;
                (await manager.ValidateAsync(certificate, selected).ConfigureAwait(false)).IsValid.Should().BeFalse();
                string base64 = Convert.ToBase64String(certificate.RawData);
                string file = Path.Combine(root, users ? "input.pem" : "input.der");
                Directory.CreateDirectory(root);
                if (users)
                {
                    await File.WriteAllTextAsync(file, PemEncoding.WriteString("CERTIFICATE", certificate.RawData))
                        .ConfigureAwait(false);
                }
                else
                {
                    await File.WriteAllBytesAsync(file, certificate.RawData).ConfigureAwait(false);
                }

                (await operations.AddAsync([base64, base64], [file], selected, issuer).ConfigureAwait(false))
                    .Should().BeTrue();
                (await operations.AddAsync([base64], null, selected, issuer).ConfigureAwait(false)).Should().BeTrue();

                foreach (TrustListIdentifier scope in new[] { TrustListIdentifier.Peers, TrustListIdentifier.Users })
                {
                    foreach (bool issuerStore in new[] { false, true })
                    {
                        using ICertificateStore store = issuerStore
                            ? manager.OpenIssuerStore(scope) : manager.OpenTrustedStore(scope);
                        using CertificateCollection found = await store.EnumerateAsync().ConfigureAwait(false);
                        if (scope == selected && issuerStore == issuer)
                        {
                            found.Should().ContainSingle().Which.Thumbprint.Should().Be(certificate.Thumbprint);
                        }
                        else
                        {
                            found.Should().BeEmpty();
                        }
                    }
                }
                if (!issuer)
                {
                    (await manager.ValidateAsync(certificate, selected).ConfigureAwait(false)).IsValid
                        .Should().BeTrue();
                }
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

    [TestCase(CertificateStoreType.Directory, "")]
    [TestCase(CertificateStoreType.X509Store, "")]
    [TestCase(FlatDirectoryCertificateStore.StoreTypeName, FlatDirectoryCertificateStore.StoreTypePrefix)]
    [TestCase(KubernetesSecretCertificateStore.StoreTypeName, KubernetesSecretCertificateStore.StoreTypePrefix)]
    public void PlcSecurity_PreservesStoreTypesAndIndependentPaths(string storeType, string prefix)
    {
        var config = new OpcPlcConfiguration();
        config.OpcUa.OpcOwnCertStoreType = storeType;
        config.OpcUa.OpcOwnCertStorePath = "custom-own";
        config.OpcUa.OpcIssuerCertStorePath = "custom-issuer";
        config.OpcUa.OpcTrustedCertStorePath = "custom-peer";
        config.OpcUa.OpcTrustedUserCertStorePath = "custom-user";
        config.OpcUa.OpcUserIssuerCertStorePath = "custom-user-issuer";
        config.OpcUa.OpcRejectedCertStorePath = "custom-rejected";
        var security = new SecurityConfiguration
        {
            TrustedIssuerCertificates = new CertificateTrustList(),
            TrustedPeerCertificates = new CertificateTrustList(),
            TrustedUserCertificates = new CertificateTrustList(),
            UserIssuerCertificates = new CertificateTrustList(),
            RejectedCertificateStore = new CertificateStoreIdentifier()
        };

        PlcSecurityConfiguration.ConfigureStores(security, config);

        security.ApplicationCertificates.Count.Should().Be(1);
        CertificateIdentifier application = security.ApplicationCertificates[0];
        application.StoreType.Should().Be(storeType);
        application.StorePath.Should().Be(prefix + "custom-own");
        application.SubjectName.Should().Be("CN=" + config.ProgramName);
        application.CertificateType.Should().Be(ObjectTypeIds.RsaSha256ApplicationCertificateType);
        string trustType = prefix.Length == 0 ? CertificateStoreType.Directory : storeType;
        CertificateStoreIdentifier[] stores = [security.TrustedIssuerCertificates, security.TrustedPeerCertificates,
            security.TrustedUserCertificates, security.UserIssuerCertificates, security.RejectedCertificateStore];
        stores.Should().OnlyContain(store => store.StoreType == trustType);
        security.TrustedIssuerCertificates.StorePath.Should().Be(prefix + "custom-issuer");
        security.TrustedPeerCertificates.StorePath.Should().Be(prefix + "custom-peer");
        security.TrustedUserCertificates.StorePath.Should().Be(prefix + "custom-user");
        security.UserIssuerCertificates.StorePath.Should().Be(prefix + "custom-user-issuer");
        security.RejectedCertificateStore.StorePath.Should().Be(prefix + "custom-rejected");
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task PlcSecurity_PreservesCliPolicyInDirectAndDiBuildersAsync(bool useDi, bool enabled)
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-policy-" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = CreatePlcConfiguration(pkiRoot, enabled);
            config.OpcUa.RejectSHA1SignedCertificates = enabled;
            config.OpcUa.DontRejectUnknownRevocationStatus = enabled;
            config.OpcUa.TrustMyself = enabled;
            config.OpcUa.MinimumCertificateKeySize = 3072;
            var collection = new ServiceCollection();
            ConfigureServices(collection, pkiRoot, enabled, "opc.tcp://localhost:4840/di-security-probe", config);
            ServiceProvider services = collection.BuildServiceProvider();
            await using (services.ConfigureAwait(false))
            {
                var directApplication = new ApplicationInstance(services.GetRequiredService<ITelemetryContext>())
                {
                    ApplicationName = config.ProgramName,
                    ApplicationType = ApplicationType.Server
                };
                await using (directApplication.ConfigureAwait(false))
                {
                    ApplicationConfiguration configuration;
                    if (useDi)
                    {
                        configuration = await services.GetRequiredService<IOpcUaApplicationConfigurationProvider>()
                            .GetAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        var builder = directApplication.Build(ApplicationUri, config.OpcUa.ProductUri)
                            .AsServer(["opc.tcp://localhost:4840/di-security-probe"])
                            .AddSignAndEncryptPolicies()
                            .AddSecurityConfiguration(PlcSecurityConfiguration.CreateApplicationCertificates(config),
                                pkiRoot);
                        PlcSecurityConfiguration.Configure(builder, config);
                        PlcSecurityConfiguration.ConfigureStores(
                            directApplication.ApplicationConfiguration.SecurityConfiguration, config);
                        configuration = await builder.CreateAsync(CancellationToken.None).ConfigureAwait(false);
                    }

                    SecurityConfiguration security = configuration.SecurityConfiguration;
                    security.AutoAcceptUntrustedCertificates.Should().Be(enabled);
                    security.RejectSHA1SignedCertificates.Should().Be(enabled);
                    security.RejectUnknownRevocationStatus.Should().Be(!enabled);
                    security.AddAppCertToTrustedStore.Should().Be(enabled);
                    security.MinimumCertificateKeySize.Should().Be(3072);
                    security.ApplicationCertificates.Count.Should().Be(1);
                    security.ApplicationCertificates[0].StorePath.Should().Be(config.OpcUa.OpcOwnCertStorePath);
                    security.ApplicationCertificates[0].StoreType.Should().Be(CertificateStoreType.Directory);
                    security.ApplicationCertificates[0].CertificateType.Should()
                        .Be(ObjectTypeIds.RsaSha256ApplicationCertificateType);
                }
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [Test]
    public async Task SharedConfiguration_CreatesAndReusesDirectoryApplicationCertificateAsync()
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-di-security-" + Guid.NewGuid().ToString("N"));
        try
        {
            string thumbprint;
            ServiceProvider firstServices = CreateServices(pkiRoot);
            await using (firstServices.ConfigureAwait(false))
            {
                IOpcUaApplicationConfigurationProvider provider =
                    firstServices.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                ApplicationConfiguration configuration = await provider.GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                configuration.ApplicationName.Should().Be(ApplicationName);
                configuration.ApplicationUri.Should().Be(ApplicationUri);
                configuration.ProductUri.Should().Be(ProductUri);
                configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates.Should().BeFalse();
                configuration.SecurityConfiguration.RejectSHA1SignedCertificates.Should().BeTrue();
                configuration.SecurityConfiguration.MinimumCertificateKeySize.Should().Be(2048);
                configuration.SecurityConfiguration.RejectUnknownRevocationStatus.Should().BeTrue();
                configuration.SecurityConfiguration.MaxRejectedCertificates.Should().Be(10);
                configuration.ServerConfiguration.SecurityPolicies.Should().NotBeNull();
                configuration.ServerConfiguration.SecurityPolicies.ToArray().Should()
                    .NotBeEmpty().And.OnlyContain(policy => policy.SecurityMode == MessageSecurityMode.SignAndEncrypt);
                configuration.SecurityConfiguration.ApplicationCertificates.Count.Should().Be(1);
                configuration.SecurityConfiguration.ApplicationCertificates[0].StorePath.Should()
                    .Be(Path.Combine(pkiRoot, "own"));

                using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, customCertificateProvided: false, CancellationToken.None)
                    .ConfigureAwait(false);
                entry.Should().NotBeNull();
                entry.Certificate.HasPrivateKey.Should().BeTrue();
                thumbprint = entry.Certificate.Thumbprint;
                using CertificateCollection stored = await ApplicationCertificateLifecycle
                    .EnumerateApplicationCertificatesAsync(
                        configuration.SecurityConfiguration.ApplicationCertificates[0],
                        [], firstServices.GetRequiredService<ITelemetryContext>()).ConfigureAwait(false);
                stored.Should().ContainSingle().Which.Thumbprint.Should().Be(thumbprint);
                stored[0].HasPrivateKey.Should().BeFalse();
                (await provider.GetAsync(CancellationToken.None).ConfigureAwait(false)).Should()
                    .BeSameAs(configuration);
            }

            ServiceProvider secondServices = CreateServices(pkiRoot);
            await using (secondServices.ConfigureAwait(false))
            {
                IOpcUaApplicationConfigurationProvider provider =
                    secondServices.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                ApplicationConfiguration configuration = await provider.GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, customCertificateProvided: true, CancellationToken.None)
                    .ConfigureAwait(false);
                entry.Should().NotBeNull();
                entry.Certificate.Thumbprint.Should().Be(thumbprint);
                entry.Certificate.HasPrivateKey.Should().BeTrue();
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CertificateLifecycle_DoesNotGenerateForMissingCustomIdentityOrCancellationAsync(bool cancel)
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-no-generate-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(pkiRoot);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                ApplicationConfiguration configuration = await provider.GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Func<Task> acquire = async () =>
                {
                    using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                        provider.Application, customCertificateProvided: !cancel, new CancellationToken(cancel))
                        .ConfigureAwait(false);
                };
                if (cancel)
                {
                    await acquire.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                }
                else
                {
                    await acquire.Should().ThrowAsync<InvalidOperationException>()
                        .WithMessage("Custom application certificate was provided but could not be loaded.")
                        .ConfigureAwait(false);
                }

                using CertificateEntryCollection entries = configuration.CertificateManager
                    .SnapshotApplicationCertificates();
                entries.Should().BeEmpty();
                string ownPath = Path.Combine(pkiRoot, "own");
                if (Directory.Exists(ownPath))
                {
                    Directory.EnumerateFiles(ownPath, "*", SearchOption.AllDirectories).Should().BeEmpty();
                }
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [Test]
    public async Task CertificateLifecycle_RejectsPublicOnlyIdentityWithoutGeneratingAsync()
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

        Func<Task> acquire = async () =>
        {
            using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                application.Object, customCertificateProvided: true).ConfigureAwait(false);
        };
        await acquire.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application certificate has no private key.").ConfigureAwait(false);

        using RSA publicKey = original.Certificate.GetRSAPublicKey();
        publicKey.KeySize.Should().Be(2048);
        application.Verify(instance => instance.CheckApplicationInstanceCertificatesAsync(
            It.IsAny<bool>(), It.IsAny<ushort?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CertificateLifecycle_StopsWhenSdkCertificateCheckFailsAsync()
    {
        var application = new Mock<IApplicationInstance>(MockBehavior.Strict);
        application.Setup(instance => instance.CheckApplicationInstanceCertificatesAsync(
            true, CertificateFactory.DefaultLifeTime, CancellationToken.None)).ReturnsAsync(false);
        Func<Task> acquire = async () =>
        {
            using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                application.Object, customCertificateProvided: false).ConfigureAwait(false);
        };

        await acquire.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Application certificate invalid.").ConfigureAwait(false);
        application.VerifyGet(instance => instance.ApplicationConfiguration, Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CertificateLifecycle_CsrMatchesIdentityAndPreservesOwnedHandlesAsync(bool publicOnly)
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-csr-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(pkiRoot);
            await using (services.ConfigureAwait(false))
            {
                var provider = services.GetRequiredService<IOpcUaApplicationConfigurationProvider>();
                ApplicationConfiguration configuration = await provider.GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                using CertificateEntry entry = await ApplicationCertificateLifecycle.EnsureAndAcquireAsync(
                    provider.Application, customCertificateProvided: false).ConfigureAwait(false);
                using var publicCertificate = new Certificate(entry.Certificate.RawData);
                Certificate input = publicOnly ? publicCertificate : entry.Certificate;

                byte[] csr = ApplicationCertificateLifecycle.CreateSigningRequest(
                    input, configuration.CertificateManager);
                var request = CertificateRequest.LoadSigningRequest(csr, HashAlgorithmName.SHA256);
                request.SubjectName.RawData.Should().Equal(entry.Certificate.SubjectName.RawData);
                using RSA publicKey = entry.Certificate.GetRSAPublicKey();
                request.PublicKey.ExportSubjectPublicKeyInfo().Should().Equal(publicKey.ExportSubjectPublicKeyInfo());

                using Certificate unrelated = CreatePeerCertificate(expired: false);
                Action mismatch = () => ApplicationCertificateLifecycle.CreateSigningRequest(
                    unrelated, configuration.CertificateManager);
                mismatch.Should().Throw<InvalidOperationException>().WithMessage("No matching application*");
                input.RawData.Should().Equal(publicCertificate.RawData);
                using CertificateEntry reacquired = configuration.CertificateManager
                    .AcquireApplicationCertificateByType(ObjectTypeIds.RsaSha256ApplicationCertificateType);
                entry.Dispose();
                using RSA privateKey = reacquired.Certificate.GetRSAPrivateKey();
                byte[] data = [1, 2, 3];
                byte[] signature = privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                    .Should().BeTrue();
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [Test]
    public async Task SharedConfiguration_RejectsUnknownPeerAndSeparatesPeerAndUserTrustAsync()
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-di-trust-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(pkiRoot);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                ICertificateManager manager = configuration.CertificateManager;
                using Certificate peer = CreatePeerCertificate(expired: false);

                var rejected = await manager.ValidateAsync(peer, TrustListIdentifier.Peers, CancellationToken.None)
                    .ConfigureAwait(false);
                rejected.IsValid.Should().BeFalse();
                rejected.StatusCode.Should().Be((StatusCode)StatusCodes.BadCertificateUntrusted);
                await manager.FlushRejectedAsync(CancellationToken.None).ConfigureAwait(false);
                using (ICertificateStore store = manager.OpenTrustedStore(TrustListIdentifier.Rejected))
                using (CertificateCollection certificates = await store.EnumerateAsync(CancellationToken.None)
                    .ConfigureAwait(false))
                {
                    certificates.Should().Contain(certificate => certificate.Thumbprint == peer.Thumbprint);
                }

                await TrustCertificateAsync(manager, TrustListIdentifier.Peers, peer).ConfigureAwait(false);
                var trustedPeer = await manager.ValidateAsync(peer, TrustListIdentifier.Peers, CancellationToken.None)
                    .ConfigureAwait(false);
                trustedPeer.IsValid.Should().BeTrue("{0}", trustedPeer.StatusCode);
                var untrustedUser = await manager.ValidateAsync(peer, TrustListIdentifier.Users, CancellationToken.None)
                    .ConfigureAwait(false);
                untrustedUser.IsValid.Should().BeFalse();
                untrustedUser.StatusCode.Should().Be((StatusCode)StatusCodes.BadCertificateUntrusted);

                await TrustCertificateAsync(manager, TrustListIdentifier.Users, peer).ConfigureAwait(false);
                var trustedUser = await manager.ValidateAsync(peer, TrustListIdentifier.Users, CancellationToken.None)
                    .ConfigureAwait(false);
                trustedUser.IsValid.Should().BeTrue("{0}", trustedUser.StatusCode);
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SharedConfiguration_AutoAcceptDoesNotBypassExpiredCertificateAsync(bool autoAccept)
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-di-expiry-" + Guid.NewGuid().ToString("N"));
        try
        {
            ServiceProvider services = CreateServices(pkiRoot, autoAccept);
            await using (services.ConfigureAwait(false))
            {
                ApplicationConfiguration configuration = await services
                    .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                using Certificate valid = CreatePeerCertificate(expired: false);
                using Certificate expired = CreatePeerCertificate(expired: true);
                var validResult = await configuration.CertificateManager.ValidateAsync(
                    valid, TrustListIdentifier.Peers, CancellationToken.None).ConfigureAwait(false);
                validResult.IsValid.Should().Be(autoAccept);
                var expiredResult = await configuration.CertificateManager.ValidateAsync(
                    expired, TrustListIdentifier.Peers, CancellationToken.None).ConfigureAwait(false);
                expiredResult.IsValid.Should().BeFalse();
                expiredResult.StatusCode.Should().Be((StatusCode)(autoAccept
                    ? StatusCodes.BadCertificateTimeInvalid : StatusCodes.BadCertificateUntrusted));
                await TrustCertificateAsync(configuration.CertificateManager, TrustListIdentifier.Peers, expired)
                    .ConfigureAwait(false);
                var trustedExpiredResult = await configuration.CertificateManager.ValidateAsync(
                    expired, TrustListIdentifier.Peers, CancellationToken.None).ConfigureAwait(false);
                trustedExpiredResult.IsValid.Should().BeFalse();
                trustedExpiredResult.StatusCode.Should().Be((StatusCode)StatusCodes.BadCertificateTimeInvalid);
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    [Test]
    public async Task HostedServer_StartsAndStopsWithPersistedDirectoryIdentityAsync()
    {
        string pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-di-host-" + Guid.NewGuid().ToString("N"));
        try
        {
            int port;
            using (var reservation = new TcpListener(IPAddress.Loopback, 0))
            {
                reservation.Start();
                port = ((IPEndPoint)reservation.LocalEndpoint).Port;
            }

            string thumbprint = null;
            for (int restart = 0; restart < 2; restart++)
            {
                var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
                {
                    DisableDefaults = true
                });
                builder.Logging.AddConsole();
                ConfigureServices(builder.Services, pkiRoot, false,
                    $"opc.tcp://localhost:{port}/di-security-probe");
                var observer = new ServerStartedObserver();
                builder.Services.AddSingleton<IServerStartupTask>(observer);
                IHost host = builder.Build();
                await using (((IAsyncDisposable)host).ConfigureAwait(false))
                {
                    try
                    {
                        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        await host.StartAsync(deadline.Token).ConfigureAwait(false);
                        await observer.Started.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
                        using (var connection = new TcpClient())
                        {
                            await connection.ConnectAsync(IPAddress.Loopback, port, deadline.Token)
                                .ConfigureAwait(false);
                            connection.Connected.Should().BeTrue();
                        }

                        ApplicationConfiguration configuration = await host.Services
                            .GetRequiredService<IOpcUaApplicationConfigurationProvider>().GetAsync(deadline.Token)
                            .ConfigureAwait(false);
                        using CertificateEntry entry = configuration.CertificateManager
                            .AcquireApplicationCertificateByType(ObjectTypeIds.RsaSha256ApplicationCertificateType);
                        entry.Should().NotBeNull();
                        entry.Certificate.HasPrivateKey.Should().BeTrue();
                        thumbprint ??= entry.Certificate.Thumbprint;
                        entry.Certificate.Thumbprint.Should().Be(thumbprint);
                    }
                    finally
                    {
                        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        await host.StopAsync(shutdown.Token).ConfigureAwait(false);
                    }

                    using var releasedPort = new TcpListener(IPAddress.Loopback, port);
                    releasedPort.Server.ExclusiveAddressUse = true;
                    releasedPort.Start();
                }
            }
        }
        finally
        {
            if (Directory.Exists(pkiRoot))
            {
                Directory.Delete(pkiRoot, recursive: true);
            }
        }
    }

    private sealed class ServerStartedObserver : IServerStartupTask
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask OnServerStartedAsync(IServerContext server, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Started.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

    private static async Task TrustCertificateAsync(
        ICertificateTrustListManager manager, TrustListIdentifier trustList, Certificate certificate)
    {
        ITrustListTransaction transaction = await manager.BeginUpdateAsync(trustList, CancellationToken.None)
            .ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            await transaction.AddTrustedCertificateAsync(certificate, CancellationToken.None).ConfigureAwait(false);
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static Certificate CreatePeerCertificate(bool expired)
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=OpcPlcDiPeer", key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature |
            X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
            true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1"), new("1.3.6.1.5.5.7.3.2") }, false));
        var names = new SubjectAlternativeNameBuilder();
        names.AddUri(new Uri("urn:localhost:OpcPlcDiPeer"));
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(expired ? -1 : 30));
        return new Certificate(certificate.Export(X509ContentType.Cert));
    }

    private static ServiceProvider CreateServices(string pkiRoot, bool autoAccept = false)
    {
        var services = new ServiceCollection();
        ConfigureServices(services, pkiRoot, autoAccept, "opc.tcp://localhost:4840/di-security-probe");
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(
        IServiceCollection services, string pkiRoot, bool autoAccept, string endpointUrl,
        OpcPlcConfiguration plcConfig = null)
    {
        plcConfig ??= CreatePlcConfiguration(pkiRoot, autoAccept);
        services.AddLogging();
        services.AddOpcUa()
            .ConfigureApplication(options =>
            {
                options.ApplicationName = ApplicationName;
                options.ApplicationUri = ApplicationUri;
                options.ProductUri = ProductUri;
                options.SubjectName = "CN=" + ApplicationName;
                options.PkiRoot = pkiRoot;
                options.AutoAcceptUntrustedCertificates = autoAccept;
                options.RejectSHA1SignedCertificates = true;
                options.MinimumCertificateKeySize = 2048;
                options.ConfigureSecurity = security => PlcSecurityConfiguration.Configure(security, plcConfig)
                    .SetMaxRejectedCertificates(10);
            })
            .AddServer(options =>
            {
                options.EndpointUrls.Add(endpointUrl);
                options.IncludeSignAndEncryptPolicies = true;
                options.IncludeUnsecurePolicyNone = false;
                options.IncludeEccPolicies = false;
                options.UserTokenPolicies.Add(new OpcUaUserTokenPolicy { TokenType = UserTokenType.Anonymous });
            })
            .AddDefaultIdentityAuthenticators(options =>
            {
                options.EnableAnonymous = true;
                options.EnableUserNamePassword = false;
                options.EnableX509 = false;
                options.EnableJwt = false;
            });
    }

    private static OpcPlcConfiguration CreatePlcConfiguration(string pkiRoot, bool autoAccept)
    {
        var config = new OpcPlcConfiguration();
        config.OpcUa.AutoAcceptCerts = autoAccept;
        config.OpcUa.RejectSHA1SignedCertificates = true;
        config.OpcUa.OpcOwnCertStorePath = Path.Combine(pkiRoot, "own");
        config.OpcUa.OpcIssuerCertStorePath = Path.Combine(pkiRoot, "issuer");
        config.OpcUa.OpcTrustedCertStorePath = Path.Combine(pkiRoot, "trusted");
        config.OpcUa.OpcTrustedUserCertStorePath = Path.Combine(pkiRoot, "trusted-user");
        config.OpcUa.OpcUserIssuerCertStorePath = Path.Combine(pkiRoot, "issuer-user");
        config.OpcUa.OpcRejectedCertStorePath = Path.Combine(pkiRoot, "rejected");
        return config;
    }
}
