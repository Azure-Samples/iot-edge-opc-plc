namespace OpcPlc.Tests
{
    using System.Collections.Generic;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    public class AlarmTests : SubscriptionTestsBase
    {
        private NodeId _eventType;

        public AlarmTests() : base(new[] { "--alm" })
        {
        }

        [SetUp]
        public void CreateMonitoredItem()
        {
            _eventType = ToNodeId(ObjectTypes.TripAlarmType);

            var areaNode = FindNode(Server, OpcPlc.Namespaces.OpcPlcAlarmsInstance, "Green", "East", "Blue");
            var southMotor = FindNode(areaNode, OpcPlc.Namespaces.OpcPlcAlarmsInstance, "SouthMotor");

            SetUpMonitoredItem(areaNode, NodeClass.Object, Attributes.EventNotifier);

            // add condition fields to retrieve selected event.
            var filter = (EventFilter)MonitoredItem.Filter;
            var whereClause = filter.WhereClause;
            var element1 = whereClause.Push(FilterOperator.OfType, _eventType);
            var element2 = whereClause.Push(FilterOperator.InList,
                new SimpleAttributeOperand
                {
                    AttributeId = Attributes.Value,
                    TypeDefinitionId = ObjectTypeIds.BaseEventType,
                    BrowsePath = new QualifiedName[] { BrowseNames.SourceNode },
                },
                new LiteralOperand
                {
                    Value = new Variant(southMotor)
                });

            whereClause.Push(FilterOperator.And, element1, element2);

            AddMonitoredItem();
        }

        [Test]
        public void AlarmEventSubscribed_FiresNotification()
        {
            // Assert
            var events = ReceiveEventsAsDictionary(1);
            foreach (var value in events)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "SouthMotor",
                });
            }
        }
    }
}