namespace OpcPlc;
using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Globalization;
using System.Text;
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
    public static string ProductUri => "https://github.com/azure-samples/iot-edge-opc-plc";
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
        // instead of using a configuration XML file, configure everything programmatically
        var application = new ApplicationInstance
        {
            ApplicationName = ApplicationName,
            ApplicationType = ApplicationType.Server,
        };

        var transportQuotas = new TransportQuotas
        {
            MaxStringLength = OpcMaxStringLength,
            MaxMessageSize = 4 * 1024 * 1024,
            MaxByteStringLength = 4 * 1024 * 1024,
        };

        // configure OPC UA server
        var serverBuilder = application.Build(ApplicationUri, ProductUri)
            .SetTransportQuotas(transportQuotas)
            .AsServer(new string[] { $"opc.tcp://{Hostname}:{ServerPort}{ServerPath}" })
            .AddSignAndEncryptPolicies()
            .AddSignPolicies();

        // use backdoor to access app config used by builder
        ApplicationConfiguration = application.ApplicationConfiguration;

        if (EnableUnsecureTransport)
        {
            serverBuilder.AddUnsecurePolicyNone();
        }

        ConfigureUserTokenPolicies(serverBuilder);

        // Support larger number of nodes.
        var securityBuilder = serverBuilder.SetMaxMessageQueueSize(MAX_MESSAGE_QUEUE_SIZE)
            .SetMaxNotificationsPerPublish(MAX_NOTIFICATIONS_PER_PUBLISH)
            .SetMaxSubscriptionCount(MAX_SUBSCRIPTION_COUNT)
            .SetMaxPublishRequestCount(MAX_PUBLISH_REQUEST_COUNT)
            .SetMaxRequestThreadCount(MAX_REQUEST_THREAD_COUNT)
            // LDS registration interval
            .SetMaxRegistrationInterval(LdsRegistrationInterval);

        // security configuration
        ApplicationConfiguration = await InitApplicationSecurityAsync(securityBuilder).ConfigureAwait(false);

        foreach (var policy in ApplicationConfiguration.ServerConfiguration.SecurityPolicies)
        {
            Logger.Information($"Added security policy {policy.SecurityPolicyUri} with mode {policy.SecurityMode}.");
            if (policy.SecurityMode == MessageSecurityMode.None)
            {
                Logger.Warning("Note: security policy 'None' is a security risk and needs to be disabled for production use");
            }
        }

        Logger.Information($"LDS(-ME) registration interval set to {LdsRegistrationInterval} ms (0 means no registration)");

        // configure OPC stack tracing
        Utils.SetTraceMask(OpcStackTraceMask);
        Utils.Tracing.TraceEventHandler += LoggerOpcUaTraceHandler;
        Logger.Information($"The OPC UA trace mask is set to: 0x{OpcStackTraceMask:X}");

        // log certificate status
        var certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
        if (certificate == null)
        {
            Logger.Information($"No existing application certificate found. Creating a self-signed application certificate valid since yesterday for {CertificateFactory.DefaultLifeTime} months," +
                $"with a {CertificateFactory.DefaultKeySize} bit key and {CertificateFactory.DefaultHashSize} bit hash.");
        }
        else
        {
            Logger.Information($"Application certificate with thumbprint '{certificate.Thumbprint}' found in the application certificate store.");
        }

        // check the certificate, creates new self signed certificate if required
        bool isCertValid = await application.CheckApplicationInstanceCertificate(silent: true, CertificateFactory.DefaultKeySize, CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
        if (!isCertValid)
        {
            throw new Exception("Application certificate invalid.");
        }

        if (certificate == null)
        {
            certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            Logger.Information($"Application certificate with thumbprint '{certificate.Thumbprint}' created.");
        }

        Logger.Information($"Application certificate is for ApplicationUri '{ApplicationConfiguration.ApplicationUri}', ApplicationName '{ApplicationConfiguration.ApplicationName}' and Subject is '{ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Subject}'");

        // show CreateSigningRequest data
        if (ShowCreateSigningRequestInfo)
        {
            await ShowCreateSigningRequestInformationAsync(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate).ConfigureAwait(false);
        }

        // show certificate store information
        await ShowCertificateStoreInformationAsync().ConfigureAwait(false);

        return ApplicationConfiguration;
    }

    private static void ConfigureUserTokenPolicies(IApplicationConfigurationBuilderServerSelected serverBuilder)
    {
        if (!DisableAnonymousAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.Anonymous));
        }

        if (!DisableUsernamePasswordAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.UserName));
        }

        if (!DisableCertAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.Certificate));
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

        // e.Exception and e.Message are special
        if (e.Exception != null)
        {
            Logger.Error(e.Exception, e.Format, e.Arguments);
            return;
        }

        // format the trace message
        var builder = new StringBuilder("OPC: ");
        builder.AppendFormat(CultureInfo.InvariantCulture, e.Format, e.Arguments);
        var message = builder.ToString().Trim();

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
    private const int MAX_PUBLISH_REQUEST_COUNT = MAX_SUBSCRIPTION_COUNT;
    private const int MAX_REQUEST_THREAD_COUNT = MAX_PUBLISH_REQUEST_COUNT;

    private static string _hostname = $"{Utils.GetHostName().ToLowerInvariant()}";
}
