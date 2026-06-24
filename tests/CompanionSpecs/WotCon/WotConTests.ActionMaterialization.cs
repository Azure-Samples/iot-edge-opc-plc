namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for §6.3.9: a successful <c>CloseAndUpdate</c> materializes the TD's
/// <c>actions[*]</c> as OPC UA Methods under the asset. Covers argument synthesis from
/// the action's <c>input</c> / <c>output</c> JSON Schemas, invocation returning canned
/// outputs, and re-upload replacement semantics.
/// </summary>
public partial class WotConTests
{
    [Test]
    public async Task CloseAndUpdateTdWithActionsMaterializesMethodsUnderAsset()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("ActionAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Lamp",
              "actions": {
                "toggle": { "description": "Toggle the lamp" },
                "fade": {
                  "input":  { "type": "object", "properties": { "brightness": { "type": "integer" }, "duration": { "type": "number" } } },
                  "output": { "type": "object", "properties": { "success": { "type": "boolean" } } }
                }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var methodRefs = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        methodRefs.Should().Contain(r => r.BrowseName.Name == "toggle");
        methodRefs.Should().Contain(r => r.BrowseName.Name == "fade");
    }

    [Test]
    public async Task CloseAndUpdateTdActionExposesInputAndOutputArgumentsFromJsonSchemas()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("ActionArgsAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Lamp",
              "actions": {
                "fade": {
                  "input":  { "type": "object", "properties": { "brightness": { "type": "integer" }, "duration": { "type": "number" } } },
                  "output": { "type": "object", "properties": { "success": { "type": "boolean" } } }
                }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var methodRefs = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        var fadeMethodId = ToNodeId(methodRefs.Single(r => r.BrowseName.Name == "fade").NodeId);

        var inputArgs = await ReadArgumentArrayAsync(fadeMethodId, BrowseNames.InputArguments).ConfigureAwait(false);
        inputArgs.Should().HaveCount(2);
        inputArgs.Should().Contain(a => a.Name == "brightness" && a.DataType == DataTypeIds.Int32);
        inputArgs.Should().Contain(a => a.Name == "duration" && a.DataType == DataTypeIds.Double);

        var outputArgs = await ReadArgumentArrayAsync(fadeMethodId, BrowseNames.OutputArguments).ConfigureAwait(false);
        outputArgs.Should().ContainSingle();
        outputArgs[0].Name.Should().Be("success");
        outputArgs[0].DataType.Should().Be(DataTypeIds.Boolean);
    }

    [Test]
    public async Task CloseAndUpdateTdActionInvocationReturnsCannedOutputValues()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("InvokeActionAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Lamp",
              "actions": {
                "fade": {
                  "input":  { "type": "object", "properties": { "brightness": { "type": "integer" } } },
                  "output": { "type": "object", "properties": { "success": { "type": "boolean" }, "ticks": { "type": "integer" } } }
                }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        var methodRefs = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        var fadeMethodId = ToNodeId(methodRefs.Single(r => r.BrowseName.Name == "fade").NodeId);

        var (status, outputs) = await CallAsync(
            objectId: assetId,
            methodId: fadeMethodId,
            arguments: new VariantCollection { new Variant(75) }).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue("invocation should succeed, got {0}", status);
        outputs.Should().HaveCount(2, "two output arguments declared in the TD");
        outputs[0].Value.Should().BeOfType<bool>();
        outputs[1].Value.Should().BeOfType<int>();
    }

    [Test]
    public async Task CloseAndUpdateReuploadReplacesMaterializedActions()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("ReplaceActionAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string firstTd = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "First",
              "actions": { "alpha": { } }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, firstTd).ConfigureAwait(false);

        var firstMethodRefs = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        firstMethodRefs.Should().Contain(r => r.BrowseName.Name == "alpha");

        const string secondTd = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "Second",
              "actions": { "beta": { } }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, secondTd).ConfigureAwait(false);

        var secondMethodRefs = await BrowseAssetMethodChildrenAsync(assetId).ConfigureAwait(false);
        secondMethodRefs.Should().Contain(r => r.BrowseName.Name == "beta");
        secondMethodRefs.Should().NotContain(r => r.BrowseName.Name == "alpha",
            "re-upload must drop methods from the previous TD generation");
    }

    private async Task<ReferenceDescriptionCollection> BrowseAssetMethodChildrenAsync(NodeId assetId)
    {
        var bd = new BrowseDescription
        {
            NodeId = assetId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Method,
            ResultMask = (uint)BrowseResultMask.All,
        };
        var resp = await Session.BrowseAsync(
            null, null, 0,
            new BrowseDescriptionCollection { bd },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        return resp.Results[0].References;
    }

    private async Task<Argument[]> ReadArgumentArrayAsync(NodeId methodId, string argumentsBrowseName)
    {
        var browsePath = new BrowsePath
        {
            StartingNode = methodId,
            RelativePath = new RelativePath
            {
                Elements =
                {
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasProperty,
                        IsInverse = false,
                        IncludeSubtypes = true,
                        TargetName = new QualifiedName(argumentsBrowseName, 0),
                    },
                },
            },
        };

        var resp = await Session.TranslateBrowsePathsToNodeIdsAsync(
            null,
            new BrowsePathCollection { browsePath },
            CancellationToken.None).ConfigureAwait(false);
        resp.Results.Should().ContainSingle();
        var bp = resp.Results[0];
        StatusCode.IsGood(bp.StatusCode).Should().BeTrue("{0} property must resolve, got {1}", argumentsBrowseName, bp.StatusCode);
        bp.Targets.Should().ContainSingle();
        var argsNodeId = ToNodeId(bp.Targets[0].TargetId);

        var dv = await ReadDataValueAsync(argsNodeId).ConfigureAwait(false);
        StatusCode.IsGood(dv.StatusCode).Should().BeTrue();
        var extObjects = dv.Value as ExtensionObject[];
        extObjects.Should().NotBeNull("{0} should decode as ExtensionObject[]", argumentsBrowseName);
        return extObjects.Select(e => (Argument)e.Body).ToArray();
    }
}
