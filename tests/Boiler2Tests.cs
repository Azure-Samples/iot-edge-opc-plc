namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.DI;
using OpcPlc.PluginNodes;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the Boiler2, which derives from DI.
/// </summary>
[TestFixture]
public class Boiler2Tests : SimulatorTestsBase
{
    public Boiler2Tests() : base([
        "--b2ts=5",    // Temperature change speed.
        "--b2bt=1",    // Base temperature.
        "--b2tt=123",  // Target temperature.
        "--b2mi=567",  // Maintenance interval.
        "--b2oi=678",  // Overheat interval.
    ])
    {
    }

    [TearDown]
    public new virtual void TearDown()
    {
    }

    [TestCase, Order(9)]
    public async Task VerifyFixedConfiguration()
    {
        var nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float tempSpeedDegreesPerSec = await ReadValueAsync<float>(nodeId).ConfigureAwait(false);
        tempSpeedDegreesPerSec.Should().Be(5);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        uint maintenanceIntervalSeconds = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        maintenanceIntervalSeconds.Should().Be(567);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        uint overheatIntervalSeconds = await ReadValueAsync<uint>(nodeId).ConfigureAwait(false);
        overheatIntervalSeconds.Should().Be(678);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float overheatThresholdDegrees = await ReadValueAsync<float>(nodeId).ConfigureAwait(false);
        overheatThresholdDegrees.Should().Be(123f + 10f);
    }

    [TestCase, Order(1)]
    public async Task TemperatureRisesAndFallsHeaterToggles()
    {
        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);
        currentTemperatureDegrees.Should().Be(1f);

        // Temperature rises with heater on for the next 20 s starting at 1°, step 5°.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 20);

        currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        var heaterStateNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        bool heaterState = await ReadValueAsync<bool>(heaterStateNodeId).ConfigureAwait(false);

        currentTemperatureDegrees.Should().Be(101f);
        heaterState.Should().BeTrue();

        // Temperature rises until 123°, then falls with heater off, step -5°.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 20);

        currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        heaterState = await ReadValueAsync<bool>(heaterStateNodeId).ConfigureAwait(false);

