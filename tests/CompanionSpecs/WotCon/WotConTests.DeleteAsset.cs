namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the <c>DeleteAsset</c> mRPC: happy-path removal (asset + Organizes ref +
/// per-asset WoTFile + materialized properties) and the BadNotFound rejection for
/// unknown AssetIds.
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task DeleteAsset_RemovesAssetAndOrganizesReference()
    {
        // Regression for the BadTooManyArguments bug: the NodeSet importer leaves
        // MethodState.InputArguments null on DeleteAsset, so before the fix any client
        // supplying the required AssetId would be rejected with BadTooManyArguments.
        var assetName = "DeleteAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (createStatus, createOutputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue();
        var assetId = createOutputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        var (deleteStatus, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DeleteAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetId) }).ConfigureAwait(false);

        StatusCode.IsGood(deleteStatus).Should().BeTrue(
            "DeleteAsset with the AssetId from CreateAsset should succeed, got {0}", deleteStatus);

        // The asset must no longer be reachable via Organizes from WoTAssetConnectionManagement.
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
        results.Results[0].References.Should().NotContain(
            r => r.BrowseName.Name == assetName,
            "the deleted asset must not be browseable from WoTAssetConnectionManagement");
    }

    [Test]
    public async Task DeleteAsset_UnknownAssetId_ReturnsBadNotFound()
    {
        var bogus = new NodeId(Guid.NewGuid(), WotConNamespaceIndex);

        var (status, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DeleteAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(bogus) }).ConfigureAwait(false);

        status.Code.Should().Be(StatusCodes.BadNotFound);
    }
}
