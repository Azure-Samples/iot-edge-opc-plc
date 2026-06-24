namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for OPC 10100-1 §6.3.4 <c>DiscoverAssets</c> on
/// <c>WoTAssetConnectionManagement</c> (i=31) and the §6.3.8 optional
/// <c>IWoTAssetType.AssetEndpoint</c> Property it draws its values from.
/// <para>
/// The fixture is shared across the test class, so assertions stay scoped to "the
/// endpoint(s) I just uploaded must / must not be in the returned set" rather than
/// asserting an exact set — sibling tests can leave their own assets in <c>_assets</c>.
/// </para>
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task DiscoverAssets_AfterUploadWithBase_IncludesEndpoint()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var endpoint = $"modbus+tcp://discover-{suffix}.invalid:1502/1";
        var (_, fileId) = await CreateAssetAndResolveFileAsync("DiscoverAssetWithBase_" + suffix).ConfigureAwait(false);

        await UploadAndFinalizeTdAsync(fileId,
            "{\"@context\":\"https://www.w3.org/2022/wot/td/v1.1\",\"title\":\"DiscoverAssetWithBase\",\"base\":\""
            + endpoint + "\"}").ConfigureAwait(false);

        var endpoints = await CallDiscoverAssetsAsync().ConfigureAwait(false);
        endpoints.Should().Contain(endpoint,
            "an asset whose TD declared a top-level 'base' must surface that URI in DiscoverAssets (§6.3.4)");
    }

    [Test]
    public async Task DiscoverAssets_TdWithoutBase_NotIncluded()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var assetName = "DiscoverAssetNoBase_" + suffix;
        var (_, fileId) = await CreateAssetAndResolveFileAsync(assetName).ConfigureAwait(false);

        await UploadAndFinalizeTdAsync(fileId,
            "{\"@context\":\"https://www.w3.org/2022/wot/td/v1.1\",\"title\":\"" + assetName + "\"}").ConfigureAwait(false);

        // The asset still exists and is browseable; the §6.3.8 AssetEndpoint Property is
        // Optional and is intentionally not materialized when the TD omits 'base', so the
        // asset simply does not contribute to DiscoverAssets. Anchor the negative
        // assertion on the unique asset name so concurrent tests cannot pollute it.
        var endpoints = await CallDiscoverAssetsAsync().ConfigureAwait(false);
        endpoints.Should().NotContain(s => s.Contains(suffix, StringComparison.Ordinal),
            "a TD without 'base' must not contribute any endpoint to DiscoverAssets");
    }

    [Test]
    public async Task DiscoverAssets_TwoAssetsSharingBase_ReturnsSingleEntry()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sharedEndpoint = $"opc.tcp://shared-{suffix}.invalid:4840";

        var (_, fileA) = await CreateAssetAndResolveFileAsync("DiscoverDupA_" + suffix).ConfigureAwait(false);
        var (_, fileB) = await CreateAssetAndResolveFileAsync("DiscoverDupB_" + suffix).ConfigureAwait(false);

        await UploadAndFinalizeTdAsync(fileA,
            "{\"@context\":\"https://www.w3.org/2022/wot/td/v1.1\",\"title\":\"DiscoverDupA\",\"base\":\""
            + sharedEndpoint + "\"}").ConfigureAwait(false);
        await UploadAndFinalizeTdAsync(fileB,
            "{\"@context\":\"https://www.w3.org/2022/wot/td/v1.1\",\"title\":\"DiscoverDupB\",\"base\":\""
            + sharedEndpoint + "\"}").ConfigureAwait(false);

        var endpoints = await CallDiscoverAssetsAsync().ConfigureAwait(false);
        endpoints.Count(e => e == sharedEndpoint).Should().Be(1,
            "DiscoverAssets de-duplicates endpoints (case-sensitive Ordinal) across assets");
    }

    [Test]
    public async Task AssetEndpoint_PropertyMaterializedWhenTdHasBase()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var endpoint = $"http://endpoint-{suffix}.invalid:8080/things/demo";
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("AssetEndpointAsset_" + suffix).ConfigureAwait(false);

        await UploadAndFinalizeTdAsync(fileId,
            "{\"@context\":\"https://www.w3.org/2022/wot/td/v1.1\",\"title\":\"AssetEndpointAsset\",\"base\":\""
            + endpoint + "\"}").ConfigureAwait(false);

        // OPC 10100-1 §6.3.8 — IWoTAssetType.AssetEndpoint (i=122) is Optional but, when
        // the TD carries 'base', we materialize it under the asset as a String Property.
        var endpointPropId = await ResolveChildByBrowseNameAsync(
            parent: assetId,
            childBrowseName: "AssetEndpoint",
            isProperty: true).ConfigureAwait(false);

        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId { NodeId = endpointPropId, AttributeId = Attributes.Value },
        };
        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
        StatusCode.IsGood(resp.Results[0].StatusCode).Should().BeTrue(
            "AssetEndpoint property must be readable, got {0}", resp.Results[0].StatusCode);
        resp.Results[0].Value.Should().Be(endpoint,
            "AssetEndpoint must carry the TD 'base' verbatim");
    }

    /// <summary>
    /// Invokes <c>DiscoverAssets</c> on the standard management instance and returns the
    /// returned endpoint list. Asserts the call succeeded and the output shape is a
    /// <see cref="string"/> array.
    /// </summary>
    private async Task<string[]> CallDiscoverAssetsAsync()
    {
        var (status, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DiscoverAssetsTypeMethodId),
            arguments: new VariantCollection()).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue(
            "DiscoverAssets must succeed (OPC 10100-1 §6.3.4 defines no failure codes), got {0}", status);
        outputs.Should().ContainSingle("DiscoverAssets has exactly one output argument 'AssetEndpoints'");
        outputs[0].Value.Should().BeAssignableTo<string[]>(
            "AssetEndpoints is declared as String[]");
        return (string[])outputs[0].Value;
    }
}
