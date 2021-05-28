namespace OpcPlc.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    /// <summary>
    /// Tests for OPC-UA Monitoring for Events.
    /// </summary>
    [TestFixture]
    public class EventMonitoringTests : SubscriptionTestsBase
    {
        private NodeId _eventType;

        public EventMonitoringTests() : base(new[] { "--simpleevents" })
        {
        }

        [SetUp]
        public void CreateMonitoredItem()
        {
            _eventType = ToNodeId(SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType);

            SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

            // add condition fields to retrieve selected event.
            var filter = (EventFilter)MonitoredItem.Filter;
            var whereClause = filter.WhereClause;
            whereClause.Push(FilterOperator.OfType, _eventType);

            AddMonitoredItem();
        }

        [Test]
        public void EventSubscribed_FiresNotification()
        {
            // Arrange
            ClearEvents();

            // Act
            // Event is fired every 3 seconds
            FireTimersWithPeriod(3000, 5);

            // Assert
            var events = ReceiveEvents(6);
            var values = events
                .Select(a => (EventFieldList)a.NotificationValue)
                .Select(EventFieldListToDictionary);
            foreach (var value in values)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceNode"] = Server,
                    ["/SourceName"] = "System",
                });
                value.Should().ContainKey("/Message")
                    .WhichValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().MatchRegex("^The system cycle '\\d+' has started\\.$");
            }
        }
    }
}