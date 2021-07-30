namespace OpcPlc
{
    using System.Diagnostics;
    using static OpcPlc.Program;

    public class PlcSimulation
    {
        /// <summary>
        /// Flags for node generation.
        /// </summary>
        public static bool SlowNodeRandomization { get; set; } = false;
        public static uint SlowNodeCount { get; set; } = 1;
        public static uint SlowNodeRate { get; set; } = 10000; // s.
        public static string SlowNodeMinValue { get; set; }
        public static string SlowNodeMaxValue { get; set; }
        public static string SlowNodeStepSize { get; set; } = "1";
        public static NodeType SlowNodeType { get; set; } = NodeType.UInt;
        public static uint SlowNodeSamplingInterval { get; set; } // ms.

        public static bool FastNodeRandomization { get; set; } = false;
        public static uint FastNodeCount { get; set; } = 1;
        public static uint FastNodeRate { get; set; } = 1000; // ms.
        public static string FastNodeMinValue { get; set; }
        public static string FastNodeMaxValue { get; set; }
        public static string FastNodeStepSize { get; set; } = "1";
        public static NodeType FastNodeType { get; set; } = NodeType.UInt;
        public static uint FastNodeSamplingInterval { get; set; } // ms.

        public static bool AddComplexTypeBoiler { get; set; }
        public static bool AddAlarmSimulation { get; set; }
        public static bool AddSimpleEventsSimulation { get; set; }
        public static bool AddReferenceTestSimulation { get; set; }
        public static string DeterministicAlarmSimulationFile { get; set; }

        public static uint EventInstanceCount { get; set; } = 0;
        public static uint EventInstanceRate { get; set; } = 1000; // ms.

        /// <summary>
        /// Simulation data.
        /// </summary>
        public static int SimulationCycleCount { get; set; } = SIMULATION_CYCLECOUNT_DEFAULT;
        public static int SimulationCycleLength { get; set; } = SIMULATION_CYCLELENGTH_DEFAULT;
        public static double SimulationMaxAmplitude { get; set; } = SIMULATION_MAXAMPLITUDE_DEFAULT;

        /// <summary>
        /// Ctor for simulation server.
        /// </summary>
        public PlcSimulation(PlcServer plcServer)
        {
            _plcServer = plcServer;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start()
        {
            if (SlowNodeCount > 0)
            {
                _slowNodeGenerator = _plcServer.TimeService.NewTimer(_plcServer.PlcNodeManager.UpdateSlowNodes, SlowNodeRate);
            }

            if (FastNodeCount > 0)
            {
                // only use the fast timers when we need to go really fast,
                // since they consume more resources and create an own thread.
                _fastNodeGenerator = FastNodeRate >= 50 || !Stopwatch.IsHighResolution ?
                    _plcServer.TimeService.NewTimer(_plcServer.PlcNodeManager.UpdateFastNodes, FastNodeRate) :
                    _plcServer.TimeService.NewFastTimer(_plcServer.PlcNodeManager.UpdateVeryFastNodes, FastNodeRate);
            }

            if (EventInstanceCount > 0)
            {
                _eventInstanceGenerator = EventInstanceRate >= 50 || !Stopwatch.IsHighResolution ?
                    _plcServer.TimeService.NewTimer(_plcServer.PlcNodeManager.UpdateEventInstances, EventInstanceRate) :
                    _plcServer.TimeService.NewFastTimer(_plcServer.PlcNodeManager.UpdateVeryFastEventInstances, EventInstanceRate);
            }

            if (AddComplexTypeBoiler)
            {
                _boiler1Generator = _plcServer.TimeService.NewTimer(_plcServer.PlcNodeManager.UpdateBoiler1, 1000);
            }

            // Start simulation of nodes from node list.
            foreach (var nodes in NodesList)
            {
                nodes.StartSimulation();
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            Disable(_slowNodeGenerator);
            Disable(_fastNodeGenerator);
            Disable(_eventInstanceGenerator);
            Disable(_boiler1Generator);

            // Stop simulation of nodes from node list.
            foreach (var nodes in NodesList)
            {
                nodes.StopSimulation();
            }
        }

        private void Disable(ITimer timer)
        {
            if (timer == null)
            {
                return;
            }

            timer.Enabled = false;
        }

        private const int SIMULATION_CYCLECOUNT_DEFAULT = 50;          // in cycles
        private const int SIMULATION_CYCLELENGTH_DEFAULT = 100;        // in msec
        private const double SIMULATION_MAXAMPLITUDE_DEFAULT = 100.0;

        private readonly PlcServer _plcServer;

        private ITimer _slowNodeGenerator;
        private ITimer _fastNodeGenerator;
        private ITimer _eventInstanceGenerator;
        private ITimer _boiler1Generator;
    }
}