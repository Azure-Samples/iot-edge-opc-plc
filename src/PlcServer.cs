namespace OpcPlc;

using global::AlarmCondition;
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
using System.Threading.Tasks;

public partial class PlcServer : StandardServer
{
    private const uint PlcShutdownWaitSeconds = 10;
    private const int PeriodicLoggingTimerSeconds = 60;

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
    private CancellationTokenSource _chaosCts;
    private Task _chaosMode;

    private bool _autoDisablePublishMetrics;
    private uint _countCreateSession;
    private uint _countCreateSubscription;
    private uint _countCreateMonitoredItems;
    private uint _countRead;
    private uint _countWrite;
    private uint _countPublish;

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

                    ThreadPool.GetAvailableThreads(out int availWorkerThreads, out _);

                    uint sessionCount = ServerInternal.ServerDiagnostics.CurrentSessionCount;
                    IList<Subscription> subscriptions = ServerInternal.SubscriptionManager.GetSubscriptions();
                    int monitoredItemsCount = subscriptions.Sum(s => s.MonitoredItemCount);

                    _autoDisablePublishMetrics = sessionCount > 40 || monitoredItemsCount > 500;

                    LogPeriodicInfo(
                        sessionCount,
                        ServerInternal.ServerDiagnostics.CurrentSubscriptionCount,
                        monitoredItemsCount,
                        ServerInternal.ServerDiagnostics.CumulatedSessionCount,
                        ServerInternal.ServerDiagnostics.CumulatedSubscriptionCount,
                        curProc.WorkingSet64 / 1024 / 1024,
                        curProc.Threads.Count,
                        availWorkerThreads,
                        PeriodicLoggingTimerSeconds,
                        _countCreateSession,
                        _countCreateSubscription,
                        _countCreateMonitoredItems,
                        _countRead,
                        _countWrite,
                        _countPublish,
                        PublishMetricsEnabled);

