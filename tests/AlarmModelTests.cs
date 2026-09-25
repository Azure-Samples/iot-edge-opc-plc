namespace OpcPlc.Tests;

using FluentAssertions;
using global::AlarmCondition;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.DeterministicAlarms.Configuration;
using OpcPlc.DeterministicAlarms.Model;
using OpcPlc.DeterministicAlarms.SimBackend;
using System;
using System.Collections.Generic;
using System.Text;

[TestFixture]
public class AlarmModelTests
{
    [TestCase("Plant/Area1", (ushort)2)]
    [TestCase("Area?Name", (ushort)4)]
    public void ModelIdentifiers_RoundTripAreaAndSource(string name, ushort namespaceIndex)
    {
        NodeId area = ModelUtils.ConstructIdForArea(name, namespaceIndex);
        NodeId source = ModelUtils.ConstructIdForSource(name, namespaceIndex);

        ParsedNodeId.Parse(area).RootId.Should().Be(name);
        ParsedNodeId.Parse(area).RootType.Should().Be(ModelUtils.Area);
        ParsedNodeId.Parse(source).RootId.Should().Be(name);
        ParsedNodeId.Parse(source).RootType.Should().Be(ModelUtils.Source);
        area.NamespaceIndex.Should().Be(namespaceIndex);
        source.NamespaceIndex.Should().Be(namespaceIndex);
        area.Should().NotBe(source);
    }

    [TestCase("Source", "Alarm", "Source?Alarm")]
    [TestCase("Source?Alarm", "Comment", "Source?Alarm/Comment")]
    public void ComponentIdentifier_PreservesExistingPath(string parentId, string name, string expected)
    {
        var parent = new BaseObjectState(null) { NodeId = new NodeId(parentId, 2) };
        var child = new BaseObjectState(parent) { SymbolicName = name };

        NodeId result = ModelUtils.ConstructIdForComponent(child, 3);

        result.Should().Be(new NodeId(expected, 3));
    }

    [Test]
    public void ComponentIdentifier_RejectsMissingOrNonStringParent()
    {
        ModelUtils.ConstructIdForComponent(null, 2).IsNull.Should().BeTrue();
        NodeId[] parentIds =
        [
            NodeId.Null,
            new NodeId(42u, 2),
            new NodeId(Guid.NewGuid(), 2),
            new NodeId((ByteString)new byte[] { 1, 2 }, 2)
        ];
        foreach (NodeId parentId in parentIds)
        {
            var parent = new BaseObjectState(null) { NodeId = parentId };
            var child = new BaseObjectState(parent) { SymbolicName = "Alarm" };
            ModelUtils.ConstructIdForComponent(child, 2).IsNull.Should().BeTrue();
        }
    }

    [Test]
    public void ComponentIdentifier_ParentlessNodeKeepsItsId()
    {
        var node = new BaseObjectState(null) { NodeId = new NodeId("Root", 2) };

        ModelUtils.ConstructIdForComponent(node, 3).Should().Be(node.NodeId);
    }

