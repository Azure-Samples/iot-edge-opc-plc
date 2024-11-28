namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

/// <summary>
/// Tests for Metrics.
/// </summary>
[NonParallelizable]
[Ignore("Failing since .NET 9 and also with new telemetry nugets")]
internal class MetricsTests : SimulatorTestsBase
{
    private readonly MeterListener _meterListener;
    private readonly Dictionary<string, object> _metrics;

    public MetricsTests()
    {
        _metrics = new Dictionary<string, object>();
        _meterListener = new MeterListener();

        _meterListener.InstrumentPublished = (instrument, listener) => {
            if (instrument.Meter.Name == MetricsHelper.Meter.Name)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback(
            (Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => _metrics.Add(instrument.Name, measurement));

        _meterListener.SetMeasurementEventCallback(
            (Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => _metrics.Add(instrument.Name, measurement));

        _meterListener.SetMeasurementEventCallback(
            (Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => _metrics.Add(instrument.Name, measurement));

        _meterListener.Start();
    }

    [SetUp]
    public void SetUp()
    {
        _metrics.Clear();

        MetricsHelper.IsEnabled = true;
    }

    [TearDown]
    public void TearDown()
    {
        _meterListener.Dispose();
    }

    [Test]
    public void TestAddSessionCount()
    {
        var sessionId = Guid.NewGuid().ToString();
        MetricsHelper.AddSessionCount(sessionId.ToString());
        _metrics.TryGetValue("opc_plc_session_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddSubscriptionCount()
    {
        var sessionId = Guid.NewGuid().ToString();
        var subscriptionId = Guid.NewGuid().ToString();
        MetricsHelper.AddSubscriptionCount(sessionId, subscriptionId);
        _metrics.TryGetValue("opc_plc_subscription_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddMonitoredItemCount()
    {
        MetricsHelper.AddMonitoredItemCount(1);
        _metrics.TryGetValue("opc_plc_monitored_item_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddPublishedCount()
    {
        MetricsHelper.AddPublishedCount(1, 0);
        _metrics.TryGetValue("opc_plc_published_count_with_type", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestRecordTotalErrors()
    {
        MetricsHelper.RecordTotalErrors("operation");
        _metrics.TryGetValue("opc_plc_total_errors", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }
}
