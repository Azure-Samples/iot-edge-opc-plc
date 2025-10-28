namespace OpcPlc.Tests;

using BoilerModel1;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using System.Text.Json;
using System.Threading.Tasks;
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
    public new virtual async Task TearDown()
    {
        await TurnHeaterOnAsync().ConfigureAwait(false);
    }

    [TestCase]
    public async Task Heater_AtStartUp_IsTurnedOn()
    {
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        BoilerDataType model = await GetBoilerModelAsync().ConfigureAwait(false);

        BoilerHeaterStateType state = model.HeaterState;
        BoilerTemperatureType temperature = model.Temperature;
        int pressure = model.Pressure;

        state.Should().Be(BoilerHeaterStateType.On, "heater should start in 'on' state");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature.Bottom.Should().Be(pressure - 100_000, "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public async Task Heater_CanBeTurnedOff()
    {
        // let heater run for a few seconds to make temperature rise
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        await TurnHeaterOffAsync().ConfigureAwait(false);

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        BoilerDataType model = await GetBoilerModelAsync().ConfigureAwait(false);

        BoilerHeaterStateType state = model.HeaterState;
        BoilerTemperatureType temperature = model.Temperature;
        int pressure = model.Pressure;

        state.Should().Be(BoilerHeaterStateType.Off, "heater should have been turned off");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature.Bottom.Should().Be(pressure - 100_000, "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public async Task Heater_WhenRunning_HasRisingPressure()
    {
        int previousPressure = 0;
        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model.Pressure;

            pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
            previousPressure = pressure;
        }
    }

    [TestCase]
    public async Task Heater_WhenStopped_HasFallingPressure()
    {
        int previousPressure = 0;
        for (int i = 0; i < 10; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model.Pressure;

            pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
            previousPressure = pressure;
        }

        await TurnHeaterOffAsync().ConfigureAwait(false);

        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            BoilerDataType model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model.Pressure;

            pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
            previousPressure = pressure;
        }
    }

    [TestCase]
    public async Task Heater_CanbeWritten()
    {
        var newValue = await GetBoilerModelAsync().ConfigureAwait(false);
        newValue.Pressure = 42_000;
        var nodeId = NodeId.Create(BoilerModel1.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(nodeId, newValue).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        var currentValue = await GetBoilerModelAsync().ConfigureAwait(false);
        currentValue.Pressure.Should().Be(newValue.Pressure);
    }

    private async Task TurnHeaterOnAsync()
    {
        var methodNode = NodeId.Create("HeaterOn", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        await Session.CallAsync(GetOpcPlcNodeId("Methods"), methodNode).ConfigureAwait(false);
    }

    private async Task TurnHeaterOffAsync()
    {
        var methodNode = NodeId.Create("HeaterOff", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        await Session.CallAsync(GetOpcPlcNodeId("Methods"), methodNode).ConfigureAwait(false);
    }

    private async Task<BoilerDataType> GetBoilerModelAsync()
    {
        var nodeId = NodeId.Create(BoilerModel1.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var value = (await Session.ReadValueAsync(nodeId).ConfigureAwait(false)).Value;

        // change dynamic in-memory created Boiler type to expected BoilerDataType by serializing and deserializing it.
        var inMemoryBoilerDataType = (value as ExtensionObject).Body;
        var json = JsonSerializer.Serialize(inMemoryBoilerDataType);

        var boilerDataTypeFromGeneratedSourceCode = JsonSerializer.Deserialize<BoilerDataType>(json);
        return boilerDataTypeFromGeneratedSourceCode;
    }
}
