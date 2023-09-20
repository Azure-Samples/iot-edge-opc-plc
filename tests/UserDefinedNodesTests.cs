namespace OpcPlc.Tests;

using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using static System.TimeSpan;

/// <summary>
/// Tests for nodes configured via JSON file.
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
        var node1 = GetOpcPlcNodeId("99bdhbdhjbd9");
        node1.Should().NotBeNull();

        var node2 = GetOpcPlcNodeId("1023");
        node2.Should().NotBeNull();
    }
}
