namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the <c>CreateAsset</c> mRPC and the resulting asset node — return shape,
/// duplicate-name rejection per §6.3.2, browseability via <c>Organizes</c>, and the
/// <c>HasInterface</c> wiring to <c>IWoTAssetType</c>.
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task CreateAsset_ReturnsNonNullAssetId()
    {
        var assetName = "TestAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue("CreateAsset should succeed, got status {0}", status);
        outputs.Should().HaveCountGreaterThanOrEqualTo(1);
        var assetId = outputs[0].Value as NodeId;
        assetId.Should().NotBeNull();
        NodeId.IsNull(assetId).Should().BeFalse("AssetId must be a real, non-null NodeId");
    }

    [Test]
    public async Task CreateAsset_DuplicateName_ReturnsBadBrowseNameDuplicated()
    {
        // Per OPC 10100-1 §6.3.2: "If an Asset with the AssetName already exists the
        // result Bad_BrowseNameDuplicated will be returned."
        var assetName = "DupAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (status1, outputs1) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(status1).Should().BeTrue();
        var firstId = outputs1[0].Value as NodeId;
        firstId.Should().NotBeNull();

        var (status2, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);

        status2.Code.Should().Be(StatusCodes.BadBrowseNameDuplicated,
            "a second CreateAsset with the same AssetName must be rejected per §6.3.2, got {0}", status2);
    }

    [Test]
    public async Task CreateAsset_NewAssetIsBrowseableFromManagementObject()
    {
        // Per OPC 10100-1 §6.3.2: "CreateAsset … adds an Organizes Reference from the
        // WoTAssetConnectionManagement Object." So the new asset must show up when we
        // browse forward Organizes from i=31.
        var assetName = "BrowseAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (createStatus, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("CreateAsset should succeed, got status {0}", createStatus);
        var assetId = outputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        var browseDescription = new BrowseDescription
        {
            NodeId = WotConNodeId(WotAssetConnectionManagementObjectId),
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        results.Results.Should().ContainSingle();
        var references = results.Results[0].References;
        references.Should().Contain(
            r => r.BrowseName.Name == assetName,
            "the new asset must be reachable via Organizes from WoTAssetConnectionManagement");

        var match = references.Single(r => r.BrowseName.Name == assetName);
        ExpandedNodeId.ToNodeId(match.NodeId, Session.NamespaceUris)
            .Should().Be(assetId, "browsed NodeId should match the AssetId returned by CreateAsset");
    }

    [Test]
    public async Task CreateAsset_NewAssetHasInterfaceToIWoTAssetType()
    {
        // Per OPC 10100-1 §6.3.8: the new asset implements the IWoTAssetType Interface.
        // The NodeSet's <WoTAssetName> placeholder models this as BaseObjectType +
        // HasInterface → IWoTAssetType (ns=WotCon;i=42). Mirror that for created assets.
        var assetName = "InterfaceAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (createStatus, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("CreateAsset should succeed, got status {0}", createStatus);
        var assetId = outputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        var browseDescription = new BrowseDescription
        {
            NodeId = assetId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasInterface,
            IncludeSubtypes = false,
            NodeClassMask = (uint)NodeClass.ObjectType,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        results.Results.Should().ContainSingle();
        var references = results.Results[0].References;
        references.Should().ContainSingle(
            r => ExpandedNodeId.ToNodeId(r.NodeId, Session.NamespaceUris) == WotConNodeId(IWoTAssetTypeId),
            "the new asset must have a HasInterface reference to IWoTAssetType per §6.3.8");
    }
}
