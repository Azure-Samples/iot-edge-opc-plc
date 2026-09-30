namespace OpcPlc.Tests;

using BoilerModel1;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

[TestFixture]
public partial class GeneratedModelEquivalenceTests
{
    [TestCase("Boiler1", typeof(BoilerModel1.Objects))]
    [TestCase("Boiler2", typeof(BoilerModel2.Objects))]
    [TestCase("Di", typeof(Opc.Ua.DI.Objects))]
    [TestCase("WotCon", typeof(Opc.Ua.WotCon.Objects))]
    public void RuntimeBindings_MatchGeneratedReferenceModels(string model, Type anchor)
    {
        Type catalog = typeof(PlcServer).Assembly.GetType("OpcPlc.RuntimeModelIds")
            .GetNestedType(model, BindingFlags.NonPublic);
        Type[] groups = catalog.GetNestedTypes(BindingFlags.NonPublic);
        groups.Should().NotBeEmpty();
        foreach (Type group in groups)
        {
            string referenceName = group.IsEnum ? "DeviceHealthEnumeration" : group.Name;
            Type reference = anchor.Assembly.GetType(anchor.Namespace + "." + referenceName);
            reference.Should().NotBeNull();
            foreach (FieldInfo field in group.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                FieldInfo expected = reference.GetField(field.Name);
                expected.Should().NotBeNull("{0}.{1} must exist in the reference model", model, field.Name);
                object actualValue = field.GetValue(null);
                object expectedValue = expected.GetValue(null);
                if (group.IsEnum)
                {
                    Convert.ToInt32(actualValue).Should().Be(Convert.ToInt32(expectedValue));
                }
                else
                {
                    actualValue.Should().Be(expectedValue, "binding {0}.{1}.{2}", model, group.Name, field.Name);
                }
            }
        }
    }

    [Test]
    public void RuntimeModels_ServerHasNoGeneratedModelDependency()
    {
        Assembly server = typeof(PlcServer).Assembly;
        server.GetReferencedAssemblies().Should().NotContain(assembly => assembly.Name == "BoilerModel1");
        server.GetTypes().Should().NotContain(type => type.Namespace == "BoilerModel1" ||
            type.Namespace == "BoilerModel2" || type.Namespace == "Opc.Ua.DI" || type.Namespace == "Opc.Ua.WotCon");
        server.GetTypes().Should().NotContain(type => typeof(IEncodeable).IsAssignableFrom(type));
        Type events = server.GetType("OpcPlc.RuntimeModelIds").GetNestedType("SimpleEvents", BindingFlags.NonPublic);
        events.GetField("CycleStepDataType").GetValue(null).Should().Be(SimpleEvents.DataTypeIds.CycleStepDataType);
        events.GetField("SystemCycleStartedEventType").GetValue(null)
            .Should().Be(SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType);
    }

