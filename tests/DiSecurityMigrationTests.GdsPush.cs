namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Gds.Client;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class DiSecurityMigrationTests
{
    [TestCase("Directory", false)]
    [TestCase("Directory", true)]
    [TestCase("FlatDirectory", false)]
    [TestCase("FlatDirectory", true)]
    [TestCase("KubernetesSecret", false)]
    [TestCase("KubernetesSecret", true)]
    public async Task GdsPush_IssuedCertificateAndPendingKeySurviveRestartAsync(string storeType, bool useCsr)
    {
        string root = Path.Combine(Path.GetTempPath(), "opcplc-push-" + Guid.NewGuid().ToString("N"));
        string clientRoot = root + "-client";
        var secretClient = new KubernetesSecretCertificateStoreTests.InMemoryKubernetesSecretStoreClient();
        var secrets = new Mock<IKubernetesSecretStoreClientFactory>();
        secrets.Setup(factory => factory.Create()).Returns(secretClient);
        int port;
        using (var reservation = new TcpListener(IPAddress.Loopback, 0))
        {
            reservation.Start();
            port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        }
        using Certificate issuer = CreateCrlSigner("gds-test-root");
        Certificate issued = null;
        string originalThumbprint = null;
        try
        {
            for (int phase = 0; phase < 3; phase++)
            {
                var plc = CreatePlcConfiguration(root, true);
                plc.AdminUser = "gds-admin";
                plc.AdminPassword = "gds-test-password";
                plc.OpcUa.Hostname = "localhost";
                plc.OpcUa.ServerPort = (ushort)port;
                plc.OpcUa.ServerPath = "/gds-push";
                plc.OpcUa.OpcOwnCertStoreType = storeType;
                plc.OpcUa.OpcKubernetesSecretNamespace = "gds-tests";
                var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
                {
                    DisableDefaults = true
                });
                builder.Logging.AddConsole();
                builder.Services.AddOpcUa().AddServer(_ => { }).AddDefaultIdentityAuthenticators(options =>
                {
                    options.EnableAnonymous = false;
                    options.EnableUserNamePassword = false;
                    options.EnableX509 = false;
                    options.EnableJwt = false;
                });
                var factory = new GdsTestServerFactory(plc);
                builder.Services.AddSingleton<IOpcUaServerFactory>(factory);
                builder.Services.AddSingleton(new OpcUaServerIdentityAuthenticatorRegistration(
                    (_, _) => factory.Server.CreateUserIdentityAuthenticators()));
                builder.Services.AddSingleton<IOpcUaApplicationConfigurationProvider>(services =>
                    new PlcApplicationConfigurationProvider(new OpcUaAppConfigFactory(plc,
                        services.GetRequiredService<ILogger<OpcUaAppConfigFactory>>(),
                        services.GetRequiredService<ILoggerFactory>(),
                        services.GetRequiredService<ITelemetryContext>(), secrets.Object)));
                var observer = new ServerStartedObserver();
                builder.Services.AddSingleton<IServerStartupTask>(observer);
                IHost host = builder.Build();
                await using (((IAsyncDisposable)host).ConfigureAwait(false))
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        await host.StartAsync(timeout.Token).ConfigureAwait(false);
                        await observer.Started.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
                        ApplicationConfiguration config = plc.OpcUa.ApplicationConfiguration;
                        using (CertificateEntry active = config.CertificateManager.AcquireApplicationCertificateByType(
                            ObjectTypeIds.RsaSha256ApplicationCertificateType))
                        {
                            originalThumbprint ??= active.Certificate.Thumbprint;
                            active.Certificate.Thumbprint.Should().Be(
                                phase == 2 ? issued.Thumbprint : originalThumbprint);
                            VerifyCertificateKey(active.Certificate);
                        }
                        if (phase == 0)
                        {
                            using ICertificateStore issuerStore = config.CertificateManager.OpenIssuerStore(
                                TrustListIdentifier.Peers);
                            await issuerStore.AddAsync(issuer, ct: timeout.Token).ConfigureAwait(false);
                            await issuerStore.AddCRLAsync(CreateCrl(issuer, "01"), timeout.Token).ConfigureAwait(false);
                            await config.CertificateManager.UpdateAsync(config.SecurityConfiguration,
                                config.ApplicationUri, timeout.Token).ConfigureAwait(false);
                        }
                        var clientApplication = new ApplicationInstance(
                            host.Services.GetRequiredService<ITelemetryContext>())
                        {
                            ApplicationName = "GdsTestClient",
                            ApplicationType = ApplicationType.Client
                        };
                        await using var clientLifetime = clientApplication.ConfigureAwait(false);
                        ApplicationConfiguration clientConfig = await clientApplication
                            .Build("urn:localhost:GdsTestClient", "urn:opcplc:gds-test")
                            .AsClient()
                            .AddSecurityConfiguration([new CertificateIdentifier
                            {
                                StoreType = CertificateStoreType.Directory,
                                StorePath = Path.Combine(clientRoot, "own"),
                                SubjectName = "CN=GdsTestClient",
                                CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                            }], clientRoot)
                            .SetAutoAcceptUntrustedCertificates(true)
                            .CreateAsync().ConfigureAwait(false);
                        (await clientApplication.CheckApplicationInstanceCertificatesAsync(silent: true)
                            .ConfigureAwait(false)).Should().BeTrue();
                        if (phase == 0)
                        {
                            using ICertificateStore clientIssuers = clientConfig.CertificateManager
                                .OpenIssuerStore(TrustListIdentifier.Peers);
                            await clientIssuers.AddAsync(issuer, ct: timeout.Token).ConfigureAwait(false);
                            await clientIssuers.AddCRLAsync(CreateCrl(issuer, "01"), timeout.Token).ConfigureAwait(false);
                        }
                        await clientConfig.CertificateManager.UpdateAsync(clientConfig.SecurityConfiguration,
                            clientConfig.ApplicationUri, timeout.Token).ConfigureAwait(false);
                        string endpoint = $"opc.tcp://localhost:{port}/gds-push";
                        var client = CreatePushClient(clientConfig);
                        await client.ConnectAsync(endpoint, timeout.Token).ConfigureAwait(false);
                        try
                        {
                            if (phase == 0)
                            {
                                if (useCsr)
                                {
                                    ByteString csr = await client.CreateSigningRequestAsync(client.DefaultApplicationGroup,
                                        client.ApplicationCertificateType, "CN=OpcPlc", true,
                                        ByteString.From(RandomNumberGenerator.GetBytes(32)), timeout.Token)
                                        .ConfigureAwait(false);
                                    var request = CertificateRequest.LoadSigningRequest(csr.ToArray(),
                                        HashAlgorithmName.SHA256,
                                        CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions,
                                        RSASignaturePadding.Pkcs1);
                                    request.SubjectName.Name.Should().Be("CN=OpcPlc");
                                    if (!request.CertificateExtensions.Any(extension => extension.Oid?.Value == "2.5.29.15"))
                                    {
                                        request.CertificateExtensions.Add(new X509KeyUsageExtension(
                                            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment |
                                            X509KeyUsageFlags.DataEncipherment, true));
                                    }
                                    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                                    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                                        new OidCollection { new("1.3.6.1.5.5.7.3.1"), new("1.3.6.1.5.5.7.3.2") }, false));
                                    using X509Certificate2 issuerPublic = X509CertificateLoader.LoadCertificate(issuer.RawData);
                                    request.CertificateExtensions.Add(System.Security.Cryptography.X509Certificates
                                        .X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                                            issuerPublic, includeKeyIdentifier: true, includeIssuerAndSerial: true));
                                    using RSA signingKey = issuer.GetRSAPrivateKey();
                                    issued = Certificate.From(request.Create(issuer.SubjectName,
                                        X509SignatureGenerator.CreateForRSA(signingKey, RSASignaturePadding.Pkcs1),
                                        DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1),
                                        RandomNumberGenerator.GetBytes(16)));
                                }
                                else
                                {
                                    issued = CreateApplicationReplacement(issuer, applicationUri: config.ApplicationUri,
                                        domainName: new Uri(factory.Server.GetEndpoints()[0].EndpointUrl).DnsSafeHost);
                                }
                                issued.Thumbprint.Should().NotBe(originalThumbprint);
                            }
                            else if (phase == 1)
                            {
                                using RSA key = useCsr ? null : issued.GetRSAPrivateKey();
                                ByteString privateKey = useCsr ? ByteString.Empty :
                                    ByteString.From(Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem()));
                                bool applyRequired = await client.UpdateCertificateAsync(client.DefaultApplicationGroup,
                                    client.ApplicationCertificateType, ByteString.From(issued.RawData),
                                    useCsr ? string.Empty : "PEM", privateKey, [ByteString.From(issuer.RawData)],
                                    timeout.Token).ConfigureAwait(false);
                                applyRequired.Should().BeTrue();
                                await client.ApplyChangesAsync(timeout.Token).ConfigureAwait(false);
                                await ((ConfigurationNodeManager)factory.Server.CurrentInstance.ConfigurationNodeManager)
                                    .DrainPendingApplyChangesAsync(timeout.Token).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            await client.DisconnectAsync().ConfigureAwait(false);
                        }

                        if (phase != 0)
                        {
                            var freshClient = CreatePushClient(clientConfig);
                            await freshClient.ConnectAsync(endpoint, timeout.Token).ConfigureAwait(false);
                            try
                            {
                                using var presented = Certificate.FromRawData(
                                    freshClient.Session.ConfiguredEndpoint.Description.ServerCertificate);
                                presented.Thumbprint.Should().Be(issued.Thumbprint);
                                var identifier = config.SecurityConfiguration.ApplicationCertificates[0];
                                using ICertificateStore store = ((ICertificateStoreResolver)config.CertificateManager)
                                    .OpenCertificateStore(identifier.StorePath, storeType, noPrivateKeys: false);
                                using CertificateCollection persisted = await store.EnumerateAsync(timeout.Token)
                                    .ConfigureAwait(false);
                                persisted.Should().ContainSingle().Which.Thumbprint.Should().Be(issued.Thumbprint);
                                using Certificate storedKey = await store.LoadPrivateKeyAsync(issued.Thumbprint,
                                    issued.Subject, config.ApplicationUri, identifier.CertificateType, null, timeout.Token)
                                    .ConfigureAwait(false);
                                VerifyCertificateKey(storedKey);
                            }
                            finally
                            {
                                await freshClient.DisconnectAsync().ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        await host.StopAsync().ConfigureAwait(false);
                    }
                }
            }
            if (storeType == KubernetesSecretCertificateStore.StoreTypeName)
            {
                Directory.Exists(root).Should().BeFalse("Kubernetes-backed keys must not fall back to local files");
            }
        }
        finally
        {
            issued?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
            if (Directory.Exists(clientRoot))
            {
                Directory.Delete(clientRoot, recursive: true);
            }
        }
    }

    private static ServerPushConfigurationClient CreatePushClient(ApplicationConfiguration configuration) => new(configuration)
    {
        AdminCredentials = new UserIdentity("gds-admin", Encoding.UTF8.GetBytes("gds-test-password"))
    };

    private sealed class GdsTestServerFactory(OpcPlcConfiguration configuration) : IOpcUaServerFactory
    {
        public PlcServer Server { get; private set; }

        public StandardServer CreateServer(ITelemetryContext telemetry, TimeProvider timeProvider) =>
            Server = new PlcServer(configuration, new PlcSimulation([]) { AddReferenceTestSimulation = false },
                new TimeService(), [], telemetry.CreateLogger<PlcServer>(), telemetry);
    }
}
