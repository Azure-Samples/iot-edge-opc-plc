namespace OpcPlc
{
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using static Program;

    public class PlcNodeManager : CustomNodeManager2
    {
        #region Properties
        public SimulatedVariableNode<uint> RandomUnsignedInt32 { get; set; }

        public SimulatedVariableNode<int> RandomSignedInt32 { get; set; }

        public SimulatedVariableNode<double> SpikeNode { get; set; }

        public SimulatedVariableNode<double> DipNode { get; set; }

        public SimulatedVariableNode<double> PosTrendNode { get; set; }

        public SimulatedVariableNode<double> NegTrendNode { get; set; }

        public SimulatedVariableNode<bool> AlternatingBooleanNode { get; set; }

        public SimulatedVariableNode<uint> StepUpNode { get; set; }

        public SimulatedVariableNode<uint> SpecialCharNameNode { get; set; }

        public SimulatedVariableNode<uint> LongIdNode { get; set; }
        #endregion

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration, string nodeFileName = null)
            : base(server, configuration, new string[] { Namespaces.OpcPlcApplications, Namespaces.OpcPlcBoiler, Namespaces.OpcPlcBoilerInstance, })
        {
            _nodeFileName = nodeFileName;
            SystemContext.NodeIdFactory = this;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void IncreaseSlowNodes(object state)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (_slowNodes != null)
            {
                IncreaseNodes(_slowNodes, PlcSimulation.SlowNodeType, StatusCodes.Good, false);
            }

            if (_slowBadNodes != null)
            {
                (StatusCode status, bool addBadValue) = BadStatusSequence[_slowBadNodesCycle++ % BadStatusSequence.Length];
                IncreaseNodes(_slowBadNodes, PlcSimulation.SlowNodeType, status, addBadValue);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void IncreaseFastNodes(object state)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (_fastNodes != null)
            {
                IncreaseNodes(_fastNodes, PlcSimulation.FastNodeType, StatusCodes.Good, false);
            }

            if (_fastBadNodes != null)
            {
                (StatusCode status, bool addBadValue) = BadStatusSequence[_fastBadNodesCycle++ % BadStatusSequence.Length];
                IncreaseNodes(_fastBadNodes, PlcSimulation.FastNodeType, status, addBadValue);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateBoiler1(object state)
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

                    AddSlowAndFastNodes(dataFolder);

                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods", NamespaceType.OpcPlcApplications);

                    AddMethods(methodsFolder);

                    AddUserConfigurableNodes(root);

                    AddComplexTypeBoiler(methodsFolder, externalReferences);

                    AddSpecialNodes(dataFolder);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        private void AddSpecialNodes(FolderState dataFolder)
        {
            if (PlcSimulation.AddSpecialCharName)
            {
                const string SpecialChars = "\"!§$%&/()=?`´\\+~*'#_-:.;,<>|@^°€µ{[]}";

                SpecialCharNameNode = new SimulatedVariableNode<uint>(SystemContext,
                    CreateBaseVariable(dataFolder, "Special_" + SpecialChars, SpecialChars, new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value", NamespaceType.OpcPlcApplications, defaultValue: (uint)0));
            }

            if (PlcSimulation.AddLongId)
            {
                // Repeat A-Z until 3950 chars are collected.
                var sb = new StringBuilder(4000);
                for (int i = 0; i < 3950; i++)
                {
                    sb.Append((char)(65 + (i % 26)));
                }

                LongIdNode = new SimulatedVariableNode<uint>(SystemContext,
                    CreateBaseVariable(dataFolder, sb.ToString(), "LongId3950", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value", NamespaceType.OpcPlcApplications, defaultValue: (uint)0));
            }
        }

        private void AddChangingNodes(FolderState dataFolder)
        {
            if (PlcSimulation.GenerateData)
            {
                StepUpNode = new SimulatedVariableNode<uint>(SystemContext, CreateBaseVariable(dataFolder, "StepUp", "StepUp", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value", NamespaceType.OpcPlcApplications));
                AlternatingBooleanNode = new SimulatedVariableNode<bool>(SystemContext, CreateBaseVariable(dataFolder, "AlternatingBoolean", "AlternatingBoolean", new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, AccessLevels.CurrentRead, "Alternating boolean value", NamespaceType.OpcPlcApplications));
                RandomSignedInt32 = new SimulatedVariableNode<int>(SystemContext, CreateBaseVariable(dataFolder, "RandomSignedInt32", "RandomSignedInt32", new NodeId((uint)BuiltInType.Int32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random signed 32 bit integer value", NamespaceType.OpcPlcApplications));
                RandomUnsignedInt32 = new SimulatedVariableNode<uint>(SystemContext, CreateBaseVariable(dataFolder, "RandomUnsignedInt32", "RandomUnsignedInt32", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random unsigned 32 bit integer value", NamespaceType.OpcPlcApplications));
            }
            if (PlcSimulation.GenerateSpikes) SpikeNode = new SimulatedVariableNode<double>(SystemContext, CreateBaseVariable(dataFolder, "SpikeData", "SpikeData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly spikes", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GenerateDips) DipNode = new SimulatedVariableNode<double>(SystemContext, CreateBaseVariable(dataFolder, "DipData", "DipData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly dips", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GeneratePosTrend) PosTrendNode = new SimulatedVariableNode<double>(SystemContext, CreateBaseVariable(dataFolder, "PositiveTrendData", "PositiveTrendData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow positive trend", NamespaceType.OpcPlcApplications));
            if (PlcSimulation.GenerateNegTrend) NegTrendNode = new SimulatedVariableNode<double>(SystemContext, CreateBaseVariable(dataFolder, "NegativeTrendData", "NegativeTrendData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow negative trend", NamespaceType.OpcPlcApplications));
        }

        private void AddSlowAndFastNodes(FolderState dataFolder)
        {
            // Normal nodes
            _slowNodes = CreateBaseLoadNodes(dataFolder, "Slow", PlcSimulation.SlowNodeCount, PlcSimulation.SlowNodeType);
            _fastNodes = CreateBaseLoadNodes(dataFolder, "Fast", PlcSimulation.FastNodeCount, PlcSimulation.FastNodeType);

            // Bad nodes
            _slowBadNodes = CreateBaseLoadNodes(dataFolder, "BadSlow", count: 1, PlcSimulation.SlowNodeType);
            _fastBadNodes = CreateBaseLoadNodes(dataFolder, "BadFast", count: 1, PlcSimulation.FastNodeType);
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

            return methodsFolder;
        }

        private void IncreaseNodes(BaseDataVariableState[] nodes, NodeType type, StatusCode status, bool addBadValue)
        {
            if (nodes == null || nodes.Length == 0)
            {
                Logger.Warning("Invalid argument {argument} provided.", nodes);
                return;
            }
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                object value = null;
                if (StatusCode.IsNotBad(status) || addBadValue)
                {
                    switch (type)
                    {
                        case NodeType.Double:
                            value = nodes[nodeIndex].Value != null
                                ? (double)nodes[nodeIndex].Value + 0.1
                                : 0.0;
                            break;
                        case NodeType.Bool:
                            value = nodes[nodeIndex].Value != null
                                ? !(bool)nodes[nodeIndex].Value
                                : true;
                            break;
                        case NodeType.UIntArray:
                            uint[] arrayValue = (uint[])nodes[nodeIndex].Value;
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
                            value = nodes[nodeIndex].Value != null
                                ? (uint)nodes[nodeIndex].Value + 1
                                : 0;
                            break;
                    }
                }

                nodes[nodeIndex].StatusCode = status;
                nodes[nodeIndex].Value = value;
                nodes[nodeIndex].Timestamp = DateTime.Now;
                nodes[nodeIndex].ClearChangeMasks(SystemContext, false);
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name, NamespaceType namespaceType)
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

        private BaseDataVariableState[] CreateBaseLoadNodes(FolderState dataFolder, string name, uint count, NodeType type)
        {
            var nodes = new BaseDataVariableState[count];

            if (count > 0)
            {
                Logger.Information($"Creating {count} {name} nodes of type: {type}");
                Logger.Information("Node values will change every " + (name.Contains("Fast") ? PlcSimulation.FastNodeRate : PlcSimulation.SlowNodeRate) + " s");
                Logger.Information("Node values sampling rate is " + (name.Contains("Fast") ? PlcSimulation.FastNodeSamplingInterval : PlcSimulation.SlowNodeSamplingInterval) + " ms");
            }

            for (int i = 0; i < count; i++)
            {
                var (dataType, valueRank, defaultValue) = GetNodeType(type);

                string id = (i + 1).ToString();
                nodes[i] = CreateBaseVariable(dataFolder, $"{name}{type}{id}", $"{name}{type}{id}", dataType, valueRank, AccessLevels.CurrentReadOrWrite, "Constantly increasing value(s)", NamespaceType.OpcPlcApplications, defaultValue);
            }

            return nodes;
        }

        private static (NodeId dataType, int valueRank, object defaultValue) GetNodeType(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Bool => (new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, true),
                NodeType.Double => (new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, (double)0.0),
                NodeType.UIntArray => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.OneDimension, new uint[32]),
                _ => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, (uint)0),
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
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, object defaultValue = null)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            };

            if (path.GetType() == Type.GetType("System.Int64"))
            {
                variable.NodeId = new NodeId((uint)path, namespaceIndex);
                variable.BrowseName = new QualifiedName(((uint)path).ToString(), namespaceIndex);
            }
            else
            {
                variable.NodeId = new NodeId(path, namespaceIndex);
                variable.BrowseName = new QualifiedName(path, namespaceIndex);
            }

            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = accessLevel;
            variable.Historizing = false;
            variable.Value = defaultValue ?? Opc.Ua.TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            variable.Description = new LocalizedText(description);

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            parent?.AddChild(variable);

            return variable;
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

        private void SetValue<T>(BaseDataVariableState variable, T value)
        {
            variable.Value = value;
            variable.Timestamp = DateTime.Now;
            variable.ClearChangeMasks(SystemContext, false);
        }

        private enum NamespaceType
        {
            OpcPlcApplications,
            Boiler,
            BoilerInstance,
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

        private uint _slowBadNodesCycle = 0;
        private uint _fastBadNodesCycle = 0;

        /// <summary>
        /// Following variables listed here are simulated.
        /// </summary>
        protected BaseDataVariableState[] _slowNodes = null;
        protected BaseDataVariableState[] _fastNodes = null;
        protected BoilerModel.BoilerState _boiler1 = null;
        protected BaseDataVariableState[] _slowBadNodes = null;
        protected BaseDataVariableState[] _fastBadNodes = null;

        /// <summary>
        /// File name for user configurable nodes.
        /// </summary>
        protected string _nodeFileName = null;
    }
}