    private static readonly (Type Type, string Model, string Uri, uint ParentId, uint Id, uint Rule,
        string Name, string Helper)[]
        ApprovedNamedPlaceholders =
    [
        (typeof(Opc.Ua.DI.ConfigurableObjectState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 1004, 6026, 11508,
            "<ObjectIdentifier>", "AddObjectIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.FunctionalGroupState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 1005, 6027, 11508,
            "<GroupIdentifier>", "AddGroupIdentifier_Placeholder"),
        (typeof(Opc.Ua.WotCon.WoTAssetConfigurationState), "Opc.Ua.WotCon", OpcPlc.Namespaces.WotCon, 105, 108, 11508,
            "<WoTConfigurationParameterName>", "AddWoTConfigurationParameterName_Placeholder"),
        (typeof(Opc.Ua.WotCon.IWoTAssetState), "Opc.Ua.WotCon", OpcPlc.Namespaces.WotCon, 42, 66, 11508,
            "<WoTPropertyName>", "AddWoTPropertyName_Placeholder"),
        (typeof(Opc.Ua.DI.NetworkState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 6247, 6596, 11510,
            "<ProfileIdentifier>", "AddProfileIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.NetworkState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 6247, 6248, 11508,
            "<CPIdentifier>", "AddCPIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.ConnectionPointState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 6308, 6499, 11510,
            "<ProfileIdentifier>", "AddProfileIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.ConnectionPointState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 6308, 6599, 11508,
            "<NetworkIdentifier>", "AddNetworkIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.TopologyElementState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 1001, 6567, 11508,
            "<GroupIdentifier>", "AddGroupIdentifier_Placeholder"),
        (typeof(Opc.Ua.DI.DeviceState), "Opc.Ua.DI", OpcPlc.Namespaces.DI, 1002, 6571, 11508,
            "<CPIdentifier>", "AddCPIdentifier_Placeholder"),
        (typeof(Opc.Ua.WotCon.WoTAssetConnectionManagementState), "Opc.Ua.WotCon", OpcPlc.Namespaces.WotCon,
            1, 2, 11508, "<WoTAssetName>", "AddWoTAssetName_Placeholder")
    ];

    private static IEnumerable<TestCaseData> NamedPlaceholderDeclarations() => ApprovedNamedPlaceholders
        .Select(item => new TestCaseData(item.Type, item.Id)
            .SetName("PlaceholderDeclaration_" + item.Type.Name + "_" + item.Id));

    [TestCaseSource(nameof(NamedPlaceholderDeclarations))]
    public void PlaceholderDeclaration_MatchesApprovedTemplate(Type type, uint declarationId)
    {
        ApprovedNamedPlaceholders.Should().HaveCount(11);
        var approved = ApprovedNamedPlaceholders.Single(item => item.Type == type && item.Id == declarationId);
        var document = XDocument.Load(BaselinePath(approved.Model));
        XNamespace schema = document.Root.Name.Namespace;
        document.Root.Element(schema + "NamespaceUris").Elements().First().Value.Should().Be(approved.Uri);
        XElement declaration = document.Root.Elements()
            .Single(node => node.Attribute("NodeId")?.Value == $"ns=1;i={approved.Id}");
        declaration.Attribute("ParentNodeId").Value.Should().Be($"ns=1;i={approved.ParentId}");
        declaration.Attribute("BrowseName").Value.Should().Be("1:" + approved.Name);
        declaration.Element(schema + "References").Elements(schema + "Reference")
            .Where(reference => reference.Attribute("ReferenceType")?.Value == "HasModellingRule")
            .Select(reference => reference.Value).Should().Equal($"i={approved.Rule}");
        type.GetMethod(approved.Helper, [typeof(ISystemContext), typeof(QualifiedName)]).Should().NotBeNull();
    }

    [TestCase(typeof(Opc.Ua.WotCon.WoTAssetConfigurationState), "AddWoTConfigurationParameterName_Placeholder",
        "<WoTConfigurationParameterName>")]
    [TestCase(typeof(Opc.Ua.WotCon.IWoTAssetState), "AddWoTPropertyName_Placeholder", "<WoTPropertyName>")]
    [TestCase(typeof(Opc.Ua.WotCon.WoTAssetConnectionManagementState), "AddWoTAssetName_Placeholder", "<WoTAssetName>")]
    [TestCase(typeof(Opc.Ua.DI.ConfigurableObjectState), "AddObjectIdentifier_Placeholder", "<ObjectIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.FunctionalGroupState), "AddGroupIdentifier_Placeholder", "<GroupIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.TopologyElementState), "AddGroupIdentifier_Placeholder", "<GroupIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.DeviceState), "AddCPIdentifier_Placeholder", "<CPIdentifier>")]
    public void Placeholder_NamedChildrenPreserveRetainedMetadata(Type type, string helper, string placeholderName)
    {
        AssertNamedPlaceholderMetadata(type, [(helper, placeholderName)]);
    }

    [TestCase(typeof(Opc.Ua.DI.NetworkState))]
    [TestCase(typeof(Opc.Ua.DI.ConnectionPointState))]
    public void PairedPlaceholders_NamedChildrenPreserveRetainedMetadata(Type type)
    {
        (string Helper, string Name)[] placeholders = type == typeof(Opc.Ua.DI.NetworkState)
            ? [("AddProfileIdentifier_Placeholder", "<ProfileIdentifier>"), ("AddCPIdentifier_Placeholder", "<CPIdentifier>")]
            : [("AddProfileIdentifier_Placeholder", "<ProfileIdentifier>"),
                ("AddNetworkIdentifier_Placeholder", "<NetworkIdentifier>")];
        AssertNamedPlaceholderMetadata(type, placeholders);
    }

    private static void AssertNamedPlaceholderMetadata(Type type, (string Helper, string Name)[] placeholders)
    {
        var context = CreateModelContext();
        ushort namespaceIndex = context.NamespaceUris.GetIndexOrAppend(
            type.Namespace == "Opc.Ua.DI" ? OpcPlc.Namespaces.DI : OpcPlc.Namespaces.WotCon);
        var actual = (BaseObjectState)Activator.CreateInstance(type, new object[] { null });
        TestCaseData retained = RetainedNodeStates().Single(test => (Type)test.Arguments[2] == type);
        var expected = new BaseObjectState(null);
        expected.Initialize(context, (string)retained.Arguments[3]);
        var instanceId = new NodeId("PlaceholderProbe", namespaceIndex);
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        PrepareOptionalChildren(context, expected, actual, (string)retained.Arguments[1], ReadOptionalDeclarations(context));
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        using var assertions = new AssertionScope("named placeholders " + type.Name);
        foreach (var placeholder in ApprovedNestedPlaceholders.Where(item => item.Type == type))
        {
            AddNestedPlaceholderInstances(context, expected, actual, placeholder.Folder, placeholder.Name);
        }
        foreach (var placeholder in placeholders)
        {
            AddNamedPlaceholderInstances(context, expected, actual, placeholder.Helper,
                new QualifiedName(placeholder.Name, namespaceIndex));
        }
        CompareChildren(context, expected, actual, (string)retained.Arguments[1]);
    }

    private static void AddNamedPlaceholderInstances(SystemContext context, BaseInstanceState expected,
        BaseInstanceState actual, string helper, QualifiedName placeholderName)
    {
        if (context.NodeIdFactory is null)
        {
            uint identifier = 10000;
            var allocator = new Moq.Mock<INodeIdFactory>();
            allocator.Setup(factory => factory.New(Moq.It.IsAny<ISystemContext>(), Moq.It.IsAny<NodeState>()))
                .Returns(() => new NodeId(++identifier, placeholderName.NamespaceIndex));
            context.NodeIdFactory = allocator.Object;
        }
        BaseInstanceState template = expected.FindChild(context, placeholderName);
        template.Should().NotBeNull();
        expected.RemoveChild(template);
        var names = new[]
        {
            new QualifiedName(placeholderName.Name.Trim('<', '>') + "First", placeholderName.NamespaceIndex),
            new QualifiedName(placeholderName.Name.Trim('<', '>') + "Second", placeholderName.NamespaceIndex)
        };
        var children = new List<BaseInstanceState>();
        foreach (QualifiedName name in names)
        {
            var child = (BaseInstanceState)actual.GetType().GetMethod(helper).Invoke(actual, [context, name]);
            if (child is Opc.Ua.DI.FunctionalGroupState group && template.FindChild(context,
                new QualifiedName("UIElement", context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.DI))) is not null)
            {
                group.AddUIElement(context);
            }
            if (actual is Opc.Ua.WotCon.WoTAssetConnectionManagementState)
            {
                var declarations = new NodeStateCollection();
                Opc.Ua.WotCon.OpcUaWotConExtensions.AddOpcUaWotCon(declarations, context);
                NodeState generatedType = declarations.Single(node => node.NodeId == expected.TypeDefinitionId);
                var endpointName = new QualifiedName("AssetEndpoint", placeholderName.NamespaceIndex);
                var source = (BaseVariableState)generatedType.FindChild(context, placeholderName)
                    .FindChild(context, endpointName);
                source.Should().NotBeNull();
                source.NodeId.Should().Be(new NodeId(169, placeholderName.NamespaceIndex));
                var endpoint = new BaseDataVariableState(child);
                endpoint.Create(context, source);
                endpoint.ReferenceTypeId = source.ReferenceTypeId;
                child.AddChild(endpoint);
                var retainedEndpoint = (BaseVariableState)template.FindChild(context, endpointName);
                if (retainedEndpoint.Value.IsNull)
                {
                    IsApprovedEndpointDefault(context, retainedEndpoint.NodeId, endpoint.DataType, endpoint.ValueRank,
                        EncodeValue(endpoint.Value, context)).Should().BeTrue("only the approved typed-null String is allowed");
                }
            }
            if (template is BaseVariableState templateVariable && templateVariable.Value.IsNull)
            {
                child.Should().BeAssignableTo<BaseVariableState>();
                ((BaseVariableState)child).Value.IsNull.Should().BeTrue("the retained placeholder has a null default");
            }
            NodeId previousId = context.AssignInstanceNodeId(child);
            context.AssignInstanceChildNodeIds(child, previousId);
            children.Add(child);
            BaseInstanceState copy = template is BaseVariableState
                ? new BaseDataVariableState(expected) : new BaseObjectState(expected);
            copy.Create(context, template);
            copy.BrowseName = name;
            copy.ReferenceTypeId = template.ReferenceTypeId;
            expected.AddChild(copy);
        }
        var descendants = new List<BaseInstanceState>();
        CollectDescendants(actual);
        descendants.Select(child => child.NodeId).Should().OnlyHaveUniqueItems();
        descendants.Should().OnlyContain(child => !child.NodeId.IsNull);
        foreach (BaseInstanceState child in children)
        {
            child.NodeId.IsNull.Should().BeFalse();
            child.NodeId.Should().NotBe(template.NodeId);
            actual.FindChild(context, child.BrowseName).Should().BeSameAs(child);
        }
        void CollectDescendants(BaseInstanceState parent)
        {
            descendants.Add(parent);
            var nested = new List<BaseInstanceState>();
            parent.GetChildren(context, nested);
            foreach (BaseInstanceState child in nested)
            {
                child.Parent.Should().BeSameAs(parent);
                CollectDescendants(child);
            }
        }
    }

    private static readonly (Type Type, string Folder, string Name, uint Id, uint ParentId, uint DataType)[]
        ApprovedNestedPlaceholders =
    [
        (typeof(Opc.Ua.DI.TopologyElementState), "ParameterSet", "<ParameterIdentifier>", 6017, 5002, 24),
        (typeof(Opc.Ua.DI.ISupportInfoState), "DeviceTypeImage", "<ImageIdentifier>", 15056, 15055, 30),
        (typeof(Opc.Ua.DI.ISupportInfoState), "Documentation", "<DocumentIdentifier>", 15058, 15057, 15),
        (typeof(Opc.Ua.DI.ISupportInfoState), "ProtocolSupport", "<ProtocolSupportIdentifier>", 15060, 15059, 15),
        (typeof(Opc.Ua.DI.ISupportInfoState), "ImageSet", "<ImageIdentifier>", 15062, 15061, 30),
        (typeof(Opc.Ua.DI.DeviceState), "DeviceTypeImage", "<ImageIdentifier>", 6210, 6209, 30),
        (typeof(Opc.Ua.DI.DeviceState), "Documentation", "<DocumentIdentifier>", 6212, 6211, 15),
        (typeof(Opc.Ua.DI.DeviceState), "ProtocolSupport", "<ProtocolSupportIdentifier>", 6214, 6213, 15),
        (typeof(Opc.Ua.DI.DeviceState), "ImageSet", "<ImageIdentifier>", 6216, 6215, 30)
    ];

    [Test]
    public void AssetEndpointDeclaration_MatchesApprovedOptionalProperty()
    {
        var document = XDocument.Load(BaselinePath("Opc.Ua.WotCon"));
        XNamespace schema = document.Root.Name.Namespace;
        XElement endpoint = document.Root.Elements(schema + "UAVariable")
            .Single(node => node.Attribute("NodeId")?.Value == "ns=1;i=169");
        endpoint.Attribute("ParentNodeId").Value.Should().Be("ns=1;i=2");
        endpoint.Attribute("BrowseName").Value.Should().Be("1:AssetEndpoint");
        endpoint.Attribute("DataType").Value.Should().Be("String");
        endpoint.Element(schema + "References").Elements(schema + "Reference")
            .Where(reference => reference.Attribute("ReferenceType")?.Value == "HasModellingRule")
            .Select(reference => reference.Value).Should().Equal("i=80");
    }

    [TestCase("Identity")]
    [TestCase("Namespace")]
    [TestCase("DataType")]
    [TestCase("Rank")]
    [TestCase("EmptyString")]
    [TestCase("NonemptyString")]
    [TestCase("NullVariant")]
    public void AssetEndpointDefault_RejectsUnapprovedChanges(string change)
    {
        var context = CreateModelContext();
        NodeId id = NodeId.Create(169, OpcPlc.Namespaces.WotCon, context.NamespaceUris);
        NodeId dataType = Opc.Ua.DataTypeIds.String;
        int rank = ValueRanks.Scalar;
        byte[] encoded = [12, 255, 255, 255, 255];
        IsApprovedEndpointDefault(context, id, dataType, rank, encoded).Should().BeTrue();
        switch (change)
        {
            case "Identity": id = NodeId.Create(122, OpcPlc.Namespaces.WotCon, context.NamespaceUris); break;
            case "Namespace": id = new NodeId(169); break;
            case "DataType": dataType = Opc.Ua.DataTypeIds.ByteString; break;
            case "Rank": rank = ValueRanks.OneDimension; break;
            case "EmptyString": encoded = [12, 0, 0, 0, 0]; break;
            case "NonemptyString": encoded = [12, 1, 0, 0, 0, 65]; break;
            case "NullVariant": encoded = [0]; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        IsApprovedEndpointDefault(context, id, dataType, rank, encoded).Should().BeFalse(change);
    }

    private static bool IsApprovedEndpointDefault(SystemContext context, NodeId declarationId, NodeId dataType,
        int rank, byte[] encoded) => declarationId == NodeId.Create(169, OpcPlc.Namespaces.WotCon, context.NamespaceUris) &&
        dataType == Opc.Ua.DataTypeIds.String && rank == ValueRanks.Scalar && encoded is [12, 255, 255, 255, 255];

    [Test]
    public void NestedPlaceholderDeclarations_MatchApprovedTemplates()
    {
        ApprovedNestedPlaceholders.Should().HaveCount(9);
        ApprovedNestedPlaceholders.Select(item => item.Id).Should().OnlyHaveUniqueItems();
        var document = XDocument.Load(BaselinePath("Opc.Ua.DI"));
        XNamespace schema = document.Root.Name.Namespace;
        foreach (var approved in ApprovedNestedPlaceholders)
        {
            XElement declaration = document.Root.Elements()
                .Single(node => node.Attribute("NodeId")?.Value == $"ns=1;i={approved.Id}");
            declaration.Attribute("ParentNodeId").Value.Should().Be($"ns=1;i={approved.ParentId}");
            declaration.Attribute("BrowseName").Value.Should().Be("1:" + approved.Name);
            declaration.Element(schema + "References").Elements(schema + "Reference")
                .Where(reference => reference.Attribute("ReferenceType")?.Value == "HasModellingRule")
                .Select(reference => reference.Value).Should().Equal("i=11510");
        }
    }

    [TestCase("Identity")]
    [TestCase("Namespace")]
    [TestCase("DataType")]
    [TestCase("Rank")]
    [TestCase("Nonempty")]
    [TestCase("TypedNull")]
    [TestCase("NullVariant")]
    public void SupportPlaceholderDefault_RejectsUnapprovedChanges(string change)
    {
        var context = CreateModelContext();
        NodeId id = NodeId.Create(15056, OpcPlc.Namespaces.DI, context.NamespaceUris);
        NodeId dataType = new(30);
        int rank = ValueRanks.Scalar;
        byte[] encoded = [15, 0, 0, 0, 0];
        IsApprovedSupportDefault(context, id, dataType, rank, encoded).Should().BeTrue();
        switch (change)
        {
            case "Identity": id = NodeId.Create(6017, OpcPlc.Namespaces.DI, context.NamespaceUris); break;
            case "Namespace": id = new NodeId(15056); break;
            case "DataType": dataType = new NodeId(15); break;
            case "Rank": rank = ValueRanks.OneDimension; break;
            case "Nonempty": encoded = [15, 1, 0, 0, 0, 1]; break;
            case "TypedNull": encoded = [15, 255, 255, 255, 255]; break;
            case "NullVariant": encoded = [0]; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        IsApprovedSupportDefault(context, id, dataType, rank, encoded).Should().BeFalse(change);
    }

    private static bool IsApprovedSupportDefault(SystemContext context, NodeId declarationId, NodeId dataType,
        int rank, byte[] encoded)
    {
        if (context.NamespaceUris.GetString(declarationId.NamespaceIndex) != OpcPlc.Namespaces.DI ||
            !declarationId.TryGetValue(out uint identifier))
        {
            return false;
        }
        var approved = ApprovedNestedPlaceholders.SingleOrDefault(item => item.Id == identifier);
        return approved.Type is not null && approved.DataType is 15 or 30 &&
            dataType == new NodeId(approved.DataType) && rank == ValueRanks.Scalar && encoded is [15, 0, 0, 0, 0];
    }

    [TestCase(typeof(Opc.Ua.DI.TopologyElementState), "ParameterSet", "<ParameterIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.ISupportInfoState), "DeviceTypeImage", "<ImageIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.ISupportInfoState), "Documentation", "<DocumentIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.ISupportInfoState), "ProtocolSupport", "<ProtocolSupportIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.ISupportInfoState), "ImageSet", "<ImageIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.DeviceState), "DeviceTypeImage", "<ImageIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.DeviceState), "Documentation", "<DocumentIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.DeviceState), "ProtocolSupport", "<ProtocolSupportIdentifier>")]
    [TestCase(typeof(Opc.Ua.DI.DeviceState), "ImageSet", "<ImageIdentifier>")]
    public void NestedPlaceholder_PublicDeclarationCopyPreservesMetadata(Type type, string folder, string placeholder)
    {
        var context = CreateModelContext();
        ushort namespaceIndex = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.DI);
        TestCaseData retained = RetainedNodeStates().Single(test => (Type)test.Arguments[2] == type);
        var expected = new BaseObjectState(null);
        expected.Initialize(context, (string)retained.Arguments[3]);
        var actual = (BaseObjectState)Activator.CreateInstance(type, new object[] { null });
        var instanceId = new NodeId("NestedPlaceholderProbe", namespaceIndex);
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        PrepareOptionalChildren(context, expected, actual, (string)retained.Arguments[1], ReadOptionalDeclarations(context));
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        AddNestedPlaceholderInstances(context, expected, actual, folder, placeholder);
    }

    private static void AddNestedPlaceholderInstances(SystemContext context, BaseInstanceState expected,
        BaseInstanceState actual, string folder, string placeholder)
    {
        var approved = ApprovedNestedPlaceholders.Single(item => item.Type == actual.GetType() && item.Folder == folder &&
            item.Name == placeholder);
        ushort namespaceIndex = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.DI);
        var folderName = new QualifiedName(folder, namespaceIndex);
        var placeholderName = new QualifiedName(placeholder, namespaceIndex);
        BaseInstanceState expectedParent = expected.FindChild(context, folderName);
        BaseInstanceState actualParent = actual.FindChild(context, folderName);
        actualParent.Should().NotBeNull();
        BaseInstanceState template = expectedParent.FindChild(context, placeholderName);
        template.Should().NotBeNull();
        template.NodeId.Should().Be(new NodeId(approved.Id, namespaceIndex));
        var declarations = new NodeStateCollection();
        Opc.Ua.DI.OpcUaDIExtensions.AddOpcUaDI(declarations, context);
        NodeState generatedType = declarations.Single(node => node.NodeId == expected.TypeDefinitionId);
        BaseInstanceState source = generatedType.FindChild(context, folderName).FindChild(context, placeholderName);
        source.Should().NotBeNull();
        source.NodeId.Should().Be(template.NodeId);
        NodeId sourceId = source.NodeId;
        NodeState sourceParent = source.Parent;
        expectedParent.RemoveChild(template);
        if (context.NodeIdFactory is null)
        {
            uint identifier = 10000;
            var allocator = new Moq.Mock<INodeIdFactory>();
            allocator.Setup(factory => factory.New(Moq.It.IsAny<ISystemContext>(), Moq.It.IsAny<NodeState>()))
                .Returns(() => new NodeId(++identifier, namespaceIndex));
            context.NodeIdFactory = allocator.Object;
        }
        using var assertions = new AssertionScope(actual.GetType().Name + "/" + folder);
        foreach (string suffix in new[] { "First", "Second" })
        {
            var name = new QualifiedName(placeholder.Trim('<', '>') + suffix, namespaceIndex);
            var child = new BaseDataVariableState(actualParent);
            child.Create(context, source);
            child.BrowseName = name;
            child.SymbolicName = name.Name;
            child.DisplayName = new LocalizedText(name.Name);
            child.ReferenceTypeId = source.ReferenceTypeId;
            NodeId previousId = context.AssignInstanceNodeId(child);
            context.AssignInstanceChildNodeIds(child, previousId);
            actualParent.AddChild(child);
            var expectedChild = new BaseDataVariableState(expectedParent);
            expectedChild.Create(context, template);
            expectedChild.BrowseName = name;
            expectedChild.ReferenceTypeId = template.ReferenceTypeId;
            expectedParent.AddChild(expectedChild);
            if (((BaseVariableState)template).Value.IsNull)
            {
                if (approved.DataType is 15 or 30)
                {
                    IsApprovedSupportDefault(context, template.NodeId, child.DataType, child.ValueRank,
                        EncodeValue(child.Value, context)).Should().BeTrue("only the approved empty ByteString default is allowed");
                }
                else
                {
                    child.Value.IsNull.Should().BeTrue();
                }
            }
        }
        var children = new List<BaseInstanceState>();
        actualParent.GetChildren(context, children);
        children.Should().HaveCount(2).And.OnlyContain(child => !child.NodeId.IsNull && child.NodeId != sourceId);
        children.Select(child => child.NodeId).Should().OnlyHaveUniqueItems();
        source.NodeId.Should().Be(sourceId);
        source.Parent.Should().BeSameAs(sourceParent);
        source.BrowseName.Should().Be(placeholderName);
        CompareChildren(context, expectedParent, actualParent, actual.GetType().Name + "/" + folderName);
    }

    private static readonly (Type Type, uint DeclarationId)[] StandaloneMethodDeclarations =
    [
        (typeof(Opc.Ua.DI.InitLockMethodState), 6393),
        (typeof(Opc.Ua.DI.RenewLockMethodState), 6396),
        (typeof(Opc.Ua.DI.ExitLockMethodState), 6398),
        (typeof(Opc.Ua.DI.BreakLockMethodState), 6400),
        (typeof(Opc.Ua.DI.TransferToDeviceMethodState), 6527),
        (typeof(Opc.Ua.DI.TransferFromDeviceMethodState), 6529),
        (typeof(Opc.Ua.DI.FetchTransferResultDataMethodState), 6531),
        (typeof(Opc.Ua.DI.GetUpdateBehaviorCachedLoadingTypeMethodState), 189),
        (typeof(Opc.Ua.DI.GetUpdateBehaviorFileSystemLoadingTypeMethodState), 206),
        (typeof(Opc.Ua.DI.ValidateFilesMethodState), 209),
        (typeof(Opc.Ua.DI.InstallSoftwarePackageMethodState), 265),
        (typeof(Opc.Ua.DI.InstallFilesMethodState), 268)
    ];

    private static IEnumerable<TestCaseData> StandaloneMethods() => StandaloneMethodDeclarations
        .Select(method => new TestCaseData(method.Type, method.DeclarationId)
            .SetName("MethodDeclaration_" + method.Type.Name));

    private static readonly string[] BoilerAlarmPaths =
    [
        "Boiler2TypeState/2:DeviceHealthAlarms/1:FailureAlarm",
        "Boiler2TypeState/2:DeviceHealthAlarms/1:CheckFunctionAlarm",
        "Boiler2TypeState/2:DeviceHealthAlarms/1:OffSpecAlarm",
        "Boiler2TypeState/2:DeviceHealthAlarms/1:MaintenanceRequiredAlarm"
    ];

    private static readonly Dictionary<string, uint[]> TypeOnlyDeclarationIds = new(StringComparer.Ordinal)
    {
        ["PrepareForUpdateStateMachineTypeState"] = [231, 233, 235, 237, 239, 241, 243, 245, 247],
        ["InstallationStateMachineTypeState"] = [271, 273, 275, 277, 279, 281, 283],
        ["PowerCycleStateMachineTypeState"] = [299, 301, 303, 305],
        ["ConfirmationStateMachineTypeState"] = [323, 325, 327, 329],
        ["LockingServicesTypeState"] = [15890],
        ["SoftwareUpdateTypeState"] = [134]
    };

    private static readonly HashSet<string> FixedAlarmArgumentPaths = new[]
    {
        "FailureAlarmTypeState", "MaintenanceRequiredAlarmTypeState", "CheckFunctionAlarmTypeState",
        "OffSpecAlarmTypeState", "DeviceHealthDiagnosticAlarmTypeState"
    }.Concat(BoilerAlarmPaths).SelectMany(root => new[] { "AddComment", "Acknowledge" }
        .Select(method => root + "/" + method + "/InputArguments")).ToHashSet(StringComparer.Ordinal);

    [TestCase("Boiler2TypeState/2:DeviceHealthAlarms/1:OtherAlarm")]
    [TestCase("Boiler2TypeState/2:DeviceHealthAlarms/2:FailureAlarm")]
    [TestCase("Boiler2TypeState/1:DeviceHealthAlarms/1:FailureAlarm")]
    [TestCase("Boiler2TypeState/2:DeviceHealthAlarms/1:FailureAlarm/Extra")]
    [TestCase("OtherTypeState/2:DeviceHealthAlarms/1:FailureAlarm")]
    public void BoilerAlarm_MetadataPolicyRejectsOtherPaths(string root)
    {
        BoilerAlarmPaths.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        IsApprovedFixedAlarmBound(root + "/AddComment/InputArguments", [0], [2], 2, 2).Should().BeFalse();
        IsApprovedFixedAlarmBound(root + "/Acknowledge/InputArguments", [0], [2], 2, 2).Should().BeFalse();
    }

    [TestCase("Path", false)]
    [TestCase("Path", true)]
    [TestCase("OutputArguments", false)]
    [TestCase("OutputArguments", true)]
    [TestCase("OldBound", false)]
    [TestCase("OldBound", true)]
    [TestCase("NewBound", false)]
    [TestCase("NewBound", true)]
    [TestCase("Dimensions", false)]
    [TestCase("Dimensions", true)]
    [TestCase("OldCount", false)]
    [TestCase("OldCount", true)]
    [TestCase("NewCount", false)]
    [TestCase("NewCount", true)]
    [TestCase("NullDimensions", false)]
    [TestCase("NullDimensions", true)]
    public void FixedAlarmBound_RejectsUnapprovedChanges(string change, bool nestedBoiler)
    {
        FixedAlarmArgumentPaths.Should().HaveCount(18);
        string path = nestedBoiler
            ? "Boiler2TypeState/2:DeviceHealthAlarms/1:FailureAlarm/AddComment/InputArguments"
            : "FailureAlarmTypeState/AddComment/InputArguments";
        uint[] expected = [0];
        uint[] actual = [2];
        int expectedCount = 2;
        int actualCount = 2;
        IsApprovedFixedAlarmBound(path, expected, actual, expectedCount, actualCount).Should().BeTrue();
        switch (change)
        {
            case "Path": path = "OtherTypeState/AddComment/InputArguments"; break;
            case "OutputArguments": path = "FailureAlarmTypeState/AddComment/OutputArguments"; break;
            case "OldBound": expected = [1]; break;
            case "NewBound": actual = [3]; break;
            case "Dimensions": actual = [2, 2]; break;
            case "OldCount": expectedCount = 1; break;
            case "NewCount": actualCount = 3; break;
            case "NullDimensions": actual = null; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        IsApprovedFixedAlarmBound(path, expected, actual, expectedCount, actualCount).Should().BeFalse(change);
    }

    private static bool IsApprovedFixedAlarmBound(string path, uint[] expected, uint[] actual,
        int expectedCount, int actualCount) => FixedAlarmArgumentPaths.Contains(path) &&
        expected is [0] && actual is [2] && expectedCount == 2 && actualCount == 2;

    [TestCase("FailureAlarm")]
    [TestCase("CheckFunctionAlarm")]
    [TestCase("OffSpecAlarm")]
    [TestCase("MaintenanceRequiredAlarm")]
    public void BoilerAlarm_GeneratedMethodMetadataPreservesRetainedSignatures(string alarmName)
    {
        var context = CreateModelContext();
        ushort boilerNamespace = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.OpcPlcBoiler);
        ushort diNamespace = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.DI);
        TestCaseData retained = RetainedNodeStates().Single(test => (Type)test.Arguments[2] == typeof(BoilerModel2.Boiler2State));
        var expected = new BaseObjectState(null);
        expected.Initialize(context, (string)retained.Arguments[3]);
        BoilerModel2.Boiler2State actual = CreateBoiler2FromFactory(context, new QualifiedName("AlarmMetadataProbe", boilerNamespace));
        var alarmsName = new QualifiedName("DeviceHealthAlarms", diNamespace);
        var name = new QualifiedName(alarmName, boilerNamespace);
        BaseInstanceState expectedAlarm = expected.FindChild(context, alarmsName).FindChild(context, name);
        BaseInstanceState actualAlarm = actual.FindChild(context, alarmsName).FindChild(context, name);
        expectedAlarm.Should().NotBeNull();
        actualAlarm.Should().NotBeNull();
        using var assertions = new AssertionScope("Boiler2/" + alarmName);
        foreach (string methodName in new[] { "Disable", "Enable", "AddComment", "Acknowledge" })
        {
            var browseName = new QualifiedName(methodName);
            var expectedMethod = (MethodState)expectedAlarm.FindChild(context, browseName);
            var actualMethod = (MethodState)actualAlarm.FindChild(context, browseName);
            actualMethod.Parent.Should().BeSameAs(actualAlarm);
            actualMethod.Executable.Should().Be(expectedMethod.Executable);
            actualMethod.UserExecutable.Should().Be(expectedMethod.UserExecutable);
            string[] emptyProperties = methodName is "Disable" or "Enable"
                ? ["InputArguments", "OutputArguments"] : ["OutputArguments"];
            foreach (string propertyName in emptyProperties)
            {
                var propertyBrowseName = new QualifiedName(propertyName);
                expectedMethod.FindChild(context, propertyBrowseName).Should().BeNull();
                string path = "Boiler2TypeState/" + alarmsName + "/" + name + "/" + methodName + "/" + propertyName;
                actualMethod.FindChild(context, propertyBrowseName).Should().BeNull(path);
            }
            if (methodName is "AddComment" or "Acknowledge")
            {
                var argumentName = new QualifiedName("InputArguments");
                var baseline = (BaseVariableState)expectedMethod.FindChild(context, argumentName);
                var generated = (BaseVariableState)actualMethod.FindChild(context, argumentName);
                generated.Parent.Should().BeSameAs(actualMethod);
                generated.DataType.Should().Be(baseline.DataType);
                generated.ValueRank.Should().Be(baseline.ValueRank).And.Be(ValueRanks.OneDimension);
                generated.AccessLevel.Should().Be(baseline.AccessLevel);
                generated.UserAccessLevel.Should().Be(baseline.UserAccessLevel);
                baseline.ArrayDimensions.ToArray().Should().Equal(0u);
                generated.ArrayDimensions.ToArray().Should().Equal(2u);
                ReadArguments(baseline.Value).Should().HaveCount(2);
                ReadArguments(generated.Value).Should().HaveCount(2);
                string path = "Boiler2TypeState/" + alarmsName + "/" + name + "/" + methodName + "/InputArguments";
                IsApprovedFixedAlarmBound(path, baseline.ArrayDimensions.ToArray(), generated.ArrayDimensions.ToArray(),
                    ReadArguments(baseline.Value).Length, ReadArguments(generated.Value).Length).Should().BeTrue(path);
                CompareArguments(baseline.Value, generated.Value, methodName);
            }
        }
    }

    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "AddComment")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Acknowledge")]
    [TestCase(typeof(Opc.Ua.DI.MaintenanceRequiredAlarmState), "AddComment")]
    [TestCase(typeof(Opc.Ua.DI.MaintenanceRequiredAlarmState), "Acknowledge")]
    [TestCase(typeof(Opc.Ua.DI.CheckFunctionAlarmState), "AddComment")]
    [TestCase(typeof(Opc.Ua.DI.CheckFunctionAlarmState), "Acknowledge")]
    [TestCase(typeof(Opc.Ua.DI.OffSpecAlarmState), "AddComment")]
    [TestCase(typeof(Opc.Ua.DI.OffSpecAlarmState), "Acknowledge")]
    [TestCase(typeof(Opc.Ua.DI.DeviceHealthDiagnosticAlarmState), "AddComment")]
    [TestCase(typeof(Opc.Ua.DI.DeviceHealthDiagnosticAlarmState), "Acknowledge")]
    public void AlarmMethod_FixedBoundsPreserveRetainedSignature(Type type, string methodName)
    {
        var context = CreateModelContext();
        TestCaseData retained = RetainedNodeStates().Single(test => (Type)test.Arguments[2] == type);
        var expected = new BaseObjectState(null);
        expected.Initialize(context, (string)retained.Arguments[3]);
        var actual = (BaseObjectState)Activator.CreateInstance(type, new object[] { null });
        actual.Create(context, new NodeId("AlarmArgumentsProbe", 2), expected.BrowseName, expected.DisplayName, false);
        var name = new QualifiedName(methodName);
        var argumentName = new QualifiedName("InputArguments");
        var retainedArguments = (BaseVariableState)expected.FindChild(context, name).FindChild(context, argumentName);
        var generatedArguments = (BaseVariableState)actual.FindChild(context, name).FindChild(context, argumentName);
        using var assertions = new AssertionScope(type.Name + "/" + methodName);
        retainedArguments.ArrayDimensions.ToArray().Should().Equal(0u);
        generatedArguments.ArrayDimensions.ToArray().Should().Equal(2u);
        ReadArguments(retainedArguments.Value).Should().HaveCount(2);
        ReadArguments(generatedArguments.Value).Should().HaveCount(2);
        CompareArguments(retainedArguments.Value, generatedArguments.Value, methodName);
    }

    [TestCase("PrepareForUpdateStateMachineTypeState")]
    [TestCase("InstallationStateMachineTypeState")]
    [TestCase("PowerCycleStateMachineTypeState")]
    [TestCase("ConfirmationStateMachineTypeState")]
    [TestCase("LockingServicesTypeState")]
    [TestCase("SoftwareUpdateTypeState")]
    public void TypeOnlyDeclarations_HaveNoModellingRule(string path)
    {
        var document = XDocument.Load(BaselinePath("Opc.Ua.DI"));
        XNamespace schema = document.Root.Name.Namespace;
        XElement parent = document.Root.Elements(schema + "UAObjectType")
            .Single(node => node.Attribute("BrowseName")?.Value == "1:" + path[..^5]);
        string parentId = parent.Attribute("NodeId").Value;
        string[] declaredIds = document.Root.Elements().Where(node =>
            node.Attribute("ParentNodeId")?.Value == parentId &&
            !node.Element(schema + "References").Elements(schema + "Reference")
                .Any(reference => reference.Attribute("ReferenceType")?.Value == "HasModellingRule"))
            .Select(node => node.Attribute("NodeId").Value).ToArray();
        declaredIds.Should().BeEquivalentTo(TypeOnlyDeclarationIds[path].Select(identifier => $"ns=1;i={identifier}"));
        TypeOnlyDeclarationIds.Values.SelectMany(identifiers => identifiers).Should().HaveCount(26).And.OnlyHaveUniqueItems();
        var context = CreateModelContext();
        uint identifier = TypeOnlyDeclarationIds[path][0];
        IsApprovedTypeOnlyDeclaration(context, path, NodeId.Create(identifier, OpcPlc.Namespaces.DI,
            context.NamespaceUris)).Should().BeTrue();
        IsApprovedTypeOnlyDeclaration(context, path, new NodeId(identifier)).Should().BeFalse();
        IsApprovedTypeOnlyDeclaration(context, path + "/Other", NodeId.Create(identifier, OpcPlc.Namespaces.DI,
            context.NamespaceUris)).Should().BeFalse();
        IsApprovedTypeOnlyDeclaration(context, path, NodeId.Create(6534, OpcPlc.Namespaces.DI,
            context.NamespaceUris)).Should().BeFalse("mandatory Locked cannot be omitted");
    }

    private static bool IsApprovedTypeOnlyDeclaration(ISystemContext context, string path, NodeId nodeId)
    {
        return TypeOnlyDeclarationIds.TryGetValue(path, out uint[] identifiers) &&
            context.NamespaceUris.GetString(nodeId.NamespaceIndex) == OpcPlc.Namespaces.DI &&
            nodeId.TryGetValue(out uint identifier) && identifiers.Contains(identifier);
    }

    [TestCaseSource(nameof(StandaloneMethods))]
    public void StandaloneMethod_GeneratedArgumentsMatchRetainedDeclaration(Type type, uint declarationId)
    {
        var context = CreateModelContext();
        MethodState expected = CreateRetainedMethodDeclaration(context, declarationId);
        var actual = (MethodState)Activator.CreateInstance(type, new object[] { null });
        actual.Create(context, new NodeId("MethodProbe", expected.NodeId.NamespaceIndex),
            expected.BrowseName, expected.DisplayName, false);

        using var assertions = new AssertionScope(type.Name + " retained declaration");
        actual.Executable.Should().Be(expected.Executable);
        actual.UserExecutable.Should().Be(expected.UserExecutable);
        CompareChildren(context, expected, actual, type.Name);
    }

    private static MethodState CreateRetainedMethodDeclaration(SystemContext context, uint declarationId)
    {
        using var stream = File.OpenRead(BaselinePath("Opc.Ua.DI"));
        var nodes = new NodeStateCollection();
        Opc.Ua.Export.UANodeSet.Read(stream).Import(context, nodes);
        var document = XDocument.Load(BaselinePath("Opc.Ua.DI"));
        XNamespace schema = document.Root.Name.Namespace;
        XElement declaration = document.Root.Elements(schema + "UAMethod")
            .Single(node => node.Attribute("NodeId")?.Value == $"ns=1;i={declarationId}");
        NodeId methodId = NodeId.Create(declarationId, OpcPlc.Namespaces.DI, context.NamespaceUris);
        var expected = new MethodState(null);
        expected.Create(context, nodes.OfType<MethodState>().Single(node => node.NodeId == methodId));
        foreach (XElement reference in declaration.Element(schema + "References").Elements(schema + "Reference")
            .Where(reference => reference.Attribute("ReferenceType")?.Value == "HasProperty"))
        {
            NodeId localId = NodeId.Parse(reference.Value);
            localId.NamespaceIndex.Should().Be(1, "DI method argument properties belong to the DI namespace");
            localId.TryGetValue(out uint identifier).Should().BeTrue();
            NodeId mappedId = NodeId.Create(identifier, OpcPlc.Namespaces.DI, context.NamespaceUris);
            var source = nodes.OfType<BaseVariableState>().Single(node => node.NodeId == mappedId);
            source.DataType.Should().Be(Opc.Ua.DataTypeIds.Argument);
            source.BrowseName.Name.Should().BeOneOf("InputArguments", "OutputArguments");
            var property = new BaseDataVariableState(expected);
            property.Create(context, source);
            property.ReferenceTypeId = ReferenceTypeIds.HasProperty;
            expected.AddChild(property);
        }
        return expected;
    }

    [TestCase(typeof(Opc.Ua.WotCon.WoTAssetFileState), "Close", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.WotCon.WoTAssetFileState), "Write", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.WotCon.WoTAssetFileState), "SetPosition", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FileSystemLoadingState), "FileSystem/Delete", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Disable", "InputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Disable", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Enable", "InputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Enable", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "AddComment", "OutputArguments")]
    [TestCase(typeof(Opc.Ua.DI.FailureAlarmState), "Acknowledge", "OutputArguments")]
    public void Method_ArgumentsMatchRetainedDeclaration(Type type, string path, string propertyName)
    {
        var context = CreateModelContext();
        TestCaseData retained = RetainedNodeStates().Single(test => (Type)test.Arguments[2] == type);
        var expected = new BaseObjectState(null);
        expected.Initialize(context, (string)retained.Arguments[3]);
        var state = (BaseInstanceState)Activator.CreateInstance(type, new object[] { null });
        state.Create(context, new NodeId("EmptyArgumentsProbe", 1),
            expected.BrowseName, expected.DisplayName, false);
        BaseInstanceState method = state;
        BaseInstanceState retainedMethod = expected;
        foreach (string name in path.Split('/'))
        {
            method = method.FindChild(context, new QualifiedName(name));
            method.Should().NotBeNull(path);
            retainedMethod = retainedMethod.FindChild(context, new QualifiedName(name));
            retainedMethod.Should().NotBeNull(path);
        }
        method.Should().BeAssignableTo<MethodState>();
        using var assertions = new AssertionScope(type.Name + "/" + path + "/" + propertyName);
        retainedMethod.FindChild(context, new QualifiedName(propertyName)).Should().BeNull();
        method.FindChild(context, new QualifiedName(propertyName)).Should().BeNull();
        CompareChildren(context, retainedMethod, method, retained.Arguments[1] + "/" + path);
    }

    [TestCase("InputArguments", false, false)]
    [TestCase("InputArguments", false, true)]
    [TestCase("InputArguments", true, false)]
    [TestCase("InputArguments", true, true)]
    [TestCase("OutputArguments", false, false)]
    [TestCase("OutputArguments", false, true)]
    [TestCase("OutputArguments", true, false)]
    [TestCase("OutputArguments", true, true)]
    public void MethodComparison_RejectsUndeclaredArguments(string propertyName, bool hasArguments, bool qualifiedName)
    {
        var context = CreateModelContext();
        var expected = new MethodState(null);
        var actual = new MethodState(null);
        string path = "WoTAssetFileState/Close";
        CompareChildren(context, expected, actual, path);
        var property = PropertyState<ArrayOf<Argument>>.With<StructureBuilder<Argument>>(actual);
        property.BrowseName = new QualifiedName(propertyName, (ushort)(qualifiedName ? 1 : 0));
        property.TypeDefinitionId = VariableTypeIds.PropertyType;
        property.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        property.DataType = Opc.Ua.DataTypeIds.Argument;
        property.ValueRank = ValueRanks.OneDimension;
        property.ArrayDimensions = [0u];
        property.AccessLevel = AccessLevels.CurrentRead;
        property.UserAccessLevel = AccessLevels.CurrentRead;
        property.Value = hasArguments ? [new Argument { Name = "Unexpected", DataType = Opc.Ua.DataTypeIds.String }] : [];
        actual.AddChild(property);
        Action compare = () => CompareChildren(context, expected, actual, path);
        compare.Should().Throw<AssertionException>();
    }

    [Test]
    public void NamedAsset_FileMethodsDoNotAddOutputArguments()
    {
        var context = CreateModelContext();
        ushort namespaceIndex = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.WotCon);
        var state = new Opc.Ua.WotCon.WoTAssetConnectionManagementState(null);
        state.Create(context, new NodeId("AssetOutputProbe", namespaceIndex), new QualifiedName("Probe", namespaceIndex),
            new LocalizedText("Probe"), false);
        foreach (string assetName in new[] { "WoTAssetNameFirst", "WoTAssetNameSecond" })
        {
            BaseObjectState asset = state.AddWoTAssetName_Placeholder(context, new QualifiedName(assetName, namespaceIndex));
            BaseInstanceState file = asset.FindChild(context, new QualifiedName("WoTFile", namespaceIndex));
            foreach (string methodName in new[] { "Close", "Write", "SetPosition" })
            {
                BaseInstanceState method = file.FindChild(context, new QualifiedName(methodName));
                method.Should().BeAssignableTo<MethodState>();
                BaseInstanceState output = method.FindChild(context, new QualifiedName("OutputArguments"));
                output.Should().BeNull(assetName + "/" + methodName);
            }
        }
    }

    [Test]
    public void ArgumentDimensions_OnlyAllowApprovedScalarNormalization()
    {
        ArgumentDimensionsEquivalent(ValueRanks.Scalar, [], null).Should().BeTrue();
        ArgumentDimensionsEquivalent(ValueRanks.Scalar, null, []).Should().BeFalse();
        ArgumentDimensionsEquivalent(ValueRanks.OneDimension, [], null).Should().BeFalse();
        ArgumentDimensionsEquivalent(ValueRanks.Any, [], null).Should().BeFalse();
        ArgumentDimensionsEquivalent(ValueRanks.OneDimension, [2], [0]).Should().BeFalse();
        ArgumentDimensionsEquivalent(ValueRanks.OneDimension, [2], [2]).Should().BeTrue();
    }

    [Test]
    public void ArgumentListDimensions_OnlyAllowApprovedUnspecifiedBound()
    {
        ArgumentListDimensionsEquivalent([2], [0]).Should().BeTrue();
        ArgumentListDimensionsEquivalent([2], [2]).Should().BeTrue();
        ArgumentListDimensionsEquivalent([2], [1]).Should().BeFalse();
        ArgumentListDimensionsEquivalent([0], [2]).Should().BeFalse();
        ArgumentListDimensionsEquivalent([2, 3], [0]).Should().BeFalse();
        ArgumentListDimensionsEquivalent([], [0]).Should().BeFalse();
        ArgumentListDimensionsEquivalent(null, [0]).Should().BeFalse();
    }

    [Test]
    public void WotOptionalMember_ParentInitializationAppliesDeclaredMetadata()
    {
        var context = CreateModelContext();
        ushort namespaceIndex = context.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.WotCon);
        var state = new Opc.Ua.WotCon.WoTAssetConnectionManagementState(null);
        var name = new QualifiedName("SupportedWoTBindings", namespaceIndex);
        state.CreateChild(context, name, assignInstanceNodeIds: false).Should().NotBeNull();
        state.Create(context, new NodeId("OptionalProbe", namespaceIndex),
            new QualifiedName("OptionalProbe", namespaceIndex), new LocalizedText("OptionalProbe"), false);
        var bindings = state.SupportedWoTBindings;
        bindings.Should().NotBeNull();
        using var assertions = new AssertionScope("SupportedWoTBindings");
        bindings.DataType.Should().Be(new NodeId(23751));
        bindings.ReferenceTypeId.Should().Be(ReferenceTypeIds.HasProperty);
        bindings.ValueRank.Should().Be(ValueRanks.OneDimension);
        bindings.ArrayDimensions.ToArray().Should().Equal(0u);
        bindings.AccessLevel.Should().Be(AccessLevels.CurrentRead);
        bindings.UserAccessLevel.Should().Be(AccessLevels.CurrentRead);
    }

    [Test]
    public void SdkNodeCopy_PreservesTypedMethodChildOwnership()
    {
        var context = CreateModelContext();
        var original = new BaseObjectState(null)
        {
            NodeId = new NodeId("CopyRoot", 1), BrowseName = new QualifiedName("CopyRoot", 1)
        };
        var method = new ReadMethodState(original);
        method.Create(context, new NodeId("CopyRoot/Read", 1), new QualifiedName("Read"), new LocalizedText("Read"), false);
        original.AddChild(method);
        var copy = new BaseObjectState(null);
        copy.Create(context, original);
        var copiedMethod = (ReadMethodState)copy.FindChild(context, method.BrowseName);
        copiedMethod.Should().NotBeNull();
        using var assertions = new AssertionScope("typed method copy ownership");
        copiedMethod.Should().NotBeSameAs(method);
        copiedMethod.Parent.Should().BeSameAs(copy);
        copiedMethod.InputArguments.Should().NotBeSameAs(method.InputArguments);
        copiedMethod.OutputArguments.Should().NotBeSameAs(method.OutputArguments);
        copiedMethod.InputArguments.Parent.Should().BeSameAs(copiedMethod);
        copiedMethod.OutputArguments.Parent.Should().BeSameAs(copiedMethod);
        method.InputArguments.Parent.Should().BeSameAs(method);
        method.OutputArguments.Parent.Should().BeSameAs(method);
    }

    [Test]
    public void SdkNodeCopy_PreservesNestedUntypedChildren()
    {
        var context = CreateModelContext();
        var original = new BaseObjectState(null) { NodeId = new NodeId("Root", 1), BrowseName = new QualifiedName("Root", 1) };
        var folder = new BaseObjectState(original)
        {
            NodeId = new NodeId("Folder", 1), BrowseName = new QualifiedName("Folder", 1)
        };
        var value = new BaseDataVariableState(folder)
        {
            NodeId = new NodeId("Value", 1), BrowseName = new QualifiedName("Value", 1),
            DataType = Opc.Ua.DataTypeIds.Float, ValueRank = ValueRanks.Scalar, Value = new Variant(20f)
        };
        folder.AddChild(value);
        original.AddChild(folder);
        var copy = new BaseObjectState(null);
        copy.Create(context, original);

        var copiedFolder = copy.FindChild(context, folder.BrowseName);
        copiedFolder.Should().NotBeNull();
        var copiedValue = copiedFolder.FindChild(context, value.BrowseName);
        copiedValue.Should().NotBeNull("copying a folder must preserve its children");
        copiedValue.Parent.Should().BeSameAs(copiedFolder);
        ((BaseVariableState)copiedValue).Value.Should().Be(value.Value);
        folder.FindChild(context, value.BrowseName).Should().BeSameAs(value, "the source must remain intact");
    }

    [TestCase("Factory")]
    [TestCase("DefaultCreate")]
    [TestCase("CopyFactory")]
    public void Boiler2NodeState_PreservesMandatoryParameterSubtree(string construction)
    {
        var context = CreateModelContext();
        var browseName = new QualifiedName("ProbeBoiler", context.NamespaceUris.GetIndexOrAppend(
            OpcPlc.Namespaces.OpcPlcBoiler));
        BoilerModel2.Boiler2State boiler;
        if (construction != "DefaultCreate")
        {
            boiler = CreateBoiler2FromFactory(context, browseName);
            if (construction == "CopyFactory")
            {
                var copied = new BoilerModel2.Boiler2State(null);
                copied.Create(context, boiler);
                boiler = copied;
            }
        }
        else
        {
            boiler = new BoilerModel2.Boiler2State(null);
            boiler.Create(context, new NodeId("ProbeBoiler", browseName.NamespaceIndex), browseName,
                new LocalizedText("ProbeBoiler"), false);
        }

        boiler.ParameterSet.Should().NotBeNull();
        var children = new List<BaseInstanceState>();
        boiler.ParameterSet.GetChildren(context, children);
        children.Select(child => child.BrowseName.Name).Should().BeEquivalentTo(new[]
        {
            "TemperatureChangeSpeed", "BaseTemperature", "TargetTemperature", "MaintenanceInterval",
            "OverheatedThresholdTemperature", "HeaterState", "CurrentTemperature", "Overheated", "OverheatInterval"
        });
        foreach (BaseInstanceState child in children)
        {
            child.Parent.Should().BeSameAs(boiler.ParameterSet);
        }
        children.OfType<BaseVariableState>().Single(child => child.BrowseName.Name == "BaseTemperature")
            .Value.GetFloat().Should().Be(20);
    }

    private static BoilerModel2.Boiler2State CreateBoiler2FromFactory(SystemContext context, QualifiedName browseName)
    {
        MethodInfo factory = typeof(BoilerModel2.Boiler2State).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "BoilerModel2")
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(method => method.Name == "CreateInstanceOfBoiler2Type");
        return (BoilerModel2.Boiler2State)factory.Invoke(null, [context, null, browseName]);
    }

    private static IEnumerable<TestCaseData> RetainedNodeStates()
    {
        (string Model, Type Anchor)[] models =
        [
            ("BoilerModel1", typeof(BoilerModel1.Objects)),
            ("BoilerModel2", typeof(BoilerModel2.Objects)),
            ("Opc.Ua.DI", typeof(Opc.Ua.DI.Objects)),
            ("Opc.Ua.WotCon", typeof(Opc.Ua.WotCon.Objects)),
            ("SimpleEvents", typeof(SimpleEvents.Objects))
        ];
        foreach (var model in models)
        {
            string source = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
                "ModelBaselines", model.Model + ".Classes.cs"));
            var classes = RetainedClassPattern().Matches(source);
            int count = 0;
            for (int index = 0; index < classes.Count; index++)
            {
                Match declaration = classes[index];
                int end = index + 1 < classes.Count ? classes[index + 1].Index : source.Length;
                string body = source[declaration.Index..end];
                Match initializer = RetainedInitializerPattern().Match(body);
                if (!initializer.Success)
                {
                    continue;
                }
                string name = declaration.Groups["name"].Value;
                Type type = model.Anchor.Assembly.GetType(model.Anchor.Namespace + "." + name);
                if (type is null && name.EndsWith("TypeState", StringComparison.Ordinal))
                {
                    type = model.Anchor.Assembly.GetType(model.Anchor.Namespace + "." + name[..^9] + "State");
                }
                string encoded = string.Concat(RetainedLiteralPattern().Matches(initializer.Groups["chunks"].Value)
                    .Select(chunk => chunk.Groups["value"].Value));
                Type[] replacements = name == "GetUpdateBehaviorMethodState"
                    ? model.Anchor.Assembly.GetExportedTypes().Where(candidate => candidate.Namespace == model.Anchor.Namespace &&
                        candidate.Name.StartsWith("GetUpdateBehavior", StringComparison.Ordinal) &&
                        candidate.Name.EndsWith("MethodState", StringComparison.Ordinal)).ToArray()
                    : [type];
                if (name == "GetUpdateBehaviorMethodState" && replacements.Length != 2)
                {
                    throw new InvalidOperationException("Expected both generated GetUpdateBehavior method states.");
                }
                foreach (Type replacement in replacements)
                {
                    yield return new TestCaseData(model.Model, name, replacement, encoded)
                        .SetName($"NodeState_RetainedInitialization_{model.Model}_{replacement?.Name ?? name}");
                }
                count++;
            }
            int expectedCount = model.Model switch
            {
                "BoilerModel1" or "BoilerModel2" => 1,
                "Opc.Ua.DI" => 44,
                "Opc.Ua.WotCon" => 10,
                "SimpleEvents" => 4,
                _ => throw new InvalidOperationException("Unexpected model.")
            };
            if (count != expectedCount)
            {
                throw new InvalidOperationException($"Expected {expectedCount} retained initializers for {model.Model}, found {count}.");
            }
        }
    }

    [TestCaseSource(nameof(RetainedNodeStates))]
    public void NodeState_InitializesLikeRetainedGeneratedClass(string model, string name, Type type, string encoded)
    {
        type.Should().NotBeNull("retained generated state {0}.{1} must have a current counterpart", model, name);
        var context = CreateModelContext();
        var actual = (BaseInstanceState)Activator.CreateInstance(type, new object[] { null });
        BaseInstanceState expected = actual switch
        {
            MethodState => new MethodState(null),
            BaseVariableState => new BaseDataVariableState(null),
            _ => new BaseObjectState(null)
        };
        expected.Initialize(context, encoded);
        var methodDeclaration = StandaloneMethodDeclarations.SingleOrDefault(method => method.Type == type);
        if (methodDeclaration.Type is not null)
        {
            model.Should().Be("Opc.Ua.DI");
            expected.Should().BeOfType<MethodState>();
            var retainedChildren = new List<BaseInstanceState>();
            expected.GetChildren(context, retainedChildren);
            retainedChildren.Should().BeEmpty("the approved standalone initializers omit their argument properties");
            expected = CreateRetainedMethodDeclaration(context, methodDeclaration.DeclarationId);
        }
        var instanceId = new NodeId("EquivalenceInstance", expected.NodeId.NamespaceIndex);
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        using var assertions = new AssertionScope(model + "." + name);
        var optionalDeclarations = ReadOptionalDeclarations(context);
        PrepareOptionalChildren(context, expected, actual, name, optionalDeclarations);
        actual.Create(context, instanceId, expected.BrowseName, expected.DisplayName, false);
        foreach (var placeholder in ApprovedNamedPlaceholders.Where(item => item.Type == type))
        {
            model.Should().Be(placeholder.Model);
            var browseName = new QualifiedName(placeholder.Name, context.NamespaceUris.GetIndexOrAppend(placeholder.Uri));
            BaseInstanceState declaration = expected.FindChild(context, browseName);
            declaration.Should().NotBeNull();
            declaration.NodeId.Should().Be(NodeId.Create(placeholder.Id, placeholder.Uri, context.NamespaceUris));
            AddNamedPlaceholderInstances(context, expected, actual, placeholder.Helper, browseName);
        }
        foreach (var placeholder in ApprovedNestedPlaceholders.Where(item => item.Type == type))
        {
            AddNestedPlaceholderInstances(context, expected, actual, placeholder.Folder, placeholder.Name);
        }
        if (actual is not MethodState)
        {
            actual.TypeDefinitionId.Should().Be(expected.TypeDefinitionId);
        }
        else
        {
            ((MethodState)actual).Executable.Should().Be(((MethodState)expected).Executable);
            ((MethodState)actual).UserExecutable.Should().Be(((MethodState)expected).UserExecutable);
        }
        CompareChildren(context, expected, actual, name);
    }

    private static void PrepareOptionalChildren(ISystemContext context, BaseInstanceState expected,
        BaseInstanceState actual, string path, HashSet<NodeId> optionalDeclarations)
    {
        var children = new List<BaseInstanceState>();
        expected.GetChildren(context, children);
        foreach (BaseInstanceState child in children)
        {
            string childPath = path + "/" + child.BrowseName;
            BaseInstanceState found = actual.FindChild(context, child.BrowseName);
            if (found is null && optionalDeclarations.Contains(child.NodeId))
            {
                found = actual.CreateChild(context, child.BrowseName, assignInstanceNodeIds: false);
                TestContext.Out.WriteLine("Prepared optional child before parent initialization: " + childPath);
            }
            if (found is not null)
            {
                PrepareOptionalChildren(context, child, found, childPath, optionalDeclarations);
            }
        }
    }

    private static void CompareChildren(ISystemContext context, BaseInstanceState expected,
        BaseInstanceState actual, string path)
    {
        var expectedChildren = new List<BaseInstanceState>();
        var actualChildren = new List<BaseInstanceState>();
        expected.GetChildren(context, expectedChildren);
        actual.GetChildren(context, actualChildren);
        expectedChildren.RemoveAll(child => IsApprovedTypeOnlyDeclaration(context, path, child.NodeId) &&
            !actualChildren.Any(actualChild => actualChild.BrowseName == child.BrowseName));
        foreach (BaseInstanceState child in expectedChildren)
        {
            string childPath = path + "/" + child.BrowseName;
            BaseInstanceState found = actualChildren.SingleOrDefault(node => node.BrowseName == child.BrowseName);
            found.Should().NotBeNull(childPath);
            if (found is null)
            {
                continue;
            }
            found.Parent.Should().BeSameAs(actual, childPath);
            found.NodeClass.Should().Be(child.NodeClass, childPath);
            found.TypeDefinitionId.Should().Be(child.TypeDefinitionId, childPath);
            found.ReferenceTypeId.Should().Be(child.ReferenceTypeId, childPath);
            if (child is MethodState method && found is MethodState generatedMethod)
            {
                generatedMethod.Executable.Should().Be(method.Executable, childPath);
                generatedMethod.UserExecutable.Should().Be(method.UserExecutable, childPath);
            }
            if (child is BaseObjectState instance && found is BaseObjectState generatedInstance)
            {
                generatedInstance.EventNotifier.Should().Be(instance.EventNotifier, childPath);
            }
            if (child is BaseVariableState variable && found is BaseVariableState generated)
            {
                generated.DataType.Should().Be(variable.DataType, childPath);
                generated.ValueRank.Should().Be(variable.ValueRank, childPath);
                if (expected is MethodState && child.BrowseName.NamespaceIndex == 0 &&
                    child.BrowseName.Name is "InputArguments" or "OutputArguments" &&
                    variable.DataType == Opc.Ua.DataTypeIds.Argument && variable.ValueRank == ValueRanks.OneDimension)
                {
                    bool dimensionsMatch = ArgumentListDimensionsEquivalent(variable.ArrayDimensions.ToArray(),
                        generated.ArrayDimensions.ToArray());
                    if (!dimensionsMatch && FixedAlarmArgumentPaths.Contains(childPath))
                    {
                        dimensionsMatch = IsApprovedFixedAlarmBound(childPath, variable.ArrayDimensions.ToArray(),
                            generated.ArrayDimensions.ToArray(), ReadArguments(variable.Value)?.Length ?? -1,
                            ReadArguments(generated.Value)?.Length ?? -1);
                    }
                    dimensionsMatch.Should().BeTrue(childPath + " argument-list bounds");
                }
                else
                {
                    generated.ArrayDimensions.ToArray().Should().Equal(variable.ArrayDimensions.ToArray(), childPath);
                }
                generated.AccessLevel.Should().Be(variable.AccessLevel, childPath);
                generated.UserAccessLevel.Should().Be(variable.UserAccessLevel, childPath);
                generated.Historizing.Should().Be(variable.Historizing, childPath);
                generated.MinimumSamplingInterval.Should().Be(variable.MinimumSamplingInterval, childPath);
                if (!variable.Value.IsNull && variable.DataType == Opc.Ua.DataTypeIds.Argument)
                {
                    CompareArguments(variable.Value, generated.Value, childPath);
                }
                else if (!variable.Value.IsNull)
                {
                    EncodeValue(generated.Value, context).Should().Equal(
                        EncodeValue(variable.Value, context), childPath + " declared default");
                }
            }
            CompareChildren(context, child, found, childPath);
        }
        actualChildren.Select(child => child.BrowseName).Should()
            .BeEquivalentTo(expectedChildren.Select(child => child.BrowseName), path + " child set");
    }

    private static HashSet<NodeId> ReadOptionalDeclarations(SystemContext context)
    {
        var result = new HashSet<NodeId>();
        foreach (string model in new[] { "BoilerModel1", "BoilerModel2", "Opc.Ua.DI", "Opc.Ua.WotCon", "SimpleEvents" })
        {
            var document = XDocument.Load(BaselinePath(model));
            XNamespace schema = document.Root.Name.Namespace;
            string[] namespaces = new[] { Opc.Ua.Namespaces.OpcUa }.Concat(
                document.Root.Element(schema + "NamespaceUris").Elements().Select(element => element.Value)).ToArray();
            foreach (XElement node in document.Root.Elements().Where(element => element.Attribute("NodeId") is not null))
            {
                bool optional = node.Element(schema + "References")?.Elements().Any(reference =>
                    reference.Attribute("ReferenceType")?.Value == "HasModellingRule" &&
                    reference.Value is "i=80" or "i=11508") == true;
                if (optional)
                {
                    NodeId local = NodeId.Parse(node.Attribute("NodeId").Value);
                    local.TryGetValue(out uint identifier).Should().BeTrue();
                    result.Add(NodeId.Create(identifier, namespaces[local.NamespaceIndex], context.NamespaceUris));
                }
            }
        }
        return result;
    }

    private static Argument[] ReadArguments(Variant value)
    {
        var factory = EncodeableFactory.Create();
        factory.Builder.AddEncodeableType(typeof(Argument)).Commit();
        var messageContext = new ServiceMessageContext(DefaultTelemetry.Create(_ => { }), factory);
        foreach (string namespaceUri in CreateModelContext().NamespaceUris.ToArray().Skip(1))
        {
            messageContext.NamespaceUris.GetIndexOrAppend(namespaceUri);
        }
        using var decoder = new BinaryDecoder(EncodeValue(value, CreateModelContext()), messageContext);
        return decoder.ReadVariant("Value").GetStructureArray<Argument>(context: messageContext).ToArray();
    }

    private static void CompareArguments(Variant expected, Variant actual, string path)
    {
        Argument[] baseline = ReadArguments(expected);
        Argument[] generated = ReadArguments(actual);
        baseline.Should().NotBeNull(path + " retained arguments decode");
        generated.Should().NotBeNull(path + " generated arguments must preserve the declared default");
        if (baseline is null || generated is null)
        {
            return;
        }
        generated.Length.Should().Be(baseline.Length, path);
        for (int index = 0; index < Math.Min(baseline.Length, generated.Length); index++)
        {
            generated[index].Name.Should().Be(baseline[index].Name, path);
            generated[index].DataType.Should().Be(baseline[index].DataType, path);
            generated[index].ValueRank.Should().Be(baseline[index].ValueRank, path);
            ArgumentDimensionsEquivalent(baseline[index].ValueRank, baseline[index].ArrayDimensions.ToArray(),
                generated[index].ArrayDimensions.ToArray()).Should().BeTrue(path + " argument dimensions");
            generated[index].Description.Should().Be(baseline[index].Description, path);
        }
    }

    private static bool ArgumentDimensionsEquivalent(int valueRank, uint[] expected, uint[] actual)
    {
        return valueRank == ValueRanks.Scalar && expected is { Length: 0 } && actual is null ||
            (expected is null ? actual is null : actual is not null && actual.SequenceEqual(expected));
    }

    private static bool ArgumentListDimensionsEquivalent(uint[] expected, uint[] actual)
    {
        return expected is { Length: 1 } && expected[0] > 0 && actual is [0] ||
            (expected is null ? actual is null : actual is not null && actual.SequenceEqual(expected));
    }

    private static SystemContext CreateModelContext()
    {
        var namespaces = new NamespaceTable();
        namespaces.GetIndexOrAppend(OpcPlc.Namespaces.OpcPlcBoiler);
        namespaces.GetIndexOrAppend(OpcPlc.Namespaces.DI);
        namespaces.GetIndexOrAppend(OpcPlc.Namespaces.WotCon);
        namespaces.GetIndexOrAppend(OpcPlc.Namespaces.OpcPlcSimpleEvents);
        var factory = EncodeableFactory.Create();
        foreach (Type type in new[] { typeof(BoilerDataType).Assembly, typeof(GeneratedModelEquivalenceTests).Assembly }
            .SelectMany(assembly => assembly.GetExportedTypes()).Where(type =>
                !type.IsAbstract && !type.ContainsGenericParameters && typeof(IEncodeable).IsAssignableFrom(type)))
        {
            factory.Builder.AddEncodeableType(type).Commit();
        }
        return new SystemContext(DefaultTelemetry.Create(_ => { }))
        {
            NamespaceUris = namespaces, EncodeableFactory = factory, TypeTable = new TypeTable(namespaces)
        };
    }

    private static byte[] EncodeValue(Variant value, ISystemContext context)
    {
        var messageContext = ServiceMessageContext.CreateEmpty(null);
        foreach (string namespaceUri in context.NamespaceUris.ToArray().Skip(1))
        {
            messageContext.NamespaceUris.GetIndexOrAppend(namespaceUri);
        }
        using var encoder = new BinaryEncoder(messageContext);
        encoder.WriteVariant("Value", value);
        return encoder.CloseAndReturnBuffer();
    }

    [GeneratedRegex(@"public partial class (?<name>\w+)", RegexOptions.CultureInvariant)]
    private static partial Regex RetainedClassPattern();

    [GeneratedRegex("private const string InitializationString\\s*=\\s*(?<chunks>(?:\"[A-Za-z0-9+/=]+\"\\s*\\+?\\s*)+);",
        RegexOptions.CultureInvariant)]
    private static partial Regex RetainedInitializerPattern();

    [GeneratedRegex("\"(?<value>[A-Za-z0-9+/=]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex RetainedLiteralPattern();

    [Test]
    public void BoilerNodeState_InitializesMandatoryChildAndRetainedDefault()
    {
        var namespaces = new NamespaceTable();
        ushort namespaceIndex = namespaces.GetIndexOrAppend(OpcPlc.Namespaces.OpcPlcBoiler);
        var factory = EncodeableFactory.Create();
        factory.Builder.AddEncodeableType(typeof(BoilerDataType))
            .AddEncodeableType(typeof(BoilerTemperatureType)).Commit();
        var context = new SystemContext(DefaultTelemetry.Create(_ => { }))
        {
            NamespaceUris = namespaces,
            EncodeableFactory = factory,
            TypeTable = new TypeTable(namespaces)
        };
        var boiler = new Boiler1State(null);
        boiler.Create(context, new NodeId("DefaultBoiler", namespaceIndex),
            new QualifiedName("DefaultBoiler", namespaceIndex), new LocalizedText("DefaultBoiler"), false);

        boiler.TypeDefinitionId.Should().Be(new NodeId(3, namespaceIndex));
        boiler.BoilerStatus.Should().NotBeNull();
        boiler.BoilerStatus.Parent.Should().BeSameAs(boiler);
        boiler.BoilerStatus.BrowseName.Should().Be(new QualifiedName("BoilerStatus", namespaceIndex));
        boiler.BoilerStatus.DataType.Should().Be(new NodeId(15032, namespaceIndex));
        boiler.BoilerStatus.ValueRank.Should().Be(ValueRanks.Scalar);
        using var stream = File.OpenRead(BaselinePath("BoilerModel1"));
        var nodes = new NodeStateCollection();
        Opc.Ua.Export.UANodeSet.Read(stream).Import(context, nodes);
        var declaration = nodes.OfType<BaseVariableState>()
            .Single(node => node.NodeId == new NodeId(4, namespaceIndex));
        declaration.Value.TryGetStructure(out BoilerDataType retainedValue).Should().BeTrue(
            "the retained NodeSet default must decode with the same factory");
        retainedValue.Temperature.Top.Should().Be(20);
        retainedValue.Pressure.Should().Be(100020);
        boiler.BoilerStatus.Value.Should().NotBeNull("the generated state must retain the declared structured default");
        boiler.BoilerStatus.Value.Temperature.Top.Should().Be(20);
        boiler.BoilerStatus.Value.Temperature.Bottom.Should().Be(20);
        boiler.BoilerStatus.Value.Pressure.Should().Be(100020);
        boiler.BoilerStatus.Value.HeaterState.Should().Be(BoilerHeaterStateType.On);
    }

    [TestCase("BoilerModel1", typeof(BoilerModel1.Objects), OpcPlc.Namespaces.OpcPlcBoiler)]
    [TestCase("BoilerModel2", typeof(BoilerModel2.Objects), OpcPlc.Namespaces.OpcPlcBoiler)]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.Objects), "http://opcfoundation.org/UA/DI/")]
    [TestCase("Opc.Ua.WotCon", typeof(Opc.Ua.WotCon.Objects), "http://opcfoundation.org/UA/WoT-Con/")]
    [TestCase("SimpleEvents", typeof(SimpleEvents.Objects), "http://microsoft.com/Opc/OpcPlc/SimpleEvents")]
    public void GeneratedIdentifiers_MatchRetainedNodeIdTable(string model, Type anchor, string namespaceUri)
    {
        using var parser = new TextFieldParser(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "ModelBaselines", model + ".NodeIds.csv"));
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var approved = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
                "ModelBaselines", "GeneratedApiDifferences.json")))[model];
        var observedDifferences = new HashSet<string>();
        using var assertions = new AssertionScope(model);
        int checkedIdentifiers = 0;
        int renamedIdentifiers = 0;
        int missingIdentifiers = 0;
        while (!parser.EndOfData)
        {
            string[] row = parser.ReadFields();
            row.Should().HaveCount(3);
            string name = row[0];
            uint identifier = uint.Parse(row[1], CultureInfo.InvariantCulture);
            Type constants = anchor.Assembly.GetType(anchor.Namespace + "." + row[2] + "s");
            constants.Should().NotBeNull("{0} declares {1}", row[2], name);
            FieldInfo field = constants?.GetField(name);
            bool hasApprovedDifference = approved.TryGetValue(name, out string replacement);
            if (hasApprovedDifference)
            {
                observedDifferences.Add(name);
                field.Should().BeNull("{0} is an explicitly approved generated API difference", name);
            }
            else
            {
                field.Should().NotBeNull("retained {0} {1} has identifier {2}", row[2], name, identifier);
            }
            if (field is not null)
            {
                field.GetValue(null).Should().Be(identifier, name);
            }
            else
            {
                string[] aliases = constants?.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(candidate => candidate.IsLiteral && candidate.FieldType == typeof(uint) &&
                        (uint)candidate.GetRawConstantValue() == identifier).Select(candidate => candidate.Name)
                    .ToArray() ?? [];
                if (aliases.Length > 0)
                {
                    renamedIdentifiers++;
                }
                else
                {
                    missingIdentifiers++;
                }
                TestContext.Out.WriteLine($"{row[2]} {identifier}: {name} -> " +
                    (aliases.Length == 0 ? "NO GENERATED EXPORT" : string.Join(", ", aliases)));
                if (hasApprovedDifference)
                {
                    aliases.Should().Equal(replacement is null ? [] : new[] { replacement }, name);
                }
            }
            Type expandedConstants = anchor.Assembly.GetType(anchor.Namespace + "." + row[2] + "Ids");
            string expandedName = replacement ?? name;
            var expanded = expandedConstants?.GetField(expandedName)?.GetValue(null)
                ?? expandedConstants?.GetProperty(expandedName)?.GetValue(null);
            if (hasApprovedDifference && replacement is null)
            {
                expanded.Should().BeNull(name);
            }
            else
            {
                expanded.Should().Be(new ExpandedNodeId(new NodeId(identifier), namespaceUri, 0), name);
            }
            checkedIdentifiers++;
        }
        checkedIdentifiers.Should().BeGreaterThan(0);
        observedDifferences.Should().BeEquivalentTo(approved.Keys, "no unconsumed API exceptions are allowed");
        TestContext.Out.WriteLine($"{model}: {checkedIdentifiers} retained identifiers; " +
            $"{renamedIdentifiers} renamed, {missingIdentifiers} without a generated numeric export.");
    }

    [Test]
    public void BoilerStructure_IdentifiersMatchRetainedModel()
    {
        var value = new BoilerDataType();
        value.TypeId.Should().Be(BoilerId(15032));
        value.BinaryEncodingId.Should().Be(BoilerId(15072));
        value.XmlEncodingId.Should().Be(BoilerId(15084));
        var temperature = new BoilerTemperatureType();
        temperature.TypeId.Should().Be(BoilerId(15001));
        temperature.BinaryEncodingId.Should().Be(BoilerId(15004));
        temperature.XmlEncodingId.Should().Be(BoilerId(15008));
        ((int)BoilerHeaterStateType.Off).Should().Be(0);
        ((int)BoilerHeaterStateType.On).Should().Be(1);
    }

    [TestCase(15096u, 15032u)]
    [TestCase(15012u, 15001u)]
    public void BoilerJsonEncoding_RemainsInRetainedNodeSet(uint encodingId, uint dataTypeId)
    {
        var document = XDocument.Load(BaselinePath("BoilerModel1"));
        XNamespace schema = document.Root.Name.Namespace;
        XElement node = document.Root.Elements(schema + "UAObject")
            .Single(element => element.Attribute("NodeId").Value == $"ns=1;i={encodingId}");
        node.Attribute("BrowseName").Value.Should().Be("Default JSON");
        node.Element(schema + "References").Elements(schema + "Reference")
            .Should().Contain(reference => reference.Attribute("ReferenceType").Value == "HasEncoding" &&
                reference.Attribute("IsForward").Value == "false" && reference.Value == $"ns=1;i={dataTypeId}");
    }

    [TestCase(95, 100, 100100, BoilerHeaterStateType.On)]
    [TestCase(int.MinValue, int.MaxValue, -1, BoilerHeaterStateType.Off)]
    public void BoilerStructure_BinaryLayoutMatchesRetainedCodec(
        int top, int bottom, int pressure, BoilerHeaterStateType heater)
    {
        var value = new BoilerDataType
        {
            Temperature = new BoilerTemperatureType { Top = top, Bottom = bottom },
            Pressure = pressure,
            HeaterState = heater
        };
        using var expectedStream = new MemoryStream();
        using var writer = new BinaryWriter(expectedStream);
        writer.Write(top);
        writer.Write(bottom);
        writer.Write(pressure);
        writer.Write((int)heater);
        byte[] expected = expectedStream.ToArray();
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);

        value.Encode(encoder);

        encoder.CloseAndReturnBuffer().Should().Equal(expected);
        using var decoder = new BinaryDecoder(expected, context);
        var decoded = new BoilerDataType();
        decoded.Decode(decoder);
        decoded.Temperature.Top.Should().Be(top);
        decoded.Temperature.Bottom.Should().Be(bottom);
        decoded.Pressure.Should().Be(pressure);
        decoded.HeaterState.Should().Be(heater);
    }

    [Test]
    public void SimpleEventStructure_BinaryLayoutMatchesRetainedCodec()
    {
        var value = new SimpleEvents.CycleStepDataType { Name = "Step 1", Duration = 1000.5 };
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] name = Encoding.UTF8.GetBytes(value.Name);
        writer.Write(name.Length);
        writer.Write(name);
        writer.Write(value.Duration);
        AssertBinary(value, stream.ToArray(), () => new SimpleEvents.CycleStepDataType());
    }

    [Test]
    public void DiStructures_BinaryLayoutMatchesRetainedCodec()
    {
        AssertBinary(new Opc.Ua.DI.FetchResultDataType(), [], () => new Opc.Ua.DI.FetchResultDataType());
        var error = new Opc.Ua.DI.TransferResultErrorDataType { Status = -123, Diagnostics = null };
        using var errorStream = new MemoryStream();
        using var errorWriter = new BinaryWriter(errorStream);
        errorWriter.Write(-123);
        errorWriter.Write((byte)0);
        AssertBinary(error, errorStream.ToArray(), () => new Opc.Ua.DI.TransferResultErrorDataType());

        var parameter = new Opc.Ua.DI.ParameterResultDataType
        {
            NodePath = new[] { new QualifiedName("Temperature", 2) }.ToArrayOf(),
            StatusCode = StatusCodes.BadOutOfRange,
            Diagnostics = null
        };
        using var parameterStream = new MemoryStream();
        using var parameterWriter = new BinaryWriter(parameterStream);
        parameterWriter.Write(1);
        parameterWriter.Write((ushort)2);
        byte[] name = Encoding.UTF8.GetBytes("Temperature");
        parameterWriter.Write(name.Length);
        parameterWriter.Write(name);
        parameterWriter.Write((uint)StatusCodes.BadOutOfRange);
        parameterWriter.Write((byte)0);
        byte[] parameterBytes = parameterStream.ToArray();
        AssertBinary(parameter, parameterBytes, () => new Opc.Ua.DI.ParameterResultDataType());

        var result = new Opc.Ua.DI.TransferResultDataDataType
        {
            SequenceNumber = 123,
            EndOfResults = true,
            ParameterDefs = new[] { parameter }.ToArrayOf()
        };
        using var resultStream = new MemoryStream();
        using var resultWriter = new BinaryWriter(resultStream);
        resultWriter.Write(123);
        resultWriter.Write(true);
        resultWriter.Write(1);
        resultWriter.Write(parameterBytes);
        AssertBinary(result, resultStream.ToArray(), () => new Opc.Ua.DI.TransferResultDataDataType());
    }

    [TestCase("BoilerModel1", typeof(BoilerHeaterStateType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.DeviceHealthEnumeration))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.SoftwareVersionFileType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.UpdateBehavior))]
    public void Enumeration_ValuesMatchRetainedDefinition(string model, Type enumType)
    {
        var document = XDocument.Load(BaselinePath(model));
        XNamespace schema = document.Root.Name.Namespace;
        XElement definition = document.Descendants(schema + "Definition").Single(element =>
            element.Attribute("Name").Value.Split(':').Last() == enumType.Name);
        bool flags = (bool?)definition.Attribute("IsOptionSet") == true;
        enumType.IsEnum.Should().BeTrue();
        using var assertions = new AssertionScope(enumType.Name);
        foreach (XElement field in definition.Elements(schema + "Field"))
        {
            string name = field.Attribute("Name").Value;
            long declared = (long)field.Attribute("Value");
            long expected = flags ? 1L << (int)declared : declared;
            Enum.TryParse(enumType, name, out object actual).Should().BeTrue(name);
            if (actual is not null)
            {
                Convert.ToInt64(actual, CultureInfo.InvariantCulture).Should().Be(expected, name);
            }
        }
    }

    [TestCase("BoilerModel1", typeof(BoilerDataType))]
    [TestCase("BoilerModel1", typeof(BoilerTemperatureType))]
    [TestCase("SimpleEvents", typeof(SimpleEvents.CycleStepDataType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.FetchResultDataType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.ParameterResultDataType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.TransferResultErrorDataType))]
    [TestCase("Opc.Ua.DI", typeof(Opc.Ua.DI.TransferResultDataDataType))]
    public void Structure_EncodingIdsAndTextFieldsMatchRetainedSchema(string model, Type type)
    {
        var document = XDocument.Load(BaselinePath(model));
        XNamespace schema = document.Root.Name.Namespace;
        XElement dataType = document.Root.Elements(schema + "UADataType").Single(node =>
            node.Attribute("BrowseName").Value.Split(':').Last() == type.Name);
        string localId = dataType.Attribute("NodeId").Value;
        string namespaceUri = document.Root.Element(schema + "NamespaceUris").Elements().First().Value;
        string[] expectedFields = dataType.Element(schema + "Definition").Elements(schema + "Field")
            .Select(field => field.Attribute("Name").Value).ToArray();
        IEncodeable value = CreateSample(type);
        value.TypeId.Should().Be(ToExpanded(localId));
        foreach ((string name, ExpandedNodeId actual) in new[]
        {
            ("Default Binary", value.BinaryEncodingId), ("Default XML", value.XmlEncodingId)
        })
        {
            XElement encoding = document.Root.Elements(schema + "UAObject").Single(node =>
                node.Attribute("BrowseName").Value == name &&
                node.Element(schema + "References").Elements(schema + "Reference").Any(reference =>
                    reference.Attribute("ReferenceType").Value == "HasEncoding" && reference.Value == localId));
            actual.Should().Be(ToExpanded(encoding.Attribute("NodeId").Value), type.Name + " " + name);
        }

        var context = ServiceMessageContext.CreateEmpty(null);
        using var xmlEncoder = new XmlEncoder(new System.Xml.XmlQualifiedName(type.Name, namespaceUri), null, context);
        value.Encode(xmlEncoder);
        string xml = xmlEncoder.CloseAndReturnText();
        XElement encoded = XElement.Parse(xml);
        encoded.Elements().Select(element => element.Name.LocalName).Should().Equal(expectedFields);
        encoded.Elements().Select(element => element.Name.NamespaceName)
            .Should().Equal(Enumerable.Repeat(namespaceUri, expectedFields.Length));
        using var reader = System.Xml.XmlReader.Create(new StringReader(xml));
        using var xmlDecoder = new XmlDecoder(type, reader, context);
        var xmlValue = (IEncodeable)Activator.CreateInstance(type);
        xmlValue.Decode(xmlDecoder);
        xmlValue.IsEqual(value).Should().BeTrue(type.Name + " XML");

        using var jsonEncoder = new JsonEncoder(context);
        value.Encode(jsonEncoder);
        string json = jsonEncoder.CloseAndReturnText();
        using var jsonDocument = JsonDocument.Parse(json);
        jsonDocument.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal(expectedFields);
        using var jsonDecoder = new JsonDecoder(json, context);
        var jsonValue = (IEncodeable)Activator.CreateInstance(type);
        jsonValue.Decode(jsonDecoder);
        jsonValue.IsEqual(value).Should().BeTrue(type.Name + " JSON");

        ExpandedNodeId ToExpanded(string id)
        {
            NodeId parsed = NodeId.Parse(id);
            parsed.TryGetValue(out uint numeric).Should().BeTrue();
            return new ExpandedNodeId(new NodeId(numeric), namespaceUri, 0);
        }
    }

    [TestCase("BoilerModel1", OpcPlc.Namespaces.OpcPlcBoiler)]
    [TestCase("BoilerModel2", OpcPlc.Namespaces.OpcPlcBoiler)]
    [TestCase("Opc.Ua.DI", "http://opcfoundation.org/UA/DI/")]
    [TestCase("Opc.Ua.WotCon", "http://opcfoundation.org/UA/WoT-Con/")]
    [TestCase("SimpleEvents", "http://microsoft.com/Opc/OpcPlc/SimpleEvents")]
    public async Task RuntimeNodes_MatchRetainedNodeSetIdentitiesAsync(string model, string namespaceUri)
    {
        var document = XDocument.Load(BaselinePath(model));
        XNamespace schema = document.Root.Name.Namespace;
        string[] modelNamespaces = new[] { Opc.Ua.Namespaces.OpcUa }.Concat(
            document.Root.Element(schema + "NamespaceUris").Elements().Select(element => element.Value)).ToArray();
        var aliases = document.Root.Element(schema + "Aliases").Elements()
            .ToDictionary(element => element.Attribute("Alias").Value, element => element.Value);
        var fixture = new PlcSimulatorFixture(["--str=false", "--simpleevents", "--wotcon"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var session = await fixture.CreateSessionAsync("ModelEquivalence").ConfigureAwait(false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var expectations = new List<(ReadValueId Request, Variant Expected, string Label)>();
            var references = new List<(BrowseDescription Request, NodeId Target, string Label)>();
            int nodeCount = 0;
            foreach (XElement node in document.Root.Elements().Where(element => element.Attribute("NodeId") != null))
            {
                NodeId localId = NodeId.Parse(node.Attribute("NodeId").Value);
                if (modelNamespaces[localId.NamespaceIndex] != namespaceUri)
                {
                    continue;
                }
                localId.TryGetValue(out uint identifier).Should().BeTrue();
                var id = NodeId.Create(identifier, namespaceUri, session.NamespaceUris);
                nodeCount++;
                string browseName = node.Attribute("BrowseName").Value;
                int separator = browseName.IndexOf(':');
                ushort browseNamespace = separator < 0 ? (ushort)0 :
                    ushort.Parse(browseName[..separator], CultureInfo.InvariantCulture);
                string name = separator < 0 ? browseName : browseName[(separator + 1)..];
                var qualifiedName = new QualifiedName(name,
                    (ushort)session.NamespaceUris.GetIndex(modelNamespaces[browseNamespace]));
                NodeClass nodeClass = Enum.Parse<NodeClass>(node.Name.LocalName[2..]);
                expectations.Add((new ReadValueId { NodeId = id, AttributeId = Attributes.NodeId },
                    new Variant(id), node.Attribute("NodeId").Value + " NodeId"));
                expectations.Add((new ReadValueId { NodeId = id, AttributeId = Attributes.NodeClass },
                    new Variant((int)nodeClass), node.Attribute("NodeId").Value + " NodeClass"));
                expectations.Add((new ReadValueId { NodeId = id, AttributeId = Attributes.BrowseName },
                    new Variant(qualifiedName), node.Attribute("NodeId").Value + " BrowseName"));
                if (nodeClass is NodeClass.Variable or NodeClass.VariableType)
                {
                    expectations.Add((new ReadValueId { NodeId = id, AttributeId = Attributes.DataType },
                        new Variant(Resolve(node.Attribute("DataType")?.Value ?? "i=24")), id + " DataType"));
                    expectations.Add((new ReadValueId { NodeId = id, AttributeId = Attributes.ValueRank },
                        new Variant((int?)node.Attribute("ValueRank") ?? -1), id + " ValueRank"));
                }
                foreach (XElement reference in node.Element(schema + "References")?.Elements() ?? [])
                {
                    NodeId referenceType = Resolve(reference.Attribute("ReferenceType").Value);
                    bool forward = (bool?)reference.Attribute("IsForward") ?? true;
                    if ((referenceType == ReferenceTypeIds.HasTypeDefinition && forward) ||
                        (referenceType == ReferenceTypeIds.HasSubtype && !forward) ||
                        referenceType == ReferenceTypeIds.HasEncoding)
                    {
                        NodeId target = Resolve(reference.Value);
                        if (model == "BoilerModel2" && identifier == 5 &&
                            referenceType == ReferenceTypeIds.HasTypeDefinition && forward)
                        {
                            target.Should().Be(Opc.Ua.ObjectTypeIds.BaseObjectType,
                                "the retained boiler models conflict here");
                            target = Opc.Ua.ObjectTypeIds.FolderType;
                        }
                        references.Add((new BrowseDescription
                        {
                            NodeId = id, ReferenceTypeId = referenceType,
                            BrowseDirection = forward ? BrowseDirection.Forward : BrowseDirection.Inverse,
                            IncludeSubtypes = false, ResultMask = (uint)BrowseResultMask.All
                        }, target, id + " " + reference.Attribute("ReferenceType").Value));
                    }
                }
            }
            expectations.Should().NotBeEmpty();
            using var assertions = new AssertionScope(model);
            foreach (var batch in expectations.Chunk(500))
            {
                var response = await session.ReadAsync(null, 0, TimestampsToReturn.Neither,
                    batch.Select(item => item.Request).ToArrayOf(), deadline.Token).ConfigureAwait(false);
                response.Results.Count.Should().Be(batch.Length);
                for (int index = 0; index < batch.Length; index++)
                {
                    StatusCode.IsGood(response.Results[index].StatusCode).Should().BeTrue(batch[index].Label);
                    response.Results[index].WrappedValue.Should().Be(batch[index].Expected, batch[index].Label);
                }
            }
            foreach (var batch in references.Chunk(200))
            {
                var response = await session.BrowseAsync(null, null, 0,
                    batch.Select(item => item.Request).ToArrayOf(), deadline.Token).ConfigureAwait(false);
                response.Results.Count.Should().Be(batch.Length);
                for (int index = 0; index < batch.Length; index++)
                {
                    StatusCode.IsGood(response.Results[index].StatusCode).Should().BeTrue(batch[index].Label);
                    response.Results[index].ContinuationPoint.Span.IsEmpty.Should().BeTrue();
                    response.Results[index].References.ToArray().Select(reference =>
                        ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris))
                        .Should().Contain(batch[index].Target, batch[index].Label);
                }
            }
            TestContext.Out.WriteLine($"{model}: checked {nodeCount} nodes, {expectations.Count} attributes, " +
                $"{references.Count} type/encoding references.");
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);

            NodeId Resolve(string value)
            {
                NodeId local = NodeId.Parse(aliases.TryGetValue(value, out string resolved) ? resolved : value);
                local.TryGetValue(out uint numeric).Should().BeTrue();
                return NodeId.Create(numeric, modelNamespaces[local.NamespaceIndex], session.NamespaceUris);
            }
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }
    }

    private static void AssertBinary(IEncodeable value, byte[] expected, Func<IEncodeable> create)
    {
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        value.Encode(encoder);
        encoder.CloseAndReturnBuffer().Should().Equal(expected);
        using var decoder = new BinaryDecoder(expected, context);
        IEncodeable decoded = create();
        decoded.Decode(decoder);
        decoded.IsEqual(value).Should().BeTrue();
    }

    private static IEncodeable CreateSample(Type type) => type.Name switch
    {
        nameof(BoilerDataType) => new BoilerDataType
        {
            Temperature = new BoilerTemperatureType { Top = -10, Bottom = 90 },
            Pressure = 100090, HeaterState = BoilerHeaterStateType.On
        },
        nameof(BoilerTemperatureType) => new BoilerTemperatureType { Top = -10, Bottom = 90 },
        nameof(SimpleEvents.CycleStepDataType) => new SimpleEvents.CycleStepDataType
        {
            Name = "Step <1> & ready", Duration = 1000.5
        },
        nameof(Opc.Ua.DI.FetchResultDataType) => new Opc.Ua.DI.FetchResultDataType(),
        nameof(Opc.Ua.DI.TransferResultErrorDataType) => new Opc.Ua.DI.TransferResultErrorDataType
        {
            Status = -123, Diagnostics = new DiagnosticInfo { AdditionalInfo = "transfer diagnostic" }
        },
        nameof(Opc.Ua.DI.ParameterResultDataType) => new Opc.Ua.DI.ParameterResultDataType
        {
            NodePath = new[] { new QualifiedName("Temperature", 2), new QualifiedName("Pressure", 3) }.ToArrayOf(),
            StatusCode = StatusCodes.BadOutOfRange,
            Diagnostics = new DiagnosticInfo { AdditionalInfo = "parameter diagnostic" }
        },
        nameof(Opc.Ua.DI.TransferResultDataDataType) => new Opc.Ua.DI.TransferResultDataDataType
        {
            SequenceNumber = 23, EndOfResults = true,
            ParameterDefs = new[] { (Opc.Ua.DI.ParameterResultDataType)CreateSample(typeof(Opc.Ua.DI.ParameterResultDataType)) }
                .ToArrayOf()
        },
        _ => throw new ArgumentException("Unexpected generated structure.", nameof(type))
    };

    private static ExpandedNodeId BoilerId(uint identifier) =>
        new(new NodeId(identifier), OpcPlc.Namespaces.OpcPlcBoiler, 0);

    private static string BaselinePath(string model) => Path.Combine(
        TestContext.CurrentContext.TestDirectory, "ModelBaselines", model + ".NodeSet2.xml");
}
