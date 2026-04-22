namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;

public partial class PlcServer
{
    /// <summary>
    /// Creates the objects used to validate the user identity tokens supported by the server.
    /// </summary>
    private async Task CreateUserIdentityValidatorAsync(ApplicationConfiguration configuration)
    {
        for (int ii = 0; ii < configuration.ServerConfiguration.UserTokenPolicies.Count; ii++)
        {
            UserTokenPolicy policy = configuration.ServerConfiguration.UserTokenPolicies[ii];

            // Create a validator for a certificate token policy.
            // Check if user certificate trust lists are specified in configuration.
            if (policy.TokenType == UserTokenType.Certificate &&
                configuration.SecurityConfiguration.TrustedUserCertificates != null &&
                configuration.SecurityConfiguration.UserIssuerCertificates != null)
            {
                var certificateValidator = new CertificateValidator(_telemetryContext);
                await certificateValidator.UpdateAsync(configuration).ConfigureAwait(false);
                certificateValidator.Update(configuration.SecurityConfiguration.UserIssuerCertificates,
                    configuration.SecurityConfiguration.TrustedUserCertificates,
                    configuration.SecurityConfiguration.RejectedCertificateStore);

                // set custom validator for user certificates.
                m_userCertificateValidator = certificateValidator;
                break;
            }
        }
    }

    /// <summary>
    /// Validates the password for a username token.
    /// </summary>
    private IUserIdentity VerifyPassword(UserNameIdentityToken userNameToken)
    {
        string userName = userNameToken.UserName;
        string password = userNameToken.DecryptedPassword is null
            ? null
            : Encoding.UTF8.GetString(userNameToken.DecryptedPassword);
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

        // user with permission to configure server
        if (userName == Config.AdminUser && password == Config.AdminPassword)
        {
            return new SystemConfigurationIdentity(new UserIdentity(userNameToken));
        }

        // standard users for CTT verification
        if (!(userName == Config.DefaultUser && password == Config.DefaultPassword))
        {
              // create an exception with a vendor defined sub-code.
            throw ServiceResultException.Create(
                StatusCodes.BadUserAccessDenied,
                "Invalid username or password.");
        }

        return new UserIdentity(userNameToken);
    }

    /// <summary>
    /// Called when a client tries to change its user identity.
    /// </summary>
    private void SessionManager_ImpersonateUser(ISession session, ImpersonateEventArgs args)
    {
        if (args.NewIdentity is AnonymousIdentityToken anonymousToken)
        {
            args.Identity = new UserIdentity(anonymousToken);
            LogTokenAccepted("Anonymous", args.Identity.DisplayName);
            return;
        }

        // check for a user name token.
        if (args.NewIdentity is UserNameIdentityToken userNameToken)
        {
            args.Identity = VerifyPassword(userNameToken);
            LogTokenAccepted("UserName", args.Identity.DisplayName);
            return;
        }

        // check for x509 user token.
        if (args.NewIdentity is X509IdentityToken x509Token)
        {
            if (x509Token.Certificate == null)
            {
                if (x509Token.CertificateData != null)
                {
                    try
                    {
                        x509Token.Certificate = X509CertificateLoader.LoadCertificate(x509Token.CertificateData);
                    }
                    catch (Exception ex)
                    {
                        // create an exception with a vendor defined sub-code.
                        throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                            "Security token is not a valid X509 token. The certificate data is invalid.", ex);
                    }
                }
                else
                {
                    // create an exception with a vendor defined sub-code.
                    throw ServiceResultException.Create(StatusCodes.BadIdentityTokenInvalid,
                        "Security token is not a valid X509 token. The certificate is missing.");
                }
            }

            VerifyCertificateAsync(x509Token.Certificate, default).GetAwaiter().GetResult();

            // A client that authenticates with a trusted X509 user certificate is treated
            // as a privileged identity (e.g. a GDS push device). Grant the SecurityAdmin /
            // ConfigureAdmin roles via SystemConfigurationIdentity so that GDS push
            // operations such as UpdateCertificate and TrustList updates are permitted.
            args.Identity = new SystemConfigurationIdentity(new UserIdentity(x509Token));
            LogTokenAccepted("X509", args.Identity.DisplayName);
            return;
        }

        // no other token types are accepted.
        throw ServiceResultException.Create(StatusCodes.BadIdentityTokenRejected,
            "Security token is not a valid user identity token.");
    }

    /// <summary>
    /// Verifies that a certificate user token is trusted.
    /// </summary>
    private async Task VerifyCertificateAsync(X509Certificate2 certificate, CancellationToken ct)
    {
        try
        {
            if (m_userCertificateValidator != null)
            {
                await m_userCertificateValidator.ValidateAsync(certificate, ct).ConfigureAwait(false);
            }
            else
            {
                await Config.OpcUa.ApplicationConfiguration.CertificateValidator.ValidateAsync(certificate, ct).ConfigureAwait(false);
            }
        }
        catch (Exception e)
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
            throw ServiceResultException.Create((uint)result, info.Text);
        }
    }

    private CertificateValidator m_userCertificateValidator;

    [LoggerMessage(Level = LogLevel.Information, Message = "{TokenType} Token Accepted: {DisplayName}")]
    partial void LogTokenAccepted(string tokenType, string displayName);
}
