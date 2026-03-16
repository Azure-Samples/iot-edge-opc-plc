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

public partial class CertificateManagementService(
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
            LogNoThumbprintSpecified();
            return false;
        }

        // search the trusted peer store and remove certificates with a specified thumbprint
        try
        {
            LogStartingRemoveCertificates();
            using ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext);
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await trustedStore.FindByThumbprintAsync(thumbprint, CancellationToken.None).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await trustedStore.DeleteAsync(thumbprint, CancellationToken.None).ConfigureAwait(false))
                    {
                        LogFailedToRemoveCertificate(thumbprint, "trusted peer");
                    }
                    else
                    {
                        LogRemovedCertificate(thumbprint, "trusted peer");
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogErrorRemovingCertificates(e, "trusted peer");
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
                        LogFailedToRemoveCertificate(thumbprint, "trusted issuer");
                    }
                    else
                    {
                        LogRemovedCertificate(thumbprint, "trusted issuer");
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogErrorRemovingCertificates(e, "trusted issuer");
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
            LogNoCertificateProvided();
            return false;
        }

        LogStartingAddCertificates(issuerCertificate ? "trusted issuer" : "trusted peer");
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
                        LogInvalidBase64String(certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogInvalidCertificateData(e, "issuer");
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
                        LogCertificateAddedToStore(certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint, "trusted issuer");
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        LogCertificateAlreadyExists(ex, certificateToAdd.SubjectName.Name, "trusted issuer");
                    }
                }
            }
            catch (Exception e)
            {
                LogErrorAddingCertificate(e, "trusted issuer");
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
                        LogCertificateAddedToStore(certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint, "trusted peer");
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        LogCertificateAlreadyExists(ex, certificateToAdd.SubjectName.Name, "trusted peer");
                    }
                }
            }
            catch (Exception e)
            {
                LogErrorAddingCertificate(e, "trusted peer");
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
            LogNoCertificateProvided();
            return false;
        }

        LogStartingAddCertificates(issuerCertificate ? "user issuer" : "trusted user");
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
                        LogInvalidBase64String(certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogInvalidCertificateData(e, "user");
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
                        LogCertificateAddedToStore(certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint, "user issuer");
                    }
                    catch (ArgumentException ex)
                    {
                        LogCertificateAlreadyExists(ex, certificateToAdd.SubjectName.Name, "user issuer");
                    }
                }
            }
            catch (Exception e)
            {
                LogErrorAddingCertificate(e, "user issuer");
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
                        LogCertificateAddedToStore(certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint, "trusted user");
                    }
                    catch (ArgumentException ex)
                    {
                        LogCertificateAlreadyExists(ex, certificateToAdd.SubjectName.Name, "trusted user");
                    }
                }
            }
            catch (Exception e)
            {
                LogErrorAddingCertificate(e, "trusted user");
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
            LogNoCrlSpecified();
            return false;
        }

        // validate input and create the new CRL
        LogStartingUpdateCrl();
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
                    LogInvalidBase64String(newCrlBase64String.Substring(0, 10));
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
            LogInvalidCrlData(e);
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
                        LogRemoveCrlFromStore("trusted peer");
                        trustedCrlIssuer = true;

                        var crlsToRemove = await trustedStore.EnumerateCRLsAsync(trustedCertificate, true, CancellationToken.None).ConfigureAwait(false);
                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await trustedStore.DeleteCRLAsync(crlToRemove, CancellationToken.None).ConfigureAwait(false))
                                {
                                    LogFailedToRemoveCrl(crlToRemove.Issuer, "trusted peer");
                                }
                            }
                            catch (Exception e)
                            {
                                LogErrorRemovingCrl(e, crlToRemove.Issuer, "trusted peer");
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogErrorRemovingCrlFromStore(e, "trusted peer");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await trustedStore.AddCRLAsync(newCrl, CancellationToken.None).ConfigureAwait(false);
                    LogCrlAddedToStore(newCrl.Issuer, "trusted peer");
                }
                catch (Exception e)
                {
                    LogErrorAddingCrlToStore(e, "trusted peer");
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
                        LogRemoveCrlFromStore("trusted issuer");
                        trustedCrlIssuer = true;
                        var crlsToRemove = await issuerStore.EnumerateCRLsAsync(issuerCertificate, true, CancellationToken.None).ConfigureAwait(false);

                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await issuerStore.DeleteCRLAsync(crlToRemove, CancellationToken.None).ConfigureAwait(false))
                                {
                                    LogFailedToRemoveCrl(crlToRemove.Issuer, "trusted issuer");
                                }
                            }
                            catch (Exception e)
                            {
                                LogErrorRemovingCrl(e, crlToRemove.Issuer, "trusted issuer");
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogErrorRemovingCrlFromStore(e, "trusted issuer");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await issuerStore.AddCRLAsync(newCrl, CancellationToken.None).ConfigureAwait(false);
                    LogCrlAddedToStore(newCrl.Issuer, "trusted issuer");
                }
                catch (Exception e)
                {
                    LogErrorAddingCrlToStore(e, "trusted issuer");
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
            LogNoNewCertificateData();
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
                    LogInvalidBase64String(newCertificateBase64String.Substring(0, 10));
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
            LogInvalidNewCertificateData(e);
            return false;
        }

        // validate input and create the private key
        LogStartUpdatingAppCert();
        byte[] privateKey = null;
        try
        {
            if (!string.IsNullOrEmpty(privateKeyBase64String))
            {
                privateKey = new byte[privateKeyBase64String.Length * 3 / 4];
                if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out _))
                {
                    LogInvalidBase64String(privateKeyBase64String.Substring(0, 10));
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
            LogInvalidPrivateKeyData(e);
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
            LogCurrentAppCertInfo(currentSubjectName, currentApplicationCertificate.Thumbprint);
        }
        else
        {
            LogNoExistingAppCert();
        }

        // for a cert update subject names of current and new certificate must match
        if (hasApplicationCertificate && !X509Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
        {
            LogSubjectNameMismatch(newCertificate.SubjectName.Name, currentSubjectName);
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
            LogFailedToVerifyCertIntegrity(e);
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
                LogPrivateKeyFormat("PFX");
            }
            catch (Exception ex)
            {
                LogCertFileNotFormat(ex, "PFX");
            }
        }
        // check if new cert is PEM
        if (string.IsNullOrEmpty(newCertFormat))
        {
            try
            {
                newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCertificate, privateKey, certificatePassword);
                newCertFormat = "PEM";
                LogPrivateKeyFormat("PEM");
            }
            catch (Exception ex)
            {
                LogCertFileNotFormat(ex, "PEM");
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
                    LogNoExistingCertForPrivateKey();
                }
            }
            catch (Exception ex)
            {
                LogCertFileNotFormat(ex, "DER");
            }
        }

        if (hasApplicationCertificate)
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                LogUnsupportedPrivateKeyFormat();
                return false;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                LogNoAppCertAndNoUsablePrivateKey();
                return false;
            }
        }

        // remove the existing and add the new application cert
        using (ICertificateStore appStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore(_telemetryContext))
        {
            LogRemoveExistingAppCert();
            try
            {
                if (hasApplicationCertificate && !await appStore.DeleteAsync(currentApplicationCertificate.Thumbprint, CancellationToken.None).ConfigureAwait(false))
                {
                    LogRemovingExistingCertFailed(currentApplicationCertificate.Thumbprint);
                }
            }
            catch (Exception ex)
            {
                LogFailedToRemoveExistingCert(ex);
            }
            try
            {
                await appStore.AddAsync(newCertificateWithPrivateKey, null, CancellationToken.None).ConfigureAwait(false);
                LogNewAppCertAdded(newCertificateWithPrivateKey.SubjectName.Name, newCertificateWithPrivateKey.Thumbprint);
            }
            catch (Exception e)
            {
                LogFailedToAddNewAppCert(e);
                return false;
            }
        }

        // update the application certificate
        try
        {
            LogActivatingNewAppCert(newCertificateWithPrivateKey.Thumbprint);
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificateWithPrivateKey;
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Thumbprint = newCertificateWithPrivateKey.Thumbprint;
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = newCertificateWithPrivateKey.SubjectName.Name;
            await _config.OpcUa.ApplicationConfiguration.CertificateValidator.UpdateCertificateAsync(_config.OpcUa.ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            LogFailedToActivateNewAppCert(e);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no thumbprint specified for certificates to remove. Please check your command line options.")]
    partial void LogNoThumbprintSpecified();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to remove certificate(s) from trusted peer and trusted issuer store")]
    partial void LogStartingRemoveCertificates();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove certificate with thumbprint '{Thumbprint}' from the {StoreName} store")]
    partial void LogFailedToRemoveCertificate(string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed certificate with thumbprint '{Thumbprint}' from the {StoreName} store")]
    partial void LogRemovedCertificate(string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while trying to remove certificate(s) from the {StoreName} store")]
    partial void LogErrorRemovingCertificates(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no certificate provided. Please check your command line options.")]
    partial void LogNoCertificateProvided();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to add certificate(s) to the {StoreType} store")]
    partial void LogStartingAddCertificates(string storeType);

    [LoggerMessage(Level = LogLevel.Error, Message = "The provided string '{PartialString}...' is not a valid base64 string")]
    partial void LogInvalidBase64String(string partialString);

    [LoggerMessage(Level = LogLevel.Error, Message = "The {CertType} certificate data is invalid. Please check your command line options")]
    partial void LogInvalidCertificateData(Exception exception, string certType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the {StoreName} store")]
    partial void LogCertificateAddedToStore(string subjectName, string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certificate '{SubjectName}' already exists in {StoreName} store")]
    partial void LogCertificateAlreadyExists(Exception exception, string subjectName, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while adding a certificate to the {StoreName} store")]
    partial void LogErrorAddingCertificate(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no CRL specified. Please check your command line options")]
    partial void LogNoCrlSpecified();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to update the current CRL")]
    partial void LogStartingUpdateCrl();

    [LoggerMessage(Level = LogLevel.Error, Message = "The new CRL data is invalid")]
    partial void LogInvalidCrlData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Remove the current CRL from the {StoreName} store")]
    partial void LogRemoveCrlFromStore(string storeName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove CRL issued by '{Issuer}' from the {StoreName} store")]
    partial void LogFailedToRemoveCrl(string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while removing the current CRL issued by '{Issuer}' from the {StoreName} store")]
    partial void LogErrorRemovingCrl(Exception exception, string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while removing the current CRL from the {StoreName} store")]
    partial void LogErrorRemovingCrlFromStore(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "The new CRL issued by '{Issuer}' was added to the {StoreName} store")]
    partial void LogCrlAddedToStore(string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while adding the new CRL to the {StoreName} store")]
    partial void LogErrorAddingCrlToStore(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no new application certificate data provided. Please check your command line options.")]
    partial void LogNoNewCertificateData();

    [LoggerMessage(Level = LogLevel.Error, Message = "The new application certificate data is invalid")]
    partial void LogInvalidNewCertificateData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start updating the current application certificate")]
    partial void LogStartUpdatingAppCert();

    [LoggerMessage(Level = LogLevel.Error, Message = "The private key data is invalid")]
    partial void LogInvalidPrivateKeyData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The current application certificate has SubjectName '{CurrentSubjectName}' and thumbprint '{CurrentThumbprint}'")]
    partial void LogCurrentAppCertInfo(string currentSubjectName, string currentThumbprint);

    [LoggerMessage(Level = LogLevel.Information, Message = "There is no existing application certificate")]
    partial void LogNoExistingAppCert();

    [LoggerMessage(Level = LogLevel.Error, Message = "The SubjectName '{NewSubjectName}' of the new certificate doesn't match the current certificates SubjectName '{CurrentSubjectName}'")]
    partial void LogSubjectNameMismatch(string newSubjectName, string currentSubjectName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to verify integrity of the new certificate and the trusted issuer list")]
    partial void LogFailedToVerifyCertIntegrity(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The private key for the new certificate was passed in using {Format} format")]
    partial void LogPrivateKeyFormat(string format);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Certificate file is not {Format}")]
    partial void LogCertFileNotFormat(Exception exception, string format);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format")]
    partial void LogNoExistingCertForPrivateKey();

    [LoggerMessage(Level = LogLevel.Error, Message = "The provided format of the private key is not supported (must be PEM or PFX) or the provided cert password is wrong")]
    partial void LogUnsupportedPrivateKeyFormat();

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no application certificate we can update and for the new application certificate there was not usable private key (must be PEM or PFX format) provided or the provided cert password is wrong")]
    partial void LogNoAppCertAndNoUsablePrivateKey();

    [LoggerMessage(Level = LogLevel.Information, Message = "Remove the existing application certificate")]
    partial void LogRemoveExistingAppCert();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Removing the existing application certificate with thumbprint '{CurrentThumbprint}' failed")]
    partial void LogRemovingExistingCertFailed(string currentThumbprint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the existing application certificate from the ApplicationCertificate store")]
    partial void LogFailedToRemoveExistingCert(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The new application certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the application certificate store")]
    partial void LogNewAppCertAdded(string subjectName, string thumbprint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add the new application certificate to the application certificate store")]
    partial void LogFailedToAddNewAppCert(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Activating the new application certificate with thumbprint '{NewThumbprint}'")]
    partial void LogActivatingNewAppCert(string newThumbprint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to activate the new application certificate")]
    partial void LogFailedToActivateNewAppCert(Exception exception);
}
