namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the variables defined in the simulator, such as fast-changing and trended nodes.
/// </summary>
[TestFixture]
public class SimulatorNodesTests : SimulatorTestsBase
{
    // Set any cmd params needed for the plc server explicitly.
    public SimulatorNodesTests() : base(["--str=false"])
    {
    }

    // Simulator does not update trended and boolean values in the first few cycles (a random number of cycles between 1 and 10)
    private const int RampUpPeriods = 10;

    // Value set for NumberOfUpdates for the simulator to update value indefinitely.
    private const int NoLimit = -1;

    [TestCase]
    public async Task Telemetry_StepUp()
    {
        var nodeId = GetOpcPlcNodeId("StepUp");

        var measurements = new List<object>();

        // need to track the first value encountered b/c the measurement stream starts when
        // the server starts and it can take several seconds for our test to start
        var firstValue = 0u;
        for (int i = 0; i < 10; i++)
        {
            FireTimersWithPeriod(FromMilliseconds(100), numberOfTimes: 1);

            var value = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
            if (firstValue == 0)
            {
                firstValue = value;
            }

            measurements.Add(value);
        }

        List<object> expectedValues = Enumerable.Range((int)firstValue, 10)
            .Select<int, object>(i => (uint)i)
            .ToList();

        measurements.Should().NotBeEmpty()
            .And.HaveCount(10)
            .And.ContainInOrder(expectedValues)
            .And.ContainItemsAssignableTo<UInt32>();
    }

    [TestCase]
    public async Task Telemetry_FastNode()
    {
        var nodeId = GetOpcPlcNodeId("FastUInt1");

        var lastValue = 0u;
        for (int i = 0; i < 10; i++)
        {
            var value = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
            if (lastValue == 0)
            {
                lastValue = value;
            }
            else
            {
                value.Should().Be(lastValue + 1);
                lastValue = value;
            }

            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);
        }

        lastValue++;

        await CallMethodAsync("StopUpdateFastNodes").ConfigureAwait(false);

