namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meters = OpcPlc.MetricsHelper;

/// <summary>
/// Tests for Metrics.
/// </summary>
internal class MetricsTests
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
            (Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => {
                _metrics.Add(instrument.Name, measurement);
            });

        _meterListener.SetMeasurementEventCallback(
            (Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => {
                _metrics.Add(instrument.Name, measurement);
            });

        _meterListener.SetMeasurementEventCallback(
            (Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) => {
                _metrics.Add(instrument.Name, measurement);
            });

        _meterListener.Start();
    }

    [SetUp]
    public void SetUp()
    {
        _metrics.Clear();
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
        Meters.AddSessionCount(sessionId.ToString());
        _metrics.TryGetValue("opc_plc_session_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddSubscriptionCount()
    {
        var sessionId = Guid.NewGuid().ToString();
        var subscriptionId = Guid.NewGuid().ToString();
        Meters.AddSubscriptionCount(sessionId, subscriptionId);
        _metrics.TryGetValue("opc_plc_subscription_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddMonitoredItemCount()
    {
        var sessionId = Guid.NewGuid().ToString();
        Meters.AddMonitoredItemCount(sessionId);
        _metrics.TryGetValue("opc_plc_monitored_item_count", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestAddPublishedCount()
    {
        var sessionId = Guid.NewGuid().ToString();
        var subscriptionId = Guid.NewGuid().ToString();
        Meters.AddPublishedCount(sessionId, subscriptionId, 1, 0);
        _metrics.TryGetValue("opc_plc_published_count_with_type", out var counter).Should().BeTrue();
        counter.Should().Be(1);
    }

    [Test]
    public void TestRecordTotalErrors()
    {
        Meters.RecordTotalErrors("operation");
        _metrics.TryGetValue("opc_plc_total_errors", out var counter).Should().BeTrue(); ;
        counter.Should().Be(1);
    }
}

