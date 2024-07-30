namespace OpcPlc;

using AlarmCondition;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Bindings;
using Opc.Ua.Server;
using OpcPlc.CompanionSpecs.DI;
using OpcPlc.Configuration;
using OpcPlc.DeterministicAlarms;
using OpcPlc.PluginNodes.Models;
using OpcPlc.Reference;
using SimpleEvents;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

public partial class PlcServer : StandardServer
{
    public PlcNodeManager PlcNodeManager { get; set; }

    public AlarmConditionServerNodeManager AlarmNodeManager { get; set; }

    public SimpleEventsNodeManager SimpleEventsNodeManager { get; set; }

    public ReferenceNodeManager SimulationNodeManager { get; set; }

    public DeterministicAlarmsNodeManager DeterministicAlarmsNodeManager { get; set; }

    public readonly OpcPlcConfiguration Config;
    public readonly PlcSimulation PlcSimulation;
    public readonly TimeService TimeService;
    private readonly ImmutableList<IPluginNodes> _pluginNodes;
    private readonly ILogger _logger;
    private readonly Timer _periodicLoggingTimer;

    public PlcServer(OpcPlcConfiguration config, PlcSimulation plcSimulation, TimeService timeService, ImmutableList<IPluginNodes> pluginNodes, ILogger logger)
    {
        Config = config;
        PlcSimulation = plcSimulation;
        TimeService = timeService;
        _pluginNodes = pluginNodes;
        _logger = logger;

        _periodicLoggingTimer = new Timer(
            (state) => {
                try
                {
                    var curProc = Process.GetCurrentProcess();

                    ThreadPool.GetAvailableThreads(out int availWorkerThreads, out int availCompletionPortThreads);

                    LogPeriodicInfo(
                        ServerInternal.SessionManager.GetSessions().Count,
                        ServerInternal.SubscriptionManager.GetSubscriptions().Count,
                        ServerInternal.SubscriptionManager.GetSubscriptions().Sum(s => s.MonitoredItemCount),
                        curProc.WorkingSet64 / 1024 / 1024,
                        availWorkerThreads,
                        availCompletionPortThreads,
                        curProc.Threads.Count);
                }
                catch
                {
                    // Ignore error during logging.
                }
            },
            state: null, dueTime: TimeSpan.FromSeconds(60), period: TimeSpan.FromSeconds(60));

        MetricsHelper.IsEnabled = Config.OtlpEndpointUri is not null;
    }

    public override ResponseHeader CreateSession(
        RequestHeader requestHeader,
        ApplicationDescription clientDescription,
        string serverUri,
        string endpointUrl,
        string sessionName,
        byte[] clientNonce,
        byte[] clientCertificate,
        double requestedSessionTimeout,
        uint maxResponseMessageSize,
        out NodeId sessionId,
        out NodeId authenticationToken,
        out double revisedSessionTimeout,
        out byte[] serverNonce,
        out byte[] serverCertificate,
        out EndpointDescriptionCollection serverEndpoints,
        out SignedSoftwareCertificateCollection serverSoftwareCertificates,
        out SignatureData serverSignature,
        out uint maxRequestMessageSize)
    {
        try
        {
            var responseHeader = base.CreateSession(requestHeader, clientDescription, serverUri, endpointUrl, sessionName, clientNonce, clientCertificate, requestedSessionTimeout, maxResponseMessageSize, out sessionId, out authenticationToken, out revisedSessionTimeout, out serverNonce, out serverCertificate, out serverEndpoints, out serverSoftwareCertificates, out serverSignature, out maxRequestMessageSize);

            MetricsHelper.AddSessionCount(sessionId.ToString());

            LogSuccessWithSessionId(nameof(CreateSession), sessionId);

            return responseHeader;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateSession));

