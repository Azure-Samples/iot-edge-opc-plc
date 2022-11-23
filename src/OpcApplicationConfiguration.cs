namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
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

    public static string HostnameLabel => _hostname.Contains('.')
                                            ? _hostname[.._hostname.IndexOf('.')]
                                            : _hostname;
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

        var alternateBaseAddresses = (from dnsName in DnsNames
                                      select $"opc.tcp://{dnsName}:{ServerPort}{ServerPath}")
                                     .Append($"opc.tcp://{Utils.GetHostName().ToLowerInvariant()}:{ServerPort}{ServerPath}")
                                     .ToArray();

        Logger.Information("Alternate base addresses (for server binding and certificate DNSNames and IPAddresses extensions): {alternateBaseAddresses}", alternateBaseAddresses);

        // configure OPC UA server
        var serverBuilder = application.Build(ApplicationUri, ProductUri)
            .SetTransportQuotas(transportQuotas)
            .AsServer(baseAddresses: new string[] {
                $"opc.tcp://{Hostname}:{ServerPort}{ServerPath}",
            },
            alternateBaseAddresses)
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
            Logger.Information("Added security policy {securityPolicyUri} with mode {securityMode}",
                policy.SecurityPolicyUri,
                policy.SecurityMode);

            if (policy.SecurityMode == MessageSecurityMode.None)
            {
                Logger.Warning("Security policy {none} is a security risk and needs to be disabled for production use", "None");
            }
        }

        Logger.Information("LDS(-ME) registration interval set to {ldsRegistrationInterval} ms (0 means no registration)",
            LdsRegistrationInterval);

        // configure OPC stack tracing
        Utils.SetTraceMask(OpcStackTraceMask);
        Logger.Information("The OPC UA trace mask is set to: {opcStackTraceMask}",
            $"0x{OpcStackTraceMask:X}");

        var microsoftLogger = new SerilogLoggerFactory(Logger)
            .CreateLogger("OPC");

        // set logger interface, disables TraceEvent
        Utils.SetLogger(microsoftLogger);

        // log certificate status
        var certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
        if (certificate == null)
        {
            Logger.Information("No existing application certificate found. Creating a self-signed application certificate valid since yesterday for {defaultLifeTime} months, " +
                "with a {defaultKeySize} bit key and {defaultHashSize} bit hash",
                CertificateFactory.DefaultLifeTime,
                CertificateFactory.DefaultKeySize,
                CertificateFactory.DefaultHashSize);
        }
        else
        {
            Logger.Information("Application certificate with thumbprint {thumbprint} found in the application certificate store",
                certificate.Thumbprint);
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
            Logger.Information("Application certificate with thumbprint {thumbprint} created",
                certificate.Thumbprint);
        }

        Logger.Information("Application certificate is for ApplicationUri {applicationUri}, ApplicationName {applicationName} and Subject is {subject}",
            ApplicationConfiguration.ApplicationUri,
            ApplicationConfiguration.ApplicationName,
            ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Subject);

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

    private const int MAX_MESSAGE_QUEUE_SIZE = 200000;
    private const int MAX_NOTIFICATIONS_PER_PUBLISH = 200000;
    private const int MAX_SUBSCRIPTION_COUNT = 200;
    private const int MAX_PUBLISH_REQUEST_COUNT = MAX_SUBSCRIPTION_COUNT;
    private const int MAX_REQUEST_THREAD_COUNT = MAX_PUBLISH_REQUEST_COUNT;

    private static string _hostname = Utils.GetHostName().ToLowerInvariant();
}
