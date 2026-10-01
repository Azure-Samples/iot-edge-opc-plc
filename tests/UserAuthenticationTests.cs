namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Identity;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Configuration;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

[TestFixture]
public class UserAuthenticationTests
{
    [Test]
    public void SessionManager_ImpersonateUser_AcceptsAnonymousIdentity()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new AnonymousIdentityToken());
        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.TokenType.Should().Be(UserTokenType.Anonymous);
    }

    [Test]
    public void SessionManager_ImpersonateUser_ReturnsSystemConfigurationIdentity_ForAdminUser()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new UserNameIdentityTokenHandler(
            testContext.Config.AdminUser,
            System.Text.Encoding.UTF8.GetBytes(testContext.Config.AdminPassword)));

        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.Should().BeOfType<SystemConfigurationIdentity>();
    }

    [Test]
    public void SessionManager_ImpersonateUser_ReturnsRegularIdentity_ForDefaultUser()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new UserNameIdentityTokenHandler(
            testContext.Config.DefaultUser,
            System.Text.Encoding.UTF8.GetBytes(testContext.Config.DefaultPassword)));

        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.TokenType.Should().Be(UserTokenType.UserName);
        args.Identity.Should().NotBeOfType<SystemConfigurationIdentity>();
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsInvalidPassword()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new UserNameIdentityTokenHandler(
            testContext.Config.DefaultUser,
            System.Text.Encoding.UTF8.GetBytes("wrong-password")));

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should()
            .Throw<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadUserAccessDenied);
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsUsernameToken_WhenNoCredentialsAreConfigured()
    {
        using var testContext = new TestServerContext(configureCredentials: false);

        var args = CreateImpersonateEventArgs(new UserNameIdentityTokenHandler(
            "sysadmin", System.Text.Encoding.UTF8.GetBytes("demo")));

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should()
            .Throw<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadUserAccessDenied);
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsUnsupportedTokenType()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new IssuedIdentityToken());

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should()
            .Throw<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadIdentityTokenRejected);
    }

    [Test]
    public void SessionManager_ImpersonateUser_AcceptsTrustedX509UserCertificate()
    {
        using var testContext = new TestServerContext();
        using var userCertificate = CreateSelfSignedUserCertificate("test-user-cert");

        ConfigureTrustedUserCertificateValidator(testContext.Server, userCertificate);

        var args = CreateImpersonateEventArgs(new X509IdentityToken
        {
            CertificateData = (ByteString)userCertificate.Export(X509ContentType.Cert)
        });

        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.TokenType.Should().Be(UserTokenType.Certificate);
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsX509TokenWithoutCertificate()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new X509IdentityToken());

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should()
            .Throw<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadIdentityTokenInvalid);
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsX509TokenWithInvalidCertificateData()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new X509IdentityToken
        {
            CertificateData = [1, 2, 3, 4]
        });

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should()
            .Throw<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadIdentityTokenInvalid);
    }

    [Test]
    public void Authentication_RejectsUntrustedX509UserCertificate()
    {
        using var testContext = new TestServerContext();
        using var userCertificate = CreateSelfSignedUserCertificate("untrusted-user-cert");
        var args = CreateImpersonateEventArgs(new X509IdentityToken
        {
            CertificateData = (ByteString)userCertificate.Export(X509ContentType.Cert)
        });

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should().Throw<ServiceResultException>()
            .Where(exception => exception.StatusCode == StatusCodes.BadIdentityTokenRejected);
        args.Identity.Should().BeNull();
    }

    [Test]
    public void Authentication_RejectsDisabledUsernamePassword()
    {
        using var testContext = new TestServerContext();
        testContext.Config.DisableUsernamePasswordAuth = true;
        var args = CreateImpersonateEventArgs(new UserNameIdentityTokenHandler(
            testContext.Config.AdminUser,
            System.Text.Encoding.UTF8.GetBytes(testContext.Config.AdminPassword)));

        Action act = () => InvokeImpersonateUser(testContext.Server, args);

        act.Should().Throw<ServiceResultException>()
            .Where(exception => exception.StatusCode == StatusCodes.BadIdentityTokenRejected);
        args.Identity.Should().BeNull();
    }

    private static ImpersonateEventArgs CreateImpersonateEventArgs(UserIdentityToken token)
    {
        IUserIdentityTokenHandler handler = token switch
        {
            AnonymousIdentityToken anonymous => new AnonymousIdentityTokenHandler(anonymous),
            X509IdentityToken certificate => new X509IdentityTokenHandler(certificate),
            IssuedIdentityToken issued => new IssuedIdentityTokenHandler(issued),
            _ => throw new ArgumentException("Unsupported test token", nameof(token))
        };
        return CreateImpersonateEventArgs(handler);
    }

    private static ImpersonateEventArgs CreateImpersonateEventArgs(IUserIdentityTokenHandler handler)
    {
        return new ImpersonateEventArgs(handler, new UserTokenPolicy(), new EndpointDescription());
    }

    private static void InvokeImpersonateUser(PlcServer server, ImpersonateEventArgs args)
    {
        var registry = new ServerIdentityRegistry();
        var serverInternal = new Mock<IServerInternal>();
        serverInternal.SetupGet(instance => instance.IdentityRegistry).Returns(registry);
        MethodInfo method = typeof(PlcServer).GetMethod(
            "RegisterUserIdentityValidators", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RegisterUserIdentityValidators method was not found.");
        method.Invoke(server, [serverInternal.Object]);

        var context = new AuthenticationContext(
            args.UserIdentityTokenHandler, args.UserTokenPolicy, args.EndpointDescription, null, null, null);
        AuthenticationResult result = registry.AuthenticateAsync(context, CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();
        if (result.Outcome == AuthenticationOutcome.Rejected)
        {
            throw new ServiceResultException(result.Error);
        }

        result.Outcome.Should().Be(AuthenticationOutcome.Accepted);
        args.Identity = result.Identity;
    }

    private static X509Certificate2 CreateSelfSignedUserCertificate(string subjectName)
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature |
            X509KeyUsageFlags.NonRepudiation |
            X509KeyUsageFlags.KeyEncipherment |
            X509KeyUsageFlags.DataEncipherment,
            true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.2") },
            true));

        using X509Certificate2 certificateWithPrivateKey = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        return X509CertificateLoader.LoadCertificate(certificateWithPrivateKey.Export(X509ContentType.Cert));
    }

    private static void ConfigureTrustedUserCertificateValidator(PlcServer server, X509Certificate2 certificate)
    {
        using var ownedCertificate = new Certificate(certificate.RawData);
        using ICertificateStore store = server.Config.OpcUa.ApplicationConfiguration.CertificateManager
            .OpenTrustedStore(TrustListIdentifier.Users);
        store.AddAsync(ownedCertificate, null, CancellationToken.None).GetAwaiter().GetResult();
    }

    private sealed class TestServerContext : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly OpcTelemetryContext _telemetryContext;
        private readonly CertificateManager _certificateManager;
        private readonly string _pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-auth-" + Guid.NewGuid().ToString("N"));

        public TestServerContext(bool configureCredentials = true)
        {
            Config = new OpcPlcConfiguration();

            if (configureCredentials)
            {
                Config.AdminUser = "test-admin";
                Config.AdminPassword = "test-admin-password";
                Config.DefaultUser = "test-user";
                Config.DefaultPassword = "test-user-password";
            }

            _loggerFactory = LoggerFactory.Create(_ => { });
            ILogger logger = _loggerFactory.CreateLogger<PlcServer>();
            _telemetryContext = new OpcTelemetryContext(_loggerFactory, "OpcPlc", "test");
            _certificateManager = new CertificateManager(_telemetryContext);
            _certificateManager.RegisterTrustList(
                TrustListIdentifier.Users,
                Path.Combine(_pkiRoot, "trusted"),
                Path.Combine(_pkiRoot, "issuers"));
            Config.OpcUa.ApplicationConfiguration = new ApplicationConfiguration
            {
                CertificateManager = _certificateManager
            };

            var simulation = new PlcSimulation(ImmutableList<IPluginNodes>.Empty);
            Server = new PlcServer(
                Config,
                simulation,
                new TimeService(),
                ImmutableList<IPluginNodes>.Empty,
                logger,
                _telemetryContext);
        }

        public OpcPlcConfiguration Config { get; }

        public PlcServer Server { get; }

        public ITelemetryContext TelemetryContext => _telemetryContext;

        public void Dispose()
        {
            Server.Dispose();
            _certificateManager.Dispose();
            _telemetryContext.Dispose();
            _loggerFactory.Dispose();
            if (Directory.Exists(_pkiRoot))
            {
                Directory.Delete(_pkiRoot, recursive: true);
            }
        }
    }
}
