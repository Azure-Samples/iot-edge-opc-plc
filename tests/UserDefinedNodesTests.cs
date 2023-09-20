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
    public UserDefinedNodesTests() : base(new[] { "--nodesfile=nodesfile.json" })
    {
    }

    [Test]
    public void TestUserDefinedNodes()
    {
        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "Child", "9999")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1023")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1025")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1026")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1027")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1029")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1030")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1031")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1032")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry", "1033")
        .Should().NotBeNull();
    }
}
