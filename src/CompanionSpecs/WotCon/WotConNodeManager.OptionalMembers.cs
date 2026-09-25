// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Materializes the optional members of <c>WoTAssetConnectionManagementType</c> (i=1) on
/// the standard instance <c>WoTAssetConnectionManagement</c> (i=31) per OPC 10100-1
/// §6.3.1 / §6.3.4 / §6.3.5 / §6.3.6 / §6.3.7. The NodeSet importer drops them because they
/// carry modelling rule <c>Optional</c> (i=80), so clients calling them today hit
/// <c>Bad_NodeIdUnknown</c> instead of a meaningful reply.
/// <para>
/// <c>SupportedWoTBindings</c> advertises the bindings catalog (see
/// <see cref="WotConBindings"/>) and <see cref="ValidateThingDescriptionBindings"/> is
/// consulted at upload time to reject TDs that reference an unsupported binding.
/// <c>DiscoverAssets</c>, <c>CreateAssetForEndpoint</c>, and <c>ConnectionTest</c> are
/// implemented (mock); <c>Configuration/License</c> surfaces the simulator's SPDX
/// identifier.
/// </para>
/// </summary>
public partial class WotConNodeManager
{
    // Type-side NodeIds from the bundled NodeSet (Opc.Ua.WotCon.NodeSet2.xml),
    // bound to the model-compiler-generated constants in Opc.Ua.WotCon.{Methods,Objects,
    // ObjectTypes,Variables} so a future NodeSet regeneration that renames any of these
    // fails the compile rather than mismatching silently at runtime.
    private const uint SupportedWoTBindingsTypeVariableId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_SupportedWoTBindings;
    private const uint DiscoverAssetsTypeMethodId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagementType_DiscoverAssets;
    private const uint DiscoverAssetsOutputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_DiscoverAssets_OutputArguments;
    private const uint CreateAssetForEndpointTypeMethodId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagementType_CreateAssetForEndpoint;
    private const uint CreateAssetForEndpointInputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_CreateAssetForEndpoint_InputArguments;
    private const uint CreateAssetForEndpointOutputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_CreateAssetForEndpoint_OutputArguments;
    private const uint ConnectionTestTypeMethodId = RuntimeModelIds.WotCon.Methods.WoTAssetConnectionManagementType_ConnectionTest;
    private const uint ConnectionTestInputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_ConnectionTest_InputArguments;
    private const uint ConnectionTestOutputArgumentsId = RuntimeModelIds.WotCon.Variables.WoTAssetConnectionManagementType_ConnectionTest_OutputArguments;
    private const uint ConfigurationTypeObjectId = RuntimeModelIds.WotCon.Objects.WoTAssetConnectionManagementType_Configuration;
    private const uint WoTAssetConfigurationTypeId = RuntimeModelIds.WotCon.ObjectTypes.WoTAssetConfigurationType;

    // OPC UA Part 5 §12.20: UriString (subtype of String) — backing DataType for
    // WoTBindingType in the WoT-Con NodeSet (DataType="i=23751" on i=40). Lives in
    // the standard namespace, so use the SDK's own constant.
    private const uint UriStringDataTypeId = Opc.Ua.DataTypes.UriString;

    // SPDX short identifier for the OPC PLC simulator's license (LICENSE in repo root).
    // Surfaced verbatim via Configuration/License per OPC 10100-1 §6.3.7.
    internal const string ServerLicenseSpdx = "MIT";

    /// <summary>
    /// Maps the type-side method NodeIds of the optional management members (i=41 / i=49 /
    /// i=75) to the runtime-allocated instance method NodeIds we materialize on i=31. The
    /// <see cref="CallAsync"/> override consults this dict to remap incoming type-method calls
    /// onto the instance method, mirroring the workaround already used for CreateAsset /
    /// DeleteAsset.
    /// </summary>
    private readonly Dictionary<NodeId, NodeId> _optionalMethodRemap = new();

