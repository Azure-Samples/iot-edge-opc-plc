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
    private const uint WoTFileObjectId = 144;
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
        // Open (mode=6 = read+write+erase, conventional for File API uploads).
        var (openStatus, openOutputs) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);

        StatusCode.IsGood(openStatus).Should().BeTrue("Open should succeed, got status {0}", openStatus);
        openOutputs.Should().HaveCountGreaterThanOrEqualTo(1);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);
        handle.Should().BeGreaterThan(0u, "Open should return a non-zero file handle");

        // Write a small TD-ish payload.
        byte[] payload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""WotConTestThing""}");
        var (writeStatus, _) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(payload),
            }).ConfigureAwait(false);

        StatusCode.IsGood(writeStatus).Should().BeTrue("Write should succeed, got status {0}", writeStatus);

        // CloseAndUpdate finalizes the upload.
        var (closeStatus, _) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);

        StatusCode.IsGood(closeStatus).Should().BeTrue(
            "CloseAndUpdate should succeed, got status {0}", closeStatus);
    }

    [Test]
    public async Task WoTFile_WriteAfterClose_ReturnsBadInvalidArgument()
    {
        // Open then plain Close (releases the handle without applying).
        var (_, openOutputs) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);

        var (closeStatus, _) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
            methodId: new NodeId(Methods.FileType_Close, 0),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue();

        // Subsequent Write on the released handle must be rejected.
        var (writeStatus, _) = await CallAsync(
            objectId: WotConNodeId(WoTFileObjectId),
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
}
