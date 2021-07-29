using Newtonsoft.Json;

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;

using static OpcPlc.Program;

namespace OpcPlc
{
    public class PlcNodeManager : CustomNodeManager2
    {
        private const string NumberOfUpdates = "NumberOfUpdates";

        #region Properties
        public SimulatedVariableNode<uint> RandomUnsignedInt32 { get; set; }

        public SimulatedVariableNode<int> RandomSignedInt32 { get; set; }

        public SimulatedVariableNode<double> SpikeNode { get; set; }

        public SimulatedVariableNode<double> DipNode { get; set; }

        public SimulatedVariableNode<double> PosTrendNode { get; set; }

        public SimulatedVariableNode<double> NegTrendNode { get; set; }

        public SimulatedVariableNode<bool> AlternatingBooleanNode { get; set; }

        public SimulatedVariableNode<uint> StepUpNode { get; set; }
        #endregion

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService, bool slowNodeRandomization, string slowNodeStepSize, string slowNodeMinValue, string slowNodeMaxValue, bool fastNodeRandomization, string fastNodeStepSize, string fastNodeMinValue, string fastNodeMaxValue, string nodeFileName = null)
            : base(server, configuration, new string[] { Namespaces.OpcPlcApplications, Namespaces.OpcPlcBoiler, Namespaces.OpcPlcBoilerInstance, })
        {
            _timeService = timeService;
            _slowNodeRandomization = slowNodeRandomization;
            _slowNodeStepSize = slowNodeStepSize;
            _fastNodeRandomization = fastNodeRandomization;
            _fastNodeStepSize = fastNodeStepSize;
            _slowNodeMinValue = slowNodeMinValue;
            _slowNodeMaxValue = slowNodeMaxValue;
            _fastNodeMinValue = fastNodeMinValue;
            _fastNodeMaxValue = fastNodeMaxValue;
            _nodeFileName = nodeFileName;
            _random = new Random();
            SystemContext.NodeIdFactory = this;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateSlowNodes(object state, ElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (!ShouldUpdateNodes(_slowNumberOfUpdates) || !_updateFastAndSlowNodes)
            {
                return;
            }

            if (_slowNodes != null)
            {
                UpdateNodes(_slowNodes, PlcSimulation.SlowNodeType, StatusCodes.Good, false);
            }

            if (_slowBadNodes != null)
            {
                (StatusCode status, bool addBadValue) = BadStatusSequence[_slowBadNodesCycle++ % BadStatusSequence.Length];
                UpdateNodes(_slowBadNodes, PlcSimulation.SlowNodeType, status, addBadValue);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateFastNodes(object state, ElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            UpdateNodes();
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateVeryFastNodes(object state, FastTimerElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            UpdateNodes();
        }

        private void UpdateNodes()
        {
            if (!ShouldUpdateNodes(_fastNumberOfUpdates) || !_updateFastAndSlowNodes)
            {
                return;
            }

            if (_fastNodes != null)
            {
                UpdateNodes(_fastNodes, PlcSimulation.FastNodeType, StatusCodes.Good, false);
            }

            if (_fastBadNodes != null)
            {
                (StatusCode status, bool addBadValue) = BadStatusSequence[_fastBadNodesCycle++ % BadStatusSequence.Length];
                UpdateNodes(_fastBadNodes, PlcSimulation.FastNodeType, status, addBadValue);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateEventInstances(object state, ElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            UpdateEventInstances();
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateVeryFastEventInstances(object state, FastTimerElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            UpdateEventInstances();
        }

        private void UpdateEventInstances()
        {
            uint eventInstanceCycle = _eventInstanceCycle++;

            for (uint i = 0; i < PlcSimulation.EventInstanceCount; i++)
            {
                var e = new BaseEventState(null);
                var info = new TranslationInfo(
                    "EventInstanceCycleEventKey",
                    "en-us",
                    "Event with index '{0}' and event cycle '{1}'",
                    i, eventInstanceCycle);

                e.Initialize(
                    SystemContext,
                    null,
                    (EventSeverity)EventSeverity.Medium,
                    new LocalizedText(info));

                e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceName, "System", false);
                e.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceNode, Opc.Ua.ObjectIds.Server, false);

                Server.ReportEvent(e);
            };
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateBoiler1(object state, ElapsedEventArgs elapsedEventArgs)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var newValue = new BoilerModel.BoilerDataType
            {
                HeaterState = _boiler1.BoilerStatus.Value.HeaterState,
            };

            int currentTemperatureBottom = _boiler1.BoilerStatus.Value.Temperature.Bottom;
            BoilerModel.BoilerTemperatureType newTemperature = newValue.Temperature;

            if (_boiler1.BoilerStatus.Value.HeaterState == BoilerModel.BoilerHeaterStateType.On)
            {
                // Heater on, increase by 1.
                newTemperature.Bottom = currentTemperatureBottom + 1;
            }
            else
            {
                // Heater off, decrease down to a minimum of 20.
                newTemperature.Bottom = currentTemperatureBottom > 20
                    ? currentTemperatureBottom - 1
                    : currentTemperatureBottom;
            }

            // Top is always 5 degrees less than bottom, with a minimum value of 20.
            newTemperature.Top = Math.Max(20, newTemperature.Bottom - 5);

            // Pressure is always 100_000 + bottom temperature.
            newValue.Pressure = 100_000 + newTemperature.Bottom;

            // Change complex value in one atomic step.
            _boiler1.BoilerStatus.Value = newValue;
            _boiler1.BoilerStatus.ClearChangeMasks(SystemContext, includeChildren: true);
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            if (node is BaseInstanceState instance && instance.Parent != null)
            {
                if (instance.Parent.NodeId.Identifier is string id)
                {
                    return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
                }
            }

            return node.NodeId;
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                FolderState root = CreateFolder(null, ProgramName, ProgramName, NamespaceType.OpcPlcApplications);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                var variables = new List<BaseDataVariableState>();

                try
                {
                    FolderState dataFolder = CreateFolder(root, "Telemetry", "Telemetry", NamespaceType.OpcPlcApplications);

                    AddChangingNodes(dataFolder);

                    AddSlowAndFastNodes(root, dataFolder, _slowNodeRandomization, _slowNodeStepSize, _slowNodeMinValue, _slowNodeMaxValue, _fastNodeRandomization, _fastNodeStepSize, _fastNodeMinValue, _fastNodeMaxValue);

                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods", NamespaceType.OpcPlcApplications);

                    AddMethods(methodsFolder);

                    AddUserConfigurableNodes(root);

                    AddComplexTypeBoiler(methodsFolder, externalReferences);

                    // Node with special chars in name and ID.
                    SpecialCharNameNodes.AddToAddressSpace(root, plcNodeManager: this);
                    // Node with ID of 3950 chars.
                    LongIdNodes.AddToAddressSpace(root, plcNodeManager: this);
                    // Change value every second to string containing single repeated uppercase letter.
                    LongStringNodes.AddToAddressSpace(root, plcNodeManager: this);
                    // Nodes with deterministic GUIDs as ID.
                    DeterministicGuidNodes.AddToAddressSpace(root, plcNodeManager: this);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        private void AddChangingNodes(FolderState dataFolder)
        {
            if (PlcSimulation.GenerateData)
            {
                StepUpNode = CreateVariableNode<uint>(CreateBaseVariable(dataFolder, "StepUp", "StepUp", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value", NamespaceType.OpcPlcApplications));
                AlternatingBooleanNode = CreateVariableNode<bool>(CreateBaseVariable(dataFolder, "AlternatingBoolean", "AlternatingBoolean", new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, AccessLevels.CurrentRead, "Alternating boolean value", NamespaceType.OpcPlcApplications));
                RandomSignedInt32 = CreateVariableNode<int>(CreateBaseVariable(dataFolder, "RandomSignedInt32", "RandomSignedInt32", new NodeId((uint)BuiltInType.Int32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random signed 32 bit integer value", NamespaceType.OpcPlcApplications));
                RandomUnsignedInt32 = CreateVariableNode<uint>(CreateBaseVariable(dataFolder, "RandomUnsignedInt32", "RandomUnsignedInt32", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random unsigned 32 bit integer value", NamespaceType.OpcPlcApplications));
            }
            if (PlcSimulation.GenerateSpikes) SpikeNode = CreateVariableNode<double>(CreateBaseVariable(dataFolder, "SpikeData", "SpikeData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly spikes", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GenerateDips) DipNode = CreateVariableNode<double>(CreateBaseVariable(dataFolder, "DipData", "DipData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly dips", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GeneratePosTrend) PosTrendNode = CreateVariableNode<double>(CreateBaseVariable(dataFolder, "PositiveTrendData", "PositiveTrendData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow positive trend", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GenerateNegTrend) NegTrendNode = CreateVariableNode<double>(CreateBaseVariable(dataFolder, "NegativeTrendData", "NegativeTrendData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow negative trend", NamespaceType.OpcPlcApplications));
        }

        public SimulatedVariableNode<T> CreateVariableNode<T>(BaseDataVariableState variable)
        {
            return new SimulatedVariableNode<T>(SystemContext, variable, _timeService);
        }

        private void AddSlowAndFastNodes(FolderState root, FolderState dataFolder, bool slowNodeRandomization, string slowNodeStepSize, string slowNodeMinValue, string slowNodeMaxValue, bool fastNodeRandomization, string fastNodeStepSize, string fastNodeMinValue, string fastNodeMaxValue)
        {
            var simulatorFolder = CreateFolder(root, "SimulatorConfiguration", "SimulatorConfiguration", NamespaceType.OpcPlcApplications);
            (_slowNodes, _slowBadNodes, _slowNumberOfUpdates) = CreateSlowOrFastNodes(PlcSimulation.SlowNodeType, "Slow", PlcSimulation.SlowNodeCount, dataFolder, simulatorFolder, slowNodeRandomization, slowNodeStepSize, slowNodeMinValue, slowNodeMaxValue);
            (_fastNodes, _fastBadNodes, _fastNumberOfUpdates) = CreateSlowOrFastNodes(PlcSimulation.FastNodeType, "Fast", PlcSimulation.FastNodeCount, dataFolder, simulatorFolder, fastNodeRandomization, fastNodeStepSize, fastNodeMinValue, fastNodeMaxValue);
        }

        private (BaseDataVariableState[] nodes, BaseDataVariableState[] badNodes, BaseDataVariableState numberOfUpdatesVariable) CreateSlowOrFastNodes(NodeType nodeType, string name, uint count, FolderState dataFolder, FolderState simulatorFolder, bool nodeRandomization, string nodeStepSize, string nodeMinValue, string nodeMaxValue)
        {
            var nodes = CreateBaseLoadNodes(dataFolder, name, count, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue);
            var badNodes = CreateBaseLoadNodes(dataFolder, $"Bad{name}", count: 1, nodeType, nodeRandomization, nodeStepSize, nodeMinValue, nodeMaxValue);
            var numberOfUpdatesVariable = CreateNumberOfUpdatesVariable(name, simulatorFolder);
            return (nodes, badNodes, numberOfUpdatesVariable);
        }

        private BaseDataVariableState CreateNumberOfUpdatesVariable(string baseName, FolderState simulatorFolder)
        {
            // Create property to hold NumberOfUpdates (to stop simulated updates after a given count)
            var variable = new BaseDataVariableState(simulatorFolder);
            var name = $"{baseName}{NumberOfUpdates}";
            variable.NodeId = new NodeId(name, NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]);
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

        private void AddComplexTypeBoiler(FolderState methodsFolder, IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            if (PlcSimulation.AddComplexTypeBoiler)
            {
                // Load complex types from binary uanodes file.
                base.LoadPredefinedNodes(SystemContext, externalReferences);

                // Find the Boiler1 node that was created when the model was loaded.
                var passiveNode = (BaseObjectState)FindPredefinedNode(new NodeId(BoilerModel.Objects.Boiler1, NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseObjectState));

                // Convert to node that can be manipulated within the server.
                _boiler1 = new BoilerModel.BoilerState(null);
                _boiler1.Create(SystemContext, passiveNode);

                base.AddPredefinedNode(SystemContext, _boiler1);

                // Create heater on/off methods.
                MethodState heaterOnMethod = CreateMethod(methodsFolder, "HeaterOn", "HeaterOn", "Turn the heater on", NamespaceType.Boiler);
                SetHeaterOnMethodProperties(ref heaterOnMethod);
                MethodState heaterOffMethod = CreateMethod(methodsFolder, "HeaterOff", "HeaterOff", "Turn the heater off", NamespaceType.Boiler);
                SetHeaterOffMethodProperties(ref heaterOffMethod);
            }
        }

        private void AddUserConfigurableNodes(FolderState root)
        {
            if (!string.IsNullOrEmpty(_nodeFileName))
            {
                string json = File.ReadAllText(_nodeFileName);

                var cfgFolder = JsonConvert.DeserializeObject<ConfigFolder>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });

                Logger.Information($"Processing node information configured in {_nodeFileName}");
                Logger.Debug($"Create folder {cfgFolder.Folder}");
                FolderState userNodesFolder = CreateFolder(root, cfgFolder.Folder, cfgFolder.Folder, NamespaceType.OpcPlcApplications);

                foreach (var node in cfgFolder.NodeList)
                {
                    if (node.NodeId.GetType() != Type.GetType("System.Int64") && node.NodeId.GetType() != Type.GetType("System.String"))
                    {
                        Logger.Error($"The type of the node configuration for node with name {node.Name} ({node.NodeId.GetType()}) is not supported. Only decimal and string are supported. Default to string.");
                        node.NodeId = node.NodeId.ToString();
                    }
                    string typedNodeId = $"{(node.NodeId.GetType() == Type.GetType("System.Int64") ? "i=" : "s=")}{node.NodeId.ToString()}";
                    if (string.IsNullOrEmpty(node.Name))
                    {
                        node.Name = typedNodeId;
                    }
                    if (string.IsNullOrEmpty(node.Description))
                    {
                        node.Description = node.Name;
                    }
                    Logger.Debug($"Create node with Id '{typedNodeId}' and BrowseName '{node.Name}' in namespace with index '{NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]}'");
                    CreateBaseVariable(userNodesFolder, node);
                }

                Logger.Information("Processing node information completed.");
            }
        }

        private FolderState AddMethods(FolderState methodsFolder)
        {
            if (PlcSimulation.GeneratePosTrend || PlcSimulation.GenerateNegTrend)
            {
                MethodState resetTrendMethod = CreateMethod(methodsFolder, "ResetTrend", "ResetTrend", "Reset the trend values to their baseline value", NamespaceType.OpcPlcApplications);
                SetResetTrendMethodProperties(ref resetTrendMethod);
            }

            if (PlcSimulation.GenerateData)
            {
                MethodState resetStepUpMethod = CreateMethod(methodsFolder, "ResetStepUp", "ResetStepUp", "Resets the StepUp counter to 0", NamespaceType.OpcPlcApplications);
                SetResetStepUpMethodProperties(ref resetStepUpMethod);
                MethodState startStepUpMethod = CreateMethod(methodsFolder, "StartStepUp", "StartStepUp", "Starts the StepUp counter", NamespaceType.OpcPlcApplications);
                SetStartStepUpMethodProperties(ref startStepUpMethod);
                MethodState stopStepUpMethod = CreateMethod(methodsFolder, "StopStepUp", "StopStepUp", "Stops the StepUp counter", NamespaceType.OpcPlcApplications);
                SetStopStepUpMethodProperties(ref stopStepUpMethod);
            }

            if (PlcSimulation.SlowNodeCount > 0 || PlcSimulation.FastNodeCount > 0)
            {
                MethodState stopUpdateFastAndSlowNodesMethod = CreateMethod(methodsFolder, "StopUpdateFastAndSlowNodes", "StopUpdateFastAndSlowNodes", "Stops the increase of value of fast and slow nodes", NamespaceType.OpcPlcApplications);
                SetStopUpdateFastAndSlowNodesProperties(ref stopUpdateFastAndSlowNodesMethod);
                MethodState startUpdateFastAndSlowNodesMethod = CreateMethod(methodsFolder, "StartUpdateFastAndSlowNodes", "StartUpdateFastAndSlowNodes", "Start the increase of value of fast and slow nodes", NamespaceType.OpcPlcApplications);
                SetStartUpdateFastAndSlowNodesProperties(ref startUpdateFastAndSlowNodesMethod);
            }

            return methodsFolder;
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
        /// Creates a new folder.
        /// </summary>
        public FolderState CreateFolder(NodeState parent, string path, string name, NamespaceType namespaceType)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(path, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);

            return folder;
        }

        private BaseDataVariableState[] CreateBaseLoadNodes(FolderState dataFolder, string name, uint count, NodeType type, bool randomize, string stepSize, string minValue, string maxValue)
        {
            var nodes = new BaseDataVariableState[count];

            if (count > 0)
            {
                Logger.Information($"Creating {count} {name} nodes of type: {type}");
                Logger.Information("Node values will change every " + (name.Contains("Fast") ? PlcSimulation.FastNodeRate : PlcSimulation.SlowNodeRate) + " ms");
                Logger.Information("Node values sampling rate is " + (name.Contains("Fast") ? PlcSimulation.FastNodeSamplingInterval : PlcSimulation.SlowNodeSamplingInterval) + " ms");
            }

            for (int i = 0; i < count; i++)
            {
                var (dataType, valueRank, defaultValue, stepTypeSize, minTypeValue, maxTypeValue) = GetNodeType(type, stepSize, minValue, maxValue);

                string id = (i + 1).ToString();
                nodes[i] = CreateBaseVariable(dataFolder, $"{name}{type}{id}", $"{name}{type}{id}", dataType, valueRank, AccessLevels.CurrentReadOrWrite, "Constantly increasing value(s)", NamespaceType.OpcPlcApplications, randomize, stepTypeSize, minTypeValue, maxTypeValue, defaultValue);
            }

            return nodes;
        }

        public static (NodeId dataType, int valueRank, object defaultValue, object stepSize, object minValue, object maxValue) GetNodeType(NodeType nodeType, string stepSize, string minValue, string maxValue)
        {
            return nodeType switch
            {
                NodeType.Bool => (new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, true, null, null, null),
                NodeType.Double => (new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, (double)0.0, double.Parse(stepSize), minValue == null ? 0.0 : double.Parse(minValue), maxValue == null ? double.MaxValue : double.Parse(maxValue)),
                NodeType.UIntArray => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.OneDimension, new uint[32], null, null, null),
                _ => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, (uint)0, uint.Parse(stepSize), minValue == null ? uint.MinValue : uint.Parse(minValue), maxValue == null ? uint.MaxValue : uint.Parse(maxValue)),
            };
        }

        /// <summary>
        /// Sets properties of the ResetTrend method.
        /// </summary>
        private void SetResetTrendMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnResetTrendCall);
        }

        /// <summary>
        /// Sets properties of the ResetStepUp method.
        /// </summary>
        private void SetResetStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnResetStepUpCall);
        }

        /// <summary>
        /// Sets properties of the StartStepUp method.
        /// </summary>
        private void SetStartStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStartStepUpCall);
        }

        /// <summary>
        /// Sets properties of the StopStepUp method.
        /// </summary>
        private void SetStopStepUpMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStopStepUpCall);
        }

        /// <summary>
        /// Sets properties of the StopUpdateFastAndSlowNodes method.
        /// </summary>
        private void SetStopUpdateFastAndSlowNodesProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStopUpdateFastAndSlowNodes);
        }

        /// <summary>
        /// Sets properties of the StartUpdateFastAndSlowNodes method.
        /// </summary>
        private void SetStartUpdateFastAndSlowNodesProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnStartUpdateFastAndSlowNodes);
        }

        /// <summary>
        /// Sets properties of the HeaterOn method.
        /// </summary>
        private void SetHeaterOnMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnHeaterOnCall);
        }

        /// <summary>
        /// Sets properties of the HeaterOff method.
        /// </summary>
        private void SetHeaterOffMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnHeaterOffCall);
        }

        /// <summary>
        /// Creates a new extended variable.
        /// </summary>
        public BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, bool randomize, object stepSizeValue, object minTypeValue, object maxTypeValue, object defaultValue = null)
        {
            var baseDataVariableState = new BaseDataVariableStateExtended(parent, randomize, stepSizeValue, minTypeValue, maxTypeValue)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            };

            return CreateBaseVariable(baseDataVariableState, parent, path, name, dataType, valueRank, accessLevel, description, namespaceType, defaultValue);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        public BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, object defaultValue = null)
        {
            var baseDataVariableState = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            };

            return CreateBaseVariable(baseDataVariableState, parent, path, name, dataType, valueRank, accessLevel, description, namespaceType, defaultValue);
        }

        private BaseDataVariableState CreateBaseVariable(BaseDataVariableState baseDataVariableState, NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, object defaultValue = null)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

            if (path.GetType() == Type.GetType("System.Int64"))
            {
                baseDataVariableState.NodeId = new NodeId((uint)path, namespaceIndex);
                baseDataVariableState.BrowseName = new QualifiedName(((uint)path).ToString(), namespaceIndex);
            }
            else
            {
                baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
                baseDataVariableState.BrowseName = new QualifiedName(path, namespaceIndex);
            }

            baseDataVariableState.DisplayName = new LocalizedText("en", name);
            baseDataVariableState.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            baseDataVariableState.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            baseDataVariableState.DataType = dataType;
            baseDataVariableState.ValueRank = valueRank;
            baseDataVariableState.AccessLevel = accessLevel;
            baseDataVariableState.UserAccessLevel = accessLevel;
            baseDataVariableState.Historizing = false;
            baseDataVariableState.Value = defaultValue ?? Opc.Ua.TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            baseDataVariableState.StatusCode = StatusCodes.Good;
            baseDataVariableState.Timestamp = _timeService.UtcNow();
            baseDataVariableState.Description = new LocalizedText(description);

            if (valueRank == ValueRanks.OneDimension)
            {
                baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            parent?.AddChild(baseDataVariableState);

            return baseDataVariableState;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private void CreateBaseVariable(NodeState parent, ConfigNode node)
        {
            if (!Enum.TryParse(node.DataType, out BuiltInType nodeDataType))
            {
                Logger.Error($"Value '{node.DataType}' of node '{node.NodeId}' cannot be parsed. Defaulting to 'Int32'");
                node.DataType = "Int32";
            }

            // We have to hard code conversion here, because AccessLevel is defined as byte in OPCUA lib.
            byte accessLevel;
            try
            {
                accessLevel = (byte)(typeof(AccessLevels).GetField(node.AccessLevel).GetValue(null));
            }
            catch
            {
                Logger.Error($"AccessLevel '{node.AccessLevel}' of node '{node.Name}' is not supported. Defaulting to 'CurrentReadOrWrite'");
                node.AccessLevel = "CurrentRead";
                accessLevel = AccessLevels.CurrentReadOrWrite;
            }
            CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, NamespaceType.OpcPlcApplications);
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var predefinedNodes = new NodeStateCollection();

            predefinedNodes.LoadFromBinaryResource(context,
                "Boiler/BoilerModel.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
                typeof(PlcNodeManager).GetTypeInfo().Assembly,
                updateTables: true);

            return predefinedNodes;
        }

        ///// <summary>
        ///// Creates a new variable.
        ///// </summary>
        //private DataItemState CreateDataItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        //{
        //    DataItemState variable = new DataItemState(parent);
        //    variable.ValuePrecision = new PropertyState<double>(variable);
        //    variable.Definition = new PropertyState<string>(variable);

        //    variable.Create(
        //        SystemContext,
        //        null,
        //        variable.BrowseName,
        //        null,
        //        true);

        //    variable.SymbolicName = name;
        //    variable.ReferenceTypeId = ReferenceTypes.Organizes;
        //    variable.NodeId = new NodeId(path, NamespaceIndex);
        //    variable.BrowseName = new QualifiedName(path, NamespaceIndex);
        //    variable.DisplayName = new LocalizedText("en", name);
        //    variable.WriteMask = AttributeWriteMask.None;
        //    variable.UserWriteMask = AttributeWriteMask.None;
        //    variable.DataType = (uint)dataType;
        //    variable.ValueRank = valueRank;
        //    variable.AccessLevel = accessLevel;
        //    variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.Historizing = false;
        //    variable.Value = TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
        //    variable.StatusCode = StatusCodes.Good;
        //    variable.Timestamp = DateTime.UtcNow;

        //    if (valueRank == ValueRanks.OneDimension)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
        //    }
        //    else if (valueRank == ValueRanks.TwoDimensions)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
        //    }

        //    variable.ValuePrecision.Value = 2;
        //    variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.Definition.Value = String.Empty;
        //    variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

        //    if (parent != null)
        //    {
        //        parent.AddChild(variable);
        //    }

        //    return variable;
        //}

        ///// <summary>
        ///// Creates a new variable using type Numeric as NodeId.
        ///// </summary>
        //private DataItemState CreateDataItemVariable(NodeState parent, uint id, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        //{
        //    DataItemState variable = new DataItemState(parent);
        //    variable.ValuePrecision = new PropertyState<double>(variable);
        //    variable.Definition = new PropertyState<string>(variable);

        //    variable.Create(
        //        SystemContext,
        //        null,
        //        variable.BrowseName,
        //        null,
        //        true);

        //    variable.SymbolicName = name;
        //    variable.ReferenceTypeId = ReferenceTypes.Organizes;
        //    variable.NodeId = new NodeId(id, NamespaceIndex);
        //    variable.BrowseName = new QualifiedName(name, NamespaceIndex);
        //    variable.DisplayName = new LocalizedText("en", name);
        //    variable.WriteMask = AttributeWriteMask.None;
        //    variable.UserWriteMask = AttributeWriteMask.None;
        //    variable.DataType = (uint)dataType;
        //    variable.ValueRank = valueRank;
        //    variable.AccessLevel = accessLevel;
        //    variable.UserAccessLevel = accessLevel;
        //    variable.Historizing = false;
        //    variable.Value = Opc.Ua.TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
        //    variable.StatusCode = StatusCodes.Good;
        //    variable.Timestamp = PlcSimulation.TimeService.UtcNow();

        //    if (valueRank == ValueRanks.OneDimension)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
        //    }
        //    else if (valueRank == ValueRanks.TwoDimensions)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
        //    }

        //    variable.ValuePrecision.Value = 2;
        //    variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.Definition.Value = String.Empty;
        //    variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
        //    variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

        //    if (parent != null)
        //    {
        //        parent.AddChild(variable);
        //    }

        //    return variable;
        //}

        ///// <summary>
        ///// Creates a new variable.
        ///// </summary>
        //private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank)
        //{
        //    BaseDataVariableState variable = new BaseDataVariableState(parent)
        //    {
        //        SymbolicName = name,
        //        ReferenceTypeId = ReferenceTypes.Organizes,
        //        TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
        //        NodeId = new NodeId(path, NamespaceIndex),
        //        BrowseName = new QualifiedName(path, NamespaceIndex),
        //        DisplayName = new LocalizedText("en", name),
        //        WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
        //        UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
        //        DataType = dataType,
        //        ValueRank = valueRank,
        //        AccessLevel = AccessLevels.CurrentReadOrWrite,
        //        UserAccessLevel = AccessLevels.CurrentReadOrWrite,
        //        Historizing = false,
        //        StatusCode = StatusCodes.Good,
        //        Timestamp = DateTime.UtcNow
        //    };

        //    if (valueRank == ValueRanks.OneDimension)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
        //    }
        //    else if (valueRank == ValueRanks.TwoDimensions)
        //    {
        //        variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
        //    }

        //    if (parent != null)
        //    {
        //        parent.AddChild(variable);
        //    }

        //    return variable;
        //}

        /// <summary>
        /// Creates a new method.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, string path, string name, string description, NamespaceType namespaceType)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(path, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true,
                Description = new LocalizedText(description),
            };

            parent?.AddChild(method);

            return method;
        }

        /// <summary>
        /// Method to reset the trend values. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetTrendCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.ResetTrendData();
            Logger.Debug("ResetTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to reset the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.ResetStepUpData();
            Logger.Debug("ResetStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStartStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.StartStepUp();
            Logger.Debug("StartStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStopStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.StopStepUp();
            Logger.Debug("StopStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop updating the fast and slow nodes
        /// </summary>
        private ServiceResult OnStopUpdateFastAndSlowNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateFastAndSlowNodes = false;
            Logger.Debug("StopUpdateFastAndSlowNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop updating the fast and slow nodes
        /// </summary>
        private ServiceResult OnStartUpdateFastAndSlowNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateFastAndSlowNodes = true;
            Logger.Debug("StartUpdateFastAndSlowNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to turn the heater on. Executes synchronously.
        /// </summary>
        private ServiceResult OnHeaterOnCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _boiler1.BoilerStatus.Value.HeaterState = BoilerModel.BoilerHeaterStateType.On;
            Logger.Debug("OnHeaterOnCall method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to turn the heater off. Executes synchronously.
        /// </summary>
        private ServiceResult OnHeaterOffCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _boiler1.BoilerStatus.Value.HeaterState = BoilerModel.BoilerHeaterStateType.Off;
            Logger.Debug("OnHeaterOffCall method called");
            return ServiceResult.Good;
        }

        private void SetValue<T>(BaseVariableState variable, T value)
        {
            variable.Value = value;
            variable.Timestamp = _timeService.Now();
            variable.ClearChangeMasks(SystemContext, false);
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

        private readonly TimeService _timeService;

        private uint _slowBadNodesCycle = 0;
        private uint _fastBadNodesCycle = 0;
        private uint _eventInstanceCycle = 0;

        private BaseDataVariableState _slowNumberOfUpdates;
        private BaseDataVariableState _fastNumberOfUpdates;

        /// <summary>
        /// Following variables listed here are simulated.
        /// </summary>
        protected BaseDataVariableState[] _slowNodes = null;
        protected BaseDataVariableState[] _fastNodes = null;
        protected BoilerModel.BoilerState _boiler1 = null;
        protected BaseDataVariableState[] _slowBadNodes = null;
        protected BaseDataVariableState[] _fastBadNodes = null;
        private readonly bool _slowNodeRandomization;
        private readonly string _slowNodeStepSize;
        private readonly string _slowNodeMinValue;
        private readonly string _slowNodeMaxValue;
        private readonly bool _fastNodeRandomization;
        private readonly string _fastNodeStepSize;
        private readonly string _fastNodeMinValue;
        private readonly string _fastNodeMaxValue;
        private readonly Random _random;
        private bool _updateFastAndSlowNodes = true;

        /// <summary>
        /// File name for user configurable nodes.
        /// </summary>
        protected string _nodeFileName = null;
    }
}