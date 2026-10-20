namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using System.Collections.Generic;

using FluentAssertions;
using Opc.Ua;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Helpers shared across the WoT-Con test partials: thin wrappers over OPC UA
/// Call / Browse / Read service requests plus a couple of higher-level builders
/// (CreateAsset + resolve WoTFile, upload+finalize a TD).
/// </summary>
public partial class WotConTests
{
    /// <summary>
    /// Opens the asset's WoTFile, writes the TD payload, then drives a successful
    /// CloseAndUpdate. Fails the test if any step does not return Good.
    /// </summary>
    private async Task UploadAndFinalizeTdAsync(NodeId fileId, string td)
    {
        uint handle = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes(td)).ConfigureAwait(false);
        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant(handle) }).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue("CloseAndUpdate should succeed, got {0}", closeStatus);
    }

    /// <summary>
    /// Browses the forward HasComponent Variable children of an asset — the materialized
    /// TD properties.
    /// </summary>
    private async Task<List<ReferenceDescription>> BrowseAssetVariableChildrenAsync(NodeId assetId)
    {
        var bd = new BrowseDescription
        {
            NodeId = assetId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All,
        };
        var resp = await Session.BrowseAsync(
            null, null, 0,
            new List<BrowseDescription> { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.ToArray().Should().ContainSingle();
        return resp.Results[0].References.ToArray().ToList();
    }

    /// <summary>
    /// Reads the <see cref="Attributes.AccessLevel"/> attribute of a variable node.
    /// </summary>
    private async Task<byte> ReadAccessLevelAsync(NodeId nodeId)
    {
        var nodesToRead = new List<ReadValueId>
        {
            new ReadValueId { NodeId = nodeId, AttributeId = Attributes.AccessLevel },
        };
        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
        StatusCode.IsGood(resp.Results[0].StatusCode).Should().BeTrue();
        return (byte)resp.Results[0].WrappedValue.AsBoxedObject(Variant.BoxingBehavior.Legacy);
    }

    /// <summary>
    /// Opens the asset's WoTFile for read+write+erase, writes the payload, and returns
    /// the file handle so the caller can drive CloseAndUpdate next.
    /// </summary>
    private async Task<uint> OpenWriteAsync(NodeId fileId, byte[] payload)
    {
        var (openStatus, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new List<Variant> { new Variant((byte)6) }).ConfigureAwait(false);
        StatusCode.IsGood(openStatus).Should().BeTrue("Open should succeed, got {0}", openStatus);
        uint handle = Convert.ToUInt32(openOutputs[0].AsBoxedObject(Variant.BoxingBehavior.Legacy));

        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new List<Variant>
            {
                new Variant(handle),
                Variant.From((ByteString)payload),
            }).ConfigureAwait(false);
        StatusCode.IsGood(writeStatus).Should().BeTrue("Write should succeed, got {0}", writeStatus);

        return handle;
    }

    /// <summary>
    /// Issues a single Call service request and returns the resulting status code and output arguments.
    /// </summary>
    private async Task<(StatusCode Status, List<Variant> Outputs)> CallAsync(
        NodeId objectId,
        NodeId methodId,
        List<Variant> arguments)
    {
        var request = new CallMethodRequest
        {
            ObjectId = objectId,
            MethodId = methodId,
            InputArguments = arguments,
        };

        var response = await Session.CallAsync(
            null,
            new List<CallMethodRequest> { request },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.ToArray().Should().ContainSingle();
        var result = response.Results[0];
        return (result.StatusCode, result.OutputArguments.ToArray().ToList());
    }

    /// <summary>
    /// Creates an asset with the given name and resolves its per-asset WoTFile child via
    /// TranslateBrowsePathsToNodeIds. Returns both NodeIds for use in File-API tests.
    /// </summary>
    private async Task<(NodeId AssetId, NodeId FileId)> CreateAssetAndResolveFileAsync(string assetName)
    {
        var (createStatus, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new List<Variant> { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("CreateAsset should succeed, got status {0}", createStatus);
        outputs[0].TryGetValue(out NodeId assetId).Should().BeTrue();
        assetId.IsNull.Should().BeFalse();

        var browsePath = new BrowsePath
        {
            StartingNode = assetId,
            RelativePath = new RelativePath
            {
                Elements =
                [
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasComponent,
                        IsInverse = false,
                        IncludeSubtypes = true,
                        TargetName = new QualifiedName("WoTFile", WotConNamespaceIndex),
                    },
                ],
            },
        };

        var response = await Session.TranslateBrowsePathsToNodeIdsAsync(
            null,
            new List<BrowsePath> { browsePath },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.ToArray().Should().ContainSingle();
        var bp = response.Results[0];
        StatusCode.IsGood(bp.StatusCode).Should().BeTrue("TranslateBrowsePath WoTFile should succeed, got {0}", bp.StatusCode);
        bp.Targets.ToArray().Should().ContainSingle("asset must have exactly one WoTFile child");
        var fileId = ExpandedNodeId.ToNodeId(bp.Targets[0].TargetId, Session.NamespaceUris);
        fileId.IsNull.Should().BeFalse("WoTFile child must resolve to a real NodeId");
        return (assetId, fileId);
    }
}
