namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the optional members of <c>WoTAssetConnectionManagementType</c> materialized
/// on the standard instance <c>WoTAssetConnectionManagement</c> (i=31). Phase 1a:
/// <c>SupportedWoTBindings</c> reads as an empty array, <c>Configuration</c> is browseable
/// with an empty <c>License</c>, and the three optional methods return
/// <c>Bad_NotImplemented</c>. See OPC 10100-1 §6.3.1 / §6.3.4 / §6.3.5 / §6.3.6 / §6.3.7.
/// </summary>
public partial class WotConTests
{
    // Type-side NodeIds of the optional members (declared on i=1, materialized at runtime
    // onto i=31 with runtime-allocated instance NodeIds).
    private const uint SupportedWoTBindingsTypeVariableId = 40;
    private const uint DiscoverAssetsTypeMethodId = 41;
    private const uint CreateAssetForEndpointTypeMethodId = 49;
    private const uint ConnectionTestTypeMethodId = 75;
    private const uint WoTAssetConfigurationTypeId = 105;

    [Test]
    public async Task OptionalMethod_DiscoverAssets_ReturnsBadNotImplemented()
    {
        var (status, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DiscoverAssetsTypeMethodId),
            arguments: new VariantCollection()).ConfigureAwait(false);

        status.Code.Should().Be(StatusCodes.BadNotImplemented,
            "DiscoverAssets is a Phase-1a stub per OPC 10100-1 §6.3.4, got {0}", status);
    }

    [Test]
    public async Task OptionalMethod_CreateAssetForEndpoint_ReturnsBadNotImplemented()
    {
        var (status, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant("StubAsset"),
                new Variant("opc.tcp://example.invalid:4840"),
            }).ConfigureAwait(false);

        status.Code.Should().Be(StatusCodes.BadNotImplemented,
            "CreateAssetForEndpoint is a Phase-1a stub per OPC 10100-1 §6.3.5, got {0}", status);
    }

    [Test]
    public async Task OptionalMethod_ConnectionTest_ReturnsBadNotImplemented()
    {
        var (status, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(ConnectionTestTypeMethodId),
            arguments: new VariantCollection { new Variant("opc.tcp://example.invalid:4840") }).ConfigureAwait(false);

        status.Code.Should().Be(StatusCodes.BadNotImplemented,
            "ConnectionTest is a Phase-1a stub per OPC 10100-1 §6.3.6, got {0}", status);
    }

    [Test]
    public async Task SupportedWoTBindings_IsReadableAndEmpty()
    {
        // OPC 10100-1 §6.3.1: Property on WoTAssetConnectionManagementType. Materialized at
        // runtime under i=31 with a fresh NodeId; resolve by BrowseName under the management
        // object rather than by the type-side i=40.
        var fileId = await ResolveChildByBrowseNameAsync(
            parent: WotConNodeId(WotAssetConnectionManagementObjectId),
            childBrowseName: "SupportedWoTBindings",
            isProperty: true).ConfigureAwait(false);

        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId { NodeId = fileId, AttributeId = Attributes.Value },
        };
        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
        var result = resp.Results[0];
        StatusCode.IsGood(result.StatusCode).Should().BeTrue(
            "SupportedWoTBindings must be readable, got {0}", result.StatusCode);
        result.Value.Should().BeAssignableTo<string[]>(
            "SupportedWoTBindings is declared as WoTBindingType[] which the NodeSet backs with UriString[]");
        ((string[])result.Value).Should().BeEmpty(
            "Phase 1a returns an empty list; populated in Phase 1b from registered protocol bindings");
    }

    [Test]
    public async Task Configuration_ObjectIsBrowseable()
    {
        // OPC 10100-1 §6.3.7: Configuration : WoTAssetConfigurationType is an Optional child
        // of WoTAssetConnectionManagementType. Phase 1a exposes it with a single empty
        // License property; the <WoTConfigurationParameterName> placeholder is omitted.
        var configId = await ResolveChildByBrowseNameAsync(
            parent: WotConNodeId(WotAssetConnectionManagementObjectId),
            childBrowseName: "Configuration",
            isProperty: false).ConfigureAwait(false);

        // Browse forward HasTypeDefinition to confirm the type definition is WoTAssetConfigurationType.
        var typeDefBrowse = new BrowseDescription
        {
            NodeId = configId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition,
            IncludeSubtypes = false,
            NodeClassMask = (uint)NodeClass.ObjectType,
            ResultMask = (uint)BrowseResultMask.All,
        };
        var typeDefResp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { typeDefBrowse },
            CancellationToken.None).ConfigureAwait(false);
        typeDefResp.Results.Should().ContainSingle();
        var typeDefRefs = typeDefResp.Results[0].References;
        typeDefRefs.Should().ContainSingle("Configuration must have exactly one HasTypeDefinition");
        var typeDefNodeId = ExpandedNodeId.ToNodeId(typeDefRefs[0].NodeId, Session.NamespaceUris);
        typeDefNodeId.Should().Be(WotConNodeId(WoTAssetConfigurationTypeId),
            "Configuration must be typed as WoTAssetConfigurationType per §6.3.7");

        // Resolve the License property and verify it reads as an empty string.
        var licenseId = await ResolveChildByBrowseNameAsync(
            parent: configId,
            childBrowseName: "License",
            isProperty: true).ConfigureAwait(false);

        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId { NodeId = licenseId, AttributeId = Attributes.Value },
        };
        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
        var result = resp.Results[0];
        StatusCode.IsGood(result.StatusCode).Should().BeTrue(
            "Configuration/License must be readable, got {0}", result.StatusCode);
        result.Value.Should().Be(string.Empty,
            "Phase 1a surfaces License as an empty string; populated from build metadata in Phase 1b");
    }

    /// <summary>
    /// Browses forward <c>HasProperty</c> or <c>HasComponent</c> children of
    /// <paramref name="parent"/> and returns the NodeId of the single child whose
    /// BrowseName name part matches <paramref name="childBrowseName"/>. Fails the test if
    /// zero or more than one match.
    /// </summary>
    private async Task<NodeId> ResolveChildByBrowseNameAsync(NodeId parent, string childBrowseName, bool isProperty)
    {
        var bd = new BrowseDescription
        {
            NodeId = parent,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = isProperty ? ReferenceTypeIds.HasProperty : ReferenceTypeIds.HasComponent,
            IncludeSubtypes = true,
            NodeClassMask = isProperty ? (uint)NodeClass.Variable : (uint)NodeClass.Object,
            ResultMask = (uint)BrowseResultMask.All,
        };
        var resp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        var matches = resp.Results[0].References
            .Where(r => r.BrowseName.Name == childBrowseName)
            .ToList();
        matches.Should().ContainSingle(
            "expected exactly one '{0}' child of {1}", childBrowseName, parent);
        var nodeId = ExpandedNodeId.ToNodeId(matches[0].NodeId, Session.NamespaceUris);
        NodeId.IsNull(nodeId).Should().BeFalse();
        return nodeId;
    }
}
