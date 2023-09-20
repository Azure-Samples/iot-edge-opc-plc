namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;

/// <summary>
/// Tests deterministic GUID nodes.
/// </summary>
[TestFixture]
public class GuidNodesTests : SubscriptionTestsBase
{
    // Set any cmd params needed for the plc server explicitly
    public GuidNodesTests() : base(new[] { "--gn=2" })
    {
    }

    [Test]
    public void TestDeterministicNodes()
    {
        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Deterministic GUID", "51b74e55-f2e3-4a4d-b79c-bf57c76ea67c")
        .Should().NotBeNull();

        FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Deterministic GUID", "1313895e-c776-4201-b893-e514864c6692")
        .Should().NotBeNull();
    }
}
