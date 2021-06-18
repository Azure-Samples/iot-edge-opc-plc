namespace OpcPlc
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using static Program;

    public class PlcSimulation
    {
        /// <summary>
        /// Flags for node generation.
        /// </summary>
        public static bool GenerateSpikes { get; set; } = true;
        public static bool GenerateDips { get; set; } = true;
        public static bool GeneratePosTrend { get; set; } = true;
        public static bool GenerateNegTrend { get; set; } = true;
        public static bool GenerateData { get; set; } = true;

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
        public static bool AddSpecialCharName { get; set; }
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

            _stepUpCycleInPhase = SimulationCycleCount;
            _alternatingBooleanCycleInPhase = SimulationCycleCount;
            _spikeCycleInPhase = SimulationCycleCount;
            _spikeAnomalyCycle = _random.Next(SimulationCycleCount);
            Logger.Verbose($"first spike anomaly cycle: {_spikeAnomalyCycle}");
            _dipCycleInPhase = SimulationCycleCount;
            _dipAnomalyCycle = _random.Next(SimulationCycleCount);
            Logger.Verbose($"first dip anomaly cycle: {_dipAnomalyCycle}");
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = SimulationCycleCount;
            Logger.Verbose($"first pos trend anomaly phase: {_posTrendAnomalyPhase}");
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = SimulationCycleCount;
            Logger.Verbose($"first neg trend anomaly phase: {_negTrendAnomalyPhase}");
            _stepUpStarted = true;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start()
        {
            if (GenerateData)
            {
                _plcServer.PlcNodeManager.RandomSignedInt32.Start(value => _random.Next(int.MinValue, int.MaxValue), SimulationCycleLength);
                _plcServer.PlcNodeManager.RandomUnsignedInt32.Start(value => (uint)_random.Next(), SimulationCycleLength);
                _plcServer.PlcNodeManager.StepUpNode.Start(StepUpGenerator, SimulationCycleLength);
                _plcServer.PlcNodeManager.AlternatingBooleanNode.Start(AlternatingBooleanGenerator, SimulationCycleLength);
            }

            if (GenerateSpikes) _plcServer.PlcNodeManager.SpikeNode.Start(SpikeGenerator, SimulationCycleLength);
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

            if (AddSpecialCharName)
            {
                _plcServer.PlcNodeManager.SpecialCharNameNode.Start(value => value + 1, periodMs: 1000);
            }

            if (AddLongId)
            {
                _plcServer.PlcNodeManager.LongIdNode.Start(value => value + 1, periodMs: 1000);
            }

            if (AddLongStringNodes)
            {
                const int A = 65, Z = 90 + 1;
                // Change value every second to string containing single repeated uppercase letter.
                _plcServer.PlcNodeManager.LongStringIdNode10.Start(value => new string((char)_random.Next(A, Z), 10 * 1024), periodMs: 1000);
                _plcServer.PlcNodeManager.LongStringIdNode50.Start(value => new string((char)_random.Next(A, Z), 50 * 1024), periodMs: 1000);
                _plcServer.PlcNodeManager.LongStringIdNode100.Start(value => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 100 * 1024)), periodMs: 1000);
                _plcServer.PlcNodeManager.LongStringIdNode200.Start(value => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 200 * 1024)), periodMs: 1000);
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            _plcServer.PlcNodeManager.SpikeNode?.Stop();
            _plcServer.PlcNodeManager.DipNode?.Stop();
            _plcServer.PlcNodeManager.PosTrendNode?.Stop();
            _plcServer.PlcNodeManager.NegTrendNode?.Stop();
            _plcServer.PlcNodeManager.AlternatingBooleanNode?.Stop();
            _plcServer.PlcNodeManager.StepUpNode?.Stop();
            _plcServer.PlcNodeManager.RandomSignedInt32?.Stop();
            _plcServer.PlcNodeManager.RandomUnsignedInt32?.Stop();
            _plcServer.PlcNodeManager.SpecialCharNameNode?.Stop();

            Disable(_slowNodeGenerator);
            Disable(_fastNodeGenerator);
            Disable(_eventInstanceGenerator);
            Disable(_boiler1Generator);
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
        /// Generates a sine wave with spikes at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double SpikeGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (GenerateSpikes && _spikeCycleInPhase == _spikeAnomalyCycle)
            {
                // todo calculate
                nextValue = SimulationMaxAmplitude * 10;
                Logger.Verbose("Generate spike anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / SimulationCycleCount) * _spikeCycleInPhase);
            }
            Logger.Verbose($"spike cycle: {_spikeCycleInPhase} data: {nextValue}");

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_spikeCycleInPhase == 0)
            {
                _spikeCycleInPhase = SimulationCycleCount;
                _spikeAnomalyCycle = _random.Next(SimulationCycleCount);
                Logger.Verbose($"next spike anomaly cycle: {_spikeAnomalyCycle}");
            }

            return nextValue;
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
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        private uint StepUpGenerator(uint value)
        {
            // increase step up value
            if (_stepUpStarted && (_stepUpCycleInPhase % (SimulationCycleCount / 50) == 0))
            {
                value++;
            }

            // end of cycle: reset cycle count
            if (--_stepUpCycleInPhase == 0)
            {
                _stepUpCycleInPhase = SimulationCycleCount;
            }

            return value;
        }

        /// <summary>
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        private bool AlternatingBooleanGenerator(bool value)
        {
            // calculate next boolean value
            bool nextAlternatingBoolean = _alternatingBooleanCycleInPhase % SimulationCycleCount == 0 ? !value : value;
            if (value != nextAlternatingBoolean)
            {
                Logger.Verbose($"data change to: {nextAlternatingBoolean}");
            }

            // end of cycle: reset cycle count
            if (--_alternatingBooleanCycleInPhase == 0)
            {
                _alternatingBooleanCycleInPhase = SimulationCycleCount;
            }

            return nextAlternatingBoolean;
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

        /// <summary>
        /// Method implementation to reset the StepUp data.
        /// </summary>
        public void ResetStepUpData()
        {
            _plcServer.PlcNodeManager.StepUpNode.Value = 0;
        }

        /// <summary>
        /// Method implementation to start the StepUp.
        /// </summary>
        public void StartStepUp()
        {
            _stepUpStarted = true;
        }

        /// <summary>
        /// Method implementation to stop the StepUp.
        /// </summary>
        public void StopStepUp()
        {
            _stepUpStarted = false;
        }

        private const int SIMULATION_CYCLECOUNT_DEFAULT = 50;          // in cycles
        private const int SIMULATION_CYCLELENGTH_DEFAULT = 100;        // in msec
        private const double SIMULATION_MAXAMPLITUDE_DEFAULT = 100.0;
        private const double TREND_BASEVALUE = 100.0;

        private readonly PlcServer _plcServer;
        private readonly Random _random;
        private int _stepUpCycleInPhase;
        private int _alternatingBooleanCycleInPhase;
        private int _spikeAnomalyCycle;
        private int _spikeCycleInPhase;
        private int _dipAnomalyCycle;
        private int _dipCycleInPhase;
        private int _posTrendAnomalyPhase;
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private int _negTrendAnomalyPhase;
        private int _negTrendCycleInPhase;
        private int _negTrendPhase;
        private bool _stepUpStarted;

        private ITimer _slowNodeGenerator;
        private ITimer _fastNodeGenerator;
        private ITimer _eventInstanceGenerator;

        private ITimer _boiler1Generator;
    }
}