        var nextValue = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        nextValue.Should().Be(lastValue);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);
        nextValue = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        nextValue.Should().Be(lastValue);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        await CallMethodAsync("StartUpdateFastNodes").ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        nextValue = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        nextValue.Should().Be(lastValue + 1);
    }

    [TestCase("DipData", -1000)]
    [TestCase("SpikeData", 1000)]
    public async Task Telemetry_ContainsOutlier(string identifier, int outlierValue)
    {
        var nodeId = GetOpcPlcNodeId(identifier);

        var outlierCount = 0;
        var maxValue = 0d;
        var minValue = 0d;

        // take 100 measurements, which is enough that at least a few outliers should be present
        for (int i = 0; i < 100; i++)
        {
            FireTimersWithPeriod(FromMilliseconds(100), numberOfTimes: 1);

            var value = await ReadValueAsync<double>(nodeId).ConfigureAwait(false);

            if (Math.Round(value) == outlierValue)
            {
                outlierCount++;
            }
            else
            {
                maxValue = Math.Max(maxValue, value);
                minValue = Math.Min(minValue, value);
            }
        }

        maxValue.Should().BeInRange(90, 100, "measurement data should have a ceiling around 100");
        minValue.Should().BeInRange(-100, -90, "measurement data should have a floor around -100");
        outlierCount.Should().BeGreaterThan(0, "there should be at least a few measurements that were {0}", outlierValue);
    }

    [Test]
    [TestCase("FastUInt1", typeof(uint), 1000u, 1, 0)]
    [TestCase("SlowUInt1", typeof(uint), 10000u, 1, 0)]
    [TestCase("RandomSignedInt32", typeof(int), 100u, 1, 0)]
    [TestCase("RandomUnsignedInt32", typeof(uint), 100u, 1, 0)]
    [TestCase("AlternatingBoolean", typeof(bool), 100u, 50, 0)]
    [TestCase("NegativeTrendData", typeof(double), 100u, 50, RampUpPeriods)]
    [TestCase("PositiveTrendData", typeof(double), 100u, 50, RampUpPeriods)]
    public async Task Telemetry_ChangesWithPeriod(string identifier, Type type, uint periodInMilliseconds, int invocations, int rampUpPeriods)
    {
        var nodeId = GetOpcPlcNodeId(identifier);
        int numberOfTimes = invocations * rampUpPeriods;
        FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes);

        // Measure the value 4 times, sleeping for a third of the period at which the value changes each time.
        // The number of times the value changes over the 4 measurements should be between 1 and 2.
        object lastValue = null;
        var numberOfValueChanges = 0;
        for (int i = 0; i < 4; i++)
        {
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: invocations);

            var value = (await Session.ReadValueAsync(nodeId).ConfigureAwait(false)).Value;
            value.Should().BeOfType(type);

            if (i > 0 && (value as IComparable).CompareTo(lastValue) != 0)
            {
                numberOfValueChanges++;
            }

            lastValue = value;
        }

        numberOfValueChanges.Should().Be(3);
    }

    [Test]
    [TestCase("BadFastUInt1", 1000u, 1)]
    public async Task BadNode_HasAlternatingStatusCode(string identifier, uint periodInMilliseconds, int invocations)
    {
        var nodeId = GetOpcPlcNodeId(identifier);

        var cycles = 15;
        var readings = new List<(StatusCode StatusCode, object Value)>(capacity: cycles);
        for (int i = 0; i < cycles; i++)
        {
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: invocations);
            try
            {
                var dataValue = await Session.ReadValueAsync(nodeId).ConfigureAwait(false);
                readings.Add((dataValue.StatusCode, dataValue.Value));
            }
            catch (ServiceResultException e)
            {
                readings.Add((e.StatusCode, null));
            }
        }

        var valuesByStatus = readings.GroupBy(v => v.StatusCode).ToDictionary(g => g.Key, g => g.ToList());

        valuesByStatus
            .Keys.Should().BeEquivalentTo(new[]
            {
                    StatusCodes.Good,
                    StatusCodes.UncertainLastUsableValue,
                    StatusCodes.BadDataLost,
                    StatusCodes.BadNoCommunication,
            });

        valuesByStatus
            .Should().ContainKey(StatusCodes.Good)
            .WhoseValue
            .Should().HaveCountGreaterThan(cycles * 5 / 10)
            .And.OnlyContain(v => v.Value != null);

        valuesByStatus
            .Should().ContainKey(StatusCodes.UncertainLastUsableValue)
            .WhoseValue
            .Should().OnlyContain(v => v.Value != null);
    }

    [Test]
    [TestCase("FastUInt1", "FastNumberOfUpdates", 1000u)]
    [TestCase("SlowUInt1", "SlowNumberOfUpdates", 10000u)]
    public async Task LimitNumberOfUpdates_StopsUpdatingAfterLimit(string identifier, string numberOfUpdatesNodeName, uint periodInMilliseconds)
    {
        var nodeId = GetOpcPlcNodeId(identifier);
        var value1 = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);

        // Change the value of the NumberOfUpdates control variable to 6.
        var numberOfUpdatesNode = GetOpcPlcNodeId(numberOfUpdatesNodeName);
        await WriteValueAsync(numberOfUpdatesNode, 6).ConfigureAwait(false);

        // Fire the timer 6 times, should increase the value each time.
        FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: 6);
        var value2 = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        value2.Should().Be(value1 + 6);

        // NumberOfUpdates variable should now be 0. The Fast node value should not change anymore.
        for (var i = 0; i < 10; i++)
        {
            (await ReadValueAsync<int>(numberOfUpdatesNode).ConfigureAwait(false)).Should().Be(0);
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: 1);
            var value3 = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
            value3.Should().Be(value1 + 6);
        }

        // Change the value of the NumberOfUpdates control variable to -1.
        // The Fast node value should now increase indefinitely.
        await WriteValueAsync(numberOfUpdatesNode, NoLimit).ConfigureAwait(false);
        FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: 3);
        var value4 = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        value4.Should().Be(value1 + 6 + 3);
        (await ReadValueAsync<int>(numberOfUpdatesNode).ConfigureAwait(false)).Should().Be(NoLimit, "NumberOfUpdates node value should not change when it is {0}", NoLimit);
    }

    [Test]
    [TestCase("NegativeTrendData", 100u, 50, false)]
    [TestCase("PositiveTrendData", 100u, 50, true)]
    public async Task TrendDataNode_HasValueWithTrend(string identifier, uint periodInMilliseconds, int invocations, bool increasing)
    {
        var nodeId = GetOpcPlcNodeId(identifier);

        FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: invocations * RampUpPeriods);

        var firstValue = await ReadValueAsync<double>(nodeId).ConfigureAwait(false);
        FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), numberOfTimes: invocations);
        var secondValue = await ReadValueAsync<double>(nodeId).ConfigureAwait(false);
        if (increasing)
        {
            secondValue.Should().BeGreaterThan(firstValue);
        }
        else
        {
            secondValue.Should().BeLessThan(firstValue);
        }
    }
}
