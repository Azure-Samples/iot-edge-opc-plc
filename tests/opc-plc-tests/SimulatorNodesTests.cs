namespace OpcPlc.Tests
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    /// <summary>
    /// Tests for the variables defined in the simulator, such as fast-changing and trended nodes.
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class SimulatorNodesTests : SimulatorTestsBase
    {
        // Simulator does not update trended and boolean values in the first few cycles (a random number of cycles between 1 and 10)
        private const int RampUpPeriods = 10;

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
                FireTimersWithPeriod(100u, 1);

                var value = Session.ReadValue(nodeId).Value;
                value.Should().BeOfType(typeof(double));

                var doubleValue = (double)value;
                if (Math.Round(doubleValue) == outlierValue)
                {
                    outlierCount++;
                }
                else
                {
                    maxValue = Math.Max(maxValue, doubleValue);
                    minValue = Math.Min(minValue, doubleValue);
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
            FireTimersWithPeriod(periodInMilliseconds, invocations * rampUpPeriods);

            // Measure the value 4 times, sleeping for a third of the period at which the value changes each time.
            // The number of times the value changes over the 4 measurements should be between 1 and 2.
            object lastValue = null;
            var numberOfValueChanges = 0;
            for (int i = 0; i < 4; i++)
            {
                FireTimersWithPeriod(periodInMilliseconds, invocations);

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
                    FireTimersWithPeriod(periodInMilliseconds, invocations);

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
        [TestCase("NegativeTrendData", 100u, 50, false)]
        [TestCase("PositiveTrendData", 100u, 50, true)]
        public void TrendDataNode_HasValueWithTrend(string identifier, uint periodInMilliseconds, int invocations, bool increasing)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            FireTimersWithPeriod(periodInMilliseconds, invocations * RampUpPeriods);

            var firstValue = (double)Session.ReadValue(nodeId).Value;
            FireTimersWithPeriod(periodInMilliseconds, invocations);
            var secondValue = (double)Session.ReadValue(nodeId).Value;
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