    [TestCase(AlarmObjectStates.ConditionType, typeof(ConditionState))]
    [TestCase(AlarmObjectStates.AlarmConditionType, typeof(AlarmConditionState))]
    [TestCase(AlarmObjectStates.TripAlarmType, typeof(TripAlarmState))]
    [TestCase(AlarmObjectStates.LimitAlarmType, typeof(LimitAlarmState))]
    [TestCase(AlarmObjectStates.OffNormalAlarmType, typeof(OffNormalAlarmState))]
    public void Create_InitializesTypedConditionProperties(AlarmObjectStates type, Type expectedType)
    {
        var fixture = new ModelFixture(type);
        ConditionState node = SimAlarmNodeModel.Create(fixture.Context, fixture.Source, fixture.Alarm, NodeId.Null);

        node.Should().BeOfType(expectedType);
        node.SourceNode.Value.Should().Be(fixture.Source.NodeId);
        node.SourceName.Value.Should().Be("Source");
        node.ConditionName.Value.Should().Be("Alarm");
        node.EventType.Value.Should().Be(node.TypeDefinitionId);
        node.BranchId.Value.IsNull.Should().BeTrue();
        node.BranchId.ValueRank.Should().Be(ValueRanks.Scalar);
        node.ClientUserId.Should().BeAssignableTo<PropertyState<string>>();
        node.Comment.Should().BeAssignableTo<ConditionVariableState<LocalizedText>>();
        node.EnabledState.TransitionTime.Should().BeAssignableTo<PropertyState<DateTimeUtc>>();
        node.EnabledState.EffectiveDisplayName.Should().BeAssignableTo<PropertyState<LocalizedText>>();
        node.BrowseName.Should().Be(new QualifiedName("Alarm", 2));
        node.NodeId.NamespaceIndex.Should().Be(2);
        node.ReferenceTypeId.Should().Be(ReferenceTypeIds.HasComponent);
        if (node is AlarmConditionState alarmNode)
        {
            alarmNode.ActiveState.TransitionTime.Should().BeAssignableTo<PropertyState<DateTimeUtc>>();
            alarmNode.ConfirmedState.Should().NotBeNull();
            alarmNode.Confirm.Should().NotBeNull();
            alarmNode.SuppressedState.Should().NotBeNull();
            alarmNode.ShelvingState.Should().NotBeNull();
        }
    }

    [Test]
    public void Create_PreservesExplicitBranchIdentifier()
    {
        var fixture = new ModelFixture(AlarmObjectStates.TripAlarmType);
        var branchId = new NodeId("Branch1", 2);

        ConditionState node = SimAlarmNodeModel.Create(fixture.Context, fixture.Source, fixture.Alarm, branchId);

        node.BranchId.Value.Should().Be(branchId);
        var sourceChildren = new List<BaseInstanceState>();
        fixture.Source.GetChildren(fixture.Context, sourceChildren);
        sourceChildren.Should().NotContain(node);
    }

