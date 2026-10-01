namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using System.Threading;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the Boiler, which is a complex type.
/// </summary>
[TestFixture]
public class BoilerTests : SimulatorTestsBase
{
    private NodeId BoilerStatusId => NodeId.Create(15013u, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);

    public BoilerTests() : base(["--ctb"])
    {
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        using var complexTypeSystem = new DefaultComplexTypeSystemFactory(Session.MessageContext.Telemetry).Create(Session);
        bool loaded = await complexTypeSystem.LoadNamespaceAsync(
            OpcPlc.Namespaces.OpcPlcBoiler, true, CancellationToken.None).ConfigureAwait(false);
        loaded.Should().BeTrue("BoilerDataType should be loaded");
        var nodeId = BoilerStatusId;
        var runtimeValue = await Session.ReadValueAsync(nodeId).ConfigureAwait(false);
        runtimeValue.WrappedValue.TryGetStructure(out IEncodeable runtimeBody).Should().BeTrue();
        runtimeBody.Should().BeAssignableTo<IStructure>();
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

        IStructure model = await GetBoilerModelAsync().ConfigureAwait(false);

        int state = model["HeaterState"].GetInt32();
        IStructure temperature = ReadStructure(model["Temperature"]);
        int pressure = model["Pressure"].GetInt32();

        state.Should().Be(1, "heater should start in 'on' state");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature["Top"].GetInt32().Should().Be(pressure - 100_005,
            "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature["Bottom"].GetInt32().Should().Be(pressure - 100_000,
            "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public async Task Heater_CanBeTurnedOff()
    {
        // let heater run for a few seconds to make temperature rise
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        await TurnHeaterOffAsync().ConfigureAwait(false);

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);

        IStructure model = await GetBoilerModelAsync().ConfigureAwait(false);

        int state = model["HeaterState"].GetInt32();
        IStructure temperature = ReadStructure(model["Temperature"]);
        int pressure = model["Pressure"].GetInt32();

        state.Should().Be(0, "heater should have been turned off");
        pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

        temperature["Top"].GetInt32().Should().Be(pressure - 100_005,
            "top is always 100,005 less than pressure. Pressure: {0}", pressure);
        temperature["Bottom"].GetInt32().Should().Be(pressure - 100_000,
            "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);
    }

    [TestCase]
    public async Task Heater_WhenRunning_HasRisingPressure()
    {
        int previousPressure = 0;
        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            IStructure model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model["Pressure"].GetInt32();

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
            IStructure model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model["Pressure"].GetInt32();

            pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
            previousPressure = pressure;
        }

        await TurnHeaterOffAsync().ConfigureAwait(false);

        for (int i = 0; i < 5; i++)
        {
            FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1000);
            IStructure model = await GetBoilerModelAsync().ConfigureAwait(false);
            int pressure = model["Pressure"].GetInt32();

            pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
            previousPressure = pressure;
        }
    }

    [TestCase]
    public async Task Heater_CanbeWritten()
    {
        var newValue = await GetBoilerModelAsync().ConfigureAwait(false);
        newValue["Pressure"] = 42_000;
        var nodeId = BoilerStatusId;
        var statusCode = await WriteValueAsync(nodeId, new ExtensionObject((IEncodeable)newValue)).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);
        var currentValue = await GetBoilerModelAsync().ConfigureAwait(false);
        currentValue["Pressure"].GetInt32().Should().Be(42_000);
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

    private async Task<IStructure> GetBoilerModelAsync()
    {
        var nodeId = BoilerStatusId;
        var value = (await ReadDataValueAsync(nodeId).ConfigureAwait(false)).WrappedValue;
        return ReadStructure(value);
    }

    private static IStructure ReadStructure(Variant value)
    {
        value.TryGetStructure(out IEncodeable body).Should().BeTrue();
        return body.Should().BeAssignableTo<IStructure>().Subject;
    }
}
