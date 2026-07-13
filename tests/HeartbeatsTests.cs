namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using OpcPlc.PluginNodes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Tests for the optional client-managed heartbeat nodes.
/// </summary>
[TestFixture]
public class HeartbeatsTests : SimulatorTestsBase
{
    public HeartbeatsTests() : base(["--hb"])
    {
    }

    [TestCase("connectionKeepalive", 123u)]
    [TestCase("datasetWriteCounter", 456u)]
    public async Task HeartbeatVariable_IsWritableUInt32(string name, uint newValue)
    {
        var nodeId = await FindNodeAsync(
            ObjectsFolder,
            OpcPlc.Namespaces.OpcPlcApplications,
            "heartbeats",
            name).ConfigureAwait(false);

        var node = await Session.ReadNodeAsync(nodeId).ConfigureAwait(false);
        var variable = node.Should().BeOfType<VariableNode>().Subject;

        ExpandedNodeId.ToNodeId(variable.DataType, Session.NamespaceUris)
            .Should().Be(DataTypeIds.UInt32);
        variable.ValueRank.Should().Be(ValueRanks.Scalar);
        variable.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        variable.UserAccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);

        (await ReadValueAsync<uint>(nodeId).ConfigureAwait(false)).Should().Be(0u);

        var statusCode = await WriteValueAsync(nodeId, newValue).ConfigureAwait(false);

        statusCode.Should().Be(StatusCodes.Good);
        (await ReadValueAsync<uint>(nodeId).ConfigureAwait(false)).Should().Be(newValue);
    }

    [Test]
    public void HeartbeatVariables_AreIncludedInPublisherMetadata()
    {
        var plugin = PluginNodes
            .OfType<HeartbeatsPluginNodes>()
            .Should().ContainSingle()
            .Subject;

        plugin.IsEnabled.Should().BeTrue();
        plugin.Nodes.Select(node => node.NodeId).Should().BeEquivalentTo(
            "heartbeats_connectionKeepalive",
            "heartbeats_datasetWriteCounter");
    }
}

/// <summary>
/// Tests the default-disabled heartbeat behavior.
/// </summary>
[TestFixture]
public class HeartbeatsDisabledTests : SimulatorTestsBase
{
    [Test]
    public async Task Heartbeats_AreDisabledByDefault()
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = ObjectsFolder,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object,
            ResultMask = (uint)BrowseResultMask.BrowseName,
        };

        var response = await Session.BrowseAsync(
            requestHeader: null,
            view: null,
            requestedMaxReferencesPerNode: 0,
            nodesToBrowse: new BrowseDescriptionCollection { browseDescription },
            ct: CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        response.Results[0].References.Should().NotContain(reference =>
            reference.BrowseName.NamespaceIndex ==
                Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcApplications) &&
            reference.BrowseName.Name == "heartbeats");
    }
}
