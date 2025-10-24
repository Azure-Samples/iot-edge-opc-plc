namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

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
    public async Task TestUserDefinedNodes()
    {
        var myTelemetryNode = await FindNodeAsync(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry").ConfigureAwait(false);
        myTelemetryNode.Should().NotBeNull();

        var childNode = await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "Child").ConfigureAwait(false);
        childNode.Should().NotBeNull();

        (await FindNodeAsync(childNode, Namespaces.OpcPlcApplications, "9999").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(childNode, Namespaces.OpcPlcApplications, "Guid").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1023").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "aRMS").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1025").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1026").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1027").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1029").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1030").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1031").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1032").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1033").ConfigureAwait(false))
            .Should().NotBeNull();

        var arrayNodeId = await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1048").ConfigureAwait(false);
        arrayNodeId.Should().NotBeNull();
        (await Session.ReadValueAsync(arrayNodeId).ConfigureAwait(false)).Value.Should().BeEquivalentTo(new int[] { 1, 2, 3, 4, 5 });
    }
}
