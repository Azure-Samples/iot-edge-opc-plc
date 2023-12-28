namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class PlcServer
{
    /// <summary>
    /// Creates the objects used to validate the user identity tokens supported by the server.
    /// </summary>
    private void CreateUserIdentityValidators(ApplicationConfiguration configuration)
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
                var certificateValidator = new CertificateValidator();
                certificateValidator.Update(configuration.SecurityConfiguration).Wait();
                certificateValidator.Update(configuration.SecurityConfiguration.UserIssuerCertificates,
                    configuration.SecurityConfiguration.TrustedUserCertificates,
                    configuration.SecurityConfiguration.RejectedCertificateStore);

                // set custom validator for user certificates.
                m_certificateValidator = certificateValidator.GetChannelValidator();
            }
        }
    }

    /// <summary>
    /// Validates the password for a username token.
    /// </summary>
    private IUserIdentity VerifyPassword(UserNameIdentityToken userNameToken)
    {
        var userName = userNameToken.UserName;
        var password = userNameToken.DecryptedPassword;
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
            // construct translation object with default text.
            var info = new TranslationInfo(
                "InvalidPassword",
                "en-US",
                "Invalid username or password.",
                userName);

            // create an exception with a vendor defined sub-code.
            throw new ServiceResultException(new ServiceResult(
                StatusCodes.BadUserAccessDenied,
                "InvalidPassword",
                LoadServerProperties().ProductUri,
                new LocalizedText(info)));
        }

        return new UserIdentity(userNameToken);
    }

    /// <summary>
    /// Called when a client tries to change its user identity.
    /// </summary>
    private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
    {
        // check for a WSS token.
        //var wssToken = args.NewIdentity as IssuedIdentityToken;

        // check for a user name token.
        if (args.NewIdentity is UserNameIdentityToken userNameToken)
        {
            args.Identity = VerifyPassword(userNameToken);
            _logger.LogInformation("UserName Token Accepted: {displayName}", args.Identity.DisplayName);
            return;
        }

        // check for x509 user token.
        if (args.NewIdentity is X509IdentityToken x509Token)
        {
            VerifyCertificate(x509Token.Certificate);
            args.Identity = new UserIdentity(x509Token);
            _logger.LogInformation("X509 Token Accepted: {displayName}", args.Identity.DisplayName);
        }
    }

    /// <summary>
    /// Verifies that a certificate user token is trusted.
    /// </summary>
    private void VerifyCertificate(X509Certificate2 certificate)
    {
        try
        {
            if (m_certificateValidator != null)
            {
                m_certificateValidator.Validate(certificate);
            }
            else
            {
                CertificateValidator.Validate(certificate);
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
                    "en-US",
                    "'{0}' is an invalid user certificate.",
                    certificate.Subject);

                result = StatusCodes.BadIdentityTokenInvalid;
            }
            else
            {
                // construct translation object with default text.
                info = new TranslationInfo(
                    "UntrustedCertificate",
                    "en-US",
                    "'{0}' is not a trusted user certificate.",
                    certificate.Subject);
            }

            // create an exception with a vendor defined sub-code.
            throw new ServiceResultException(new ServiceResult(
                result,
                info.Key,
                "http://opcfoundation.org/UA/Sample/",
                new LocalizedText(info)));
        }
    }

    private ICertificateValidator m_certificateValidator;
}
