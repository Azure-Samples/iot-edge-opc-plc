namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Tests for the per-asset <c>WoTAssetFileType</c> instance (§6.3.10): the standard
/// File API round-trip (Open/Write/CloseAndUpdate), rejection of writes on released
/// handles, and per-asset handle isolation (no singleton sharing).
/// </summary>
public partial class WotConTests
{
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
                new Variant(Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""IsoA""}")),
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
}
