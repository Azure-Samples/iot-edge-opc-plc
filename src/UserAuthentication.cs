namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Identity;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class PlcServer
{
    /// <summary>
    /// Registers validators for the user identity tokens supported by the server.
    /// </summary>
    private void RegisterUserIdentityValidators(IServerInternal server)
    {
        foreach (IUserTokenAuthenticator authenticator in CreateUserIdentityAuthenticators())
        {
            server.IdentityRegistry.Register(authenticator);
        }
    }

    internal IEnumerable<IUserTokenAuthenticator> CreateUserIdentityAuthenticators()
    {
        foreach (UserTokenType tokenType in Enum.GetValues<UserTokenType>())
        {
            yield return new RejectedTokenAuthenticator(tokenType);
        }

        if (!Config.DisableAnonymousAuth)
        {
            yield return new AnonymousAuthenticator();
        }

        if (!Config.DisableUsernamePasswordAuth)
        {
            yield return new UserNamePasswordAuthenticator((token, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                IUserIdentity identity = VerifyPassword(token);
                LogTokenAccepted("UserName", identity.DisplayName);
                return ValueTask.FromResult(identity);
            });
        }

        if (!Config.DisableCertAuth)
        {
            yield return new X509Authenticator(VerifyUserCertificateAsync);
        }
    }

    /// <summary>
    /// Validates the password for a username token.
    /// </summary>
    private IUserIdentity VerifyPassword(UserNameIdentityTokenHandler userNameToken)
    {
        string userName = userNameToken.UserName;
        string password = Encoding.UTF8.GetString(userNameToken.DecryptedPassword.AsSpan());
        if (string.IsNullOrEmpty(userName))
        {
            // an empty username is not accepted.
            throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                "Security token is not a valid username token. An empty username is not accepted.");
        }

        if (string.IsNullOrEmpty(password))
        {
            // an empty password is not accepted.
            throw ServiceResultException.Create(StatusCodes.BadIdentityTokenRejected,
                "Security token is not a valid username token. An empty password is not accepted.");
        }

        // User with permission to configure the server. The account only exists when admin
        // credentials were explicitly configured; there is no built-in admin account.
        if (Config.HasAdminCredentials &&
            userName == Config.AdminUser &&
            password == Config.AdminPassword)
        {
            return new SystemConfigurationIdentity(new UserIdentity(userNameToken));
        }

        // Standard user, e.g. for CTT verification.
        if (!Config.HasDefaultUserCredentials ||
            userName != Config.DefaultUser ||
            password != Config.DefaultPassword)
        {
            // create an exception with a vendor defined sub-code.
            throw ServiceResultException.Create(
                StatusCodes.BadUserAccessDenied,
                "Invalid username or password.");
        }

        return new UserIdentity(userNameToken);
    }

    /// <summary>
    /// Validates an X509 user token and grants the existing PLC configuration identity.
    /// </summary>
    private async ValueTask<IUserIdentity> VerifyUserCertificateAsync(
        X509IdentityTokenHandler tokenHandler,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (tokenHandler.Token is not X509IdentityToken token || token.CertificateData.Span.IsEmpty)
        {
            throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                "Security token is not a valid X509 token. The certificate is missing.");
        }

        Certificate certificate;
        try
        {
            certificate = new Certificate(token.CertificateData.Span);
        }
        catch (Exception ex)
        {
            throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                "Security token is not a valid X509 token. The certificate data is invalid.", ex);
        }

        using (certificate)
        {
            await VerifyCertificateAsync(certificate, ct).ConfigureAwait(false);
        }

        var identity = new SystemConfigurationIdentity(new UserIdentity(tokenHandler));
        LogTokenAccepted("X509", identity.DisplayName);
        return identity;
    }

    /// <summary>
    /// Verifies that a certificate user token is trusted.
    /// </summary>
    private async Task VerifyCertificateAsync(Certificate certificate, CancellationToken ct)
    {
        try
        {
            var result = await Config.OpcUa.ApplicationConfiguration.CertificateManager.ValidateAsync(
                certificate, TrustListIdentifier.Users, ct).ConfigureAwait(false);
            result.ThrowIfInvalid();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            TranslationInfo info;
            StatusCode result = StatusCodes.BadIdentityTokenRejected;
            if (e is ServiceResultException se &&
                se.StatusCode == StatusCodes.BadCertificateUseNotAllowed)
            {
                info = new TranslationInfo(
                    "InvalidCertificate",
                    locale: string.Empty, // Invariant.
                    "'{0}' is an invalid user certificate.",
                    certificate.Subject);

                result = StatusCodes.BadIdentityTokenInvalid;
            }
            else
            {
                // construct translation object with default text.
                info = new TranslationInfo(
                    "UntrustedCertificate",
                    locale: string.Empty, // Invariant.
                    "'{0}' is not a trusted user certificate.",
                    certificate.Subject);
            }

            // create an exception with a vendor defined sub-code.
            throw ServiceResultException.Create(result, info.Text, certificate.Subject);
        }
    }

    private sealed class RejectedTokenAuthenticator(UserTokenType tokenType) : IUserTokenAuthenticator
    {
        public UserTokenType TokenType => tokenType;

        public string IssuedTokenProfileUri => null;

        public ValueTask<AuthenticationResult> AuthenticateAsync(AuthenticationContext context, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(AuthenticationResult.Reject(new ServiceResult(
                StatusCodes.BadIdentityTokenRejected)));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{TokenType} Token Accepted: {DisplayName}")]
    partial void LogTokenAccepted(string tokenType, string displayName);
}
