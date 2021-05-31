namespace OpcPlc.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using static System.TimeSpan;

    /// <summary>
    /// Tests for the variables defined in the simulator, such as fast-changing and trended nodes.
    /// </summary>
    [TestFixture]
    public class SimulatorNodesTests : SimulatorTestsBase
    {
        // Set any cmd params needed for the plc server explicitly.
        public SimulatorNodesTests() : base(new string[] { })
        {
        }

        // Simulator does not update trended and boolean values in the first few cycles (a random number of cycles between 1 and 10)
        private const int RampUpPeriods = 10;

        // Value set for NumberOfUpdates for the simulator to update value indefinitely.
        private const int NoLimit = -1;

        [TestCase]
        public void Telemetry_StepUp()
        {
            var nodeId = GetOpcPlcNodeId("StepUp");

            var measurements = new List<object>();

            // need to track the first value encountered b/c the measurement stream starts when
            // the server starts and it can take several seconds for our test to start
            var firstValue = 0u;
            for (int i = 0; i < 10; i++)
            {
                FireTimersWithPeriod(FromMilliseconds(100), 1);

                var value = ReadValue<uint>(nodeId);
                if (firstValue == 0)
                {
                    firstValue = value;
                }

                measurements.Add(value);
            }

            List<uint> expectedValues = Enumerable.Range((int)firstValue, 10)
                .Select<int, uint>(i => (uint)i)
                .ToList();

            measurements.Should().NotBeEmpty()
                .And.HaveCount(10)
                .And.ContainInOrder(expectedValues)
                .And.ContainItemsAssignableTo<UInt32>();
        }

        [TestCase("DipData", -1000)]
        [TestCase("SpikeData", 1000)]
        public void Telemetry_ContainsOutlier(string identifier, int outlierValue)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var outlierCount = 0;
            var maxValue = 0d;
            var minValue = 0d;

            // take 100 measurements, which is enough that at least a few outliers should be present
            for (int i = 0; i < 100; i++)
            {
                FireTimersWithPeriod(FromMilliseconds(100), 1);

                var value = ReadValue<double>(nodeId);

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
        public void Telemetry_ChangesWithPeriod(string identifier, Type type, uint periodInMilliseconds, int invocations, int rampUpPeriods)
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
                FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), invocations);

                var value = Session.ReadValue(nodeId).Value;
                value.Should().BeOfType(type);

                if (i > 0)
                {
                    if (((IComparable)value).CompareTo(lastValue) != 0)
                    {
                        numberOfValueChanges++;
                    }
                }

                lastValue = value;
            }

            numberOfValueChanges.Should().Be(3);
        }

        [Test]
        [TestCase("BadFastUInt1", 1000u, 1)]
        public void BadNode_HasAlternatingStatusCode(string identifier, uint periodInMilliseconds, int invocations)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var cycles = 15;
            var values = Enumerable.Range(0, cycles)
                .Select(i =>
                {
                    FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), invocations);

                    try
                    {
                        var value = Session.ReadValue(nodeId);
                        return (value.StatusCode, value.Value);
                    }
                    catch (ServiceResultException e)
                    {
                        return (e.StatusCode, null);
                    }
                }).ToList();

            var valuesByStatus = values.GroupBy(v => v.StatusCode).ToDictionary(g => g.Key, g => g.ToList());

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
                .WhichValue
                .Should().HaveCountGreaterThan(cycles * 5 / 10)
                .And.OnlyContain(v => v.Value != null);

            valuesByStatus
                .Should().ContainKey(StatusCodes.UncertainLastUsableValue)
                .WhichValue
                .Should().OnlyContain(v => v.Value != null);
        }

        [Test]
        [TestCase("FastUInt1", "FastNumberOfUpdates", 1000u)]
        [TestCase("SlowUInt1", "SlowNumberOfUpdates", 10000u)]
        public void LimitNumberOfUpdates_StopsUpdatingAfterLimit(string identifier, string numberOfUpdatesNodeName, uint periodInMilliseconds)
        {
            var nodeId = GetOpcPlcNodeId(identifier);
            var value1 = ReadValue<uint>(nodeId);

            // Change the value of the NumberOfUpdates control variable to 6.
            var numberOfUpdatesNode = GetOpcPlcNodeId(numberOfUpdatesNodeName);
            WriteValue(numberOfUpdatesNode, 6);

            // Fire the timer 6 times, should increase the value each time.
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), 6);
            var value2 = ReadValue<uint>(nodeId);
            value2.Should().Be(value1 + 6);

            // NumberOfUpdates variable should now be 0. The Fast node value should not change anymore.
            for (var i = 0; i < 10; i++)
            {
                ReadValue<int>(numberOfUpdatesNode).Should().Be(0);
                FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), 1);
                var value3 = ReadValue<uint>(nodeId);
                value3.Should().Be(value1 + 6);
            }

            // Change the value of the NumberOfUpdates control variable to -1.
            // The Fast node value should now increase indefinitely.
            WriteValue(numberOfUpdatesNode, NoLimit);
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), 3);
            var value4 = ReadValue<uint>(nodeId);
            value4.Should().Be(value1 + 6 + 3);
            ReadValue<int>(numberOfUpdatesNode).Should().Be(NoLimit, "NumberOfUpdates node value should not change when it is {0}", NoLimit);
        }

        [Test]
        [TestCase("NegativeTrendData", 100u, 50, false)]
        [TestCase("PositiveTrendData", 100u, 50, true)]
        public void TrendDataNode_HasValueWithTrend(string identifier, uint periodInMilliseconds, int invocations, bool increasing)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), invocations * RampUpPeriods);

            var firstValue = ReadValue<double>(nodeId);
            FireTimersWithPeriod(FromMilliseconds(periodInMilliseconds), invocations);
            var secondValue = ReadValue<double>(nodeId);
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
}