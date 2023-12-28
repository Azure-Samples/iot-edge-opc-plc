namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;

public class PluginNodeBase(TimeService timeService, ILogger logger)
{
    protected readonly TimeService _timeService = timeService;
    protected readonly ILogger _logger = logger;

    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; protected set; } = new List<NodeWithIntervals>();
}
