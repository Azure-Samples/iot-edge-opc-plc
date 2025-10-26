namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Tests for interacting with OPC-UA Variable nodes.
/// </summary>
[TestFixture]
public class VariableTests : SimulatorTestsBase
{
    private NodeId _scalarStaticNode;

    // Set any cmd params needed for the plc server explicitly.
    public VariableTests() : base(["--ref"])
    {
    }

    [SetUp]
    public async Task SetUp()
    {
        _scalarStaticNode = await FindNodeAsync(ObjectsFolder, OpcPlc.Namespaces.OpcPlcReferenceTest, "ReferenceTest", "Scalar", "Scalar_Static").ConfigureAwait(false);
    }

    public static IEnumerable NodeWriteTestCases
    {
        get
        {
            yield return new TestCaseData(new[] { "Scalar_Static_Double" }, Fake.Random.Double());
            yield return new TestCaseData(new[] { "Scalar_Static_Arrays", "Scalar_Static_Arrays_String" }, Fake.Lorem.Words());
        }
    }

    [Test]
    [TestCaseSource(nameof(NodeWriteTestCases))]
    public async Task WriteValue_UpdatesValue(string[] pathParts, object newValue)
    {
        var nodeId = await FindNodeAsync(_scalarStaticNode, OpcPlc.Namespaces.OpcPlcReferenceTest, pathParts).ConfigureAwait(false);

        var results = await WriteValueAsync(nodeId, newValue).ConfigureAwait(false);

        results.Should().Be(StatusCodes.Good);

        (await Session.ReadValueAsync(nodeId).ConfigureAwait(false))
            .Value
            .Should().BeEquivalentTo(newValue);
    }
}
