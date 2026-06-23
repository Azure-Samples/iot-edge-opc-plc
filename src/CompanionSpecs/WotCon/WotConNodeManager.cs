// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Node manager for the OPC UA WoT-Con (Web of Things Connectivity) companion specification.
/// Manages WoT asset onboarding, Thing Description parsing, and asset property simulation.
/// </summary>
public partial class WotConNodeManager : CustomNodeManager2
{
    private const uint WotAssetConnectionManagementObjectId = 31;
    private const uint IWoTAssetTypeId = 42;
    private const uint CreateAssetMethodTypeId = 26;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint CreateAssetInputArgumentsId = 33;
    private const uint CreateAssetOutputArgumentsId = 34;
    private const uint DeleteAssetMethodTypeId = 29;
    private const uint DeleteAssetMethodInstanceId = 35;
    private const uint DeleteAssetInputArgumentsId = 36;

    // Per OPC 10100-1 §6.3.10: WoTAssetFileType (ns=WotCon;i=110) is a subtype of standard
    // FileType that adds a CloseAndUpdate method (type-method i=111). Each created asset
    // owns its own WoTAssetFileType instance; the singleton WoTFile node (i=144) shipped
    // in the NodeSet as a placeholder under <WoTAssetName> (i=2) is intentionally left
    // unreferenced.
    private const uint WoTAssetFileTypeId = 110;
    private const uint FileCloseAndUpdateTypeMethodId = 111;

    private readonly ILogger _logger;
    private readonly Dictionary<string, WotAsset> _assets;
    private readonly Dictionary<NodeId, WotAsset> _filesByNodeId = new();

    public WotConNodeManager(IServerInternal server, ApplicationConfiguration configuration, ILogger logger = null)
        : base(server, configuration)
    {
        _logger = logger;
        _assets = new Dictionary<string, WotAsset>();

        SetNamespaces(new[] { OpcPlc.Namespaces.WotCon });

        _logger?.LogInformation("[WotCon] WotConNodeManager initialized");
    }

