namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
    public async Task TestDeterministicGuidNodes()
    {
        var deterministicGuidNode = FindNode(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Deterministic GUID");
        deterministicGuidNode.Should().NotBeNull();

        await Task.Delay(10_000);

        var guidNode1 = FindNode(deterministicGuidNode, Namespaces.OpcPlcApplications, "51b74e55-f2e3-4a4d-b79c-bf57c76ea67c");
        guidNode1.Should().NotBeNull();

        var guidNode2 = FindNode(deterministicGuidNode, Namespaces.OpcPlcApplications, "1313895e-c776-4201-b893-e514864c6692");
        guidNode2.Should().NotBeNull();
    }
}
