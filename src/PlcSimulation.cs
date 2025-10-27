namespace OpcPlc;

using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Timers;

public class PlcSimulation
{
    private readonly ImmutableList<IPluginNodes> _pluginNodes;

    public PlcSimulation(ImmutableList<IPluginNodes> pluginNodes)
    {
        _pluginNodes = pluginNodes;
    }

    /// <summary>
    /// Flags for node generation.
    /// </summary>

    // alm|alarms
    // add alarm simulation to address space.
    public bool AddAlarmSimulation { get; set; }

    // ses|simpleevents
    // Add simple events simulation to address space.
    public bool AddSimpleEventsSimulation { get; set; }

    // ref|referencetest
    // Add reference test simulation node manager to address space.
    public bool AddReferenceTestSimulation { get; set; } = true;
    public string DeterministicAlarmSimulationFile { get; set; }

    public uint EventInstanceCount { get; set; }
    public uint EventInstanceRate { get; set; } = 1000; // ms.

    /// <summary>
    /// Simulation data.
    /// </summary>
    public int SimulationCycleCount { get; set; } = SIMULATION_CYCLECOUNT_DEFAULT;
    public int SimulationCycleLength { get; set; } = SIMULATION_CYCLELENGTH_DEFAULT;

    /// <summary>
    /// Start the simulation.
    /// </summary>
    public void Start(PlcServer plcServer)
    {
        _plcServer = plcServer;

        if (EventInstanceCount > 0)
        {
            _eventInstanceGenerator = EventInstanceRate >= 50 || !Stopwatch.IsHighResolution
                ? _plcServer.TimeService.NewTimer(UpdateEventInstances, intervalInMilliseconds: EventInstanceRate)
                : _plcServer.TimeService.NewFastTimer(UpdateVeryFastEventInstances, intervalInMilliseconds: EventInstanceRate);
        }

        // Start simulation of nodes from plugin nodes list.
        foreach (var plugin in _pluginNodes)
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
        foreach (var plugin in _pluginNodes)
        {
            plugin.StopSimulation();
        }
    }

    private void UpdateEventInstances(object state, ElapsedEventArgs elapsedEventArgs)
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
                locale: string.Empty, // Invariant.
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
        }
    }

    private void UpdateVeryFastEventInstances(object state, FastTimerElapsedEventArgs elapsedEventArgs)
    {
        UpdateEventInstances();
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
    private const int SIMULATION_CYCLELENGTH_DEFAULT = 100;        // in ms

    private PlcServer _plcServer;

    private ITimer _eventInstanceGenerator;
    private uint _eventInstanceCycle;
}
