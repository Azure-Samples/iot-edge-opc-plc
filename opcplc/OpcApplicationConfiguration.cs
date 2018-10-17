
using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPlc
{
    using System.Threading.Tasks;
    using static Opc.Ua.CertificateStoreType;
    using static Program;

    public class OpcApplicationConfiguration
    {
        public static ApplicationConfiguration ApplicationConfiguration { get; private set; }
        public static string PlcHostname
        {
            get => _plcHostname;
            set => _plcHostname = value.ToLowerInvariant();
        }

        public static string PlcHostnameLabel => (_plcHostname.Contains(".") ? _plcHostname.Substring(0, _plcHostname.IndexOf('.')) : _plcHostname);
        public static string ApplicationName => $"{_plcHostname}";

        public static string ApplicationUri => $"urn:{PlcHostnameLabel}{(string.IsNullOrEmpty(ServerPath) ? string.Empty : (ServerPath.StartsWith("/") ? string.Empty : ":"))}{ServerPath.Replace("/", ":")}";

        public static string ProductUri => $"https://github.com/hansgschossmann/iot-edge-opc-plc.git";

        public static ushort ServerPort { get; set; } = 50000;
        public static string ServerPath { get; set; } = string.Empty;
        public static bool TrustMyself { get; set; } = false;

        // Enable Utils.TraceMasks.OperationDetail to get output for IoTHub telemetry operations. Current: 0x287 (647), with OperationDetail: 0x2C7 (711)
        public static int OpcStackTraceMask { get; set; } = 0;
        public static string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;
        public static string OpcOwnCertStoreType { get; set; } = X509Store;

        public static string OpcOwnCertDirectoryStorePathDefault => "CertificateStores/own";
        public static string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcOwnCertStorePath { get; set; } = OpcOwnCertX509StorePathDefault;
        public static string OpcTrustedCertStoreType { get; set; } = Directory;

        public static string OpcTrustedCertDirectoryStorePathDefault => "CertificateStores/trusted";
        public static string OpcTrustedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcTrustedCertStorePath { get; set; } = OpcTrustedCertDirectoryStorePathDefault;
        public static string OpcRejectedCertStoreType { get; set; } = Directory;

        public static string OpcRejectedCertDirectoryStorePathDefault => "CertificateStores/rejected";
        public static string OpcRejectedCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcRejectedCertStorePath { get; set; } = OpcRejectedCertDirectoryStorePathDefault;
        public static string OpcIssuerCertStoreType { get; set; } = Directory;

        public static string OpcIssuerCertDirectoryStorePathDefault => "CertificateStores/issuers";
        public static string OpcIssuerCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcIssuerCertStorePath { get; set; } = OpcIssuerCertDirectoryStorePathDefault;
        public static int LdsRegistrationInterval { get; set; } = 0;
        public static bool AutoAcceptCerts { get; set; } = false;

        public static int OpcTraceToLoggerVerbose = 0;
        public static int OpcTraceToLoggerDebug = 0;
        public static int OpcTraceToLoggerInformation = 0;
        public static int OpcTraceToLoggerWarning = 0;
        public static int OpcTraceToLoggerError = 0;
        public static int OpcTraceToLoggerFatal = 0;

        /// <summary>
        /// Configures all OPC stack settings
        /// </summary>
        public async Task<ApplicationConfiguration> ConfigureAsync()
        {
            // Instead of using a Config.xml we configure everything programmatically.

            //
            // OPC UA Application configuration
            //
            ApplicationConfiguration = new ApplicationConfiguration();

            // Passed in as command line argument
            ApplicationConfiguration.ApplicationName = ApplicationName;
            ApplicationConfiguration.ApplicationUri = ApplicationUri;
            ApplicationConfiguration.ProductUri = ProductUri;
            ApplicationConfiguration.ApplicationType = ApplicationType.Server;

            //
            // TraceConfiguration
            //
            ApplicationConfiguration.TraceConfiguration = new TraceConfiguration();
            ApplicationConfiguration.TraceConfiguration.TraceMasks = OpcStackTraceMask;
            ApplicationConfiguration.TraceConfiguration.ApplySettings();
            Utils.Tracing.TraceEventHandler += new EventHandler<TraceEventArgs>(LoggerOpcUaTraceHandler);
            Logger.Information($"opcstacktracemask set to: 0x{OpcStackTraceMask:X}");

            //
            // Security configuration
            //
            ApplicationConfiguration.SecurityConfiguration = new SecurityConfiguration();

            // TrustedIssuerCertificates
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates = new CertificateTrustList();
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType = OpcIssuerCertStoreType;
            ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath = OpcIssuerCertStorePath;
            Logger.Information($"Trusted Issuer store type is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Logger.Information($"Trusted Issuer Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            // TrustedPeerCertificates
            ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates = new CertificateTrustList();
            ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType = OpcTrustedCertStoreType;
            if (string.IsNullOrEmpty(OpcTrustedCertStorePath))
            {
                // Set default.
                ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath = OpcTrustedCertStoreType == X509Store ? OpcTrustedCertX509StorePathDefault : OpcTrustedCertDirectoryStorePathDefault;
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
            ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType = OpcRejectedCertStoreType;
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

            // We make the default reference stack behavior configurable to put our own certificate into the trusted peer store.
            // Note: SecurityConfiguration.AddAppCertToTrustedStore only works for Application instance objects, which we do not have.
            if (TrustMyself)
            {
                // Ensure it is trusted
                try
                {
                    ICertificateStore store = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                    if (store == null)
                    {
                        Logger.Warning($"Can not open trusted peer store. StorePath={ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                    }
                    else
                    {
                        try
                        {
                            Logger.Information($"Adding server certificate to trusted peer store. StorePath={ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                            X509Certificate2 publicKey = new X509Certificate2(certificate.RawData);
                            await store.Add(publicKey);
                        }
                        finally
                        {
                            store.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, $"Can not add server certificate to trusted peer store. StorePath={ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath})");
                }
            }
            else
            {
                Logger.Warning("Server certificate is not added to trusted peer store.");
            }

            //
            // TransportConfigurations
            //
            ApplicationConfiguration.TransportQuotas = new TransportQuotas();

            //
            // ServerConfiguration
            //
            ApplicationConfiguration.ServerConfiguration = new ServerConfiguration();

            // BaseAddresses
            if (ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // We do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                ApplicationConfiguration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{PlcHostname}:{ServerPort}{ServerPath}");
            }
            foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
            {
                Logger.Information($"OPC UA server base address: {endpoint}");
            }

            // SecurityPolicies
            // We do not allow security policy SecurityPolicies.None, but always high security
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Logger.Information($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // MaxRegistrationInterval
            ApplicationConfiguration.ServerConfiguration.MaxRegistrationInterval = LdsRegistrationInterval;
            Logger.Information($"LDS(-ME) registration intervall set to {LdsRegistrationInterval} ms (0 means no registration)");

            return ApplicationConfiguration;
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
        /// Event handler to log OPC UA stack trace messages into own logger.
        /// </summary>
        private static void LoggerOpcUaTraceHandler(object sender, TraceEventArgs e)
        {
            // return fast if no trace needed
            if ((e.TraceMask & OpcStackTraceMask) == 0)
            {
                return;
            }

            // e.Exception and e.Message are always null

            // format the trace message
            string message = string.Empty;
            message = string.Format(e.Format, e.Arguments).Trim();
            message = "OPC: " + message;

            // map logging level
            if ((e.TraceMask & OpcTraceToLoggerVerbose) != 0)
            {
                Logger.Verbose(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerDebug) != 0)
            {
                Logger.Debug(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerInformation) != 0)
            {
                Logger.Information(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerWarning) != 0)
            {
                Logger.Warning(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerError) != 0)
            {
                Logger.Error(message);
                return;
            }
            if ((e.TraceMask & OpcTraceToLoggerFatal) != 0)
            {
                Logger.Fatal(message);
                return;
            }
            return;
        }

        private static string _plcHostname = $"{Utils.GetHostName().ToLowerInvariant()}";
    }
}
