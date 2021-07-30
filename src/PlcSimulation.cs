using System;
using System.Diagnostics;
using System.Text;
using static OpcPlc.Program;

namespace OpcPlc
{
    public class PlcSimulation
    {
        /// <summary>
        /// Flags for node generation.
        /// </summary>
        public static bool GenerateDips { get; set; } = true;
        public static bool GeneratePosTrend { get; set; } = true;
        public static bool GenerateNegTrend { get; set; } = true;

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
        public static bool AddLongId { get; set; }
        public static bool AddLongStringNodes { get; set; }
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
            _random = new Random();

            _dipCycleInPhase = SimulationCycleCount;
            _dipAnomalyCycle = _random.Next(SimulationCycleCount);
            Logger.Verbose($"first dip anomaly cycle: {_dipAnomalyCycle}");
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = SimulationCycleCount;
            Logger.Verbose($"first pos trend anomaly phase: {_posTrendAnomalyPhase}");
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = SimulationCycleCount;
            Logger.Verbose($"first neg trend anomaly phase: {_negTrendAnomalyPhase}");
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start()
        {
            if (GenerateDips) _plcServer.PlcNodeManager.DipNode.Start(DipGenerator, SimulationCycleLength);
            if (GeneratePosTrend) _plcServer.PlcNodeManager.PosTrendNode.Start(PosTrendGenerator, SimulationCycleLength);
            if (GenerateNegTrend) _plcServer.PlcNodeManager.NegTrendNode.Start(NegTrendGenerator, SimulationCycleLength);

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
                nodes.StartSimulation(_plcServer);
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            _plcServer.PlcNodeManager.DipNode?.Stop();
            _plcServer.PlcNodeManager.PosTrendNode?.Stop();
            _plcServer.PlcNodeManager.NegTrendNode?.Stop();

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

        /// <summary>
        /// Generates a sine wave with dips at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double DipGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (GenerateDips && _dipCycleInPhase == _dipAnomalyCycle)
            {
                nextValue = SimulationMaxAmplitude * -10;
                Logger.Verbose("Generate dip anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / SimulationCycleCount) * _dipCycleInPhase);
            }
            Logger.Verbose($"spike cycle: {_dipCycleInPhase} data: {nextValue}");

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_dipCycleInPhase == 0)
            {
                _dipCycleInPhase = SimulationCycleCount;
                _dipAnomalyCycle = _random.Next(SimulationCycleCount);
                Logger.Verbose($"next dip anomaly cycle: {_dipAnomalyCycle}");
            }

            return nextValue;
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double PosTrendGenerator(double value)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (GeneratePosTrend && _posTrendPhase >= _posTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE + ((_posTrendPhase - _posTrendAnomalyPhase) / 10d);
                Logger.Verbose("Generate postrend anomaly");
            }

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_posTrendCycleInPhase == 0)
            {
                _posTrendCycleInPhase = SimulationCycleCount;
                _posTrendPhase++;
                Logger.Verbose($"pos trend phase: {_posTrendPhase}, data: {nextValue}");
            }

            return nextValue;
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double NegTrendGenerator(double value)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (GenerateNegTrend && _negTrendPhase >= _negTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE - ((_negTrendPhase - _negTrendAnomalyPhase) / 10d);
                Logger.Verbose("Generate negtrend anomaly");
            }

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_negTrendCycleInPhase == 0)
            {
                _negTrendCycleInPhase = SimulationCycleCount;
                _negTrendPhase++;
                Logger.Verbose($"neg trend phase: {_negTrendPhase}, data: {nextValue}");
            }

            return nextValue;
        }

        /// <summary>
        /// Method implementation to reset the trend data.
        /// </summary>
        public void ResetTrendData()
        {
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = SimulationCycleCount;
            _posTrendPhase = 0;
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = SimulationCycleCount;
            _negTrendPhase = 0;
        }

        private const int SIMULATION_CYCLECOUNT_DEFAULT = 50;          // in cycles
        private const int SIMULATION_CYCLELENGTH_DEFAULT = 100;        // in msec
        private const double SIMULATION_MAXAMPLITUDE_DEFAULT = 100.0;
        private const double TREND_BASEVALUE = 100.0;

        private readonly PlcServer _plcServer;
        private readonly Random _random;

        private int _dipAnomalyCycle;
        private int _dipCycleInPhase;
        private int _posTrendAnomalyPhase;
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private int _negTrendAnomalyPhase;
        private int _negTrendCycleInPhase;
        private int _negTrendPhase;

        private ITimer _slowNodeGenerator;
        private ITimer _fastNodeGenerator;
        private ITimer _eventInstanceGenerator;
        private ITimer _boiler1Generator;
    }
}