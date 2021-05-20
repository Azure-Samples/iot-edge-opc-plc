namespace OpcPlc.Tests
{
    using BoilerModel;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

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

            FireTimersWithPeriod(1000u, 1000);

            BoilerDataType model = GetBoilerModel();

            BoilerHeaterStateType state = model.HeaterState;
            BoilerTemperatureType temperature = model.Temperature;
            int pressure = model.Pressure;

            state.Should().Be(BoilerHeaterStateType.On, "heater should start in 'on' state");
            pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

            temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
            temperature.Bottom.Should().Be(pressure - 100_000, "btoom is always 100,000 less than pressure. Pressure: {0}", pressure);
        }

        [TestCase]
        public void shouldTurnOffWhenRequested()
        {
            InvokeHeaterMethod("HeaterOff");

            FireTimersWithPeriod(1000u, 1000);

            BoilerDataType model = GetBoilerModel();

            BoilerHeaterStateType state = model.HeaterState;
            BoilerTemperatureType temperature = model.Temperature;
            int pressure = model.Pressure;

            state.Should().Be(BoilerHeaterStateType.Off, "heater should have been turned off");
            pressure.Should().BeGreaterThan(10_000, "pressure should start at 10k and get higher");

            temperature.Top.Should().Be(pressure - 100_005, "top is always 100,005 less than pressure. Pressure: {0}", pressure);
            temperature.Bottom.Should().Be(pressure - 100_000, "btoom is always 100,000 less than pressure. Pressure: {0}", pressure);
        }

        [TestCase]
        public void RunningHeater_shouldHaveRisingPressureOverTime()
        {
            int previousPressure = 0;
            for (int i = 0; i < 5; i++)
            {
                FireTimersWithPeriod(1000u, 1000);
                BoilerDataType model = GetBoilerModel();
                int pressure = model.Pressure;

                pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
                previousPressure = pressure;
            }
        }

        [TestCase]
        public void StoppedHeater_shouldHaveDecreasingPressureOverTime()
        {
            int previousPressure = 0;
            for (int i = 0; i < 10; i++)
            {
                FireTimersWithPeriod(1000u, 1000);
                BoilerDataType model = GetBoilerModel();
                int pressure = model.Pressure;

                pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
                previousPressure = pressure;
            }

            InvokeHeaterMethod("HeaterOff");

            for (int i = 0; i < 5; i++)
            {
                FireTimersWithPeriod(1000u, 1000);
                BoilerDataType model = GetBoilerModel();
                int pressure = model.Pressure;

                pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
                previousPressure = pressure;
            }
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