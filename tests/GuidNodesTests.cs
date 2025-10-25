namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

/// <summary>
/// Tests deterministic GUID nodes.
/// </summary>
[TestFixture]
public class GuidNodesTests : SubscriptionTestsBase
{
    // Set any cmd params needed for the plc server explicitly
    public GuidNodesTests() : base(["--gn=2"])
    {
    }

    [Test]
    public async Task TestDeterministicGuidNodes()
    {
        var deterministicGuidNode = await FindNodeAsync(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "Telemetry", "Deterministic GUID").ConfigureAwait(false);
        deterministicGuidNode.Should().NotBeNull();

        var guidNode1 = await FindNodeAsync(deterministicGuidNode, Namespaces.OpcPlcApplications, "51b74e55-f2e3-4a4d-b79c-bf57c76ea67c").ConfigureAwait(false);
        guidNode1.Should().NotBeNull();

        var guidNode2 = await FindNodeAsync(deterministicGuidNode, Namespaces.OpcPlcApplications, "1313895e-c776-4201-b893-e514864c6692").ConfigureAwait(false);
        guidNode2.Should().NotBeNull();
    }
}
