namespace OpcPlc
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Timers;
    using static OpcPlc.Program;

    public class PlcNodeManager : CustomNodeManager2
    {
        #region Properties
        #endregion

        public PlcNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService)
            : base(server, configuration, new string[] { Namespaces.OpcPlcApplications, Namespaces.OpcPlcBoiler, Namespaces.OpcPlcBoilerInstance, })
        {
            _timeService = timeService;
            SystemContext.NodeIdFactory = this;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void UpdateEventInstances(object state, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }

        public void UpdateVeryFastEventInstances(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }
#pragma warning restore IDE0060 // Remove unused parameter

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
                    source: null,
                    EventSeverity.Medium,
                    new LocalizedText(info));

                e.SetChildValue(SystemContext, BrowseNames.SourceName, "System", false);
                e.SetChildValue(SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);

                Server.ReportEvent(e);
            };
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
                    FolderState telemetryFolder = CreateFolder(root, "Telemetry", "Telemetry", NamespaceType.OpcPlcApplications);
                    FolderState methodsFolder = CreateFolder(root, "Methods", "Methods", NamespaceType.OpcPlcApplications);

                    AddComplexTypeBoiler(methodsFolder, externalReferences);

                    // Add nodes to address space from plugin nodes list.
                    foreach (var pluginNodes in Program.PluginNodes)
                    {
                        pluginNodes.AddToAddressSpace(telemetryFolder, methodsFolder, plcNodeManager: this);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error creating address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }

        public SimulatedVariableNode<T> CreateVariableNode<T>(BaseDataVariableState variable)
        {
            return new SimulatedVariableNode<T>(SystemContext, variable, _timeService);
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        public FolderState CreateFolder(NodeState parent, string path, string name, NamespaceType namespaceType)
        {
            var existingFolder = parent?.FindChildBySymbolicName(SystemContext, name);
            if(existingFolder != null)
            {
                return (FolderState)existingFolder;
            }

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

        /// <summary>
        /// Creates a new method.
        /// </summary>
        public MethodState CreateMethod(NodeState parent, string path, string name, string description, NamespaceType namespaceType)
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

        #region Complex type boiler
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

        public void UpdateBoiler1(object state, ElapsedEventArgs elapsedEventArgs)
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
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
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
        #endregion

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

        private readonly TimeService _timeService;

        private uint _eventInstanceCycle = 0;

        /// <summary>
        /// Following variables listed here are simulated.
        /// </summary>
        protected BoilerModel.BoilerState _boiler1 = null;
    }
}