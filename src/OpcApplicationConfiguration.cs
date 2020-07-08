namespace OpcPlc
{
    using Opc.Ua;
    using System;
    using System.Threading.Tasks;
    using static Program;

    /// <summary>
    /// Class for OPC Application configuration.
    /// </summary>
    public partial class OpcApplicationConfiguration
    {
        /// <summary>
        /// Configuration info for the OPC application.
        /// </summary>
        public static ApplicationConfiguration ApplicationConfiguration { get; private set; }
        public static string Hostname
        {
            get => _hostname;
            set => _hostname = value.ToLowerInvariant();
        }

        public static string HostnameLabel => (_hostname.Contains(".") ? _hostname.Substring(0, _hostname.IndexOf('.')) : _hostname);
        public static string ApplicationName => ProgramName;
        public static string ApplicationUri => $"urn:{ProgramName}:{HostnameLabel}{(string.IsNullOrEmpty(ServerPath) ? string.Empty : (ServerPath.StartsWith("/") ? string.Empty : ":"))}{ServerPath.Replace("/", ":")}";
        public static string ProductUri => $"https://github.com/azure-samples/iot-edge-opc-plc";
        public static ushort ServerPort { get; set; } = 50000;
        public static string ServerPath { get; set; } = string.Empty;

        /// <summary>
        /// Default endpoint security of the application.
        /// </summary>
        public static string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

        /// <summary>
        /// Enables unsecure endpoint access to the application.
        /// </summary>
        public static bool EnableUnsecureTransport { get; set; } = false;

        /// <summary>
        /// Sets the LDS registration interval.
        /// </summary>
        public static int LdsRegistrationInterval { get; set; } = 0;

        /// <summary>
        /// Set the max string length the OPC stack supports.
        /// </summary>
        public static int OpcMaxStringLength { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// <summary>
        /// Mapping of the application logging levels to OPC stack logging levels.
        /// </summary>
        public static int OpcTraceToLoggerVerbose = 0;
        public static int OpcTraceToLoggerDebug = 0;
        public static int OpcTraceToLoggerInformation = 0;
        public static int OpcTraceToLoggerWarning = 0;
        public static int OpcTraceToLoggerError = 0;
        public static int OpcTraceToLoggerFatal = 0;

        /// <summary>
        /// Set the OPC stack log level.
        /// </summary>
        public static int OpcStackTraceMask { get; set; } = 0;

        /// <summary>
        /// Ctor of the OPC application configuration.
        /// </summary>
        public OpcApplicationConfiguration()
        {
        }

        /// <summary>
        /// Configures all OPC stack settings.
        /// </summary>
        public async Task<ApplicationConfiguration> ConfigureAsync()
        {
            // instead of using a configuration XML file, we configure everything programmatically

            // passed in as command line argument
            ApplicationConfiguration = new ApplicationConfiguration
            {
                ApplicationName = ApplicationName,
                ApplicationUri = ApplicationUri,
                ProductUri = ProductUri,
                ApplicationType = ApplicationType.Server,

                // configure OPC stack tracing
                TraceConfiguration = new TraceConfiguration()
            };
            ApplicationConfiguration.TraceConfiguration.TraceMasks = OpcStackTraceMask;
            ApplicationConfiguration.TraceConfiguration.ApplySettings();
            Utils.Tracing.TraceEventHandler += new EventHandler<TraceEventArgs>(LoggerOpcUaTraceHandler);
            Logger.Information($"opcstacktracemask set to: 0x{OpcStackTraceMask:X}");

            // configure transport settings
            ApplicationConfiguration.TransportQuotas = new TransportQuotas
            {
                MaxStringLength = OpcMaxStringLength,
                MaxMessageSize = 4 * 1024 * 1024
            };

            // configure OPC UA server
            ApplicationConfiguration.ServerConfiguration = new ServerConfiguration();

            // configure server base addresses
            if (ApplicationConfiguration.ServerConfiguration.BaseAddresses.Count == 0)
            {
                // we do not use the localhost replacement mechanism of the configuration loading, to immediately show the base address here
                ApplicationConfiguration.ServerConfiguration.BaseAddresses.Add($"opc.tcp://{Hostname}:{ServerPort}{ServerPath}");
            }
            foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
            {
                Logger.Information($"OPC UA server base address: {endpoint}");
            }

            // by default use high secure transport
            var newPolicy = new ServerSecurityPolicy()
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            };
            ApplicationConfiguration.ServerConfiguration.SecurityPolicies.Add(newPolicy);
            Logger.Information($"Security policy {newPolicy.SecurityPolicyUri} with mode {newPolicy.SecurityMode} added");

            // add user token policies
            var userTokenPolicies = new UserTokenPolicyCollection();

            if (!DisableAnonymousAuth)
            {
                userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.Anonymous));
            }

            if (!DisableUsernamePasswordAuth)
            {
                userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.UserName));
            }

            if (!DisableCertAuth)
            {
                userTokenPolicies.Add(new UserTokenPolicy(UserTokenType.Certificate));
            }

            ApplicationConfiguration.ServerConfiguration.UserTokenPolicies = userTokenPolicies;

            // add none secure transport on request
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

            // security configuration
            await InitApplicationSecurityAsync().ConfigureAwait(false);

            // set LDS registration interval
            ApplicationConfiguration.ServerConfiguration.MaxRegistrationInterval = LdsRegistrationInterval;
            Logger.Information($"LDS(-ME) registration interval set to {LdsRegistrationInterval} ms (0 means no registration)");

            // show certificate store information
            await ShowCertificateStoreInformationAsync().ConfigureAwait(false);

            // Support larger number of nodes.
            ApplicationConfiguration.ServerConfiguration.MaxMessageQueueSize = MAX_MESSAGE_QUEUE_SIZE;
            ApplicationConfiguration.ServerConfiguration.MaxNotificationsPerPublish = MAX_NOTIFICATIONS_PER_PUBLISH;
            ApplicationConfiguration.ServerConfiguration.MaxSubscriptionCount = MAX_SUBSCRIPTION_COUNT;

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
            string message = string.Format(e.Format, e.Arguments).Trim();
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

        private const int MAX_MESSAGE_QUEUE_SIZE = 200000;
        private const int MAX_NOTIFICATIONS_PER_PUBLISH = 200000;
        private const int MAX_SUBSCRIPTION_COUNT = 200;

        private static string _hostname = $"{Utils.GetHostName().ToLowerInvariant()}";
    }
}
