namespace OpcPlc.Tests
{
    using BoilerModel;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Tests for the variables defined in the simulator, such as fast-changing and trended nodes.
    /// </summary>
    [TestFixture]
    public class BoilerTests : SimulatorTestsBase
    {
        public BoilerTests() : base(new[] { "--ctb" }) { }

        [TearDown]
        public void TearDown()
        {
            InvokeHeaterMethod("HeaterOn");
        }

        [TestCase]
        public void shouldStartTurnedOn()
        {
            BoilerDataType model = GetBoilerModel();

            BoilerHeaterStateType state = model.HeaterState;
            BoilerTemperatureType temperature = model.Temperature;
            int pressure = model.Pressure;

            state.Should().Be(BoilerHeaterStateType.On, "heater should start in 'on' state");
            pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

            temperature.Top.Should().Be(20, "temperature is not changing in unit tests");
            temperature.Bottom.Should().Be(20, "temperature is not changing in unit tests");
        }

        [TestCase]
        public void shouldHaveRisingPressureOverTime()
        {
            BoilerDataType model = GetBoilerModel();
            int pressure = model.Pressure;
            Console.WriteLine(pressure);

            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);
            FireTimersWithPeriod(100u, 1000);

            model = GetBoilerModel();
            pressure = model.Pressure;
            Console.WriteLine(pressure);
        }

        [TestCase]
        public void shouldTurnOffWhenRequested()
        {
            InvokeHeaterMethod("HeaterOff");

            BoilerDataType model = GetBoilerModel();

            BoilerHeaterStateType state = model.HeaterState;
            BoilerTemperatureType temperature = model.Temperature;
            int pressure = model.Pressure;

            state.Should().Be(BoilerHeaterStateType.Off, "heater should have been turned off");
            pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

            temperature.Top.Should().Be(20, "temperature is not changing in unit tests");
            temperature.Bottom.Should().Be(20, "temperature is not changing in unit tests");
        }

        private void InvokeHeaterMethod(string methodName)
        {
            var methodNode = NodeId.Create(methodName, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
            Session.Call(GetOpcPlcNodeId("Methods"), methodNode);
        }

        private BoilerDataType GetBoilerModel()
        {
            var nodeId = NodeId.Create(BoilerModel.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
            var value = Session.ReadValue(nodeId).Value;
            return (value as ExtensionObject).Body as BoilerModel.BoilerDataType;
        }

    }
}