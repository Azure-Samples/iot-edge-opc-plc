namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.Configuration;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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

        var args = CreateImpersonateEventArgs(new UserNameIdentityToken
        {
            UserName = testContext.Config.AdminUser,
            DecryptedPassword = System.Text.Encoding.UTF8.GetBytes(testContext.Config.AdminPassword)
        });

        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.Should().BeOfType<SystemConfigurationIdentity>();
    }

    [Test]
    public void SessionManager_ImpersonateUser_ReturnsRegularIdentity_ForDefaultUser()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new UserNameIdentityToken
        {
            UserName = testContext.Config.DefaultUser,
            DecryptedPassword = System.Text.Encoding.UTF8.GetBytes(testContext.Config.DefaultPassword)
        });

        InvokeImpersonateUser(testContext.Server, args);

        args.Identity.Should().NotBeNull();
        args.Identity.TokenType.Should().Be(UserTokenType.UserName);
        args.Identity.Should().NotBeOfType<SystemConfigurationIdentity>();
    }

    [Test]
    public void SessionManager_ImpersonateUser_RejectsInvalidPassword()
    {
        using var testContext = new TestServerContext();

        var args = CreateImpersonateEventArgs(new UserNameIdentityToken
        {
            UserName = testContext.Config.DefaultUser,
            DecryptedPassword = System.Text.Encoding.UTF8.GetBytes("wrong-password")
        });

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

        ConfigureTrustedUserCertificateValidator(testContext.Server, userCertificate, testContext.TelemetryContext);

        var args = CreateImpersonateEventArgs(new X509IdentityToken
        {
            CertificateData = userCertificate.Export(X509ContentType.Cert)
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

    private static ImpersonateEventArgs CreateImpersonateEventArgs(UserIdentityToken token)
    {
        return new ImpersonateEventArgs(token, new UserTokenPolicy(), new EndpointDescription());
    }

    private static void InvokeImpersonateUser(PlcServer server, ImpersonateEventArgs args)
    {
        MethodInfo method = typeof(PlcServer).GetMethod("SessionManager_ImpersonateUser", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SessionManager_ImpersonateUser method was not found.");

        try
        {
            method.Invoke(server, [null, args]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
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

    private static void ConfigureTrustedUserCertificateValidator(PlcServer server, X509Certificate2 certificate, ITelemetryContext telemetryContext)
    {
        var validator = new CertificateValidator(telemetryContext);

        var trustedCertificates = new CertificateIdentifierCollection
        {
            new(certificate)
        };

        var trustList = new CertificateTrustList
        {
            TrustedCertificates = trustedCertificates
        };

        validator.Update(trustList, trustList, null);

        FieldInfo validatorField = typeof(PlcServer).GetField("m_userCertificateValidator", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("m_userCertificateValidator field was not found.");

        validatorField.SetValue(server, validator);
    }

    private sealed class TestServerContext : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly OpcTelemetryContext _telemetryContext;

        public TestServerContext()
        {
            Config = new OpcPlcConfiguration();
            _loggerFactory = LoggerFactory.Create(_ => { });
            ILogger logger = _loggerFactory.CreateLogger<PlcServer>();
            _telemetryContext = new OpcTelemetryContext(_loggerFactory, "OpcPlc", "test");

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
            _telemetryContext.Dispose();
            _loggerFactory.Dispose();
        }
    }
}
