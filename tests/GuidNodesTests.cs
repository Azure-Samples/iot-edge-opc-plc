namespace OpcPlc.Tests
{
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
            var guidNode1 = GetOpcPlcNodeId("65e451f1-56f1-ce84-a44f-6addf176beaf");
            guidNode1.Should().NotBeNull();

            var guidNode2 = GetOpcPlcNodeId("9513141f-c697-8a1f-a236-e14864e4bf7e");
            guidNode2.Should().NotBeNull();
        }
    }
}