namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.Configuration;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public partial class PlcNodeManager : CustomNodeManager2
{
    private readonly OpcPlcConfiguration _config;
    private readonly ImmutableList<IPluginNodes> _pluginNodes;
    private readonly ILogger _logger;

    public PlcNodeManager(IServerInternal server, OpcPlcConfiguration config, ApplicationConfiguration appConfig, TimeService timeService, PlcSimulation plcSimulation, ImmutableList<IPluginNodes> pluginNodes, ILogger logger)
        : base(server, appConfig, [Namespaces.OpcPlcApplications, Namespaces.OpcPlcBoiler, Namespaces.OpcPlcBoilerInstance,])
    {
        _config = config;
        _timeService = timeService;
        PlcSimulationInstance = plcSimulation;
        _pluginNodes = pluginNodes;
        _logger = logger;
        SystemContext.NodeIdFactory = this;
        SystemContext.EncodeableFactory = Server.Factory;
    }

    public PlcSimulation PlcSimulationInstance { get; }

    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        return node is BaseInstanceState instance &&
               instance.Parent != null &&
               instance.Parent.NodeId.Identifier is string id
                  ? new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex)
                  : node.NodeId;
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

            _externalReferences = externalReferences;

            FolderState root = CreateFolder(parent: null, _config.ProgramName, _config.ProgramName, NamespaceType.OpcPlcApplications);
            root.AddReference(ReferenceTypes.Organizes, isInverse: true, ObjectIds.ObjectsFolder);
            references.Add(new NodeStateReference(ReferenceTypes.Organizes, isInverse: false, root.NodeId));
            root.EventNotifier = EventNotifiers.SubscribeToEvents;
            AddRootNotifier(root);

            try
            {
                FolderState telemetryFolder = CreateFolder(root, "Telemetry", "Telemetry", NamespaceType.OpcPlcApplications);
                FolderState methodsFolder = CreateFolder(root, "Methods", "Methods", NamespaceType.OpcPlcApplications);

                // Add nodes to address space from plugin nodes list.
                foreach (var plugin in _pluginNodes)
                {
                    plugin.AddToAddressSpace(telemetryFolder, methodsFolder, plcNodeManager: this);
                }
            }
            catch (Exception e)
            {
                LogErrorCreatingAddressSpace(e);
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
        if (existingFolder != null)
        {
            return (FolderState)existingFolder;
        }

        ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

        var folder = new FolderState(parent) {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId(path, namespaceIndex),
            BrowseName = new QualifiedName(path, namespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None,
        };

        parent?.AddChild(folder);

        return folder;
    }

    /// <summary>
    /// Creates a new extended variable.
    /// </summary>
    public BaseDataVariableState CreateBaseVariable(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, bool randomize, object stepSizeValue, object minTypeValue, object maxTypeValue, object defaultValue = null)
    {
        var baseDataVariableState = new BaseDataVariableStateExtended(parent, randomize, stepSizeValue, minTypeValue, maxTypeValue) {
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
        var baseDataVariableState = new BaseDataVariableState(parent) {
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

        var method = new MethodState(parent) {
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
    /// Registers a node as a root notifier for event subscriptions.
    /// </summary>
    public void RegisterRootNotifier(NodeState notifier)
    {
        AddRootNotifier(notifier);
    }

    private BaseDataVariableState CreateBaseVariable(BaseDataVariableState baseDataVariableState, NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, NamespaceType namespaceType, object defaultValue = null)
    {
        ushort namespaceIndex = NamespaceIndexes[(int)namespaceType];

        if (path is uint or long)
        {
            baseDataVariableState.NodeId = new NodeId((uint)path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(((uint)path).ToString(), namespaceIndex);
        }
        else if (path is string)
        {
            baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(path, namespaceIndex);
        }
        else
        {
            LogNodeIdType(path.GetType().ToString());
            baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(name, namespaceIndex);
        }

        baseDataVariableState.DisplayName = new LocalizedText("en", name);
        baseDataVariableState.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
        baseDataVariableState.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
        baseDataVariableState.DataType = dataType;
        baseDataVariableState.ValueRank = valueRank;
        baseDataVariableState.AccessLevel = accessLevel;
        baseDataVariableState.UserAccessLevel = accessLevel;
        baseDataVariableState.Historizing = false;
        baseDataVariableState.Value = defaultValue ?? TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
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
    /// Loads a predefined node set by using the specified handler.
    /// </summary>
    public void LoadPredefinedNodes(Func<ISystemContext, NodeStateCollection> loadPredefinedNodesHandler)
    {
        _loadPredefinedNodesHandler = loadPredefinedNodesHandler;

        base.LoadPredefinedNodes(SystemContext, _externalReferences);
    }

    /// <summary>
    /// Adds a predefined node set.
    /// </summary>
    public void AddPredefinedNode(NodeState node)
    {
        base.AddPredefinedNode(SystemContext, node);
    }

    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        return _loadPredefinedNodesHandler?.Invoke(context);
    }

    private readonly TimeService _timeService;
    private IDictionary<NodeId, IList<IReference>> _externalReferences;
    private Func<ISystemContext, NodeStateCollection> _loadPredefinedNodesHandler;

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error creating address space")]
    partial void LogErrorCreatingAddressSpace(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "NodeId type is {NodeIdType}")]
    partial void LogNodeIdType(string nodeIdType);
}
