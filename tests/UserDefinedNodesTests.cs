namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;

/// <summary>
/// Tests the nodes configured via nodesfile.json.
/// </summary>
[TestFixture]
public class UserDefinedNodesTests : SubscriptionTestsBase
{
    // Set any cmd params needed for the plc server explicitly
    public UserDefinedNodesTests() : base(["--nodesfile=nodesfile.json"])
    {
    }

    [Test]
    public void TestUserDefinedNodes()
    {
        var myTelemetryNode = FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry");
        myTelemetryNode.Should().NotBeNull();

        var childNode = FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "Child");
        childNode.Should().NotBeNull();

        FindNode(childNode, Namespaces.OpcPlcApplications, "9999")
        .Should().NotBeNull();

        FindNode(childNode, Namespaces.OpcPlcApplications, "Guid")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1023")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "aRMS")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1025")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1026")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1027")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1029")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1030")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1031")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1032")
        .Should().NotBeNull();

        FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1033")
        .Should().NotBeNull();

        var arrayNodeId = FindNode(myTelemetryNode, Namespaces.OpcPlcApplications, "1048");
        arrayNodeId.Should().NotBeNull();
        Session.ReadValue(arrayNodeId).Value.Should().BeEquivalentTo(new int[] { 1, 2, 3, 4, 5 });
    }
}
