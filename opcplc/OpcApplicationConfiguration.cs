
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
        /// configuration info for the application
        /// </summary>
        public static ApplicationConfiguration ApplicationConfiguration { get; private set; }
        public static string PlcHostname
        {
            get => _plcHostname;
            set => _plcHostname = value.ToLowerInvariant();
        }

        public static string PlcHostnameLabel => (_plcHostname.Contains(".") ? _plcHostname.Substring(0, _plcHostname.IndexOf('.')) : _plcHostname);
        public static string ApplicationName => ProgramName;
        public static string ApplicationUri => $"urn:{ProgramName}:{PlcHostnameLabel}{(string.IsNullOrEmpty(ServerPath) ? string.Empty : (ServerPath.StartsWith("/") ? string.Empty : ":"))}{ServerPath.Replace("/", ":")}";
        public static string ProductUri => $"https://github.com/azure/iot-edge-opc-plc.git";
        public static ushort ServerPort { get; set; } = 50000;
        public static string ServerPath { get; set; } = string.Empty;

        /// <summary>
        /// default endpoint security of the application
        /// </summary>
        public static string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

        /// <summary>
        /// enables unsecure endpoint access to the application
        /// </summary>
        public static bool EnableUnsecureTransport { get; set; } = false;

        /// <summary>
        /// sets the LDS registration interval
        /// </summary>
        public static int LdsRegistrationInterval { get; set; } = 0;

        /// <summary>
        /// mapping of the application logging levels to OPC stack logging levels
        /// </summary>
        public static int OpcTraceToLoggerVerbose = 0;
        public static int OpcTraceToLoggerDebug = 0;
        public static int OpcTraceToLoggerInformation = 0;
        public static int OpcTraceToLoggerWarning = 0;
        public static int OpcTraceToLoggerError = 0;
        public static int OpcTraceToLoggerFatal = 0;

        /// <summary>
        /// set the OPC stack log level
        /// </summary>
        public static int OpcStackTraceMask { get; set; } = 0;

        /// <summary>
        /// Ctor
        /// </summary>
        public OpcApplicationConfiguration()
        {
        }

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

            var applicationCertificate = await InitApplicationSecurityAsync();
            
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
            // Add high secure transport.
            ServerSecurityPolicy newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Logger.Information($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // Add none secure transport.
            if (EnableUnsecureTransport)
            {
                newPolicy = new ServerSecurityPolicy()
                {
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None
                };
                ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
                Logger.Information($"Unsecure security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");
                Logger.Warning($"Note: This is a security risk and needs to be disabled for production use");
            }

            // MaxRegistrationInterval
            ApplicationConfiguration.ServerConfiguration.MaxRegistrationInterval = LdsRegistrationInterval;
            Logger.Information($"LDS(-ME) registration intervall set to {LdsRegistrationInterval} ms (0 means no registration)");

            // show CreateSigningRequest data
            if (ShowCreateSigningRequestInfo)
            {
                await ShowCreateSigningRequestInformationAsync(applicationCertificate);
            }

            // show certificate store information
            await ShowCertificateStoreInformationAsync();

            return ApplicationConfiguration;
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
