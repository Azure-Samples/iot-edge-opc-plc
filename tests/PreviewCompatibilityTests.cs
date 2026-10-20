namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class PreviewCompatibilityTests : SimulatorTestsBase
{
    public PreviewCompatibilityTests() : base(["--alm", "--ses"])
    {
    }

    [Test]
    public void SampleNamespaces_PreserveConnectorBrowsePathIndexes()
    {
        Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI).Should().Be(2);
        Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcBoiler).Should().Be(4);
        Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcReferenceTest).Should().Be(8);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task BoilerSwitch_AcceptsAdvertisedBooleanArgument(bool on)
    {
        ushort ns = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcBoiler);
        var response = await Session.CallAsync(null,
        [
            new CallMethodRequest
            {
                ObjectId = new NodeId(5019, ns),
                MethodId = new NodeId(7019, ns),
                InputArguments = [on]
            }
        ], CancellationToken.None).ConfigureAwait(false);

        response.Results[0].StatusCode.Should().Be(StatusCodes.Good);
        (await ReadValueAsync<bool>(new NodeId(
            BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, ns))
            .ConfigureAwait(false)).Should().Be(on);
    }

    [Test]
    public async Task BoilerInstance_PreservesTypeDefinition()
    {
        ushort ns = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcBoiler);
        var response = await Session.BrowseAsync(null, null, 0,
        [
            new BrowseDescription
            {
                NodeId = new NodeId(5017, ns),
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition,
                ResultMask = (uint)BrowseResultMask.All
            }
        ], CancellationToken.None).ConfigureAwait(false);

        response.Results[0].StatusCode.Should().Be(StatusCodes.Good);
        response.Results[0].References.ToArray().Should().ContainSingle()
            .Which.NodeId.Should().Be(new ExpandedNodeId(1000, ns));

        var hierarchy = await Session.BrowseAsync(null, null, 0,
        [
            new BrowseDescription
            {
                NodeId = new NodeId(1000, ns),
                BrowseDirection = BrowseDirection.Inverse,
                ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                ResultMask = (uint)BrowseResultMask.All
            }
        ], CancellationToken.None).ConfigureAwait(false);
        ushort di = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI);
        hierarchy.Results[0].References.ToArray().Should().ContainSingle()
            .Which.NodeId.Should().Be(new ExpandedNodeId(1002, di));
        NodeId boiler = await FindNodeAsync(new NodeId(5, ns),
            OpcPlc.Namespaces.OpcPlcBoiler, "Boiler &#2").ConfigureAwait(false);
        boiler.Should().Be(new NodeId(5017, ns));
        var children = await Session.BrowseAsync(null, null, 0,
        [
            new BrowseDescription
            {
                NodeId = new NodeId(5, ns),
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                ResultMask = (uint)BrowseResultMask.All
            }
        ], CancellationToken.None).ConfigureAwait(false);
        ReferenceDescription discovered = children.Results[0].References.ToArray()
            .Single(reference => reference.NodeId == new ExpandedNodeId(5017, ns));
        discovered.DisplayName.Text.Should().Be("Boiler #2");
        discovered.TypeDefinition.Should().Be(new ExpandedNodeId(1000, ns));
    }

    [Test]
    public async Task BoilerRelativePaths_ResolveAndPreserveWriteStatuses()
    {
        ushort ns = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcBoiler);
        ushort di = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI);
        string root = $"/{ns}:Boiler &#2/{di}:ParameterSet";
        var response = await Session.TranslateBrowsePathsToNodeIdsAsync(null,
        [
            Path(new NodeId(5, ns), root + $"/{ns}:TargetTemperature"),
            Path(new NodeId(5, ns), root + $"/{ns}:DoesNotExist"),
            Path(new NodeId(5, ns), root + $"/{ns}:BaseTemperature"),
            Path(new NodeId(5, ns), root)
        ], CancellationToken.None).ConfigureAwait(false);

        response.Results[0].StatusCode.Should().Be(StatusCodes.Good);
        response.Results[0].Targets[0].TargetId.Should().Be(new ExpandedNodeId(6217, ns));
        response.Results[1].StatusCode.Should().Be(StatusCodes.BadNoMatch);
        response.Results[2].StatusCode.Should().Be(StatusCodes.Good);
        response.Results[3].StatusCode.Should().Be(StatusCodes.Good);
        NodeId target = ExpandedNodeId.ToNodeId(response.Results[0].Targets[0].TargetId, Session.NamespaceUris);
        float original = await ReadValueAsync<float>(target).ConfigureAwait(false);
        try
        {
            var writes = await Session.WriteAsync(null,
            [
                Write(target, 42.5f),
                Write(ExpandedNodeId.ToNodeId(response.Results[2].Targets[0].TargetId, Session.NamespaceUris),
                    "not-a-number"),
                Write(ExpandedNodeId.ToNodeId(response.Results[3].Targets[0].TargetId, Session.NamespaceUris), 42.5f)
            ], CancellationToken.None).ConfigureAwait(false);

            writes.Results[0].Should().Be(StatusCodes.Good);
            writes.Results[1].Should().Be(StatusCodes.BadTypeMismatch);
            writes.Results[2].Should().Be(StatusCodes.BadAttributeIdInvalid);
            (await ReadValueAsync<float>(target).ConfigureAwait(false)).Should().Be(42.5f);
        }
        finally
        {
            await WriteValueAsync(target, original).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task UnknownReferenceBrowsePath_ReturnsBadNoMatch()
    {
        ushort ns = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcReferenceTest);
        var response = await Session.TranslateBrowsePathsToNodeIdsAsync(null,
        [
            Path(new NodeId("Scalar", ns), $"/{ns}:Scalar_Static/{ns}:doesNotExist")
        ], CancellationToken.None).ConfigureAwait(false);
        response.Results[0].StatusCode.Should().Be(StatusCodes.BadNoMatch);
    }

    private BrowsePath Path(NodeId start, string path) => new()
    {
        StartingNode = start,
        RelativePath = RelativePath.Parse(path, Session.TypeTree)
    };

    private static WriteValue Write(NodeId nodeId, Variant value) => new()
    {
        NodeId = nodeId,
        AttributeId = Attributes.Value,
        Value = new DataValue(value)
    };
}
