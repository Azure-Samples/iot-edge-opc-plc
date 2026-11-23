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
/// Tests for the per-asset <c>WoTAssetFileType</c> instance (§6.3.10): the standard
/// File API round-trip (Open/Write/CloseAndUpdate), rejection of writes on released
/// handles, and per-asset handle isolation (no singleton sharing).
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task WoTFile_Read_ReturnsLastSuccessfulUpload()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "ReadTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().BeEmpty();

        const string firstTd = "{\"title\":\"Readback \u00e9\",\"properties\":{\"temperature\":{\"type\":\"number\"}}}";
        await UploadAndFinalizeTdAsync(fileId, firstTd).ConfigureAwait(false);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(firstTd);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(firstTd, "each read starts at position zero");

        uint handle = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes("{ invalid")).ConfigureAwait(false);
        var (status, _) = await CallAsync(
            fileId, WotConNodeId(FileCloseAndUpdateTypeMethodId),
            [new Variant(handle)]).ConfigureAwait(false);
        status.Code.Should().Be(StatusCodes.BadDecodingError);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(firstTd);

        const string secondTd = """{"title":"Updated readback"}""";
        await UploadAndFinalizeTdAsync(fileId, secondTd).ConfigureAwait(false);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(secondTd);
    }

    [Test]
    public async Task WoTFile_Size_TracksCommittedTdOnly()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "SizeTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """{"title":"Size","properties":{"temperature":{"type":"number"}}}""";
        ulong expectedSize = (ulong)Encoding.UTF8.GetByteCount(td);
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false)).Should().Be(expectedSize);

        uint rejected = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes("{ invalid")).ConfigureAwait(false);
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false))
            .Should().Be(expectedSize, "a staged write must not change Size");
        var (rejectedStatus, _) = await CallAsync(
            fileId, WotConNodeId(FileCloseAndUpdateTypeMethodId),
            [new Variant(rejected)]).ConfigureAwait(false);
        rejectedStatus.Code.Should().Be(StatusCodes.BadDecodingError);
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false))
            .Should().Be(expectedSize, "a rejected upload must not change Size");

        uint abandoned = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes("{}")).ConfigureAwait(false);
        var (closeStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Close, 0),
            [new Variant(abandoned)]).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue();
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false))
            .Should().Be(expectedSize, "an abandoned upload must not change Size");
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(td);
    }

    [Test]
    public async Task WoTFile_LastModifiedTime_ChangesOnlyWhenBytesChange()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "ModifiedTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """{"title":"Modified","properties":{"temperature":{"type":"number"}}}""";
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        DateTime initial = await ReadFilePropertyAsync<DateTime>(fileId, BrowseNames.LastModifiedTime)
            .ConfigureAwait(false);

        await Task.Delay(20).ConfigureAwait(false);
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        (await ReadFilePropertyAsync<DateTime>(fileId, BrowseNames.LastModifiedTime).ConfigureAwait(false))
            .Should().Be(initial, "identical bytes are not a modification");

        const string reformatted = """{ "title": "Modified", "properties": { "temperature": { "type": "number" } } }""";
        await UploadAndFinalizeTdAsync(fileId, reformatted).ConfigureAwait(false);
        (await ReadFilePropertyAsync<DateTime>(fileId, BrowseNames.LastModifiedTime).ConfigureAwait(false))
            .Should().BeAfter(initial, "read-back bytes changed even though the TD is semantically equal");
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false))
            .Should().Be((ulong)Encoding.UTF8.GetByteCount(reformatted));
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(reformatted);
    }

    [TestCase(1)]
    [TestCase(9)]
    public async Task WoTFile_ReadOnlyHandle_RejectsWriteAndCloseAndUpdate(byte mode)
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "ReadOnlyTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """{"title":"ReadOnly"}""";
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        var (_, outputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Open, 0),
            [new Variant(mode)]).ConfigureAwait(false);
        uint handle = Convert.ToUInt32(outputs[0].Value);
        var (positionStatus, positionOutputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_GetPosition, 0),
            [new Variant(handle)]).ConfigureAwait(false);
        StatusCode.IsGood(positionStatus).Should().BeTrue();
        positionOutputs[0].Value.Should().Be(mode == 9 ? (ulong)Encoding.UTF8.GetByteCount(td) : 0ul);
        var (setPositionStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_SetPosition, 0),
            [new Variant(handle), new Variant(0ul)]).ConfigureAwait(false);
        StatusCode.IsGood(setPositionStatus).Should().BeTrue();

        var (writeStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Write, 0),
            [new Variant(handle), new Variant(Encoding.UTF8.GetBytes("""{"title":"Wrong"}"""))])
            .ConfigureAwait(false);
        writeStatus.Code.Should().Be(StatusCodes.BadInvalidState);

        var (updateStatus, _) = await CallAsync(
            fileId, WotConNodeId(FileCloseAndUpdateTypeMethodId),
            [new Variant(handle)]).ConfigureAwait(false);
        updateStatus.Code.Should().Be(StatusCodes.BadInvalidState);

        var (readStatus, readOutputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Read, 0),
            [new Variant(handle), new Variant(1024)]).ConfigureAwait(false);
        StatusCode.IsGood(readStatus).Should().BeTrue("a rejected update must leave the read handle open");
        Encoding.UTF8.GetString((byte[])readOutputs[0].Value).Should().Be(td);
        var (closeStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Close, 0),
            [new Variant(handle)]).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue();
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(td);
    }

    [Test]
    public async Task WoTFile_ReadHandle_KeepsSnapshotAcrossUpdates()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "SnapshotTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string firstTd = """{"title":"Snapshot"}""";
        await UploadAndFinalizeTdAsync(fileId, firstTd).ConfigureAwait(false);
        var (_, outputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Open, 0),
            [new Variant((byte)1)]).ConfigureAwait(false);
        uint handle = Convert.ToUInt32(outputs[0].Value);

        const string secondTd = """{"title":"Replacement"}""";
        await UploadAndFinalizeTdAsync(fileId, secondTd).ConfigureAwait(false);
        var (readStatus, readOutputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Read, 0),
            [new Variant(handle), new Variant(1024)]).ConfigureAwait(false);
        StatusCode.IsGood(readStatus).Should().BeTrue();
        Encoding.UTF8.GetString((byte[])readOutputs[0].Value).Should().Be(firstTd);
        var (closeStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Close, 0),
            [new Variant(handle)]).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue();
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(secondTd);
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(8)]
    [TestCase(16)]
    public async Task WoTFile_InvalidOpenMode_ReturnsBadInvalidArgument(byte mode)
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "InvalidModeTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        var (status, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Open, 0),
            [new Variant(mode)]).ConfigureAwait(false);
        status.Code.Should().Be(StatusCodes.BadInvalidArgument);
    }

    [TestCase(3)]
    [TestCase(7)]
    [TestCase(11)]
    [TestCase(15)]
    public async Task WoTFile_ReadWriteModes_RespectEraseAndAppend(byte mode)
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync(
            "ReadWriteTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """{"title":"ReadWrite"}""";
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        var (openStatus, outputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Open, 0),
            [new Variant(mode)]).ConfigureAwait(false);
        StatusCode.IsGood(openStatus).Should().BeTrue();
        uint handle = Convert.ToUInt32(outputs[0].Value);
        var (positionStatus, positionOutputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_GetPosition, 0),
            [new Variant(handle)]).ConfigureAwait(false);
        StatusCode.IsGood(positionStatus).Should().BeTrue();
        positionOutputs[0].Value.Should().Be(mode == 11 ? (ulong)Encoding.UTF8.GetByteCount(td) : 0ul);

        var (readStatus, readOutputs) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Read, 0),
            [new Variant(handle), new Variant(1024)]).ConfigureAwait(false);
        StatusCode.IsGood(readStatus).Should().BeTrue();
        Encoding.UTF8.GetString((byte[])readOutputs[0].Value).Should().Be(mode == 3 ? td : string.Empty);

        bool erased = mode == 7 || mode == 15;
        string suffix = erased ? td : " ";
        var (writeStatus, _) = await CallAsync(
            fileId, new NodeId(Methods.FileType_Write, 0),
            [new Variant(handle), new Variant(Encoding.UTF8.GetBytes(suffix))]).ConfigureAwait(false);
        StatusCode.IsGood(writeStatus).Should().BeTrue();
        var (updateStatus, _) = await CallAsync(
            fileId, WotConNodeId(FileCloseAndUpdateTypeMethodId),
            [new Variant(handle)]).ConfigureAwait(false);
        StatusCode.IsGood(updateStatus).Should().BeTrue();
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(erased ? td : td + " ");
    }

    [Test]
    public async Task WoTFile_RoundTrip_OpenWriteCloseAndUpdate_Succeeds()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync("RoundTripAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        // Open (mode=6 = write+erase, conventional for File API uploads).
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
    public async Task WoTFile_WriteExceedsMaxByteStringLength_ReturnsBadRequestTooLarge()
    {
        // The per-asset WoTFile advertises MaxByteStringLength = 64 KiB in CreateAssetFileNode
        // (sized for realistic TD JSON-LD payloads and below the OPC UA transport's default
        // MaxMessageSize so the cap is actually enforceable). A Write whose data would push
        // total length past that limit must be rejected with Bad_RequestTooLarge so clients
        // can trust the advertised cap (and the simulator is not DoS-able by an unauthenticated
        // Write loop).
        const int MaxByteStringLength = 64 * 1024;

        var (_, fileId) = await CreateAssetAndResolveFileAsync("WriteTooLargeAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        var (_, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);

        byte[] tooLarge = new byte[MaxByteStringLength + 1];

        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(tooLarge),
            }).ConfigureAwait(false);

        writeStatus.Code.Should().Be(StatusCodes.BadRequestTooLarge,
            "Write past MaxByteStringLength must be rejected with Bad_RequestTooLarge");
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

    /// <summary>
    /// Per-asset WoTFile address-space audit (regression). The per-asset
    /// <c>WoTAssetFileType</c> instance must expose the full OPC 10000-5 §10 FileType
    /// layout — every mandatory and SDK-optional Variable + every method — plus the
    /// WoT-Con <c>CloseAndUpdate</c> extension method. A previous bug forgot to set
    /// <c>ReferenceTypeId = HasComponent</c> on the per-asset file instance, so handler
    /// invocations still worked (the SDK routes by NodeId) but the address space showed
    /// an empty FileType node. Browse-based assertions like this one would have caught
    /// that regression; the handler-call tests didn't.
    /// </summary>
    [Test]
    public async Task WoTFile_AddressSpace_ExposesFullFileTypeLayoutPlusCloseAndUpdate()
    {
        var (_, fileId) = await CreateAssetAndResolveFileAsync("WoTFileAudit_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        // Properties on FileType are wired via HasProperty; methods via HasComponent.
        // Both descend from HierarchicalReferences \u2014 one browse with IncludeSubtypes=true
        // covers the whole audit, with the NodeClass on each reference telling us which
        // side we're looking at.
        var bd = new BrowseDescription
        {
            NodeId = fileId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = (uint)(NodeClass.Variable | NodeClass.Method),
            ResultMask = (uint)BrowseResultMask.All,
        };

        var resp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        var children = resp.Results[0].References;

        var variableNames = children
            .Where(r => r.NodeClass == NodeClass.Variable)
            .Select(r => r.BrowseName.Name)
            .ToHashSet(StringComparer.Ordinal);
        var methodChildren = children
            .Where(r => r.NodeClass == NodeClass.Method)
            .ToList();
        var methodNames = methodChildren
            .Select(r => r.BrowseName.Name)
            .ToHashSet(StringComparer.Ordinal);

        // OPC 10000-5 \u00a710 FileType Variables. Mandatory: Size, Writable, UserWritable,
        // OpenCount. Optional (SDK FileState.Create wires them by default and we light
        // them up in WotConNodeManager.CreateAssetNode): MimeType, MaxByteStringLength,
        // LastModifiedTime.
        variableNames.Should().BeEquivalentTo(
            new[]
            {
                BrowseNames.Size,
                BrowseNames.Writable,
                BrowseNames.UserWritable,
                BrowseNames.OpenCount,
                BrowseNames.MimeType,
                BrowseNames.MaxByteStringLength,
                BrowseNames.LastModifiedTime,
            },
            "per-asset WoTFile must surface all seven OPC 10000-5 \u00a710 FileType Variables");

        // OPC 10000-5 \u00a710 methods inherited from FileType, plus the WoT-Con extension.
        methodNames.Should().BeEquivalentTo(
            new[]
            {
                BrowseNames.Open,
                BrowseNames.Close,
                BrowseNames.Read,
                BrowseNames.Write,
                BrowseNames.GetPosition,
                BrowseNames.SetPosition,
                "CloseAndUpdate",
            },
            "per-asset WoTFile must expose the six inherited FileType methods plus the WoT-Con CloseAndUpdate extension (\u00a76.3.10)");

        // The six inherited methods live in NS=0; CloseAndUpdate is declared in the WoT-Con
        // companion namespace. Pinning this boundary catches a future regression where the
        // CloseAndUpdate node accidentally gets reparented onto NS=0 (or vice versa).
        var wotConNs = WotConNamespaceIndex;
        foreach (var inherited in new[]
        {
            BrowseNames.Open, BrowseNames.Close, BrowseNames.Read, BrowseNames.Write,
            BrowseNames.GetPosition, BrowseNames.SetPosition,
        })
        {
            methodChildren.Single(r => r.BrowseName.Name == inherited).BrowseName.NamespaceIndex
                .Should().Be(0, "{0} is inherited from the standard FileType in NS=0", inherited);
        }

        methodChildren.Single(r => r.BrowseName.Name == "CloseAndUpdate").BrowseName.NamespaceIndex
            .Should().Be(wotConNs, "CloseAndUpdate is declared by the WoT-Con companion spec");
    }
}
