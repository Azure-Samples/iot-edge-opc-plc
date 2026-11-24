namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
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
    public async Task CloseAndUpdate_MaterializationFailure_LeavesAssetEmptyAndRetryRebuilds()
    {
        string assetName = "FailedTd_" + Guid.NewGuid().ToString("N")[..8];
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync(assetName).ConfigureAwait(false);
        const string tdA = """{"title":"A","base":"http://a.invalid/","properties":{"temperature":{"type":"number"}},"actions":{"reset":{}}}""";
        const string tdB = """{"title":"B","properties":{"pressure":{"type":"number"}},"actions":{"stop":{}}}""";
        await UploadAndFinalizeTdAsync(fileId, tdA).ConfigureAwait(false);

        OpcPlc.CompanionSpecs.WotCon.WotConNodeManager.MaterializationFaultInjector = name =>
        {
            if (name == assetName)
            {
                throw new InvalidOperationException("injected materialization failure");
            }
        };
        try
        {
            uint handle = await OpenWriteAsync(fileId, Encoding.UTF8.GetBytes(tdB)).ConfigureAwait(false);
            var (status, _) = await CallAsync(
                fileId, WotConNodeId(FileCloseAndUpdateTypeMethodId),
                [new Variant(handle)]).ConfigureAwait(false);
            status.Should().Be(StatusCodes.BadInternalError);
        }
        finally
        {
            OpcPlc.CompanionSpecs.WotCon.WotConNodeManager.MaterializationFaultInjector = null;
        }

        (await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false))
            .Select(r => r.BrowseName.Name).Should().NotContain(["temperature", "pressure"]);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().BeEmpty();
        (await ReadFilePropertyAsync<ulong>(fileId, BrowseNames.Size).ConfigureAwait(false)).Should().Be(0UL);

        await UploadAndFinalizeTdAsync(fileId, tdA).ConfigureAwait(false);
        (await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false))
            .Select(r => r.BrowseName.Name).Should().Contain("temperature").And.NotContain("pressure");
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(tdA);
    }
    [TestCase(false)]
    [TestCase(true)]
    public async Task CloseAndUpdate_UnchangedTd_PreservesNodeIdsAndValues(bool reformat)
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync(
            "UnchangedTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """
            {
              "title": "Unchanged",
              "base": "http://unchanged.invalid/",
              "properties": { "setpoint": { "type": "number", "observable": false } },
              "actions": { "reset": {} }
            }
            """;
        const string reformattedTd = """
            {
              "actions": { "reset": {} },
              "properties": { "setpoint": { "observable": false, "type": "number" } },
              "base": "http://unchanged.invalid/",
              "title": "Unchanged"
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        var properties = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        var propertyId = ToNodeId(properties.Single(r => r.BrowseName.Name == "setpoint").NodeId);
        var actions = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        var actionId = ToNodeId(actions.Single(r => r.BrowseName.Name == "reset").NodeId);
        var endpointId = await ResolveChildByBrowseNameAsync(
            assetId, "AssetEndpoint", isProperty: true).ConfigureAwait(false);
        var writeStatus = await WriteValueAsync(propertyId, 42.5).ConfigureAwait(false);
        StatusCode.IsGood(writeStatus).Should().BeTrue();

        string uploadedTd = reformat ? reformattedTd : td;
        await UploadAndFinalizeTdAsync(fileId, uploadedTd).ConfigureAwait(false);

        var updatedProperties = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        ToNodeId(updatedProperties.Single(r => r.BrowseName.Name == "setpoint").NodeId).Should().Be(propertyId);
        var updatedActions = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        ToNodeId(updatedActions.Single(r => r.BrowseName.Name == "reset").NodeId).Should().Be(actionId);
        (await ResolveChildByBrowseNameAsync(assetId, "AssetEndpoint", isProperty: true).ConfigureAwait(false))
            .Should().Be(endpointId);
        var value = await ReadDataValueAsync(propertyId).ConfigureAwait(false);
        StatusCode.IsGood(value.StatusCode).Should().BeTrue();
        value.WrappedValue.AsBoxedObject(Variant.BoxingBehavior.Legacy).Should().Be(42.5, "unchanged uploads must not reset an existing variable");
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(uploadedTd);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CloseAndUpdate_ChangedForms_ReplacesNodes(bool reorder)
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync(
            "ChangedFormsTd_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        const string td = """
            {
              "title": "Forms",
              "properties": {
                "setpoint": { "type": "number", "forms": [{"href":"first"},{"href":"second"}] }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);
        var properties = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        var previousId = ToNodeId(properties.Single(r => r.BrowseName.Name == "setpoint").NodeId);
        string updatedTd = reorder
            ? td.Replace("""[{"href":"first"},{"href":"second"}]""", """[{"href":"second"},{"href":"first"}]""")
            : td.Replace("second", "changed");

        await UploadAndFinalizeTdAsync(fileId, updatedTd).ConfigureAwait(false);

        var updatedProperties = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        ToNodeId(updatedProperties.Single(r => r.BrowseName.Name == "setpoint").NodeId).Should().NotBe(previousId);
        var previousValue = await ReadDataValueAsync(previousId).ConfigureAwait(false);
        previousValue.StatusCode.Should().Be(StatusCodes.BadNodeIdUnknown);
        (await ReadStoredTdAsync(fileId).ConfigureAwait(false)).Should().Be(updatedTd);
    }

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
            arguments: new List<Variant> { new Variant(handle) }).ConfigureAwait(false);

        closeStatus.Should().Be(StatusCodes.BadDecodingError,
            "malformed JSON must be rejected with Bad_DecodingError");
    }

    [Test]
    public async Task CloseAndUpdate_MissingTitle_ReturnsBadDecodingError()
    {
        // OPC 10100-1 §6.3.10.2 lists Bad_DecodingError for a TD that cannot be parsed.
        // The Thing Description spec marks `title` as mandatory, so a payload that omits
        // it fails TD parsing and must surface as Bad_DecodingError (not Bad_InvalidArgument).
        var (_, fileId) = await CreateAssetAndResolveFileAsync("MissingTitleAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        byte[] payload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1""}");
        uint handle = await OpenWriteAsync(fileId, payload).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant(handle) }).ConfigureAwait(false);

        closeStatus.Should().Be(StatusCodes.BadDecodingError,
            "a TD without a non-empty 'title' must be rejected with Bad_DecodingError");
    }

    [Test]
    public async Task CloseAndUpdate_UnknownHandle_ReturnsBadInvalidState()
    {
        // OPC 10100-1 §6.3.10.2 lists Bad_InvalidState for "the file was not opened for
        // writing". An unknown / never-opened FileHandle is the simplest case of that.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("UnknownHandleAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant((uint)9999) }).ConfigureAwait(false);

        closeStatus.Should().Be(StatusCodes.BadInvalidState,
            "CloseAndUpdate against an unknown FileHandle must be rejected with Bad_InvalidState");
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
            arguments: new List<Variant> { new Variant(firstHandle) }).ConfigureAwait(false);
        StatusCode.IsGood(firstStatus).Should().BeTrue("first CloseAndUpdate must succeed, got {0}", firstStatus);

        byte[] secondPayload = Encoding.UTF8.GetBytes(@"{""@context"":""https://www.w3.org/2022/wot/td/v1.1"",""title"":""SecondUpload""}");
        uint secondHandle = await OpenWriteAsync(fileId, secondPayload).ConfigureAwait(false);
        var (secondStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant(secondHandle) }).ConfigureAwait(false);
        StatusCode.IsGood(secondStatus).Should().BeTrue("re-upload CloseAndUpdate must succeed, got {0}", secondStatus);
    }

    [Test]
    public async Task CloseAndUpdate_UnknownBinding_ReturnsBadNotSupported()
    {
        // OPC 10100-1 §6.3.1: a TD whose @context references a W3C-registered binding
        // template URI that is not advertised on SupportedWoTBindings must be rejected
        // with Bad_NotSupported. URIs we do not recognise as bindings (W3C TD base
        // context, semantic vocabularies) are ignored — see the existing CloseAndUpdate_*
        // tests that succeed with only the W3C TD context.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("UnknownBindingAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"@context\":[\"https://www.w3.org/2022/wot/td/v1.1\",\"https://www.w3.org/2019/wot/modbus\"],\"title\":\"UnknownBindingTd\"}");
        uint handle = await OpenWriteAsync(fileId, payload).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant(handle) }).ConfigureAwait(false);

        closeStatus.Should().Be(StatusCodes.BadNotSupported,
            "TDs that declare a binding outside SupportedWoTBindings must be rejected with Bad_NotSupported");
    }

    [Test]
    public async Task CloseAndUpdate_SimulatorBinding_Accepted()
    {
        // The OPC PLC simulator binding is the one we advertise on SupportedWoTBindings,
        // so a TD whose @context array includes it (alongside the W3C TD base) must be
        // accepted with Good.
        var (_, fileId) = await CreateAssetAndResolveFileAsync("SimBindingAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"@context\":[\"https://www.w3.org/2022/wot/td/v1.1\",\"https://opcfoundation.org/OpcPlc/simulator\"],\"title\":\"SimBindingTd\"}");
        uint handle = await OpenWriteAsync(fileId, payload).ConfigureAwait(false);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new List<Variant> { new Variant(handle) }).ConfigureAwait(false);

        StatusCode.IsGood(closeStatus).Should().BeTrue(
            "TDs that opt into the simulator binding must round-trip CloseAndUpdate, got {0}", closeStatus);
    }
}
