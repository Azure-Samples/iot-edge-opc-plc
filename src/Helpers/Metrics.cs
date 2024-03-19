namespace OpcPlc;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

public static class MetricsConfig
{
    /// <summary>
    /// The name of the service.
    /// </summary>
    public const string ServiceName = "opc-plc";

    /// <summary>
    /// The meter for the service.
    /// </summary>
    public static readonly Meter Meter = new(ServiceName);

    private const string OPC_PLC_POD_COUNT_METRIC = "opc_plc_pod_count";
    private const string OPC_PLC_SESSION_COUNT_METRIC = "opc_plc_session_count";
    private const string OPC_PLC_SUBSCRIPTION_COUNT_METRIC = "opc_plc_subscription_count";
    private const string OPC_PLC_MONITORED_ITEM_COUNT_METRIC = "opc_plc_monitored_item_count";
    private const string OPC_PLC_PUBLISHED_COUNT_METRIC = "opc_plc_published_count";
    private const string OPC_PLC_PUBLISHED_COUNT_WITH_TYPE_METRIC = "opc_plc_published_count_with_type";
    private const string OPC_PLC_TOTAL_ERRORS_METRIC = "opc_plc_total_errors";

    private static string KUBERNETES_NODE
    {
        get
        {
            return Environment.GetEnvironmentVariable("KUBERNETES_NODE");
        }
    }

    private static string ROLE_INSTANCE
    {
        get
        {
            try
            {
                return Environment.MachineName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static string SIMULATION_ID
    {
        get
        {
            try
            {
                var simulationId = Environment.GetEnvironmentVariable("SIMULATION_ID");
                if (string.IsNullOrEmpty(simulationId))
                {
                    return null;
                }

                return simulationId;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static string CLUSTER_NAME
    {
        get
        {
            try
            {
                var clusterName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");

                if (string.IsNullOrEmpty(clusterName))
                {
                    return null;
                }

                return clusterName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static readonly IDictionary<string, object> BaseDimensions = new Dictionary<string, object>
    {
        { "kubernetes_node",    KUBERNETES_NODE ?? "node"       },
        { "role_instance",      ROLE_INSTANCE ?? "host"         },
        { "app",                "opc-plc"                       },
        { "simid",              SIMULATION_ID ?? "simulation"   },
        { "cluster",            CLUSTER_NAME ?? "cluster"       },
    };

    private static readonly UpDownCounter<int> SessionCount = Meter.CreateUpDownCounter<int>(OPC_PLC_SESSION_COUNT_METRIC);
    private static readonly UpDownCounter<int> SubscriptionCount = Meter.CreateUpDownCounter<int>(OPC_PLC_SUBSCRIPTION_COUNT_METRIC);
    private static readonly UpDownCounter<int> MonitoredItemCount = Meter.CreateUpDownCounter<int>(OPC_PLC_MONITORED_ITEM_COUNT_METRIC);
    private static readonly Counter<int> PublishedCount = Meter.CreateCounter<int>(OPC_PLC_PUBLISHED_COUNT_METRIC);
    private static readonly Counter<int> PublishedCountWithType = Meter.CreateCounter<int>(OPC_PLC_PUBLISHED_COUNT_WITH_TYPE_METRIC);
    private static readonly Counter<int> TotalErrors = Meter.CreateCounter<int>(OPC_PLC_TOTAL_ERRORS_METRIC);

    private static readonly ObservableGauge<int> PodCountGauge = Meter.CreateObservableGauge<int>(OPC_PLC_POD_COUNT_METRIC, () => {
        return new Measurement<int>(1, ConvertDictionaryToKeyVaultPairArray(BaseDimensions));
    });

    /// <summary>
    /// Add a session count.
    /// </summary>
    public static void AddSessionCount(string sessionId, int delta = 1)
    {
        var dimensions = MergeWithBaseDimensions(new KeyValuePair<string, object>("session", sessionId));
        SessionCount.Add(delta, dimensions);
    }

    /// <summary>
    /// Add a subscription count.
    /// </summary>
    public static void AddSubscriptionCount(string sessionId, string subscriptionId, int delta = 1)
    {
        var dimensions = MergeWithBaseDimensions(
                       new KeyValuePair<string, object>("session", sessionId),
                       new KeyValuePair<string, object>("subscription", subscriptionId));

        SubscriptionCount.Add(delta, dimensions);
    }

    /// <summary>
    /// Add a monitored item count.
    /// </summary>
    public static void AddMonitoredItemCount(string sessionId, string subscriptionId, int delta = 1)
    {
        var dimensions = MergeWithBaseDimensions(
                        new KeyValuePair<string, object>("session", sessionId),
                        new KeyValuePair<string, object>("subscription", subscriptionId));
        MonitoredItemCount.Add(delta, dimensions);
    }

    /// <summary>
    /// Add a published count.
    /// </summary>
    public static void AddPublishedCount(string sessionId, string subscriptionId, int dataPoints, int events)
    {
        var dimensions = ConvertDictionaryToKeyVaultPairArray(BaseDimensions);
        PublishedCount.Add(1, dimensions);

        if (dataPoints > 0)
        {
            var dataPointsDimensions = MergeWithBaseDimensions(
                        new KeyValuePair<string, object>("type", "data_point"));
            PublishedCountWithType.Add(dataPoints, dataPointsDimensions);;
        }

        if (events > 0)
        {
            var eventsDimensions = MergeWithBaseDimensions(
                        new KeyValuePair<string, object>("type", "event"));
            PublishedCountWithType.Add(events, eventsDimensions);
        }
    }

    /// <summary>
    /// Record total errors.
    /// </summary>
    public static void RecordTotalErrors(string operation, string errorType, int delta = 1)
    {
        var dimensions = MergeWithBaseDimensions(
            new KeyValuePair<string, object>("operation", operation),
            new KeyValuePair<string, object>("error_type", errorType));
        TotalErrors.Add(delta, dimensions);
    }

    /// <summary>
    /// Convert a dictionary to a key value pair array.
    /// </summary>
    private static KeyValuePair<string, object>[] ConvertDictionaryToKeyVaultPairArray(IDictionary<string, object> dictionary)
    {
        return dictionary.Select(item => new KeyValuePair<string, object>(item.Key, item.Value)).ToArray();
    }

    /// <summary>
    /// Merge the base dimensions with the given dimensions.
    /// </summary>
    private static KeyValuePair<string, object>[] MergeWithBaseDimensions(params KeyValuePair<string, object>[] items)
    {
        var newDimensions = new Dictionary<string, object>(BaseDimensions);
        foreach (var item in items)
        {
            newDimensions[item.Key] = item.Value;
        }
        return ConvertDictionaryToKeyVaultPairArray(newDimensions);
    }
}
