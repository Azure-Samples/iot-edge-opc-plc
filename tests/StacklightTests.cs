namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the Stacklight plugin, which uses IA companion spec types for DI discovery.
/// </summary>
[TestFixture]
public class StacklightTests : SimulatorTestsBase
{
    // IA companion spec type NodeId numeric identifiers.
    private const uint IaStacklightTypeId = 1010;
    private const uint IaStackElementLightTypeId = 1006;
    private const uint IaStacklightOperationModeId = 3002;
    private const uint IaSignalColorId = 3004;
    private const uint IaSignalModeLightId = 3005;

    public StacklightTests() : base(["--sl"])
    {
    }

    private ushort IaNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.IA);

    private NodeId StacklightNodeId => GetOpcPlcNodeId("Stacklight");

    private async Task<NodeId> BrowseTypeDefinitionAsync(NodeId nodeId)
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = nodeId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition,
            IncludeSubtypes = false,
            NodeClassMask = (uint)NodeClass.ObjectType,
            ResultMask = (uint)BrowseResultMask.TargetInfo,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        var references = results.Results[0].References;
        references.Should().ContainSingle("node should have exactly one HasTypeDefinition reference");

        return ExpandedNodeId.ToNodeId(references[0].NodeId, Session.NamespaceUris);
    }

    [Test]
    public async Task Stacklight_HasCorrectTypeDefinition()
    {
        var expectedTypeDefNodeId = new NodeId(IaStacklightTypeId, IaNamespaceIndex);
        var actualTypeDefNodeId = await BrowseTypeDefinitionAsync(StacklightNodeId).ConfigureAwait(false);
        actualTypeDefNodeId.Should().Be(expectedTypeDefNodeId, "Stacklight should be typed as IA StacklightType for DI discovery");
    }

    [Test]
    public async Task Stacklight_StacklightMode_HasCorrectDataType()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");
        var node = await Session.ReadNodeAsync(stacklightModeNodeId).ConfigureAwait(false);

        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        var expectedDataTypeNodeId = new NodeId(IaStacklightOperationModeId, IaNamespaceIndex);

        var actualDataTypeNodeId = ExpandedNodeId.ToNodeId(variableNode.DataType, Session.NamespaceUris);
        actualDataTypeNodeId.Should().Be(expectedDataTypeNodeId, "StacklightMode should use IA StacklightOperationMode data type");
    }

    [Test]
    public async Task Stacklight_StacklightMode_DefaultValueIsRed()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");
        var value = Convert.ToInt32((await ReadDataValueAsync(stacklightModeNodeId).ConfigureAwait(false)).Value);

        value.Should().Be(0, "default StacklightMode should be 0 (Red lamp active)");
    }

    [Test]
    public async Task Stacklight_StacklightMode_IsWritable()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");

        // Write value 2 (Green).
        var statusCode = await WriteValueAsync(stacklightModeNodeId, 2).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);

        var value = Convert.ToInt32((await ReadDataValueAsync(stacklightModeNodeId).ConfigureAwait(false)).Value);
        value.Should().Be(2);

        // Restore to 0 (Red).
        await WriteValueAsync(stacklightModeNodeId, 0).ConfigureAwait(false);
    }

    [TestCase("Red", 0)]
    [TestCase("Yellow", 1)]
    [TestCase("Green", 2)]
    public async Task Stacklight_LampElement_HasCorrectTypeDefinition(string colorName, int index)
    {
        var lampNodeId = GetOpcPlcNodeId($"Stacklight_Lamp_{colorName}");
        var expectedTypeDefNodeId = new NodeId(IaStackElementLightTypeId, IaNamespaceIndex);
        var actualTypeDefNodeId = await BrowseTypeDefinitionAsync(lampNodeId).ConfigureAwait(false);
        actualTypeDefNodeId.Should().Be(expectedTypeDefNodeId, $"Lamp_{colorName} should be typed as IA StackElementLightType");
    }

    [TestCase("Red", 1)]
    [TestCase("Yellow", 2)]
    [TestCase("Green", 3)]
    public async Task Stacklight_LampElement_NumberInList(string colorName, int expectedNumber)
    {
        var nodeId = GetOpcPlcNodeId($"Stacklight_Lamp_{colorName}_NumberInList");
        var value = Convert.ToInt32((await ReadDataValueAsync(nodeId).ConfigureAwait(false)).Value);

        value.Should().Be(expectedNumber, $"Lamp_{colorName} NumberInList should be {expectedNumber}");
    }

    [TestCase("Red", 1)]
    [TestCase("Yellow", 4)]
    [TestCase("Green", 2)]
    public async Task Stacklight_LampElement_SignalColor(string colorName, int expectedColor)
    {
        var nodeId = GetOpcPlcNodeId($"Stacklight_Lamp_{colorName}_SignalColor");

        // Verify data type is IA SignalColor enum.
        var node = await Session.ReadNodeAsync(nodeId).ConfigureAwait(false);
        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        var expectedDataTypeNodeId = new NodeId(IaSignalColorId, IaNamespaceIndex);
        var actualDataTypeNodeId = ExpandedNodeId.ToNodeId(variableNode.DataType, Session.NamespaceUris);
        actualDataTypeNodeId.Should().Be(expectedDataTypeNodeId, $"SignalColor should use IA SignalColor data type");

        // Verify value.
        var value = Convert.ToInt32((await ReadDataValueAsync(nodeId).ConfigureAwait(false)).Value);
        value.Should().Be(expectedColor, $"Lamp_{colorName} SignalColor should be {expectedColor}");
    }

    [TestCase("Red", 0)]
    [TestCase("Yellow", 0)]
    [TestCase("Green", 0)]
    public async Task Stacklight_LampElement_SignalMode(string colorName, int expectedMode)
    {
        var nodeId = GetOpcPlcNodeId($"Stacklight_Lamp_{colorName}_SignalMode");

        // Verify data type is IA SignalModeLight enum.
        var node = await Session.ReadNodeAsync(nodeId).ConfigureAwait(false);
        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        var expectedDataTypeNodeId = new NodeId(IaSignalModeLightId, IaNamespaceIndex);
        var actualDataTypeNodeId = ExpandedNodeId.ToNodeId(variableNode.DataType, Session.NamespaceUris);
        actualDataTypeNodeId.Should().Be(expectedDataTypeNodeId, $"SignalMode should use IA SignalModeLight data type");

        // Verify value.
        var value = Convert.ToInt32((await ReadDataValueAsync(nodeId).ConfigureAwait(false)).Value);
        value.Should().Be(expectedMode, $"Lamp_{colorName} SignalMode should be {expectedMode} (Continuous)");
    }

    [Test]
    public async Task Stacklight_DefaultState_RedLampOn()
    {
        // After initial ApplyStacklightMode (mode=0=Red), only the red lamp should be on.
        var redSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Red_SignalOn")).ConfigureAwait(false)).Value;
        var yellowSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Yellow_SignalOn")).ConfigureAwait(false)).Value;
        var greenSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Green_SignalOn")).ConfigureAwait(false)).Value;

        redSignalOn.Should().BeTrue("Red lamp should be on in default mode");
        yellowSignalOn.Should().BeFalse("Yellow lamp should be off in default mode");
        greenSignalOn.Should().BeFalse("Green lamp should be off in default mode");
    }

    [Test]
    public async Task Stacklight_WriteModeYellow_SwitchesActiveLamp()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");

        // Set mode to 1 (Yellow) and fire timer to apply.
        await WriteValueAsync(stacklightModeNodeId, 1).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var redSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Red_SignalOn")).ConfigureAwait(false)).Value;
        var yellowSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Yellow_SignalOn")).ConfigureAwait(false)).Value;
        var greenSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Green_SignalOn")).ConfigureAwait(false)).Value;

        redSignalOn.Should().BeFalse("Red lamp should be off in Yellow mode");
        yellowSignalOn.Should().BeTrue("Yellow lamp should be on in Yellow mode");
        greenSignalOn.Should().BeFalse("Green lamp should be off in Yellow mode");

        // Restore to 0 (Red).
        await WriteValueAsync(stacklightModeNodeId, 0).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);
    }

    [Test]
    public async Task Stacklight_WriteModeGreen_SwitchesActiveLamp()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");

        // Set mode to 2 (Green) and fire timer to apply.
        await WriteValueAsync(stacklightModeNodeId, 2).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var redSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Red_SignalOn")).ConfigureAwait(false)).Value;
        var yellowSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Yellow_SignalOn")).ConfigureAwait(false)).Value;
        var greenSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Green_SignalOn")).ConfigureAwait(false)).Value;

        redSignalOn.Should().BeFalse("Red lamp should be off in Green mode");
        yellowSignalOn.Should().BeFalse("Yellow lamp should be off in Green mode");
        greenSignalOn.Should().BeTrue("Green lamp should be on in Green mode");

        // Restore to 0 (Red).
        await WriteValueAsync(stacklightModeNodeId, 0).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);
    }

    [Test]
    public async Task Stacklight_InvalidMode_AllLampsOff()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");

        // Set an invalid mode (99) and fire timer.
        await WriteValueAsync(stacklightModeNodeId, 99).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var redSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Red_SignalOn")).ConfigureAwait(false)).Value;
        var yellowSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Yellow_SignalOn")).ConfigureAwait(false)).Value;
        var greenSignalOn = (bool)(await ReadDataValueAsync(GetOpcPlcNodeId("Stacklight_Lamp_Green_SignalOn")).ConfigureAwait(false)).Value;

        redSignalOn.Should().BeFalse("All lamps should be off for invalid mode");
        yellowSignalOn.Should().BeFalse("All lamps should be off for invalid mode");
        greenSignalOn.Should().BeFalse("All lamps should be off for invalid mode");

        // Restore to 0 (Red).
        await WriteValueAsync(stacklightModeNodeId, 0).ConfigureAwait(false);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);
    }

    [Test]
    public async Task Stacklight_LampElements_ConnectedViaHasOrderedComponent()
    {
        // Browse the Stacklight node to verify lamp elements use HasOrderedComponent references.
        var browseDescription = new BrowseDescription
        {
            NodeId = StacklightNodeId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasOrderedComponent,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        var references = results.Results[0].References;
        references.Should().HaveCount(3, "Stacklight should have 3 lamp elements connected via HasOrderedComponent");

        var displayNames = references.Select(r => r.DisplayName.Text).ToList();
        displayNames.Should().Contain("Lamp_Red");
        displayNames.Should().Contain("Lamp_Yellow");
        displayNames.Should().Contain("Lamp_Green");
    }

    [Test]
    public async Task Stacklight_StacklightMode_BrowseNameUsesIaNamespace()
    {
        var stacklightModeNodeId = GetOpcPlcNodeId("Stacklight_StacklightMode");
        var node = await Session.ReadNodeAsync(stacklightModeNodeId).ConfigureAwait(false);

        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        variableNode.BrowseName.NamespaceIndex.Should().Be(IaNamespaceIndex, "StacklightMode BrowseName should use the IA namespace");
        variableNode.BrowseName.Name.Should().Be("StacklightMode");
    }
}
