namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the WoT-Con (Web of Things Connectivity) companion spec node manager.
/// Verifies the CreateAsset mRPC and the WoTFile File API (Open/Write/Close/CloseAndUpdate)
/// that the OPC UA Commander uses to upload Thing Descriptions.
/// </summary>
[TestFixture]
public class WotConTests : SimulatorTestsBase
{
    // NodeIds defined in the WoT-Con NodeSet (Opc.Ua.WotCon.NodeSet2.xml).
    // Namespace is OpcPlc.Namespaces.WotCon ("http://opcfoundation.org/UA/WoT-Con/");
    // index is server-assigned at runtime.
    private const uint WotAssetConnectionManagementObjectId = 31;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint IWoTAssetTypeId = 42;
    private const uint FileCloseAndUpdateTypeMethodId = 111;

    public WotConTests() : base(["--wotcon"])
    {
    }

    private ushort WotConNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);

    private NodeId WotConNodeId(uint identifier) => new(identifier, WotConNamespaceIndex);

    [Test]
    public void WotConNamespace_IsRegistered()
    {
        WotConNamespaceIndex.Should().BeGreaterThan((ushort)0,
            "the WoT-Con namespace should be registered when --wotcon is set");
    }

    [Test]
    public async Task WotAssetConnectionManagement_HasCreateAssetMethod()
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = WotConNodeId(WotAssetConnectionManagementObjectId),
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Method,
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
            r => r.BrowseName.Name == "CreateAsset",
            "CreateAsset method must be a child of WoTAssetConnectionManagement");
    }

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
    public async Task CreateAsset_DuplicateName_IsIdempotent()
    {
        var assetName = "DupAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (status1, outputs1) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(status1).Should().BeTrue();
        var firstId = outputs1[0].Value as NodeId;

        var (status2, outputs2) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(status2).Should().BeTrue();
        var secondId = outputs2[0].Value as NodeId;

        secondId.Should().Be(firstId,
            "CreateAsset is idempotent — a second call with the same name returns the same AssetId");
    }

    [Test]
    public async Task WoTFile_RoundTrip_OpenWriteCloseAndUpdate_Succeeds()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync("RoundTripAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        // Open (mode=6 = read+write+erase, conventional for File API uploads).
        var (openStatus, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);

        StatusCode.IsGood(openStatus).Should().BeTrue("Open should succeed, got status {0}", openStatus);
        openOutputs.Should().HaveCountGreaterThanOrEqualTo(1);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);
        handle.Should().BeGreaterThan(0u, "Open should return a non-zero file handle");

        // Write a small TD-ish payload.
        byte[] payload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""WotConTestThing""}");
        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(payload),
            }).ConfigureAwait(false);

        StatusCode.IsGood(writeStatus).Should().BeTrue("Write should succeed, got status {0}", writeStatus);

        // CloseAndUpdate finalizes the upload.
        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);

        StatusCode.IsGood(closeStatus).Should().BeTrue(
            "CloseAndUpdate should succeed, got status {0}", closeStatus);
    }

    [Test]
    public async Task WoTFile_WriteAfterClose_ReturnsBadInvalidArgument()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync("WriteAfterCloseAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        // Open then plain Close (releases the handle without applying).
        var (_, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Close, 0),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue();

        // Subsequent Write on the released handle must be rejected.
        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(new byte[] { 1, 2, 3 }),
            }).ConfigureAwait(false);

        writeStatus.Code.Should().Be(StatusCodes.BadInvalidArgument,
            "writing on a released handle must fail with BadInvalidArgument");
    }

    [Test]
    public async Task CreateAsset_PerAssetWoTFileNodesAreDistinctAndIsolated()
    {
        // Per OPC 10100-1 §6.3.10: each asset owns its own WoTAssetFileType instance.
        // Two assets created back-to-back must expose two distinct WoTFile NodeIds,
        // and uploads on one must not surface on the other.
        var (assetA, fileA) = await CreateAssetAndResolveFileAsync("IsoA_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        var (assetB, fileB) = await CreateAssetAndResolveFileAsync("IsoB_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        assetA.Should().NotBe(assetB, "each CreateAsset call must mint a distinct AssetId");
        fileA.Should().NotBe(fileB, "each asset must own a distinct WoTFile instance (no singleton sharing)");

        // Open + Write on A.
        var (_, openAOutputs) = await CallAsync(
            objectId: fileA,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        uint handleA = Convert.ToUInt32(openAOutputs[0].Value);

        var (writeAStatus, _) = await CallAsync(
            objectId: fileA,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handleA),
                new Variant(Encoding.UTF8.GetBytes("asset-A-payload")),
            }).ConfigureAwait(false);
        StatusCode.IsGood(writeAStatus).Should().BeTrue();

        // Using A's handle against B's file must be rejected as Unknown file handle.
        var (writeAOnBStatus, _) = await CallAsync(
            objectId: fileB,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handleA),
                new Variant(Encoding.UTF8.GetBytes("should-not-land")),
            }).ConfigureAwait(false);
        writeAOnBStatus.Code.Should().Be(StatusCodes.BadInvalidArgument,
            "a handle minted by asset A.Open must not be valid on asset B");

        // A's CloseAndUpdate still finalizes only A's buffer.
        var (closeAStatus, _) = await CallAsync(
            objectId: fileA,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handleA) }).ConfigureAwait(false);
        StatusCode.IsGood(closeAStatus).Should().BeTrue();
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
            "the new asset must have a HasInterface reference to IWoTAssetType per \u00a76.3.8");
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
