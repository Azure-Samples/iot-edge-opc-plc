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
    private const uint DeleteAssetMethodInstanceId = 35;
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

    [Test]
    public async Task DeleteAsset_RemovesAssetAndOrganizesReference()
    {
        // Regression for the BadTooManyArguments bug: the NodeSet importer leaves
        // MethodState.InputArguments null on DeleteAsset, so before the fix any client
        // supplying the required AssetId would be rejected with BadTooManyArguments.
        var assetName = "DeleteAsset_" + Guid.NewGuid().ToString("N")[..8];

        var (createStatus, createOutputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue();
        var assetId = createOutputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        var (deleteStatus, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DeleteAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetId) }).ConfigureAwait(false);

        StatusCode.IsGood(deleteStatus).Should().BeTrue(
            "DeleteAsset with the AssetId from CreateAsset should succeed, got {0}", deleteStatus);

        // The asset must no longer be reachable via Organizes from WoTAssetConnectionManagement.
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
        results.Results[0].References.Should().NotContain(
            r => r.BrowseName.Name == assetName,
            "the deleted asset must not be browseable from WoTAssetConnectionManagement");
    }

    [Test]
    public async Task DeleteAsset_UnknownAssetId_ReturnsBadNotFound()
    {
        var bogus = new NodeId(Guid.NewGuid(), WotConNamespaceIndex);

        var (status, _) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(DeleteAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(bogus) }).ConfigureAwait(false);

        status.Code.Should().Be(StatusCodes.BadNotFound);
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

    [Test]
    public async Task CloseAndUpdate_TdWithProperties_MaterializesVariablesUnderAsset()
    {
        // Per OPC 10100-1 \u00a76.3.8 Table 14: each TD `properties[*]` entry becomes an OPC UA
        // Variable under the asset. Exercise the three primitive mappings plus an array.
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("PropAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Boiler",
              "properties": {
                "temperature": { "type": "number", "unit": "Cel", "description": "boiler outlet temperature" },
                "running":     { "type": "boolean", "readOnly": true },
                "setpoint":    { "type": "number", "writeOnly": true },
                "samples":     { "type": "array", "items": { "type": "integer" } }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var refs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);

        refs.Should().Contain(r => r.BrowseName.Name == "temperature");
        refs.Should().Contain(r => r.BrowseName.Name == "running");
        refs.Should().Contain(r => r.BrowseName.Name == "setpoint");
        refs.Should().Contain(r => r.BrowseName.Name == "samples");

        // Scalar number \u2192 Double, scalar value seeded.
        var temperatureId = ToNodeId(refs.Single(r => r.BrowseName.Name == "temperature").NodeId);
        var temperatureValue = await ReadDataValueAsync(temperatureId).ConfigureAwait(false);
        StatusCode.IsGood(temperatureValue.StatusCode).Should().BeTrue();
        temperatureValue.Value.Should().BeOfType<double>();

        // Array of integer \u2192 Int32 with ValueRank.OneDimension; seed yields int[].
        var samplesId = ToNodeId(refs.Single(r => r.BrowseName.Name == "samples").NodeId);
        var samplesValue = await ReadDataValueAsync(samplesId).ConfigureAwait(false);
        StatusCode.IsGood(samplesValue.StatusCode).Should().BeTrue();
        samplesValue.Value.Should().BeOfType<int[]>();
    }

    [Test]
    public async Task CloseAndUpdate_TdProperty_HonoursReadOnlyAndWriteOnlyAccessLevels()
    {
        // readOnly \u2192 AccessLevel.CurrentRead only; writeOnly \u2192 CurrentWrite only;
        // neither \u2192 CurrentReadOrWrite (default).
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("AccessLevelAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "AccessLevels",
              "properties": {
                "ro":  { "type": "number", "readOnly": true },
                "wo":  { "type": "number", "writeOnly": true },
                "rw":  { "type": "number" }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var refs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);

        byte roLevel = await ReadAccessLevelAsync(ToNodeId(refs.Single(r => r.BrowseName.Name == "ro").NodeId)).ConfigureAwait(false);
        byte woLevel = await ReadAccessLevelAsync(ToNodeId(refs.Single(r => r.BrowseName.Name == "wo").NodeId)).ConfigureAwait(false);
        byte rwLevel = await ReadAccessLevelAsync(ToNodeId(refs.Single(r => r.BrowseName.Name == "rw").NodeId)).ConfigureAwait(false);

        roLevel.Should().Be(AccessLevels.CurrentRead);
        woLevel.Should().Be(AccessLevels.CurrentWrite);
        rwLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
    }

    [Test]
    public async Task CloseAndUpdate_TdPropertyWithUnit_ExposesEngineeringUnitsChild()
    {
        // TD `unit` becomes a standard OPC UA EngineeringUnits PropertyState
        // (HasProperty, DataType=EUInformation) on the variable.
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("UnitAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "WithUnit",
              "properties": {
                "temperature": { "type": "number", "unit": "Cel" }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var refs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        var temperatureId = ToNodeId(refs.Single(r => r.BrowseName.Name == "temperature").NodeId);

        var propertyBrowse = new BrowseDescription
        {
            NodeId = temperatureId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All,
        };
        var browseResp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { propertyBrowse },
            CancellationToken.None).ConfigureAwait(false);
        var euRef = browseResp.Results[0].References
            .SingleOrDefault(r => r.BrowseName.Name == BrowseNames.EngineeringUnits);
        euRef.Should().NotBeNull("temperature must expose an EngineeringUnits property");

        var euValue = await ReadDataValueAsync(ToNodeId(euRef.NodeId)).ConfigureAwait(false);
        StatusCode.IsGood(euValue.StatusCode).Should().BeTrue();
        var eu = ExtensionObject.ToEncodeable(euValue.Value as ExtensionObject) as EUInformation;
        eu.Should().NotBeNull();
        eu.DisplayName.Text.Should().Be("Cel");
    }

    [Test]
    public async Task CloseAndUpdate_Reupload_ReplacesMaterializedProperties()
    {
        // A second CloseAndUpdate with a different TD must drop the old variables and
        // expose only the new ones \u2014 the asset's visible address space reflects the
        // most recently uploaded TD.
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("ReplaceAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string firstTd = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "First",
              "properties": { "alpha": { "type": "number" } }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, firstTd).ConfigureAwait(false);

        var firstRefs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        firstRefs.Should().Contain(r => r.BrowseName.Name == "alpha");

        const string secondTd = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Second",
              "properties": { "beta": { "type": "boolean" } }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, secondTd).ConfigureAwait(false);

        var secondRefs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        secondRefs.Should().Contain(r => r.BrowseName.Name == "beta");
        secondRefs.Should().NotContain(r => r.BrowseName.Name == "alpha",
            "re-upload must drop variables from the previous TD generation");
    }

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

    /// <summary>
    /// Browses the forward HasComponent Variable children of an asset \u2014 the materialized
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
    /// Opens the asset's WoTFile for read+write+erase, writes the payload, and returns
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
