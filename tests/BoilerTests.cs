namespace OpcPlc.Tests;

using BoilerModel1;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client.ComplexTypes;
using System.Dynamic;
using System.Text.Json;
using static System.TimeSpan;

/// <summary>
/// Tests for the Boiler, which is a complex type.
/// </summary>
[TestFixture]
public class BoilerTests : SimulatorTestsBase
{
    private ComplexTypeSystem _complexTypeSystem;
    public BoilerTests() : base(["--ctb"])
    {
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _complexTypeSystem = new ComplexTypeSystem(Session);
        var loaded =  _complexTypeSystem.LoadNamespace(OpcPlc.Namespaces.OpcPlcBoiler).ConfigureAwait(false).GetAwaiter().GetResult();
        loaded.Should().BeTrue("BoilerDataType should be loaded");
    }

    [TearDown]
    public new virtual void TearDown()
    {
        TurnHeaterOn();
    }

    [TestCase]
    public void Heater_AtStartUp_IsTurnedOn()
    {
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        BoilerDataType model = GetBoilerModel();

        BoilerHeaterStateType state = model.HeaterState;
        BoilerTemperatureType temperature = model.Temperature;
        int pressure = model.Pressure;

        state.Should().Be(BoilerHeaterStateType.On, "heater should start in 'on' state");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature.Bottom.Should().Be(pressure - 100_000, "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public void Heater_CanBeTurnedOff()
    {
        // let heater run for a few seconds to make temperature rise
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        TurnHeaterOff();

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        BoilerDataType model = GetBoilerModel();

        BoilerHeaterStateType state = model.HeaterState;
        BoilerTemperatureType temperature = model.Temperature;
        int pressure = model.Pressure;

        state.Should().Be(BoilerHeaterStateType.Off, "heater should have been turned off");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature.Bottom.Should().Be(pressure - 100_000, "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public void Heater_WhenRunning_HasRisingPressure()
    {
        int previousPressure = 0;
        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = GetBoilerModel();
            int pressure = model.Pressure;

            pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
            previousPressure = pressure;
        }
    }

    [TestCase]
    public void Heater_WhenStopped_HasFallingPressure()
    {
        int previousPressure = 0;
        for (int i = 0; i < 10; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = GetBoilerModel();
            int pressure = model.Pressure;

            pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
            previousPressure = pressure;
        }

        TurnHeaterOff();

        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = GetBoilerModel();
            int pressure = model.Pressure;

            pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
            previousPressure = pressure;
        }
    }

    [TestCase]
    public void Heater_CanbeWritten()
    {
        var newValue = GetBoilerModel();
        newValue.Pressure = 42_000;
        var nodeId = NodeId.Create(BoilerModel1.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = WriteValue(nodeId, newValue);
        statusCode.Should().Be(StatusCodes.Good);
        var currentValue = GetBoilerModel();
        currentValue.Pressure.Should().Be(newValue.Pressure);
    }

    private void TurnHeaterOn()
    {
        var methodNode = NodeId.Create("HeaterOn", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        Session.Call(GetOpcPlcNodeId("Methods"), methodNode);
    }

    private void TurnHeaterOff()
    {
        var methodNode = NodeId.Create("HeaterOff", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        Session.Call(GetOpcPlcNodeId("Methods"), methodNode);
    }

    private BoilerDataType GetBoilerModel()
    {
        var nodeId = NodeId.Create(BoilerModel1.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var value = Session.ReadValue(nodeId).Value;

        // change dynamic in-memory created Boiler type to expected BoilerDataType by serializing and deserializing it.
        var inmemoryBoilerDataType = (value as ExtensionObject).Body;
        var json = JsonSerializer.Serialize(inmemoryBoilerDataType);

        var boilerDataTypeFromGeneratedSourceCode = JsonSerializer.Deserialize<BoilerDataType>(json);
        return boilerDataTypeFromGeneratedSourceCode;
    }
}
