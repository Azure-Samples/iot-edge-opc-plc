namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using OpcPlc.PluginNodes;
using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Tests the nodes configured via nodesfile.json.
/// </summary>
[TestFixture]
public class UserDefinedNodesTests : SubscriptionTestsBase
{
    [Test]
    public void Configuration_DefaultsAndLooseJsonSyntaxArePreserved()
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            """{"folder":"Root","nodelist":[/* node */{"nodeid":123,},],}""");
        folder.Folder.Should().Be("Root");
        var node = folder.NodeList[0];
        ((object)node.NodeId).Should().Be(123L);
        node.DataType.Should().Be("Int32");
        node.ValueRank.Should().Be(-1);
        node.AccessLevel.Should().Be("CurrentReadOrWrite");
        node.Value.Should().BeNull();
    }

    [TestCase("""{"Folder":"Root","NodeList":[{}]}""")]
    [TestCase("""{"Folder":"Root","NodeList":[{"NodeId":null}]}""")]
    [TestCase("null")]
    public void Configuration_InvalidRequiredValuesAreRejected(string json)
    {
        Action deserialize = () => UserDefinedPluginNodes.DeserializeConfiguration(json);
        deserialize.Should().Throw<JsonException>();
    }

    [TestCase("123", typeof(long), "123")]
    [TestCase("\"string-id\"", typeof(string), "string-id")]
    [TestCase("\"c6522393-5ca1-4547-9551-29d110ca4a3f\"", typeof(string),
        "c6522393-5ca1-4547-9551-29d110ca4a3f")]
    public void Configuration_NodeIdentifiersKeepTheirRuntimeTypes(string jsonId, Type type, string expected)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":{{jsonId}}}]}""");
        object id = folder.NodeList[0].NodeId;
        id.Should().BeOfType(type);
        id.ToString().Should().Be(expected);
    }

    [TestCase("\"text\"", typeof(string), "text")]
    [TestCase("1234", typeof(long), "1234")]
    [TestCase("-4321", typeof(long), "-4321")]
    [TestCase("true", typeof(bool), "True")]
    [TestCase("1.5", typeof(double), null)]
    public void Configuration_ScalarValuesKeepTheirRuntimeTypes(string jsonValue, Type type, string expected)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","Value":{{jsonValue}}}]}""");
        object value = folder.NodeList[0].Value;
        value.Should().BeOfType(type);
        if (expected is not null)
        {
            value.ToString().Should().Be(expected);
        }
        else
        {
            value.Should().Be(1.5d);
        }
    }

    [TestCase("String", """["a","b"]""", typeof(string[]))]
    [TestCase("Boolean", "[true,false]", typeof(bool[]))]
    [TestCase("Float", "[1.5,2.5]", typeof(float[]))]
    [TestCase("UInt32", "[0,4294967295]", typeof(uint[]))]
    [TestCase("Int32", "[-2147483648,2147483647]", typeof(int[]))]
    public void Configuration_ArrayValuesUseTheConfiguredType(string dataType, string jsonValue, Type type)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"{{dataType}}","ValueRank":1,"Value":{{jsonValue}}}]}""");
        var node = folder.NodeList[0];
        object value = UserDefinedPluginNodes.UpdateArrayValue(node, (JsonElement)node.Value);
        value.Should().BeOfType(type);
        value.Should().BeEquivalentTo(JsonSerializer.Deserialize(jsonValue, type));
    }

    [Test]
    public void Configuration_ArrayConversionsPreservePrimitiveCoercion()
    {
        using var integers = JsonDocument.Parse("""["1","-2"]""");
        using var booleans = JsonDocument.Parse("""["true",0,1]""");
        using var strings = JsonDocument.Parse("""[123,true,null]""");
        UserDefinedPluginNodes.UpdateArrayValue(new ConfigNode { DataType = "Int32" }, integers.RootElement)
            .Should().BeEquivalentTo(new[] { 1, -2 });
        UserDefinedPluginNodes.UpdateArrayValue(new ConfigNode { DataType = "Boolean" }, booleans.RootElement)
            .Should().BeEquivalentTo(new[] { true, false, true });
        UserDefinedPluginNodes.UpdateArrayValue(new ConfigNode { DataType = "String" }, strings.RootElement)
            .Should().BeEquivalentTo(new[] { "123", "True", null });
    }

    [Test]
    public void Configuration_ExplicitSettingsAndTypeMetadataArePreserved()
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            """
            {"$type":"Ignored.Type","NodeList":[
                {"NodeId":"id","DataType":"Float","ValueRank":1,"AccessLevel":"CurrentRead"}]}
            """);
        var node = folder.NodeList[0];
        node.DataType.Should().Be("Float");
        node.ValueRank.Should().Be(1);
        node.AccessLevel.Should().Be("CurrentRead");
    }

    // Set any cmd params needed for the plc server explicitly
    public UserDefinedNodesTests() : base(["--nodesfile=nodesfile.json"])
    {
    }

    [TestCase("1030", "Some value")]
    [TestCase("1031", 1234L)]
    [TestCase("1032", -4321L)]
    [TestCase("1033", true)]
    public async Task UserDefinedScalarValues_ArePublished(string id, object expected)
    {
        var folder = await FindNodeAsync(
            ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry").ConfigureAwait(false);
        var nodeId = await FindNodeAsync(folder, Namespaces.OpcPlcApplications, id).ConfigureAwait(false);
        var value = await ReadDataValueAsync(nodeId).ConfigureAwait(false);
        Opc.Ua.StatusCode.IsGood(value.StatusCode).Should().BeTrue();
        value.WrappedValue.AsBoxedObject(Opc.Ua.Variant.BoxingBehavior.Legacy).Should().Be(expected);
    }

    [Test]
    public async Task TestUserDefinedNodes()
    {
        var myTelemetryNode = await FindNodeAsync(ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry").ConfigureAwait(false);
        myTelemetryNode.Should().NotBeNull();

        var childNode = await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "Child").ConfigureAwait(false);
        childNode.Should().NotBeNull();

        (await FindNodeAsync(childNode, Namespaces.OpcPlcApplications, "9999").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(childNode, Namespaces.OpcPlcApplications, "Guid").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1023").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "aRMS").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1025").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1026").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1027").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1029").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1030").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1031").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1032").ConfigureAwait(false))
            .Should().NotBeNull();

        (await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1033").ConfigureAwait(false))
            .Should().NotBeNull();

        var arrayNodeId = await FindNodeAsync(myTelemetryNode, Namespaces.OpcPlcApplications, "1048").ConfigureAwait(false);
        arrayNodeId.Should().NotBeNull();
        (await ReadDataValueAsync(arrayNodeId).ConfigureAwait(false)).WrappedValue.AsBoxedObject(Opc.Ua.Variant.BoxingBehavior.Legacy).Should().BeEquivalentTo(new int[] { 1, 2, 3, 4, 5 });
    }
}
