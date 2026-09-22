namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for enumerating the assets currently managed by the server.
/// <para>
/// OPC 10100-1 v1.02 defines <b>no</b> list / query method on
/// <c>WoTAssetConnectionManagementType</c> — the method set is CreateAsset, DeleteAsset,
/// DiscoverAssets (which returns asset <i>endpoints</i>, not assets), CreateAssetForEndpoint
/// and ConnectionTest. Listing is therefore expressed with the plain <c>Browse</c> service
/// over the <c>Organizes</c> References that §6.3.2 mandates from
/// <c>WoTAssetConnectionManagement</c> to each asset.
/// </para>
/// <para>
/// These tests pin that contract because it is the server-side behaviour consumed by the
/// <c>listAssets</c> mRPC of the AIO OPC UA Commander: BrowseName carries the AssetName,
/// the target NodeId is the AssetId returned by CreateAsset, deletes are reflected
/// immediately, management-object plumbing is excluded, and the result pages correctly
/// through continuation points.
/// </para>
/// </summary>
public partial class WotConTests
{
    /// <summary>
    /// Upper bound on BrowseNext iterations so a server that keeps handing back a
    /// continuation point fails the test instead of hanging the run.
    /// </summary>
    private const int MaxBrowseNextIterations = 1000;

    [Test]
    public async Task ListAssets_BrowseReturnsEveryCreatedAssetWithItsAssetId()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var names = new[] { "ListA_" + suffix, "ListB_" + suffix, "ListC_" + suffix };

        var created = new NodeId[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            created[i] = await CreateManagedAssetAsync(names[i]).ConfigureAwait(false);
        }

        var (references, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);