    /// <summary>
    /// Materializes the optional members of <c>WoTAssetConnectionManagementType</c> on the
    /// standard instance <c>WoTAssetConnectionManagement</c> (i=31): one Property
    /// (<c>SupportedWoTBindings</c>), one Object (<c>Configuration</c>), and three Methods
    /// (<c>DiscoverAssets</c>, <c>CreateAssetForEndpoint</c>, <c>ConnectionTest</c>). All
    /// three methods are wired with real handlers; the <c>Configuration</c> object exposes
    /// the simulator's SPDX <c>License</c>.
    /// </summary>
    private void SetupOptionalManagementMembers(ISystemContext context, ushort nsIdx, BaseObjectState managementObject)
    {
        try
        {
            MaterializeSupportedWoTBindings(context, nsIdx, managementObject);
            MaterializeConfigurationObject(context, nsIdx, managementObject);

            MaterializeOptionalMethod(
                context,
                nsIdx,
                managementObject,
                typeMethodId: DiscoverAssetsTypeMethodId,
                browseName: RuntimeModelIds.WotCon.BrowseNames.DiscoverAssets,
                inputArgs: null,
                outputArgs: new[] { MakeArgArray("AssetEndpoints", DataTypes.String) },
                handler: OnDiscoverAssets);

            MaterializeOptionalMethod(
                context,
                nsIdx,
                managementObject,
                typeMethodId: CreateAssetForEndpointTypeMethodId,
                browseName: RuntimeModelIds.WotCon.BrowseNames.CreateAssetForEndpoint,
                inputArgs: new[]
                {
                    MakeArg("AssetName", DataTypes.String),
                    MakeArg("AssetEndpoint", DataTypes.String),
                },
                outputArgs: new[] { MakeArg("AssetId", DataTypes.NodeId) },
                handler: null,
                asyncHandler: OnCreateAssetForEndpointAsync);

            MaterializeOptionalMethod(
                context,
                nsIdx,
                managementObject,
                typeMethodId: ConnectionTestTypeMethodId,
                browseName: RuntimeModelIds.WotCon.BrowseNames.ConnectionTest,
                inputArgs: new[] { MakeArg("AssetEndpoint", DataTypes.String) },
                outputArgs: new[]
                {
                    MakeArg("Success", DataTypes.Boolean),
                    MakeArg("Status", DataTypes.String),
                },
                handler: OnConnectionTest);

            _logger?.LogInformation("[WotCon] Materialized {Count} optional management members on i={Mgmt}",
                2 + _optionalMethodRemap.Count, WotAssetConnectionManagementObjectId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[WotCon] Failed to set up optional management members");
        }
    }

    private void MaterializeSupportedWoTBindings(ISystemContext context, ushort nsIdx, BaseObjectState managementObject)
    {
        var prop = PropertyState<ArrayOf<string>>.With<VariantBuilder>(managementObject);
        prop.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        prop.BrowseName = new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.SupportedWoTBindings, NamespaceIndex);
        prop.DisplayName = new LocalizedText(RuntimeModelIds.WotCon.BrowseNames.SupportedWoTBindings);
        prop.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        prop.TypeDefinitionId = VariableTypeIds.PropertyType;
        prop.DataType = new NodeId(UriStringDataTypeId, 0);
        prop.ValueRank = ValueRanks.OneDimension;
        prop.ArrayDimensions = [0];
        prop.AccessLevel = AccessLevels.CurrentRead;
        prop.UserAccessLevel = AccessLevels.CurrentRead;
        prop.Value = ((string[])WotConBindings.SupportedBindings.Clone()).ToArrayOf();
        prop.StatusCode = StatusCodes.Good;
        prop.Timestamp = DateTime.UtcNow;

        managementObject.AddChild(prop);
        AddPredefinedNodeSynchronously(prop);
        _logger?.LogDebug("[WotCon] Materialized SupportedWoTBindings property NodeId={NodeId} (type-side i={TypeId}) with {Count} binding(s)",
            prop.NodeId, SupportedWoTBindingsTypeVariableId, WotConBindings.SupportedBindings.Length);
    }

    private void MaterializeConfigurationObject(ISystemContext context, ushort nsIdx, BaseObjectState managementObject)
    {
        var configObject = new BaseObjectState(managementObject)
        {
            NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
            BrowseName = new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.Configuration, NamespaceIndex),
            DisplayName = new LocalizedText(RuntimeModelIds.WotCon.BrowseNames.Configuration),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = new NodeId(WoTAssetConfigurationTypeId, nsIdx),
        };

