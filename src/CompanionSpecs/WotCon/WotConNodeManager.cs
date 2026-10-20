// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LocalizedText = Opc.Ua.LocalizedText;

/// <summary>
/// Node manager for the OPC UA WoT-Con (Web of Things Connectivity) companion specification.
/// Manages WoT asset onboarding, Thing Description parsing, and asset property simulation.
/// </summary>
public partial class WotConNodeManager : AsyncCustomNodeManager
{
    // Local aliases bound to the source-generated NodeIds in Opc.Ua.WotCon.Constants.cs
    // (produced by ModelCompiler from WotConnection.xml). They keep the readable, intent-
    // revealing names used throughout this file while ensuring a compile-time link to the
    // canonical NodeSet — a NodeSet regeneration that renames or removes any of these
    // breaks the build immediately instead of silently mismatching at runtime.
    private const uint WotAssetConnectionManagementObjectId = RuntimeModelIds.WotCon.Objects.WoTAssetConnectionManagement;
    private const uint IWoTAssetTypeId = RuntimeModelIds.WotCon.ObjectTypes.IWoTAssetType;
    private const uint CreateAssetMethodTypeId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagementType_CreateAsset;
    private const uint CreateAssetMethodInstanceId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagement_CreateAsset;
    private const uint CreateAssetInputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagement_CreateAsset_InputArguments;
    private const uint CreateAssetOutputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagement_CreateAsset_OutputArguments;
    private const uint DeleteAssetMethodTypeId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagementType_DeleteAsset;
    private const uint DeleteAssetMethodInstanceId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagement_DeleteAsset;
    private const uint DeleteAssetInputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagement_DeleteAsset_InputArguments;

    // Per OPC 10100-1 §6.3.10: WoTAssetFileType (ns=WotCon;i=110) is a subtype of standard
    // FileType that adds a CloseAndUpdate method (type-method i=111). Each created asset
    // owns its own WoTAssetFileType instance; the singleton WoTFile node (i=144) shipped
    // in the NodeSet as a placeholder under <WoTAssetName> (i=2) is intentionally left
    // unreferenced.
    private const uint WoTAssetFileTypeId = RuntimeModelIds.WotCon.ObjectTypes.WoTAssetFileType;
    private const uint FileCloseAndUpdateTypeMethodId = RuntimeModelIds.WotCon.Methods.WoTAssetFileType_CloseAndUpdate;

    // Per OPC 10100-1 §6.3.11: HasWoTComponent (ns=WotCon;i=142) is a subtype of
    // HasComponent (i=47) used to link an asset to its materialized WoT affordances
    // (Variables for Properties, Methods for Actions). Generic HasComponent stays in
    // use for non-affordance plumbing such as the per-asset WoTFile.
    private const uint HasWoTComponentReferenceTypeId = RuntimeModelIds.WotCon.ReferenceTypes.HasWoTComponent;

    // Strict UTF-8 decoder: throws DecoderFallbackException on malformed byte sequences
    // instead of silently substituting U+FFFD. Used to validate uploaded TD payloads.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly ILogger _logger;
    private readonly TimeService _timeService;
    private readonly SemaphoreSlim _mutationGate = new(1, 1);
    private int _stopping;

    // Registries support concurrent discovery and file calls. Address-space mutations
    // are serialized by _mutationGate; per-asset file buffers retain their own lock.
    private readonly ConcurrentDictionary<string, WotAsset> _assets = new();
    private readonly ConcurrentDictionary<NodeId, WotAsset> _filesByNodeId = new();