    /// <summary>
    /// Loads the WoT-Con NodeSet and sets up the asset management surface.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();
        LoadNodeSet(context, predefinedNodes);
        return predefinedNodes;
    }

    /// <summary>
    /// Loads the WoT-Con NodeSet2 from the embedded or filesystem resource.
    /// </summary>
    private void LoadNodeSet(ISystemContext context, NodeStateCollection predefinedNodes)
    {
        try
        {
            var xmlPath = "CompanionSpecs/WotCon/Opc.Ua.WotCon.NodeSet2.xml";
            var snapLocation = Environment.GetEnvironmentVariable("SNAP");
            if (!string.IsNullOrWhiteSpace(snapLocation))
            {
                // Application running as a snap.
                xmlPath = Path.Join(snapLocation, xmlPath);
            }

            if (File.Exists(xmlPath))
            {
                using (var stream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read))
                {
                    LoadNodeSetFromStream(context, stream, predefinedNodes);
                }
            }
            else
            {
                _logger?.LogWarning("[WotCon] WoT-Con NodeSet2 not found at {Path}", xmlPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to load WoT-Con NodeSet");
        }
    }

    /// <summary>
    /// Called by the SDK after predefined nodes have been integrated into the address space.
    /// This is the correct hook for registering method handlers, because nodes are now
    /// reachable via FindPredefinedNode using the server-assigned namespace index.
    /// </summary>
    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        base.CreateAddressSpace(externalReferences);
        SetupMethodHandlers(SystemContext);
    }

    /// <summary>
    /// Diagnostic override: logs every incoming Call request and remaps type→instance MethodId
    /// as a workaround for clients that send the type-declaration MethodId on an instance object.
    /// </summary>
    public override void Call(
        OperationContext context,
        IList<CallMethodRequest> methodsToCall,
        IList<CallMethodResult> results,
        IList<ServiceResult> errors)
    {
        try
        {
            ushort nsIdx = (ushort)Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);
            var mgmtObjectId = new NodeId(WotAssetConnectionManagementObjectId, nsIdx);
            var createTypeMethodId = new NodeId(CreateAssetMethodTypeId, nsIdx);
            var createInstanceMethodId = new NodeId(CreateAssetMethodInstanceId, nsIdx);
            var deleteTypeMethodId = new NodeId(DeleteAssetMethodTypeId, nsIdx);
            var deleteInstanceMethodId = new NodeId(DeleteAssetMethodInstanceId, nsIdx);

            for (int i = 0; i < methodsToCall.Count; i++)
            {
                var req = methodsToCall[i];
                _logger?.LogInformation("[WotCon] Call request[{Idx}]: ObjectId={ObjectId} MethodId={MethodId}", i, req.ObjectId, req.MethodId);
                if (req.ObjectId == mgmtObjectId && req.MethodId == createTypeMethodId)
                {
                    _logger?.LogInformation("[WotCon] Remapping CreateAsset type MethodId {From} -> instance {To}", req.MethodId, createInstanceMethodId);
                    req.MethodId = createInstanceMethodId;
                }
                else if (req.ObjectId == mgmtObjectId && req.MethodId == deleteTypeMethodId)
                {
                    _logger?.LogInformation("[WotCon] Remapping DeleteAsset type MethodId {From} -> instance {To}", req.MethodId, deleteInstanceMethodId);
                    req.MethodId = deleteInstanceMethodId;
                }
                else if (_filesByNodeId.TryGetValue(req.ObjectId, out var fileAsset)
                    && fileAsset.FileMethodMap.TryGetValue(req.MethodId, out var instMethod))
                {
                    // Per-asset WoTAssetFileType: rewrite NS=0 FileType type-method IDs (and the
                    // WoT-Con CloseAndUpdate type-method ns=WotCon;i=111) to the per-asset instance.
                    _logger?.LogInformation("[WotCon] Remapping File type MethodId {From} -> instance {To} for asset {Asset}",
                        req.MethodId, instMethod, fileAsset.Name);
                    req.MethodId = instMethod;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[WotCon] Call override pre-processing failed");
        }

        base.Call(context, methodsToCall, results, errors);
    }

    /// <summary>
    /// Loads NodeSet from a stream using the UA SDK NodeSet importer.
    /// </summary>
    private void LoadNodeSetFromStream(ISystemContext context, Stream stream, NodeStateCollection predefinedNodes)
    {
        try
        {
            var nodeSet = UANodeSet.Read(stream);
            nodeSet.Import(context, predefinedNodes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception loading WoT-Con NodeSet");
        }
    }

    /// <summary>
    /// Registers method handlers on both the type declaration and instance nodes to work around
    /// OPC UA Stack 1.5.378 limitation where Call service doesn't resolve MethodDeclarationId.
    /// </summary>
    private void SetupMethodHandlers(ISystemContext context)
    {
        try
        {
            // The NodeSet may be assigned a different namespace index by the server than the
            // manager's own NamespaceIndex (the server tracks all imported namespaces in a global table).
            ushort wotConNamespaceIndex = (ushort)Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);

            // Find the WoTAssetConnectionManagement instance object (i=31)
            var managementObjectId = new NodeId(WotAssetConnectionManagementObjectId, wotConNamespaceIndex);
            var managementObject = FindPredefinedNode<BaseObjectState>(managementObjectId);

            if (managementObject == null)
            {
                _logger?.LogWarning("[WotCon] WoTAssetConnectionManagement object (ns={NamespaceIndex};i={ObjectId}) not found", wotConNamespaceIndex, WotAssetConnectionManagementObjectId);
                return;
            }

            // Diagnostics: enumerate children of management object to confirm method was materialized
            var mgmtChildren = new List<BaseInstanceState>();
            managementObject.GetChildren(context, mgmtChildren);
            _logger?.LogInformation("[WotCon] Management object (ns={Ns};i={Id}) has {Count} children", wotConNamespaceIndex, WotAssetConnectionManagementObjectId, mgmtChildren.Count);
            foreach (var c in mgmtChildren)
            {
                var asMethod = c as MethodState;
                _logger?.LogInformation("[WotCon]   child: NodeId={NodeId} BrowseName={Bn} Type={Type} MethodDeclarationId={Mdi}",
                    c.NodeId, c.BrowseName, c.GetType().Name, asMethod?.MethodDeclarationId);
            }

            // Find the CreateAsset method on the instance (i=32)
            var createAssetInstanceId = new NodeId(CreateAssetMethodInstanceId, wotConNamespaceIndex);
            var createAssetMethod = FindPredefinedNode<MethodState>(createAssetInstanceId);

            if (createAssetMethod == null)
            {
                _logger?.LogWarning("[WotCon] CreateAsset method instance (ns={NamespaceIndex};i={MethodId}) not found", wotConNamespaceIndex, CreateAssetMethodInstanceId);
                return;
            }

            _logger?.LogInformation("[WotCon] Found CreateAsset method instance NodeId={NodeId} MethodDeclarationId={Mdi}",
                createAssetMethod.NodeId, createAssetMethod.MethodDeclarationId);

            // Workaround for NodeSet2 importer in 1.5.378 not wiring strongly-typed properties:
            // 1. MethodState.InputArguments / OutputArguments fields stay null, so MethodState.Call
            //    treats expectedCount=0 and any client-supplied arg yields BadTooManyArguments.
            // 2. Parent BaseObjectState.GetChildren() returns 0 because HasComponent references
            //    are not materialized into m_children, so FindMethod can't locate the method.
            // Rehydrate both links manually.
            RehydrateMethodArguments(createAssetMethod, CreateAssetInputArgumentsId, CreateAssetOutputArgumentsId);
            RehydrateChildLink(managementObject, createAssetMethod);

            // Register handler on the instance method
            createAssetMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnCreateAsset);

            // Workaround for 1.5.378: also try to register on the type declaration (i=26)
            // This allows both type-based and instance-based Call dispatches to work
            var createAssetTypeId = new NodeId(CreateAssetMethodTypeId, wotConNamespaceIndex);
            var createAssetTypeMethod = FindPredefinedNode<MethodState>(createAssetTypeId);

            if (createAssetTypeMethod != null)
            {
                createAssetTypeMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnCreateAsset);
                _logger?.LogInformation("[WotCon] Registered OnCreateAsset handler on both type (i={TypeMethodId}) and instance (i={InstanceMethodId})", CreateAssetMethodTypeId, CreateAssetMethodInstanceId);
            }
            else
            {
                _logger?.LogInformation("[WotCon] CreateAsset method type (i={MethodId}) not found in predefined nodes", CreateAssetMethodTypeId);
            }

            // DeleteAsset (§6.3.3) — same NodeSet importer workaround: rehydrate the
            // InputArguments property (single AssetId : NodeId) and register the handler on
            // both the instance (i=35) and type (i=29) declarations.
            var deleteAssetInstanceId = new NodeId(DeleteAssetMethodInstanceId, wotConNamespaceIndex);
            var deleteAssetMethod = FindPredefinedNode<MethodState>(deleteAssetInstanceId);
            if (deleteAssetMethod != null)
            {
                RehydrateMethodArguments(deleteAssetMethod, DeleteAssetInputArgumentsId, outputArgumentsId: 0);
                RehydrateChildLink(managementObject, deleteAssetMethod);
                deleteAssetMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnDeleteAsset);

                var deleteAssetTypeId = new NodeId(DeleteAssetMethodTypeId, wotConNamespaceIndex);
                var deleteAssetTypeMethod = FindPredefinedNode<MethodState>(deleteAssetTypeId);
                if (deleteAssetTypeMethod != null)
                {
                    deleteAssetTypeMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnDeleteAsset);
                }

                _logger?.LogInformation("[WotCon] Registered OnDeleteAsset handler on instance (i={InstanceMethodId})", DeleteAssetMethodInstanceId);
            }
            else
            {
                _logger?.LogWarning("[WotCon] DeleteAsset method instance (ns={NamespaceIndex};i={MethodId}) not found", wotConNamespaceIndex, DeleteAssetMethodInstanceId);
            }

            _logger?.LogInformation("[WotCon] WoT-Con method handlers registered successfully (ns={NamespaceIndex})", wotConNamespaceIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to setup method handlers");
        }
    }

    /// <summary>
    /// Pulls an <see cref="Argument"/> array out of whatever the NodeSet importer stored
    /// (raw <see cref="Argument"/>[], <see cref="ExtensionObject"/>[], or a wrapper).
    /// </summary>
    private Argument[] ExtractArguments(object value)
    {
        if (value is Argument[] args)
        {
            return args;
        }

        if (value is ExtensionObject[] extensions)
        {
            var list = new Argument[extensions.Length];
            for (int i = 0; i < extensions.Length; i++)
            {
                list[i] = extensions[i]?.Body as Argument;
            }

            return list;
        }

        return null;
    }

    /// <summary>
    /// Finds the <c>InputArguments</c> / <c>OutputArguments</c> PropertyState siblings of
    /// <paramref name="method"/> in the predefined-nodes table by NodeId and assigns them to
    /// the strongly-typed MethodState properties so the SDK's argument-count validator sees
    /// the expected signature. Pass <c>0</c> for either ID to skip wiring that side.
    /// </summary>
    private void RehydrateMethodArguments(MethodState method, uint inputArgumentsId, uint outputArgumentsId)
    {
        // The NodeSet2 importer in 1.5.378 also leaves the method's HasProperty references
        // and the args' .Parent fields unpopulated, so we can't traverse from method to args
        // (or vice versa) through the SDK graph. Look the args up directly by NodeId — the
        // XML pins them with stable identifiers.
        ushort nsIdx = (ushort)Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);
        if (inputArgumentsId != 0)
        {
            WireArgProperty(method, new NodeId(inputArgumentsId, nsIdx), input: true);
        }

        if (outputArgumentsId != 0)
        {
            WireArgProperty(method, new NodeId(outputArgumentsId, nsIdx), input: false);
        }
    }

    /// <summary>
    /// Looks the property up in the predefined-nodes table by NodeId, adapts it to the
    /// strongly-typed <see cref="PropertyState{T}"/> the MethodState API expects, and assigns
    /// it to either <see cref="MethodState.InputArguments"/> or <see cref="MethodState.OutputArguments"/>.
    /// </summary>
    private void WireArgProperty(MethodState method, NodeId propertyId, bool input)
    {
        var node = FindPredefinedNode<BaseVariableState>(propertyId);
        if (node == null)
        {
            _logger?.LogWarning("[WotCon] {Kind}Arguments node {NodeId} not found in predefined nodes",
                input ? "Input" : "Output", propertyId);
            return;
        }

        var prop = ToArgumentProperty(node);
        if (input)
        {
            method.InputArguments = prop;
        }
        else
        {
            method.OutputArguments = prop;
        }

        _logger?.LogInformation("[WotCon] Wired {Kind}Arguments {NodeId} ({Count} args) onto method {Method}",
            input ? "Input" : "Output", propertyId, prop?.Value?.Length ?? 0, method.NodeId);
    }

    /// <summary>
    /// Adapts an arbitrary <see cref="BaseVariableState"/> instance into the strongly-typed
    /// <see cref="PropertyState{T}"/> the MethodState API requires. If it's already the right
    /// type, return as-is; otherwise build a new property carrying the same Value/NodeId.
    /// </summary>
    private PropertyState<Argument[]> ToArgumentProperty(BaseVariableState v)
    {
        if (v is PropertyState<Argument[]> p)
        {
            return p;
        }

        var prop = new PropertyState<Argument[]>(v.Parent)
        {
            NodeId = v.NodeId,
            BrowseName = v.BrowseName,
            DisplayName = v.DisplayName,
            TypeDefinitionId = VariableTypeIds.PropertyType,
            DataType = DataTypeIds.Argument,
            ValueRank = ValueRanks.OneDimension,
            Value = ExtractArguments(v.Value),
        };
        return prop;
    }

    /// <summary>
    /// Adds <paramref name="child"/> to the parent's child collection so the SDK's
    /// <c>BaseObjectState.GetChildren</c> (and therefore <c>NodeState.FindMethod</c>) can locate it.
    /// </summary>
    private void RehydrateChildLink(BaseObjectState parent, BaseInstanceState child)
    {
        var existing = new List<BaseInstanceState>();
        parent.GetChildren(SystemContext, existing);
        foreach (var c in existing)
        {
            if (c.NodeId == child.NodeId)
            {
                return;
            }
        }

        parent.AddChild(child);
        _logger?.LogInformation("[WotCon] Wired child link {Parent} -> {Child}", parent.NodeId, child.NodeId);
    }

    /// <summary>
    /// Handles CreateAsset method calls per the WoT-Con companion spec.
    /// The InputArgument is the <c>AssetName</c> (a friendly identifier the client picks).
    /// The handler creates a placeholder asset object — the Thing Description JSON is then
    /// uploaded separately by the client via the WoTFile File API, after which properties
    /// are materialized.
    /// </summary>
    private ServiceResult OnCreateAsset(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            _logger?.LogWarning("[WotCon] CreateAsset called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            var assetName = inputArguments[0] as string;
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument, "AssetName cannot be empty");
            }

            // Idempotency: return existing AssetId if the asset has already been created.
            if (_assets.TryGetValue(assetName, out var existingAsset))
            {
                outputArguments[0] = existingAsset.AssetId;
                _logger?.LogInformation("[WotCon] Asset '{AssetName}' already exists; returned existing AssetId {AssetId}", assetName, existingAsset.AssetId);
                return ServiceResult.Good;
            }

            // Create a placeholder asset node. Property materialization happens later when the
            // client uploads the Thing Description through the WoTFile File API.
            var placeholder = new ThingDescriptionInfo { Name = assetName };
            var asset = CreateAssetNode(context, placeholder);
            if (asset == null)
            {
                return new ServiceResult(StatusCodes.BadInternalError, "Failed to create asset node");
            }

            _assets[assetName] = asset;
            if (asset.FileNodeId != null)
            {
                _filesByNodeId[asset.FileNodeId] = asset;
            }

            outputArguments[0] = asset.AssetId;
            _logger?.LogInformation("[WotCon] Created WoT asset '{AssetName}' with AssetId {AssetId} and WoTFile {FileId}", assetName, asset.AssetId, asset.FileNodeId);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnCreateAsset");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    /// <summary>
    /// Handles DeleteAsset method calls per OPC 10100-1 §6.3.3. Removes the asset object,
    /// its per-asset WoTAssetFileType instance, materialized properties, and the
    /// <c>Organizes</c> reference from WoTAssetConnectionManagement. Closes any open file
    /// handles for the asset.
    /// </summary>
    private ServiceResult OnDeleteAsset(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            _logger?.LogWarning("[WotCon] DeleteAsset called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            var assetId = inputArguments[0] as NodeId;
            if (NodeId.IsNull(assetId))
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument, "AssetId cannot be null");
            }

            // Locate the asset by its root NodeId. Small N — linear scan is fine.
            string assetName = null;
            WotAsset asset = null;
            foreach (var kvp in _assets)
            {
                if (kvp.Value.AssetId == assetId)
                {
                    assetName = kvp.Key;
                    asset = kvp.Value;
                    break;
                }
            }

            if (asset == null)
            {
                _logger?.LogInformation("[WotCon] DeleteAsset: AssetId {AssetId} not found", assetId);
                return new ServiceResult(StatusCodes.BadNotFound);
            }

            // Close any open file handles for this asset so MemoryStreams don't leak.
            lock (asset.FileLock)
            {
                foreach (var stream in asset.FileBuffers.Values)
                {
                    stream.Dispose();
                }

                asset.FileBuffers.Clear();
            }

            // Remove the forward Organizes ref from WoTAssetConnectionManagement so the asset
            // stops being browseable from the entry point. The inverse on the asset goes away
            // with DeleteNode below.
            var managementNodeId = new NodeId(WotAssetConnectionManagementObjectId, NamespaceIndex);
            var managementObject = FindPredefinedNode<BaseObjectState>(managementNodeId);
            managementObject?.RemoveReference(ReferenceTypeIds.Organizes, isInverse: false, assetId);

            // DeleteNode recursively removes the asset and all HasComponent children
            // (the per-asset WoTFile + its standard FileType properties and methods,
            // plus any materialized TD properties).
            bool deleted = DeleteNode(SystemContext, assetId);

            _assets.Remove(assetName);
            if (asset.FileNodeId != null)
            {
                _filesByNodeId.Remove(asset.FileNodeId);
            }

            if (!deleted)
            {
                _logger?.LogWarning("[WotCon] DeleteAsset: DeleteNode returned false for {AssetId}", assetId);
            }

            _logger?.LogInformation("[WotCon] Deleted WoT asset '{AssetName}' AssetId={AssetId}", assetName, assetId);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnDeleteAsset");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    /// <summary>
    /// Parses a Thing Description JSON-LD and extracts asset information.
    /// </summary>
    private ThingDescriptionInfo ParseThingDescription(string json)
    {
        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                // Extract title as asset name
                if (!root.TryGetProperty("title", out var titleElement) || titleElement.ValueKind != JsonValueKind.String)
                {
                    _logger?.LogWarning("[WotCon] Thing Description missing 'title' field");
                    return null;
                }

                var assetName = titleElement.GetString();
                if (string.IsNullOrWhiteSpace(assetName))
                {
                    _logger?.LogWarning("[WotCon] Thing Description title is empty");
                    return null;
                }

                // Extract properties and their metadata
                var properties = new Dictionary<string, PropertyInfo>();

                if (root.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in propertiesElement.EnumerateObject())
                    {
                        var propName = prop.Name;
                        var propValue = prop.Value;

                        if (propValue.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var propertyInfo = new PropertyInfo { Name = propName };

                        // Extract type
                        if (propValue.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                        {
                            propertyInfo.Type = typeElement.GetString();
                        }

                        // Extract description
                        if (propValue.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
                        {
                            propertyInfo.Description = descElement.GetString();
                        }

                        properties[propName] = propertyInfo;
                    }
                }

                return new ThingDescriptionInfo
                {
                    Name = assetName,
                    Properties = properties,
                };
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "[WotCon] Failed to parse Thing Description JSON");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception parsing Thing Description");
            return null;
        }
    }

    /// <summary>
    /// Creates an OPC UA asset node with properties from the Thing Description, plus a
    /// per-asset WoTAssetFileType instance for TD upload. Returns a populated
    /// <see cref="WotAsset"/> with AssetId, FileNodeId and the type-method to instance-method
    /// remap table the <see cref="Call"/> override needs.
    /// </summary>
    private WotAsset CreateAssetNode(ISystemContext context, ThingDescriptionInfo assetInfo)
    {
        try
        {
            // Create asset root object (BaseObjectState)
            var assetNodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
            var assetNode = new BaseObjectState(null)
            {
                NodeId = assetNodeId,
                BrowseName = new QualifiedName(assetInfo.Name, NamespaceIndex),
                DisplayName = assetInfo.Name,
                TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            };

            // Per OPC 10100-1 §6.3.2: link the new asset to WoTAssetConnectionManagement (i=31)
            // with a forward Organizes reference so the asset is browseable from the entry point.
            // We add the inverse on the asset side now (cheap, the node is fresh) and the forward
            // on the (already-loaded) management object below.
            assetNode.AddReference(
                ReferenceTypeIds.Organizes,
                isInverse: true,
                new NodeId(WotAssetConnectionManagementObjectId, NamespaceIndex));

            // Per OPC 10100-1 §6.3.8: the new Object implements the IWoTAssetType Interface.
            // The NodeSet's <WoTAssetName> placeholder (ns=1;i=2) follows the same pattern:
            // TypeDefinition=BaseObjectType plus HasInterface to IWoTAssetType (ns=1;i=42).
            assetNode.AddReference(
                ReferenceTypeIds.HasInterface,
                isInverse: false,
                new NodeId(IWoTAssetTypeId, NamespaceIndex));

            var asset = new WotAsset
            {
                Name = assetInfo.Name,
                AssetId = assetNodeId,
                ThingDescription = null,
            };

            // Per OPC 10100-1 §6.3.10: each asset owns a WoTAssetFileType instance (HasComponent
            // child), carrying its own Open/Read/Write/Close/GetPosition/SetPosition + the
            // WoT-Con CloseAndUpdate extension. The instance and its method NodeIds are unique
            // per asset so concurrent uploads do not collide.
            CreateAssetFileNode(context, assetNode, asset);

            // Create property variables based on Thing Description
            foreach (var property in assetInfo.Properties.Values)
            {
                var propertyNodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
                var builtInType = GetBuiltInTypeFromJson(property.Type);

                var propertyNode = new BaseDataVariableState(assetNode)
                {
                    NodeId = propertyNodeId,
                    BrowseName = new QualifiedName(property.Name, NamespaceIndex),
                    DisplayName = property.Name,
                    Description = property.Description ?? string.Empty,
                    DataType = builtInType,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = AccessLevels.CurrentRead,
                };

                // Initialize with a simulated value
                propertyNode.Value = GenerateSimulatedValue(builtInType);
                propertyNode.StatusCode = StatusCodes.Good;
                propertyNode.Timestamp = DateTime.UtcNow;

                // Add property to asset
                assetNode.AddChild(propertyNode);
            }

            // Add the asset to the server's address space
            AddPredefinedNode(context, assetNode);

            // Forward Organizes ref on the management object (already loaded from NodeSet).
            // Pair to the inverse added on the asset above; together they make the asset
            // browseable from WoTAssetConnectionManagement per OPC 10100-1 §6.3.2.
            var managementNodeId = new NodeId(WotAssetConnectionManagementObjectId, NamespaceIndex);
            var managementObject = FindPredefinedNode<BaseObjectState>(managementNodeId);
            if (managementObject != null)
            {
                managementObject.AddReference(ReferenceTypeIds.Organizes, isInverse: false, assetNodeId);
            }
            else
            {
                _logger?.LogWarning(
                    "[WotCon] WoTAssetConnectionManagement (i={NodeId}) not found; asset {AssetId} will not be browseable from entry point",
                    WotAssetConnectionManagementObjectId,
                    assetNodeId);
            }

            return asset;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to create asset node for '{AssetName}'", assetInfo.Name);
            return null;
        }
    }

    /// <summary>
    /// Maps JSON type names to OPC UA built-in data types.
    /// </summary>
    private NodeId GetBuiltInTypeFromJson(string jsonType)
    {
        return jsonType?.ToLowerInvariant() switch
        {
            "number" => DataTypeIds.Double,
            "integer" => DataTypeIds.Int32,
            "boolean" => DataTypeIds.Boolean,
            "string" => DataTypeIds.String,
            _ => DataTypeIds.String,
        };
    }

    /// <summary>
    /// Generates a simulated value for a property based on its data type.
    /// </summary>
    private object GenerateSimulatedValue(NodeId dataTypeId)
    {
        if (dataTypeId == DataTypeIds.Double)
        {
            return 42.0; // Simulate a sensor reading
        }

        if (dataTypeId == DataTypeIds.Int32)
        {
            return 100; // Simulate a counter
        }

        if (dataTypeId == DataTypeIds.Boolean)
        {
            return true; // Simulate a switch
        }

        // Default to empty string
        return string.Empty;
    }

    /// <summary>
    /// Creates a per-asset WoTAssetFileType instance (ns=WotCon;i=110) as a HasComponent
    /// child of <paramref name="assetNode"/>. Uses the SDK's <see cref="FileState"/> so the
    /// full standard FileType layout (Size, Writable, UserWritable, OpenCount, MimeType,
    /// MaxByteStringLength, LastModifiedTime + Open/Close/Read/Write/GetPosition/SetPosition)
    /// is materialized automatically per OPC 10000-5 §10. The WoT-Con-specific CloseAndUpdate
    /// method (type-method ns=WotCon;i=111) is added as an additional HasComponent child.
    /// Populates <see cref="WotAsset.FileNodeId"/> and <see cref="WotAsset.FileMethodMap"/>
    /// so the Call override can rewrite incoming type-method IDs onto this instance.
    /// </summary>
    private void CreateAssetFileNode(ISystemContext context, BaseObjectState assetNode, WotAsset asset)
    {
        var fileNodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        var fileNode = new FileState(assetNode)
        {
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = new NodeId(WoTAssetFileTypeId, NamespaceIndex),
        };
        fileNode.Create(context, fileNodeId, new QualifiedName("WoTFile", NamespaceIndex), new Opc.Ua.LocalizedText("WoTFile"), assignNodeIds: true);

        // Mandatory FileType properties.
        fileNode.Size.Value = 0UL;
        fileNode.Writable.Value = true;
        fileNode.UserWritable.Value = true;
        fileNode.OpenCount.Value = 0;

        // Optional FileType properties (materialized by InitializeOptionalChildren).
        if (fileNode.MimeType != null)
        {
            fileNode.MimeType.Value = "application/td+json";
        }

        if (fileNode.MaxByteStringLength != null)
        {
            fileNode.MaxByteStringLength.Value = 16U * 1024U * 1024U;
        }

        if (fileNode.LastModifiedTime != null)
        {
            fileNode.LastModifiedTime.Value = DateTime.UtcNow;
        }

        // Wire per-asset handlers onto the auto-generated standard FileType methods.
        fileNode.Open.OnCallMethod = (c, m, i, o) => OnPerAssetFileOpen(asset, fileNode, i, o);
        fileNode.Close.OnCallMethod = (c, m, i, o) => OnPerAssetFileClose(asset, fileNode, i);
        fileNode.Read.OnCallMethod = (c, m, i, o) => OnPerAssetFileRead(asset, i, o);
        fileNode.Write.OnCallMethod = (c, m, i, o) => OnPerAssetFileWrite(asset, fileNode, i);
        fileNode.GetPosition.OnCallMethod = (c, m, i, o) => OnPerAssetFileGetPosition(asset, i, o);
        fileNode.SetPosition.OnCallMethod = (c, m, i, o) => OnPerAssetFileSetPosition(asset, i);

        // Defensive: remap NS=0 FileType type-method IDs onto this instance's method NodeIds
        // for clients that call the type-method instead of browsing for the instance method.
        asset.FileMethodMap[new NodeId(Methods.FileType_Open, 0)] = fileNode.Open.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Close, 0)] = fileNode.Close.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Read, 0)] = fileNode.Read.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Write, 0)] = fileNode.Write.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_GetPosition, 0)] = fileNode.GetPosition.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_SetPosition, 0)] = fileNode.SetPosition.NodeId;

        // WoT-Con-specific CloseAndUpdate (OPC 10100-1 §6.3.10) — added alongside Close, not
        // a replacement for it. Spec defines a single FileHandle UInt32 input argument.
        var closeAndUpdate = CreateFileMethod(fileNode, "CloseAndUpdate",
            FileCloseAndUpdateTypeMethodId, namespaceIndex: NamespaceIndex,
            inputArgs: new[] { MakeArg("FileHandle", DataTypes.UInt32) }, outputArgs: null,
            handler: (c, m, i, o) => OnPerAssetFileCloseAndUpdate(asset, fileNode, i));
        asset.FileMethodMap[new NodeId(FileCloseAndUpdateTypeMethodId, NamespaceIndex)] = closeAndUpdate.NodeId;

        assetNode.AddChild(fileNode);
        asset.FileNodeId = fileNode.NodeId;
    }

    private Argument MakeArg(string name, uint dataTypeId) => new()
    {
        Name = name,
        DataType = new NodeId(dataTypeId, 0),
        ValueRank = ValueRanks.Scalar,
    };

    private MethodState CreateFileMethod(
        BaseObjectState parent,
        string browseName,
        uint methodDeclarationId,
        ushort namespaceIndex,
        Argument[] inputArgs,
        Argument[] outputArgs,
        GenericMethodCalledEventHandler handler)
    {
        var method = new MethodState(parent)
        {
            NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
            BrowseName = new QualifiedName(browseName, NamespaceIndex),
            DisplayName = browseName,
            SymbolicName = browseName,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            MethodDeclarationId = new NodeId(methodDeclarationId, namespaceIndex),
            Executable = true,
            UserExecutable = true,
            OnCallMethod = handler,
        };

        if (inputArgs != null)
        {
            method.InputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
                BrowseName = BrowseNames.InputArguments,
                DisplayName = BrowseNames.InputArguments,
                TypeDefinitionId = VariableTypeIds.PropertyType,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                DataType = DataTypeIds.Argument,
                ValueRank = ValueRanks.OneDimension,
                Value = inputArgs,
            };
        }

        if (outputArgs != null)
        {
            method.OutputArguments = new PropertyState<Argument[]>(method)
            {
                NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
                BrowseName = BrowseNames.OutputArguments,
                DisplayName = BrowseNames.OutputArguments,
                TypeDefinitionId = VariableTypeIds.PropertyType,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                DataType = DataTypeIds.Argument,
                ValueRank = ValueRanks.OneDimension,
                Value = outputArgs,
            };
        }

        parent.AddChild(method);
        return method;
    }

    private ServiceResult OnPerAssetFileOpen(WotAsset asset, FileState fileNode, IList<object> inputArguments, IList<object> outputArguments)
    {
        try
        {
            uint handle;
            lock (asset.FileLock)
            {
                handle = asset.NextFileHandle++;
                asset.FileBuffers[handle] = new MemoryStream();
            }

            outputArguments[0] = handle;
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, asset.FileBuffers.Count);
            }

            _logger?.LogInformation("[WotCon] {Asset}.Open -> handle {Handle}", asset.Name, handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileOpen failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileWrite(WotAsset asset, FileState fileNode, IList<object> inputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            byte[] data = inputArguments[1] as byte[] ?? Array.Empty<byte>();

            MemoryStream stream;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out stream))
                {
                    return new ServiceResult(StatusCodes.BadInvalidArgument, "Unknown file handle");
                }
            }

            stream.Write(data, 0, data.Length);
            fileNode.Size.Value = (ulong)stream.Length;
            _logger?.LogInformation("[WotCon] {Asset}.Write handle={Handle} wrote {Bytes} bytes (total {Total})",
                asset.Name, handle, data.Length, stream.Length);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileWrite failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileRead(WotAsset asset, IList<object> inputArguments, IList<object> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            int length = Convert.ToInt32(inputArguments[1]);

            MemoryStream stream;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out stream))
                {
                    return new ServiceResult(StatusCodes.BadInvalidArgument, "Unknown file handle");
                }
            }

            var buffer = new byte[Math.Max(0, length)];
            int read = length > 0 ? stream.Read(buffer, 0, length) : 0;
            var result = new byte[read];
            Array.Copy(buffer, result, read);
            outputArguments[0] = result;
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileRead failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileClose(WotAsset asset, FileState fileNode, IList<object> inputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            CloseAssetHandle(asset, handle);
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, asset.FileBuffers.Count);
            }

            _logger?.LogInformation("[WotCon] {Asset}.Close handle={Handle}", asset.Name, handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileClose failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileGetPosition(WotAsset asset, IList<object> inputArguments, IList<object> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            MemoryStream stream;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out stream))
                {
                    return new ServiceResult(StatusCodes.BadInvalidArgument, "Unknown file handle");
                }
            }

            outputArguments[0] = (ulong)stream.Position;
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileGetPosition failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileSetPosition(WotAsset asset, IList<object> inputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            ulong position = Convert.ToUInt64(inputArguments[1]);
            MemoryStream stream;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out stream))
                {
                    return new ServiceResult(StatusCodes.BadInvalidArgument, "Unknown file handle");
                }
            }

            stream.Position = (long)position;
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileSetPosition failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileCloseAndUpdate(WotAsset asset, FileState fileNode, IList<object> inputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            byte[] payload = null;
            lock (asset.FileLock)
            {
                if (asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    payload = stream.ToArray();
                }
            }

            CloseAssetHandle(asset, handle);
            asset.LastFinalizedPayload = payload;
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, asset.FileBuffers.Count);
            }

            if (fileNode.LastModifiedTime != null)
            {
                fileNode.LastModifiedTime.Value = DateTime.UtcNow;
            }

            _logger?.LogInformation("[WotCon] {Asset}.CloseAndUpdate handle={Handle} payload {Bytes} bytes",
                asset.Name, handle, payload?.Length ?? 0);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileCloseAndUpdate failed", asset.Name);
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private void CloseAssetHandle(WotAsset asset, uint handle)
    {
        lock (asset.FileLock)
        {
            if (asset.FileBuffers.TryGetValue(handle, out var stream))
            {
                stream.Dispose();
                asset.FileBuffers.Remove(handle);
            }
        }
    }

    /// <summary>
    /// Internal model for Thing Description metadata.
    /// </summary>
    private class ThingDescriptionInfo
    {
        public string Name { get; set; }

        public Dictionary<string, PropertyInfo> Properties { get; set; } = new();
    }

    /// <summary>
    /// Internal model for a property from a Thing Description.
    /// </summary>
    private class PropertyInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// Internal model for a managed asset.
    /// </summary>
    private sealed class WotAsset
    {
        public string Name { get; set; }

        public NodeId AssetId { get; set; }

        public string ThingDescription { get; set; }

        // Per-asset WoTAssetFileType instance NodeId (HasComponent child of the asset).
        public NodeId FileNodeId { get; set; }

        // Type-method NodeId → per-asset instance-method NodeId. Used by the Call
        // override to rewrite NS=0 FileType_Open/Close/Read/Write/GetPosition/SetPosition
        // and the WoT-Con CloseAndUpdate type-method (ns=WotCon;i=111) onto this asset's
        // instance methods.
        public Dictionary<NodeId, NodeId> FileMethodMap { get; } = new();

        // Active upload buffers keyed by file handle returned from Open.
        public Dictionary<uint, MemoryStream> FileBuffers { get; } = new();

        public object FileLock { get; } = new();

        public uint NextFileHandle { get; set; } = 1;

        // Most recent payload finalized via CloseAndUpdate. Kept for diagnostics and to
        // support TD materialization later in the plan.
        public byte[] LastFinalizedPayload { get; set; }
    }
}
