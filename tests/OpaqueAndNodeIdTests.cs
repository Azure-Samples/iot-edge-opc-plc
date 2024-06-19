namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;

/// <summary>
/// Tests NodeIds of various IdTypes and ExpandedNodeIds.
/// </summary>
[TestFixture]
public class OpaqueAndNodeIdTests : SubscriptionTestsBase
{
    [Test]
    public void TestNodeIdNodes()
    {
        var specialFolder = FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Special");
        specialFolder.Should().NotBeNull();

        var stringNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdString");
        stringNodeId.Should().NotBeNull();
        var stringNodeIdValue = ReadValue<Opc.Ua.NodeId>(stringNodeId);
        stringNodeIdValue.Should().NotBeNull();
        stringNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.String);

        var numericNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdNumeric");
        numericNodeId.Should().NotBeNull();
        var numericNodeIdValue = ReadValue<Opc.Ua.NodeId>(numericNodeId);
        numericNodeIdValue.Should().NotBeNull();
        numericNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Numeric);

        var guidNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdGuid");
        guidNodeId.Should().NotBeNull();
        var guidNodeIdValue = ReadValue<Opc.Ua.NodeId>(guidNodeId);
        guidNodeIdValue.Should().NotBeNull();
        guidNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Guid);

        var opaqueNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticNodeIdOpaque");
        opaqueNodeId.Should().NotBeNull();
        var opaqueNodeIdValue = ReadValue<Opc.Ua.NodeId>(opaqueNodeId);
        opaqueNodeIdValue.Should().NotBeNull();
        opaqueNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Opaque);

        var stringExpandedNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdString");
        stringExpandedNodeId.Should().NotBeNull();
        var stringExpandedNodeIdValue = ReadValue<Opc.Ua.ExpandedNodeId>(stringExpandedNodeId);
        stringExpandedNodeIdValue.Should().NotBeNull();
        stringExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.String);

        var numericExpandedNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdNumeric");
        numericExpandedNodeId.Should().NotBeNull();
        var numericExpandedNodeIdValue = ReadValue<Opc.Ua.ExpandedNodeId>(numericExpandedNodeId);
        numericExpandedNodeIdValue.Should().NotBeNull();
        numericExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Numeric);

        var guidExpandedNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdGuid");
        guidExpandedNodeId.Should().NotBeNull();
        var guidExpandedNodeIdValue = ReadValue<Opc.Ua.ExpandedNodeId>(guidExpandedNodeId);
        guidExpandedNodeIdValue.Should().NotBeNull();
        guidExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Guid);

        var opaqueExpandedNodeId = FindNode(specialFolder, Namespaces.OpcPlcApplications, "ScalarStaticExpandedNodeIdOpaque");
        opaqueExpandedNodeId.Should().NotBeNull();
        var opaqueExpandedNodeIdValue = ReadValue<Opc.Ua.ExpandedNodeId>(opaqueExpandedNodeId);
        opaqueExpandedNodeIdValue.Should().NotBeNull();
        opaqueExpandedNodeIdValue.IdType.Should().Be(Opc.Ua.IdType.Opaque);
    }
}
