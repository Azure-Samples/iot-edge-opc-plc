namespace OpcPlc.Tests
{
    using BoilerModel;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using static System.TimeSpan;

    /// <summary>
    /// Tests for the Boiler, which is a complex type.
    /// </summary>
    [TestFixture]
    public class BoilerTests : SimulatorTestsBase
    {
        public BoilerTests() : base(new[] { "--ctb" })
        {
        }

        [TearDown]
        public new virtual void TearDown()
        {
            TurnHeaterOn();
        }

        [TestCase]
        public void Heater_AtStartUp_IsTurnedOn()
        {
            FireTimersWithPeriod(FromSeconds(1), 1000);

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
            FireTimersWithPeriod(FromSeconds(1), 1000);

            TurnHeaterOff();

            FireTimersWithPeriod(FromSeconds(1), 1000);

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
        public void Heater_WhenRunning_HasRisingPressure()
        {
            int previousPressure = 0;
            for (int i = 0; i < 5; i++)
            {
                FireTimersWithPeriod(FromSeconds(1), 1000);
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
                FireTimersWithPeriod(FromSeconds(1), 1000);
                BoilerDataType model = GetBoilerModel();
                int pressure = model.Pressure;

                pressure.Should().BeGreaterThan(previousPressure, "pressure should build when heater is on");
                previousPressure = pressure;
            }

            TurnHeaterOff();

            for (int i = 0; i < 5; i++)
            {
                FireTimersWithPeriod(FromSeconds(1), 1000);
                BoilerDataType model = GetBoilerModel();
                int pressure = model.Pressure;

                pressure.Should().BeLessThan(previousPressure, "pressure should drop when heater is off");
                previousPressure = pressure;
            }
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
            var nodeId = NodeId.Create(BoilerModel.Variables.Boiler1_BoilerStatus, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
            var value = Session.ReadValue(nodeId).Value;
            return value.Should().BeOfType<ExtensionObject>().Which.Body.Should().BeOfType<BoilerDataType>().Subject;
        }
    }
}