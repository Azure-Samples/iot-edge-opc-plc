
using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPlc
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using static Opc.Ua.CertificateStoreType;
    using static Program;

    public partial class OpcApplicationConfiguration
    {
        /// <summary>
        /// add own certificate to trusted peer store
        /// </summary>
        public static bool TrustMyself { get; set; } = false;

        /// <summary>
        /// certficate store configuration for own, trusted peer, issuer and rejected stores
        /// </summary>
        public static string OpcOwnCertStoreType { get; set; } = X509Store;
        public static string OpcOwnCertDirectoryStorePathDefault => "pki/own";
        public static string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcOwnCertStorePath { get; set; } = OpcOwnCertX509StorePathDefault;

        public static string OpcTrustedCertDirectoryStorePathDefault => "pki/trusted";
        public static string OpcTrustedCertStorePath { get; set; } = OpcTrustedCertDirectoryStorePathDefault;

        public static string OpcRejectedCertDirectoryStorePathDefault => "pki/rejected";
        public static string OpcRejectedCertStorePath { get; set; } = OpcRejectedCertDirectoryStorePathDefault;

        public static string OpcIssuerCertDirectoryStorePathDefault => "pki/issuer";
        public static string OpcIssuerCertStorePath { get; set; } = OpcIssuerCertDirectoryStorePathDefault;

        /// <summary>
        /// accept certs of the clients automatically
        /// </summary>
        public static bool AutoAcceptCerts { get; set; } = false;

        /// <summary>
        /// show CSR information during startup
        /// </summary>
        public static bool ShowCreateSigningRequestInfo { get; set; } = false;

        /// <summary>
        /// update application certificate
        /// </summary>
        public static string NewCertificateBase64String { get; set; } = null;
        public static string NewCertificateFileName { get; set; } = null;
        public static string CertificatePassword { get; set; } = string.Empty;

        /// <summary>
        /// if there is no application cert installed we need to install the private key as well
        /// </summary>
        public static string PrivateKeyBase64String { get; set; } = null;
        public static string PrivateKeyFileName { get; set; } = null;

        /// <summary>
        /// issuer certificates to add
        /// </summary>
        public static List<string> IssuerCertificateBase64Strings = null;
        public static List<string> IssuerCertificateFileNames = null;

        /// <summary>
        /// trusted certificates to add
        /// </summary>
        public static List<string> TrustedCertificateBase64Strings = null;
        public static List<string> TrustedCertificateFileNames = null;

        /// <summary>
        /// CRL to update/install
        /// </summary>
        public static string CrlFileName { get; set; } = null;
        public static string CrlBase64String { get; set; } = null;

        /// <summary>
        /// thumbprint of certificates to delete
        /// </summary>
        public static List<string> ThumbprintsToRemove = null;

        /// <summary>
        /// Configures all OPC stack settings
        /// </summary>
        public async Task<X509Certificate2> InitApplicationSecurityAsync()
        {
            //
            // Security configuration
            //
            ApplicationConfiguration.SecurityConfiguration = new SecurityConfiguration();

            // TrustedIssuerCertificates
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = CertificateStoreType.Directory;
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = OpcIssuerCertStorePath;
            Logger.Information($"Trusted Issuer store type is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Logger.Information($"Trusted Issuer Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // TrustedPeerCertificates
            ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType = CertificateStoreType.Directory;
            if (string.IsNullOrEmpty(OpcTrustedCertStorePath))
            {
                // Set default.
                ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertDirectoryStorePathDefault;
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_TPC_SP")))
                {
                    // Use environment variable.
                    ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = Environment.GetEnvironmentVariable("_TPC_SP");
                }
            }
            else
            {
                ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertStorePath;
            }
            Logger.Information($"Trusted Peer Certificate store type is: {ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType}");
            Logger.Information($"Trusted Peer Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");

            // RejectedCertificateStore
            ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore = new CertificateTrustList();
            ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType = CertificateStoreType.Directory;
            ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath = OpcRejectedCertStorePath;

            Logger.Information($"Rejected certificate store type is: {ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Logger.Information($"Rejected Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            // AutoAcceptUntrustedCertificates
            // This is a security risk and should be set to true only for debugging purposes.
            ApplicationConfiguration.SecurityConfiguration.AutoAcceptUntrustedCertificates = false;

            // AddAppCertToTrustStore: this does only work on Application objects, here for completeness
            ApplicationConfiguration.SecurityConfiguration.AddAppCertToTrustedStore = TrustMyself;

            // RejectSHA1SignedCertificates
            // We allow SHA1 certificates for now as many OPC Servers still use them
            ApplicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates = false;
            Logger.Information($"Rejection of SHA1 signed certificates is {(ApplicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");

            // MinimunCertificatesKeySize
            // We allow a minimum key size of 1024 bit, as many OPC UA servers still use them
            ApplicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize = 1024;
            Logger.Information($"Minimum certificate key size set to {ApplicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize}");

            // Application certificate
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType = OpcOwnCertStoreType;
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath = OpcOwnCertStorePath;
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = ApplicationConfiguration.ApplicationName;
            Logger.Information($"Application Certificate store type is: {ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType}");
            Logger.Information($"Application Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath}");
            Logger.Information($"Application Certificate subject name is: {ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName}");

            // handle cert validation
            if (AutoAcceptCerts)
            {
                Logger.Warning("WARNING: Automatically accepting certificates. This is a security risk.");
                ApplicationConfiguration.SecurityConfiguration.AutoAcceptUntrustedCertificates = true;
            }
            ApplicationConfiguration.CertificateValidator = new Opc.Ua.CertificateValidator();
            ApplicationConfiguration.CertificateValidator.CertificateValidation += new Opc.Ua.CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            // update security information
            await ApplicationConfiguration.CertificateValidator.Update(ApplicationConfiguration.SecurityConfiguration);

            // remove issuer and trusted certificates with the given thumbprints
            if (ThumbprintsToRemove?.Count > 0)
            {
                if (!await RemoveCertificatesAsync(ThumbprintsToRemove))
                {
                    throw new Exception("Removing certificates failed.");
                }
            }

            // add trusted issuer certificates
            if (IssuerCertificateBase64Strings?.Count > 0 || IssuerCertificateFileNames?.Count > 0)
            {
                if (!await AddCertificatesAsync(IssuerCertificateBase64Strings, IssuerCertificateFileNames, true))
                {
                    throw new Exception("Adding trusted issuer certificate(s) failed.");
                }
            }

            // add trusted peer certificates
            if (TrustedCertificateBase64Strings?.Count > 0 || TrustedCertificateFileNames?.Count > 0)
            {
                if (!await AddCertificatesAsync(TrustedCertificateBase64Strings, TrustedCertificateFileNames, false))
                {
                    throw new Exception("Adding trusted peer certificate(s) failed.");
                }
            }

            // update CRL if requested
            if (!string.IsNullOrEmpty(CrlBase64String) || !string.IsNullOrEmpty(CrlFileName))
            {
                if (!await UpdateCrl(CrlBase64String, CrlFileName))
                {
                    throw new Exception("CRL update failed.");
                }
            }

            // update application certificate if requested
            if (!string.IsNullOrEmpty(NewCertificateBase64String) || !string.IsNullOrEmpty(NewCertificateFileName))
            {
                if (!await UpdateApplicationCertificateAsync(NewCertificateBase64String, NewCertificateFileName, CertificatePassword, PrivateKeyBase64String, PrivateKeyFileName))
                {
                        throw new Exception("Update/Setting of the application certificate failed.");
                }
            }

            // Use existing certificate, if it is there.
            X509Certificate2 certificate = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Find(true);
            if (certificate == null)
            {
                Logger.Information($"No existing Application certificate found. Create a self-signed Application certificate valid from yesterday for {CertificateFactory.defaultLifeTime} months,");
                Logger.Information($"with a {CertificateFactory.defaultKeySize} bit key and {CertificateFactory.defaultHashSize} bit hash.");
                certificate = CertificateFactory.CreateCertificate(
                    ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    ApplicationConfiguration.ApplicationUri,
                    ApplicationConfiguration.ApplicationName,
                    ApplicationConfiguration.ApplicationName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null
                    );
                ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate ?? throw new Exception("OPC UA application certificate can not be created! Cannot continue without it!");
            }
            else
            {
                Logger.Information("Application certificate found in Application Certificate Store");
            }
            ApplicationConfiguration.ApplicationUri = Utils.GetApplicationUriFromCertificate(certificate);
            Logger.Information($"Application certificate is for Application URI '{ApplicationConfiguration.ApplicationUri}', Application '{ApplicationConfiguration.ApplicationName} and has Subject '{ApplicationConfiguration.ApplicationName}'");

            // We make the default reference stack behavior configurable to put our own certificate into the trusted peer store, but only for self-signed certs.
            // Note: SecurityConfiguration.AddAppCertToTrustedStore only works for Application instance objects, which we do not have.
            if (TrustMyself)
            {
                // Ensure it is trusted
                try
                {
                    using (ICertificateStore trustedStore = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
                    {
                        Logger.Information($"Adding server certificate to trusted peer store. StorePath={ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                        X509Certificate2 publicKeyCert = new X509Certificate2(certificate.RawData);
                        await trustedStore.Add(publicKeyCert);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning(e, $"Can not add server certificate to trusted peer store. Maybe it is already there.");
                }
            }
            else
            {
                Logger.Information("Application certificate is not added to trusted peer store.");
            }

            return certificate;
        }

        /// <summary>
        /// Show information needed for the Create Signing Request process.
        /// </summary>
        public static async Task ShowCreateSigningRequestInformationAsync(X509Certificate2 certificate)
        {
            try
            {
                // we need a certificate with a private key
                certificate = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(null);
                var certificateSigningRequest = CertificateFactory.CreateSigningRequest(certificate);
                Logger.Information($"----------------------- CreateSigningRequest information ------------------");
                Logger.Information($"ApplicationUri: {ApplicationConfiguration.ApplicationUri}");
                Logger.Information($"ApplicationName: {ApplicationConfiguration.ApplicationName}");
                Logger.Information($"ApplicationType: {ApplicationConfiguration.ApplicationType}");
                Logger.Information($"ProductUri: {ApplicationConfiguration.ProductUri}");
                int serverNum = 0;
                foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
                {
                    Logger.Information($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                }
                foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.AlternateBaseAddresses)
                {
                    Logger.Information($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                }
                string[] serverCapabilities = ApplicationConfiguration.ServerConfiguration.ServerCapabilities.ToArray();
                Logger.Information($"ServerCapabilities: {string.Join(", ", serverCapabilities)}");
                Logger.Information($"CSR (base64 encoded): {Convert.ToBase64String(certificateSigningRequest)}");
                Logger.Information($"---------------------------------------------------------------------------");
                await File.WriteAllBytesAsync($"{ApplicationConfiguration.ApplicationName}.csr", certificateSigningRequest);
                Logger.Information($"Binary CSR written to '{ApplicationConfiguration.ApplicationName}.csr'");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in CSR creation");
            }
        }


        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        public static async Task ShowCertificateStoreInformationAsync()
        {
            // show trusted issuer certs
            using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore())
            {
                var certs = await certStore.Enumerate();
                int certNum = 1;
                Logger.Information($"Trusted issuer certificate store contains {certs.Count} certs");
                foreach (var cert in certs)
                {
                    Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = certStore.EnumerateCRLs();
                    int crlNum = 1;
                    Logger.Information($"Trusted issuer certificate store has {crls.Count} CRLs.");
                    foreach (var crl in certStore.EnumerateCRLs())
                    {
                        Logger.Information($"{crlNum++:D2}: Issuer '{crl.Issuer}', Next update time '{crl.NextUpdateTime}'");
                    }
                }
            }

            // show trusted peer certs
            using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
            {
                var certs = await certStore.Enumerate();
                int certNum = 1;
                Logger.Information($"Trusted peer certificate store contains {certs.Count} certs");
                foreach (var cert in certs)
                {
                    Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = certStore.EnumerateCRLs();
                    int crlNum = 1;
                    Logger.Information($"Trusted peer certificate store has {crls.Count} CRLs.");
                    foreach (var crl in certStore.EnumerateCRLs())
                    {
                        Logger.Information($"{crlNum++:D2}: Issuer '{crl.Issuer}', Next update time '{crl.NextUpdateTime}'");
                    }
                }
            }

            // show rejected peer certs
            using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.OpenStore())
            {
                var certs = await certStore.Enumerate();
                int certNum = 1;
                Logger.Information($"Rejected certificate store contains {certs.Count} certs");
                foreach (var cert in certs)
                {
                    Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                }
            }
        }

        /// <summary>
        /// Event handler to validate certificates.
        /// </summary>
        private static void CertificateValidator_CertificateValidation(Opc.Ua.CertificateValidator validator, Opc.Ua.CertificateValidationEventArgs e)
       {
            if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = AutoAcceptCerts;
                if (AutoAcceptCerts)
                {
                    Logger.Information($"Accepting Certificate: {e.Certificate.Subject}");
                }
                else
                {
                    Logger.Information($"Rejecting Certificate: {e.Certificate.Subject}");
                }
            }
        }

        /// <summary>
        /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store
        /// </summary>
        private async Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
        {
            if (thumbprintsToRemove.Count == 0)
            {
                Logger.Fatal($"There is no thumbprint specified for certificates to remove. Please check your command line options.");
                return false;
            }

            // search the trusted peer store and remove certificates with a specified thumbprint
            using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
            {
                foreach (var thumbprint in thumbprintsToRemove)
                {
                    var certToRemove = await trustedStore.FindByThumbprint(thumbprint);
                    if (certToRemove != null && certToRemove.Count > 0)
                    {
                        if (await trustedStore.Delete(thumbprint) == false)
                        {
                            Logger.Warning($"Failed to delete certificate with thumbprint '{thumbprint}' from the trusted peer store.");
                        }
                    }
                }
            }

            // search the trusted issuer store and remove certificates with a specified thumbprint
            using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
            {
                foreach (var thumbprint in thumbprintsToRemove)
                {
                    var certToRemove = await issuerStore.FindByThumbprint(thumbprint);
                    if (certToRemove != null && certToRemove.Count > 0)
                    {
                        if (await issuerStore.Delete(thumbprint) == false)
                        {
                            Logger.Warning($"Failed to delete certificate with thumbprint '{thumbprint}' from the trusted issuer store.");
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Validate and add certificates to the trusted issuer or trusted peer certificate store.
        /// </summary>
        private async Task<bool> AddCertificatesAsync(
            List<string> certificateBase64Strings,
            List<string> certificateFileNames,
            bool issuerCertificate = true)
        {
            if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
            {
                Logger.Fatal($"There is no certificate provided. Please check your command line options.");
                return false;
            }

            X509Certificate2Collection certificatesToAdd = new X509Certificate2Collection();
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
                            Logger.Fatal($"The provided string '{certificateBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, $"The issuer certificate data is invalid. Please check your command line options.");
                return false;
            }

            // add the certificate to the right store
            if (issuerCertificate)
            {
                using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                {
                    foreach (var certificateToAdd in certificatesToAdd)
                    {
                        try
                        {
                            await issuerStore.Add(certificateToAdd);
                            Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' added to trusted issuer store.");
                        }
                        catch (ArgumentException)
                        {
                            // ignore error if issuer cert already exists
                            Logger.Warning($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted issuer store.");
                        }
                    }
                }
            }
            else
            {
                using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                {
                    foreach (var certificateToAdd in certificatesToAdd)
                    {
                        try
                        {
                            await trustedStore.Add(certificateToAdd);
                            Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' added to trusted issuer store.");
                        }
                        catch (ArgumentException)
                        {
                            // ignore error if issuer cert already exists
                            Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted issuer store.");
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Update the CRL in the corresponding store.
        /// </summary>
        private async Task<bool> UpdateCrl(string newCrlBase64String, string newCrlFileName)
        {
            if (string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
            {
                Logger.Fatal($"There is no CRL specified. Please check your command line options.");
                return false;
            }

            // validate input and create the new CRL
            X509CRL newCrl;
            try
            {
                if (string.IsNullOrEmpty(newCrlFileName))
                {
                    byte[] buffer = new byte[newCrlBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(newCrlBase64String, buffer, out int written))
                    {
                        newCrl = new X509CRL(buffer);
                    }
                    else
                    {
                        Logger.Fatal($"The provided string '{newCrlBase64String.Substring(0, 10)}...' is not a valid base64 string.");
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
                Logger.Fatal(e, $"The new CRL data is invalid.");
                return false;
            }

            // check if CRL was signed by a trusted peer cert
            using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
            {
                bool trustedCrlIssuer = false;
                var trustedCertificates = await trustedStore.Enumerate();
                foreach (var trustedCertificate in trustedCertificates)
                {
                    try
                    {
                        if (Utils.CompareDistinguishedName(newCrl.Issuer, trustedCertificate.Subject) && newCrl.VerifySignature(trustedCertificate, false))
                        {
                            // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                            trustedCrlIssuer = true;
                            var crlsToRemove = trustedStore.EnumerateCRLs(trustedCertificate);
                            foreach (var crlToRemove in crlsToRemove)
                            {
                                try
                                {
                                    if (trustedStore.DeleteCRL(crlToRemove) == false)
                                    {
                                        Logger.Warning($"Failed to delete CRL issued by '{crlToRemove.Issuer}' from the trusted peer store.");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Warning(e, $"Error while deleting CRL '{crlToRemove.Issuer}' from the trusted peer store.");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Fatal(e, $"Error while deleting CRL from trusted peer store.");
                        return false;
                    }
                }
                // add the CRL if we trust the issuer
                if (trustedCrlIssuer)
                {
                    trustedStore.AddCRL(newCrl);
                }
            }

            // check if CRL was signed by a trusted issuer cert
            using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
            {
                bool trustedCrlIssuer = false;
                var issuerCertificates = await issuerStore.Enumerate();
                foreach (var issuerCertificate in issuerCertificates)
                {
                    try
                    {
                        if (Utils.CompareDistinguishedName(newCrl.Issuer, issuerCertificate.Subject) && newCrl.VerifySignature(issuerCertificate, false))
                        {
                            // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                            trustedCrlIssuer = true;
                            var crlsToRemove = issuerStore.EnumerateCRLs(issuerCertificate);
                            foreach (var crlToRemove in crlsToRemove)
                            {
                                try
                                {
                                    if (issuerStore.DeleteCRL(crlToRemove) == false)
                                    {
                                        Logger.Warning($"Failed to delete CRL issued by '{crlToRemove.Issuer}' from the trusted issuer store.");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Warning(e, $"Error while deleting CRL '{crlToRemove.Issuer}' from the trusted issuer store.");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Fatal(e, $"Error while deleting CRL from trusted issuer store.");
                        return false;
                    }
                }
                // add the CRL if we trust the issuer
                if (trustedCrlIssuer)
                {
                    issuerStore.AddCRL(newCrl);
                }
            }

            return true;
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
                Logger.Fatal($"There is no new application certificate data provided. Please check your command line options.");
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
                        Logger.Fatal($"The provided string '{newCertificateBase64String.Substring(0, 10)}...' is not a valid base64 string.");
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
                Logger.Fatal(e, $"The new application certificate data is invalid.");
                return false;
            }

            // validate input and create the private key
            byte[] privateKey = null;
            try
            {
                if (!string.IsNullOrEmpty(privateKeyBase64String))
                {
                    privateKey = new byte[privateKeyBase64String.Length * 3 / 4];
                    if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out int written))
                    {
                        Logger.Fatal($"The provided string '{privateKeyBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(privateKeyFileName))
                {
                    privateKey = await File.ReadAllBytesAsync(privateKeyFileName);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, $"The private key data is invalid.");
                return false;
            }

            // for a cert update subject names of current and new certificate must match
            bool hasApplicationCertificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate?.Certificate != null;
            string currentSubjectName = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate?.SubjectName.Name;
            if (hasApplicationCertificate && !Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
            {
                Logger.Fatal($"The SubjectName '{newCertificate.SubjectName.Name}' of the new certificate doesn't match the applications SubjectName '{currentSubjectName}'.");
                return false;
            }

            // if the new cert is not selfsigned verify with the issuer certs
            if (!Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
            {
                try
                {
                    // verify the new certificate was signed by a trusted issuer or trusted peer
                    CertificateValidator certValidator = new CertificateValidator();
                    CertificateTrustList verificationTrustList = new CertificateTrustList();
                    CertificateIdentifierCollection verificationCollection = new CertificateIdentifierCollection();
                    using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                    {
                        var certs = await issuerStore.Enumerate();
                        foreach (var cert in certs)
                        {
                            verificationCollection.Add(new CertificateIdentifier(cert));
                        }
                    }
                    using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                    {
                        var certs = await trustedStore.Enumerate();
                        foreach (var cert in certs)
                        {
                            verificationCollection.Add(new CertificateIdentifier(cert));
                        }
                    }
                    verificationTrustList.TrustedCertificates = verificationCollection;
                    certValidator.Update(verificationTrustList, verificationTrustList, null);
                    certValidator.Validate(newCertificate);
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, $"Failed to verify integrity of the new certificate and the trusted issuer list.");
                    return false;
                }
            }

            // detect format of new cert and create/update the application certificate
            X509Certificate2 newCertificateWithPrivateKey = null;
            string newCertFormat = null;
            // check if new cert is PFX
            if (string.IsNullOrEmpty(newCertFormat))
            {
                try
                {
                    X509Certificate2 certWithPrivateKey = CertificateFactory.CreateCertificateFromPKCS12(privateKey, certificatePassword);
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                    newCertFormat = "PFX";
                }
                catch
                {
                    Logger.Debug($"Certificate file is not PFX");
                }
            }
            // check if new cert is PEM
            if (string.IsNullOrEmpty(newCertFormat))
            {
                try
                {
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCertificate, privateKey, certificatePassword);
                    newCertFormat = "PEM";
                }
                catch
                {
                    Logger.Debug($"Certificate file is not PEM");
                }
            }
            if (string.IsNullOrEmpty(newCertFormat))
            {
                // check if new cert is DER and there is an existing application certificate
                try
                {
                    if (hasApplicationCertificate)
                    {
                        X509Certificate2 certWithPrivateKey = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(certificatePassword);
                        newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                        newCertFormat = "DER";
                    }
                }
                catch
                {
                    Logger.Debug($"Application certificate format is not DER");
                }
            }

            // if there is no current application cert, we need a new cert with a private key
            if (hasApplicationCertificate)
            {
                if (string.IsNullOrEmpty(newCertFormat))
                {
                    Logger.Fatal($"The provided application certificate format is not supported (must be DER or PEM or PFX format) or the provided cert password is wrong.");
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(newCertFormat))
                {
                    Logger.Fatal($"There is no application certificate we can update and the provided application certificate has no private key (must be PEM or PFX format) or the provided cert password is wrong.");
                    return false;
                }
            }

            // delete the existing and add the new application cert
            using (ICertificateStore appStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath))
            {
                try
                {
                    if (hasApplicationCertificate && !await appStore.Delete(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Thumbprint))
                    {
                        Logger.Warning($"Deletion of the existing application certificate with thumbprint '{ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Thumbprint}' failed.");
                    }
                }
                catch
                {
                }
                await appStore.Add(newCertificateWithPrivateKey);
                Logger.Information($"The new application certificate '{newCertificateWithPrivateKey.SubjectName.Name}' is now active.");
            }
            return true;
        }
    }
}
