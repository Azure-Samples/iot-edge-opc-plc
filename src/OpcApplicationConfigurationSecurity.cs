namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

/// <summary>
/// Class for OPC Application configuration. Here the security relevant configuration.
/// </summary>
public partial class OpcApplicationConfiguration
{
    private readonly Configuration _config;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public OpcApplicationConfiguration(Configuration configuration, ILogger logger, ILoggerFactory loggerFactory)
    {
        _config = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;

        OpcOwnCertStorePath = OpcOwnCertDirectoryStorePathDefault;
        OpcTrustedCertStorePath = OpcTrustedCertDirectoryStorePathDefault;
        OpcRejectedCertStorePath = OpcRejectedCertDirectoryStorePathDefault;
        OpcIssuerCertStorePath = OpcIssuerCertDirectoryStorePathDefault;
    }

    /// <summary>
    /// Add own certificate to trusted peer store.
    /// </summary>
    public bool TrustMyself { get; set; }

    /// <summary>
    /// Certificate store configuration for own, trusted peer, issuer and rejected stores.
    /// </summary>
    public string OpcOwnPKIRootDefault { get; } = "pki";
    public string OpcOwnCertStoreType { get; set; } = CertificateStoreType.Directory;
    public string OpcOwnCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "own");
    public string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
    public string OpcOwnCertStorePath { get; set; }
    public string OpcTrustedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "trusted");
    public string OpcTrustedCertStorePath { get; set; }

    public string OpcRejectedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "rejected");
    public string OpcRejectedCertStorePath { get; set; }

    public string OpcIssuerCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "issuer");
    public string OpcIssuerCertStorePath { get; set; }

    /// <summary>
    /// Accept certs of the clients automatically.
    /// </summary>
    public bool AutoAcceptCerts { get; set; }

    /// <summary>
    /// Don't reject chain validation with CA certs with unknown revocation status,
    /// e.g. when the CRL is not available or the OCSP provider is offline.
    /// The default value is <see langword="false"/>, so rejection is enabled.
    /// </summary>
    public bool DontRejectUnknownRevocationStatus { get; set; }

    /// <summary>
    /// Show CSR information during startup.
    /// </summary>
    public bool ShowCreateSigningRequestInfo { get; set; }

    /// <summary>
    /// Update application certificate.
    /// </summary>
    public string NewCertificateBase64String { get; set; }
    public string NewCertificateFileName { get; set; }
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// If there is no application cert installed we need to install the private key as well.
    /// </summary>
    public string PrivateKeyBase64String { get; set; }
    public string PrivateKeyFileName { get; set; }

    /// <summary>
    /// Issuer certificates to add.
    /// </summary>
    public List<string> IssuerCertificateBase64Strings { get; set; }
    public List<string> IssuerCertificateFileNames { get; set; }

    /// <summary>
    /// Trusted certificates to add.
    /// </summary>
    public List<string> TrustedCertificateBase64Strings { get; set; }
    public List<string> TrustedCertificateFileNames { get; set; }

    /// <summary>
    /// CRL to update/install.
    /// </summary>
    public string CrlFileName { get; set; }
    public string CrlBase64String { get; set; }

    /// <summary>
    /// Thumbprint of certificates to delete.
    /// </summary>
    public List<string> ThumbprintsToRemove { get; set; }

    /// <summary>
    /// Additional certificate DNS names.
    /// </summary>
    public List<string> DnsNames { get; set; } = new();

    /// <summary>
    /// Configures OPC stack security.
    /// </summary>
    public async Task<ApplicationConfiguration> InitApplicationSecurityAsync(IApplicationConfigurationBuilderServerOptions securityBuilder)
    {

        if (OpcOwnCertStoreType == FlatDirectoryCertificateStore.StoreTypeName)
        {
            // Register FlatDirectoryCertificateStoreType as known certificate store type.
            var certStoreTypeName = CertificateStoreType.GetCertificateStoreTypeByName(FlatDirectoryCertificateStore.StoreTypeName);

            if (certStoreTypeName is null)
            {
                CertificateStoreType.RegisterCertificateStoreType(
                    FlatDirectoryCertificateStore.StoreTypeName,
                    new FlatDirectoryCertificateStoreType());
            }
        }

        var options = securityBuilder.AddSecurityConfiguration(ApplicationName, OpcOwnPKIRootDefault)
            .SetAutoAcceptUntrustedCertificates(AutoAcceptCerts)
            .SetRejectUnknownRevocationStatus(!DontRejectUnknownRevocationStatus)
            .SetRejectSHA1SignedCertificates(false)
            .SetMinimumCertificateKeySize(1024)
            .SetAddAppCertToTrustedStore(TrustMyself);

        var securityConfiguration = ApplicationConfiguration.SecurityConfiguration;

        if (OpcOwnCertStoreType == FlatDirectoryCertificateStore.StoreTypeName)
        {
            securityConfiguration.ApplicationCertificate.StoreType = OpcOwnCertStoreType;
            securityConfiguration.ApplicationCertificate.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + OpcOwnCertStorePath;

            // configure trusted issuer certificates store
            securityConfiguration.TrustedIssuerCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.TrustedIssuerCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + OpcIssuerCertStorePath;

            // configure trusted peer certificates store
            securityConfiguration.TrustedPeerCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.TrustedPeerCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + OpcTrustedCertStorePath;

            // configure rejected certificates store
            securityConfiguration.RejectedCertificateStore.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.RejectedCertificateStore.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + OpcRejectedCertStorePath;
        }
        else
        {
            securityConfiguration.ApplicationCertificate.StoreType = OpcOwnCertStoreType;
            securityConfiguration.ApplicationCertificate.StorePath = OpcOwnCertStorePath;

            // configure trusted issuer certificates store
            securityConfiguration.TrustedIssuerCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.TrustedIssuerCertificates.StorePath = OpcIssuerCertStorePath;

            // configure trusted peer certificates store
            securityConfiguration.TrustedPeerCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertStorePath;

            // configure rejected certificates store
            securityConfiguration.RejectedCertificateStore.StoreType = CertificateStoreType.Directory;
            securityConfiguration.RejectedCertificateStore.StorePath = OpcRejectedCertStorePath;

        }

        ApplicationConfiguration = await options.Create().ConfigureAwait(false);

        _logger.LogInformation("Application Certificate store type is: {storeType}", securityConfiguration.ApplicationCertificate.StoreType);
        _logger.LogInformation("Application Certificate store path is: {storePath}", securityConfiguration.ApplicationCertificate.StorePath);

        _logger.LogInformation("Rejection of SHA1 signed certificates is {status}",
            securityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled");

        _logger.LogInformation("Minimum certificate key size set to {minimumCertificateKeySize}",
            securityConfiguration.MinimumCertificateKeySize);

        _logger.LogInformation("Trusted Issuer store type is: {storeType}", securityConfiguration.TrustedIssuerCertificates.StoreType);
        _logger.LogInformation("Trusted Issuer Certificate store path is: {storePath}", securityConfiguration.TrustedIssuerCertificates.StorePath);

        _logger.LogInformation("Trusted Peer Certificate store type is: {storeType}", securityConfiguration.TrustedPeerCertificates.StoreType);
        _logger.LogInformation("Trusted Peer Certificate store path is: {storePath}", securityConfiguration.TrustedPeerCertificates.StorePath);

        _logger.LogInformation("Rejected certificate store type is: {storeType}", securityConfiguration.RejectedCertificateStore.StoreType);
        _logger.LogInformation("Rejected Certificate store path is: {storePath}", securityConfiguration.RejectedCertificateStore.StorePath);

        // handle cert validation
        if (AutoAcceptCerts)
        {
            _logger.LogWarning("Automatically accepting all client certificates, this is a security risk!");
        }
        ApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

        // remove issuer and trusted certificates with the given thumbprints
        if (ThumbprintsToRemove?.Count > 0 &&
            !await RemoveCertificatesAsync(ThumbprintsToRemove).ConfigureAwait(false))
        {
            throw new Exception("Removing certificates failed.");
        }

        // add trusted issuer certificates
        if ((IssuerCertificateBase64Strings?.Count > 0 || IssuerCertificateFileNames?.Count > 0) &&
            !await AddCertificatesAsync(IssuerCertificateBase64Strings, IssuerCertificateFileNames, true).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted issuer certificate(s) failed.");
        }

        // add trusted peer certificates
        if ((TrustedCertificateBase64Strings?.Count > 0 || TrustedCertificateFileNames?.Count > 0) &&
            !await AddCertificatesAsync(TrustedCertificateBase64Strings, TrustedCertificateFileNames, false).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted peer certificate(s) failed.");
        }

        // update CRL if requested
        if ((!string.IsNullOrEmpty(CrlBase64String) || !string.IsNullOrEmpty(CrlFileName)) &&
            !await UpdateCrlAsync(CrlBase64String, CrlFileName).ConfigureAwait(false))
        {
            throw new Exception("CRL update failed.");
        }

        // update application certificate if requested or use the existing certificate
        if ((!string.IsNullOrEmpty(NewCertificateBase64String) || !string.IsNullOrEmpty(NewCertificateFileName)) &&
            !await UpdateApplicationCertificateAsync(NewCertificateBase64String, NewCertificateFileName, CertificatePassword, PrivateKeyBase64String, PrivateKeyFileName).ConfigureAwait(false))
        {
            throw new Exception("Update/Setting of the application certificate failed.");
        }

        return ApplicationConfiguration;
    }

    /// <summary>
    /// Show information needed for the Create Signing Request process.
    /// </summary>
    public async Task ShowCreateSigningRequestInformationAsync(X509Certificate2 certificate)
    {
        try
        {
            // we need a certificate with a private key
            if (!certificate.HasPrivateKey)
            {
                // fetch the certificate with the private key
                try
                {
                    certificate = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(null).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while loading private key");
                    return;
                }
            }
            byte[] certificateSigningRequest = null;
            try
            {
                certificateSigningRequest = CertificateFactory.CreateSigningRequest(certificate);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while creating signing request");
                return;
            }
            _logger.LogInformation("----------------------- CreateSigningRequest information ------------------");
            _logger.LogInformation("ApplicationUri: {applicationUri}", ApplicationConfiguration.ApplicationUri);
            _logger.LogInformation("ApplicationName: {applicationName}", ApplicationConfiguration.ApplicationName);
            _logger.LogInformation("ApplicationType: {applicationType}", ApplicationConfiguration.ApplicationType);
            _logger.LogInformation("ProductUri: {productUri}", ApplicationConfiguration.ProductUri);
            if (ApplicationConfiguration.ApplicationType != ApplicationType.Client)
            {
                int serverNum = 0;
                foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
                {
                    _logger.LogInformation($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                }
                foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.AlternateBaseAddresses)
                {
                    _logger.LogInformation($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                }
                string[] serverCapabilities = ApplicationConfiguration.ServerConfiguration.ServerCapabilities.ToArray();
                _logger.LogInformation($"ServerCapabilities: {string.Join(", ", serverCapabilities)}");
            }
            _logger.LogInformation("CSR (base64 encoded):");
            Console.WriteLine($"{Convert.ToBase64String(certificateSigningRequest)}");
            _logger.LogInformation("---------------------------------------------------------------------------");
            try
            {
                await File.WriteAllBytesAsync($"{ApplicationConfiguration.ApplicationName}.csr", certificateSigningRequest).ConfigureAwait(false);
                _logger.LogInformation($"Binary CSR written to '{ApplicationConfiguration.ApplicationName}.csr'");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while writing .csr file");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CSR creation");
        }
    }

    /// <summary>
    /// Show all certificates in the certificate stores.
    /// </summary>
    public async Task ShowCertificateStoreInformationAsync()
    {
        // show application certs
        try
        {
            using ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Application store contains {count} certs", certs.Count);
            foreach (var cert in certs)
            {
                _logger.LogInformation("{index}: Subject {subject} (thumbprint: {thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from application store");
        }

        // show trusted issuer certs
        try
        {
            using ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Trusted issuer store contains {count} certs", certs.Count);
            foreach (var cert in certs)
            {
                _logger.LogInformation("{index}: Subject {subject} (thumbprint: {thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("Trusted issuer store has {count} CRLs", crls.Count);
                foreach (var crl in crls)
                {
                    _logger.LogInformation("{index}: Issuer {issuer}, Next update time {nextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from trusted issuer store");
        }

        // show trusted peer certs
        try
        {
            using ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Trusted peer store contains {count} certs", certs.Count);
            foreach (var cert in certs)
            {
                _logger.LogInformation("{index}: Subject {subject} (thumbprint: {thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("Trusted peer store has {count} CRLs", crls.Count);
                foreach (var crl in crls)
                {
                    _logger.LogInformation("{index}: Issuer {issuer}, Next update time {nextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from trusted peer store");
        }

        // show rejected peer certs
        try
        {
            using ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Rejected certificate store contains {count} certs", certs.Count);
            foreach (var cert in certs)
            {
                _logger.LogInformation("{index}: Subject {subject} (thumbprint: {thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from rejected certificate store");
        }
    }

    /// <summary>
    /// Event handler to validate certificates.
    /// </summary>
    private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
    {
        if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
        {
            e.Accept = AutoAcceptCerts;
            if (AutoAcceptCerts)
            {
                _logger.LogWarning("Trusting certificate {certificateSubject} because of corresponding command line option", e.Certificate.Subject);
            }
            else
            {
                _logger.LogError(
                    "Rejecting OPC application with certificate {certificateSubject}. If you want to trust this certificate, please copy it from the directory {rejectedCertificateStore} to {trustedPeerCertificates}",
                    e.Certificate.Subject,
                    $"{ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}{Path.DirectorySeparatorChar}certs",
                    $"{ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}{Path.DirectorySeparatorChar}certs");
            }
        }
    }

    /// <summary>
    /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store.
    /// </summary>
    private async Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
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
            using ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await trustedStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await trustedStore.Delete(thumbprint).ConfigureAwait(false))
                    {
                        _logger.LogWarning($"Failed to remove certificate with thumbprint '{thumbprint}' from the trusted peer store");
                    }
                    else
                    {
                        _logger.LogInformation($"Removed certificate with thumbprint '{thumbprint}' from the trusted peer store");
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
            using ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath);
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await issuerStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await issuerStore.Delete(thumbprint).ConfigureAwait(false))
                    {
                        _logger.LogWarning($"Failed to delete certificate with thumbprint '{thumbprint}' from the trusted issuer store");
                    }
                    else
                    {
                        _logger.LogInformation($"Removed certificate with thumbprint '{thumbprint}' from the trusted issuer store");
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
    private async Task<bool> AddCertificatesAsync(
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

        _logger.LogInformation($"Starting to add certificate(s) to the {(issuerCertificate ? "trusted issuer" : "trusted peer")} store");
        var certificatesToAdd = new X509Certificate2Collection();
        try
        {
            // validate the input and build issuer cert collection
            if (certificateFileNames?.Count > 0)
            {
                foreach (var certificateFileName in certificateFileNames)
                {
                    var certificate = new X509Certificate2(certificateFileName);
                    certificatesToAdd.Add(certificate);
                }
            }
            if (certificateBase64Strings?.Count > 0)
            {
                foreach (var certificateBase64String in certificateBase64Strings)
                {
                    byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(certificateBase64String, buffer, out int written))
                    {
                        var certificate = new X509Certificate2(buffer);
                        certificatesToAdd.Add(certificate);
                    }
                    else
                    {
                        _logger.LogError($"The provided string '{certificateBase64String.Substring(0, 10)}...' is not a valid base64 string");
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
                using ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await issuerStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation($"Certificate '{certificateToAdd.SubjectName.Name}' and thumbprint '{certificateToAdd.Thumbprint}' was added to the trusted issuer store");
                    }
                    catch (ArgumentException)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted issuer store");
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
                using ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath);
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await trustedStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation($"Certificate '{certificateToAdd.SubjectName.Name}' and thumbprint '{certificateToAdd.Thumbprint}' was added to the trusted peer store");
                    }
                    catch (ArgumentException)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted peer store");
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
    /// Update the CRL in the corresponding store.
    /// </summary>
    private async Task<bool> UpdateCrlAsync(string newCrlBase64String, string newCrlFileName)
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
                if (Convert.TryFromBase64String(newCrlBase64String, buffer, out int written))
                {
                    newCrl = new X509CRL(buffer);
                }
                else
                {
                    _logger.LogError($"The provided string '{newCrlBase64String.Substring(0, 10)}...' is not a valid base64 string");
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
        using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
        {
            bool trustedCrlIssuer = false;
            var trustedCertificates = await trustedStore.Enumerate().ConfigureAwait(false);
            foreach (var trustedCertificate in trustedCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, trustedCertificate.Subject) && newCrl.VerifySignature(trustedCertificate, false))
                    {
                        // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                        _logger.LogInformation("Remove the current CRL from the trusted peer store");
                        trustedCrlIssuer = true;

                        var crlsToRemove = await trustedStore.EnumerateCRLs(trustedCertificate).ConfigureAwait(false);
                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await trustedStore.DeleteCRL(crlToRemove).ConfigureAwait(false))
                                {
                                    _logger.LogWarning($"Failed to remove CRL issued by '{crlToRemove.Issuer}' from the trusted peer store");
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, $"Error while removing the current CRL issued by '{crlToRemove.Issuer}' from the trusted peer store");
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
                    await trustedStore.AddCRL(newCrl).ConfigureAwait(false);
                    _logger.LogInformation($"The new CRL issued by '{newCrl.Issuer}' was added to the trusted peer store");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while adding the new CRL to the trusted peer store");
                    result = false;
                }
            }
        }

        // check if CRL was signed by a trusted issuer cert
        using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
        {
            bool trustedCrlIssuer = false;
            var issuerCertificates = await issuerStore.Enumerate().ConfigureAwait(false);
            foreach (var issuerCertificate in issuerCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, issuerCertificate.Subject) && newCrl.VerifySignature(issuerCertificate, false))
                    {
                        // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                        _logger.LogInformation("Remove the current CRL from the trusted issuer store");
                        trustedCrlIssuer = true;
                        var crlsToRemove = await issuerStore.EnumerateCRLs(issuerCertificate).ConfigureAwait(false);
                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await issuerStore.DeleteCRL(crlToRemove).ConfigureAwait(false))
                                {
                                    _logger.LogWarning($"Failed to remove the current CRL issued by '{crlToRemove.Issuer}' from the trusted issuer store");
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, $"Error while removing the current CRL issued by '{crlToRemove.Issuer}' from the trusted issuer store");
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
                    await issuerStore.AddCRL(newCrl).ConfigureAwait(false);
                    _logger.LogInformation($"The new CRL issued by '{newCrl.Issuer}' was added to the trusted issuer store");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error while adding the new CRL issued by '{newCrl.Issuer}' to the trusted issuer store");
                    result = false;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Validate and update the application.
    /// </summary>
    private async Task<bool> UpdateApplicationCertificateAsync(
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
                if (Convert.TryFromBase64String(newCertificateBase64String, buffer, out int written))
                {
                    newCertificate = new X509Certificate2(buffer);
                }
                else
                {
                    _logger.LogError($"The provided string '{newCertificateBase64String.Substring(0, 10)}...' is not a valid base64 string");
                    return false;
                }
            }
            else
            {
                newCertificate = new X509Certificate2(newCertificateFileName);
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
                if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out int written))
                {
                    _logger.LogError($"The provided string '{privateKeyBase64String.Substring(0, 10)}...' is not a valid base64 string");
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
        if (ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate?.Certificate != null)
        {
            hasApplicationCertificate = true;
            currentApplicationCertificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            currentSubjectName = currentApplicationCertificate.SubjectName.Name;
            _logger.LogInformation($"The current application certificate has SubjectName '{currentSubjectName}' and thumbprint '{currentApplicationCertificate.Thumbprint}'");
        }
        else
        {
            _logger.LogInformation("There is no existing application certificate");
        }

        // for a cert update subject names of current and new certificate must match
        if (hasApplicationCertificate && !X509Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
        {
            _logger.LogError($"The SubjectName '{newCertificate.SubjectName.Name}' of the new certificate doesn't match the current certificates SubjectName '{currentSubjectName}'");
            return false;
        }

        // if the new cert is not self-signed verify with the trusted peer and trusted issuer certificates
        try
        {
            if (!X509Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
            {
                // verify the new certificate was signed by a trusted issuer or trusted peer
                var certValidator = new CertificateValidator();
                var verificationTrustList = new CertificateTrustList();
                var verificationCollection = new CertificateIdentifierCollection();
                using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                {
                    var certs = await issuerStore.Enumerate().ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                {
                    var certs = await trustedStore.Enumerate().ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                verificationTrustList.TrustedCertificates = verificationCollection;
                certValidator.Update(verificationTrustList, verificationTrustList, null);
                certValidator.Validate(newCertificate);
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
            catch
            {
                _logger.LogDebug("Certificate file is not PFX");
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
            catch
            {
                _logger.LogDebug("Certificate file is not PEM");
            }
        }
        if (string.IsNullOrEmpty(newCertFormat))
        {
            // check if new cert is DER and there is an existing application certificate
            try
            {
                if (hasApplicationCertificate)
                {
                    X509Certificate2 certWithPrivateKey = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(certificatePassword).ConfigureAwait(false);
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                    newCertFormat = "DER";
                }
                else
                {
                    _logger.LogError("There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format");
                }
            }
            catch
            {
                _logger.LogDebug("Application certificate format is not DER");
            }
        }

        // if there is no current application cert, we need a new cert with a private key
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
        using (ICertificateStore appStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath))
        {
            _logger.LogInformation("Remove the existing application certificate");
            try
            {
                if (hasApplicationCertificate && !await appStore.Delete(currentApplicationCertificate.Thumbprint).ConfigureAwait(false))
                {
                    _logger.LogWarning($"Removing the existing application certificate with thumbprint '{currentApplicationCertificate.Thumbprint}' failed");
                }
            }
            catch
            {
                _logger.LogWarning("Failed to remove the existing application certificate from the ApplicationCertificate store");
            }
            try
            {
                await appStore.Add(newCertificateWithPrivateKey).ConfigureAwait(false);
                _logger.LogInformation($"The new application certificate '{newCertificateWithPrivateKey.SubjectName.Name}' and thumbprint '{newCertificateWithPrivateKey.Thumbprint}' was added to the application certificate store");
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
            _logger.LogInformation($"Activating the new application certificate with thumbprint '{newCertificateWithPrivateKey.Thumbprint}'");
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificate;
            await ApplicationConfiguration.CertificateValidator.UpdateCertificate(ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to activate the new application certificate");
            return false;
        }

        return true;
    }
}
