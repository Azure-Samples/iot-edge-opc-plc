namespace OpcPlc.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using static System.TimeSpan;

    [TestFixture]
    public class DeterministicAlarmsTests : SubscriptionTestsBase
    {
        public DeterministicAlarmsTests() : base(new[]
        {
            "--dalm",
            "--dalmfile=DeterministicAlarmsTests/dalm001.json",
        })
        {
        }

        [SetUp]
        public void CreateMonitoredItem()
        {
            SetUpMonitoredItem(Objects.Server, NodeClass.Object, Attributes.EventNotifier);

            AddMonitoredItem();
        }

        [Test]
        public void FiresEventSequence()
        {
            var waitUntilStartInSeconds = FromSeconds(9); // value in dalm001.json file

            FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/EventId"] = "V1_DoorOpen(1)",
                    ["/EventType"] = ToNodeId(ObjectTypes.TripAlarmType),
                    ["/SourceNode"] = AlarmNodeId("VendingMachine1"),
                    ["/SourceName"] = "VendingMachine1",
                    ["/Message"] = new LocalizedText("Door Open"),
                    ["/Severity"] = EventSeverity.High,
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);

            FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/EventId"] = "V2_LightOff(1)",
                    ["/EventType"] = ToNodeId(ObjectTypes.OffNormalAlarmType),
                    ["/SourceNode"] = AlarmNodeId("VendingMachine2"),
                    ["/SourceName"] = "VendingMachine2",
                    ["/Message"] = new LocalizedText("Light Off in machine"),
                    ["/Severity"] = EventSeverity.Medium,
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);

            FireTimersWithPeriodAndReceiveEvents(FromSeconds(7), 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/EventId"] = "V1_DoorOpen(1)",
                    ["/EventType"] = ToNodeId(ObjectTypes.TripAlarmType),
                    ["/SourceNode"] = AlarmNodeId("VendingMachine1"),
                    ["/SourceName"] = "VendingMachine1",
                    ["/Message"] = new LocalizedText("Door Closed"),
                    ["/Severity"] = EventSeverity.Medium,
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);

            FireTimersWithPeriodAndReceiveEvents(FromSeconds(4), 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/EventId"] = "V1_TemperatureHigh(1)",
                    ["/EventType"] = ToNodeId(ObjectTypes.LimitAlarmType),
                    ["/SourceNode"] = AlarmNodeId("VendingMachine1"),
                    ["/SourceName"] = "VendingMachine1",
                    ["/Message"] = new LocalizedText("Temperature is HIGH"),
                    ["/Severity"] = EventSeverity.High,
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/EventId"] = "V1_DoorOpen(2)",
                    ["/Message"] = new LocalizedText("Door Open"),
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);

            FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), 1)
                .First()
                .Should().Contain(new Dictionary<string, object>
                {
                    ["/Message"] = new LocalizedText("Light Off in machine"),
                });

            FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);

            // At this point, the *runningForSeconds* limit in the JSON file causes execution to stop
            FireTimersWithPeriodAndReceiveEvents(FromSeconds(1), 0);
        }

        private NodeId AlarmNodeId(string identifier)
        {
            return NodeId.Create(identifier, OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance, Session.NamespaceUris);
        }
    }
}