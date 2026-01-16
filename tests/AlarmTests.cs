namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestFixture]
public class AlarmTests : SubscriptionTestsBase
{
    private NodeId _eventType;

    public AlarmTests() : base(["--alm"])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        _eventType = ToNodeId(ObjectTypes.TripAlarmType);

        var areaNode = await FindNodeAsync(Server, OpcPlc.Namespaces.OpcPlcAlarmsInstance, "Green", "East", "Blue").ConfigureAwait(false);
        var southMotor = await FindNodeAsync(areaNode, OpcPlc.Namespaces.OpcPlcAlarmsInstance, "SouthMotor").ConfigureAwait(false);

        SetUpMonitoredItem(areaNode, NodeClass.Object, Attributes.EventNotifier);

        // add condition fields to retrieve selected event.
        var filter = (EventFilter)MonitoredItem.Filter;
        var whereClause = filter.WhereClause;
        var element1 = whereClause.Push(FilterOperator.OfType, _eventType);
        var element2 = whereClause.Push(FilterOperator.InList,
            new SimpleAttributeOperand {
                AttributeId = Attributes.Value,
                TypeDefinitionId = ObjectTypeIds.BaseEventType,
                BrowsePath = new QualifiedName[] { BrowseNames.SourceNode },
            },
            new LiteralOperand {
                Value = new Variant(southMotor)
            });

        whereClause.Push(FilterOperator.And, element1, element2);

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void AlarmEventSubscribed_FiresNotification()
    {
        // Assert
        var events = ReceiveEventsAsDictionary(1);
        foreach (var value in events)
        {
            value.Should().Contain(new Dictionary<string, object> {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "SouthMotor",
            });
        }
    }
}
