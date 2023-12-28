namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;

public class PluginNodeBase(PlcSimulation plcSimulation, TimeService timeService, ILogger logger)
{
    protected readonly PlcSimulation _plcSimulation = plcSimulation;
    protected readonly TimeService _timeService = timeService;
    protected readonly ILogger _logger = logger;

    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; protected set; } = new List<NodeWithIntervals>();
}
