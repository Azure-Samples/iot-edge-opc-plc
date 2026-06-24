namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for §6.3.8 Table 14: a successful <c>CloseAndUpdate</c> materializes the TD's
/// <c>properties[*]</c> as OPC UA Variables under the asset, honouring
/// <c>readOnly</c> / <c>writeOnly</c> (AccessLevel), <c>unit</c> (EngineeringUnits
/// PropertyState), and array element types. Re-uploads replace the previous
/// generation.
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task CloseAndUpdate_TdWithProperties_MaterializesVariablesUnderAsset()
    {
        // Per OPC 10100-1 §6.3.8 Table 14: each TD `properties[*]` entry becomes an OPC UA
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

        // Scalar number → Double, scalar value seeded.
        var temperatureId = ToNodeId(refs.Single(r => r.BrowseName.Name == "temperature").NodeId);
        var temperatureValue = await ReadDataValueAsync(temperatureId).ConfigureAwait(false);
        StatusCode.IsGood(temperatureValue.StatusCode).Should().BeTrue();
        temperatureValue.Value.Should().BeOfType<double>();

        // Array of integer → Int32 with ValueRank.OneDimension; seed yields int[].
        var samplesId = ToNodeId(refs.Single(r => r.BrowseName.Name == "samples").NodeId);
        var samplesValue = await ReadDataValueAsync(samplesId).ConfigureAwait(false);
        StatusCode.IsGood(samplesValue.StatusCode).Should().BeTrue();
        samplesValue.Value.Should().BeOfType<int[]>();
    }

    [Test]
    public async Task CloseAndUpdate_TdProperty_HonoursReadOnlyAndWriteOnlyAccessLevels()
    {
        // readOnly → AccessLevel.CurrentRead only; writeOnly → CurrentWrite only;
        // neither → CurrentReadOrWrite (default).
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
        // expose only the new ones — the asset's visible address space reflects the
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
}
