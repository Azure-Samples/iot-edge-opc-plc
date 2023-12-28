namespace OpcPlc;

using AlarmCondition;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Bindings;
using Opc.Ua.Server;
using OpcPlc.CompanionSpecs.DI;
using OpcPlc.DeterministicAlarms;
using OpcPlc.Reference;
using SimpleEvents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

public partial class PlcServer : StandardServer
{
    public PlcNodeManager PlcNodeManager { get; set; }

    public AlarmConditionServerNodeManager AlarmNodeManager { get; set; }

    public SimpleEventsNodeManager SimpleEventsNodeManager { get; set; }

    public ReferenceNodeManager SimulationNodeManager { get; set; }

    public DeterministicAlarmsNodeManager DeterministicAlarmsNodeManager { get; set; }

    public readonly TimeService TimeService;

    private readonly ILogger _logger;

    public PlcServer(TimeService timeService, ILogger logger)
    {
        TimeService = timeService;
        _logger = logger;
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

        // Add encodable complex types.
        server.Factory.AddEncodeableTypes(Assembly.GetExecutingAssembly());
        EncodeableFactory.GlobalFactory.AddEncodeableTypes(Assembly.GetExecutingAssembly());

        // Add DI node manager first so that it gets the namespace index 2.
        var diNodeManager = new DiNodeManager(server, configuration);
        nodeManagers.Add(diNodeManager);

        PlcNodeManager = new PlcNodeManager(
            server,
            configuration,
            TimeService);

        nodeManagers.Add(PlcNodeManager);

        if (PlcSimulationInstance.AddSimpleEventsSimulation)
        {
            SimpleEventsNodeManager = new SimpleEventsNodeManager(server, configuration);
            nodeManagers.Add(SimpleEventsNodeManager);
        }

        if (PlcSimulationInstance.AddAlarmSimulation)
        {
            AlarmNodeManager = new AlarmConditionServerNodeManager(server, configuration);
            nodeManagers.Add(AlarmNodeManager);
        }

        if (PlcSimulationInstance.AddReferenceTestSimulation)
        {
            SimulationNodeManager = new ReferenceNodeManager(server, configuration);
            nodeManagers.Add(SimulationNodeManager);
        }

        if (PlcSimulationInstance.DeterministicAlarmSimulationFile != null)
        {
            var scriptFileName = PlcSimulationInstance.DeterministicAlarmSimulationFile;
            if (string.IsNullOrWhiteSpace(scriptFileName))
            {
                string errorMessage = "The script file for deterministic testing is not set (deterministicalarms).";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
            if (!File.Exists(scriptFileName))
            {
                string errorMessage = $"The script file ({scriptFileName}) for deterministic testing does not exist.";
                _logger.LogError(errorMessage);
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
                resourceManager.Add(id.Value, "en-US", field.Name);
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
                ServerInternal.Status.Value.ShutdownReason = new LocalizedText("en-US", "Application closed.");
                ServerInternal.Status.Variable.ShutdownReason.Value = new LocalizedText("en-US", "Application closed.");
                ServerInternal.Status.Value.State = ServerState.Shutdown;
                ServerInternal.Status.Variable.State.Value = ServerState.Shutdown;
                ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                for (uint secondsUntilShutdown = _plcShutdownWaitSeconds; secondsUntilShutdown > 0; secondsUntilShutdown--)
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

    private const uint _plcShutdownWaitSeconds = 10;
}
