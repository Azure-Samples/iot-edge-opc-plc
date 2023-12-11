namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using OpcPlc.Certs;
using System;
using System.Linq;
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
    public static string ApplicationUri => $"urn:{ProgramName}:{HostnameLabel}{(string.IsNullOrEmpty(ServerPath)
        ? string.Empty
        : (ServerPath.StartsWith('/') ? string.Empty : ":"))}{ServerPath.Replace('/', ':')}";
    public static string ProductUri => "https://github.com/azure-samples/iot-edge-opc-plc";
    public static ushort ServerPort { get; set; } = 50000;
    public static string ServerPath { get; set; } = string.Empty;
    public static int MaxSessionCount { get; set; } = 100;
    public static int MaxSubscriptionCount { get; set; } = 100;
    public static int MaxQueuedRequestCount { get; set; } = 2_000;

    /// <summary>
    /// Default endpoint security of the application.
    /// </summary>
    public static string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

    /// <summary>
    /// Enables unsecure endpoint access to the application.
    /// </summary>
    public static bool EnableUnsecureTransport { get; set; } = false;

    /// <summary>
    /// Sets the LDS registration interval in milliseconds.
    /// </summary>
    public static int LdsRegistrationInterval { get; set; } = 0;

    /// <summary>
    /// Set the max string length the OPC stack supports.
    /// </summary>
    public static int OpcMaxStringLength { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Set when a flat directory certificate store should be used.
    /// </summary>
    public static bool EnableFlatDirectoryCertStore { get; set; } = false;

    /// <summary>
    /// the flat directory certificate store shall only be initialized once
    /// </summary>
    private static bool _flatDirectoryCertStoreInitialized = false;

    /// <summary>
    /// Configures all OPC stack settings.
    /// </summary>
    public async Task<ApplicationConfiguration> ConfigureAsync()
    {
        if (!_flatDirectoryCertStoreInitialized)
        {
            // Register FlatDirectoryCertificateStoreType as knows certificate store type.
            CertificateStoreType.RegisterCertificateStoreType(
                FlatDirectoryCertificateStore.StoreTypeName,
                new FlatDirectoryCertificateStoreType());
            _flatDirectoryCertStoreInitialized = true;
        }

        // instead of using a configuration XML file, configure everything programmatically
        var application = new ApplicationInstance
        {
            ApplicationName = ApplicationName, // Name in the certificate, e.g. OpcPlc.
            ApplicationType = ApplicationType.Server,
        };

        var transportQuotas = new TransportQuotas
        {
            MaxStringLength = OpcMaxStringLength,
            MaxMessageSize = 4 * 1024 * 1024, // 4 MB.
            MaxByteStringLength = 4 * 1024 * 1024, // 4 MB.
        };

        var operationLimits = new OperationLimits()
        {
            MaxMonitoredItemsPerCall = 2500,
            MaxNodesPerBrowse = 2500,
            MaxNodesPerHistoryReadData = 1000,
            MaxNodesPerHistoryReadEvents = 1000,
            MaxNodesPerHistoryUpdateData = 1000,
            MaxNodesPerHistoryUpdateEvents = 1000,
            MaxNodesPerMethodCall = 1000,
            MaxNodesPerNodeManagement = 1000,
            MaxNodesPerRead = 2500,
            MaxNodesPerWrite = 1000,
            MaxNodesPerRegisterNodes = 1000,
            MaxNodesPerTranslateBrowsePathsToNodeIds = 1000,
        };

        var alternateBaseAddresses = from dnsName in DnsNames
                                     select $"opc.tcp://{dnsName}:{ServerPort}{ServerPath}";

        // When no DNS names are configured, use the hostname as alternative name.
        if (!alternateBaseAddresses.Any())
        {
            try
            {
                alternateBaseAddresses = alternateBaseAddresses.Append($"opc.tcp://{Utils.GetHostName().ToLowerInvariant()}:{ServerPort}{ServerPath}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not get hostname.");
            }
        }

        Logger.LogInformation("Alternate base addresses (for server binding and certificate DNSNames and IPAddresses extensions): {alternateBaseAddresses}", alternateBaseAddresses);

        // configure OPC UA server
        var serverBuilder = application.Build(ApplicationUri, ProductUri)
            .SetTransportQuotas(transportQuotas)
            .AsServer(baseAddresses: new string[] {
                $"opc.tcp://{Hostname}:{ServerPort}{ServerPath}",
            },
            alternateBaseAddresses.ToArray())
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
        var securityBuilder = serverBuilder
            .SetMaxMessageQueueSize(MAX_MESSAGE_QUEUE_SIZE)
            .SetMaxNotificationQueueSize(MAX_NOTIFICATION_QUEUE_SIZE)
            .SetMaxNotificationsPerPublish(MAX_NOTIFICATIONS_PER_PUBLISH)
            .SetMaxPublishRequestCount(MAX_PUBLISH_REQUEST_COUNT)
            .SetMaxRequestThreadCount(MAX_REQUEST_THREAD_COUNT)
            // LDS registration interval.
            .SetMaxRegistrationInterval(LdsRegistrationInterval)
            // Enable auditing events and diagnostics.
            .SetDiagnosticsEnabled(true)
            .SetAuditingEnabled(true)
            // Set the server capabilities.
            .SetMaxSessionCount(MaxSessionCount)
            .SetMaxSubscriptionCount(MaxSubscriptionCount)
            .SetMaxQueuedRequestCount(MaxQueuedRequestCount)
            .SetOperationLimits(operationLimits);

        // Security configuration.
        ApplicationConfiguration = await InitApplicationSecurityAsync(securityBuilder).ConfigureAwait(false);

        foreach (var policy in ApplicationConfiguration.ServerConfiguration.SecurityPolicies)
        {
            Logger.LogInformation("Added security policy {securityPolicyUri} with mode {securityMode}",
                policy.SecurityPolicyUri,
                policy.SecurityMode);

            if (policy.SecurityMode == MessageSecurityMode.None)
            {
                Logger.LogWarning("Security policy {none} is a security risk and needs to be disabled for production use", "None");
            }
        }

        Logger.LogInformation("LDS(-ME) registration interval set to {ldsRegistrationInterval} ms (0 means no registration)",
            LdsRegistrationInterval);

        var microsoftLogger = Program.LoggerFactory.CreateLogger("OpcUa");

        // set logger interface, disables TraceEvent
        Utils.SetLogger(microsoftLogger);

        // log certificate status
        var certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
        if (certificate == null)
        {
            Logger.LogInformation("No existing application certificate found. Creating a self-signed application certificate valid since yesterday for {defaultLifeTime} months, " +
                "with a {defaultKeySize} bit key and {defaultHashSize} bit hash",
                CertificateFactory.DefaultLifeTime,
                CertificateFactory.DefaultKeySize,
                CertificateFactory.DefaultHashSize);
        }
        else
        {
            Logger.LogInformation("Application certificate with thumbprint {thumbprint} found in the application certificate store",
                certificate.Thumbprint);
        }

        // Check the certificate, create new self-signed certificate if necessary.
        bool isCertValid = await application.CheckApplicationInstanceCertificate(silent: true, CertificateFactory.DefaultKeySize, lifeTimeInMonths: CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
        if (!isCertValid)
        {
            throw new Exception("Application certificate invalid.");
        }

        if (certificate == null)
        {
            certificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            Logger.LogInformation("Application certificate with thumbprint {thumbprint} created",
                certificate.Thumbprint);
        }

        Logger.LogInformation("Application certificate is for ApplicationUri {applicationUri}, ApplicationName {applicationName} and Subject is {subject}",
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
        Logger.LogInformation("Application configured with MaxSessionCount {maxSessionCount} and MaxSubscriptionCount {maxSubscriptionCount}",
            ApplicationConfiguration.ServerConfiguration.MaxSessionCount,
            ApplicationConfiguration.ServerConfiguration.MaxSubscriptionCount);

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

    // Number of publish responses that can be cached per subscription for republish.
    // If this value is too high and if the publish responses are not acknowledged,
    // the server may run out of memory for large number of subscriptions.
    private const int MAX_MESSAGE_QUEUE_SIZE = 20;

    // Max. queue size for monitored items.
    private const int MAX_NOTIFICATION_QUEUE_SIZE = 1_000;

    // Max. number of notifications per publish response. Limit on server side.
    private const int MAX_NOTIFICATIONS_PER_PUBLISH = 2_000;

    // Max. number of publish requests per session that can be queued for processing.
    private const int MAX_PUBLISH_REQUEST_COUNT = 20;

    // Max. number of threads that can be used for processing service requests.
    // The value should be higher than MAX_PUBLISH_REQUEST_COUNT to avoid a deadlock.
    private const int MAX_REQUEST_THREAD_COUNT = 200;

    private static string _hostname = Utils.GetHostName().ToLowerInvariant();
}
