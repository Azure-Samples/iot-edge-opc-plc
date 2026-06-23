namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the optional members of <c>WoTAssetConnectionManagementType</c> materialized
/// on the standard instance <c>WoTAssetConnectionManagement</c> (i=31):
/// <c>SupportedWoTBindings</c> advertises the bindings catalog, <c>Configuration</c> is
/// browseable with an empty <c>License</c>, and the three optional methods return
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
    public async Task CreateAssetForEndpoint_RegistersAssetAndMaterializesEndpoint()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var assetName = "CreateAFE_" + suffix;
        var endpoint = $"opc.tcp://endpoint-first-{suffix}.invalid:4840";

        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant(assetName),
                new Variant(endpoint),
            }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue(
            "CreateAssetForEndpoint must succeed for fresh inputs (§6.3.5), got {0}", status);
        outputs.Should().ContainSingle("§6.3.5 declares a single AssetId output argument");
        var assetId = outputs[0].Value as NodeId;
        NodeId.IsNull(assetId).Should().BeFalse("AssetId output must be a non-null NodeId");

        // The supplied endpoint must surface via DiscoverAssets immediately — no TD upload
        // round-trip required for the endpoint-first onboarding flow.
        var endpoints = await CallDiscoverAssetsAsync().ConfigureAwait(false);
        endpoints.Should().Contain(endpoint,
            "CreateAssetForEndpoint must make the endpoint visible to DiscoverAssets without a TD upload");

        // §6.3.8 AssetEndpoint Property must be materialized under the new asset.
        var endpointPropId = await ResolveChildByBrowseNameAsync(
            parent: assetId,
            childBrowseName: "AssetEndpoint",
            isProperty: true).ConfigureAwait(false);
        var read = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither,
            new ReadValueIdCollection
            {
                new ReadValueId { NodeId = endpointPropId, AttributeId = Attributes.Value },
            },
            CancellationToken.None).ConfigureAwait(false);
        read.Results[0].Value.Should().Be(endpoint, "AssetEndpoint Property must carry the supplied URI verbatim");
    }

    [Test]
    public async Task CreateAssetForEndpoint_DuplicateName_ReturnsBadBrowseNameDuplicated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var assetName = "CreateAFEDup_" + suffix;

        var (status1, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant(assetName),
                new Variant($"opc.tcp://first-{suffix}.invalid:4840"),
            }).ConfigureAwait(false);
        StatusCode.IsGood(status1).Should().BeTrue("first CreateAssetForEndpoint must succeed, got {0}", status1);

        var (status2, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant(assetName),
                new Variant($"opc.tcp://second-{suffix}.invalid:4840"),
            }).ConfigureAwait(false);
        status2.Code.Should().Be(StatusCodes.BadBrowseNameDuplicated,
            "§6.3.5 inherits the §6.3.2 duplicate-name rule — a second create with the same AssetName must fail, got {0}", status2);
    }

    [Test]
    public async Task ConnectionTest_KnownEndpoint_ReturnsSimulated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var endpoint = $"modbus+tcp://known-{suffix}.invalid:502/1";

        // Onboard an asset against this endpoint via CreateAssetForEndpoint so the
        // simulator considers it "known".
        var (createStatus, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant("ConnTestKnown_" + suffix),
                new Variant(endpoint),
            }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("setup CreateAssetForEndpoint must succeed, got {0}", createStatus);

        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(ConnectionTestTypeMethodId),
            arguments: new VariantCollection { new Variant(endpoint) }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue(
            "ConnectionTest method itself must return Good; the verdict travels on the outputs (§6.3.6), got {0}", status);
        outputs.Should().HaveCount(2, "§6.3.6 declares two output arguments (Success, Status)");
        outputs[0].Value.Should().Be(true, "Success must be true for a known endpoint");
        outputs[1].Value.Should().Be("Simulated",
            "the simulator never opens a real southbound connection — a hit on the endpoint table reports 'Simulated'");
    }

    [Test]
    public async Task ConnectionTest_UnknownEndpoint_ReturnsUnknownEndpoint()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var endpoint = $"opc.tcp://never-onboarded-{suffix}.invalid:4840";

        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(ConnectionTestTypeMethodId),
            arguments: new VariantCollection { new Variant(endpoint) }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue(
            "ConnectionTest method itself must return Good even for unknown endpoints (§6.3.6), got {0}", status);
        outputs[0].Value.Should().Be(false, "Success must be false for an unknown endpoint");
        outputs[1].Value.Should().Be("UnknownEndpoint",
            "Status reports the failure category in a clientreadable form");
    }

    [Test]
    public async Task SupportedWoTBindings_AdvertisesSimulatorBinding()
    {
        // OPC 10100-1 §6.3.1: Property on WoTAssetConnectionManagementType. Materialized at
        // runtime under i=31 with a fresh NodeId; resolve by BrowseName under the management
        // object rather than by the type-side i=40. The server advertises the OPC PLC
        // simulator binding so CloseAndUpdate can validate TD bindings against it.
        const string SimulatorBindingUri = "https://opcfoundation.org/OpcPlc/simulator";

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
        ((string[])result.Value).Should().Contain(SimulatorBindingUri,
            "the server advertises the OPC PLC simulator binding so TDs can target it");
    }

    [Test]
    public async Task Configuration_ObjectIsBrowseable()
    {
        // OPC 10100-1 §6.3.7: Configuration : WoTAssetConfigurationType is an Optional child
        // of WoTAssetConnectionManagementType. Exposes a single empty License property; the
        // <WoTConfigurationParameterName> placeholder is omitted.
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
        result.Value.Should().Be("MIT",
            "License surfaces the SPDX identifier of the simulator's license per §6.3.7");
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
