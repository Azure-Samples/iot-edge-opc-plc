namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for OPC 10100-1 §6.3.11: materialized Variables (from TD <c>properties</c>) and
/// Methods (from TD <c>actions</c>) under an asset must be linked with
/// <c>HasWoTComponent</c> (subtype of <c>HasComponent</c>) rather than plain
/// <c>HasComponent</c>. Plumbing children that are not WoT affordances — e.g. the
/// per-asset <c>WoTFile</c> — must stay on <c>HasComponent</c>.
/// </summary>
public partial class WotConTests
{
    private const uint HasWoTComponentReferenceTypeId = 142;

    [Test]
    public async Task CloseAndUpdateTdPropertiesAreLinkedViaHasWoTComponent()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("HwtcProp_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Sensor",
              "properties": {
                "temperature": { "type": "number" }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var refs = await BrowseStrictHasWoTComponentChildrenAsync(assetId).ConfigureAwait(false);
        refs.Should().Contain(r => r.BrowseName.Name == "temperature" && r.NodeClass == NodeClass.Variable);
    }

    [Test]
    public async Task CloseAndUpdateTdActionsAreLinkedViaHasWoTComponent()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("HwtcAct_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Lamp",
              "actions": {
                "toggle": {}
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var refs = await BrowseStrictHasWoTComponentChildrenAsync(assetId).ConfigureAwait(false);
        refs.Should().Contain(r => r.BrowseName.Name == "toggle" && r.NodeClass == NodeClass.Method);
    }

    [Test]
    public async Task CreateAssetPerAssetWoTFileStaysOnPlainHasComponent()
    {
        var (assetId, _) = await CreateAssetAndResolveFileAsync("HwtcFile_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        var hwtcRefs = await BrowseStrictHasWoTComponentChildrenAsync(assetId).ConfigureAwait(false);
        hwtcRefs.Should().NotContain(
            r => r.BrowseName.Name == "WoTFile",
            "WoTFile is OPC 10000-5 FileType plumbing, not a WoT affordance \u2014 must remain on plain HasComponent");
    }

    /// <summary>
    /// Browses forward children of an asset reached strictly via <c>HasWoTComponent</c>
    /// (<c>IncludeSubtypes=false</c>) so the assertion fails if the wiring degrades
    /// back to plain <c>HasComponent</c>.
    /// </summary>
    private async Task<ReferenceDescriptionCollection> BrowseStrictHasWoTComponentChildrenAsync(NodeId assetId)
    {
        var bd = new BrowseDescription
        {
            NodeId = assetId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = new NodeId(HasWoTComponentReferenceTypeId, WotConNamespaceIndex),
            IncludeSubtypes = false,
            NodeClassMask = (uint)(NodeClass.Variable | NodeClass.Method),
            ResultMask = (uint)BrowseResultMask.All,
        };

        var resp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        return resp.Results[0].References;
    }
}
