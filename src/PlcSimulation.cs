using System;
using System.Threading;

namespace OpcPlc
{
    using static Program;

    public class PlcSimulation
    {
        /// <summary>
        /// Flags for anomaly generation.
        /// </summary>
        public static bool GenerateSpikes { get; set; } = true;
        public static bool GenerateDips { get; set; } = true;
        public static bool GeneratePosTrend { get; set; } = true;
        public static bool GenerateNegTrend { get; set; } = true;
        public static bool GenerateData { get; set; } = true;
        public static uint SlowNodes { get; set; } = 0;
        public static uint SlowNodeRate { get; set; } = 10; // s.
        public static NodeType SlowNodeType { get; set; } = NodeType.UInt;
        public static uint FastNodes { get; set; } = 0;
        public static uint FastNodeRate { get; set; } = 1; // s.
        public static NodeType FastNodeType { get; set; } = NodeType.UInt;

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
            _cyclesInPhase = SimulationCycleCount;
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
            _stepUp = 0;
            _stepUpStarted = true;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start()
        {
            if (GenerateSpikes) _spikeGenerator = new Timer(SpikeGenerator, null, 0, SimulationCycleLength);
            if (GenerateDips) _dipGenerator = new Timer(DipGenerator, null, 0, SimulationCycleLength);
            if (GeneratePosTrend) _posTrendGenerator = new Timer(PosTrendGenerator, null, 0, SimulationCycleLength);
            if (GenerateNegTrend) _negTrendGenerator = new Timer(NegTrendGenerator, null, 0, SimulationCycleLength);

            if (GenerateData)
            {
                _dataGenerator = new Timer(ValueGenerator, null, 0, SimulationCycleLength);
            }

            _slowNodeGenerator = new Timer(_plcServer.PlcNodeManager.IncreaseSlowNodes, null, 0, SlowNodeRate * 1000);
            _fastNodeGenerator = new Timer(_plcServer.PlcNodeManager.IncreaseFastNodes, null, 0, FastNodeRate * 1000);
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            if (_spikeGenerator != null)
            {
                _spikeGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_dipGenerator != null)
            {
                _dipGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_posTrendGenerator != null)
            {
                _posTrendGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_negTrendGenerator != null)
            {
                _negTrendGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_dataGenerator != null)
            {
                _dataGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_slowNodeGenerator != null)
            {
                _slowNodeGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (_fastNodeGenerator != null)
            {
                _fastNodeGenerator.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Generates a sine wave with spikes at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void SpikeGenerator(object state)
        {
            // calculate next value
            double nextValue = 0;
            if (GenerateSpikes && _spikeCycleInPhase == _spikeAnomalyCycle)
            {
                // todo calculate
                nextValue = SimulationMaxAmplitude * 10;
                Logger.Verbose($"generate spike anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / SimulationCycleCount) * _spikeCycleInPhase);
            }
            Logger.Verbose($"spike cycle: {_spikeCycleInPhase} data: {nextValue}");
            _plcServer.PlcNodeManager.SpikeData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_spikeCycleInPhase == 0)
            {
                _spikeCycleInPhase = SimulationCycleCount;
                _spikeAnomalyCycle = _random.Next(SimulationCycleCount);
                Logger.Verbose($"next spike anomaly cycle: {_spikeAnomalyCycle}");
            }
        }

        /// <summary>
        /// Generates a sine wave with dips at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void DipGenerator(object state)
        {
            // calculate next value
            double nextValue = 0;
            if (GenerateDips && _dipCycleInPhase == _dipAnomalyCycle)
            {
                nextValue = SimulationMaxAmplitude * -10;
                Logger.Verbose($"generate dip anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / SimulationCycleCount) * _dipCycleInPhase);
            }
            Logger.Verbose($"spike cycle: {_dipCycleInPhase} data: {nextValue}");
            _plcServer.PlcNodeManager.DipData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_dipCycleInPhase == 0)
            {
                _dipCycleInPhase = SimulationCycleCount;
                _dipAnomalyCycle = _random.Next(SimulationCycleCount);
                Logger.Verbose($"next dip anomaly cycle: {_dipAnomalyCycle}");
            }
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void PosTrendGenerator(object state)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (GeneratePosTrend && _posTrendPhase >= _posTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE + (_posTrendPhase - _posTrendAnomalyPhase) / 10;
                Logger.Verbose($"generate postrend anomaly");
            }
            _plcServer.PlcNodeManager.PosTrendData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_posTrendCycleInPhase == 0)
            {
                _posTrendCycleInPhase = SimulationCycleCount;
                _posTrendPhase++;
                Logger.Verbose($"pos trend phase: {_posTrendPhase}, data: {nextValue}");
            }
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void NegTrendGenerator(object state)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (GenerateNegTrend && _negTrendPhase >= _negTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE - (_negTrendPhase - _negTrendAnomalyPhase) / 10;
                Logger.Verbose($"generate negtrend anomaly");
            }
            _plcServer.PlcNodeManager.NegTrendData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_negTrendCycleInPhase == 0)
            {
                _negTrendCycleInPhase = SimulationCycleCount;
                _negTrendPhase++;
                Logger.Verbose($"neg trend phase: {_negTrendPhase}, data: {nextValue}");
            }
        }

        /// <summary>
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        private void ValueGenerator(object state)
        {
            // calculate next boolean value
            bool nextAlternatingBoolean = (_cyclesInPhase % (SimulationCycleCount / 2)) == 0 ? !_currentAlternatingBoolean : _currentAlternatingBoolean;
            if (_currentAlternatingBoolean != nextAlternatingBoolean)
            {
                Logger.Verbose($"data change to: {nextAlternatingBoolean}");
                _currentAlternatingBoolean = nextAlternatingBoolean;
            }
            _plcServer.PlcNodeManager.AlternatingBoolean = nextAlternatingBoolean;

            // calculate next Int values
            _plcServer.PlcNodeManager.RandomSignedInt32 = _random.Next(Int32.MinValue, Int32.MaxValue);
            _plcServer.PlcNodeManager.RandomUnsignedInt32 = (UInt32)_random.Next();

            // increase step up value
            if (_stepUpStarted && (_cyclesInPhase % (SimulationCycleCount / 50) == 0))
            {
                _plcServer.PlcNodeManager.StepUp = _stepUp++;
            }

            // end of cycle: reset cycle count
            if (--_cyclesInPhase == 0)
            {
                _cyclesInPhase = SimulationCycleCount;
            }
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
            _plcServer.PlcNodeManager.StepUp = _stepUp = 0;
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

        private const int SIMULATION_CYCLECOUNT_DEFAULT = 50;           // in cycles
        private const int SIMULATION_CYCLELENGTH_DEFAULT = 100;        // in msec
        private const double SIMULATION_MAXAMPLITUDE_DEFAULT = 100.0;
        private const double TREND_BASEVALUE = 100.0;

        private PlcServer _plcServer;
        private Random _random;
        private int _cyclesInPhase;
        private Timer _dataGenerator;
        private bool _currentAlternatingBoolean;
        private Timer _spikeGenerator;
        private int _spikeAnomalyCycle;
        private int _spikeCycleInPhase;
        private Timer _dipGenerator;
        private int _dipAnomalyCycle;
        private int _dipCycleInPhase;
        private Timer _posTrendGenerator;
        private int _posTrendAnomalyPhase;
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private Timer _negTrendGenerator;
        private int _negTrendAnomalyPhase;
        private int _negTrendCycleInPhase;
        private int _negTrendPhase;
        private uint _stepUp;
        private bool _stepUpStarted;

        private Timer _slowNodeGenerator;
        private Timer _fastNodeGenerator;
    }
}