            LogError(nameof(CreateSession), ex);
            throw;
        }
    }

    public override ResponseHeader CreateSubscription(
        RequestHeader requestHeader,
        double requestedPublishingInterval,
        uint requestedLifetimeCount,
        uint requestedMaxKeepAliveCount,
        uint maxNotificationsPerPublish,
        bool publishingEnabled,
        byte priority,
        out uint subscriptionId,
        out double revisedPublishingInterval,
        out uint revisedLifetimeCount,
        out uint revisedMaxKeepAliveCount)
    {
        try
        {
            OperationContext context = ValidateRequest(requestHeader, RequestType.CreateSubscription);

            var responseHeader = base.CreateSubscription(requestHeader, requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount, maxNotificationsPerPublish, publishingEnabled, priority, out subscriptionId, out revisedPublishingInterval, out revisedLifetimeCount, out revisedMaxKeepAliveCount);

            MetricsHelper.AddSubscriptionCount(context.SessionId.ToString(), subscriptionId.ToString());

            LogSuccessWithSessionIdAndSubscriptionId(
                nameof(CreateSubscription),
                context.SessionId,
                subscriptionId);

            return responseHeader;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateSubscription));

            LogError(nameof(CreateSubscription), ex);
            throw;
        }
    }

    public override ResponseHeader CreateMonitoredItems(
        RequestHeader requestHeader,
        uint subscriptionId,
        TimestampsToReturn timestampsToReturn,
        MonitoredItemCreateRequestCollection itemsToCreate,
        out MonitoredItemCreateResultCollection results,
        out DiagnosticInfoCollection diagnosticInfos)
    {
        results = default;
        diagnosticInfos = default;

        try
        {
            OperationContext context = ValidateRequest(requestHeader, RequestType.CreateMonitoredItems);

            var responseHeader = base.CreateMonitoredItems(requestHeader, subscriptionId, timestampsToReturn, itemsToCreate, out results, out diagnosticInfos);

            MetricsHelper.AddMonitoredItemCount(itemsToCreate.Count);

            LogSuccessWithSessionIdAndSubscriptionIdAndCount(
                nameof(CreateMonitoredItems),
                context.SessionId,
                subscriptionId,
                itemsToCreate.Count);

            return responseHeader;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateMonitoredItems));

            LogError(nameof(CreateSubscription), ex);
            throw;
        }
    }

    public override ResponseHeader Publish(
        RequestHeader requestHeader,
        SubscriptionAcknowledgementCollection subscriptionAcknowledgements,
        out uint subscriptionId,
        out UInt32Collection availableSequenceNumbers,
        out bool moreNotifications,
        out NotificationMessage notificationMessage,
        out StatusCodeCollection results,
        out DiagnosticInfoCollection diagnosticInfos)
    {
        subscriptionId = default;
        availableSequenceNumbers = default;
        moreNotifications = default;
        notificationMessage = default;
        results = default;
        diagnosticInfos = default;

        try
        {
            OperationContext context = ValidateRequest(requestHeader, RequestType.Publish);

            var responseHeader = base.Publish(requestHeader, subscriptionAcknowledgements, out subscriptionId, out availableSequenceNumbers, out moreNotifications, out notificationMessage, out results, out diagnosticInfos);

            MetricsHelper.AddPublishedCount(context.SessionId.ToString(), subscriptionId.ToString(), notificationMessage, _logger);

            LogSuccessWithSessionIdAndSubscriptionId(
                nameof(Publish),
                context.SessionId,
                subscriptionId);

            return responseHeader;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNoSubscription)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                StatusCodes.BadNoSubscription.ToString(),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadNoSubscription };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSessionIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                StatusCodes.BadSessionIdInvalid.ToString(),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadSessionIdInvalid };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSecureChannelIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                StatusCodes.BadSecureChannelIdInvalid.ToString(),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadSecureChannelIdInvalid };
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogError(nameof(Publish), ex);
            throw;
        }
    }

    public override ResponseHeader Read(
        RequestHeader requestHeader,
        double maxAge,
        TimestampsToReturn timestampsToReturn,
        ReadValueIdCollection nodesToRead,
        out DataValueCollection results,
        out DiagnosticInfoCollection diagnosticInfos)
    {
        results = default;
        diagnosticInfos = default;

        try
        {
            var responseHeader = base.Read(requestHeader, maxAge, timestampsToReturn, nodesToRead, out results, out diagnosticInfos);

            LogSuccess(nameof(Read));

            return responseHeader;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(Read));

            LogError(nameof(Read), ex);
            throw;
        }
    }

    public override ResponseHeader Write(RequestHeader requestHeader, WriteValueCollection nodesToWrite, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
    {
        try
        {
            var responseHeader = base.Write(requestHeader, nodesToWrite, out results, out diagnosticInfos);

            LogSuccess(nameof(Write));

            return responseHeader;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(Write));

            LogError(nameof(Write), ex);
            throw;
        }
    }

    /// <summary>
    /// Creates the node managers for the server.
    /// </summary>
    /// <remarks>
    /// This method allows the sub-class create any additional node managers which it uses. The SDK
    /// always creates a CoreNodesManager which handles the built-in nodes defined by the specification.
    /// Any additional NodeManagers are expected to handle application specific nodes.
    /// </remarks>
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManagers = new List<INodeManager>();

        // When used via NuGet package in-memory, the server needs to use its own encodable factory.
        // Otherwise the client will not load the type definitions for decoding correctly. There is currently no public
        // API to set the encodable factory and it is not possible to provide an own implementation, because other classes
        // require the StandardServer or ServerInternalData as objects, so we need to use reflection to set it.
        var serverInternalDataField = typeof(StandardServer).GetField("m_serverInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        var encodableFactoryField = serverInternalDataField.FieldType.GetField("m_factory", BindingFlags.Instance | BindingFlags.NonPublic);
        encodableFactoryField.SetValue(server, new EncodeableFactory(false));

        // Add encodable complex types.
        server.Factory.AddEncodeableTypes(Assembly.GetExecutingAssembly());

        // Add DI node manager first so that it gets the namespace index 2.
        var diNodeManager = new DiNodeManager(server, configuration);
        nodeManagers.Add(diNodeManager);

        PlcNodeManager = new PlcNodeManager(
            server,
            Config,
            appConfig: configuration,
            TimeService,
            PlcSimulation,
            _pluginNodes,
            _logger);

        nodeManagers.Add(PlcNodeManager);

        if (PlcSimulation.AddSimpleEventsSimulation)
        {
            SimpleEventsNodeManager = new SimpleEventsNodeManager(server, configuration);
            nodeManagers.Add(SimpleEventsNodeManager);
        }

        if (PlcSimulation.AddAlarmSimulation)
        {
            AlarmNodeManager = new AlarmConditionServerNodeManager(server, configuration);
            nodeManagers.Add(AlarmNodeManager);
        }

        if (PlcSimulation.AddReferenceTestSimulation)
        {
            SimulationNodeManager = new ReferenceNodeManager(server, configuration);
            nodeManagers.Add(SimulationNodeManager);
        }

        if (PlcSimulation.DeterministicAlarmSimulationFile != null)
        {
            var scriptFileName = PlcSimulation.DeterministicAlarmSimulationFile;
            if (string.IsNullOrWhiteSpace(scriptFileName))
            {
                string errorMessage = "The script file for deterministic testing is not set (deterministicalarms)";
                LogErrorMessage(errorMessage);
                throw new Exception(errorMessage);
            }
            if (!File.Exists(scriptFileName))
            {
                string errorMessage = $"The script file ({scriptFileName}) for deterministic testing does not exist";
                LogErrorMessage(errorMessage);
                throw new Exception(errorMessage);
            }

            DeterministicAlarmsNodeManager = new DeterministicAlarmsNodeManager(server, configuration, TimeService, scriptFileName, _logger);
            nodeManagers.Add(DeterministicAlarmsNodeManager);
        }

        var masterNodeManager = new MasterNodeManager(server, configuration, dynamicNamespaceUri: null, nodeManagers.ToArray());

        return masterNodeManager;
    }

    /// <summary>
    /// Loads the non-configurable properties for the application.
    /// </summary>
    /// <remarks>
    /// These properties are exposed by the server but cannot be changed by administrators.
    /// </remarks>
    protected override ServerProperties LoadServerProperties()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        string opcPlcBuildNumber = fileVersion.ProductVersion[(fileVersion.ProductVersion.IndexOf('+') + 1)..];
        string opcUaSdkVersion = Utils.GetAssemblySoftwareVersion();
        string opcUaSdkBuildNumber = opcUaSdkVersion[(opcUaSdkVersion.IndexOf('+') + 1)..];

        var properties = new ServerProperties {
            ManufacturerName = "Microsoft",
            ProductName = "IoT Edge OPC UA PLC",
            ProductUri = "https://github.com/Azure-Samples/iot-edge-opc-plc",
            SoftwareVersion = $"{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart} (OPC UA SDK {Utils.GetAssemblyBuildNumber()})",
            BuildNumber = $"{opcPlcBuildNumber} (OPC UA SDK {opcUaSdkBuildNumber} from {Utils.GetAssemblyTimestamp():yyyy-MM-ddTHH:mm:ssZ})",
            BuildDate = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location),
        };

        return properties;
    }

    /// <summary>
    /// Creates the resource manager for the server.
    /// </summary>
    protected override ResourceManager CreateResourceManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var resourceManager = new ResourceManager(server, configuration);

        FieldInfo[] fields = typeof(StatusCodes).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            uint? id = field.GetValue(typeof(StatusCodes)) as uint?;

            if (id != null)
            {
                resourceManager.Add(id.Value, locale: string.Empty, field.Name); // Empty locale name: Invariant.
            }
        }

        return resourceManager;
    }

    /// <summary>
    /// Initializes the server before it starts up.
    /// </summary>
    protected override void OnServerStarting(ApplicationConfiguration configuration)
    {
        base.OnServerStarting(configuration);

        // it is up to the application to decide how to validate user identity tokens.
        // this function creates validator for X509 identity tokens.
        CreateUserIdentityValidators(configuration);
    }

    protected override void OnServerStarted(IServerInternal server)
    {
        // start the simulation
        base.OnServerStarted(server);

        // request notifications when the user identity is changed, all valid users are accepted by default.
        server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);
    }

    /// <inheritdoc/>
    protected override void ProcessRequest(IEndpointIncomingRequest request, object calldata)
    {
        if (request is IAsyncResult asyncResult &&
            asyncResult.AsyncState is object[] asyncStateArray &&
            asyncStateArray[0] is TcpServerChannel channel)
        {
            using var scope = _logger.BeginScope("ChannelId:\"{ChannelId}\"", channel.Id);
            base.ProcessRequest(request, calldata);
        }
        else
        {
            base.ProcessRequest(request, calldata);
        }
    }

    /// <summary>
    /// Cleans up before the server shuts down.
    /// </summary>
    /// <remarks>
    /// This method is called before any shutdown processing occurs.
    /// </remarks>
    protected override void OnServerStopping()
    {
        try
        {
            // check for connected clients
            IList<Session> currentSessions = ServerInternal.SessionManager.GetSessions();

            if (currentSessions.Count > 0)
            {
                // provide some time for the connected clients to detect the shutdown state.
                ServerInternal.Status.Value.ShutdownReason = new LocalizedText(string.Empty, "Application closed."); // Invariant.
                ServerInternal.Status.Variable.ShutdownReason.Value = new LocalizedText(string.Empty, "Application closed."); // Invariant.
                ServerInternal.Status.Value.State = ServerState.Shutdown;
                ServerInternal.Status.Variable.State.Value = ServerState.Shutdown;
                ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                for (uint secondsUntilShutdown = PlcShutdownWaitSeconds; secondsUntilShutdown > 0; secondsUntilShutdown--)
                {
                    ServerInternal.Status.Value.SecondsTillShutdown = secondsUntilShutdown;
                    ServerInternal.Status.Variable.SecondsTillShutdown.Value = secondsUntilShutdown;
                    ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, includeChildren: true);

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch
        {
            // ignore error during shutdown procedure
        }

        base.OnServerStopping();
    }

    private const uint PlcShutdownWaitSeconds = 10;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "\n\t# Open sessions: {Sessions}\n" +
                  "\t# Open subscriptions: {Subscriptions}\n" +
                  "\t# Monitored items: {MonitoredItems:N0}\n" +
                  "\t# Working set: {WorkingSet:N0} MB\n" +
                  "\t# Available worker threads: {AvailWorkerThreads:N0}\n" +
                  "\t# Available completion port threads: {AvailCompletionPortThreads:N0}\n" +
                  "\t# Thread count: {ThreadCount:N0}")]
    partial void LogPeriodicInfo(int sessions, int subscriptions, int monitoredItems, long workingSet, int availWorkerThreads, int availCompletionPortThreads, int threadCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Function} completed successfully")]
    partial void LogSuccess(string function);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Function} completed successfully with sessionId: {SessionId}")]
    partial void LogSuccessWithSessionId(string function, NodeId sessionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Function} completed successfully with sessionId: {SessionId} and subscriptionId: {SubscriptionId}")]
    partial void LogSuccessWithSessionIdAndSubscriptionId(string function, NodeId sessionId, uint subscriptionId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Function} completed successfully with sessionId: {SessionId}, subscriptionId: {SubscriptionId} and count: {Count}")]
    partial void LogSuccessWithSessionIdAndSubscriptionIdAndCount(string function, NodeId sessionId, uint subscriptionId, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{Function} error")]
    partial void LogError(string function, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{Function} error: {StatusCode}")]
    partial void LogErrorWithStatusCode(string function, string statusCode, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{message}")]
    partial void LogErrorMessage(string message);
}
