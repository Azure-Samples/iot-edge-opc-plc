namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

public partial class SlowFastCommon
{
    private readonly PlcNodeManager _plcNodeManager;
    protected readonly TimeService _timeService;
    protected readonly ILogger _logger;

    private readonly Random _random = new();
    private BaseDataVariableState _numberOfUpdates;
    private uint _badNodesCycle;
    private const string NumberOfUpdates = "NumberOfUpdates";

    public SlowFastCommon(PlcNodeManager plcNodeManager, TimeService timeService, ILogger logger)
    {
        _plcNodeManager = plcNodeManager ?? throw new ArgumentNullException(nameof(plcNodeManager));
        _timeService = timeService;
        _logger = logger;
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
        variable.BrowseName = name;
        variable.DisplayName = name;
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
        if (!ShouldUpdateNodes(_numberOfUpdates) || !updateNodes)
        {
            return;
        }

        if (nodes != null)
        {
            UpdateNodes(nodes, nodeType, StatusCodes.Good, addBadValue: false);
        }

        if (badNodes != null)
        {
            (StatusCode status, bool addBadValue) = BadStatusSequence[_badNodesCycle++ % BadStatusSequence.Length];
            UpdateNodes(badNodes, nodeType, status, addBadValue);
        }
    }

    private void UpdateNodes(BaseDataVariableState[] nodes, NodeType type, StatusCode status, bool addBadValue)
    {
        if (nodes == null || nodes.Length == 0)
        {
            LogInvalidArgument(nodes);
            return;
        }

        for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
        {
            var extendedNode = (BaseDataVariableStateExtended)nodes[nodeIndex];

            object value = null;
            if (StatusCode.IsNotBad(status) || addBadValue)
            {
                switch (type)
                {
                    case NodeType.Double:
                        var minDoubleValue = (double)extendedNode.MinValue;
                        var maxDoubleValue = (double)extendedNode.MaxValue;
                        var extendedDoubleNodeValue = (double)(extendedNode.Value ?? minDoubleValue);

                        if (extendedNode.Randomize)
                        {
                            if (minDoubleValue != maxDoubleValue)
                            {
                                // Hybrid range case (e.g. -5.0 to 5.0).
                                if (minDoubleValue < 0 && maxDoubleValue > 0)
                                {
                                    // If new random value is same as previous one, generate a new one until it is not.
                                    while (value == null || extendedDoubleNodeValue == (double)value)
                                    {
                                        // Split the range from 0 on both sides.
                                        var value1 = _random.NextDouble() * maxDoubleValue;
                                        var value2 = _random.NextDouble() * minDoubleValue;

                                        // Return random value from positive or negative range, randomly.
                                        value = _random.Next(10) % 2 == 0 ? value1 : value2;
                                    }
                                }
                                else // Negative and positive only range cases (e.g. -5.0 to -8.0 or 0 to 9.5).
                                {
                                    // If new random value is same as previous one, generate a new one until it is not.
                                    while (value == null || extendedDoubleNodeValue == (double)value)
                                    {
                                        value = minDoubleValue + (_random.NextDouble() * (maxDoubleValue - minDoubleValue));
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Range {minDoubleValue} to {maxDoubleValue}does not have provision for randomness.");
                            }
                        }
                        else
                        {
                            // Positive only range cases (e.g. 0 to 9.5).
                            if (minDoubleValue >= 0 && maxDoubleValue > 0)
                            {
                                value = (extendedDoubleNodeValue % maxDoubleValue) < minDoubleValue
                                    ? minDoubleValue
                                    : ((extendedDoubleNodeValue % maxDoubleValue) + (double)extendedNode.StepSize) > maxDoubleValue
                                        ? minDoubleValue
                                        : ((extendedDoubleNodeValue % maxDoubleValue) + (double)extendedNode.StepSize);
                            }
                            else if (maxDoubleValue <= 0 && minDoubleValue < 0) // Negative-only range cases (e.g. 0 to -9.5).
                            {
                                value = (extendedDoubleNodeValue % minDoubleValue) > maxDoubleValue
                                    ? maxDoubleValue
                                    : ((extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize) < minDoubleValue
                                        ? maxDoubleValue
                                        : (extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize;
                            }
                            else
                            {
                                // This is to prevent infinite loop while attempting to create a different random number than previous one if no range is provided.
                                throw new ArgumentException($"Negative to positive range {minDoubleValue} to {maxDoubleValue} for sequential node values is not supported currently.");
                            }
                        }
                        break;

                    case NodeType.Bool:
                        value = extendedNode.Value == null || !(bool)extendedNode.Value;
                        break;

                    case NodeType.UIntArray:
                        uint[] arrayValue = (uint[])extendedNode.Value;
                        if (arrayValue != null)
                        {
                            for (int arrayIndex = 0; arrayIndex < arrayValue?.Length; arrayIndex++)
                            {
                                arrayValue[arrayIndex]++;
                            }
                        }
                        else
                        {
                            arrayValue = new uint[32];
                        }
                        value = arrayValue;
                        break;

                    case NodeType.UInt:
                    default:
                        var minUIntValue = (uint)extendedNode.MinValue;
                        var maxUIntValue = (uint)extendedNode.MaxValue;
                        var extendedUIntNodeValue = (uint)(extendedNode.Value ?? minUIntValue);

                        if (extendedNode.Randomize)
                        {
                            if (minUIntValue != maxUIntValue)
                            {
                                // If new random value is same as previous one, generate a new one until it is not.
                                while (value == null || extendedUIntNodeValue == (uint)value)
                                {
                                    // uint.MaxValue + 1 cycles back to 0 which causes infinite loop here hence a check maxUIntValue == uint.MaxValue to prevent it.
                                    value = (uint)(minUIntValue + (_random.NextDouble() * ((maxUIntValue == uint.MaxValue ? maxUIntValue : maxUIntValue + 1) - minUIntValue)));
                                }
                            }
                            else
                            {
                                // This is to prevent infinite loop while attempting to create a different random number than previous one if no range is provided.
                                throw new ArgumentException($"Range {minUIntValue} to {maxUIntValue} does not have provision for randomness.");
                            }
                        }
                        else
                        {
                            value = (extendedUIntNodeValue % maxUIntValue) < minUIntValue
                                ? minUIntValue
                                : ((extendedUIntNodeValue % maxUIntValue) + (uint)extendedNode.StepSize) > maxUIntValue
                                    ? minUIntValue
                                    : ((extendedUIntNodeValue % maxUIntValue) + (uint)extendedNode.StepSize);
                        }

                        break;
                }
            }

            extendedNode.StatusCode = status;
            SetValue(extendedNode, value);
        }
    }

    private void SetValue<T>(BaseVariableState variable, T value)
    {
        variable.Value = value;
        variable.Timestamp = _timeService.Now();
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    /// <summary>
    /// Determines whether the values of simulated nodes should be updated, based
    /// on the value of the corresponding <see cref="NumberOfUpdates"/> variable.
    /// Decrements the NumberOfUpdates variable value and returns true if the NumberOfUpdates variable value if greater than zero,
    /// returns false if the NumberOfUpdates variable value is zero,
    /// returns true if the NumberOfUpdates variable value is less than zero.
    /// </summary>
    /// <param name="numberOfUpdatesVariable">Node that contains the setting of the number of updates to apply.</param>
    /// <returns>True if the value of the node should be updated by the simulator, false otherwise.</returns>
    private bool ShouldUpdateNodes(BaseDataVariableState numberOfUpdatesVariable)
    {
        var value = (int)numberOfUpdatesVariable.Value;
        if (value == 0)
        {
            return false;
        }

        if (value > 0)
        {
            SetValue(numberOfUpdatesVariable, value - 1);
        }

        return true;
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

    private readonly (StatusCode, bool)[] BadStatusSequence =
    [
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.UncertainLastUsableValue, true),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.UncertainLastUsableValue, true),
        ( StatusCodes.BadDataLost, true),
        ( StatusCodes.BadNoCommunication, false)
    ];

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating {Count} {Name} nodes of type: {Type}")]
    partial void LogCreatingNodes(uint count, string name, NodeType type);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Node values will change every {NodeRate:N0} ms")]
    partial void LogNodeValuesChangeRate(uint nodeRate);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid argument {Argument} provided")]
    partial void LogInvalidArgument(object argument);
}
