namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.DI;
using static System.TimeSpan;

/// <summary>
/// Tests for the Boiler2, which derives from DI.
/// </summary>
[TestFixture]
public class Boiler2Tests : SimulatorTestsBase
{
    public Boiler2Tests() : base(new[] {
        "--b2ts=5",
        "--b2bt=1",
        "--b2tt=123",
        "--b2mi=567",
        "--b2oi=678",
    })
    {
    }

    [TearDown]
    public new virtual void TearDown()
    {
    }

    [TestCase]
    public void VerifyFixedConfiguration()
    {
        var nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var tempSpeedDegreesPerSec = (float)Session.ReadValue(nodeId).Value;
        tempSpeedDegreesPerSec.Should().Be(5);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var maintenanceIntervalSeconds = (uint)Session.ReadValue(nodeId).Value;
        maintenanceIntervalSeconds.Should().Be(567);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var overheatIntervalSeconds = (uint)Session.ReadValue(nodeId).Value;
        overheatIntervalSeconds.Should().Be(678);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var _overheatThresholdDegrees = (float)Session.ReadValue(nodeId).Value;
        _overheatThresholdDegrees.Should().Be(123f + 10f);
    }

    [TestCase]
    public void TemperatureRisesAndFallsHeaterToggles()
    {
        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;
        currentTemperatureDegrees.Should().Be(1f);

        // Temperature rises with heater on for the next 20 s starting at 1�, step 5�.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 20);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        var heaterStateNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(101f);
        heaterState.Should().BeTrue();

        // Temperature rises until 123�, then falls with heater off, step -5�.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 20);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(48f);
        heaterState.Should().BeFalse();
    }

    [TestCase]
    public void TestDeviceHealth()
    {
        // 1. NORMAL: Base temperature <= temperature <= target temperature

        var deviceHealthNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        deviceHealth.Should().Be(DeviceHealthEnumeration.NORMAL);

        // 2. MAINTENANCE_REQUIRED: Triggered by the maintenance interval

        // Fast forward to trigger maintenance required.
        FireTimersWithPeriod(FromSeconds(567), numberOfTimes: 1);

        deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        // TODO: Fix spec, bcs state is overwritten immediately!
        deviceHealth.Should().Be(DeviceHealthEnumeration.MAINTENANCE_REQUIRED);

        // 3. FAILURE: Temperature > overheated temperature

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);

        // Fast forward to trigger overheat, then cool down for 2 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 2);

        var currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        currentTemperatureDegrees.Should().Be(133);
        deviceHealth.Should().Be(DeviceHealthEnumeration.FAILURE);

        // 4. CHECK_FUNCTION: Target temperature < Temperature < overheated temperature

        // Fast forward to trigger overheat, then cool down for 3 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 3);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        currentTemperatureDegrees.Should().Be(128);
        deviceHealth.Should().Be(DeviceHealthEnumeration.CHECK_FUNCTION);

        // 5. OFF_SPEC 1: Temperature < base temperature

        // Cool down for 5 s.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 5);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = WriteValue(baseTemperatureNodeId, currentTemperatureDegrees + 10f);
        statusCode.Should().Be(StatusCodes.Good);

        // Fast forward 1 s to update the DeviceHealth.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        // TODO: Simulation does not allow base temp > current temp due to Min/Max.
        ////deviceHealth.Should().Be(DeviceHealthEnumeration.OFF_SPEC);

        // 6. OFF_SPEC 2: Temperature > overheated temperature + 5

        // Fast forward to trigger overheat.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        deviceHealth = (DeviceHealthEnumeration)Session.ReadValue(deviceHealthNodeId).Value;

        currentTemperatureDegrees.Should().Be(143);
        deviceHealth.Should().Be(DeviceHealthEnumeration.OFF_SPEC);
    }
}