                    _countCreateSession = 0;
                    _countCreateSubscription = 0;
                    _countCreateMonitoredItems = 0;
                    _countRead = 0;
                    _countWrite = 0;
                    _countPublish = 0;
                }
                catch
                {
                    // Ignore error during logging.
                }
            },
            state: null, dueTime: TimeSpan.FromSeconds(PeriodicLoggingTimerSeconds), period: TimeSpan.FromSeconds(PeriodicLoggingTimerSeconds));

        MetricsHelper.IsEnabled = Config.OtlpEndpointUri is not null;
    }

    /// <summary>
    /// Enable publish requests metrics only if the following apply:
    /// 1) Metrics are enabled by specifying OtlpEndpointUri,
    /// 2) OtlpPublishMetrics is "enable",
    /// 3) OtlpPublishMetrics is not "disable",
    /// 4) When OtlpPublishMetrics is "auto": sessions <= 40 and monitored items <= 500.
    /// </summary>
    private bool PublishMetricsEnabled =>
        MetricsHelper.IsEnabled &&
        (
         (Config.OtlpPublishMetrics == "enable" && Config.OtlpPublishMetrics != "disable") ||
         (Config.OtlpPublishMetrics == "auto" && !_autoDisablePublishMetrics)
        );

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
        _countCreateSession++;

        try
        {
            var responseHeader = base.CreateSession(requestHeader, clientDescription, serverUri, endpointUrl, sessionName, clientNonce, clientCertificate, requestedSessionTimeout, maxResponseMessageSize, out sessionId, out authenticationToken, out revisedSessionTimeout, out serverNonce, out serverCertificate, out serverEndpoints, out serverSoftwareCertificates, out serverSignature, out maxRequestMessageSize);

            MetricsHelper.AddSessionCount(sessionId.ToString());

            LogSuccessWithSessionId(nameof(CreateSession), sessionId);

            return responseHeader;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadServerHalted)
        {
            // Handle when a client attempts to reconnect while the server is still starting up or halting.
            LogCreateSessionWhileHalted();

            sessionId = null;
            authenticationToken = null;
            revisedSessionTimeout = 0;
            serverNonce = Array.Empty<byte>();
            serverCertificate = Array.Empty<byte>();
            serverEndpoints = new EndpointDescriptionCollection();
            serverSoftwareCertificates = new SignedSoftwareCertificateCollection();
            serverSignature = new SignatureData();
            maxRequestMessageSize = 0;

            return new ResponseHeader { ServiceResult = StatusCodes.BadServerHalted };
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
        _countCreateSubscription++;

        try
        {
            var responseHeader = base.CreateSubscription(requestHeader, requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount, maxNotificationsPerPublish, publishingEnabled, priority, out subscriptionId, out revisedPublishingInterval, out revisedLifetimeCount, out revisedMaxKeepAliveCount);

            NodeId sessionId = GetSessionId(requestHeader.AuthenticationToken);
            MetricsHelper.AddSubscriptionCount(sessionId.ToString(), subscriptionId.ToString());

            LogSuccessWithSessionIdAndSubscriptionId(
                nameof(CreateSubscription),
                sessionId,
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
        _countCreateMonitoredItems += (uint)itemsToCreate.Count;

        results = default;
        diagnosticInfos = default;

        try
        {
            var responseHeader = base.CreateMonitoredItems(requestHeader, subscriptionId, timestampsToReturn, itemsToCreate, out results, out diagnosticInfos);

            MetricsHelper.AddMonitoredItemCount(itemsToCreate.Count);

            // Only log items with good status codes.
            var successfulItems = itemsToCreate
                .Zip(results, (request, result) => new { Request = request, Result = result })
                .Where(item => StatusCode.IsGood(item.Result.StatusCode))
                .Select(item => item.Request.ItemToMonitor.NodeId)
                .ToList();

            if (successfulItems.Any())
            {
                LogCreatedMonitoredItems(
                    GetSessionName(requestHeader.AuthenticationToken),
                    string.Join(", ", successfulItems));
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogSuccessWithSessionIdAndSubscriptionIdAndCount(
                    nameof(CreateMonitoredItems),
                    GetSessionId(requestHeader.AuthenticationToken),
                    subscriptionId,
                    itemsToCreate.Count);
            }

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
        _countPublish++;

        subscriptionId = default;
        availableSequenceNumbers = default;
        moreNotifications = default;
        notificationMessage = default;
        results = default;
        diagnosticInfos = default;

        try
        {
            var responseHeader = base.Publish(requestHeader, subscriptionAcknowledgements, out subscriptionId, out availableSequenceNumbers, out moreNotifications, out notificationMessage, out results, out diagnosticInfos);

            if (PublishMetricsEnabled)
            {
                AddPublishMetrics(notificationMessage);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogSuccessWithSessionIdAndSubscriptionId(
                    nameof(Publish),
                    GetSessionId(requestHeader.AuthenticationToken),
                    subscriptionId);
            }

            return responseHeader;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNoSubscription)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadNoSubscription),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadNoSubscription };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSessionIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSessionIdInvalid),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadSessionIdInvalid };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSecureChannelIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSecureChannelIdInvalid),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadSecureChannelIdInvalid };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSessionClosed)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSessionClosed),
                ex);

            return new ResponseHeader { ServiceResult = StatusCodes.BadSessionClosed };
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
        _countRead++;

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
        _countWrite++;

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
        if (serverInternalDataField != null)
        {
            var encodableFactoryField = serverInternalDataField.FieldType.GetField("m_factory", BindingFlags.Instance | BindingFlags.NonPublic);
            if (encodableFactoryField != null)
            {
                encodableFactoryField.SetValue(server, new EncodeableFactory(false));
            }
        }

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
                throw new ArgumentNullException(errorMessage);
            }
            if (!File.Exists(scriptFileName))
            {
                string errorMessage = $"The script file ({scriptFileName}) for deterministic testing does not exist";
                LogErrorMessage(errorMessage);
                throw new InvalidOperationException(errorMessage);
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
        CreateUserIdentityValidatorAsync(configuration).GetAwaiter().GetResult();
    }

    protected override void OnServerStarted(IServerInternal server)
    {
        // start the simulation
        base.OnServerStarted(server);

        // request notifications when the user identity is changed, all valid users are accepted by default.
        server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);

        if (Config.RunInChaosMode)
        {
            LogStartChaos();
            Chaos = true;
        }
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
                // Provide some time for the connected clients to detect the shutdown state.
                // For newest stack: var shutdownReason = new LocalizedText(string.Empty, "Application closed."); // Invariant.

                for (uint secondsUntilShutdown = PlcShutdownWaitSeconds; secondsUntilShutdown > 0; secondsUntilShutdown--)
                {
                    ServerInternal.Status.Value.SecondsTillShutdown = secondsUntilShutdown;
                    ServerInternal.Status.Variable.SecondsTillShutdown.Value = secondsUntilShutdown;
                    ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, includeChildren: true);

                    // For newest stack:
                    //ServerInternal.UpdateServerStatus(status => {
                    //    status.Value.State = ServerState.Shutdown;
                    //    status.Value.ShutdownReason = shutdownReason;
                    //    status.Value.SecondsTillShutdown = secondsUntilShutdown;
                    //});

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
        catch
        {
            // ignore error during shutdown procedure
        }

        base.OnServerStopping();

        if (Config.RunInChaosMode)
        {
            Chaos = false;
            LogChaosModeStopped();
        }
    }

    /// <summary>
    /// Run in chaos mode and randomly delete sessions, subscriptions
    /// inject errors and so on.
    /// </summary>
    public bool Chaos
    {
        get
        {
            return _chaosMode != null;
        }
        set
        {
            if (value)
            {
                if (_chaosMode == null)
                {
                    _chaosCts = new CancellationTokenSource();
                    _chaosMode = ChaosAsync(_chaosCts.Token);
                }
            }
            else if (_chaosMode != null)
            {
                _chaosCts.Cancel();
                _chaosMode.GetAwaiter().GetResult();
                _chaosCts.Dispose();
                _chaosMode = null;
                _chaosCts = null;
            }
        }
    }

    /// <summary>
    /// Inject errors responding to incoming requests. The error
    /// rate is the probability of injection, e.g. 3 means 1 out
    /// of 3 requests will be injected with a random error.
    /// </summary>
    public int InjectErrorResponseRate { get; set; }

    private NodeId[] Sessions => CurrentInstance.SessionManager
        .GetSessions()
        .Select(s => s.Id)
        .ToArray();

    /// <summary>
    /// Close all sessions
    /// </summary>
    /// <param name="deleteSubscriptions"></param>
    public void CloseSessions(bool deleteSubscriptions = false)
    {
        if (deleteSubscriptions)
        {
            LogClosingAllSessionsAndSubscriptions();
        }
        else
        {
            LogClosingAllSessions();
        }
        foreach (var session in Sessions)
        {
            CurrentInstance.CloseSession(null, session, deleteSubscriptions);
        }
    }

    private uint[] Subscriptions => CurrentInstance.SubscriptionManager
        .GetSubscriptions()
        .Select(s => s.Id)
        .ToArray();

    /// <summary>
    /// Close all subscriptions. Notify expiration (timeout) of the
    /// subscription before closing (status message) if notifyExpiration
    /// is set to true.
    /// </summary>
    /// <param name="notifyExpiration"></param>
    public void CloseSubscriptions(bool notifyExpiration = false)
    {
        if (notifyExpiration)
        {
            LogNotifyingExpirationAndClosingAllSubscriptions();
        }
        else
        {
            LogClosingAllSubscriptions();
        }
        foreach (var subscription in Subscriptions)
        {
            CloseSubscription(subscription, notifyExpiration);
        }
    }

    /// <summary>
    /// Close subscription. Notify expiration (timeout) of the
    /// subscription before closing (status message) if notifyExpiration
    /// is set to true.
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="notifyExpiration"></param>
    public void CloseSubscription(uint subscriptionId, bool notifyExpiration)
    {
        if (notifyExpiration)
        {
            NotifySubscriptionExpiration(subscriptionId);
        }
        CurrentInstance.DeleteSubscription(subscriptionId);
    }

    private void NotifySubscriptionExpiration(uint subscriptionId)
    {
        try
        {
            var subscription = CurrentInstance.SubscriptionManager
                .GetSubscriptions()
                .FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription != null)
            {
                var expireMethod = typeof(SubscriptionManager).GetMethod("SubscriptionExpired", BindingFlags.NonPublic | BindingFlags.Instance);
                expireMethod?.Invoke(CurrentInstance.SubscriptionManager, new object[] { subscription });
            }
        }
        catch
        {
            // Nothing to do
        }
    }

    /// <summary>
    /// Chaos monkey mode
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
#pragma warning disable CA5394 // Do not use insecure randomness
    private async Task ChaosAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(10, 60)), ct).ConfigureAwait(false);
                LogChaosMonkeyTime();
                LogSubscriptionsAndSessions(Subscriptions.Length, Sessions.Length);
                switch (Random.Shared.Next(0, 16))
                {
                    case 0:
                        CloseSessions(true);
                        break;
                    case 1:
                        CloseSessions(false);
                        break;
                    case 2:
                        CloseSubscriptions(true);
                        break;
                    case 3:
                        CloseSubscriptions(false);
                        break;
                    case > 3 and < 8:
                        var sessions = Sessions;
                        if (sessions.Length == 0)
                        {
                            break;
                        }

                        var session = sessions[Random.Shared.Next(0, sessions.Length)];
                        var delete = Random.Shared.Next() % 2 == 0;
                        LogClosingSession(session, delete);
                        CurrentInstance.CloseSession(null, session, delete);
                        break;
                    case > 10 and < 13:
                        if (InjectErrorResponseRate != 0)
                        {
                            break;
                        }
                        InjectErrorResponseRate = Random.Shared.Next(1, 20);
                        var duration = TimeSpan.FromSeconds(Random.Shared.Next(10, 150));
                        LogInjectingRandomErrors(InjectErrorResponseRate, duration.TotalMilliseconds);
                        _ = Task.Run(async () => {
                            try
                            {
                                await Task.Delay(duration, ct).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { }
                            InjectErrorResponseRate = 0;
                        }, ct);
                        break;
                    default:
                        var subscriptions = Subscriptions;
                        if (subscriptions.Length == 0)
                        {
                            break;
                        }

                        var subscription = subscriptions[Random.Shared.Next(0, subscriptions.Length)];
                        var notify = Random.Shared.Next() % 2 == 0;
                        CloseSubscription(subscription, notify);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Nothing to do
        }
    }

    /// <summary>
    /// Errors to inject, tilt the scale towards the most common errors.
    /// </summary>
    private static readonly StatusCode[] BadStatusCodes =
    {
        StatusCodes.BadCertificateInvalid,
        StatusCodes.BadAlreadyExists,
        StatusCodes.BadNoSubscription,
        StatusCodes.BadSecureChannelClosed,
        StatusCodes.BadSessionClosed,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadSessionIdInvalid,
        StatusCodes.BadConnectionClosed,
        StatusCodes.BadServerHalted,
        StatusCodes.BadNotConnected,
        StatusCodes.BadNoCommunication,
        StatusCodes.BadRequestInterrupted,
        StatusCodes.BadRequestInterrupted,
        StatusCodes.BadRequestInterrupted,
    };

    protected override OperationContext ValidateRequest(RequestHeader requestHeader, RequestType requestType)
    {
        if (InjectErrorResponseRate != 0)
        {
            var dice = Random.Shared.Next(0, BadStatusCodes.Length * InjectErrorResponseRate);
            if (dice < BadStatusCodes.Length)
            {
                var error = BadStatusCodes[dice];
                LogInjectingError(error);
                throw new ServiceResultException(error);
            }
        }
        return base.ValidateRequest(requestHeader, requestType);
    }
