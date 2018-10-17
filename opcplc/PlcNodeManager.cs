
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;

namespace OpcPlc
{
    using static Program;

    public class PlcNodeManager : CustomNodeManager2
    {
        public UInt32 RandomUnsignedInt32
        {
            get => (UInt32)_randomUnsignedInt32.Value;
            set
            {
                _randomUnsignedInt32.Value = value;
                _randomUnsignedInt32.Timestamp = DateTime.Now;
                _randomUnsignedInt32.ClearChangeMasks(SystemContext, false);
            }
        }

        public Int32 RandomSignedInt32
        {
            get => (Int32)_randomSignedInt32.Value;
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

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, Namespaces.OpcPlcApplications)
        {
            SystemContext.NodeIdFactory = this;
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            BaseInstanceState instance = node as BaseInstanceState;

            if (instance != null && instance.Parent != null)
            {
                string id = instance.Parent.NodeId.Identifier as string;

                if (id != null)
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
            FolderState folder = new FolderState(parent)
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
                IList<IReference> references = null;

                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                FolderState root = CreateFolder(null, ProgramName, ProgramName);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                List<BaseDataVariableState> variables = new List<BaseDataVariableState>();

                try
                {
                    FolderState dataFolder = CreateFolder(root, "Telemetry", "Telemetry");

                    _alternatingBoolean = CreateBaseVariable(dataFolder, "AlternatingBoolean", "AlternatingBoolean", BuiltInType.Boolean, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _randomSignedInt32 = CreateBaseVariable(dataFolder, "RandomSignedInt32", "RandomSignedInt32", BuiltInType.Int32, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _randomUnsignedInt32 = CreateBaseVariable(dataFolder, "RandomUnsignedInt32", "RandomUnsignedInt32", BuiltInType.UInt32, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _spikeData = CreateBaseVariable(dataFolder, "SpikeData", "SpikeData", BuiltInType.Double, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _dipData = CreateBaseVariable(dataFolder, "DipData", "DipData", BuiltInType.Double, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _posTrendData = CreateBaseVariable(dataFolder, "PositiveTrendData", "PositiveTrendData", BuiltInType.Float, ValueRanks.Scalar, AccessLevels.CurrentRead);
                    _negTrendData = CreateBaseVariable(dataFolder, "NegativeTrendData", "NegativeTrendData", BuiltInType.Float, ValueRanks.Scalar, AccessLevels.CurrentRead);

                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods");

                    MethodState resetMethod = CreateMethod(methodsFolder, "Reset", "Reset");
                    SetResetMethodProperties(ref resetMethod);
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        /// <summary>
        /// Sets properies of the Reset method.
        /// </summary>
        private void SetResetMethodProperties(ref MethodState method)
        {
            method.OnCallMethod = new GenericMethodCalledEventHandler(OnResetCall);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        {
            return CreateBaseVariable(parent, path, name, (uint)dataType, valueRank, accessLevel);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateBaseVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank, byte accessLevel)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.TypeDefinitionId = VariableTypeIds.BaseDataVariableType;
            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

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
        private DataItemState CreateDataItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        {
            DataItemState variable = new DataItemState(parent);
            variable.ValuePrecision = new PropertyState<double>(variable);
            variable.Definition = new PropertyState<string>(variable);

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = String.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable using type Numeric as NodeId.
        /// </summary>
        private DataItemState CreateDataItemVariable(NodeState parent, uint id, string name, BuiltInType dataType, int valueRank, byte accessLevel)
        {
            DataItemState variable = new DataItemState(parent);
            variable.ValuePrecision = new PropertyState<double>(variable);
            variable.Definition = new PropertyState<string>(variable);

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.NodeId = new NodeId(id, NamespaceIndex);
            variable.BrowseName = new QualifiedName(name, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = accessLevel;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
            }

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = String.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

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
        /// Creates a new method.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, string path, string name)
        {
            MethodState method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
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
        /// Creates a new method using type Numeric for the NodeId.
        /// </summary>
        private MethodState CreateMethod(NodeState parent, uint id, string name)
        {
            MethodState method = new MethodState(parent)
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
        private ServiceResult OnResetCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            Program.PlcSimulation.ResetTrendData();
            Logger.Debug($"Reset method called");
            return ServiceResult.Good;
        }

        private BaseDataVariableState _alternatingBoolean = null;
        private BaseDataVariableState _randomUnsignedInt32 = null;
        private BaseDataVariableState _randomSignedInt32 = null;
        private BaseDataVariableState _spikeData = null;
        private BaseDataVariableState _dipData = null;
        private BaseDataVariableState _posTrendData = null;
        private BaseDataVariableState _negTrendData = null;
    }
}