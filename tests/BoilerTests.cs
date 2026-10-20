namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using OpcPlc.PluginNodes;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [Test]
    public async Task HeaterMethod_PublishesNewSnapshotWithoutSimulationTickAsync()
    {
        var manager = PlcServer.CurrentInstance.NodeManager.AsyncNodeManagers.OfType<PlcNodeManager>().Single();
        var variable = manager.FindPredefinedNode<BaseDataVariableState>(BoilerStatusId);
        Variant before = variable.Value;
        DateTimeUtc timestamp = variable.Timestamp;
        NodeStateChangeMasks observed = NodeStateChangeMasks.None;
        var previousHandler = variable.OnStateChanged;
        variable.OnStateChanged = (context, node, changes) =>
        {
            observed |= changes;
            previousHandler?.Invoke(context, node, changes);
        };
        try
        {
            await TurnHeaterOffAsync().ConfigureAwait(false);

            ReadStructure(before)["HeaterState"].GetInt32().Should().Be(1);
            ReadStructure(variable.Value)["HeaterState"].GetInt32().Should().Be(0);
            observed.Should().HaveFlag(NodeStateChangeMasks.Value);
            variable.Timestamp.ToDateTime().Should().BeOnOrAfter(timestamp.ToDateTime());
            (await GetBoilerModelAsync().ConfigureAwait(false))["HeaterState"].GetInt32().Should().Be(0);
        }
        finally
        {
            variable.OnStateChanged = previousHandler;
        }
    }

    [Test]
    public async Task HeaterMethod_WaitsForSimulationPublicationAsync()
    {
        var manager = PlcServer.CurrentInstance.NodeManager.AsyncNodeManagers.OfType<PlcNodeManager>().Single();
        var variable = manager.FindPredefinedNode<BaseDataVariableState>(BoilerStatusId);
        var plugin = PluginNodes.OfType<ComplexTypeBoilerPluginNode>().Single();
        var method = manager.FindPredefinedNode<MethodState>(
            NodeId.Create("HeaterOff", OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris));
        using var release = new ManualResetEventSlim();
        using var methodStarted = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var previousHandler = variable.OnStateChanged;
        int notifications = 0;
        variable.OnStateChanged = (context, node, changes) =>
        {
            if (Interlocked.Increment(ref notifications) == 1)
            {
                entered.TrySetResult();
                release.Wait(TimeSpan.FromSeconds(10));
            }
            previousHandler?.Invoke(context, node, changes);
        };
        Task update = Task.Run(() => plugin.UpdateBoiler1(null, null));
        Task command = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            command = Task.Run(() =>
            {
                methodStarted.Set();
                method.OnCallMethod(manager.SystemContext, method, [], new List<Variant>())
                    .Should().Be(ServiceResult.Good);
            });
            methodStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            var completed = await Task.WhenAny(command, Task.Delay(TimeSpan.FromMilliseconds(100)))
                .ConfigureAwait(false);
            completed.Should().NotBe(command, "commands must wait for the simulation's compound update");
        }
        finally
        {
            release.Set();
            try
            {
                await Task.WhenAll(update, command ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }
            finally
            {
                variable.OnStateChanged = previousHandler;
            }
        }
        ReadStructure(variable.Value)["HeaterState"].GetInt32().Should().Be(0);
        notifications.Should().Be(2);
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
