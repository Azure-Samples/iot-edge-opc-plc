namespace OpcPlc;

using global::AlarmCondition;
using Microsoft.Extensions.Logging;
using Opc.Ua;
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
    private readonly ITelemetryContext _telemetryContext;
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

    // Store previous values for LogPeriodicInfo
    private uint _lastLoggedSessions;
    private uint _lastLoggedSubscriptions;
    private int _lastLoggedMonitoredItems;
    private uint _lastLoggedTotalSessions;
    private uint _lastLoggedTotalSubscriptions;
    private long _lastLoggedWorkingSet;
    private int _lastLoggedThreadCount;
    private int _lastLoggedAvailWorkerThreads;
    private uint _lastLoggedCountCreateSession;
    private uint _lastLoggedCountCreateSubscription;
    private uint _lastLoggedCountCreateMonitoredItems;
    private uint _lastLoggedCountRead;
    private uint _lastLoggedCountWrite;
    private uint _lastLoggedCountPublish;
    private bool _lastLoggedPublishMetricsEnabled;

    public PlcServer(OpcPlcConfiguration config, PlcSimulation plcSimulation, TimeService timeService, ImmutableList<IPluginNodes> pluginNodes, ILogger logger, ITelemetryContext telemetryContext)
    {
        Config = config;
        PlcSimulation = plcSimulation;
        TimeService = timeService;
        _telemetryContext = telemetryContext ?? throw new ArgumentNullException(nameof(telemetryContext));
        _pluginNodes = pluginNodes;
        _logger = logger;

        _periodicLoggingTimer = new Timer(
            (state) => {
                try
                {
                    LogPeriodicInfoIfChanged();

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

    /// <summary>
    /// Logs periodic server information only if any of the monitored values have changed since the last log.
    /// </summary>
    private void LogPeriodicInfoIfChanged()
    {
        var curProc = Process.GetCurrentProcess();

        ThreadPool.GetAvailableThreads(out int availWorkerThreads, out _);

        uint sessionCount = ServerInternal.ServerDiagnostics.CurrentSessionCount;
        IList<ISubscription> subscriptions = ServerInternal.SubscriptionManager.GetSubscriptions();
        int monitoredItemsCount = subscriptions.Sum(s => s.MonitoredItemCount);

        _autoDisablePublishMetrics = sessionCount > 40 || monitoredItemsCount > 500;

        uint currentSubscriptionCount = ServerInternal.ServerDiagnostics.CurrentSubscriptionCount;
        uint cumulatedSessionCount = ServerInternal.ServerDiagnostics.CumulatedSessionCount;
        uint cumulatedSubscriptionCount = ServerInternal.ServerDiagnostics.CumulatedSubscriptionCount;
        long workingSet = curProc.WorkingSet64 / 1024 / 1024;
        int threadCount = curProc.Threads.Count;
        bool publishMetricsEnabled = PublishMetricsEnabled;

        // Only log if any value has changed
        if (sessionCount != _lastLoggedSessions ||
            currentSubscriptionCount != _lastLoggedSubscriptions ||
            monitoredItemsCount != _lastLoggedMonitoredItems ||
            cumulatedSessionCount != _lastLoggedTotalSessions ||
            cumulatedSubscriptionCount != _lastLoggedTotalSubscriptions ||
            workingSet != _lastLoggedWorkingSet ||
            threadCount != _lastLoggedThreadCount ||
            availWorkerThreads != _lastLoggedAvailWorkerThreads ||
            _countCreateSession != _lastLoggedCountCreateSession ||
            _countCreateSubscription != _lastLoggedCountCreateSubscription ||
            _countCreateMonitoredItems != _lastLoggedCountCreateMonitoredItems ||
            _countRead != _lastLoggedCountRead ||
            _countWrite != _lastLoggedCountWrite ||
            _countPublish != _lastLoggedCountPublish ||
            publishMetricsEnabled != _lastLoggedPublishMetricsEnabled)
        {
            LogPeriodicInfo(
                sessionCount,
                currentSubscriptionCount,
                monitoredItemsCount,
                cumulatedSessionCount,
                cumulatedSubscriptionCount,
                workingSet,
                threadCount,
                availWorkerThreads,
                PeriodicLoggingTimerSeconds,
                _countCreateSession,
                _countCreateSubscription,
                _countCreateMonitoredItems,
                _countRead,
                _countWrite,
                _countPublish,
                publishMetricsEnabled);

            // Update last logged values
            _lastLoggedSessions = sessionCount;
            _lastLoggedSubscriptions = currentSubscriptionCount;
            _lastLoggedMonitoredItems = monitoredItemsCount;
            _lastLoggedTotalSessions = cumulatedSessionCount;
            _lastLoggedTotalSubscriptions = cumulatedSubscriptionCount;
            _lastLoggedWorkingSet = workingSet;
            _lastLoggedThreadCount = threadCount;
            _lastLoggedAvailWorkerThreads = availWorkerThreads;
            _lastLoggedCountCreateSession = _countCreateSession;
            _lastLoggedCountCreateSubscription = _countCreateSubscription;
            _lastLoggedCountCreateMonitoredItems = _countCreateMonitoredItems;
            _lastLoggedCountRead = _countRead;
            _lastLoggedCountWrite = _countWrite;
            _lastLoggedCountPublish = _countPublish;
            _lastLoggedPublishMetricsEnabled = publishMetricsEnabled;
        }
    }

    public override async Task<CreateSessionResponse> CreateSessionAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        ApplicationDescription clientDescription,
        string serverUri,
        string endpointUrl,
        string sessionName,
        byte[] clientNonce,
        byte[] clientCertificate,
        double requestedSessionTimeout,
        uint maxResponseMessageSize,
        CancellationToken ct)
    {
        _countCreateSession++;

        try
        {
            var response = await base.CreateSessionAsync(
                secureChannelContext,
                requestHeader,
                clientDescription,
                serverUri,
                endpointUrl,
                sessionName,
                clientNonce,
                clientCertificate,
                requestedSessionTimeout,
                maxResponseMessageSize,
                ct).ConfigureAwait(false);

            MetricsHelper.AddSessionCount(response.SessionId.ToString());

            LogSuccessWithSessionId(nameof(CreateSession), response.SessionId);

            return response;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadServerHalted)
        {
            // Handle when a client attempts to reconnect while the server is still starting up or halting.
            LogCreateSessionWhileHalted();

            return new CreateSessionResponse
            {
                ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadServerHalted },
                SessionId = null,
                AuthenticationToken = null,
                RevisedSessionTimeout = 0,
                ServerNonce = Array.Empty<byte>(),
                ServerCertificate = Array.Empty<byte>(),
                ServerEndpoints = [],
                ServerSignature = new SignatureData(),
                MaxRequestMessageSize = 0
            };
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateSession));

            LogError(nameof(CreateSession), ex);
            throw;
        }
    }

    public override async Task<CreateSubscriptionResponse> CreateSubscriptionAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        double requestedPublishingInterval,
        uint requestedLifetimeCount,
        uint requestedMaxKeepAliveCount,
        uint maxNotificationsPerPublish,
        bool publishingEnabled,
        byte priority,
        CancellationToken ct)
    {
        _countCreateSubscription++;

        try
        {
            var response = await base.CreateSubscriptionAsync(
                secureChannelContext,
                requestHeader,
                requestedPublishingInterval,
                requestedLifetimeCount,
                requestedMaxKeepAliveCount,
                maxNotificationsPerPublish,
                publishingEnabled,
                priority,
                ct).ConfigureAwait(false);

            NodeId sessionId = GetSessionId(requestHeader.AuthenticationToken);
            MetricsHelper.AddSubscriptionCount(sessionId.ToString(), response.SubscriptionId.ToString());

            LogSuccessWithSessionIdAndSubscriptionId(
                nameof(CreateSubscription),
                sessionId,
                response.SubscriptionId);

            return response;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateSubscription));

            LogError(nameof(CreateSubscription), ex);
            throw;
        }
    }

    public override async Task<CreateMonitoredItemsResponse> CreateMonitoredItemsAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        uint subscriptionId,
        TimestampsToReturn timestampsToReturn,
        MonitoredItemCreateRequestCollection itemsToCreate,
        CancellationToken ct)
    {
        _countCreateMonitoredItems += (uint)itemsToCreate.Count;

        try
        {
            var response = await base.CreateMonitoredItemsAsync(
                secureChannelContext,
                requestHeader,
                subscriptionId,
                timestampsToReturn,
                itemsToCreate,
                ct).ConfigureAwait(false);

            MetricsHelper.AddMonitoredItemCount(itemsToCreate.Count);

            // Only log items with good status codes.
            var successfulItems = itemsToCreate
                .Zip(response.Results, (request, result) => new { Request = request, Result = result })
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

            return response;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(CreateMonitoredItems));

            LogError(nameof(CreateSubscription), ex);
            throw;
        }
    }

    public override async Task<PublishResponse> PublishAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        SubscriptionAcknowledgementCollection subscriptionAcknowledgements,
        CancellationToken ct)
    {
        _countPublish++;

        try
        {
            var response = await base.PublishAsync(
                secureChannelContext,
                requestHeader,
                subscriptionAcknowledgements,
                ct).ConfigureAwait(false);

            if (PublishMetricsEnabled)
            {
                AddPublishMetrics(response.NotificationMessage);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogSuccessWithSessionIdAndSubscriptionId(
                    nameof(Publish),
                    GetSessionId(requestHeader.AuthenticationToken),
                    response.SubscriptionId);
            }

            return response;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNoSubscription)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadNoSubscription),
                ex);

            return new PublishResponse { ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadNoSubscription } };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSessionIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSessionIdInvalid),
                ex);

            return new PublishResponse { ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadSessionIdInvalid } };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSecureChannelIdInvalid)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSecureChannelIdInvalid),
                ex);

            return new PublishResponse { ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadSecureChannelIdInvalid } };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadSessionClosed)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadSessionClosed),
                ex);

            return new PublishResponse { ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadSessionClosed } };
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadTimeout)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogErrorWithStatusCode(
                nameof(Publish),
                nameof(StatusCodes.BadTimeout),
                ex);

            return new PublishResponse { ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadTimeout } };
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(Publish));

            LogError(nameof(Publish), ex);
            throw;
        }
    }

    public override async Task<ReadResponse> ReadAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        double maxAge,
        TimestampsToReturn timestampsToReturn,
        ReadValueIdCollection nodesToRead,
        CancellationToken ct)
    {
        _countRead++;

        try
        {
            var response = await base.ReadAsync(
                secureChannelContext,
                requestHeader,
                maxAge,
                timestampsToReturn,
                nodesToRead,
                ct).ConfigureAwait(false);

            LogSuccess(nameof(Read));

            return response;
        }
        catch (Exception ex)
        {
            MetricsHelper.RecordTotalErrors(nameof(Read));

            LogError(nameof(Read), ex);
            throw;
        }
    }

    public override async Task<WriteResponse> WriteAsync(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        WriteValueCollection nodesToWrite,
        CancellationToken ct)
    {
        _countWrite++;

        try
        {
            var response = await base.WriteAsync(
                secureChannelContext,
                requestHeader,
                nodesToWrite,
                ct).ConfigureAwait(false);

            LogSuccess(nameof(Write));

            return response;
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
                encodableFactoryField.SetValue(server, encodableFactoryField.GetValue(server));
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
            SimpleEventsNodeManager = new SimpleEventsNodeManager(server, configuration, _logger);
            nodeManagers.Add(SimpleEventsNodeManager);
        }

        if (PlcSimulation.AddAlarmSimulation)
        {
            AlarmNodeManager = new AlarmConditionServerNodeManager(server, configuration, _logger, server.Telemetry);
            nodeManagers.Add(AlarmNodeManager);
        }

        if (PlcSimulation.AddReferenceTestSimulation)
        {
            SimulationNodeManager = new ReferenceNodeManager(server, configuration, _logger, server.Telemetry);
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
        var resourceManager = new ResourceManager(configuration);

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

    /// <summary>
    /// Cleans up before the server shuts down.
    /// </summary>
    /// <remarks>
    /// This method is called before any shutdown processing occurs.
    /// </remarks>
    protected override async ValueTask OnServerStoppingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // check for connected clients
            IList<ISession> currentSessions = ServerInternal.SessionManager.GetSessions();

            if (currentSessions.Count > 0)
            {
                // Provide some time for the connected clients to detect the shutdown state.
                var shutdownReason = new LocalizedText(string.Empty, "Application closed."); // Invariant.

                for (uint secondsUntilShutdown = PlcShutdownWaitSeconds; secondsUntilShutdown > 0; secondsUntilShutdown--)
                {
                    ServerInternal.UpdateServerStatus(status => {
                        status.Value.State = ServerState.Shutdown;
                        status.Value.ShutdownReason = shutdownReason;
                        status.Value.SecondsTillShutdown = secondsUntilShutdown;
                    });

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation during shutdown procedure
        }
        catch
        {
            // ignore error during shutdown procedure
        }

        await base.OnServerStoppingAsync(cancellationToken).ConfigureAwait(false);

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
    public async Task CloseSessionsAsync(bool deleteSubscriptions, CancellationToken ct)
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
            await CurrentInstance.CloseSessionAsync(null, session, deleteSubscriptions, ct).ConfigureAwait(false);
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
    public async Task CloseSubscriptionsAsync(bool notifyExpiration, CancellationToken ct)
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
            await CloseSubscriptionAsync(subscription, notifyExpiration, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Close subscription. Notify expiration (timeout) of the
    /// subscription before closing (status message) if notifyExpiration
    /// is set to true.
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="notifyExpiration"></param>
    public async Task CloseSubscriptionAsync(uint subscriptionId, bool notifyExpiration, CancellationToken ct)
    {
        if (notifyExpiration)
        {
            NotifySubscriptionExpiration(subscriptionId);
        }

        await CurrentInstance.DeleteSubscriptionAsync(subscriptionId, ct).ConfigureAwait(false);
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
                        await CloseSessionsAsync(true, ct).ConfigureAwait(false);
                        break;
                    case 1:
                        await CloseSessionsAsync(false, ct).ConfigureAwait(false);
                        break;
                    case 2:
                        await CloseSubscriptionsAsync(true, ct).ConfigureAwait(false);
                        break;
                    case 3:
                        await CloseSubscriptionsAsync(false, ct).ConfigureAwait(false);
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
                        await CurrentInstance.CloseSessionAsync(null, session, delete, ct).ConfigureAwait(false);
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
                        await CloseSubscriptionAsync(subscription, notify, ct).ConfigureAwait(false);
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

    protected override OperationContext ValidateRequest(
        SecureChannelContext secureChannelContext,
        RequestHeader requestHeader,
        RequestType requestType)
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
        return base.ValidateRequest(secureChannelContext, requestHeader, requestType);
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
