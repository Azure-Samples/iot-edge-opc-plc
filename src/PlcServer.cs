namespace OpcPlc
{
    using AlarmCondition;
    using Opc.Ua;
    using Opc.Ua.Server;
    using OpcPlc.DeterministicAlarms;
    using OpcPlc.Reference;
    using SimpleEvents;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using static Program;

    public partial class PlcServer : StandardServer
    {
        public PlcNodeManager PlcNodeManager = null;
        public AlarmConditionServerNodeManager AlarmNodeManager = null;
        public SimpleEventsNodeManager SimpleEventsNodeManager = null;
        public ReferenceNodeManager SimulationNodeManager = null;
        public DeterministicAlarmsNodeManager DeterministicAlarmsNodeManager = null;
        public readonly TimeService TimeService;

        public PlcServer(TimeService timeService)
        {
            TimeService = timeService;
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

            if(!string.IsNullOrEmpty(NodesFileName) && !File.Exists(NodesFileName))
            {
                string errorMessage = $"The user node configuration file {NodesFileName} does not exist.";
                Logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // Add encodable complex types.
            server.Factory.AddEncodeableTypes(Assembly.GetExecutingAssembly());

            PlcNodeManager = new PlcNodeManager(
                server,
                configuration,
                TimeService,
                PlcSimulation.SlowNodeRandomization,
                PlcSimulation.SlowNodeStepSize,
                PlcSimulation.SlowNodeMinValue,
                PlcSimulation.SlowNodeMaxValue,
                PlcSimulation.FastNodeRandomization,
                PlcSimulation.FastNodeStepSize,
                PlcSimulation.FastNodeMinValue,
                PlcSimulation.FastNodeMaxValue, 
                NodesFileName);

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

            if(PlcSimulation.AddDeterministicAlarmSimulation)
            {
                if(string.IsNullOrEmpty(ScriptFileName))
                {
                    string errorMessage = $"The script file for deterministic testing is not set (deterministicalarmscripfile).";
                    Logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }
                if(!File.Exists(ScriptFileName))
                {
                    string errorMessage = $"The script file ({ScriptFileName}) for deterministic testing does not exist.";
                    Logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }

                DeterministicAlarmsNodeManager = new DeterministicAlarmsNodeManager(server, configuration, ScriptFileName);
                nodeManagers.Add(DeterministicAlarmsNodeManager);
            }

            var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());

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
            var properties = new ServerProperties
            {
                ManufacturerName = "Microsoft",
                ProductName = "IoTEdge OPC UA PLC",
                ProductUri = "https://github.com/Azure/iot-edge-opc-plc.git",
                SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                BuildNumber = Utils.GetAssemblyBuildNumber(),
                BuildDate = Utils.GetAssemblyTimestamp()
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
                IList<Session> currentessions = ServerInternal.SessionManager.GetSessions();

                if (currentessions.Count > 0)
                {
                    // provide some time for the connected clients to detect the shutdown state.
                    ServerInternal.Status.Value.ShutdownReason = new LocalizedText("en-US", "Application closed.");
                    ServerInternal.Status.Variable.ShutdownReason.Value = new LocalizedText("en-US", "Application closed.");
                    ServerInternal.Status.Value.State = ServerState.Shutdown;
                    ServerInternal.Status.Variable.State.Value = ServerState.Shutdown;
                    ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                    for (uint timeTillShutdown = _plcShutdownWaitPeriod; timeTillShutdown > 0; timeTillShutdown--)
                    {
                        ServerInternal.Status.Value.SecondsTillShutdown = timeTillShutdown;
                        ServerInternal.Status.Variable.SecondsTillShutdown.Value = timeTillShutdown;
                        ServerInternal.Status.Variable.ClearChangeMasks(ServerInternal.DefaultSystemContext, true);

                        Thread.Sleep(1000);
                    }
                }
            }
            catch
            {
                // ignore error during shutdown procedure
            }

            base.OnServerStopping();
        }

        private const uint _plcShutdownWaitPeriod = 10;
    }
}
