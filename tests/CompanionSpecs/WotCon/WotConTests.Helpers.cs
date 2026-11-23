namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using Opc.Ua;
using System;
using System.IO;
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
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue("CloseAndUpdate should succeed, got {0}", closeStatus);
    }

    private async Task<string> ReadStoredTdAsync(NodeId fileId)
    {
        var (openStatus, outputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Open, 0),
            [new Variant((byte)1)]).ConfigureAwait(false);
        StatusCode.IsGood(openStatus).Should().BeTrue();
        uint handle = Convert.ToUInt32(outputs[0].Value);
        using var buffer = new MemoryStream();
        try
        {
            while (true)
            {
                var (readStatus, readOutputs) = await CallAsync(
                    fileId, new NodeId(Methods.FileType_Read, 0),
                    [new Variant(handle), new Variant(7)]).ConfigureAwait(false);
                StatusCode.IsGood(readStatus).Should().BeTrue();
                byte[] chunk = (byte[])readOutputs[0].Value;
                if (chunk.Length == 0)
                {
                    return Encoding.UTF8.GetString(buffer.ToArray());
                }

                buffer.Write(chunk, 0, chunk.Length);
            }
        }
        finally
        {
            var (closeStatus, _) = await CallAsync(
                fileId, new NodeId(Methods.FileType_Close, 0),
                [new Variant(handle)]).ConfigureAwait(false);
            StatusCode.IsGood(closeStatus).Should().BeTrue();
        }
    }

    /// <summary>
    /// Reads the value of a standard FileType property (e.g. Size, LastModifiedTime) of the WoTFile.
    /// </summary>
    private async Task<T> ReadFilePropertyAsync<T>(NodeId fileId, string browseName)
    {
        var browsePath = new BrowsePath
        {
            StartingNode = fileId,
            RelativePath = new RelativePath
            {
                Elements =
                {
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasProperty,
                        IsInverse = false,
                        IncludeSubtypes = true,
                        TargetName = new QualifiedName(browseName, 0),
                    },
                },
            },
        };
        var translate = await Session.TranslateBrowsePathsToNodeIdsAsync(
            null, new BrowsePathCollection { browsePath }, CancellationToken.None).ConfigureAwait(false);
        translate.Results[0].Targets.Should().ContainSingle();
        var propertyId = ExpandedNodeId.ToNodeId(translate.Results[0].Targets[0].TargetId, Session.NamespaceUris);

        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither,
            new ReadValueIdCollection { new ReadValueId { NodeId = propertyId, AttributeId = Attributes.Value } },
            CancellationToken.None).ConfigureAwait(false);
        StatusCode.IsGood(resp.Results[0].StatusCode).Should().BeTrue();
        return (T)resp.Results[0].Value;
    }

    /// <summary>
    /// Browses the forward HasComponent Variable children of an asset — the materialized
    /// TD properties.
    /// </summary>
    private async Task<ReferenceDescriptionCollection> BrowseAssetVariableChildrenAsync(NodeId assetId)
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
            new BrowseDescriptionCollection { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        return resp.Results[0].References;
    }

    /// <summary>
    /// Reads the <see cref="Attributes.AccessLevel"/> attribute of a variable node.
    /// </summary>
    private async Task<byte> ReadAccessLevelAsync(NodeId nodeId)
    {
        var nodesToRead = new ReadValueIdCollection
        {
            new ReadValueId { NodeId = nodeId, AttributeId = Attributes.AccessLevel },
        };
        var resp = await Session.ReadAsync(
            null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
        StatusCode.IsGood(resp.Results[0].StatusCode).Should().BeTrue();
        return (byte)resp.Results[0].Value;
    }

    /// <summary>
    /// Opens the asset's WoTFile for write+erase, writes the payload, and returns
    /// the file handle so the caller can drive CloseAndUpdate next.
    /// </summary>
    private async Task<uint> OpenWriteAsync(NodeId fileId, byte[] payload)
    {
        var (openStatus, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        StatusCode.IsGood(openStatus).Should().BeTrue("Open should succeed, got {0}", openStatus);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);

        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(payload),
            }).ConfigureAwait(false);
        StatusCode.IsGood(writeStatus).Should().BeTrue("Write should succeed, got {0}", writeStatus);

        return handle;
    }

    /// <summary>
    /// Issues a single Call service request and returns the resulting status code and output arguments.
    /// </summary>
    private async Task<(StatusCode Status, VariantCollection Outputs)> CallAsync(
        NodeId objectId,
        NodeId methodId,
        VariantCollection arguments)
    {
        var request = new CallMethodRequest
        {
            ObjectId = objectId,
            MethodId = methodId,
            InputArguments = arguments,
        };

        var response = await Session.CallAsync(
            null,
            new CallMethodRequestCollection { request },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        var result = response.Results[0];
        return (result.StatusCode, result.OutputArguments ?? new VariantCollection());
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
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("CreateAsset should succeed, got status {0}", createStatus);
        var assetId = outputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        var browsePath = new BrowsePath
        {
            StartingNode = assetId,
            RelativePath = new RelativePath
            {
                Elements =
                {
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasComponent,
                        IsInverse = false,
                        IncludeSubtypes = true,
                        TargetName = new QualifiedName("WoTFile", WotConNamespaceIndex),
                    },
                },
            },
        };

        var response = await Session.TranslateBrowsePathsToNodeIdsAsync(
            null,
            new BrowsePathCollection { browsePath },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        var bp = response.Results[0];
        StatusCode.IsGood(bp.StatusCode).Should().BeTrue("TranslateBrowsePath WoTFile should succeed, got {0}", bp.StatusCode);
        bp.Targets.Should().ContainSingle("asset must have exactly one WoTFile child");
        var fileId = ExpandedNodeId.ToNodeId(bp.Targets[0].TargetId, Session.NamespaceUris);
        NodeId.IsNull(fileId).Should().BeFalse("WoTFile child must resolve to a real NodeId");
        return (assetId, fileId);
    }
}
