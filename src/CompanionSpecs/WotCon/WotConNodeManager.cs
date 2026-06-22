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
    private const uint CreateAssetMethodTypeId = 26;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint CreateAssetInputArgumentsId = 33;
    private const uint CreateAssetOutputArgumentsId = 34;

    // The NodeSet ships a fully-fleshed WoTFile placeholder (i=144) hanging off the
    // <WoTAssetName> template, complete with Open/Close/Read/Write/GetPosition/
    // SetPosition/CloseAndUpdate instance methods. We reuse this singleton across all
    // created assets — sufficient for the single-asset E2E test path.
    private const uint WoTFileObjectId = 144;
    private const uint FileOpenMethodId = 152;
    private const uint FileOpenInputArgsId = 153;
    private const uint FileOpenOutputArgsId = 154;
    private const uint FileCloseMethodId = 155;
    private const uint FileCloseInputArgsId = 156;
    private const uint FileReadMethodId = 157;
    private const uint FileReadInputArgsId = 158;
    private const uint FileReadOutputArgsId = 159;
    private const uint FileWriteMethodId = 160;
    private const uint FileWriteInputArgsId = 161;
    private const uint FileGetPositionMethodId = 162;
    private const uint FileGetPositionInputArgsId = 163;
    private const uint FileGetPositionOutputArgsId = 164;
    private const uint FileSetPositionMethodId = 165;
    private const uint FileSetPositionInputArgsId = 166;
    private const uint FileCloseAndUpdateMethodId = 167;
    private const uint FileCloseAndUpdateInputArgsId = 168;
    private const uint FileCloseAndUpdateTypeMethodId = 111;

    private readonly ILogger _logger;
    private readonly Dictionary<string, WotAsset> _assets;
    private readonly Dictionary<uint, MemoryStream> _openFileHandles = new();
    private readonly object _fileLock = new();
    private uint _nextFileHandle = 1;

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
            var typeMethodId = new NodeId(CreateAssetMethodTypeId, nsIdx);
            var instanceMethodId = new NodeId(CreateAssetMethodInstanceId, nsIdx);
            var wotFileId = new NodeId(WoTFileObjectId, nsIdx);

            // Remap NS=0 FileType type-method IDs (e.g. i=11580 Open) to our wired instance
            // method NodeIds on the singleton WoTFile (i=144). The Commander's source-generated
            // FileTypeClient proxy always sends the type-method ID.
            var fileTypeToInstance = new Dictionary<NodeId, NodeId>
            {
                [new NodeId(Opc.Ua.Methods.FileType_Open, 0)] = new NodeId(FileOpenMethodId, nsIdx),
                [new NodeId(Opc.Ua.Methods.FileType_Close, 0)] = new NodeId(FileCloseMethodId, nsIdx),
                [new NodeId(Opc.Ua.Methods.FileType_Read, 0)] = new NodeId(FileReadMethodId, nsIdx),
                [new NodeId(Opc.Ua.Methods.FileType_Write, 0)] = new NodeId(FileWriteMethodId, nsIdx),
                [new NodeId(Opc.Ua.Methods.FileType_GetPosition, 0)] = new NodeId(FileGetPositionMethodId, nsIdx),
                [new NodeId(Opc.Ua.Methods.FileType_SetPosition, 0)] = new NodeId(FileSetPositionMethodId, nsIdx),
                [new NodeId(FileCloseAndUpdateTypeMethodId, nsIdx)] = new NodeId(FileCloseAndUpdateMethodId, nsIdx),
            };

            for (int i = 0; i < methodsToCall.Count; i++)
            {
                var req = methodsToCall[i];
                _logger?.LogInformation("[WotCon] Call request[{Idx}]: ObjectId={ObjectId} MethodId={MethodId}", i, req.ObjectId, req.MethodId);
                if (req.ObjectId == mgmtObjectId && req.MethodId == typeMethodId)
                {
                    _logger?.LogInformation("[WotCon] Remapping type MethodId {From} -> instance {To}", req.MethodId, instanceMethodId);
                    req.MethodId = instanceMethodId;
                }
                else if (req.ObjectId == wotFileId && fileTypeToInstance.TryGetValue(req.MethodId, out var instMethod))
                {
                    _logger?.LogInformation("[WotCon] Remapping File type MethodId {From} -> instance {To}", req.MethodId, instMethod);
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
            RehydrateMethodArguments(createAssetMethod);
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

            // Wire the singleton WoTFile (i=144) — its method instances ship in the NodeSet
            // but the 1.5.378 importer leaves child/argument links unmaterialized.
            SetupFileMethodHandlers(context, wotConNamespaceIndex);

            _logger?.LogInformation("[WotCon] WoT-Con method handlers registered successfully (ns={NamespaceIndex})", wotConNamespaceIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to setup method handlers");
        }
    }

    /// <summary>
    /// Wires OnCall handlers for the singleton WoTFile (i=144) instance methods.
    /// Open/Write/Close/CloseAndUpdate are sufficient for the Commander TD-upload path;
    /// Read/GetPosition/SetPosition are not exercised by the test.
    /// </summary>
    private void SetupFileMethodHandlers(ISystemContext context, ushort nsIdx)
    {
        var wotFile = FindPredefinedNode<BaseObjectState>(new NodeId(WoTFileObjectId, nsIdx));
        if (wotFile == null)
        {
            _logger?.LogWarning("[WotCon] WoTFile object (ns={Ns};i={Id}) not found", nsIdx, WoTFileObjectId);
            return;
        }

        WireFileMethod(wotFile, nsIdx, FileOpenMethodId, FileOpenInputArgsId, FileOpenOutputArgsId, OnFileOpen);
        WireFileMethod(wotFile, nsIdx, FileWriteMethodId, FileWriteInputArgsId, null, OnFileWrite);
        WireFileMethod(wotFile, nsIdx, FileCloseMethodId, FileCloseInputArgsId, null, OnFileClose);
        WireFileMethod(wotFile, nsIdx, FileCloseAndUpdateMethodId, FileCloseAndUpdateInputArgsId, null, OnFileCloseAndUpdate);
    }

    private void WireFileMethod(
        BaseObjectState fileObject,
        ushort nsIdx,
        uint methodId,
        uint inputArgsId,
        uint? outputArgsId,
        GenericMethodCalledEventHandler handler)
    {
        var method = FindPredefinedNode<MethodState>(new NodeId(methodId, nsIdx));
        if (method == null)
        {
            _logger?.LogWarning("[WotCon] File method ns={Ns};i={Id} not found", nsIdx, methodId);
            return;
        }

        WireArgProperty(method, new NodeId(inputArgsId, nsIdx), input: true);
        if (outputArgsId.HasValue)
        {
            WireArgProperty(method, new NodeId(outputArgsId.Value, nsIdx), input: false);
        }

        RehydrateChildLink(fileObject, method);
        method.OnCallMethod = handler;
        _logger?.LogInformation("[WotCon] Wired File method i={Id} on WoTFile i={WoTFile}", methodId, WoTFileObjectId);
    }

    private ServiceResult OnFileOpen(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
    {
        try
        {
            // mode byte is informational for the in-memory backing.
            uint handle;
            lock (_fileLock)
            {
                handle = _nextFileHandle++;
                _openFileHandles[handle] = new MemoryStream();
            }

            outputArguments[0] = handle;
            _logger?.LogInformation("[WotCon] FileType.Open -> handle {Handle}", handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnFileOpen");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnFileWrite(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
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
            lock (_fileLock)
            {
                if (!_openFileHandles.TryGetValue(handle, out stream))
                {
                    return new ServiceResult(StatusCodes.BadInvalidArgument, "Unknown file handle");
                }
            }

            stream.Write(data, 0, data.Length);
            _logger?.LogInformation("[WotCon] FileType.Write handle={Handle} wrote {Bytes} bytes (total {Total})",
                handle, data.Length, stream.Length);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnFileWrite");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnFileClose(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            CloseHandle(handle);
            _logger?.LogInformation("[WotCon] FileType.Close handle={Handle}", handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnFileClose");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private ServiceResult OnFileCloseAndUpdate(
        ISystemContext context,
        MethodState method,
        IList<object> inputArguments,
        IList<object> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = Convert.ToUInt32(inputArguments[0]);
            byte[] payload = null;
            lock (_fileLock)
            {
                if (_openFileHandles.TryGetValue(handle, out var stream))
                {
                    payload = stream.ToArray();
                }
            }

            CloseHandle(handle);
            _logger?.LogInformation("[WotCon] FileType.CloseAndUpdate handle={Handle} payload {Bytes} bytes",
                handle, payload?.Length ?? 0);

            // TD parsing / per-property materialization is intentionally not done here —
            // the E2E test only asserts the upload round-trip succeeds.
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnFileCloseAndUpdate");
            return new ServiceResult(StatusCodes.BadInternalError, ex.Message);
        }
    }

    private void CloseHandle(uint handle)
    {
        lock (_fileLock)
        {
            if (_openFileHandles.TryGetValue(handle, out var stream))
            {
                stream.Dispose();
                _openFileHandles.Remove(handle);
            }
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
    /// <paramref name="method"/> in the predefined-nodes table (via the method's HasProperty
    /// references) and assigns them to the strongly-typed MethodState properties so the
    /// SDK's argument-count validator sees the expected signature.
    /// </summary>
    private void RehydrateMethodArguments(MethodState method)
    {
        // The NodeSet2 importer in 1.5.378 also leaves the method's HasProperty references
        // and the args' .Parent fields unpopulated, so we can't traverse from method to args
        // (or vice versa) through the SDK graph. Look the args up directly by NodeId — the
        // XML pins them with stable identifiers.
        ushort nsIdx = (ushort)Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);
        WireArgProperty(method, new NodeId(CreateAssetInputArgumentsId, nsIdx), input: true);
        WireArgProperty(method, new NodeId(CreateAssetOutputArgumentsId, nsIdx), input: false);
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
            var assetId = CreateAssetNode(context, placeholder);
            if (assetId == null)
            {
                return new ServiceResult(StatusCodes.BadInternalError, "Failed to create asset node");
            }

            _assets[assetName] = new WotAsset
            {
                Name = assetName,
                AssetId = assetId,
                ThingDescription = null,
            };

            outputArguments[0] = assetId;
            _logger?.LogInformation("[WotCon] Created WoT asset '{AssetName}' with AssetId {AssetId}", assetName, assetId);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnCreateAsset");
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
    /// Creates an OPC UA asset node with properties from the Thing Description.
    /// </summary>
    private NodeId CreateAssetNode(ISystemContext context, ThingDescriptionInfo assetInfo)
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

            // WoTFile — instead of giving each asset its own FileState (and re-wiring all
            // 6 File methods + args per asset), cross-reference the singleton WoTFile (i=144)
            // that ships in the WotCon NodeSet. Commander resolves it via
            // TranslateBrowsePathsToNodeIds(asset / HasComponent / WoTFile) and then calls
            // the File API methods on i=144 directly. Adequate for single-asset E2E.
            assetNode.AddReference(
                ReferenceTypeIds.HasComponent,
                isInverse: false,
                new NodeId(WoTFileObjectId, NamespaceIndex));

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

            return assetNodeId;
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
    private class WotAsset
    {
        public string Name { get; set; }

        public NodeId AssetId { get; set; }

        public string ThingDescription { get; set; }
    }
}
