namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

public partial class SlowFastCommon
{
    private readonly PlcNodeManager _plcNodeManager;
    protected readonly TimeService _timeService;
    protected readonly ILogger _logger;

    private readonly SlowFastNodeSimulation _simulation;
    private BaseDataVariableState _numberOfUpdates;
    private const string NumberOfUpdates = "NumberOfUpdates";

    public SlowFastCommon(PlcNodeManager plcNodeManager, TimeService timeService, ILogger logger)
    {
        _plcNodeManager = plcNodeManager ?? throw new ArgumentNullException(nameof(plcNodeManager));
        _timeService = timeService;
        _logger = logger;
        _simulation = new SlowFastNodeSimulation(plcNodeManager.SystemContext, timeService, logger);
    }

    public (BaseDataVariableState[] nodes, BaseDataVariableState[] badNodes) CreateNodes(NodeType nodeType, string name, uint count, FolderState folder, FolderState simulatorFolder, bool nodeRandomization, string nodeStepSize, string nodeMinValue, string nodeMaxValue, uint nodeRate)
    {
        var nodes = CreateBaseLoadNodes(folder, name, count, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue, nodeRate);

        uint badNodesCount = count == 0u
            ? 0u
            : 1u;

        var badNodes = CreateBaseLoadNodes(folder, $"Bad{name}", badNodesCount, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue, nodeRate);

        _numberOfUpdates = CreateNumberOfUpdatesVariable(name, simulatorFolder);

        return (nodes, badNodes);
    }

    private BaseDataVariableState[] CreateBaseLoadNodes(FolderState folder, string name, uint count, NodeType type, bool randomize, string stepSize, string minValue, string maxValue, uint nodeRate)
    {
        var nodes = new BaseDataVariableState[count];

        if (count > 0)
        {
            LogCreatingNodes(count, name, type);
            LogNodeValuesChangeRate(nodeRate);
        }

        for (int i = 0; i < count; i++)
        {
            var (dataType, valueRank, defaultValue, stepTypeSize, minTypeValue, maxTypeValue) = GetNodeType(type, stepSize, minValue, maxValue);

            string id = (i + 1).ToString();
            nodes[i] = _plcNodeManager.CreateBaseVariable(
                folder,
                path: $"{name}{type}{id}",
                name: $"{name}{type}{id}",
                dataType,
                valueRank,
                AccessLevels.CurrentReadOrWrite,
                "Constantly increasing value(s)",
                NamespaceType.OpcPlcApplications,
                randomize,
                stepTypeSize,
                minTypeValue,
                maxTypeValue,
                defaultValue);
        }

        return nodes;
    }

    private BaseDataVariableState CreateNumberOfUpdatesVariable(string baseName, FolderState simulatorFolder)
    {
        // Create property to hold NumberOfUpdates (to stop simulated updates after a given count)
        var variable = new BaseDataVariableState(simulatorFolder);
        var name = $"{baseName}{NumberOfUpdates}";
        variable.NodeId = new NodeId(name, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]);
        variable.DataType = DataTypeIds.Int32;
        variable.Value = -1; // a value < 0 means to update nodes indefinitely.
        variable.ValueRank = ValueRanks.Scalar;
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.BrowseName = new QualifiedName(name);
        variable.DisplayName = new LocalizedText(name);
        variable.Description = new LocalizedText("The number of times to update the {name} nodes. Set to -1 to update indefinitely.");
        variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
        simulatorFolder.AddChild(variable);

        return variable;
    }

    private static (NodeId dataType, int valueRank, object defaultValue, object stepSize, object minValue, object maxValue) GetNodeType(NodeType nodeType, string stepSize, string minValue, string maxValue)
    {
        return nodeType switch {
            NodeType.Bool => (new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, true, null, null, null),

            NodeType.Double => (new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, 0.0, double.Parse(stepSize),
                minValue == null
                    ? 0.0
                    : double.Parse(minValue),
                maxValue == null
                    ? double.MaxValue
                    : double.Parse(maxValue)),

            NodeType.UIntArray => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.OneDimension, new uint[32], null, null, null),

            _ => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, (uint)0, uint.Parse(stepSize),
                minValue == null
                    ? uint.MinValue
                    : uint.Parse(minValue),
                maxValue == null
                    ? uint.MaxValue
                    : uint.Parse(maxValue)),
        };
    }

    public void UpdateNodes(BaseDataVariableState[] nodes, BaseDataVariableState[] badNodes, NodeType nodeType, bool updateNodes)
    {
        _simulation.UpdateNodes(nodes, badNodes, nodeType, _numberOfUpdates, updateNodes);
    }


    /// <summary>
    /// Parse node data type, default to Int.
    /// </summary>
    public static NodeType ParseNodeType(string type)
    {
        return Enum.TryParse(type, ignoreCase: true, out NodeType nodeType)
            ? nodeType
            : NodeType.UInt;
    }

    /// <summary>
    /// Parse step size.
    /// </summary>
    public static string ParseStepSize(string stepSize)
    {
        if (double.TryParse(stepSize, out double stepSizeResult))
        {
            if (stepSizeResult < 0)
            {
                throw new ArgumentException($"Step size cannot be specified as negative value, current value is {stepSize}.");
            }
        }
        else
        {
            throw new ArgumentException($"Step size {stepSize} cannot be parsed as numeric value.");
        }

        return stepSize;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating {Count} {Name} nodes of type: {Type}")]
    partial void LogCreatingNodes(uint count, string name, NodeType type);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Node values will change every {NodeRate:N0} ms")]
    partial void LogNodeValuesChangeRate(uint nodeRate);

}
