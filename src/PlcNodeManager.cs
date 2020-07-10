namespace OpcPlc
{
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using static Program;

    public class PlcNodeManager : CustomNodeManager2
    {
        public uint RandomUnsignedInt32
        {
            get => (uint)_randomUnsignedInt32.Value;
            set
            {
                _randomUnsignedInt32.Value = value;
                _randomUnsignedInt32.Timestamp = DateTime.Now;
                _randomUnsignedInt32.ClearChangeMasks(SystemContext, false);
            }
        }

        public int RandomSignedInt32
        {
            get => (int)_randomSignedInt32.Value;
            set
            {
                _randomSignedInt32.Value = value;
                _randomSignedInt32.Timestamp = DateTime.Now;
                _randomSignedInt32.ClearChangeMasks(SystemContext, false);
            }
        }

        public double SpikeData
        {
            get => (double)_spikeData.Value;
            set
            {
                _spikeData.Value = value;
                _spikeData.Timestamp = DateTime.Now;
                _spikeData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double DipData
        {
            get => (double)_dipData.Value;
            set
            {
                _dipData.Value = value;
                _dipData.Timestamp = DateTime.Now;
                _dipData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double PosTrendData
        {
            get => (double)_posTrendData.Value;
            set
            {
                _posTrendData.Value = value;
                _posTrendData.Timestamp = DateTime.Now;
                _posTrendData.ClearChangeMasks(SystemContext, false);
            }
        }

        public double NegTrendData
        {
            get => (double)_negTrendData.Value;
            set
            {
                _negTrendData.Value = value;
                _negTrendData.Timestamp = DateTime.Now;
                _negTrendData.ClearChangeMasks(SystemContext, false);
            }
        }

        public bool AlternatingBoolean
        {
            get => (bool)_alternatingBoolean.Value;
            set
            {
                _alternatingBoolean.Value = value;
                _alternatingBoolean.Timestamp = DateTime.Now;
                _alternatingBoolean.ClearChangeMasks(SystemContext, false);
            }
        }

        public uint StepUp
        {
            get => (uint)_stepUp.Value;
            set
            {
                _stepUp.Value = value;
                _stepUp.Timestamp = DateTime.Now;
                _stepUp.ClearChangeMasks(SystemContext, false);
            }
        }

        public void IncreaseSlowNodes(object state)
        {
            IncreaseNodes(_slowNodes, PlcSimulation.SlowNodeType);
        }

        public void IncreaseFastNodes(object state)
        {
            IncreaseNodes(_fastNodes, PlcSimulation.FastNodeType);
        }

        private void IncreaseNodes(BaseDataVariableState[] nodes, NodeType type)
        {
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                object value;

                switch (type)
                {
                    case NodeType.Double:
                        value = (double)nodes[nodeIndex].Value + 0.1;
                        break;
                    case NodeType.Bool:
                        value = !(bool)nodes[nodeIndex].Value;
                        break;
                    case NodeType.UIntArray:
                        uint[] arrayValue = (uint[])nodes[nodeIndex].Value;
                        for (int arrayIndex = 0; arrayIndex < arrayValue?.Length; arrayIndex++)
                        {
                            arrayValue[arrayIndex]++;
                        }
                        value = arrayValue;
                        break;
                    case NodeType.UInt:
                    default:
                        value = (uint)nodes[nodeIndex].Value + 1;
                        break;
                }

                nodes[nodeIndex].Value = value;
                nodes[nodeIndex].Timestamp = DateTime.Now;
                nodes[nodeIndex].ClearChangeMasks(SystemContext, false);
            }
        }

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration, string nodeFileName = null)
        : base(server, configuration, Namespaces.OpcPlcApplications)
        {
            _nodeFileName = nodeFileName;
            SystemContext.NodeIdFactory = this;
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
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
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

                FolderState root = CreateFolder(null, ProgramName, ProgramName);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                var variables = new List<BaseDataVariableState>();

                try
                {
                    FolderState dataFolder = CreateFolder(root, "Telemetry", "Telemetry");

                    if (PlcSimulation.GenerateData) _stepUp = CreateBaseVariable(dataFolder, "StepUp", "StepUp", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentReadOrWrite, "Constantly increasing value");
                    if (PlcSimulation.GenerateData) _alternatingBoolean = CreateBaseVariable(dataFolder, "AlternatingBoolean", "AlternatingBoolean", new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, AccessLevels.CurrentRead, "Alternating boolean value");
                    if (PlcSimulation.GenerateData) _randomSignedInt32 = CreateBaseVariable(dataFolder, "RandomSignedInt32", "RandomSignedInt32", new NodeId((uint)BuiltInType.Int32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random signed 32 bit integer value");
                    if (PlcSimulation.GenerateData) _randomUnsignedInt32 = CreateBaseVariable(dataFolder, "RandomUnsignedInt32", "RandomUnsignedInt32", new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, AccessLevels.CurrentRead, "Random unsigned 32 bit integer value");
                    if (PlcSimulation.GenerateSpikes) _spikeData = CreateBaseVariable(dataFolder, "SpikeData", "SpikeData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly spikes");
                    if (PlcSimulation.GenerateDips) _dipData = CreateBaseVariable(dataFolder, "DipData", "DipData", new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value which generates randomly dips");
                    if (PlcSimulation.GeneratePosTrend) _posTrendData = CreateBaseVariable(dataFolder, "PositiveTrendData", "PositiveTrendData", new NodeId((uint)BuiltInType.Float), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow positive trend");
                    if (PlcSimulation.GenerateNegTrend) _negTrendData = CreateBaseVariable(dataFolder, "NegativeTrendData", "NegativeTrendData", new NodeId((uint)BuiltInType.Float), ValueRanks.Scalar, AccessLevels.CurrentRead, "Value with a slow negative trend");

                    // Process slow/fast nodes
                    _slowNodes = CreateBaseLoadNodes(dataFolder, "Slow", PlcSimulation.SlowNodeCount, PlcSimulation.SlowNodeType);
                    _fastNodes = CreateBaseLoadNodes(dataFolder, "Fast", PlcSimulation.FastNodeCount, PlcSimulation.FastNodeType);

                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods");
                    if (PlcSimulation.GeneratePosTrend || PlcSimulation.GenerateNegTrend)
                    {
                        MethodState resetTrendMethod = CreateMethod(methodsFolder, "ResetTrend", "ResetTrend", "Reset the trend values to their baseline value");
                        SetResetTrendMethodProperties(ref resetTrendMethod);
                    }

                    if (PlcSimulation.GenerateData)
                    {
                        MethodState resetStepUpMethod = CreateMethod(methodsFolder, "ResetStepUp", "ResetStepUp", "Resets the StepUp counter to 0");
                        SetResetStepUpMethodProperties(ref resetStepUpMethod);
                        MethodState startStepUpMethod = CreateMethod(methodsFolder, "StartStepUp", "StartStepUp", "Starts the StepUp counter");
                        SetStartStepUpMethodProperties(ref startStepUpMethod);
                        MethodState stopStepUpMethod = CreateMethod(methodsFolder, "StopStepUp", "StopStepUp", "Stops the StepUp counter");
                        SetStopStepUpMethodProperties(ref stopStepUpMethod);
                    }

                    // process user configurable nodes
                    if (!string.IsNullOrEmpty(_nodeFileName))
                    {
                        string json;
                        using (var reader = new StreamReader(_nodeFileName))
                        {
                            json = reader.ReadToEnd();
                        }
                        ConfigFolder cfgFolder = JsonConvert.DeserializeObject<ConfigFolder>(json, new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All
                        });

                        Logger.Information($"Processing node information configured in {_nodeFileName}");
                        Logger.Debug($"Create folder {cfgFolder.Folder}");
                        FolderState userNodesFolder = CreateFolder(root, cfgFolder.Folder, cfgFolder.Folder);

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
                            Logger.Debug($"Create node with Id '{typedNodeId}' and BrowseName '{node.Name}' in namespace with index '{NamespaceIndex}'");
                            CreateBaseVariable(userNodesFolder, node);
                        }
                        Logger.Information($"Processing node information completed.");
                    }
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        private BaseDataVariableState[] CreateBaseLoadNodes(FolderState dataFolder, string name, uint count, NodeType type)
        {
            var nodes = new BaseDataVariableState[count];

            for (int i = 0; i < count; i++)
            {
                var (dataType, valueRank, defaultValue) = GetNodeType(type);

                string id = (i + 1).ToString();
                nodes[i] = CreateBaseVariable(dataFolder, $"{name}{type}{id}", $"{name}{type}{id}", dataType, valueRank, AccessLevels.CurrentReadOrWrite, "Constantly increasing value(s)", defaultValue);
            }

            return nodes;
        }

        private static (NodeId dataType, int valueRank, object defaultValue) GetNodeType(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Bool => (new NodeId((uint)BuiltInType.Boolean), ValueRanks.Scalar, null),
                NodeType.Double => (new NodeId((uint)BuiltInType.Double), ValueRanks.Scalar, null),
                NodeType.UIntArray => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.OneDimension, new uint[32]),
                _ => (new NodeId((uint)BuiltInType.UInt32), ValueRanks.Scalar, null),
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
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, object defaultValue = null)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            };

            if (path.GetType() == Type.GetType("System.Int64"))
            {
                variable.NodeId = new NodeId((uint)path, NamespaceIndex);
                variable.BrowseName = new QualifiedName(((uint)path).ToString(), NamespaceIndex);
            }
            else
            {
                variable.NodeId = new NodeId(path, NamespaceIndex);
                variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            }
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = accessLevel;
            variable.Historizing = false;
            variable.Value = defaultValue ?? TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
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

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private void CreateBaseVariable(NodeState parent, ConfigNode node)
        {
            if (Enum.TryParse(node.DataType, out BuiltInType nodeDataType) == false)
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
            CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description.ToString());
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
        private MethodState CreateMethod(NodeState parent, string path, string name, string description)
        {
            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true,
                Description = new LocalizedText(description)
            };

            if (parent != null)
            {
                parent.AddChild(method);
            }

            return method;
        }

        /// <summary>
        /// Creates a new method using type Numeric for the NodeId.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, uint id, string name)
        {
            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(id, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            if (parent != null)
            {
                parent.AddChild(method);
            }

            return method;
        }

        /// <summary>
        /// Method to reset the trend values. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetTrendCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.ResetTrendData();
            Logger.Debug($"ResetTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to reset the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.ResetStepUpData();
            Logger.Debug($"ResetStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStartStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.StartStepUp();
            Logger.Debug($"StartStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to stop the stepup value. Executes synchronously.
        /// </summary>
        private ServiceResult OnStopStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.StopStepUp();
            Logger.Debug($"StopStepUp method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Following variables listed here are simulated.
        /// </summary>
        protected BaseDataVariableState _stepUp = null;
        protected BaseDataVariableState _alternatingBoolean = null;
        protected BaseDataVariableState _randomUnsignedInt32 = null;
        protected BaseDataVariableState _randomSignedInt32 = null;
        protected BaseDataVariableState _spikeData = null;
        protected BaseDataVariableState _dipData = null;
        protected BaseDataVariableState _posTrendData = null;
        protected BaseDataVariableState _negTrendData = null;
        protected BaseDataVariableState[] _slowNodes = null;
        protected BaseDataVariableState[] _fastNodes = null;

        /// <summary>
        /// File name for user configurable nodes.
        /// </summary>
        protected string _nodeFileName = null;
    }
}