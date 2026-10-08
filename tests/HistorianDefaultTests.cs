namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Threading.Tasks;

public class HistorianDefaultTests : SimulatorTestsBase
{
    [TestCase("Historian")]
    [TestCase("HistorianInt32")]
    public async Task HistorianNodesAreAbsentWithoutFlag(string nodeName)
    {
        DataValue result = await ReadDataValueAsync(GetOpcPlcNodeId(nodeName)).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadNodeIdUnknown);
    }

    [Test]
    public async Task ExistingTelemetryRemainsAvailable()
    {
        DataValue result = await ReadDataValueAsync(GetOpcPlcNodeId("FastUInt1")).ConfigureAwait(false);

        StatusCode.IsGood(result.StatusCode).Should().BeTrue();
        result.Value.Should().BeOfType<uint>();
    }
}