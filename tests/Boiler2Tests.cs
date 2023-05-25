namespace OpcPlc.Tests;

using BoilerModel2;
using FluentAssertions;
using NUnit.Framework;
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
        ////TurnHeaterOn();
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
        // Temperature rises with heater on for the next 10 s starting at 1�, step 10�.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 10);

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        var heaterStateNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(101f);
        heaterState.Should().BeTrue();

        // Temperature rises until 123�, then falls with heater off, step -10�.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 10);

        currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        heaterState = (bool)Session.ReadValue(heaterStateNodeId).Value;

        currentTemperatureDegrees.Should().Be(53f);
        heaterState.Should().BeFalse();
    }

    ////[TestCase]
    ////public void Heater_CanBeTurnedOff()
    ////{
    ////    // let heater run for a few seconds to make temperature rise
    ////    FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

    ////    TurnHeaterOff();

    ////    FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

    ////    BoilerDataType model = GetBoilerModel();

    ////    BoilerHeaterStateType state = model.HeaterState;
    ////    BoilerTemperatureType temperature = model.Temperature;
    ////    int pressure = model.Pressure;

    ////    state.Should().Be(BoilerHeaterStateType.Off, "heater should have been turned off");
    ////    pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

    ////    temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
    ////    temperature.Bottom.Should().Be(pressure - 100_000, "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    ////}

    ////[TestCase]
    ////public void Heater_WhenRunning_HasRisingPressure()
    ////{
    ////    int previousPressure = 0;
    ////    for (int i = 0; i < 5; i++)
    ////    {
    ////        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
    ////        BoilerDataType model = GetBoilerModel();
    ////        int pressure = model.Pressure;

    ////        pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
    ////        previousPressure = pressure;
    ////    }
    ////}

    ////[TestCase]
    ////public void Heater_WhenStopped_HasFallingPressure()
    ////{
    ////    int previousPressure = 0;
    ////    for (int i = 0; i < 10; i++)
    ////    {
    ////        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
    ////        BoilerDataType model = GetBoilerModel();
    ////        int pressure = model.Pressure;

    ////        pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
    ////        previousPressure = pressure;
    ////    }

    ////    TurnHeaterOff();

    ////    for (int i = 0; i < 5; i++)
    ////    {
    ////        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
    ////        BoilerDataType model = GetBoilerModel();
    ////        int pressure = model.Pressure;

    ////        pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
    ////        previousPressure = pressure;
    ////    }
    ////}

    ////private void TurnHeaterOn()
    ////{
    ////    var methodNode = NodeId.Create("HeaterOn", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
    ////    Session.Call(GetOpcPlcNodeId("Methods"), methodNode);
    ////}

    ////private void TurnHeaterOff()
    ////{
    ////    var methodNode = NodeId.Create("HeaterOff", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
    ////    Session.Call(GetOpcPlcNodeId("Methods"), methodNode);
    ////}

    ////private BoilerDataType GetBoilerModel()
    ////{
    ////    var nodeId = NodeId.Create(BoilerModel2.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
    ////    var value = Session.ReadValue(nodeId).Value;
    ////    return value.Should().BeOfType<ExtensionObject>().Which.Body.Should().BeOfType<BoilerDataType>().Subject;
    ////}
}