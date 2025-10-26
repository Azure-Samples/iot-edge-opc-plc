namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

/// <summary>
/// Tests NodeIds of various IdTypes and ExpandedNodeIds.
/// </summary>
[TestFixture]
public class OpaqueAndNodeIdTests : SubscriptionTestsBase
{
    [Test]
    public async Task TestNodeIdNodes()
    {
        var specialFolder = await FindNodeAsync(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Special").ConfigureAwait(false);
        specialFolder.Should().NotBeNull();

        var stringNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdString").ConfigureAwait(false);
        stringNodeId.Should().NotBeNull();
        var stringNodeIdValue = await ReadValueAsync<Opc.Ua.NodeId>(stringNodeId).ConfigureAwait(false);
        stringNodeIdValue.Should().NotBeNull();
        stringNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.String);

        var numericNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdNumeric").ConfigureAwait(false);
        numericNodeId.Should().NotBeNull();
        var numericNodeIdValue = await ReadValueAsync<Opc.Ua.NodeId>(numericNodeId).ConfigureAwait(false);
        numericNodeIdValue.Should().NotBeNull();
        numericNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Numeric);

        var guidNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdGuid").ConfigureAwait(false);
        guidNodeId.Should().NotBeNull();
        var guidNodeIdValue = await ReadValueAsync<Opc.Ua.NodeId>(guidNodeId).ConfigureAwait(false);
        guidNodeIdValue.Should().NotBeNull();
        guidNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Guid);

        var opaqueNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdOpaque").ConfigureAwait(false);
        opaqueNodeId.Should().NotBeNull();
        var opaqueNodeIdValue = await ReadValueAsync<Opc.Ua.NodeId>(opaqueNodeId).ConfigureAwait(false);
        opaqueNodeIdValue.Should().NotBeNull();
        opaqueNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Opaque);

        var stringExpandedNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdString").ConfigureAwait(false);
        stringExpandedNodeId.Should().NotBeNull();
        var stringExpandedNodeIdValue = await ReadValueAsync<Opc.Ua.ExpandedNodeId>(stringExpandedNodeId).ConfigureAwait(false);
        stringExpandedNodeIdValue.Should().NotBeNull();
        stringExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.String);

        var numericExpandedNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdNumeric").ConfigureAwait(false);
        numericExpandedNodeId.Should().NotBeNull();
        var numericExpandedNodeIdValue = await ReadValueAsync<Opc.Ua.ExpandedNodeId>(numericExpandedNodeId).ConfigureAwait(false);
        numericExpandedNodeIdValue.Should().NotBeNull();
        numericExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Numeric);

        var guidExpandedNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdGuid").ConfigureAwait(false);
        guidExpandedNodeId.Should().NotBeNull();
        var guidExpandedNodeIdValue = await ReadValueAsync<Opc.Ua.ExpandedNodeId>(guidExpandedNodeId).ConfigureAwait(false);
        guidExpandedNodeIdValue.Should().NotBeNull();
        guidExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Guid);

        var opaqueExpandedNodeId = await FindNodeAsync(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdOpaque").ConfigureAwait(false);
        opaqueExpandedNodeId.Should().NotBeNull();
        var opaqueExpandedNodeIdValue = await ReadValueAsync<Opc.Ua.ExpandedNodeId>(opaqueExpandedNodeId).ConfigureAwait(false);
        opaqueExpandedNodeIdValue.Should().NotBeNull();
        opaqueExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Opaque);
    }
}
