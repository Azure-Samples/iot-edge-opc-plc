// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.DeterministicAlarms.Model;

using Opc.Ua;
using OpcPlc.DeterministicAlarms.Configuration;
using OpcPlc.DeterministicAlarms.SimBackend;
using System;
using System.Text;

public static class SimAlarmNodeModel
{
    public static ConditionState Create(
        ISystemContext context, BaseObjectState source, SimAlarmStateBackend alarm, NodeId branchId)
    {
        ConditionState node;
        if (alarm.AlarmType == AlarmObjectStates.ConditionType)
        {
            node = new ConditionState(source);
        }
        else
        {
            node = alarm.AlarmType switch
            {
                AlarmObjectStates.TripAlarmType => new TripAlarmState(source),
                AlarmObjectStates.LimitAlarmType => new LimitAlarmState(source),
                AlarmObjectStates.OffNormalAlarmType => new OffNormalAlarmState(source),
                _ => new AlarmConditionState(source),
            };
            CreateAlarmSpecificElements(context, (AlarmConditionState)node, branchId);
        }

        CreateCommonFields(context, node, alarm);
        node.Create(
            context, NodeId.Null, new QualifiedName(alarm.Name, source.BrowseName.NamespaceIndex), default, true);
        node.ReferenceTypeId = ReferenceTypeIds.HasComponent;
        node.BranchId ??= PropertyState<NodeId>.With<VariantBuilder>(node, NodeId.Null);
        node.EventType.Value = node.TypeDefinitionId;
        node.SourceNode.Value = source.NodeId;
        node.SourceName.Value = source.SymbolicName;
        node.ConditionName.Value = node.SymbolicName;
        node.Time.Value = DateTime.UtcNow;
        node.ReceiveTime.Value = node.Time.Value;
        node.BranchId.Value = branchId;
        return node;
    }

    private static void CreateAlarmSpecificElements(ISystemContext context, AlarmConditionState node, NodeId branchId)
    {
        node.ConfirmedState = new TwoStateVariableState(node);
        node.Confirm = new AddCommentMethodState(node);
        if (branchId.IsNull)
        {
            node.SuppressedState = new TwoStateVariableState(node);
            node.ShelvingState = new ShelvedStateMachineState(node);
        }

        node.ActiveState = new TwoStateVariableState(node);
        node.ActiveState.TransitionTime = PropertyState<DateTimeUtc>.With<VariantBuilder>(node.ActiveState);
        node.ActiveState.EffectiveDisplayName = PropertyState<LocalizedText>.With<VariantBuilder>(node.ActiveState);
        node.ActiveState.Create(context, NodeId.Null, new QualifiedName(BrowseNames.ActiveState), default, false);
    }

    private static void CreateCommonFields(ISystemContext context, ConditionState node, SimAlarmStateBackend alarm)
    {
        node.SymbolicName = alarm.Name;
        node.Comment = ConditionVariableState<LocalizedText>.With<VariantBuilder>(node);
        node.ClientUserId = PropertyState<string>.With<VariantBuilder>(node);
        node.AddComment = new AddCommentMethodState(node);
        node.EnabledState = new TwoStateVariableState(node);
        node.EnabledState.TransitionTime = PropertyState<DateTimeUtc>.With<VariantBuilder>(node.EnabledState);
        node.EnabledState.EffectiveDisplayName = PropertyState<LocalizedText>.With<VariantBuilder>(node.EnabledState);
        node.EnabledState.Create(context, NodeId.Null, new QualifiedName(BrowseNames.EnabledState), default, false);
        node.BranchId = PropertyState<NodeId>.With<VariantBuilder>(node);
        node.BranchId.Create(context, NodeId.Null, new QualifiedName(BrowseNames.BranchId), default, false);
    }

    public static void Update(
        ISystemContext context, ConditionState node, SimAlarmStateBackend alarm, string eventId = null)
    {
        node.EventId.Value = (ByteString)(eventId != null
            ? Encoding.UTF8.GetBytes(eventId)
            : Guid.NewGuid().ToByteArray());
        node.Time.Value = DateTime.UtcNow;
        node.ReceiveTime.Value = node.Time.Value;
        node.Retain.Value = true;

        if (alarm != null)
        {
            node.Time.Value = alarm.Time;
            node.Message.Value = new LocalizedText(alarm.Reason);
            node.SetComment(context, alarm.Comment, alarm.UserName);
            node.SetSeverity(context, alarm.Severity);
            node.EnabledState.TransitionTime.Value = alarm.EnableTime;
            node.SetEnableState(context, (alarm.State & SimConditionStatesEnum.Enabled) != 0);

            if (node is AlarmConditionState nodeAlarm)
            {
                nodeAlarm.SetAcknowledgedState(context, (alarm.State & SimConditionStatesEnum.Acknowledged) != 0);
                nodeAlarm.SetConfirmedState(context, (alarm.State & SimConditionStatesEnum.Confirmed) != 0);
                nodeAlarm.SetActiveState(context, (alarm.State & SimConditionStatesEnum.Active) != 0);
                nodeAlarm.SetSuppressedState(context, (alarm.State & SimConditionStatesEnum.Suppressed) != 0);
                nodeAlarm.ActiveState.TransitionTime.Value = alarm.ActiveTime;
                if (!nodeAlarm.ActiveState.Id.Value)
                {
                    nodeAlarm.Retain.Value = false;
                }
            }
        }

        if ((alarm.State & SimConditionStatesEnum.Deleted) != 0)
        {
            node.Retain.Value = false;
        }

        if (!node.EnabledState.Id.Value)
        {
            node.Retain.Value = false;
        }
    }
}