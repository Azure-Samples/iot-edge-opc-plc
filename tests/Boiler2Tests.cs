namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Opc.Ua;
using static System.TimeSpan;

/// <summary>
/// Tests for the Boiler2, which derives from DI.
/// </summary>
[TestFixture]
public class Boiler2Tests : SimulatorTestsBase
{
    public Boiler2Tests() : base(new[] {
        "--b2ts=10",
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
        tempSpeedDegreesPerSec.Should().Be(10);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var maintenanceIntervalSeconds = (uint)Session.ReadValue(nodeId).Value;
        maintenanceIntervalSeconds.Should().Be(567);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var overheatIntervalSeconds = (uint)Session.ReadValue(nodeId).Value;
        overheatIntervalSeconds.Should().Be(678);

        nodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var _overheatThresholdDegrees = (float)Session.ReadValue(nodeId).Value;
        _overheatThresholdDegrees.Should().Be(123.0f + 10.0f);
    }

    [TestCase]
    public void TemperatureRisesAndFallsHeaterToggles()
    {
        // Temperature rises with heater on for the next 10 s starting at 1°, step 10°.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 10);

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        var heaterStateNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(101f);
        heaterState.Should().BeTrue();

        // Temperature rises until 123°, then falls with heater off, step -10°.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 10);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(53f);
        heaterState.Should().BeFalse();
    }

    [TestCase]
    public void TestDeviceHealth()
    {
        // DeviceHealth (DeviceHealthEnumeration) details:
        //-NORMAL: Base temperature <= temperature <= target temperature
        //- FAILURE: Temperature > overheated temperature
        //- CHECK_FUNCTION: Target temperature < Temperature < overheated temperature
        //- OFF_SPEC: Temperature < base temperature or temperature > overheated temperature + 5
        //- MAINTENANCE_REQUIRED: Triggered by the maintenance interval
    }
}