    [TestCase(AlarmObjectStates.TripAlarmType, SimConditionStatesEnum.Enabled | SimConditionStatesEnum.Active, true)]
    [TestCase(AlarmObjectStates.TripAlarmType, SimConditionStatesEnum.Enabled, false)]
    [TestCase(AlarmObjectStates.TripAlarmType, SimConditionStatesEnum.Active, false)]
    [TestCase(AlarmObjectStates.TripAlarmType,
        SimConditionStatesEnum.Enabled | SimConditionStatesEnum.Active | SimConditionStatesEnum.Deleted, false)]
    [TestCase(AlarmObjectStates.ConditionType, SimConditionStatesEnum.Enabled, false)]
    [TestCase(AlarmObjectStates.ConditionType, SimConditionStatesEnum.Undefined, false)]
    [TestCase(AlarmObjectStates.ConditionType, SimConditionStatesEnum.Enabled | SimConditionStatesEnum.Deleted, false)]
    public void Update_PreservesRetentionRules(AlarmObjectStates type, SimConditionStatesEnum state, bool retained)
    {
        var fixture = new ModelFixture(type);
        ConditionState node = SimAlarmNodeModel.Create(fixture.Context, fixture.Source, fixture.Alarm, NodeId.Null);
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.State), state);

        SimAlarmNodeModel.Update(fixture.Context, node, fixture.Alarm, "event-1");

        node.Retain.Value.Should().Be(retained);
        node.EnabledState.Id.Value.Should().Be(state.HasFlag(SimConditionStatesEnum.Enabled));
        if (node is AlarmConditionState alarmNode)
        {
            alarmNode.ActiveState.Id.Value.Should().Be(state.HasFlag(SimConditionStatesEnum.Active));
        }
    }

    [Test]
    public void Update_PreservesEventPayloadAndStateFlags()
    {
        var fixture = new ModelFixture(AlarmObjectStates.TripAlarmType);
        ConditionState node = SimAlarmNodeModel.Create(fixture.Context, fixture.Source, fixture.Alarm, NodeId.Null);
        var alarmTime = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.Time), alarmTime);
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.ActiveTime), alarmTime.AddSeconds(-1));
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.Reason), "Door Open");
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.Comment), new LocalizedText("Operator comment"));
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.UserName), "operator");
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.Severity), EventSeverity.High);
        SetBackendValue(fixture.Alarm, nameof(SimAlarmStateBackend.State),
            SimConditionStatesEnum.Enabled | SimConditionStatesEnum.Active | SimConditionStatesEnum.Acknowledged |
            SimConditionStatesEnum.Confirmed | SimConditionStatesEnum.Suppressed);

        SimAlarmNodeModel.Update(fixture.Context, node, fixture.Alarm, "V1_DoorOpen-1 (1)");

        node.EventId.Value.ToArray().Should().Equal(Encoding.UTF8.GetBytes("V1_DoorOpen-1 (1)"));
        node.EventId.Value.ToHexString().Should().Be(Convert.ToHexString(node.EventId.Value.Span));
        node.Time.Value.Should().Be((DateTimeUtc)alarmTime);
        node.Message.Value.Should().Be(new LocalizedText("Door Open"));
        node.Comment.Value.Should().Be(new LocalizedText("Operator comment"));
        node.ClientUserId.Value.Should().Be("operator");
        node.Severity.Value.Should().Be((ushort)EventSeverity.High);
        var alarmNode = (AlarmConditionState)node;
        alarmNode.ActiveState.TransitionTime.Value.Should().Be((DateTimeUtc)alarmTime.AddSeconds(-1));
        alarmNode.AckedState.Id.Value.Should().BeTrue();
        alarmNode.ConfirmedState.Id.Value.Should().BeTrue();
        alarmNode.SuppressedState.Id.Value.Should().BeTrue();
    }

    [Test]
    public void Update_ReplacesEventIdOnEachUpdate()
    {
        var fixture = new ModelFixture(AlarmObjectStates.ConditionType);
        ConditionState node = SimAlarmNodeModel.Create(fixture.Context, fixture.Source, fixture.Alarm, NodeId.Null);
        SimAlarmNodeModel.Update(fixture.Context, node, fixture.Alarm);
        ByteString firstEventId = node.EventId.Value;

        SimAlarmNodeModel.Update(fixture.Context, node, fixture.Alarm);

        firstEventId.Span.Length.Should().Be(16);
        node.EventId.Value.Span.Length.Should().Be(16);
        node.EventId.Value.Should().NotBe(firstEventId);
        SimAlarmNodeModel.Update(fixture.Context, node, fixture.Alarm, string.Empty);
        node.EventId.Value.IsNull.Should().BeFalse();
        node.EventId.Value.Span.IsEmpty.Should().BeTrue();
    }

    private static void SetBackendValue<T>(SimAlarmStateBackend alarm, string propertyName, T value)
    {
        typeof(SimAlarmStateBackend).GetProperty(propertyName)!.SetValue(alarm, value);
    }

    private sealed class ModelFixture
    {
        public SystemContext Context { get; }
        public BaseObjectState Source { get; }
        public SimAlarmStateBackend Alarm { get; }

        public ModelFixture(AlarmObjectStates type)
        {
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:application");
            namespaces.GetIndexOrAppend("urn:opcplc:test:alarms");
            uint nodeId = 100;
            var nodeIdFactory = new Mock<INodeIdFactory>();
            nodeIdFactory.Setup(factory => factory.New(It.IsAny<ISystemContext>(), It.IsAny<NodeState>()))
                .Returns(() => new NodeId(++nodeId, 2));
            Context = new SystemContext(null)
            {
                NamespaceUris = namespaces,
                TypeTable = new TypeTable(namespaces),
                NodeIdFactory = nodeIdFactory.Object
            };
            Source = new BaseObjectState(null)
            {
                NodeId = new NodeId("Source", 2),
                BrowseName = new QualifiedName("Source", 2),
                SymbolicName = "Source"
            };
            var backend = new SimSourceNodeBackend();
            backend.CreateAlarms([new Alarm { Id = "alarm-1", Name = "Alarm", ObjectType = type }]);
            Alarm = backend.Alarms["alarm-1"];
        }
    }
}