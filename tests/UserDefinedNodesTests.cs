namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Opc.Ua;
using OpcPlc.PluginNodes;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Namespaces = OpcPlc.Namespaces;

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
    [TestCase("\"2026-10-07T12:34:56Z\"", typeof(string), "2026-10-07T12:34:56Z")]
    [TestCase("\"2026-10-07T12:34:56+02:00\"", typeof(string), "2026-10-07T12:34:56+02:00")]
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
    public void Configuration_ScalarValuesKeepTheirRuntimeTypes(string jsonValue, Type type, string expected)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","Value":{{jsonValue}}}]}""");
        object value = folder.NodeList[0].Value;
        value.Should().BeOfType(type);
        value.ToString().Should().Be(expected);
    }

    [TestCase("1.5")]
    [TestCase("9007199254740993.0")]
    [TestCase("184467440737095516150e-1")]
    [TestCase("1.000000000000000000000000000000000001")]
    public void Configuration_NumericTokensPreservePrecisionUntilConversion(string jsonValue)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","Value":{{jsonValue}}}]}""");
        folder.NodeList[0].Value.Should().BeOfType<JsonElement>().Which.GetRawText().Should().Be(jsonValue);
    }

    [TestCase("Double", "1.5", typeof(double), 1.5)]
    [TestCase("Boolean", "1.5", typeof(bool), true)]
    [TestCase("Boolean", "0.0", typeof(bool), false)]
    [TestCase("Variant", "1.5", typeof(double), 1.5)]
    public void Configuration_NumericTokensPreserveNonIntegerConversions(
        string dataType, string jsonValue, Type type, object expected)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"{{dataType}}","Value":{{jsonValue}}}]}""");
        object value = UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]);
        value.Should().BeOfType(type).And.Be(expected);
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
    public void Configuration_DateConversionDependsOnDataType()
    {
        const string text = "2026-10-07T12:34:56Z";
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"{{text}}","Value":"{{text}}","DataType":"String"}]}""");
        var node = folder.NodeList[0];
        UserDefinedPluginNodes.GetScalarValue(node).Should().Be(text);
        node.DataType = "DateTime";
        UserDefinedPluginNodes.GetScalarValue(node)
            .Should().Be(new DateTime(2026, 10, 7, 12, 34, 56, DateTimeKind.Utc));
        ((object)node.NodeId).Should().Be(text);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Registration_CancellationPropagatesAsync(bool preCanceled)
    {
        using var cancellation = new CancellationTokenSource();
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """{"Folder":"Root","NodeList":[]}""").ConfigureAwait(false);
            var logger = new CancelOnFolderLogger(cancellation);
            var plugin = new UserDefinedPluginNodes(new TimeService(), logger);
            var options = new Mono.Options.OptionSet();
            plugin.AddOptions(options);
            options.Parse([$"--nodesfile={path}"]);
            var root = new Opc.Ua.FolderState(null);
            var telemetry = new Opc.Ua.FolderState(root);
            if (preCanceled)
            {
                cancellation.Cancel();
            }
            Func<Task> register = () => plugin.AddToAddressSpaceAsync(
                telemetry, null, null, cancellation.Token).AsTask();

            var failure = await register.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            failure.Which.CancellationToken.Should().Be(cancellation.Token);
            plugin.Nodes.Should().BeEmpty();
            logger.SawFolder.Should().Be(!preCanceled);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class CancelOnFolderLogger(CancellationTokenSource cancellation) : ILogger
    {
        public bool SawFolder { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (formatter(state, exception).StartsWith("Create folder", StringComparison.Ordinal))
            {
                SawFolder = true;
                cancellation.Cancel();
            }
        }
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
    [TestCase("1031", 1234u)]
    [TestCase("1032", -4321)]
    [TestCase("1033", true)]
    public async Task UserDefinedScalarValues_ArePublished(string id, object expected)
    {
        var folder = await FindNodeAsync(
            ObjectsFolder, Namespaces.OpcPlcApplications, "OpcPlc", "MyTelemetry").ConfigureAwait(false);
        var nodeId = await FindNodeAsync(folder, Namespaces.OpcPlcApplications, id).ConfigureAwait(false);
        var value = await ReadDataValueAsync(nodeId).ConfigureAwait(false);
        Opc.Ua.StatusCode.IsGood(value.StatusCode).Should().BeTrue();
        value.WrappedValue.AsBoxedObject(Opc.Ua.Variant.BoxingBehavior.Legacy).Should().Be(expected);
        value.WrappedValue.TypeInfo.Should().Be(TypeInfo.Construct(expected.GetType()));
    }

    [TestCase("SByte", "-128", BuiltInType.SByte)]
    [TestCase("Byte", "255", BuiltInType.Byte)]
    [TestCase("Int16", "-32768", BuiltInType.Int16)]
    [TestCase("UInt16", "65535", BuiltInType.UInt16)]
    [TestCase("Int32", "-2147483648", BuiltInType.Int32)]
    [TestCase("UInt32", "4294967295", BuiltInType.UInt32)]
    [TestCase("Int64", "-9223372036854775808", BuiltInType.Int64)]
    [TestCase("UInt64", "18446744073709551615", BuiltInType.UInt64)]
    [TestCase("Int64", "9007199254740993.0", BuiltInType.Int64)]
    [TestCase("Int64", "-9223372036854775808.0", BuiltInType.Int64)]
    [TestCase("UInt64", "18446744073709551615.0", BuiltInType.UInt64)]
    [TestCase("UInt64", "184467440737095516150e-1", BuiltInType.UInt64)]
    [TestCase("Int32", "1e3", BuiltInType.Int32)]
    [TestCase("Int32", "0e400", BuiltInType.Int32)]
    [TestCase("Float", "1.5", BuiltInType.Float)]
    [TestCase("Double", "1.5", BuiltInType.Double)]
    public void Configuration_ScalarValuesUseTheConfiguredType(
        string dataType, string jsonValue, BuiltInType expectedType)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"{{dataType}}","Value":{{jsonValue}}}]}""");
        Variant value = VariantHelper.CastFrom(UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]));
        value.TypeInfo.BuiltInType.Should().Be(expectedType);
        Convert.ToDecimal(value.AsBoxedObject(), System.Globalization.CultureInfo.InvariantCulture)
            .Should().Be(decimal.Parse(jsonValue, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestCase("Byte", "256")]
    [TestCase("UInt32", "-1")]
    [TestCase("Int32", "2147483648")]
    [TestCase("UInt64", "18446744073709551616")]
    [TestCase("Int64", "9223372036854775808.0")]
    [TestCase("UInt64", "18446744073709551616.0")]
    [TestCase("Int32", "1e400")]
    [TestCase("Int32", "1.00000000000000001")]
    [TestCase("Int32", "2147483647.00000001")]
    [TestCase("Int32", "1e-400")]
    [TestCase("Int32", "1.5")]
    [TestCase("Int32", "1.000000000000000000000000000000000001")]
    [TestCase("Float", "1e40")]
    [TestCase("Float", "-1e40")]
    [TestCase("Double", "1e400")]
    [TestCase("Double", "-1e400")]
    [TestCase("Float", "\"1e40\"")]
    [TestCase("Double", "\"1e400\"")]
    [TestCase("Float", "\"NaN\"")]
    [TestCase("Double", "\"Infinity\"")]
    public void Configuration_ScalarOverflowIsRejected(string dataType, string jsonValue)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"{{dataType}}","Value":{{jsonValue}}}]}""");
        Action convert = () => UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]);
        convert.Should().Throw<OverflowException>();
    }

    [TestCase("\"invalid\"")]
    public void Configuration_InvalidIntegerIsRejected(string jsonValue)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"Int32","Value":{{jsonValue}}}]}""");
        Action convert = () => UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]);
        convert.Should().Throw<FormatException>();
    }

    [TestCase(ValueRanks.Scalar)]
    [TestCase(ValueRanks.Any)]
    [TestCase(ValueRanks.ScalarOrOneDimension)]
    public void Configuration_ScalarCompatibleRanksConvertNumericAndDateValues(int rank)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""
            {"NodeList":[
                {"NodeId":"number","DataType":"UInt32","ValueRank":{{rank}},"Value":1234},
                {"NodeId":"date","DataType":"DateTime","ValueRank":{{rank}},"Value":"2026-10-07T12:34:56Z"}
            ]}
            """);
        UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]).Should().BeOfType<uint>().Which.Should().Be(1234);
        UserDefinedPluginNodes.GetScalarValue(folder.NodeList[1]).Should()
            .Be(new DateTime(2026, 10, 7, 12, 34, 56, DateTimeKind.Utc));
    }

    [TestCase(ValueRanks.Any)]
    [TestCase(ValueRanks.ScalarOrOneDimension)]
    [TestCase(ValueRanks.OneDimension)]
    public void Configuration_ArrayValuesAreNotConvertedAsScalars(int rank)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"array","DataType":"UInt32","ValueRank":{{rank}},"Value":[1,2]}]}""");
        var node = folder.NodeList[0];
        UserDefinedPluginNodes.GetScalarValue(node).Should().Be(node.Value);
        node.Value = new uint[] { 1, 2 };
        UserDefinedPluginNodes.GetScalarValue(node).Should().BeSameAs(node.Value);
    }

    [TestCase("Float", "3.4028234663852886e38")]
    [TestCase("Float", "-3.4028234663852886e38")]
    [TestCase("Double", "1.7976931348623157e308")]
    [TestCase("Double", "-1.7976931348623157e308")]
    public void Configuration_MaximumFiniteFloatingValuesAreAccepted(string dataType, string jsonValue)
    {
        var folder = UserDefinedPluginNodes.DeserializeConfiguration(
            $$"""{"NodeList":[{"NodeId":"id","DataType":"{{dataType}}","Value":{{jsonValue}}}]}""");
        object value = UserDefinedPluginNodes.GetScalarValue(folder.NodeList[0]);
        if (dataType == "Float")
        {
            value.Should().BeOfType<float>();
        }
        else
        {
            value.Should().BeOfType<double>();
        }
        double number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        double.IsFinite(number).Should().BeTrue();
        Math.Abs(number).Should().Be(dataType == "Float" ? float.MaxValue : double.MaxValue);
    }

    [Test]
    public async Task PublisherIdentifiers_ResolveToUserDefinedVariablesAsync()
    {
        var nodes = PluginNodes.OfType<UserDefinedPluginNodes>().Single().Nodes;
        nodes.Should().Contain(node => node.NodeIdTypePrefix == "i");
        nodes.Should().Contain(node => node.NodeIdTypePrefix == "s");
        nodes.Should().Contain(node => node.NodeIdTypePrefix == "g");
        foreach (var node in nodes)
        {
            node.Namespace.Should().Be(Namespaces.OpcPlcApplications);
            var expanded = ExpandedNodeId.Parse($"nsu={node.Namespace};{node.NodeIdTypePrefix}={node.NodeId}");
            var id = ExpandedNodeId.ToNodeId(expanded, Session.NamespaceUris);
            var value = await ReadDataValueAsync(id).ConfigureAwait(false);
            StatusCode.IsGood(value.StatusCode).Should().BeTrue("publisher identifier {0} must resolve", expanded);
        }
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