        // OPC 10100-1 §6.3.7 Table 16: WoTAssetConfigurationType exposes a License
        // Property (i=109, String, modelling rule Optional). The OPC PLC simulator ships
        // under the MIT license (see LICENSE in the repo root); surface that as an SPDX
        // identifier so clients can interpret it programmatically without parsing prose.
        // The <WoTConfigurationParameterName> placeholder (i=108, modelling rule
        // OptionalPlaceholder) is deliberately omitted — no configuration parameters are
        // defined yet.
        var license = PropertyState<string>.With<VariantBuilder>(configObject);
        license.NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex);
        license.BrowseName = new QualifiedName(RuntimeModelIds.WotCon.BrowseNames.License, NamespaceIndex);
        license.DisplayName = new LocalizedText(RuntimeModelIds.WotCon.BrowseNames.License);
        license.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        license.TypeDefinitionId = VariableTypeIds.PropertyType;
        license.DataType = DataTypeIds.String;
        license.ValueRank = ValueRanks.Scalar;
        license.AccessLevel = AccessLevels.CurrentRead;
        license.UserAccessLevel = AccessLevels.CurrentRead;
        license.Value = ServerLicenseSpdx;
        license.StatusCode = StatusCodes.Good;
        license.Timestamp = DateTime.UtcNow;

        configObject.AddChild(license);
        managementObject.AddChild(configObject);
        AddPredefinedNodeSynchronously(configObject);
    }

    /// <summary>
    /// Materializes an optional method on i=31 as a fresh <see cref="MethodState"/> with its
    /// own NodeId and rehydrated <c>InputArguments</c> / <c>OutputArguments</c> properties,
    /// then registers <paramref name="handler"/> on both the new instance and the type-side
    /// method (i=<paramref name="typeMethodId"/>) and records the type-to-instance remap so
    /// the <see cref="CallAsync"/> override can route either invocation form.
    /// </summary>
    private void MaterializeOptionalMethod(
        ISystemContext context,
        ushort nsIdx,
        BaseObjectState managementObject,
        uint typeMethodId,
        string browseName,
        Argument[] inputArgs,
        Argument[] outputArgs,
        GenericMethodCalledEventHandler handler,
        GenericMethodCalledEventHandler2Async asyncHandler = null)
    {
        var typeMethodNodeId = new NodeId(typeMethodId, nsIdx);
        var method = new MethodState(managementObject)
        {
            NodeId = new NodeId(Guid.NewGuid(), NamespaceIndex),
            BrowseName = new QualifiedName(browseName, NamespaceIndex),
            DisplayName = new LocalizedText(browseName),
            SymbolicName = browseName,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            MethodDeclarationId = typeMethodNodeId,
            Executable = true,
            UserExecutable = true,
            OnCallMethod = handler,
            OnCallMethod2Async = asyncHandler,
        };

        if (inputArgs != null)
        {
            method.InputArguments = CreateArgumentProperty(method, BrowseNames.InputArguments, inputArgs);
        }

        if (outputArgs != null)
        {
            method.OutputArguments = CreateArgumentProperty(method, BrowseNames.OutputArguments, outputArgs);
        }

        managementObject.AddChild(method);
        AddPredefinedNodeSynchronously(method);

        // Belt-and-braces: under normal operation the Call override remaps the type-method
        // NodeId to the instance method above before dispatch, so this second wire is
        // redundant. It's kept as a backstop for any path that resolves the type method
        // directly (e.g. a client that bypasses the remap, or a future SDK change in the
        // Call pipeline) so the same handler still fires.
        var typeMethodNode = FindPredefinedNode<MethodState>(typeMethodNodeId);
        if (typeMethodNode != null)
        {
            typeMethodNode.OnCallMethod = handler;
            typeMethodNode.OnCallMethod2Async = asyncHandler;
        }

        _optionalMethodRemap[typeMethodNodeId] = method.NodeId;
    }

    private Argument MakeArgArray(string name, uint dataTypeId) => new()
    {
        Name = name,
        DataType = new NodeId(dataTypeId, 0),
        ValueRank = ValueRanks.OneDimension,
        ArrayDimensions = new[] { 0u },
    };

    private ServiceResult OnDiscoverAssets(
        ISystemContext context,
        MethodState method,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments)
    {
        // OPC 10100-1 §6.3.4: return the list of asset endpoints currently known to the
        // server. We populate that from each managed asset's AssetEndpoint Property
        // (§6.3.8 / IWoTAssetType.AssetEndpoint), which in turn is derived from the TD's
        // top-level `base` URI on upload. Assets whose TD omits `base` simply don't
        // contribute. De-dup is case-sensitive Ordinal — the spec doesn't define
        // endpoint syntax (§6.3.8 calls it "vendor-specific"), and URIs are
        // case-sensitive in their path / query components. Output is sorted so two
        // calls against the same population return identical arrays regardless of
        // dictionary insertion order (ConcurrentDictionary doesn't preserve it).
        var endpoints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in _assets.Values)
        {
            if (!string.IsNullOrEmpty(asset.AssetEndpoint))
            {
                endpoints.Add(asset.AssetEndpoint);
            }
        }

        var result = endpoints.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        outputArguments[0] = Variant.From(result.ToArrayOf());

        _logger?.LogDebug("[WotCon] DiscoverAssets returning {Count} endpoint(s)", result.Length);
        return ServiceResult.Good;
    }

    /// <summary>
    /// OPC 10100-1 §6.3.5: create an asset and stamp the supplied <c>AssetEndpoint</c>
    /// onto it in a single call. Equivalent to <c>CreateAsset(AssetName)</c> followed by
    /// an upload of a TD whose top-level <c>base</c> equals <paramref name="AssetEndpoint"
    /// /> — but skips the FileType round-trip for the endpoint-first onboarding flow.
    /// The per-asset <c>WoTAssetFileType</c> instance is still wired, so a later TD
    /// upload can populate Properties / Actions; if that TD carries a different
    /// <c>base</c>, it wins (TD is the authoritative source of the model).
    /// </summary>
    private async ValueTask<ServiceResult> OnCreateAssetForEndpointAsync(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (inputArguments.Count < 2)
        {
            _logger?.LogWarning("[WotCon] CreateAssetForEndpoint called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        string assetName = inputArguments[0].GetString();
        string endpoint = inputArguments[1].GetString();
        if (string.IsNullOrWhiteSpace(assetName))
        {
            // Mirrors §6.3.2's Bad_BrowseNameInvalid for an invalid AssetName; the
            // AssetName flows into the asset's BrowseName the same way as in CreateAsset.
            return ServiceResult.Create(StatusCodes.BadBrowseNameInvalid, "{0}", "AssetName cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            // §6.3.5 doesn't define a status for this. Bad_InvalidArgument is the closest
            // match — the method's whole point is the endpoint, so an empty one is a
            // malformed request, not a missing optional argument.
            return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "AssetEndpoint cannot be empty");
        }

        var (result, assetId) = await CreateAssetInternalAsync(context, assetName, endpoint, cancellationToken)
            .ConfigureAwait(false);
        if (ServiceResult.IsBad(result))
        {
            return result;
        }

        outputArguments[0] = assetId;
        return ServiceResult.Good;
    }

    /// <summary>
    /// OPC 10100-1 §6.3.6: probe whether an <c>AssetEndpoint</c> is reachable. The
    /// simulator never opens a southbound connection, so reachability collapses to
    /// "is this endpoint registered with any asset?". Returns
    /// <c>(Success=true, Status="Simulated")</c> on a hit and
    /// <c>(Success=false, Status="UnknownEndpoint")</c> otherwise; the method itself
    /// returns <c>Good</c> because the outcome travels on the output arguments — the
    /// spec defines no failure StatusCodes for this method.
    /// </summary>
    private ServiceResult OnConnectionTest(
        ISystemContext context,
        MethodState method,
        ArrayOf<Variant> inputArguments,
        List<Variant> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            _logger?.LogWarning("[WotCon] ConnectionTest called with insufficient arguments");
            return new ServiceResult(StatusCodes.BadArgumentsMissing);
        }

        string endpoint = inputArguments[0].GetString();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return ServiceResult.Create(StatusCodes.BadInvalidArgument, "{0}", "AssetEndpoint cannot be empty");
        }

        var known = false;
        foreach (var asset in _assets.Values)
        {
            if (string.Equals(asset.AssetEndpoint, endpoint, StringComparison.Ordinal))
            {
                known = true;
                break;
            }
        }

        outputArguments[0] = known;
        outputArguments[1] = known ? "Simulated" : "UnknownEndpoint";
        _logger?.LogDebug("[WotCon] ConnectionTest endpoint='{Endpoint}' known={Known}", endpoint, known);
        return ServiceResult.Good;
    }

    /// <summary>
    /// Walks the TD's <c>@context</c> (OPC 10100-1 §6.3.1), treats any entry that
    /// exact-matches an URI in <see cref="WotConBindings.KnownBindings"/> as a binding
    /// declaration, and rejects those not in <see cref="WotConBindings.SupportedBindings"/>.
    /// URIs unrecognised as bindings (W3C TD base context, semantic vocabularies, vendor
    /// extensions we don't model) are ignored so unrelated TDs keep round-tripping.
    /// Returns <c>Bad_NotSupported</c> with a diagnostic message naming the offending URI.
    /// </summary>
    internal ServiceResult ValidateThingDescriptionBindings(ThingDescriptionInfo td)
    {
        if (td?.Contexts == null)
        {
            return ServiceResult.Good;
        }

        foreach (var ctx in td.Contexts)
        {
            if (string.IsNullOrEmpty(ctx))
            {
                continue;
            }

            if (!WotConBindings.KnownBindings.Contains(ctx))
            {
                continue;
            }

            if (Array.IndexOf(WotConBindings.SupportedBindings, ctx) < 0)
            {
                _logger?.LogWarning(
                    "[WotCon] TD references unsupported binding '{Binding}'. Supported: {Supported}",
                    ctx, string.Join(", ", WotConBindings.SupportedBindings));
                return ServiceResult.Create(
                    StatusCodes.BadNotSupported,
                    "{0}",
                    $"WoT binding '{ctx}' is not supported by this server. Supported bindings: {string.Join(", ", WotConBindings.SupportedBindings)}.");
            }
        }

        return ServiceResult.Good;
    }
}
