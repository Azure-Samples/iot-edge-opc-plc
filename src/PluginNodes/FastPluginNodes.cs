namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Timers;
    using static OpcPlc.Program;

    /// <summary>
    /// Nodes with fast changing values.
    /// </summary>
    public class FastPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private uint NodeCount { get; set; } = 1;
        private uint NodeRate { get; set; } = 10000; // ms.
        private NodeType NodeType { get; set; } = NodeType.UInt;
        private string NodeMinValue { get; set; }
        private string NodeMaxValue { get; set; }
        private bool NodeRandomization { get; set; } = false;
        private string NodeStepSize { get; set; } = "1";
        private uint NodeSamplingInterval { get; set; } // ms.

        private PlcNodeManager _plcNodeManager;
        protected BaseDataVariableState[] _nodes = null;
        protected BaseDataVariableState[] _badNodes = null;
        private BaseDataVariableState _numberOfUpdates;
        private readonly Random _random = new Random();
        private ITimer _nodeGenerator;
        private uint _badNodesCycle = 0;
        private bool _updateNodes = true;
        private const string NumberOfUpdates = "NumberOfUpdates";

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "fn|fastnodes=",
                $"number of fast nodes\nDefault: {NodeCount}",
                (uint i) => NodeCount = i);

            optionSet.Add(
                "fr|fastrate=",
                $"rate in seconds to change fast nodes\nDefault: {NodeRate / 1000}",
                (uint i) => NodeRate = i * 1000);

            optionSet.Add(
                "ft|fasttype=",
                $"data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeType}",
                (string p) => NodeType = ParseNodeType(p));

            optionSet.Add(
                "ftl|fasttypelowerbound=",
                $"lower bound of data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: min value of node type.",
                (string p) => NodeMinValue = p);

            optionSet.Add(
                "ftu|fasttypeupperbound=",
                $"upper bound of data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: max value of node type.",
                (string p) => NodeMaxValue = p);

            optionSet.Add(
                "ftr|fasttyperandomization=",
                $"randomization of fast nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeRandomization}",
                (string p) => NodeRandomization = bool.Parse(p));

            optionSet.Add(
                "fts|fasttypestepsize=",
                $"step or increment size of fast nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeStepSize}",
                (string p) => NodeStepSize = ParseStepSize(p));

            optionSet.Add(
                "fsi|fastnodesamplinginterval=",
                $"rate in milliseconds to sample fast nodes\nDefault: {NodeSamplingInterval}",
                (uint i) => NodeSamplingInterval = i);

            optionSet.Add(
                "vfr|veryfastrate=",
                $"rate in milliseconds to change fast nodes\nDefault: {NodeRate}",
                (uint i) => NodeRate = i);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            FolderState folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Fast",
                name: "Fast",
                NamespaceType.OpcPlcApplications);

            FolderState simulatorFolder = _plcNodeManager.CreateFolder(
                telemetryFolder.Parent, // Root.
                path: "SimulatorConfiguration",
                name: "SimulatorConfiguration",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder, simulatorFolder);
            AddMethods(methodsFolder);
        }

        private void AddMethods(FolderState methodsFolder)
        {
            MethodState stopUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StopUpdateFastNodes",
                name: "StopUpdateFastNodes",
                "Stop the increase of value of fast nodes",
                NamespaceType.OpcPlcApplications);

            SetStopUpdateProperties(ref stopUpdateMethod);

            MethodState startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StartUpdateFastNodes",
                name: "StartUpdateFastNodes",
                "Start the increase of value of fast nodes",
                NamespaceType.OpcPlcApplications);

            SetStartUpdateProperties(ref startUpdateMethod);
        }

        public void StartSimulation()
        {
            // Only use the fast timers when we need to go really fast,
            // since they consume more resources and create an own thread.
            _nodeGenerator = NodeRate >= 50 || !Stopwatch.IsHighResolution ?
                TimeService.NewTimer(UpdateFastNodes, NodeRate) :
                TimeService.NewFastTimer(UpdateVeryFastNodes, NodeRate);
        }

        public void StopSimulation()
        {
            if (_nodeGenerator != null)
            {
                _nodeGenerator.Enabled = false;
            }
        }

        private void AddNodes(FolderState folder, FolderState simulatorFolder)
        {
            (_nodes, _badNodes, _numberOfUpdates) = CreateNodes(NodeType, "Fast", NodeCount, folder, simulatorFolder, NodeRandomization, NodeStepSize, NodeMinValue, NodeMaxValue);

            var nodes = new List<NodeWithIntervals>();

            foreach (var node in _nodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            foreach (var node in _badNodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            Nodes = nodes;
        }

        private (BaseDataVariableState[] nodes, BaseDataVariableState[] badNodes, BaseDataVariableState numberOfUpdatesVariable) CreateNodes(NodeType nodeType, string name, uint count, FolderState folder, FolderState simulatorFolder, bool nodeRandomization, string nodeStepSize, string nodeMinValue, string nodeMaxValue)
        {
            var nodes = CreateBaseLoadNodes(folder, name, count, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue);
            var badNodes = CreateBaseLoadNodes(folder, $"Bad{name}", count: 1, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue);
            var numberOfUpdatesVariable = CreateNumberOfUpdatesVariable(name, simulatorFolder);

            return (nodes, badNodes, numberOfUpdatesVariable);
        }

        private BaseDataVariableState[] CreateBaseLoadNodes(FolderState folder, string name, uint count, NodeType type, bool randomize, string stepSize, string minValue, string maxValue)
        {
            var nodes = new BaseDataVariableState[count];

            if (count > 0)
            {
                Logger.Information($"Creating {count} {name} nodes of type: {type}");
                Logger.Information("Node values will change every " + NodeRate + " ms");
                Logger.Information("Node values sampling rate is " + NodeSamplingInterval + " ms");
            }

            for (int i = 0; i < count; i++)
            {
                var (dataType, valueRank, defaultValue, stepTypeSize, minTypeValue, maxTypeValue) =
                    GetNodeType(type, stepSize, minValue, maxValue);

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
            simulatorFolder.AddChild(variable);

            return variable;
        }

        private static (NodeId dataType, int valueRank, object defaultValue, object stepSize, object minValue, object maxValue) GetNodeType(NodeType nodeType, string stepSize, string minValue, string maxValue)
        {
            return nodeType switch
            {
                NodeType.Bool => (new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, true, null, null, null),

                NodeType.Double => (new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, (double)0.0, double.Parse(stepSize),
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

        public void UpdateFastNodes(object state, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateNodes();
        }

        public void UpdateVeryFastNodes(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            UpdateNodes();
        }

        private void UpdateNodes()
        {
            if (!ShouldUpdateNodes(_numberOfUpdates) || !_updateNodes)
            {
                return;
            }

            if (_nodes != null)
            {
                UpdateNodes(_nodes, NodeType, StatusCodes.Good, false);
            }

            if (_badNodes != null)
            {
                (StatusCode status, bool addBadValue) = BadStatusSequence[_badNodesCycle++ % BadStatusSequence.Length];
                UpdateNodes(_badNodes, NodeType, status, addBadValue);
            }
        }

        public void UpdateNodes(BaseDataVariableState[] nodes, NodeType type, StatusCode status, bool addBadValue)
        {
            if (nodes == null || nodes.Length == 0)
            {
                Logger.Warning("Invalid argument {argument} provided.", nodes);
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

                                            // Return random value from postive or negative range, randomly.
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
                                else if (maxDoubleValue <= 0 && minDoubleValue < 0) // Negative only range cases (e.g. 0 to -9.5).
                                {
                                    value = (extendedDoubleNodeValue % minDoubleValue) > maxDoubleValue
                                    ? maxDoubleValue
                                     : ((extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize) < minDoubleValue
                                                 ? maxDoubleValue
                                                 : (extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize;
                                }
                                else
                                {
                                    // This is to prevent infinte loop while attempting to create a different random number than previous one if no range is provided.
                                    throw new ArgumentException($"Negative to positive range {minDoubleValue} to {maxDoubleValue} for sequential node values is not supported currently.");
                                }
                            }
                            break;

                        case NodeType.Bool:
                            value = extendedNode.Value != null
                                ? !(bool)extendedNode.Value
                                : true;
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
                                        // uint.MaxValue + 1 cycles back to 0 which causes infinte loop here hence a check maxUIntValue == uint.MaxValue to prevent it.
                                        value = (uint)(minUIntValue + (_random.NextDouble() * ((maxUIntValue == uint.MaxValue ? maxUIntValue : maxUIntValue + 1) - minUIntValue)));
                                    }
                                }
                                else
                                {
                                    // This is to prevent infinte loop while attempting to create a different random number than previous one if no range is provided.
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
            variable.Timestamp = TimeService.Now();
            variable.ClearChangeMasks(_plcNodeManager.SystemContext, false);
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
        /// Sets properties of the StopUpdateFastNodes method.
        /// </summary>
        private void SetStopUpdateProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStopUpdateFastNodes);
        }

        /// <summary>
        /// Sets properties of the StartUpdateFastNodes method.
        /// </summary>
        private void SetStartUpdateProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStartUpdateFastNodes);
        }

        /// <summary>
        /// Method to stop updating the fast nodes.
        /// </summary>
        private ServiceResult OnStopUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = false;
            Logger.Debug("StopUpdateFastNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop updating the fast nodes.
        /// </summary>
        private ServiceResult OnStartUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = true;
            Logger.Debug("StartUpdateFastNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Parse node data type, default to Int.
        /// </summary>
        private static NodeType ParseNodeType(string type)
        {
            return Enum.TryParse(type, ignoreCase: true, out NodeType nodeType)
                ? nodeType
                : NodeType.UInt;
        }

        /// <summary>
        /// Parse step size.
        /// </summary>
        private static string ParseStepSize(string stepSize)
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

        private readonly (StatusCode, bool)[] BadStatusSequence = new (StatusCode, bool)[]
        {
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
        };
    }
}