#pragma warning restore CA5394 // Do not use insecure randomness

    private void AddPublishMetrics(NotificationMessage notificationMessage)
    {
        int events = 0;
        int dataChanges = 0;
        int diagnostics = 0;

        notificationMessage.NotificationData.ForEach(x => {
            if (x.Body is DataChangeNotification changeNotification)
            {
                dataChanges += changeNotification.MonitoredItems.Count;
                diagnostics += changeNotification.DiagnosticInfos.Count;
            }
            else if (x.Body is EventNotificationList eventNotification)
            {
                events += eventNotification.Events.Count;
            }
            else
            {
                LogUnknownNotification(x.Body.GetType().Name);
            }
        });

        MetricsHelper.AddPublishedCount(dataChanges, events);
    }

    private NodeId GetSessionId(NodeId authenticationToken) => ServerInternal.SessionManager.GetSession(authenticationToken).Id;

    private string GetSessionName(NodeId authenticationToken) => ServerInternal.SessionManager.GetSession(authenticationToken).SessionDiagnostics.SessionName;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "\n\t# Open/total sessions: {Sessions} | {TotalSessions}\n" +
                  "\t# Open/total subscriptions: {Subscriptions} | {TotalSubscriptions}\n" +
                  "\t# Monitored items: {MonitoredItems:N0}\n" +
                  "\t# Working set: {WorkingSet:N0} MB\n" +
                  "\t# Used/available worker threads: {ThreadCount:N0} | {AvailWorkerThreads:N0}\n" +
                  "\t# Stats for the last {PeriodicLoggingTimerSeconds} s\n" +
                  "\t# Sessions created: {CountCreateSession}\n" +
                  "\t# Subscriptions created: {CountCreateSubscription}\n" +
                  "\t# Monitored items created: {CountCreateMonitoredItems}\n" +
                  "\t# Read requests: {CountRead}\n" +
                  "\t# Write requests: {CountWrite}\n" +
                  "\t# Publish requests: {CountPublish}\n" +
                  "\t# Publish metrics enabled: {PublishMetricsEnabled:N0}")]
    partial void LogPeriodicInfo(
        uint sessions,
        uint subscriptions,
        int monitoredItems,
        uint totalSessions,
        uint totalSubscriptions,
        long workingSet,
        int threadCount,
        int availWorkerThreads,
        int periodicLoggingTimerSeconds,
        uint countCreateSession,
        uint countCreateSubscription,
        uint countCreateMonitoredItems,
        uint countRead,
        uint countWrite,
        uint countPublish,
        bool publishMetricsEnabled);

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
        Level = LogLevel.Debug,
        Message = "{Function} error: {StatusCode}")]
    partial void LogErrorWithStatusCode(string function, string statusCode, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{message}")]
    partial void LogErrorMessage(string message);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Unknown notification type: {NotificationType}")]
    partial void LogUnknownNotification(string notificationType);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Starting chaos mode...")]
    partial void LogStartChaos();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "=================== CHAOS MONKEY TIME ===================")]
    partial void LogChaosMonkeyTime();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "--------> Injecting error: {StatusCode}")]
    partial void LogInjectingError(StatusCode statusCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Subscriptions} subscriptions in {Sessions} sessions!")]
    partial void LogSubscriptionsAndSessions(int subscriptions, int sessions);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Closing all sessions and associated subscriptions !!!!!!")]
    partial void LogClosingAllSessionsAndSubscriptions();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Closing all sessions !!!!!")]
    partial void LogClosingAllSessions();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Notifying expiration and closing all subscriptions !!!!!")]
    partial void LogNotifyingExpirationAndClosingAllSubscriptions();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Closing all subscriptions !!!!!")]
    partial void LogClosingAllSubscriptions();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Closing session {Session} (delete subscriptions: {Delete}) !!!!!")]
    partial void LogClosingSession(NodeId session, bool delete);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Injecting random errors every {Rate} responses for {Duration:N0} ms !!!!!")]
    partial void LogInjectingRandomErrors(int rate, double duration);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "!!!!! Closing subscription {Subscription} (notify: {Notify}) !!!!!")]
    partial void LogClosingSubscription(uint subscription, bool notify);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Session {SessionName} subscribed to {NodeIds}")]
    partial void LogCreatedMonitoredItems(string sessionName, string nodeIds);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Chaos mode stopped!")]
    partial void LogChaosModeStopped();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "CreateSession attempted while server halted (client reconnect during startup/shutdown)")]
    partial void LogCreateSessionWhileHalted();
}
