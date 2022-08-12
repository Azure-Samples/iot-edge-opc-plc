namespace OpcPlc.Tests;

using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using static System.TimeSpan;

/// <summary>
/// Tests for OPC-UA Monitoring for Data changes.
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
        var guidNode1 = GetOpcPlcNodeId("51b74e55-f2e3-4a4d-b79c-bf57c76ea67c");
        guidNode1.Should().NotBeNull();

        var guidNode2 = GetOpcPlcNodeId("1313895e-c776-4201-b893-e514864c6692");
        guidNode2.Should().NotBeNull();
    }
}
