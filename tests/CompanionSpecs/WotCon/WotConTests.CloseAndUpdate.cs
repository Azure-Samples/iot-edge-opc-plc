namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Tests for the WoT-Con extension method <c>CloseAndUpdate</c> (§6.3.2 + §6.3.8) that
/// finalize a Thing-Description upload: parsing failures, semantic-validation
/// failures, and re-upload semantics on the parsed-TD scaffolding. Materialization
/// behaviour (Variables / Methods produced from the parsed TD) lives in the
/// per-feature partials.
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task CloseAndUpdate_MalformedJsonPayload_ReturnsBadDecodingError()
    {
        // Per OPC 10100-1 §6.3.2/§6.3.8 the upload must be a JSON-LD Thing Description.
        // A payload that is not valid JSON must be rejected with Bad_DecodingError so the
        // client can distinguish a transport / encoding bug from a semantic TD bug.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("MalformedJsonAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        uint handle = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes("{ not valid json")).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);

        closeStatus.Code.Should().Be(StatusCodes.BadDecodingError,
            "malformed JSON must be rejected with Bad_DecodingError");
    }

    [Test]
    public async Task CloseAndUpdate_MissingTitle_ReturnsBadInvalidArgument()
    {
        // The Thing Description spec marks `title` as mandatory. A well-formed JSON
        // payload that omits `title` is a semantic failure (Bad_InvalidArgument), not
        // a decoding failure.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("MissingTitleAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        byte[] payload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1""}");
        uint handle = await OpenWriteAsync(fileId, payload).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);

        closeStatus.Code.Should().Be(StatusCodes.BadInvalidArgument,
            "a TD without a non-empty 'title' must be rejected with Bad_InvalidArgument");
    }

    [Test]
    public async Task CloseAndUpdate_Reupload_OverwritesPreviousThingDescription()
    {
        // Per plan item 1 step 4: "a re-upload replaces it." Two successive successful
        // CloseAndUpdate calls against the same asset must both return Good.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("ReuploadAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        byte[] firstPayload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""FirstUpload""}");
        uint firstHandle = await OpenWriteAsync(fileId, firstPayload).ConfigureAwait(false);
        var (firstStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(firstHandle) }).ConfigureAwait(false);
        StatusCode.IsGood(firstStatus).Should().BeTrue("first CloseAndUpdate must succeed, got {0}", firstStatus);

        byte[] secondPayload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""SecondUpload""}");
        uint secondHandle = await OpenWriteAsync(fileId, secondPayload).ConfigureAwait(false);
        var (secondStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(secondHandle) }).ConfigureAwait(false);
        StatusCode.IsGood(secondStatus).Should().BeTrue("re-upload CloseAndUpdate must succeed, got {0}", secondStatus);
    }
}
