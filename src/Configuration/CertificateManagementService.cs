namespace OpcPlc.Configuration;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public class CertificateManagementService(
    OpcPlcConfiguration config,
    ILogger logger,
    ITelemetryContext telemetryContext)
{
    private readonly OpcPlcConfiguration _config = config;
    private readonly ILogger _logger = logger;
    private readonly ITelemetryContext _telemetryContext = telemetryContext ?? throw new ArgumentNullException(nameof(telemetryContext));

    /// <summary>
    /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store.
    /// </summary>
    public async Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
    {
        bool result = true;

        if (thumbprintsToRemove.Count == 0)
        {
            _logger.LogError("There is no thumbprint specified for certificates to remove. Please check your command line options.");
            return false;
        }

        // search the trusted peer store and remove certificates with a specified thumbprint
        try
        {
            _logger.LogInformation("Starting to remove certificate(s) from trusted peer and trusted issuer store");
            using ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext);
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await trustedStore.FindByThumbprintAsync(thumbprint, CancellationToken.None).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await trustedStore.DeleteAsync(thumbprint, CancellationToken.None).ConfigureAwait(false))
                    {
                        _logger.LogWarning("Failed to remove certificate with thumbprint '{Thumbprint}' from the trusted peer store", thumbprint);
                    }
                    else
                    {
                        _logger.LogInformation("Removed certificate with thumbprint '{Thumbprint}' from the trusted peer store", thumbprint);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to remove certificate(s) from the trusted peer store");
            result = false;
        }

        // search the trusted issuer store and remove certificates with a specified thumbprint
        try
        {
            using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore(_telemetryContext);
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await issuerStore.FindByThumbprintAsync(thumbprint, CancellationToken.None).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await issuerStore.DeleteAsync(thumbprint, CancellationToken.None).ConfigureAwait(false))
                    {
                        _logger.LogWarning("Failed to delete certificate with thumbprint '{Thumbprint}' from the trusted issuer store", thumbprint);
                    }
                    else
                    {
                        _logger.LogInformation("Removed certificate with thumbprint '{Thumbprint}' from the trusted issuer store", thumbprint);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to remove certificate(s) from the trusted issuer store");
            result = false;
        }
        return result;
    }

    /// <summary>
    /// Validate and add certificates to the trusted issuer or trusted peer store.
    /// </summary>
    public async Task<bool> AddCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        bool result = true;

        if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
        {
            _logger.LogError("There is no certificate provided. Please check your command line options.");
            return false;
        }

        _logger.LogInformation("Starting to add certificate(s) to the {StoreType} store", issuerCertificate ? "trusted issuer" : "trusted peer");
        var certificatesToAdd = new X509Certificate2Collection();
        try
        {
            // validate the input and build issuer cert collection
            if (certificateFileNames?.Count > 0)
            {
                foreach (var certificateFileName in certificateFileNames)
                {
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(certificateFileName);
                    certificatesToAdd.Add(certificate);
                }
            }
            if (certificateBase64Strings?.Count > 0)
            {
                foreach (var certificateBase64String in certificateBase64Strings)
                {
                    byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(certificateBase64String, buffer, out _))
                    {
                        var certificate = X509CertificateLoader.LoadCertificate(buffer);
                        certificatesToAdd.Add(certificate);
                    }
                    else
                    {
                        _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The issuer certificate data is invalid. Please check your command line options");
            return false;
        }

        // add the certificate to the right store
        if (issuerCertificate)
        {
            try
            {
                using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore(_telemetryContext);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await issuerStore.AddAsync(certificateToAdd, null, CancellationToken.None).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted issuer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted issuer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted issuer store");
                result = false;
            }
        }
        else
        {
            try
            {
                using ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await trustedStore.AddAsync(certificateToAdd, null, CancellationToken.None).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted peer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted peer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted peer store");
                result = false;
            }
        }
        return result;
    }

    /// <summary>
    /// Validate and add certificates to the trusted user or user issuer store.
    /// </summary>
    public async Task<bool> AddUserCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        bool result = true;

        if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
        {
            _logger.LogError("There is no certificate provided. Please check your command line options.");
            return false;
        }

        _logger.LogInformation("Starting to add certificate(s) to the {StoreType} store", issuerCertificate ? "user issuer" : "trusted user");
        var certificatesToAdd = new X509Certificate2Collection();
        try
        {
            // validate the input and build cert collection
            if (certificateFileNames?.Count > 0)
            {
                foreach (var certificateFileName in certificateFileNames)
                {
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(certificateFileName);
                    certificatesToAdd.Add(certificate);
                }
            }
            if (certificateBase64Strings?.Count > 0)
            {
                foreach (var certificateBase64String in certificateBase64Strings)
                {
                    byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(certificateBase64String, buffer, out _))
                    {
                        var certificate = X509CertificateLoader.LoadCertificate(buffer);
                        certificatesToAdd.Add(certificate);
                    }
                    else
                    {
                        _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The user certificate data is invalid. Please check your command line options");
            return false;
        }

        // add to the configured user stores
        if (issuerCertificate)
        {
            try
            {
                using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.UserIssuerCertificates.OpenStore(_telemetryContext);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await issuerStore.AddAsync(certificateToAdd, null, CancellationToken.None).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the user issuer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in user issuer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the user issuer store");
                result = false;
            }
        }
        else
        {
            try
            {
                using ICertificateStore trustedUserStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedUserCertificates.OpenStore(_telemetryContext);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await trustedUserStore.AddAsync(certificateToAdd, null, CancellationToken.None).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted user store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted user store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted user store");
                result = false;
            }
        }

        return result;
    }

    /// <summary>
    /// Update the CRL in the corresponding store.
    /// </summary>
    public async Task<bool> UpdateCrlAsync(string newCrlBase64String, string newCrlFileName)
    {
        bool result = true;

        if (string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
        {
            _logger.LogError("There is no CRL specified. Please check your command line options");
            return false;
        }

        // validate input and create the new CRL
        _logger.LogInformation("Starting to update the current CRL");
        X509CRL newCrl;
        try
        {
            if (!string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
            {
                byte[] buffer = new byte[newCrlBase64String.Length * 3 / 4];
                if (Convert.TryFromBase64String(newCrlBase64String, buffer, out _))
                {
                    newCrl = new X509CRL(buffer);
                }
                else
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", newCrlBase64String.Substring(0, 10));
                    return false;
                }
            }
            else
            {
                newCrl = new X509CRL(newCrlFileName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The new CRL data is invalid");
            return false;
        }

        // check if CRL was signed by a trusted peer cert
        using (ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext))
        {
            bool trustedCrlIssuer = false;
            var trustedCertificates = await trustedStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);

            foreach (var trustedCertificate in trustedCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, trustedCertificate.Subject) && newCrl.VerifySignature(trustedCertificate, throwOnError: false))
                    {
                        // The issuer of the new CRL is trusted. Delete the CRLs of the issuer in the trusted store.
                        _logger.LogInformation("Remove the current CRL from the trusted peer store");
                        trustedCrlIssuer = true;

                        var crlsToRemove = await trustedStore.EnumerateCRLsAsync(trustedCertificate, true, CancellationToken.None).ConfigureAwait(false);
                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await trustedStore.DeleteCRLAsync(crlToRemove, CancellationToken.None).ConfigureAwait(false))
                                {
                                    _logger.LogWarning("Failed to remove CRL issued by '{Issuer}' from the trusted peer store", crlToRemove.Issuer);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Error while removing the current CRL issued by '{Issuer}' from the trusted peer store", crlToRemove.Issuer);
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while removing the current CRL from the trusted peer store");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await trustedStore.AddCRLAsync(newCrl, CancellationToken.None).ConfigureAwait(false);
                    _logger.LogInformation("The new CRL issued by '{Issuer}' was added to the trusted peer store", newCrl.Issuer);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while adding the new CRL to the trusted peer store");
                    result = false;
                }
            }
        }

        // check if CRL was signed by a trusted issuer cert
        using (ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore(_telemetryContext))
        {
            bool trustedCrlIssuer = false;
            var issuerCertificates = await issuerStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);

            foreach (var issuerCertificate in issuerCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, issuerCertificate.Subject) && newCrl.VerifySignature(issuerCertificate, false))
                    {
                        // The issuer of the new CRL is trusted. Delete the CRLs of the issuer in the trusted store.
                        _logger.LogInformation("Remove the current CRL from the trusted issuer store");
                        trustedCrlIssuer = true;
                        var crlsToRemove = await issuerStore.EnumerateCRLsAsync(issuerCertificate, true, CancellationToken.None).ConfigureAwait(false);

                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await issuerStore.DeleteCRLAsync(crlToRemove, CancellationToken.None).ConfigureAwait(false))
                                {
                                    _logger.LogWarning("Failed to remove the current CRL issued by '{Issuer}' from the trusted issuer store", crlToRemove.Issuer);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Error while removing the current CRL issued by '{Issuer}' from the trusted issuer store", crlToRemove.Issuer);
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while removing the current CRL from the trusted issuer store");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await issuerStore.AddCRLAsync(newCrl, CancellationToken.None).ConfigureAwait(false);
                    _logger.LogInformation("The new CRL issued by '{Issuer}' was added to the trusted issuer store", newCrl.Issuer);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while adding the new CRL issued by '{Issuer}' to the trusted issuer store", newCrl.Issuer);
                    result = false;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Validate and update the application.
    /// </summary>
    public async Task<bool> UpdateApplicationCertificateAsync(
        string newCertificateBase64String,
        string newCertificateFileName,
        string certificatePassword,
        string privateKeyBase64String,
        string privateKeyFileName)
    {
        if (string.IsNullOrEmpty(newCertificateFileName) && string.IsNullOrEmpty(newCertificateBase64String))
        {
            _logger.LogError("There is no new application certificate data provided. Please check your command line options.");
            return false;
        }

        // validate input and create the new application cert
        X509Certificate2 newCertificate;
        try
        {
            if (string.IsNullOrEmpty(newCertificateFileName))
            {
                byte[] buffer = new byte[newCertificateBase64String.Length * 3 / 4];
                if (Convert.TryFromBase64String(newCertificateBase64String, buffer, out _))
                {
                    newCertificate = X509CertificateLoader.LoadCertificate(buffer);
                }
                else
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", newCertificateBase64String.Substring(0, 10));
                    return false;
                }
            }
            else
            {
                newCertificate = X509CertificateLoader.LoadCertificateFromFile(newCertificateFileName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The new application certificate data is invalid");
            return false;
        }

        // validate input and create the private key
        _logger.LogInformation("Start updating the current application certificate");
        byte[] privateKey = null;
        try
        {
            if (!string.IsNullOrEmpty(privateKeyBase64String))
            {
                privateKey = new byte[privateKeyBase64String.Length * 3 / 4];
                if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out _))
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", privateKeyBase64String.Substring(0, 10));
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(privateKeyFileName))
            {
                privateKey = await File.ReadAllBytesAsync(privateKeyFileName).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The private key data is invalid");
            return false;
        }

        // check if there is an application certificate and fetch its data
        bool hasApplicationCertificate = false;
        X509Certificate2 currentApplicationCertificate = null;
        string currentSubjectName = null;
        if (_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate?.Certificate != null)
        {
            hasApplicationCertificate = true;
            currentApplicationCertificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            currentSubjectName = currentApplicationCertificate.SubjectName.Name;
            _logger.LogInformation("The current application certificate has SubjectName '{CurrentSubjectName}' and thumbprint '{CurrentThumbprint}'", currentSubjectName, currentApplicationCertificate.Thumbprint);
        }
        else
        {
            _logger.LogInformation("There is no existing application certificate");
        }

        // for a cert update subject names of current and new certificate must match
        if (hasApplicationCertificate && !X509Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
        {
            _logger.LogError("The SubjectName '{NewSubjectName}' of the new certificate doesn't match the current certificates SubjectName '{CurrentSubjectName}'", newCertificate.SubjectName.Name, currentSubjectName);
            return false;
        }

        // if the new cert is not self-signed verify with the trusted peer and trusted issuer certificates
        try
        {
            if (!X509Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
            {
                // verify the new certificate was signed by a trusted issuer or trusted peer
                var certValidator = new CertificateValidator(_telemetryContext);
                var verificationTrustList = new CertificateTrustList();
                var verificationCollection = new CertificateIdentifierCollection();
                using (ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore(_telemetryContext))
                {
                    var certs = await issuerStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                using (ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext))
                {
                    var certs = await trustedStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                verificationTrustList.TrustedCertificates = verificationCollection;
                certValidator.Update(verificationTrustList, verificationTrustList, null);
                await certValidator.ValidateAsync(newCertificate, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to verify integrity of the new certificate and the trusted issuer list");
            return false;
        }

        // detect format of new cert and create/update the application certificate
        X509Certificate2 newCertificateWithPrivateKey = null;
        string newCertFormat = null;
        // check if new cert is PFX
        if (string.IsNullOrEmpty(newCertFormat))
        {
            try
            {
                X509Certificate2 certWithPrivateKey = X509Utils.CreateCertificateFromPKCS12(privateKey, certificatePassword);
                newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                newCertFormat = "PFX";
                _logger.LogInformation("The private key for the new certificate was passed in using PFX format");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Certificate file is not PFX");
            }
        }
        // check if new cert is PEM
        if (string.IsNullOrEmpty(newCertFormat))
        {
            try
            {
                newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCertificate, privateKey, certificatePassword);
                newCertFormat = "PEM";
                _logger.LogInformation("The private key for the new certificate was passed in using PEM format");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Certificate file is not PEM");
            }
        }

        if (string.IsNullOrEmpty(newCertFormat))
        {
            // check if new cert is DER and there is an existing application certificate
            try
            {
                if (hasApplicationCertificate)
                {
                    X509Certificate2 certWithPrivateKey = await LoadCertificatePrivateKeyAsync(
                        _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate,
                        certificatePassword,
                        CancellationToken.None).ConfigureAwait(false);
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                    newCertFormat = "DER";
                }
                else
                {
                    _logger.LogError("There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Application certificate format is not DER");
            }
        }

        if (hasApplicationCertificate)
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                _logger.LogError("The provided format of the private key is not supported (must be PEM or PFX) or the provided cert password is wrong");
                return false;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                _logger.LogError("There is no application certificate we can update and for the new application certificate there was not usable private key (must be PEM or PFX format) provided or the provided cert password is wrong");
                return false;
            }
        }

        // remove the existing and add the new application cert
        using (ICertificateStore appStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore(_telemetryContext))
        {
            _logger.LogInformation("Remove the existing application certificate");
            try
            {
                if (hasApplicationCertificate && !await appStore.DeleteAsync(currentApplicationCertificate.Thumbprint, CancellationToken.None).ConfigureAwait(false))
                {
                    _logger.LogWarning("Removing the existing application certificate with thumbprint '{CurrentThumbprint}' failed", currentApplicationCertificate.Thumbprint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the existing application certificate from the ApplicationCertificate store");
            }
            try
            {
                await appStore.AddAsync(newCertificateWithPrivateKey, null, CancellationToken.None).ConfigureAwait(false);
                _logger.LogInformation("The new application certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the application certificate store", newCertificateWithPrivateKey.SubjectName.Name, newCertificateWithPrivateKey.Thumbprint);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add the new application certificate to the application certificate store");
                return false;
            }
        }

        // update the application certificate
        try
        {
            _logger.LogInformation("Activating the new application certificate with thumbprint '{NewThumbprint}'", newCertificateWithPrivateKey.Thumbprint);
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificateWithPrivateKey;
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Thumbprint = newCertificateWithPrivateKey.Thumbprint;
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = newCertificateWithPrivateKey.SubjectName.Name;
            await _config.OpcUa.ApplicationConfiguration.CertificateValidator.UpdateCertificateAsync(_config.OpcUa.ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to activate the new application certificate");
            return false;
        }

        return true;
    }

    private async Task<X509Certificate2> LoadCertificatePrivateKeyAsync(CertificateIdentifier certificateIdentifier, string password, CancellationToken ct)
    {
        if (certificateIdentifier is null)
        {
            return null;
        }

        using ICertificateStore store = certificateIdentifier.OpenStore(_telemetryContext);
        return await store.LoadPrivateKeyAsync(
            certificateIdentifier.Thumbprint,
            certificateIdentifier.SubjectName,
            null,
            certificateIdentifier.CertificateType,
            password?.ToCharArray(),
            ct).ConfigureAwait(false);
    }
}
