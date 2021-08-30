namespace OpcPlc
{
    using Opc.Ua;
    using System.Diagnostics;
    using System.Timers;

    public class PlcSimulation
    {
        /// <summary>
        /// Flags for node generation.
        /// </summary>
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
            if (EventInstanceCount > 0)
            {
                _eventInstanceGenerator = EventInstanceRate >= 50 || !Stopwatch.IsHighResolution
                    ? _plcServer.TimeService.NewTimer(UpdateEventInstances, EventInstanceRate)
                    : _plcServer.TimeService.NewFastTimer(UpdateVeryFastEventInstances, EventInstanceRate);
            }

            // Start simulation of nodes from plugin nodes list.
            foreach (var plugin in Program.PluginNodes)
            {
                plugin.StartSimulation();
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            Disable(_eventInstanceGenerator);

            // Stop simulation of nodes from plugin nodes list.
            foreach (var plugin in Program.PluginNodes)
            {
                plugin.StopSimulation();
            }
        }

        private void UpdateEventInstances(object state, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }

        private void UpdateVeryFastEventInstances(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }

        private void UpdateEventInstances()
        {
            uint eventInstanceCycle = _eventInstanceCycle++;

            for (uint i = 0; i < EventInstanceCount; i++)
            {
                var e = new BaseEventState(null);
                var info = new TranslationInfo(
                    "EventInstanceCycleEventKey",
                    "en-us",
                    "Event with index '{0}' and event cycle '{1}'",
                    i, eventInstanceCycle);

                e.Initialize(
                    _plcServer.PlcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.Medium,
                    new LocalizedText(info));

                e.SetChildValue(_plcServer.PlcNodeManager.SystemContext, BrowseNames.SourceName, "System", false);
                e.SetChildValue(_plcServer.PlcNodeManager.SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);

                _plcServer.PlcNodeManager.Server.ReportEvent(e);
            };
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

        private readonly PlcServer _plcServer;

        private ITimer _eventInstanceGenerator;
        private uint _eventInstanceCycle = 0;
    }
}