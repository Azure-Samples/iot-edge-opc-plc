namespace OpcPlc.Configuration;

using Opc.Ua;

/// <summary>
/// Class for OPC Application configuration.
/// </summary>
public partial class OpcApplicationConfiguration
{
    /// <summary>
    /// Configuration info for the OPC application.
    /// </summary>
    public ApplicationConfiguration ApplicationConfiguration { get; set; }

    public string Hostname
    {
        get => _hostname;
        set => _hostname = value.ToLowerInvariant();
    }

    public string HostnameLabel => _hostname.Contains('.')
                                        ? _hostname[.._hostname.IndexOf('.')]
                                        : _hostname;

    public string ProductUri => "https://github.com/azure-samples/iot-edge-opc-plc";

    public ushort ServerPort { get; set; } = 50000;

    public string ServerPath { get; set; } = string.Empty;

    public int MaxSessionCount { get; set; } = 100;

    public int MaxSessionTimeout { get; set; } = 3_600_000; // 1 h.

    public int MaxSubscriptionCount { get; set; } = 100;

    public int MaxQueuedRequestCount { get; set; } = 2_000;

    /// <summary>
    /// Default endpoint security of the application.
    /// </summary>
    public string ServerSecurityPolicy { get; set; } = SecurityPolicies.Basic128Rsa15;

    /// <summary>
    /// Enables unsecure endpoint access to the application.
    /// </summary>
    public bool EnableUnsecureTransport { get; set; } = false;

    /// <summary>
    /// Sets the LDS registration interval in milliseconds.
    /// </summary>
    public int LdsRegistrationInterval { get; set; } = 0;

    /// <summary>
    /// Set the max string length the OPC stack supports.
    /// </summary>
    public int OpcMaxStringLength { get; set; } = 4 * 1024 * 1024;

    // Number of publish responses that can be cached per subscription for republish.
    // If this value is too high and if the publish responses are not acknowledged,
    // the server may run out of memory for large number of subscriptions.
    public int MaxMessageQueueSize { get; } = 20;

    // Max. queue size for monitored items.
    public int MaxNotificationQueueSize { get; } = 1_000;

    // Max. number of notifications per publish response. Limit on server side.
    public int MaxNotificationsPerPublish { get; } = 2_000;

    // Max. number of publish requests per session that can be queued for processing.
    public int MaxPublishRequestPerSession { get; } = 20;

    // Max. number of threads that can be used for processing service requests.
    // The value should be higher than MaxPublishRequestPerSession to avoid a deadlock.
    public int MaxRequestThreadCount { get; } = 200;

    private string _hostname = Utils.GetHostName().ToLowerInvariant();
}
