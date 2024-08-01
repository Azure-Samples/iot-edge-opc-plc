namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

public static class MetricsHelper
{
    /// <summary>
    /// The name of the service.
    /// </summary>
    public const string ServiceName = "opc-plc";

    /// <summary>
    /// The meter for the service.
    /// </summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>
    /// Gets or sets whether the meter is enabled.
    /// </summary>
    public static bool IsEnabled { get; set; }

    private const string OPC_PLC_SESSION_COUNT_METRIC = "opc_plc_session_count";
    private const string OPC_PLC_SUBSCRIPTION_COUNT_METRIC = "opc_plc_subscription_count";
    private const string OPC_PLC_MONITORED_ITEM_COUNT_METRIC = "opc_plc_monitored_item_count";
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
                string simulationId = Environment.GetEnvironmentVariable("SIMULATION_ID");

                return string.IsNullOrEmpty(simulationId)
                    ? null
                    : simulationId;
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
                string clusterName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");

                return string.IsNullOrEmpty(clusterName)
                    ? null
                    : clusterName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static readonly IDictionary<string, object> _baseDimensions = new Dictionary<string, object>
    {
        { "kubernetes_node",    KUBERNETES_NODE ?? "node"       },
        { "role_instance",      ROLE_INSTANCE ?? "host"         },
        { "app",                "opc-plc"                       },
        { "simid",              SIMULATION_ID ?? "simulation"   },
        { "cluster",            CLUSTER_NAME ?? "cluster"       },
    };

    private static readonly UpDownCounter<int> _sessionCount = Meter.CreateUpDownCounter<int>(OPC_PLC_SESSION_COUNT_METRIC);
    private static readonly UpDownCounter<int> _subscriptionCount = Meter.CreateUpDownCounter<int>(OPC_PLC_SUBSCRIPTION_COUNT_METRIC);
    private static readonly UpDownCounter<int> _monitoredItemCount = Meter.CreateUpDownCounter<int>(OPC_PLC_MONITORED_ITEM_COUNT_METRIC);
    private static readonly Counter<int> _publishedCountWithType = Meter.CreateCounter<int>(OPC_PLC_PUBLISHED_COUNT_WITH_TYPE_METRIC);
    private static readonly Counter<int> _totalErrors = Meter.CreateCounter<int>(OPC_PLC_TOTAL_ERRORS_METRIC);

    /// <summary>
    /// Add a session count.
    /// </summary>
    public static void AddSessionCount(string sessionId, int delta = 1)
    {
        if (!IsEnabled)
        {
            return;
        }

        var dimensions = MergeWithBaseDimensions(new KeyValuePair<string, object>("session", sessionId));
        _sessionCount.Add(delta, dimensions);
    }

    /// <summary>
    /// Add a subscription count.
    /// </summary>
    public static void AddSubscriptionCount(string sessionId, string subscriptionId, int delta = 1)
    {
        if (!IsEnabled)
        {
            return;
        }

        var dimensions = MergeWithBaseDimensions(
                       new KeyValuePair<string, object>("session", sessionId),
                       new KeyValuePair<string, object>("subscription", subscriptionId));

        _subscriptionCount.Add(delta, dimensions);
    }

    /// <summary>
    /// Add a monitored item count.
    /// </summary>
    public static void AddMonitoredItemCount(int delta = 1)
    {
        if (!IsEnabled)
        {
            return;
        }

        _monitoredItemCount.Add(delta, ConvertDictionaryToKeyVaultPairArray(_baseDimensions));
    }

    /// <summary>
    /// Add a published count.
    /// </summary>
    public static void AddPublishedCount(int dataChanges, int events)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (dataChanges > 0)
        {
            var dataPointsDimensions = MergeWithBaseDimensions(
                        new KeyValuePair<string, object>("type", "data_point"));
            _publishedCountWithType.Add(dataChanges, dataPointsDimensions);
        }

        if (events > 0)
        {
            var eventsDimensions = MergeWithBaseDimensions(
                        new KeyValuePair<string, object>("type", "event"));
            _publishedCountWithType.Add(events, eventsDimensions);
        }
    }

    /// <summary>
    /// Record total errors.
    /// </summary>
    public static void RecordTotalErrors(string operation, int delta = 1)
    {
        if (!IsEnabled)
        {
            return;
        }

        var dimensions = MergeWithBaseDimensions(
            new KeyValuePair<string, object>("operation", operation));
        _totalErrors.Add(delta, dimensions);
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
        var newDimensions = new Dictionary<string, object>(_baseDimensions);
        foreach (var item in items)
        {
            newDimensions[item.Key] = item.Value;
        }
        return ConvertDictionaryToKeyVaultPairArray(newDimensions);
    }
}