        currentTemperatureDegrees.Should().Be(48f);
        heaterState.Should().BeFalse();
    }

    [Test]
    public async Task TimerCallbackExceptionDoesNotBlockSubsequentUpdates()
    {
        var overheatThresholdNodeId = NodeId.Create(
            BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature,
            OpcPlc.Namespaces.OpcPlcBoiler,
            Session.NamespaceUris);
        var currentTemperatureNodeId = NodeId.Create(
            BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature,
            OpcPlc.Namespaces.OpcPlcBoiler,
            Session.NamespaceUris);
        float originalThreshold = await ReadValueAsync<float>(overheatThresholdNodeId).ConfigureAwait(false);

        try
        {
            var statusCode = await WriteValueAsync(overheatThresholdNodeId, float.NaN).ConfigureAwait(false);
            statusCode.Should().Be(StatusCodes.Good);

            FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

            statusCode = await WriteValueAsync(overheatThresholdNodeId, originalThreshold).ConfigureAwait(false);
            statusCode.Should().Be(StatusCodes.Good);

            FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);

            float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);
            currentTemperatureDegrees.Should().Be(originalThreshold + 10f);
        }
        finally
        {
            _ = await WriteValueAsync(overheatThresholdNodeId, originalThreshold).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task TimerCallbacksWaitForLockInsteadOfBeingSkipped()
    {
        var boiler2 = PluginNodes.OfType<Boiler2PluginNodes>().Single();
        var callbackLock = (SemaphoreSlim)typeof(Boiler2PluginNodes)
            .GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(boiler2);

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var overheatThresholdNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var temperatureChangeSpeedNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);

        float overheatThreshold = await ReadValueAsync<float>(overheatThresholdNodeId).ConfigureAwait(false);
        float baseTemperature = await ReadValueAsync<float>(baseTemperatureNodeId).ConfigureAwait(false);
        float temperatureChangeSpeed = await ReadValueAsync<float>(temperatureChangeSpeedNodeId).ConfigureAwait(false);
        float temperatureBefore = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        // Overheat sets the temperature to threshold + 10 and turns the heater off, then one 1 s tick cools it down.
        float overheatTemperature = overheatThreshold + 10f;
        float expectedTemperature = overheatTemperature - Math.Min(temperatureChangeSpeed, Math.Abs(overheatTemperature - baseTemperature));

        // Simulate a long-running callback holding the lock while the overheat and 1 s timers fire.
        await callbackLock.WaitAsync().ConfigureAwait(false);
        try
        {
            FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

            float temperatureWhileLocked = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);
            temperatureWhileLocked.Should().Be(temperatureBefore, "callbacks must wait while the lock is held");
        }
        finally
        {
            callbackLock.Release();
        }

        // Both queued callbacks must run, in order, once the lock is released.
        float temperatureAfterRelease = float.NaN;
        var deadline = DateTime.UtcNow + FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            temperatureAfterRelease = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);
            if (temperatureAfterRelease == expectedTemperature && callbackLock.CurrentCount == 1)
            {
                break;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        temperatureAfterRelease.Should().Be(expectedTemperature, "the overheat and 1 s callbacks must not be skipped");
        callbackLock.CurrentCount.Should().Be(1, "the lock must be released after the queued callbacks ran");
    }

    [TestCase, Order(2)]
    public async Task DeviceHealth_Normal()
    {
        // 1. NORMAL: Base temperature <= temperature <= target temperature

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        deviceHealth.Should().Be(DeviceHealthEnumeration.NORMAL);
    }

    [TestCase, Order(3)]
    public async Task DeviceHealth_MaintenanceRequired()
    {
        // 2. MAINTENANCE_REQUIRED: Triggered by the maintenance interval

        // Fast forward to trigger maintenance required.
        FireTimersWithPeriod(FromSeconds(567), numberOfTimes: 1);

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        // TODO: Fix spec, bcs state is overwritten immediately!
        deviceHealth.Should().Be(DeviceHealthEnumeration.MAINTENANCE_REQUIRED);
    }

    [TestCase, Order(4)]
    public async Task DeviceHealth_Failure()
    {
        // 3. FAILURE: Temperature > overheated temperature

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);

        // Fast forward to trigger overheat, then cool down for 2 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 2);

        float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        currentTemperatureDegrees.Should().Be(133);
        deviceHealth.Should().Be(DeviceHealthEnumeration.FAILURE);
    }

    [TestCase, Order(5)]
    public async Task DeviceHealth_CheckFunction()
    {
        // 4. CHECK_FUNCTION: Target temperature < Temperature < overheated temperature

        // Fast forward to trigger overheat, then cool down for 3 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 3);

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        currentTemperatureDegrees.Should().Be(128);
        deviceHealth.Should().Be(DeviceHealthEnumeration.CHECK_FUNCTION);
    }

    [TestCase, Order(6)]
    public async Task DeviceHealth_OffSpec1()
    {
        // 5. OFF_SPEC 1: Temperature < base temperature

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(baseTemperatureNodeId, currentTemperatureDegrees + 10f).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);

        // Fast forward 1 s to update the DeviceHealth.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        deviceHealth.Should().Be(DeviceHealthEnumeration.OFF_SPEC);
    }

    [TestCase, Order(7)]
    public async Task DeviceHealth_OffSpec2()
    {
        // 6. OFF_SPEC 2: Temperature > overheated temperature + 5

        // Fast forward to trigger overheat.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        float currentTemperatureDegrees = await ReadValueAsync<float>(currentTemperatureNodeId).ConfigureAwait(false);

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = await ReadValueAsync<DeviceHealthEnumeration>(deviceHealthNodeId).ConfigureAwait(false);

        currentTemperatureDegrees.Should().Be(143);
        deviceHealth.Should().Be(DeviceHealthEnumeration.OFF_SPEC);
    }

    [TestCase, Order(8)]
    public async Task SetBaseTemperature()
    {
        var newValue = 25f;
        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(baseTemperatureNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        float currentBaseTemperature = await ReadValueAsync<float>(baseTemperatureNodeId).ConfigureAwait(false);
        currentBaseTemperature.Should().Be(newValue);
    }

    [TestCase, Order(10)]
    public async Task SetTargetTemperature()
    {
        var newValue = 125f;
        var targetTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TargetTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(targetTemperatureNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        float currentTargetTemperature = await ReadValueAsync<float>(targetTemperatureNodeId).ConfigureAwait(false);
        currentTargetTemperature.Should().Be(newValue);
    }

    [TestCase, Order(11)]
    public async Task SetTemperatureChangeSpeed()
    {
        var newValue = 10f;
        var temperatureChangeSpeedNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(temperatureChangeSpeedNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        float currentTemperatureChangeSpeed = await ReadValueAsync<float>(temperatureChangeSpeedNodeId).ConfigureAwait(false);
        currentTemperatureChangeSpeed.Should().Be(newValue);
    }

    [TestCase, Order(12)]
    public async Task SetOverheatedThresholdTemperature()
    {
        var newValue = 100f;
        var overheatedThresholdTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(overheatedThresholdTemperatureNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        float currentOverheatedThresholdTemperature = await ReadValueAsync<float>(overheatedThresholdTemperatureNodeId)
            .ConfigureAwait(false);
        currentOverheatedThresholdTemperature.Should().Be(newValue);
    }

    [TestCase, Order(13)]
    public async Task SetMaintenanceInterval()
    {
        var newValue = 360u;
        var maintenanceIntervalNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(maintenanceIntervalNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        uint currentMaintenanceInterval = await ReadValueAsync<uint>(maintenanceIntervalNodeId).ConfigureAwait(false);
        currentMaintenanceInterval.Should().Be(newValue);
    }

    [TestCase, Order(14)]
    public async Task SetOverheatInterval()
    {
        var newValue = 150u;
        var overheatIntervalNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(overheatIntervalNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        uint currentMaintenanceInterval = await ReadValueAsync<uint>(overheatIntervalNodeId).ConfigureAwait(false);
        currentMaintenanceInterval.Should().Be(newValue);
    }

    [TestCase, Order(15)]
    public async Task SetAssetId()
    {
        var newValue = "Asset-12345";
        var assetIdNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_AssetId, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(assetIdNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        string currentAssetId = await ReadValueAsync<string>(assetIdNodeId).ConfigureAwait(false);
        currentAssetId.Should().Be(newValue);
    }

    [TestCase, Order(16)]
    public async Task SetDeviceManual()
    {
        var newValue = "https://example.com/manual/boiler2.pdf";
        var deviceManualNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceManual, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(deviceManualNodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        string currentDeviceManual = await ReadValueAsync<string>(deviceManualNodeId).ConfigureAwait(false);
        currentDeviceManual.Should().Be(newValue);
    }
}