    public WotConNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService, ILogger logger = null)
        : base(server, configuration)
    {
        _timeService = timeService ?? new TimeService();
        _logger = logger;

        SetNamespaces(new[] { OpcPlc.Namespaces.WotCon });

        _logger?.LogInformation("[WotCon] WotConNodeManager initialized");
    }

    /// <summary>
    /// Loads the WoT-Con NodeSet and sets up the asset management surface.
    /// </summary>
    protected override ValueTask<NodeStateCollection> LoadPredefinedNodesAsync(
        ISystemContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var predefinedNodes = new NodeStateCollection();
        LoadNodeSet(context, predefinedNodes);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(predefinedNodes);
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
                using var stream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read);
                LoadNodeSetFromStream(context, stream, predefinedNodes);
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
    public override async ValueTask CreateAddressSpaceAsync(
        IDictionary<NodeId, IList<IReference>> externalReferences, CancellationToken cancellationToken = default)
    {
        await base.CreateAddressSpaceAsync(externalReferences, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        SetupMethodHandlers(SystemContext);
        StartValueSimulation();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref _stopping, 1);
            Interlocked.Exchange(ref _simulationTimer, null)?.Dispose();
            _mutationGate.Wait();
            _simulationGate.Wait();
            try
            {
                CloseAllAssetFiles();
                base.Dispose(disposing);
                _assets.Clear();
                _filesByNodeId.Clear();
                _optionalMethodRemap.Clear();
            }
            finally
            {
                _simulationGate.Release();
                _mutationGate.Release();
            }
            return;
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DeleteAddressSpaceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Exchange(ref _stopping, 1);
        Interlocked.Exchange(ref _simulationTimer, null)?.Dispose();
        await _mutationGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await _simulationGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                CloseAllAssetFiles();
                await base.DeleteAddressSpaceAsync(CancellationToken.None).ConfigureAwait(false);
                _assets.Clear();
                _filesByNodeId.Clear();
                _optionalMethodRemap.Clear();
            }
            finally
            {
                _simulationGate.Release();
            }
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private void CloseAllAssetFiles()
    {
        foreach (WotAsset asset in _assets.Values)
        {
            lock (asset.FileLock)
            {
                asset.IsDeleted = true;
                foreach (MemoryStream stream in asset.FileBuffers.Values)
                {
                    stream.Dispose();
                }
                asset.FileBuffers.Clear();
                var file = FindPredefinedNode<FileState>(asset.FileNodeId);
                if (file?.OpenCount is not null)
                {
                    file.OpenCount.Value = 0;
                }
            }
        }
    }

    /// <summary>
    /// Diagnostic override: logs every incoming Call request and remaps type→instance MethodId
    /// as a workaround for clients that send the type-declaration MethodId on an instance object.
    /// </summary>
    public override ValueTask CallAsync(
        OperationContext context,
        ArrayOf<CallMethodRequest> methodsToCall,
        IList<CallMethodResult> results,
        IList<ServiceResult> errors,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Volatile.Read(ref _stopping) != 0)
        {
            throw new ServiceResultException(StatusCodes.BadServerHalted);
        }
        RemapMethods(methodsToCall);
        return base.CallAsync(context, methodsToCall, results, errors, cancellationToken);
    }

    private void RemapMethods(ArrayOf<CallMethodRequest> methodsToCall)
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
                else if (req.ObjectId == mgmtObjectId
                    && _optionalMethodRemap.TryGetValue(req.MethodId, out var optionalInstMethod))
                {
                    // Optional management members materialized on i=31: clients calling
                    // the type-side method (i=41 / i=49 / i=75) get remapped onto the
                    // runtime-allocated instance method that carries the stub handler.
                    _logger?.LogInformation("[WotCon] Remapping optional MethodId {From} -> instance {To}",
                        req.MethodId, optionalInstMethod);
                    req.MethodId = optionalInstMethod;
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
            createAssetMethod.OnCallMethod2Async = OnCreateAssetAsync;

            // Workaround for 1.5.378: also try to register on the type declaration (i=26)
            // This allows both type-based and instance-based Call dispatches to work
            var createAssetTypeId = new NodeId(CreateAssetMethodTypeId, wotConNamespaceIndex);
            var createAssetTypeMethod = FindPredefinedNode<MethodState>(createAssetTypeId);

            if (createAssetTypeMethod != null)
            {
                createAssetTypeMethod.OnCallMethod2Async = OnCreateAssetAsync;
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
                deleteAssetMethod.OnCallMethod2Async = OnDeleteAssetAsync;

                var deleteAssetTypeId = new NodeId(DeleteAssetMethodTypeId, wotConNamespaceIndex);
                var deleteAssetTypeMethod = FindPredefinedNode<MethodState>(deleteAssetTypeId);
                if (deleteAssetTypeMethod != null)
                {
                    deleteAssetTypeMethod.OnCallMethod2Async = OnDeleteAssetAsync;
                }

                _logger?.LogInformation("[WotCon] Registered OnDeleteAsset handler on instance (i={InstanceMethodId})", DeleteAssetMethodInstanceId);
            }
            else
            {
                _logger?.LogWarning("[WotCon] DeleteAsset method instance (ns={NamespaceIndex};i={MethodId}) not found", wotConNamespaceIndex, DeleteAssetMethodInstanceId);
            }

            // OPC 10100-1 §6.3.1 / §6.3.4 / §6.3.5 / §6.3.6 / §6.3.7 — materialize the
            // optional members of WoTAssetConnectionManagementType on i=31. See
            // WotConNodeManager.OptionalMembers.cs.
            SetupOptionalManagementMembers(context, wotConNamespaceIndex, managementObject);

            _logger?.LogInformation("[WotCon] WoT-Con method handlers registered successfully (ns={NamespaceIndex})", wotConNamespaceIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to setup method handlers");
        }
    }

    /// <summary>
    /// Reads the typed argument array stored by the NodeSet importer.
    /// </summary>
    private ArrayOf<Argument> ExtractArguments(Variant value)
    {
        return value.GetStructureArray<Argument>(context: Server.MessageContext);
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
            input ? "Input" : "Output", propertyId, prop?.Value.Count ?? 0, method.NodeId);
    }

    /// <summary>
    /// Adapts an arbitrary <see cref="BaseVariableState"/> instance into the strongly-typed
    /// <see cref="PropertyState{T}"/> the MethodState API requires. If it's already the right
    /// type, return as-is; otherwise build a new property carrying the same Value/NodeId.
    /// </summary>
    private PropertyState<ArrayOf<Argument>> ToArgumentProperty(BaseVariableState v)
    {
        if (v is PropertyState<ArrayOf<Argument>> p)
        {
            return p;
        }

        var prop = PropertyState<ArrayOf<Argument>>.With<StructureBuilder<Argument>>(v.Parent);
        prop.NodeId = v.NodeId;
        prop.BrowseName = v.BrowseName;
        prop.DisplayName = v.DisplayName;
        prop.TypeDefinitionId = VariableTypeIds.PropertyType;
        prop.DataType = DataTypeIds.Argument;
        prop.ValueRank = ValueRanks.OneDimension;
        prop.Value = ExtractArguments(v.Value);
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
    private async ValueTask<ServiceResult> OnCreateAssetAsync(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputArguments.Count < 1)
        {
            _logger?.LogWarning("[WotCon] CreateAsset called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        string assetName = inputArguments[0].GetString();
        if (string.IsNullOrWhiteSpace(assetName))
        {
            // §6.3.2 specifies Bad_BrowseNameInvalid for an invalid AssetName.
            return ServiceResult.Create(StatusCodes.BadBrowseNameInvalid, "{0}", "AssetName cannot be empty");
        }

        var (result, assetId) = await CreateAssetInternalAsync(context, assetName, endpoint: null, cancellationToken)
            .ConfigureAwait(false);
        if (ServiceResult.IsBad(result))
        {
            return result;
        }

        outputArguments[0] = assetId;
        return ServiceResult.Good;
    }

    /// <summary>
    /// Shared create-asset path used by both <see cref="OnCreateAssetAsync"/> (\u00a76.3.2) and
    /// <c>OnCreateAssetForEndpoint</c> (\u00a76.3.5). Enforces the \u00a76.3.2 duplicate-name rule
    /// (<c>Bad_BrowseNameDuplicated</c>), creates the asset object + per-asset
    /// <c>WoTAssetFileType</c> instance, and \u2014 when <paramref name="endpoint"/> is
    /// non-empty \u2014 stamps it onto <see cref="WotAsset.AssetEndpoint"/> and materializes
    /// the \u00a76.3.8 <c>AssetEndpoint</c> Property the same way a TD upload with a
    /// top-level <c>base</c> would.
    /// </summary>
    private async ValueTask<(ServiceResult Result, NodeId AssetId)> CreateAssetInternalAsync(
        ISystemContext context,
        string assetName,
        string endpoint,
        CancellationToken cancellationToken)
    {
        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _stopping) != 0)
            {
                return (new ServiceResult(StatusCodes.BadServerHalted), NodeId.Null);
            }
            cancellationToken = CancellationToken.None;
            if (_assets.ContainsKey(assetName))
            {
                _logger?.LogInformation("[WotCon] CreateAsset rejected: AssetName '{AssetName}' already exists", assetName);
                return (ServiceResult.Create(StatusCodes.BadBrowseNameDuplicated,
                    "An asset named '{0}' already exists", assetName), NodeId.Null);
            }

            var placeholder = new ThingDescriptionInfo { Name = assetName };
            var asset = await CreateAssetNodeAsync(context, placeholder, cancellationToken).ConfigureAwait(false);
            if (asset == null)
            {
                return (ServiceResult.Create(StatusCodes.BadInternalError, "{0}", "Failed to create asset node"), NodeId.Null);
            }

            // Atomic insert closes the check-then-act race against a concurrent
            // CreateAsset for the same name. If we lose, roll back the address-space
            // nodes we just created so an orphan tree can't leak. DeleteNode is
            // recursive (see OnDeleteAsset), so dropping the asset root also drops
            // the per-asset WoTFile and its FileType children.
            if (!_assets.TryAdd(assetName, asset))
            {
                await DeleteNodeAsync(SystemContext, asset.AssetId, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation("[WotCon] CreateAsset rejected: AssetName '{AssetName}' created concurrently by another caller", assetName);
                return (ServiceResult.Create(StatusCodes.BadBrowseNameDuplicated,
                    "An asset named '{0}' already exists", assetName), NodeId.Null);
            }

            if (!asset.FileNodeId.IsNull)
            {
                _filesByNodeId[asset.FileNodeId] = asset;
            }

            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                asset.AssetEndpoint = endpoint;
                await MaterializeAssetEndpointAsync(context, asset, cancellationToken).ConfigureAwait(false);
            }

            _logger?.LogInformation(
                "[WotCon] Created WoT asset '{AssetName}' with AssetId {AssetId} and WoTFile {FileId}{EndpointSuffix}",
                assetName, asset.AssetId, asset.FileNodeId,
                string.IsNullOrWhiteSpace(endpoint) ? string.Empty : $" (endpoint={endpoint})");
            ReportAssetModelChange(context, asset.AssetId, ModelChangeStructureVerbMask.NodeAdded);
            return (ServiceResult.Good, asset.AssetId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "[WotCon] Exception in CreateAssetInternal for '{AssetName}'", assetName);
            return (ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message), NodeId.Null);
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <summary>
    /// Handles DeleteAsset method calls per OPC 10100-1 §6.3.3. Removes the asset object,
    /// its per-asset WoTAssetFileType instance, materialized properties, and the
    /// <c>Organizes</c> reference from WoTAssetConnectionManagement. Closes any open file
    /// handles for the asset.
    /// </summary>
    private async ValueTask<ServiceResult> OnDeleteAssetAsync(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputArguments.Count < 1)
        {
            _logger?.LogWarning("[WotCon] DeleteAsset called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _stopping) != 0)
            {
                return StatusCodes.BadServerHalted;
            }
            cancellationToken = CancellationToken.None;
            NodeId assetId = inputArguments[0].GetNodeId();
            if (assetId.IsNull)
            {
                return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "AssetId cannot be null");
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

            // LifecycleGate serializes against an in-flight CloseAndUpdate materialization.
            // Setting IsDeleted under the gate means a CloseAndUpdate that wins the gate
            // after we release will short-circuit instead of writing into the address-space
            // subtree we're about to remove.
            bool deleted;
            await asset.LifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Close any open file handles for this asset so MemoryStreams don't leak.
                lock (asset.FileLock)
                {
                    asset.IsDeleted = true;
                    foreach (var stream in asset.FileBuffers.Values)
                    {
                        stream.Dispose();
                    }

                    asset.FileBuffers.Clear();
                    var file = FindPredefinedNode<FileState>(asset.FileNodeId);
                    if (file?.OpenCount is not null)
                    {
                        file.OpenCount.Value = 0;
                    }
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
                deleted = await DeleteNodeAsync(SystemContext, assetId, cancellationToken).ConfigureAwait(false);

                _assets.TryRemove(assetName, out _);
                if (!asset.FileNodeId.IsNull)
                {
                    _filesByNodeId.TryRemove(asset.FileNodeId, out _);
                }

                if (!deleted)
                {
                    _logger?.LogWarning("[WotCon] DeleteAsset: DeleteNode returned false for {AssetId}", assetId);
                }
            }
            finally
            {
                asset.LifecycleGate.Release();
            }

            _logger?.LogInformation("[WotCon] Deleted WoT asset '{AssetName}' AssetId={AssetId}", assetName, assetId);
            if (deleted)
            {
                ReportAssetModelChange(context, assetId, ModelChangeStructureVerbMask.NodeDeleted);
            }

            return ServiceResult.Good;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "[WotCon] Exception in OnDeleteAsset");
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private void ReportAssetModelChange(
        ISystemContext context,
        NodeId assetId,
        ModelChangeStructureVerbMask verb)
    {
        var modelChangeEvent = new GeneralModelChangeEventState(null);
        modelChangeEvent.Initialize(
            context,
            source: null,
            EventSeverity.Low,
            new Opc.Ua.LocalizedText("WoT asset address space changed."));
        modelChangeEvent.SetChildValue(context, BrowseNames.SourceNode, ObjectIds.Server, copy: false);
        modelChangeEvent.SetChildValue(context, BrowseNames.SourceName, "Server", copy: false);
        modelChangeEvent.SetChildValue(
            context,
            BrowseNames.Changes,
            Variant.FromStructure(new ModelChangeStructureDataType[]
            {
                new()
                {
                    Affected = assetId,
                    AffectedType = ObjectTypeIds.BaseObjectType,
                    Verb = (byte)verb,
                },
            }.ToArrayOf()),
            copy: false);

        Server.ReportEvent(modelChangeEvent);
    }

    /// <summary>
    /// Creates an OPC UA asset node with properties from the Thing Description, plus a
    /// per-asset WoTAssetFileType instance for TD upload. Returns a populated
    /// <see cref="WotAsset"/> with AssetId, FileNodeId and the type-method to instance-method
    /// remap table the <see cref="CallAsync"/> override needs.
    /// </summary>
    private async ValueTask<WotAsset> CreateAssetNodeAsync(
        ISystemContext context, ThingDescriptionInfo assetInfo, CancellationToken cancellationToken)
    {
        try
        {
            // Create asset root object (BaseObjectState)
            var assetNodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
            var assetNode = new BaseObjectState(null)
            {
                NodeId = assetNodeId,
                BrowseName = new QualifiedName(assetInfo.Name, NamespaceIndex),
                DisplayName = new LocalizedText(assetInfo.Name),
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

            // Add the asset to the server's address space. TD-driven Variables are materialized
            // later when the client uploads the Thing Description via CloseAndUpdate — see
            // MaterializeAssetProperties + OnPerAssetFileCloseAndUpdate.
            await AddPredefinedNodeAsync(context, assetNode, cancellationToken).ConfigureAwait(false);

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
    /// Creates a per-asset WoTAssetFileType instance (ns=WotCon;i=110) as a HasComponent
    /// child of <paramref name="assetNode"/>. Uses the SDK FileState layout and copies
    /// CloseAndUpdate from the imported WoT-Con model.
    /// Populates <see cref="WotAsset.FileNodeId"/> and <see cref="WotAsset.FileMethodMap"/>
    /// so the Call override can rewrite incoming type-method IDs onto this instance.
    /// </summary>
    private void CreateAssetFileNode(ISystemContext context, BaseObjectState assetNode, WotAsset asset)
    {
        var fileNodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        var fileNode = new FileState(assetNode)
        {
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
        };
        fileNode.Create(
            context,
            fileNodeId,
            new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.WoTFile, NamespaceIndex),
            new Opc.Ua.LocalizedText(RuntimeModelIds.WotCon.BrowseNames.WoTFile),
            assignNodeIds: true);

        fileNode.TypeDefinitionId = new NodeId(RuntimeModelIds.WotCon.ObjectTypes.WoTAssetFileType, NamespaceIndex);
        var closeAndUpdate = new MethodState(fileNode);
        closeAndUpdate.Create(context,
            FindPredefinedNode<MethodState>(new NodeId(FileCloseAndUpdateTypeMethodId, NamespaceIndex)));
        closeAndUpdate.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        closeAndUpdate.InputArguments = PropertyState<ArrayOf<Argument>>.With<StructureBuilder<Argument>>(
            closeAndUpdate);
        closeAndUpdate.InputArguments.BrowseName = new QualifiedName(Opc.Ua.BrowseNames.InputArguments);
        closeAndUpdate.InputArguments.DisplayName = new LocalizedText(Opc.Ua.BrowseNames.InputArguments);
        closeAndUpdate.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        closeAndUpdate.InputArguments.DataType = DataTypeIds.Argument;
        closeAndUpdate.InputArguments.ValueRank = ValueRanks.OneDimension;
        closeAndUpdate.InputArguments.Value = ExtractArguments(FindPredefinedNode<BaseVariableState>(new NodeId(
            RuntimeModelIds.WotCon.Variables.WoTAssetFileType_CloseAndUpdate_InputArguments, NamespaceIndex)).Value);
        fileNode.AddChild(closeAndUpdate);

        fileNode.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        fileNode.BrowseName = new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.WoTFile, NamespaceIndex);

        fileNode.CreateOrReplaceMimeType(context, null, assignInstanceNodeIds: true);
        fileNode.CreateOrReplaceMaxByteStringLength(context, null, assignInstanceNodeIds: true);
        fileNode.CreateOrReplaceLastModifiedTime(context, null, assignInstanceNodeIds: true);
        fileNode.MimeType.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        fileNode.MaxByteStringLength.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        fileNode.LastModifiedTime.ReferenceTypeId = ReferenceTypeIds.HasProperty;

        // Mandatory FileType properties.
        fileNode.Size.Value = 0UL;
        fileNode.Writable.Value = true;
        fileNode.UserWritable.Value = true;
        fileNode.OpenCount.Value = 0;

        // Optional FileType properties created explicitly above.
        if (fileNode.MimeType != null)
        {
            fileNode.MimeType.Value = "application/td+json";
        }

        if (fileNode.MaxByteStringLength != null)
        {
            // 64 KiB is comfortably above any realistic Thing Description JSON-LD payload
            // (typical TDs are a few KiB) and well below the OPC UA transport's default
            // MaxMessageSize, so the limit is actually enforceable end-to-end.
            fileNode.MaxByteStringLength.Value = 64U * 1024U;
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

        closeAndUpdate.OnCallMethod2Async = (context, method, objectId, inputs, outputs, token) =>
            OnPerAssetFileCloseAndUpdateAsync(asset, fileNode, inputs, token);

        // Reassign per-instance NodeIds to every child after they have all been wired up.
        // Passing assignNodeIds:true to Create() is too early — FileState's standard children
        // are not populated yet, so AssignNodeIds finds nothing to rewrite and the children
        // (Open, Close, Read, Write, Size, MaxByteStringLength, ...) keep the type-definition
        // NodeIds (NS=0). That makes them shared across all assets and causes reads on
        // optional Properties to hit the standard type-definition node (value 0) instead of
        // the per-asset value we set. Reassigning here gives every child a fresh GUID in our
        // namespace, isolating the per-asset state. We enumerate the FileState typed children
        // explicitly because the SDK's GetChildren on the auto-generated FileState does not
        // expose them, so AssignNodeIds(ctx, mappingTable) alone does nothing for them.
        ReassignChildNodeId(fileNode.Size);
        ReassignChildNodeId(fileNode.Writable);
        ReassignChildNodeId(fileNode.UserWritable);
        ReassignChildNodeId(fileNode.OpenCount);
        ReassignChildNodeId(fileNode.MimeType);
        ReassignChildNodeId(fileNode.MaxByteStringLength);
        ReassignChildNodeId(fileNode.LastModifiedTime);
        ReassignMethodNodeIds(fileNode.Open);
        ReassignMethodNodeIds(fileNode.Close);
        ReassignMethodNodeIds(fileNode.Read);
        ReassignMethodNodeIds(fileNode.Write);
        ReassignMethodNodeIds(fileNode.GetPosition);
        ReassignMethodNodeIds(fileNode.SetPosition);
        ReassignMethodNodeIds(closeAndUpdate);

        // Defensive: remap NS=0 FileType type-method IDs onto this instance's method NodeIds
        // for clients that call the type-method instead of browsing for the instance method.
        // Must be populated AFTER AssignNodeIds so the values reflect the freshly assigned
        // per-instance NodeIds.
        asset.FileMethodMap[new NodeId(Methods.FileType_Open, 0)] = fileNode.Open.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Close, 0)] = fileNode.Close.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Read, 0)] = fileNode.Read.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_Write, 0)] = fileNode.Write.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_GetPosition, 0)] = fileNode.GetPosition.NodeId;
        asset.FileMethodMap[new NodeId(Methods.FileType_SetPosition, 0)] = fileNode.SetPosition.NodeId;
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

    private void ReassignChildNodeId(BaseInstanceState child)
    {
        if (child != null)
        {
            child.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        }
    }

    private void ReassignMethodNodeIds(MethodState method)
    {
        if (method == null)
        {
            return;
        }

        method.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        if (method.InputArguments != null)
        {
            method.InputArguments.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        }

        if (method.OutputArguments != null)
        {
            method.OutputArguments.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        }
    }

    private ServiceResult OnPerAssetFileOpen(WotAsset asset, FileState fileNode, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        try
        {
            uint handle;
            int openCount;
            lock (asset.FileLock)
            {
                if (asset.IsDeleted || Volatile.Read(ref _stopping) != 0)
                {
                    return StatusCodes.BadObjectDeleted;
                }
                handle = asset.NextFileHandle++;
                asset.FileBuffers[handle] = new MemoryStream();
                openCount = asset.FileBuffers.Count;
            }

            outputArguments[0] = handle;
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, openCount);
            }

            _logger?.LogInformation("[WotCon] {Asset}.Open -> handle {Handle}", asset.Name, handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileOpen failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileWrite(WotAsset asset, FileState fileNode, ArrayOf<Variant> inputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = (uint)inputArguments[0];
            ByteString data = inputArguments[1].GetByteString();

            // FileLock has to cover the stream write itself: MemoryStream is not
            // thread-safe and a concurrent Close/CloseAndUpdate could dispose the
            // stream mid-write.
            long totalLength;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "Unknown file handle");
                }

                // Enforce the limit advertised on MaxByteStringLength so clients can trust
                // the property instead of being able to grow the in-memory buffer unbounded.
                uint maxBytes = fileNode.MaxByteStringLength?.Value ?? 0;
                if (maxBytes > 0 && stream.Length + data.Span.Length > maxBytes)
                {
                    return ServiceResult.Create(StatusCodes.BadRequestTooLarge, "{0}",
                        $"Write would exceed MaxByteStringLength ({maxBytes} bytes).");
                }

                stream.Write(data.Span);
                totalLength = stream.Length;
            }

            fileNode.Size.Value = (ulong)totalLength;
            _logger?.LogInformation("[WotCon] {Asset}.Write handle={Handle} wrote {Bytes} bytes (total {Total})",
                asset.Name, handle, data.Span.Length, totalLength);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileWrite failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileRead(WotAsset asset, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = (uint)inputArguments[0];
            int length = (int)inputArguments[1];

            // FileLock has to cover the stream read itself: MemoryStream is not
            // thread-safe and a concurrent Close/CloseAndUpdate could dispose the
            // stream mid-read.
            byte[] result;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "Unknown file handle");
                }

                var buffer = new byte[Math.Max(0, length)];
                int read = length > 0 ? stream.Read(buffer, 0, length) : 0;
                result = new byte[read];
                Array.Copy(buffer, result, read);
            }

            outputArguments[0] = (ByteString)result;
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileRead failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileClose(WotAsset asset, FileState fileNode, ArrayOf<Variant> inputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = (uint)inputArguments[0];
            int openCount = CloseAssetHandle(asset, handle);
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, openCount);
            }

            _logger?.LogInformation("[WotCon] {Asset}.Close handle={Handle}", asset.Name, handle);
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileClose failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileGetPosition(WotAsset asset, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = (uint)inputArguments[0];
            ulong position;
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "Unknown file handle");
                }

                position = (ulong)stream.Position;
            }

            outputArguments[0] = position;
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileGetPosition failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private ServiceResult OnPerAssetFileSetPosition(WotAsset asset, ArrayOf<Variant> inputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        try
        {
            uint handle = (uint)inputArguments[0];
            ulong position = (ulong)inputArguments[1];
            lock (asset.FileLock)
            {
                if (!asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "Unknown file handle");
                }

                stream.Position = (long)position;
            }

            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileSetPosition failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
    }

    private async ValueTask<ServiceResult> OnPerAssetFileCloseAndUpdateAsync(
        WotAsset asset, FileState fileNode, ArrayOf<Variant> inputArguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputArguments.Count < 1)
        {
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _stopping) != 0)
            {
                return StatusCodes.BadServerHalted;
            }
            cancellationToken = CancellationToken.None;
            uint handle = (uint)inputArguments[0];

            // Snapshot + dispose under one lock acquisition so a concurrent Read/Write
            // can't observe the buffer between ToArray and Dispose.
            byte[] payload = null;
            bool handleKnown = false;
            int openCount;
            lock (asset.FileLock)
            {
                if (asset.IsDeleted)
                {
                    return StatusCodes.BadObjectDeleted;
                }
                if (asset.FileBuffers.TryGetValue(handle, out var stream))
                {
                    handleKnown = true;
                    payload = stream.ToArray();
                    stream.Dispose();
                    asset.FileBuffers.Remove(handle);
                }

                openCount = asset.FileBuffers.Count;
            }

            // §6.3.10.2: an unknown / not-open-for-writing handle must surface as
            // Bad_InvalidState — bail before mutating any per-asset state.
            if (!handleKnown)
            {
                _logger?.LogWarning("[WotCon] {Asset}.CloseAndUpdate received unknown handle={Handle}", asset.Name, handle);
                return ServiceResult.Create(StatusCodes.BadInvalidState, "{0}", "FileHandle is not open for writing.");
            }

            asset.LastFinalizedPayload = payload;
            if (fileNode.OpenCount != null)
            {
                fileNode.OpenCount.Value = (ushort)Math.Min(ushort.MaxValue, openCount);
            }

            if (fileNode.LastModifiedTime != null)
            {
                fileNode.LastModifiedTime.Value = DateTime.UtcNow;
            }

            // Per OPC 10100-1 §6.3.2 + §6.3.8 the upload finalization is the trigger to
            // materialize the asset's information model from the Thing Description. For now
            // (plan item 1) we only decode + parse + persist; Variable/Method materialization
            // lands with plan items 2 and 3.
            //
            // TODO: validate against the WoT-Con TD JSON Schema (Annex A.2). The lightweight
            // "well-formed JSON + non-empty title" gate below is sufficient for mock-mode
            // round-trips today.
            if (payload == null || payload.Length == 0)
            {
                _logger?.LogWarning("[WotCon] {Asset}.CloseAndUpdate handle={Handle} produced an empty payload", asset.Name, handle);
                return ServiceResult.Create(StatusCodes.BadDecodingError, "{0}", "Thing Description payload is empty.");
            }

            string json;
            try
            {
                // Strict UTF-8: reject malformed byte sequences instead of silently substituting U+FFFD.
                json = StrictUtf8.GetString(payload);
            }
            catch (DecoderFallbackException ex)
            {
                _logger?.LogWarning(ex, "[WotCon] {Asset}.CloseAndUpdate handle={Handle} payload is not valid UTF-8", asset.Name, handle);
                return ServiceResult.Create(StatusCodes.BadDecodingError, "{0}", "Thing Description payload is not valid UTF-8.");
            }

            ThingDescriptionInfo parsed;
            try
            {
                parsed = ThingDescriptionParser.Parse(json, _logger);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "[WotCon] {Asset}.CloseAndUpdate handle={Handle} payload is malformed JSON", asset.Name, handle);
                return ServiceResult.Create(StatusCodes.BadDecodingError, "{0}", "Thing Description payload is not valid JSON.");
            }

            if (parsed == null)
            {
                // §6.3.10.2: a TD that omits a mandatory member fails to parse as a valid TD.
                return ServiceResult.Create(StatusCodes.BadDecodingError, "{0}", "Thing Description is missing a non-empty 'title'.");
            }

            // OPC 10100-1 §6.3.1: reject TDs that reference a WoT binding outside the
            // server's advertised SupportedWoTBindings catalog. Returns Bad_NotSupported
            // with a diagnostic message naming the offending binding URI.
            var bindingResult = ValidateThingDescriptionBindings(parsed);
            if (ServiceResult.IsBad(bindingResult))
            {
                return bindingResult;
            }

            // Persist both the raw JSON (for diagnostics / re-export) and the parsed form
            // (for later materialization). Re-uploads overwrite both.

            // Per OPC 10100-1 §6.3.2 + §6.3.8 + §6.3.9: a successful TD upload materializes
            // the asset's information model. Today: WoT Properties → OPC UA Variables and WoT
            // Actions → OPC UA Methods under the asset.
            //
            // LifecycleGate + IsDeleted closes the upload-vs-delete race: a concurrent
            // DeleteAsset that beat us to the gate has already torn the asset down, and a
            // second concurrent CloseAndUpdate is serialized so the last writer's
            // materialization fully replaces the prior generation instead of interleaving.
            await asset.LifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (asset.IsDeleted)
                {
                    _logger?.LogInformation("[WotCon] {Asset}.CloseAndUpdate aborted: asset was deleted concurrently", asset.Name);
                    return ServiceResult.Create(StatusCodes.BadObjectDeleted, "{0}", "Asset was deleted before the upload could be materialized.");
                }

                try
                {
                    asset.ThingDescription = json;
                    asset.ParsedThingDescription = parsed;
                    asset.AssetEndpoint = string.IsNullOrWhiteSpace(parsed.Base) ? null : parsed.Base;
                    await MaterializeAssetEndpointAsync(SystemContext, asset, cancellationToken).ConfigureAwait(false);
                    await MaterializeAssetPropertiesAsync(SystemContext, asset, parsed, cancellationToken)
                        .ConfigureAwait(false);
                    await MaterializeAssetActionsAsync(SystemContext, asset, parsed, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[WotCon] {Asset}.CloseAndUpdate materialization failed", asset.Name);
                    return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", "Failed to materialize Thing Description.");
                }
            }
            finally
            {
                asset.LifecycleGate.Release();
            }

            _logger?.LogInformation(
                "[WotCon] {Asset}.CloseAndUpdate handle={Handle} payload {Bytes} bytes; parsed TD title='{Title}' properties={PropertyCount} actions={ActionCount}",
                asset.Name,
                handle,
                payload.Length,
                parsed.Name,
                parsed.Properties.Count,
                parsed.Actions.Count);
            return ServiceResult.Good;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "[WotCon] {Asset}.OnPerAssetFileCloseAndUpdate failed", asset.Name);
            return ServiceResult.Create(StatusCodes.BadInternalError, "{0}", ex.Message);
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    // Disposes the buffer for <paramref name="handle"/> if present and returns the
    // post-close open-handle count so the caller can publish OpenCount without taking
    // FileLock a second time. Idempotent: an unknown handle is a no-op.
    private int CloseAssetHandle(WotAsset asset, uint handle)
    {
        lock (asset.FileLock)
        {
            if (asset.FileBuffers.TryGetValue(handle, out var stream))
            {
                stream.Dispose();
                asset.FileBuffers.Remove(handle);
            }

            return asset.FileBuffers.Count;
        }
    }

    /// <summary>
    /// Materializes the optional <c>AssetEndpoint</c> Property on the asset object per
    /// OPC 10100-1 §6.3.8 when the TD carries a top-level <c>base</c>. Shape mirrors
    /// <c>IWoTAssetType.AssetEndpoint</c> (i=122): String, scalar, HasProperty reference,
    /// PropertyType type definition. On re-upload, the previous generation is dropped
    /// first; when the new TD omits <c>base</c>, no Property is re-created so the asset
    /// simply does not contribute to <see cref="OnDiscoverAssets"/>.
    /// </summary>
    private async ValueTask MaterializeAssetEndpointAsync(
        ISystemContext context, WotAsset asset, CancellationToken cancellationToken)
    {
        var assetNode = FindPredefinedNode<BaseObjectState>(asset.AssetId);
        if (assetNode == null)
        {
            _logger?.LogWarning("[WotCon] {Asset}: asset object node {AssetId} not found; skipping AssetEndpoint materialization", asset.Name, asset.AssetId);
            return;
        }

        if (!asset.AssetEndpointNodeId.IsNull)
        {
            await DeleteNodeAsync(SystemContext, asset.AssetEndpointNodeId, cancellationToken).ConfigureAwait(false);
            asset.AssetEndpointNodeId = NodeId.Null;
        }

        if (string.IsNullOrEmpty(asset.AssetEndpoint))
        {
            return;
        }

        var endpointProp = PropertyState<string>.With<VariantBuilder>(assetNode);
        endpointProp.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        endpointProp.BrowseName = new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.AssetEndpoint, NamespaceIndex);
        endpointProp.DisplayName = new LocalizedText(RuntimeModelIds.WotCon.BrowseNames.AssetEndpoint);
        endpointProp.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        endpointProp.TypeDefinitionId = VariableTypeIds.PropertyType;
        endpointProp.DataType = DataTypeIds.String;
        endpointProp.ValueRank = ValueRanks.Scalar;
        endpointProp.AccessLevel = AccessLevels.CurrentRead;
        endpointProp.UserAccessLevel = AccessLevels.CurrentRead;
        endpointProp.Value = asset.AssetEndpoint;
        endpointProp.StatusCode = StatusCodes.Good;
        endpointProp.Timestamp = DateTime.UtcNow;

        assetNode.AddChild(endpointProp);
        await AddPredefinedNodeAsync(context, endpointProp, cancellationToken).ConfigureAwait(false);
        asset.AssetEndpointNodeId = endpointProp.NodeId;
    }

    /// <summary>
    /// Materializes the WoT Properties from <paramref name="td"/> as OPC UA Variables under
    /// the asset. Implements OPC 10100-1 §6.3.8 Table 14 (primitives only — nested objects /
    /// structured types are deferred to a later plan item).
    /// <para>
    /// Re-upload semantics: any previously materialized Variable for this asset is removed
    /// from the address space before the new generation is created, so the visible properties
    /// always reflect the most recently uploaded TD.
    /// </para>
    /// </summary>
    private async ValueTask MaterializeAssetPropertiesAsync(
        ISystemContext context, WotAsset asset, ThingDescriptionInfo td, CancellationToken cancellationToken)
    {
        var assetNode = FindPredefinedNode<BaseObjectState>(asset.AssetId);
        if (assetNode == null)
        {
            _logger?.LogWarning("[WotCon] {Asset}: asset object node {AssetId} not found; skipping materialization", asset.Name, asset.AssetId);
            return;
        }

        // Drop the previous generation of materialized properties. DeleteNode removes the
        // node from PredefinedNodes and tears down the HasComponent references on both ends.
        foreach (var staleId in asset.MaterializedPropertyNodeIds.Values)
        {
            await DeleteNodeAsync(SystemContext, staleId, cancellationToken).ConfigureAwait(false);
        }

        asset.MaterializedPropertyNodeIds.Clear();

        foreach (var property in td.Properties.Values)
        {
            var (dataType, valueRank) = ThingDescriptionParser.GetUaType(property);

            // TD readOnly / writeOnly → AccessLevel. When neither is set the property is R/W.
            byte accessLevel = property.WriteOnly
                ? AccessLevels.CurrentWrite
                : property.ReadOnly
                    ? AccessLevels.CurrentRead
                    : AccessLevels.CurrentReadOrWrite;

            var propertyNode = new BaseDataVariableState(assetNode)
            {
                NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
                BrowseName = new QualifiedName(property.Name, NamespaceIndex),
                DisplayName = new LocalizedText(property.Name),
                Description = new LocalizedText(property.Description ?? string.Empty),
                ReferenceTypeId = new NodeId(HasWoTComponentReferenceTypeId, NamespaceIndex),
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = accessLevel,
                UserAccessLevel = accessLevel,
                // observable=true (TD default) lets the SDK accept MonitoredItems on the variable.
                // observable=false leaves MinimumSamplingInterval at -1 (Indeterminate) so clients
                // requesting a subscription get a clear sampling-not-supported signal.
                MinimumSamplingInterval = property.Observable ? 1000.0 : -1.0,
                Value = WotMockValueGenerator.Generate(dataType, valueRank),
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow,
            };

            // TD `unit` → standard EngineeringUnits property on the variable.
            if (!string.IsNullOrEmpty(property.Unit))
            {
                propertyNode.AddChild(BuildEngineeringUnitsProperty(propertyNode, property.Unit));
            }

            assetNode.AddChild(propertyNode);
            await AddPredefinedNodeAsync(context, propertyNode, cancellationToken).ConfigureAwait(false);
            asset.MaterializedPropertyNodeIds[property.Name] = propertyNode.NodeId;
        }

        _logger?.LogInformation(
            "[WotCon] {Asset}: materialized {Count} TD properties",
            asset.Name, td.Properties.Count);
    }

    /// <summary>
    /// Builds the standard OPC UA <c>EngineeringUnits</c> PropertyState carrying the TD unit
    /// string. NamespaceUri / UnitId are intentionally left empty — the WoT TD only carries a
    /// free-form unit label, and the UNECE Common Code lookup is out of scope for mock-mode.
    /// </summary>
    private PropertyState<EUInformation> BuildEngineeringUnitsProperty(BaseDataVariableState parent, string unit)
    {
        var property = PropertyState<EUInformation>.With<StructureBuilder<EUInformation>>(parent);
        property.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        property.BrowseName = new QualifiedName(BrowseNames.EngineeringUnits, 0);
        property.DisplayName = new LocalizedText(BrowseNames.EngineeringUnits);
        property.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        property.TypeDefinitionId = VariableTypeIds.PropertyType;
        property.DataType = DataTypeIds.EUInformation;
        property.ValueRank = ValueRanks.Scalar;
        property.Value = new EUInformation
        {
            DisplayName = new LocalizedText(unit),
            NamespaceUri = string.Empty,
            UnitId = 0,
        };
        return property;
    }

    /// <summary>
    /// Materializes the WoT Actions from <paramref name="td"/> as OPC UA Methods under the
    /// asset. Implements OPC 10100-1 §6.3.9: each TD <c>actions[*]</c> entry becomes a
    /// <see cref="MethodState"/> child of the asset with <c>InputArguments</c> /
    /// <c>OutputArguments</c> synthesised from the action's <c>input</c> / <c>output</c> JSON
    /// Schemas (one <see cref="Argument"/> per property; top-level primitive schemas surface
    /// as a single <c>value</c> argument). The handler is mock-only — it logs the call and
    /// returns zero / empty values shaped from the output schema.
    /// <para>
    /// Re-upload semantics: any previously materialized Method for this asset is removed
    /// before the new generation is created, so the visible actions always reflect the
    /// most recently uploaded TD.
    /// </para>
    /// </summary>
    private async ValueTask MaterializeAssetActionsAsync(
        ISystemContext context, WotAsset asset, ThingDescriptionInfo td, CancellationToken cancellationToken)
    {
        var assetNode = FindPredefinedNode<BaseObjectState>(asset.AssetId);
        if (assetNode == null)
        {
            _logger?.LogWarning("[WotCon] {Asset}: asset object node {AssetId} not found; skipping action materialization", asset.Name, asset.AssetId);
            return;
        }

        foreach (var staleId in asset.MaterializedActionNodeIds.Values)
        {
            await DeleteNodeAsync(SystemContext, staleId, cancellationToken).ConfigureAwait(false);
        }

        asset.MaterializedActionNodeIds.Clear();

        foreach (var action in td.Actions.Values)
        {
            var inputArgs = BuildArgumentArray(action.Input);
            var outputArgs = BuildArgumentArray(action.Output);

            var methodNode = new MethodState(assetNode)
            {
                NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
                BrowseName = new QualifiedName(action.Name, NamespaceIndex),
                DisplayName = new LocalizedText(action.Name),
                SymbolicName = action.Name,
                Description = new LocalizedText(action.Description ?? string.Empty),
                ReferenceTypeId = new NodeId(HasWoTComponentReferenceTypeId, NamespaceIndex),
                Executable = true,
                UserExecutable = true,
            };

            // Capture by-value snapshot so each handler closure sees its own argument list.
            var capturedAction = action;
            methodNode.OnCallMethod = (c, m, i, o) => OnTdActionInvoked(asset, capturedAction, i, o);

            if (inputArgs != null)
            {
                methodNode.InputArguments = CreateArgumentProperty(methodNode, BrowseNames.InputArguments, inputArgs);
            }

            if (outputArgs != null)
            {
                methodNode.OutputArguments = CreateArgumentProperty(methodNode, BrowseNames.OutputArguments, outputArgs);
            }

            assetNode.AddChild(methodNode);
            await AddPredefinedNodeAsync(context, methodNode, cancellationToken).ConfigureAwait(false);
            asset.MaterializedActionNodeIds[action.Name] = methodNode.NodeId;
        }

        _logger?.LogInformation(
            "[WotCon] {Asset}: materialized {Count} TD actions",
            asset.Name, td.Actions.Count);
    }

    /// <summary>
    /// Converts a list of TD action arguments into the SDK's <see cref="Argument"/> array
    /// shape. Returns <c>null</c> for an empty list so the caller can leave the corresponding
    /// <c>InputArguments</c> / <c>OutputArguments</c> property absent — clients then know the
    /// action has no input or no output rather than an empty argument list.
    /// </summary>
    private static Argument[] BuildArgumentArray(List<ThingArgumentInfo> arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            return null;
        }

        var result = new Argument[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
        {
            var argInfo = arguments[i];
            var (dataType, valueRank) = ThingDescriptionParser.GetUaType(argInfo);
            result[i] = new Argument
            {
                Name = argInfo.Name,
                Description = new LocalizedText(argInfo.Description ?? string.Empty),
                DataType = dataType,
                ValueRank = valueRank,
            };
        }

        return result;
    }

    /// <summary>
    /// Default handler for a materialized TD action. Logs the invocation and populates each
    /// output argument with a mock value shaped from the action's output schema.
    /// </summary>
    private ServiceResult OnTdActionInvoked(
        WotAsset asset,
        ThingActionInfo action,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments)
    {
        _logger?.LogInformation(
            "[WotCon] {Asset}.{Action}: invoked with {InputCount} input(s); returning {OutputCount} canned output(s)",
            asset.Name, action.Name, inputArguments.Count, outputArguments?.Count ?? 0);

        if (outputArguments == null || action.Output == null)
        {
            return ServiceResult.Good;
        }

        int count = Math.Min(outputArguments.Count, action.Output.Count);
        for (int i = 0; i < count; i++)
        {
            var (dataType, valueRank) = ThingDescriptionParser.GetUaType(action.Output[i]);
            outputArguments[i] = WotMockValueGenerator.Generate(dataType, valueRank);
        }

        return ServiceResult.Good;
    }

    private PropertyState<ArrayOf<Argument>> CreateArgumentProperty(
        MethodState method, string browseName, Argument[] arguments)
    {
        var property = PropertyState<ArrayOf<Argument>>.With<StructureBuilder<Argument>>(method);
        property.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        property.BrowseName = new QualifiedName(browseName);
        property.DisplayName = new LocalizedText(browseName);
        property.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        property.TypeDefinitionId = VariableTypeIds.PropertyType;
        property.DataType = DataTypeIds.Argument;
        property.ValueRank = ValueRanks.OneDimension;
        property.Value = arguments.ToArrayOf();
        return property;
    }
}
