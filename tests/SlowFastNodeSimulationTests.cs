namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using OpcPlc.PluginNodes;
using System;
using System.Linq;

[TestFixture]
public class SlowFastNodeSimulationTests
{
    [TestCase(0u, 0u, 10u, 1u, 1u)]
    [TestCase(9u, 2u, 10u, 2u, 2u)]
    [TestCase(1u, 2u, 10u, 1u, 2u)]
    [TestCase(10u, 2u, 10u, 1u, 2u)]
    public void UInt_SequentialPreservesRange(uint initial, uint min, uint max, uint step, uint expected)
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UInt, new Variant(initial), step, min, max);

        fixture.Update(node, NodeType.UInt);

        node.Value.GetUInt32().Should().Be(expected);
    }

    [TestCase(0.0, 0.0, 10.0, 0.5, 0.5)]
    [TestCase(9.5, 2.0, 10.0, 1.0, 2.0)]
    [TestCase(1.0, 2.0, 10.0, 1.0, 2.0)]
    [TestCase(0.0, -10.0, 0.0, 0.5, -0.5)]
    [TestCase(-9.5, -10.0, -2.0, 1.0, -2.0)]
    [TestCase(-1.0, -10.0, -2.0, 1.0, -2.0)]
    public void Double_SequentialPreservesRange(double initial, double min, double max, double step, double expected)
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.Double, new Variant(initial), step, min, max);

        fixture.Update(node, NodeType.Double);

        node.Value.GetDouble().Should().Be(expected);
    }

    [Test]
    public void Boolean_AlternatesAndRecoversFromNull()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.Bool, new Variant(true));

        fixture.Update(node, NodeType.Bool);
        node.Value.GetBoolean().Should().BeFalse();
        fixture.Update(node, NodeType.Bool);
        node.Value.GetBoolean().Should().BeTrue();
        node.Value = Variant.Null;
        fixture.Update(node, NodeType.Bool);

        node.Value.GetBoolean().Should().BeTrue();
    }

    [Test]
    public void UIntArray_IncrementsWithoutMutatingPreviousSample()
    {
        var fixture = new SimulationFixture();
        uint[] initial = [0, 41, uint.MaxValue];
        var node = CreateNode(NodeType.UIntArray, Variant.From(initial.ToArrayOf()));
        Variant previousSample = node.Value;

        fixture.Update(node, NodeType.UIntArray);

        node.Value.GetUInt32Array().ToArray().Should().Equal(1u, 42u, 0u);
        previousSample.GetUInt32Array().ToArray().Should().Equal(0u, 41u, uint.MaxValue);
        initial.Should().Equal(0u, 41u, uint.MaxValue);
        node.Value.TypeInfo.BuiltInType.Should().Be(BuiltInType.UInt32);
        node.Value.TypeInfo.ValueRank.Should().Be(ValueRanks.OneDimension);
    }

    [Test]
    public void UIntArray_NullRestartsWith32Zeros()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UIntArray, Variant.Null);

        fixture.Update(node, NodeType.UIntArray);
        node.Value.GetUInt32Array().ToArray().Should().Equal(new uint[32]);
        fixture.Update(node, NodeType.UIntArray);

        node.Value.GetUInt32Array().ToArray().Should().Equal(Enumerable.Repeat(1u, 32));
    }

    [Test]
    public void UIntArray_EmptyRemainsEmpty()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UIntArray, Variant.From(Array.Empty<uint>().ToArrayOf()));

        fixture.Update(node, NodeType.UIntArray);

        node.Value.IsNull.Should().BeFalse();
        node.Value.GetUInt32Array().ToArray().Should().BeEmpty();
    }

    [TestCase(NodeType.UInt)]
    [TestCase(NodeType.Double)]
    [TestCase(NodeType.Bool)]
    [TestCase(NodeType.UIntArray)]
    public void BadNodes_PreserveStatusSequenceAndNullRecovery(NodeType type)
    {
        var fixture = new SimulationFixture();
        Variant initial = type switch
        {
            NodeType.UInt => new Variant(0u),
            NodeType.Double => new Variant(0.0),
            NodeType.Bool => new Variant(true),
            _ => Variant.From(new uint[32].ToArrayOf())
        };
        var node = CreateNode(type, initial);
        StatusCode[] expectedStatuses =
        [
            StatusCodes.Good, StatusCodes.Good, StatusCodes.Good, StatusCodes.UncertainLastUsableValue,
            StatusCodes.Good, StatusCodes.Good, StatusCodes.Good, StatusCodes.UncertainLastUsableValue,
            StatusCodes.BadDataLost, StatusCodes.BadNoCommunication, StatusCodes.Good
        ];

        for (int cycle = 0; cycle < expectedStatuses.Length; cycle++)
        {
            fixture.Simulation.UpdateNodes(null, [node], type, fixture.Limit, true);
            node.StatusCode.Should().Be(expectedStatuses[cycle], "cycle {0}", cycle);
            node.Value.IsNull.Should().Be(cycle == 9, "only BadNoCommunication clears the value");
            node.Timestamp.Should().Be((DateTimeUtc)fixture.Now);
        }

        Variant expectedRecovery = type switch
        {
            NodeType.UInt => new Variant(1u),
            NodeType.Double => new Variant(1.0),
            NodeType.Bool => new Variant(true),
            _ => Variant.From(new uint[32].ToArrayOf())
        };
        node.Value.Should().Be(expectedRecovery);
    }

    [Test]
    public void Limit_StopsBothNodeGroupsAndResumesIndefinitely()
    {
        var fixture = new SimulationFixture();
        var good = CreateNode(NodeType.UInt, new Variant(0u));
        var bad = CreateNode(NodeType.UInt, new Variant(0u));
        fixture.Limit.Value = new Variant(2);

        for (int cycle = 0; cycle < 5; cycle++)
        {
            fixture.Simulation.UpdateNodes([good], [bad], NodeType.UInt, fixture.Limit, true);
        }

        fixture.Limit.Value.GetInt32().Should().Be(0);
        good.Value.GetUInt32().Should().Be(2u);
        bad.Value.GetUInt32().Should().Be(2u);
        fixture.Limit.Value = new Variant(-1);
        fixture.Simulation.UpdateNodes([good], [bad], NodeType.UInt, fixture.Limit, true);
        fixture.Simulation.UpdateNodes([good], [bad], NodeType.UInt, fixture.Limit, true);

        good.Value.GetUInt32().Should().Be(4u);
        bad.Value.GetUInt32().Should().Be(4u);
        bad.StatusCode.Should().Be((StatusCode)StatusCodes.UncertainLastUsableValue);
        fixture.Limit.Value.GetInt32().Should().Be(-1);
    }

    [Test]
    public void Pause_PreservesValuesAndStatusCycleButConsumesPositiveLimit()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UInt, new Variant(10u));
        fixture.Limit.Value = new Variant(5);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            fixture.Simulation.UpdateNodes(null, [node], NodeType.UInt, fixture.Limit, false);
        }

        node.Value.GetUInt32().Should().Be(10u);
        fixture.Limit.Value.GetInt32().Should().Be(2);
        fixture.Simulation.UpdateNodes(null, [node], NodeType.UInt, fixture.Limit, true);

        node.Value.GetUInt32().Should().Be(11u);
        node.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
    }

    [Test]
    public void Updates_PublishValueStatusAndTimestampChanges()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UInt, new Variant(1u));
        node.StatusCode = StatusCodes.BadDataLost;
        node.ClearChangeMasks(fixture.Context, false);
        NodeStateChangeMasks changes = NodeStateChangeMasks.None;
        node.OnStateChanged = (_, _, mask) => changes |= mask;
        fixture.Limit.Value = new Variant(1);

        fixture.Update(node, NodeType.UInt);

        node.Value.GetUInt32().Should().Be(2u);
        node.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        node.Timestamp.Should().Be((DateTimeUtc)fixture.Now);
        fixture.Limit.Timestamp.Should().Be((DateTimeUtc)fixture.Now);
        changes.Should().HaveFlag(NodeStateChangeMasks.Value);
        node.ChangeMasks.Should().Be(NodeStateChangeMasks.None);
    }

    [TestCase(0.0, 10.0)]
    [TestCase(-8.0, -5.0)]
    [TestCase(-5.0, 5.0)]
    public void Double_RandomValuesStayInRangeAndChange(double min, double max)
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.Double, new Variant(min), 1.0, min, max, true);
        double previous = min;

        for (int cycle = 0; cycle < 64; cycle++)
        {
            fixture.Update(node, NodeType.Double);
            double current = node.Value.GetDouble();
            current.Should().BeInRange(min, max).And.NotBe(previous);
            previous = current;
        }
    }

    [TestCase(10u, 20u)]
    [TestCase(0u, uint.MaxValue)]
    public void UInt_RandomValuesStayInRangeAndChange(uint min, uint max)
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.UInt, new Variant(min), 1u, min, max, true);
        uint previous = min;

        for (int cycle = 0; cycle < 64; cycle++)
        {
            fixture.Update(node, NodeType.UInt);
            uint current = node.Value.GetUInt32();
            current.Should().BeInRange(min, max).And.NotBe(previous);
            previous = current;
        }
    }

    [TestCase(NodeType.UInt)]
    [TestCase(NodeType.Double)]
    public void Randomization_RejectsSingleValueRange(NodeType type)
    {
        var fixture = new SimulationFixture();
        var node = type == NodeType.UInt
            ? CreateNode(type, new Variant(5u), 1u, 5u, 5u, true)
            : CreateNode(type, new Variant(5.0), 1.0, 5.0, 5.0, true);

        Action update = () => fixture.Update(node, type);

        update.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Double_SequentialRejectsMixedSignRange()
    {
        var fixture = new SimulationFixture();
        var node = CreateNode(NodeType.Double, new Variant(0.0), 1.0, -5.0, 5.0);

        Action update = () => fixture.Update(node, NodeType.Double);

        update.Should().Throw<ArgumentException>();
    }

    private static BaseDataVariableStateExtended CreateNode(
        NodeType type, Variant initial, object step = null, object min = null, object max = null, bool randomize = false)
    {
        object defaultStep = type == NodeType.Double ? (object)1.0 : 1u;
        object defaultMin = type == NodeType.Double ? (object)0.0 : 0u;
        object defaultMax = type == NodeType.Double ? (object)100.0 : 100u;
        return new BaseDataVariableStateExtended(
            new FolderState(null), randomize, step ?? defaultStep, min ?? defaultMin, max ?? defaultMax)
        {
            Value = initial
        };
    }

    private sealed class SimulationFixture
    {
        public SystemContext Context { get; } = new(null);
        public DateTime Now { get; } = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        public BaseDataVariableState Limit { get; } = new(null) { Value = new Variant(-1) };
        public SlowFastNodeSimulation Simulation { get; }

        public SimulationFixture()
        {
            var timeService = new Mock<TimeService>();
            timeService.Setup(service => service.Now()).Returns(Now);
            Simulation = new SlowFastNodeSimulation(Context, timeService.Object, NullLogger.Instance);
        }

        public void Update(BaseDataVariableState node, NodeType type)
        {
            Simulation.UpdateNodes([node], null, type, Limit, true);
        }
    }
}