        for (int i = 0; i < names.Length; i++)
        {
            var matches = references.Where(r => r.BrowseName.Name == names[i]).ToList();
            matches.Should().ContainSingle(
                "asset '{0}' must appear exactly once in the Organizes listing", names[i]);
            matches[0].NodeClass.Should().Be(NodeClass.Object, "an asset is an Object per §6.3.8");
            matches[0].BrowseName.NamespaceIndex.Should().Be(
                WotConNamespaceIndex,
                "the asset BrowseName must live in the WoT-Con namespace");
            ExpandedNodeId.ToNodeId(matches[0].NodeId, Session.NamespaceUris)
                .Should().Be(created[i], "the listed NodeId must be the AssetId returned by CreateAsset");
        }
    }

    [Test]
    public async Task ListAssets_DeletedAssetDisappearsAndSiblingsRemain()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var doomedName = "ListDoomed_" + suffix;
        var survivorName = "ListSurvivor_" + suffix;

        var doomedId = await CreateManagedAssetAsync(doomedName).ConfigureAwait(false);
        var survivorId = await CreateManagedAssetAsync(survivorName).ConfigureAwait(false);

        var (before, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);
        before.Should().Contain(r => r.BrowseName.Name == doomedName);
        before.Should().Contain(r => r.BrowseName.Name == survivorName);

        var (deleteStatus, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DeleteAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(doomedId) }).ConfigureAwait(false);
        StatusCode.IsGood(deleteStatus).Should().BeTrue("DeleteAsset should succeed, got {0}", deleteStatus);

        var (after, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);
        after.Should().NotContain(
            r => r.BrowseName.Name == doomedName,
            "a deleted asset must vanish from the listing immediately");

        var survivor = after.Should().ContainSingle(r => r.BrowseName.Name == survivorName).Subject;
        ExpandedNodeId.ToNodeId(survivor.NodeId, Session.NamespaceUris).Should().Be(
            survivorId,
            "deleting one asset must not disturb its siblings");
    }

    [Test]
    public async Task ListAssets_ExcludesManagementObjectPlumbing()
    {
        // The listing filter (Organizes + NodeClass.Object) must return assets only. The
        // management object's own children — the Configuration Object (§6.3.7), the
        // SupportedWoTBindings Property (§6.3.1) and the five methods — hang off
        // HasComponent / HasProperty and must not leak into the result.
        var assetName = "ListFilter_" + Guid.NewGuid().ToString("N")[..8];
        _ = await CreateManagedAssetAsync(assetName).ConfigureAwait(false);

        var everything = await BrowseAllChildrenOfManagementObjectAsync().ConfigureAwait(false);
        var plumbing = new[] { "Configuration", "SupportedWoTBindings", "CreateAsset", "DeleteAsset" };
        foreach (var name in plumbing)
        {
            everything.Should().Contain(
                r => r.BrowseName.Name == name,
                "'{0}' must exist under the management object, otherwise this test proves nothing", name);
        }

        var (assets, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);
        assets.Should().Contain(r => r.BrowseName.Name == assetName);
        foreach (var name in plumbing)
        {
            assets.Should().NotContain(
                r => r.BrowseName.Name == name,
                "'{0}' is management-object plumbing, not an asset", name);
        }
    }

    [Test]
    public async Task ListAssets_IncludesAssetsCreatedForEndpoint()
    {
        // §6.3.5 CreateAssetForEndpoint registers an asset exactly like §6.3.2 CreateAsset,
        // so endpoint-first onboarding must be visible to the same listing browse.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var assetName = "ListAFE_" + suffix;

        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetForEndpointTypeMethodId),
            arguments: new VariantCollection
            {
                new Variant(assetName),
                new Variant($"opc.tcp://list-afe-{suffix}.invalid:4840"),
            }).ConfigureAwait(false);
        StatusCode.IsGood(status).Should().BeTrue("CreateAssetForEndpoint should succeed, got {0}", status);
        var assetId = outputs[0].Value as NodeId;
        NodeId.IsNull(assetId).Should().BeFalse();

        var (references, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);

        var match = references.Should().ContainSingle(r => r.BrowseName.Name == assetName).Subject;
        ExpandedNodeId.ToNodeId(match.NodeId, Session.NamespaceUris).Should().Be(assetId);
    }

    [Test]
    public async Task ListAssets_PagesThroughContinuationPoints()
    {
        // A client that caps references per node must still see the full population via
        // BrowseNext. This is the paging path the Commander listAssets mRPC relies on.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var names = new[] { "ListPageA_" + suffix, "ListPageB_" + suffix, "ListPageC_" + suffix };
        foreach (var name in names)
        {
            _ = await CreateManagedAssetAsync(name).ConfigureAwait(false);
        }

        var (paged, serviceCalls) = await BrowseManagedAssetsAsync(requestedMaxReferencesPerNode: 1)
            .ConfigureAwait(false);

        serviceCalls.Should().BeGreaterThan(
            1,
            "capping the page size at one reference must force at least one BrowseNext");

        foreach (var name in names)
        {
            paged.Should().Contain(
                r => r.BrowseName.Name == name,
                "asset '{0}' must survive continuation-point paging", name);
        }

        var (unpaged, _) = await BrowseManagedAssetsAsync().ConfigureAwait(false);
        paged.Select(r => r.NodeId).Should().BeEquivalentTo(
            unpaged.Select(r => r.NodeId),
            "paged and unpaged browses must return the same population");
    }

    /// <summary>
    /// Calls <c>CreateAsset(assetName)</c> on the management object and returns the AssetId.
    /// </summary>
    private async Task<NodeId> CreateManagedAssetAsync(string assetName)
    {
        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue(
            "CreateAsset('{0}') should succeed, got {1}", assetName, status);
        var assetId = outputs[0].Value as NodeId;
        NodeId.IsNull(assetId).Should().BeFalse("AssetId must be a real, non-null NodeId");
        return assetId;
    }

    /// <summary>
    /// Enumerates the managed assets the way a WoT-Con client has to: forward
    /// <c>Organizes</c> References from <c>WoTAssetConnectionManagement</c> restricted to
    /// Objects, following continuation points to exhaustion. Returns the aggregated
    /// References plus the number of Browse / BrowseNext service calls it took, so paging
    /// behaviour can be asserted.
    /// </summary>
    private async Task<(ReferenceDescriptionCollection References, int ServiceCalls)> BrowseManagedAssetsAsync(
        uint requestedMaxReferencesPerNode = 0)
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = WotConNodeId(WotAssetConnectionManagementObjectId),
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var response = await Session.BrowseAsync(
            null,
            null,
            requestedMaxReferencesPerNode,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        StatusCode.IsGood(response.Results[0].StatusCode).Should().BeTrue(
            "Browse(WoTAssetConnectionManagement / Organizes) should succeed, got {0}",
            response.Results[0].StatusCode);

        var all = new ReferenceDescriptionCollection(response.Results[0].References);
        var continuationPoint = response.Results[0].ContinuationPoint;
        int serviceCalls = 1;

        while (continuationPoint != null && continuationPoint.Length > 0)
        {
            serviceCalls.Should().BeLessThan(
                MaxBrowseNextIterations,
                "the server must eventually stop returning continuation points");

            var next = await Session.BrowseNextAsync(
                null,
                false,
                new ByteStringCollection { continuationPoint },
                CancellationToken.None).ConfigureAwait(false);
            serviceCalls++;

            next.Results.Should().ContainSingle();
            StatusCode.IsGood(next.Results[0].StatusCode).Should().BeTrue(
                "BrowseNext should succeed, got {0}", next.Results[0].StatusCode);

            all.AddRange(next.Results[0].References);
            continuationPoint = next.Results[0].ContinuationPoint;
        }

        return (all, serviceCalls);
    }

    /// <summary>
    /// Browses every forward Reference of the management object regardless of Reference
    /// type or NodeClass — used to prove that the plumbing children the listing filter
    /// excludes do actually exist.
    /// </summary>
    private async Task<ReferenceDescriptionCollection> BrowseAllChildrenOfManagementObjectAsync()
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = WotConNodeId(WotAssetConnectionManagementObjectId),
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = null,
            IncludeSubtypes = true,
            NodeClassMask = 0,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var response = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        StatusCode.IsGood(response.Results[0].StatusCode).Should().BeTrue();
        return response.Results[0].References;
    }
}
