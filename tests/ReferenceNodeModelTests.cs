namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Server;
using Opc.Ua.Test;
using OpcPlc.Reference;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Range = Opc.Ua.Range;

[TestFixture]
public class ReferenceNodeModelTests
{
    private static BuiltInType[] BuiltInTypes() => Enum.GetValues<BuiltInType>();

    [TestCaseSource(nameof(BuiltInTypes))]
    public void DataItem_CreatesDeclaredTypeAndMetadata(BuiltInType type)
    {
        var fixture = new ModelFixture();
        string path = "DataAccess_DataItem_" + type;

        DataItemState node = ReferenceDataItemFactory.Create(
            fixture.Context, 2, fixture.Parent, path, type.ToString(), type, ValueRanks.Scalar);

        node.NodeId.Should().Be(new NodeId(path, 2));
        node.BrowseName.Should().Be(new QualifiedName(path, 2));
        node.DisplayName.Should().Be(new LocalizedText("en", type.ToString()));
        node.DataType.Should().Be(new NodeId((uint)type));
        node.ValueRank.Should().Be(ValueRanks.Scalar);
        node.ReferenceTypeId.Should().Be(ReferenceTypeIds.Organizes);
        node.TypeDefinitionId.Should().Be(VariableTypeIds.DataItemType);
        node.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        node.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.UserAccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.Historizing.Should().BeFalse();
        node.ValuePrecision.Value.Should().Be(2.0);
        node.ValuePrecision.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.ValuePrecision.UserAccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.Definition.Value.Should().BeEmpty();
        node.Definition.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.Definition.UserAccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        var children = new List<BaseInstanceState>();
        fixture.Parent.GetChildren(fixture.Context, children);
        children.Should().Contain(node);
    }

    [TestCase(BuiltInType.Boolean)]
    [TestCase(BuiltInType.Double)]
    [TestCase(BuiltInType.Float)]
    [TestCase(BuiltInType.Int32)]
    [TestCase(BuiltInType.UInt32)]
    public void DataItem_DefaultScalarHasExpectedValueAndType(BuiltInType type)
    {
        var fixture = new ModelFixture();
        DataItemState node = ReferenceDataItemFactory.Create(
            fixture.Context, 2, fixture.Parent, "Value", "Value", type, ValueRanks.Scalar);
        Variant expected = type switch
        {
            BuiltInType.Boolean => new Variant(false),
            BuiltInType.Double => new Variant(0.0),
            BuiltInType.Float => new Variant(0.0f),
            BuiltInType.Int32 => new Variant(0),
            _ => new Variant(0u)
        };

        node.Value.Should().Be(expected);
        node.Value.TypeInfo.BuiltInType.Should().Be(type);
        node.Value.TypeInfo.ValueRank.Should().Be(ValueRanks.Scalar);
    }

    [TestCase(ValueRanks.Scalar)]
    [TestCase(ValueRanks.OneDimension)]
    [TestCase(ValueRanks.TwoDimensions)]
    public void DataItem_DefaultDoubleRoundTripsWithDeclaredMetadata(int rank)
    {
        var fixture = new ModelFixture();
        DataItemState node = ReferenceDataItemFactory.Create(
            fixture.Context, 2, fixture.Parent, "DoubleValue", "DoubleValue", BuiltInType.Double, rank);
        if (rank == ValueRanks.OneDimension)
        {
            node.ArrayDimensions.ToArray().Should().Equal(0u);
        }
        else if (rank == ValueRanks.TwoDimensions)
        {
            node.ArrayDimensions.ToArray().Should().Equal(0u, 0u);
        }

        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariant("Value", node.Value);
        using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);
        Variant decoded = decoder.ReadVariant("Value");

        decoded.Should().Be(node.Value);
        if (rank == ValueRanks.TwoDimensions)
        {
            decoded.IsNull.Should().BeTrue();
            node.ValueRank.Should().Be(rank);
            node.DataType.Should().Be(DataTypeIds.Double);
        }
        else
        {
            decoded.TypeInfo.BuiltInType.Should().Be(BuiltInType.Double);
            decoded.TypeInfo.ValueRank.Should().Be(rank);
        }
    }

    [Test]
    public void DataItem_MatrixValueRoundTripsShapeAndPayload()
    {
        var fixture = new ModelFixture();
        DataItemState node = ReferenceDataItemFactory.Create(
            fixture.Context, 2, fixture.Parent, "Matrix", "Matrix", BuiltInType.Double, ValueRanks.TwoDimensions);
        double[,] values = { { 1.25, 2.5, 3.75 }, { -4.0, 0.0, 6.5 } };
        node.Value = Variant.From(values.ToMatrixOf());
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariant("Value", node.Value);
        using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);

        Variant decoded = decoder.ReadVariant("Value");

        decoded.Should().Be(node.Value);
        decoded.TypeInfo.BuiltInType.Should().Be(BuiltInType.Double);
        decoded.TypeInfo.ValueRank.Should().Be(ValueRanks.TwoDimensions);
        Array roundTrip = decoded.GetDoubleMatrix().CreateArrayInstance();
        roundTrip.GetLength(0).Should().Be(2);
        roundTrip.GetLength(1).Should().Be(3);
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                ((double)roundTrip.GetValue(row, column)).Should().Be(values[row, column]);
            }
        }
    }

    [Test]
    public void DataItem_CanBeCreatedWithoutParent()
    {
        var fixture = new ModelFixture();

        DataItemState node = ReferenceDataItemFactory.Create(
            fixture.Context, 2, null, "Detached", "Detached", BuiltInType.UInt32, ValueRanks.Scalar);

        node.Parent.Should().BeNull();
        node.NodeId.Should().Be(new NodeId("Detached", 2));
    }

    private static BuiltInType[] NumericTypes() =>
    [
        BuiltInType.SByte, BuiltInType.Byte, BuiltInType.Int16, BuiltInType.UInt16, BuiltInType.Int32,
        BuiltInType.UInt32, BuiltInType.Int64, BuiltInType.UInt64, BuiltInType.Float, BuiltInType.Double
    ];

    [TestCaseSource(nameof(NumericTypes))]
    public async Task Analog_MetadataAndScalarWritesPreserveInstrumentRangeAsync(BuiltInType type)
    {
        var fixture = new ModelFixture();
        AnalogItemState node = fixture.Analog(type);
        bool unsigned = type is BuiltInType.Byte or BuiltInType.UInt16 or BuiltInType.UInt32 or BuiltInType.UInt64;
        node.InstrumentRange.Value.Low.Should().Be(unsigned ? 0 : -10);
        node.InstrumentRange.Value.High.Should().Be(120);
        node.EURange.Value.Low.Should().Be(0);
        node.EURange.Value.High.Should().Be(100);
        node.EngineeringUnits.Value.UnitId.Should().Be(12890);
        node.EngineeringUnits.Value.DisplayName.Text.Should().Be("mV");
        node.EngineeringUnits.Value.Description.Text.Should().Be("millivolt");
        node.NodeId.Should().Be(new NodeId("Analog", 2));
        node.BrowseName.Should().Be(new QualifiedName("Analog", 2));
        node.TypeDefinitionId.Should().Be(VariableTypeIds.AnalogItemType);
        node.ReferenceTypeId.Should().Be(ReferenceTypeIds.Organizes);
        node.EURange.NodeId.Should().Be(new NodeId("DataItems_Analog_EURange", 2));
        node.InstrumentRange.NodeId.Should().Be(new NodeId("DataItems_Analog_InstrumentRange", 2));
        node.EngineeringUnits.NodeId.Should().Be(new NodeId("DataItems_Analog_EngineeringUnits", 2));
        node.Value.TypeInfo.BuiltInType.Should().Be(type);

        Variant accepted = new Variant(120).ConvertTo(type);
        ServiceResult.IsGood(await WriteAsync(fixture, node, accepted).ConfigureAwait(false)).Should().BeTrue();
        node.Value.Should().Be(accepted);
        Variant rejected = new Variant(121).ConvertTo(type);
        ServiceResult result = await WriteAsync(fixture, node, rejected).ConfigureAwait(false);
        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfRange);
        node.Value.Should().Be(accepted);
    }

    [Test]
    public async Task Analog_RejectsTypeAndIndexRangeWithoutChangingValueAsync()
    {
        var fixture = new ModelFixture();
        AnalogItemState node = fixture.Analog(BuiltInType.Double);
        Variant initial = node.Value;

        ServiceResult wrongType = await WriteAsync(fixture, node, new Variant("invalid")).ConfigureAwait(false);
        ServiceResult wrongRank = await WriteAsync(fixture, node,
            Variant.From(new double[] { 10 }.ToArrayOf())).ConfigureAwait(false);
        ServiceResult indexed = await WriteAsync(fixture, node, new Variant(10.0), "0").ConfigureAwait(false);

        wrongType.StatusCode.Should().Be((StatusCode)StatusCodes.BadTypeMismatch);
        wrongRank.StatusCode.Should().Be((StatusCode)StatusCodes.BadTypeMismatch);
        indexed.StatusCode.Should().Be((StatusCode)StatusCodes.BadIndexRangeInvalid);
        node.Value.Should().Be(initial);
    }

    [Test]
    public async Task Analog_OptionalInstrumentRangeAndCustomEURangeArePreservedAsync()
    {
        var fixture = new ModelFixture();
        AnalogItemState node = ReferenceDataItemFactory.CreateAnalog(fixture.Context, 2, fixture.Parent,
            "Custom", "Custom", DataTypeIds.Double, ValueRanks.Scalar, 25.0, new Range(50, 20));
        node.EURange.Value.Low.Should().Be(20);
        node.EURange.Value.High.Should().Be(50);
        node.InstrumentRange = null;

        ServiceResult result = await WriteAsync(fixture, node, new Variant(5000.0)).ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        node.Value.GetDouble().Should().Be(5000.0);
    }

    [Test]
    public async Task Analog_ArrayIndexedWritePreservesOtherElementsAndPreviousSampleAsync()
    {
        var fixture = new ModelFixture();
        AnalogItemState node = ReferenceDataItemFactory.CreateAnalog(fixture.Context, 2, fixture.Parent,
            "Array", "Array", DataTypeIds.Double, ValueRanks.OneDimension, new double[] { 1, 2, 3, 4 });
        Variant previous = node.Value;

        ServiceResult result = await WriteAsync(fixture, node,
            Variant.From(new double[] { 50, 60 }.ToArrayOf()), "1:2").ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        node.Value.GetDoubleArray().ToArray().Should().Equal(1.0, 50.0, 60.0, 4.0);
        previous.GetDoubleArray().ToArray().Should().Equal(1.0, 2.0, 3.0, 4.0);
        Variant updated = node.Value;
        ServiceResult invalid = await WriteAsync(fixture, node,
            Variant.From(new double[] { 7 }.ToArrayOf()), "8").ConfigureAwait(false);
        ServiceResult.IsBad(invalid).Should().BeTrue();
        node.Value.Should().Be(updated);
    }

    [Test]
    public async Task Analog_MatrixIndexedWritePreservesShapeAsync()
    {
        var fixture = new ModelFixture();
        double[,] values = { { 1, 2, 3 }, { 4, 5, 6 } };
        AnalogItemState node = ReferenceDataItemFactory.CreateAnalog(fixture.Context, 2, fixture.Parent,
            "Matrix", "Matrix", DataTypeIds.Double, ValueRanks.TwoDimensions, Variant.From(values.ToMatrixOf()));
        double[,] replacement = { { 20, 30 } };
        Variant previous = node.Value;

        ServiceResult result = await WriteAsync(fixture, node,
            Variant.From(replacement.ToMatrixOf()), "0,1:2").ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        double[,] expected = { { 1, 20, 30 }, { 4, 5, 6 } };
        node.Value.Should().Be(Variant.From(expected.ToMatrixOf()));
        previous.Should().Be(Variant.From(values.ToMatrixOf()));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AnalogRange_WritesDecodedStructureAndRejectsInvalidInputsAsync(bool instrument)
    {
        var fixture = new ModelFixture();
        AnalogItemState node = fixture.Analog(BuiltInType.Byte);
        PropertyState<Range> property = instrument ? node.InstrumentRange : node.EURange;
        Variant valid = Variant.FromStructure(new Range(200, 10));

        ServiceResult.IsGood(await WriteAsync(fixture, property, valid).ConfigureAwait(false)).Should().BeTrue();
        property.Value.Low.Should().Be(10);
        property.Value.High.Should().Be(200);
        ServiceResult excessive = await WriteAsync(fixture, property,
            Variant.FromStructure(new Range(256, 0))).ConfigureAwait(false);
        ServiceResult wrongType = await WriteAsync(fixture, property, new Variant(10.0)).ConfigureAwait(false);
        ServiceResult indexed = await WriteAsync(fixture, property, valid, "0").ConfigureAwait(false);

        excessive.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfRange);
        wrongType.StatusCode.Should().Be((StatusCode)StatusCodes.BadTypeMismatch);
        indexed.StatusCode.Should().Be((StatusCode)StatusCodes.BadIndexRangeInvalid);
        property.Value.High.Should().Be(200);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void TwoState_UsesSuppliedInitialValueAndPreservesText(bool initial)
    {
        var fixture = new ModelFixture();
        int calls = 0;
        TwoStateDiscreteState node = ReferenceDataItemFactory.CreateTwoState(fixture.Context, 2, fixture.Parent,
            "Switch", "Switch", "open", "closed", variable =>
            {
                variable.DataType.Should().Be(DataTypeIds.Boolean);
                calls++;
                return initial;
            });

        calls.Should().Be(1);
        node.Value.Should().Be(initial);
        node.TrueState.Value.Text.Should().Be("open");
        node.FalseState.Value.Text.Should().Be("closed");
        node.TrueState.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.FalseState.UserAccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.NodeId.Should().Be(new NodeId("DataItems_Switch", 2));
        node.TrueState.NodeId.Should().Be(new NodeId("DataItems_Switch_TrueState", 2));
    }

    [TestCase(0u, true)]
    [TestCase(2u, true)]
    [TestCase(3u, false)]
    [TestCase(uint.MaxValue, false)]
    public async Task MultiState_ValidatesEnumBoundsAsync(uint candidate, bool accepted)
    {
        var fixture = new ModelFixture();
        MultiStateDiscreteState node = ReferenceDataItemFactory.CreateMultiState(
            fixture.Context, 2, fixture.Parent, "State", "State", "open", "closed", "jammed");
        node.EnumStrings.Value.ToArray().Should().Equal(
            new LocalizedText("open"), new LocalizedText("closed"), new LocalizedText("jammed"));

        ServiceResult result = await WriteAsync(fixture, node, new Variant(candidate)).ConfigureAwait(false);

        if (accepted)
        {
            ServiceResult.IsGood(result).Should().BeTrue();
            node.Value.Should().Be(candidate);
        }
        else
        {
            result.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfRange);
            node.Value.Should().Be(0u);
        }
    }

    [Test]
    public async Task MultiState_RejectsWrongTypeAndIndexRangeAsync()
    {
        var fixture = new ModelFixture();
        MultiStateDiscreteState node = ReferenceDataItemFactory.CreateMultiState(
            fixture.Context, 2, fixture.Parent, "State", "State", "open", "closed");
        ServiceResult wrongType = await WriteAsync(fixture, node, new Variant("closed")).ConfigureAwait(false);
        ServiceResult indexed = await WriteAsync(fixture, node, new Variant(1u), "0").ConfigureAwait(false);
        wrongType.StatusCode.Should().Be((StatusCode)StatusCodes.BadTypeMismatch);
        indexed.StatusCode.Should().Be((StatusCode)StatusCodes.BadIndexRangeInvalid);
        node.Value.Should().Be(0u);
    }

    [TestCase(BuiltInType.Byte)]
    [TestCase(BuiltInType.SByte)]
    [TestCase(BuiltInType.Int16)]
    [TestCase(BuiltInType.Int32)]
    [TestCase(BuiltInType.Int64)]
    [TestCase(BuiltInType.UInt16)]
    [TestCase(BuiltInType.UInt32)]
    [TestCase(BuiltInType.UInt64)]
    public async Task MultiStateValue_WritesUpdateValueAndTextAsync(BuiltInType type)
    {
        var fixture = new ModelFixture();
        MultiStateValueDiscreteState node = ReferenceDataItemFactory.CreateMultiStateValue(
            fixture.Context, 2, fixture.Parent, "State", "State", new NodeId((uint)type), "open", "closed", "jammed");
        node.ValueAsText.Value.Text.Should().Be("open");
        node.DataType.Should().Be(new NodeId((uint)type));
        Variant value = new Variant(2).ConvertTo(type);
        ServiceResult result = await WriteAsync(fixture, node, value).ConfigureAwait(false);
        ServiceResult.IsGood(result).Should().BeTrue();
        node.Value.Should().Be(value);
        node.ValueAsText.Value.Text.Should().Be("jammed");

        ServiceResult invalid = await WriteAsync(fixture, node, new Variant(3).ConvertTo(type)).ConfigureAwait(false);
        invalid.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfRange);
        node.Value.Should().Be(value);
        node.ValueAsText.Value.Text.Should().Be("jammed");
    }

    [TestCase("two")]
    [TestCase("multi")]
    [TestCase("value")]
    public void Discrete_IdentifiersMatchOriginalCreationOrder(string kind)
    {
        var fixture = new ModelFixture();
        BaseDataVariableState original = kind switch
        {
            "two" => new TwoStateDiscreteState(fixture.Parent),
            "multi" => new MultiStateDiscreteState(fixture.Parent),
            _ => new MultiStateValueDiscreteState(fixture.Parent)
        };
        original.NodeId = new NodeId("State", 2);
        original.BrowseName = new QualifiedName("State", 2);
        original.DisplayName = new LocalizedText("en", "State");
        original.WriteMask = AttributeWriteMask.None;
        original.UserWriteMask = AttributeWriteMask.None;
        original.Create(fixture.Context, NodeId.Null, original.BrowseName, default, true);

        BaseDataVariableState migrated = kind switch
        {
            "two" => ReferenceDataItemFactory.CreateTwoState(
                fixture.Context, 2, fixture.Parent, "State", "State", "open", "closed", _ => true),
            "multi" => ReferenceDataItemFactory.CreateMultiState(
                fixture.Context, 2, fixture.Parent, "State", "State", "open", "closed"),
            _ => ReferenceDataItemFactory.CreateMultiStateValue(
                fixture.Context, 2, fixture.Parent, "State", "State", NodeId.Null, "open", "closed")
        };

        migrated.NodeId.Should().Be(original.NodeId);
        migrated.BrowseName.Should().Be(original.BrowseName);
        migrated.DisplayName.Should().Be(original.DisplayName);
        migrated.WriteMask.Should().Be(original.WriteMask);
        migrated.UserWriteMask.Should().Be(original.UserWriteMask);
        var originalChildren = new List<BaseInstanceState>();
        var migratedChildren = new List<BaseInstanceState>();
        original.GetChildren(fixture.Context, originalChildren);
        migrated.GetChildren(fixture.Context, migratedChildren);
        foreach (BaseInstanceState child in originalChildren)
        {
            migratedChildren.Should().ContainSingle(candidate => candidate.SymbolicName == child.SymbolicName)
                .Which.NodeId.Should().Be(child.NodeId);
        }
    }

    private static IEnumerable<TestCaseData> AnalogArrays()
    {
        yield return new TestCaseData(BuiltInType.Boolean, new[] { true, false });
        yield return new TestCaseData(BuiltInType.Byte, new byte[] { 0, 255 });
        yield return new TestCaseData(BuiltInType.ByteString, new byte[][] { [1, 2], [3, 4] });
        yield return new TestCaseData(BuiltInType.DateTime, new[] { DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1) });
        yield return new TestCaseData(BuiltInType.Guid, new[] { Guid.Empty, new Guid("00112233-4455-6677-8899-aabbccddeeff") });
        yield return new TestCaseData(BuiltInType.Double, new[] { 1.25, -2.5 });
        yield return new TestCaseData(BuiltInType.String, new[] { "first", "second" });
        yield return new TestCaseData(BuiltInType.UInt64, new[] { 0UL, ulong.MaxValue });
        yield return new TestCaseData(BuiltInType.Variant, new Variant[] { new(10), new("text") });
    }

    [TestCaseSource(nameof(AnalogArrays))]
    public void Analog_InitialArrayValuesRoundTrip(BuiltInType type, object initialValues)
    {
        var fixture = new ModelFixture();
        AnalogItemState node = ReferenceDataItemFactory.CreateAnalog(fixture.Context, 2, fixture.Parent,
            "Array", "Array", new NodeId((uint)type), ValueRanks.OneDimension, initialValues);
        node.Value.TypeInfo.BuiltInType.Should().Be(type);
        node.Value.TypeInfo.ValueRank.Should().Be(ValueRanks.OneDimension);
        node.ArrayDimensions.ToArray().Should().Equal(0u);
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariant("Value", node.Value);
        using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);

        decoder.ReadVariant("Value").Should().Be(node.Value);
    }

    [TestCase(-1)]
    [TestCase(3)]
    public async Task MultiStateValue_RejectsOutOfRangeWithoutChangingTextAsync(int candidate)
    {
        var fixture = new ModelFixture();
        MultiStateValueDiscreteState node = ReferenceDataItemFactory.CreateMultiStateValue(
            fixture.Context, 2, fixture.Parent, "State", "State", DataTypeIds.Int32, "open", "closed", "jammed");

        ServiceResult result = await WriteAsync(fixture, node, new Variant(candidate)).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfRange);
        node.ValueAsText.Value.Text.Should().Be("open");
    }

    [Test]
    public async Task MultiStateValue_RejectsWrongTypeAndIndexRangeAsync()
    {
        var fixture = new ModelFixture();
        MultiStateValueDiscreteState node = ReferenceDataItemFactory.CreateMultiStateValue(
            fixture.Context, 2, fixture.Parent, "State", "State", NodeId.Null, "open", "closed");

        ServiceResult wrongType = await WriteAsync(fixture, node, new Variant("closed")).ConfigureAwait(false);
        ServiceResult indexed = await WriteAsync(fixture, node, new Variant(1u), "0").ConfigureAwait(false);

        wrongType.StatusCode.Should().Be((StatusCode)StatusCodes.BadTypeMismatch);
        indexed.StatusCode.Should().Be((StatusCode)StatusCodes.BadIndexRangeInvalid);
        node.ValueAsText.Value.Text.Should().Be("open");
    }

    private static IEnumerable<TestCaseData> GeneratedValueCases()
    {
        BuiltInType[] types =
        [
            BuiltInType.Boolean, BuiltInType.Byte, BuiltInType.ByteString, BuiltInType.DateTime,
            BuiltInType.Double, BuiltInType.Guid, BuiltInType.Int32, BuiltInType.UInt64,
            BuiltInType.String, BuiltInType.LocalizedText, BuiltInType.NodeId, BuiltInType.XmlElement
        ];
        foreach (BuiltInType type in types)
        {
            foreach (int rank in new[] { ValueRanks.Scalar, ValueRanks.OneDimension, ValueRanks.TwoDimensions })
            {
                yield return new TestCaseData(type, rank);
            }
        }
    }

    [TestCaseSource(nameof(GeneratedValueCases))]
    public void GenericVariable_GeneratedValuesPreserveMetadataAndRoundTrip(BuiltInType type, int rank)
    {
        var fixture = new ModelFixture();
        var generator = new DataGenerator(new RandomSource(17), null) { BoundaryValueFrequency = 0 };
        var replay = new DataGenerator(new RandomSource(17), null) { BoundaryValueFrequency = 0 };
        string path = "Scalar_Static_" + type + "_" + rank;
        BaseDataVariableState node = ReferenceDataItemFactory.CreateVariable(
            2, fixture.Parent, path, type.ToString(), new NodeId((uint)type), rank,
            variable => ReferenceDataItemFactory.GetNewValue(generator, variable, fixture.Context.TypeTable));

        node.NodeId.Should().Be(new NodeId(path, 2));
        node.BrowseName.Should().Be(new QualifiedName(path, 2));
        node.DisplayName.Should().Be(new LocalizedText("en", type.ToString()));
        node.TypeDefinitionId.Should().Be(VariableTypeIds.BaseDataVariableType);
        node.ReferenceTypeId.Should().Be(ReferenceTypeIds.Organizes);
        node.WriteMask.Should().Be(AttributeWriteMask.DisplayName | AttributeWriteMask.Description);
        node.UserWriteMask.Should().Be(node.WriteMask);
        node.AccessLevel.Should().Be(AccessLevels.CurrentReadOrWrite);
        node.UserAccessLevel.Should().Be(node.AccessLevel);
        node.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        node.Historizing.Should().BeFalse();
        node.DataType.Should().Be(new NodeId((uint)type));
        node.ValueRank.Should().Be(rank);
        if (rank > 0)
        {
            node.ArrayDimensions.Count.Should().Be(rank);
        }
        var children = new List<BaseInstanceState>();
        fixture.Parent.GetChildren(fixture.Context, children);
        children.Should().Contain(node);

        for (int sample = 0; sample < 3; sample++)
        {
            if (sample > 0)
            {
                node.Value = ReferenceDataItemFactory.GetNewValue(generator, node, fixture.Context.TypeTable);
            }
            Variant repeated = ReferenceDataItemFactory.GetNewValue(replay, node, fixture.Context.TypeTable);
            node.Value.Should().Be(repeated);
            node.Value.IsNull.Should().BeFalse();
            node.Value.TypeInfo.BuiltInType.Should().Be(type);
            node.Value.TypeInfo.ValueRank.Should().Be(rank);
            var context = ServiceMessageContext.CreateEmpty(null);
            using var encoder = new BinaryEncoder(context);
            encoder.WriteVariant("Value", node.Value);
            using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);
            decoder.ReadVariant("Value").Should().Be(node.Value);
        }
    }

    [Test]
    public void GenericVariable_InitializerSeesDeclaredMetadataAndRunsOnce()
    {
        int calls = 0;
        BaseDataVariableState node = ReferenceDataItemFactory.CreateVariable(
            2, null, "Detached", "Value", DataTypeIds.UInt32, ValueRanks.Scalar, variable =>
            {
                calls++;
                variable.NodeId.Should().Be(new NodeId("Detached", 2));
                variable.DataType.Should().Be(DataTypeIds.UInt32);
                variable.ValueRank.Should().Be(ValueRanks.Scalar);
                return new Variant(42u);
            });

        calls.Should().Be(1);
        node.Parent.Should().BeNull();
        node.Value.GetUInt32().Should().Be(42u);
    }

    [Test]
    public void GenericVariable_RejectsMissingGenerator()
    {
        var fixture = new ModelFixture();
        var node = new BaseDataVariableState(null);
        Action generate = () => ReferenceDataItemFactory.GetNewValue(null, node, fixture.Context.TypeTable);

        generate.Should().Throw<ArgumentNullException>();
    }

    private static IEnumerable<TestCaseData> ReferenceMethodCases()
    {
        yield return new TestCaseData("Add", new Variant[] { new(1.5f), new(2u) }, new Variant(3.5f));
        yield return new TestCaseData("Multiply", new Variant[] { new((short)-3), new((ushort)4) }, new Variant(-12));
        yield return new TestCaseData("Divide", new Variant[] { new(7), new((ushort)2) }, new Variant(3.5f));
        yield return new TestCaseData("Substract", new Variant[] { new((short)-3), new((byte)4) }, new Variant((short)-7));
        yield return new TestCaseData("Hello", new Variant[] { new("PLC") }, new Variant("hello PLC"));
        yield return new TestCaseData("Output", Array.Empty<Variant>(), new Variant("Output"));
    }

    [TestCaseSource(nameof(ReferenceMethodCases))]
    public async Task ReferenceMethod_SdkCallReturnsExpectedTypeAndValueAsync(
        string name, Variant[] input, Variant expected)
    {
        var fixture = new ModelFixture();
        MethodState method = CreateReferenceMethod(fixture, name);
        var outputs = new List<Variant>();
        var errors = new List<ServiceResult>();

        ServiceResult result = await method.CallAsync(fixture.Context, fixture.Parent.NodeId,
            input.ToArrayOf(), errors, outputs, CancellationToken.None).ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        outputs.Should().ContainSingle().Which.Should().Be(expected);
        outputs[0].TypeInfo.Should().Be(expected.TypeInfo);
        errors.Should().HaveCount(input.Length);
        foreach (ServiceResult error in errors)
        {
            ServiceResult.IsGood(error).Should().BeTrue();
        }
    }

    [TestCase("Void")]
    [TestCase("Input")]
    public async Task ReferenceMethod_NoOutputContractsArePreservedAsync(string name)
    {
        var fixture = new ModelFixture();
        MethodState method = CreateReferenceMethod(fixture, name);
        ArrayOf<Variant> input = name == "Input" ? [new Variant("ignored")] : [];
        var outputs = new List<Variant>();

        ServiceResult result = await method.CallAsync(fixture.Context, fixture.Parent.NodeId,
            input, new List<ServiceResult>(), outputs, CancellationToken.None).ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        outputs.Should().BeEmpty();
    }

    [TestCase("Add")]
    [TestCase("Multiply")]
    [TestCase("Divide")]
    [TestCase("Substract")]
    [TestCase("Hello")]
    [TestCase("Input")]
    public async Task ReferenceMethod_SdkRejectsMissingAndWrongArgumentsAsync(string name)
    {
        var fixture = new ModelFixture();
        MethodState method = CreateReferenceMethod(fixture, name);
        GenericMethodCalledEventHandler callback = method.OnCallMethod;
        bool invoked = false;
        method.OnCallMethod = (context, calledMethod, arguments, results) =>
        {
            invoked = true;
            return callback(context, calledMethod, arguments, results);
        };
        var outputs = new List<Variant>();
        ServiceResult missing = await method.CallAsync(fixture.Context, fixture.Parent.NodeId,
            [], new List<ServiceResult>(), outputs, CancellationToken.None).ConfigureAwait(false);
        missing.StatusCode.Should().Be((StatusCode)StatusCodes.BadArgumentsMissing);

        var wrong = new Variant[method.InputArguments.Value.Count];
        Array.Fill(wrong, new Variant(true));
        var errors = new List<ServiceResult>();
        ServiceResult invalid = await method.CallAsync(fixture.Context, fixture.Parent.NodeId,
            wrong.ToArrayOf(), errors, outputs, CancellationToken.None).ConfigureAwait(false);

        ServiceResult.IsGood(invalid).Should().BeTrue();
        errors.Should().Contain(error => error.StatusCode == StatusCodes.BadTypeMismatch);
        invoked.Should().BeFalse();
        outputs.Should().BeEmpty();
    }

    [TestCase("Add")]
    [TestCase("Multiply")]
    [TestCase("Divide")]
    [TestCase("Substract")]
    public void ReferenceMethod_CallbackRejectsMismatchedScalarTypes(string name)
    {
        var fixture = new ModelFixture();
        MethodState method = CreateReferenceMethod(fixture, name);
        var outputs = new List<Variant> { new Variant("unchanged") };
        ArrayOf<Variant>[] invalidInputs =
        [
            [new Variant("invalid"), new Variant(2u)],
            [Variant.Null, Variant.Null],
            [Variant.From(new uint[] { 1 }.ToArrayOf()), new Variant(2u)]
        ];
        foreach (ArrayOf<Variant> input in invalidInputs)
        {
            ServiceResult result = method.OnCallMethod(fixture.Context, method, input, outputs);
            result.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidArgument);
            outputs[0].Should().Be(new Variant("unchanged"));
        }
    }

    [TestCaseSource(nameof(ReferenceMethodCases))]
    public void ReferenceMethod_CallbackRejectsMissingOutputSlot(string name, Variant[] input, Variant expected)
    {
        var fixture = new ModelFixture();
        MethodState method = CreateReferenceMethod(fixture, name);

        ServiceResult result = method.OnCallMethod(fixture.Context, method, input.ToArrayOf(), []);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidArgument);
    }

    [Test]
    public void ReferenceMethod_PreservesArithmeticAndNullStringEdgeCases()
    {
        var outputs = new List<Variant> { Variant.Null };
        ServiceResult.IsGood(ReferenceMethodHandlers.OnDivideCall(null, null,
            [new Variant(1), new Variant((ushort)0)], outputs)).Should().BeTrue();
        float.IsPositiveInfinity(outputs[0].GetFloat()).Should().BeTrue();
        ReferenceMethodHandlers.OnDivideCall(null, null, [new Variant(0), new Variant((ushort)0)], outputs);
        float.IsNaN(outputs[0].GetFloat()).Should().BeTrue();
        ReferenceMethodHandlers.OnSubtractCall(null, null,
            [new Variant(short.MinValue), new Variant((byte)1)], outputs);
        outputs[0].GetInt16().Should().Be(short.MaxValue);
        ReferenceMethodHandlers.OnMultiplyCall(null, null,
            [new Variant(short.MinValue), new Variant(ushort.MaxValue)], outputs);
        outputs[0].GetInt32().Should().Be(-2147450880);
        ServiceResult.IsGood(ReferenceMethodHandlers.OnHelloCall(null, null, [Variant.Null], outputs))
            .Should().BeTrue();
        outputs[0].GetString().Should().Be("hello ");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ReferenceMethod_ArgumentPropertyPreservesMetadataAndEncoding(bool input)
    {
        var method = new MethodState(null) { BrowseName = new QualifiedName("Methods_Add", 2) };
        PropertyState<ArrayOf<Argument>> property = ReferenceMethodHandlers.CreateArguments(method, input,
            ("Float value", DataTypeIds.Float), ("UInt32 value", DataTypeIds.UInt32));
        string name = input ? BrowseNames.InputArguments : BrowseNames.OutputArguments;

        property.Parent.Should().BeSameAs(method);
        property.NodeId.Should().Be(new NodeId("Methods_Add" + (input ? "InArgs" : "OutArgs"), 2));
        property.BrowseName.Should().Be(new QualifiedName(name));
        property.DisplayName.Should().Be(new LocalizedText(name));
        property.ReferenceTypeId.Should().Be(ReferenceTypeIds.HasProperty);
        property.TypeDefinitionId.Should().Be(VariableTypeIds.PropertyType);
        property.DataType.Should().Be(DataTypeIds.Argument);
        property.ValueRank.Should().Be(ValueRanks.OneDimension);
        property.Value.Count.Should().Be(2);
        property.Value[0].Name.Should().Be("Float value");
        property.Value[0].Description.Should().Be(new LocalizedText("Float value"));
        property.Value[0].DataType.Should().Be(DataTypeIds.Float);
        property.Value[1].DataType.Should().Be(DataTypeIds.UInt32);
        property.Value[0].ValueRank.Should().Be(ValueRanks.Scalar);
        var context = ServiceMessageContext.CreateEmpty(null);
        Variant value = Variant.FromStructure(property.Value);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariant("Arguments", value);
        encoder.CloseAndReturnBuffer().Should().NotBeEmpty();
    }

    private static MethodState CreateReferenceMethod(ModelFixture fixture, string name)
    {
        var method = new MethodState(fixture.Parent)
        {
            BrowseName = new QualifiedName("Methods_" + name, 2),
            NodeId = new NodeId("Methods_" + name, 2),
            Executable = true,
            UserExecutable = true
        };
        (NodeId[] Inputs, NodeId Output, GenericMethodCalledEventHandler Handler) definition = name switch
        {
            "Add" => ([DataTypeIds.Float, DataTypeIds.UInt32], DataTypeIds.Float, ReferenceMethodHandlers.OnAddCall),
            "Multiply" => ([DataTypeIds.Int16, DataTypeIds.UInt16], DataTypeIds.Int32,
                ReferenceMethodHandlers.OnMultiplyCall),
            "Divide" => ([DataTypeIds.Int32, DataTypeIds.UInt16], DataTypeIds.Float, ReferenceMethodHandlers.OnDivideCall),
            "Substract" => ([DataTypeIds.Int16, DataTypeIds.Byte], DataTypeIds.Int16,
                ReferenceMethodHandlers.OnSubtractCall),
            "Hello" => ([DataTypeIds.String], DataTypeIds.String, ReferenceMethodHandlers.OnHelloCall),
            "Input" => ([DataTypeIds.String], NodeId.Null, ReferenceMethodHandlers.OnInputCall),
            "Output" => ([], DataTypeIds.String, ReferenceMethodHandlers.OnOutputCall),
            _ => ([], NodeId.Null, ReferenceMethodHandlers.OnVoidCall)
        };
        method.OnCallMethod = definition.Handler;
        if (definition.Inputs.Length > 0)
        {
            var arguments = new (string, NodeId)[definition.Inputs.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                arguments[index] = ("Argument" + index, definition.Inputs[index]);
            }
            method.InputArguments = ReferenceMethodHandlers.CreateArguments(method, true, arguments);
        }
        if (!definition.Output.IsNull)
        {
            method.OutputArguments = ReferenceMethodHandlers.CreateArguments(method, false, ("Result", definition.Output));
        }
        return method;
    }

    private static async Task<ServiceResult> WriteAsync(
        ModelFixture fixture, BaseVariableState node, Variant value, string range = null)
    {
        return await node.WriteAttributeAsync(fixture.Context, Attributes.Value, NumericRange.Parse(range),
            new DataValue(value), CancellationToken.None).ConfigureAwait(false);
    }

    [Test]
    public async Task ReferenceAddressSpace_CompletesWithoutErrorsAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);

        fixture.Logger.Errors.Should().BeEmpty();
        fixture.Find<NodeState>("MyCompany_Instructions").Should().NotBeNull();
        fixture.Find<BaseDataVariableState>("Scalar_Static_Double").Value.TypeInfo.BuiltInType
            .Should().Be(BuiltInType.Double);
        fixture.ExternalReferences[ObjectIds.ObjectsFolder].Should().Contain(reference =>
            reference.ReferenceTypeId == ReferenceTypeIds.Organizes && !reference.IsInverse &&
            reference.TargetId == new ExpandedNodeId(fixture.Id("ReferenceTest")));
    }

    [Test]
    public async Task ReferenceAddressSpace_MethodArgumentsHaveDistinctIdsAndCorrectOwnersAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        MethodState hello = fixture.Find<MethodState>("Methods_Hello");
        MethodState output = fixture.Find<MethodState>("Methods_Output");

        hello.Should().NotBeNull();
        output.Should().NotBeNull();
        output.OutputArguments.Parent.Should().BeSameAs(output);
        output.OutputArguments.NodeId.Should().NotBe(hello.OutputArguments.NodeId);
        fixture.Find<PropertyState<ArrayOf<Argument>>>("Methods_HelloOutArgs")
            .Should().BeSameAs(hello.OutputArguments);
        fixture.Find<PropertyState<ArrayOf<Argument>>>("Methods_OutputOutArgs")
            .Should().BeSameAs(output.OutputArguments);
        hello.OutputArguments.Value[0].Name.Should().Be("Hello Result");
        output.OutputArguments.Value[0].Name.Should().Be("Output Result");
    }

    [Test]
    public async Task ReferenceAddressSpace_ManualValuesKeepTheirWireTypesAndPayloadsAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        Variant decimalValue = fixture.Find<BaseDataVariableState>("Scalar_Static_Decimal").Value;
        DecimalDataType decimalData = decimalValue.GetStructure<DecimalDataType>();
        decimalData.Scale.Should().Be(100);
        new BigInteger(decimalData.Value.Span).Should().Be(
            BigInteger.Parse("1234567890123546789012345678901234567890123456789012345"));

        ArrayOf<QualifiedName> names = fixture.Find<BaseDataVariableState>(
            "DataAccess_AnalogType_Array_QualifiedName").Value.GetQualifiedNameArray();
        names.Count.Should().Be(10);
        for (int index = 0; index < names.Count; index++)
        {
            names[index].Should().Be(new QualifiedName("q" + index));
        }

        ArrayOf<Opc.Ua.XmlElement> elements = fixture.Find<BaseDataVariableState>(
            "DataAccess_AnalogType_Array_XmlElement").Value.GetXmlElementArray();
        elements.Count.Should().Be(10);
        for (int index = 0; index < elements.Count; index++)
        {
            ((System.Xml.XmlElement)elements[index]).LocalName.Should().Be("tag" + (index + 1));
        }

        Variant strings = fixture.Find<BaseDataVariableState>("Scalar_Static_Arrays_String").Value;
        strings.GetStringArray().Count.Should().Be(10);
        strings.GetStringArray()[4].Should().Be("Horse# Black Lemon Lemon Grape");
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariant("Strings", strings);
        using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);
        decoder.ReadVariant("Strings").Should().Be(strings);
    }

    [Test]
    public async Task ReferenceAddressSpace_StaticArrayAdjustmentsAreAssignedBackAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        ArrayOf<double> doubles = fixture.Find<BaseDataVariableState>("Scalar_Static_Arrays_Double")
            .Value.GetDoubleArray();
        ArrayOf<float> floats = fixture.Find<BaseDataVariableState>("Scalar_Static_Arrays_Float")
            .Value.GetFloatArray();
        doubles.Count.Should().BeGreaterThanOrEqualTo(4);
        floats.Count.Should().BeGreaterThanOrEqualTo(4);
        for (int index = 0; index < 4; index++)
        {
            Math.Abs(doubles[index]).Should().BeLessThan(10E+10);
            Math.Abs(floats[index]).Should().BeLessThan(0xf10E + 4);
        }
    }

    [Test]
    public async Task ReferenceAddressSpace_PreservesReferenceDirectionsAndViewsAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var forward = new List<IReference>();
        var inverse = new List<IReference>();
        fixture.Find<NodeState>("References_HasForwardReference").GetReferences(fixture.Manager.SystemContext, forward);
        fixture.Find<NodeState>("References_HasInverseReference").GetReferences(fixture.Manager.SystemContext, inverse);
        var target = new ExpandedNodeId(fixture.Id("Scalar_Instructions"));
        forward.Should().Contain(reference => reference.ReferenceTypeId == ReferenceTypeIds.HasCause &&
            !reference.IsInverse && reference.TargetId == target);
        inverse.Should().Contain(reference => reference.ReferenceTypeId == ReferenceTypeIds.HasCause &&
            reference.IsInverse && reference.TargetId == target);

        ViewState view = fixture.Find<ViewState>("Views_Operations");
        view.ContainsNoLoops.Should().BeTrue();
        fixture.ExternalReferences[ObjectIds.ViewsFolder].Should().Contain(reference =>
            reference.ReferenceTypeId == ReferenceTypeIds.Organizes && !reference.IsInverse &&
            reference.TargetId == new ExpandedNodeId(view.NodeId));
    }

    [TestCase("AnonymousAccess")]
    [TestCase("AuthenticatedUser")]
    [TestCase("AdminUser")]
    public async Task ReferenceAddressSpace_PreservesRolePermissionsAsync(string name)
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        BaseDataVariableState node = fixture.Find<BaseDataVariableState>("AccessRights_RolePermissions_" + name);
        NodeId role = name switch
        {
            "AnonymousAccess" => ObjectIds.WellKnownRole_Anonymous,
            "AuthenticatedUser" => ObjectIds.WellKnownRole_AuthenticatedUser,
            _ => ObjectIds.WellKnownRole_SecurityAdmin
        };
        node.RolePermissions.Count.Should().Be(1);
        node.RolePermissions[0].RoleId.Should().Be(role);
        node.RolePermissions[0].Permissions.Should().Be((uint)(PermissionType.Browse | PermissionType.Read |
            PermissionType.ReadRolePermissions | PermissionType.Write));
        if (name == "AdminUser")
        {
            node.AccessRestrictions.Should().Be(AccessRestrictionType.EncryptionRequired);
        }
    }

    [TestCase("Hello", "hello PLC")]
    [TestCase("Output", "Output")]
    public async Task ReferenceAddressSpace_RegisteredMethodsReturnIndependentResultsAsync(string name, string expected)
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        MethodState method = fixture.Find<MethodState>("Methods_" + name);
        ArrayOf<Variant> inputs = name == "Hello" ? [new Variant("PLC")] : [];
        var outputs = new List<Variant>();
        var errors = new List<ServiceResult>();

        ServiceResult result = await method.CallAsync(fixture.Manager.SystemContext, fixture.Id("Methods"),
            inputs, errors, outputs, CancellationToken.None).ConfigureAwait(false);

        ServiceResult.IsGood(result).Should().BeTrue();
        outputs.Should().ContainSingle().Which.GetString().Should().Be(expected);
        foreach (ServiceResult error in errors)
        {
            ServiceResult.IsGood(error).Should().BeTrue();
        }
    }

    [Test]
    public async Task ReferenceAddressSpace_SimulationControlsAcceptTypedWritesAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        BaseDataVariableState interval = fixture.Find<BaseDataVariableState>("Scalar_Simulation_Interval");
        BaseDataVariableState enabled = fixture.Find<BaseDataVariableState>("Scalar_Simulation_Enabled");
        interval.Value.GetUInt16().Should().Be(1000);
        enabled.Value.GetBoolean().Should().BeTrue();
        var context = fixture.Manager.SystemContext;

        ServiceResult stopped = await enabled.WriteAttributeAsync(context, Attributes.Value, default,
            new DataValue(new Variant(false)), CancellationToken.None).ConfigureAwait(false);
        ServiceResult changed = await interval.WriteAttributeAsync(context, Attributes.Value, default,
            new DataValue(new Variant((ushort)250)), CancellationToken.None).ConfigureAwait(false);
        ServiceResult restarted = await enabled.WriteAttributeAsync(context, Attributes.Value, default,
            new DataValue(new Variant(true)), CancellationToken.None).ConfigureAwait(false);

        ServiceResult.IsGood(stopped).Should().BeTrue();
        ServiceResult.IsGood(changed).Should().BeTrue();
        ServiceResult.IsGood(restarted).Should().BeTrue();
        interval.Value.GetUInt16().Should().Be(250);
        enabled.Value.GetBoolean().Should().BeTrue();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ReferenceAddressSpace_PreCanceledCreate_DoesNotRegisterNodesAsync()
    {
        var fixture = new ReferenceFixture();
        await using var cleanup = fixture.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> create = () => fixture.Manager.CreateAddressSpaceAsync(
            fixture.ExternalReferences, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        fixture.ExternalReferences.Should().BeEmpty();
        fixture.Find<NodeState>("ReferenceTest").Should().BeNull();
    }

    [Test]
    public async Task ReferenceAddressSpace_Delete_RemovesNodesAndRejectsControlWritesAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var enabled = fixture.Find<BaseDataVariableState>("Scalar_Simulation_Enabled");
        var interval = fixture.Find<BaseDataVariableState>("Scalar_Simulation_Interval");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> delete = () => fixture.Manager.DeleteAddressSpaceAsync(cancellation.Token).AsTask();
        await delete.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        (await fixture.Manager.GetManagerHandleAsync(enabled.NodeId).ConfigureAwait(false))
            .Should().BeOfType<NodeHandle>().Which.Node.Should().BeSameAs(enabled);
        (await fixture.Manager.GetManagerHandleAsync(ObjectIds.ObjectsFolder).ConfigureAwait(false)).Should().BeNull();
        (await fixture.Manager.GetManagerHandleAsync(fixture.Id("Missing")).ConfigureAwait(false)).Should().BeNull();

        await fixture.Manager.DeleteAddressSpaceAsync().ConfigureAwait(false);

        fixture.Find<NodeState>("ReferenceTest").Should().BeNull();
        (await fixture.Manager.GetManagerHandleAsync(enabled.NodeId).ConfigureAwait(false)).Should().BeNull();
        ServiceResult enableResult = await enabled.WriteAttributeAsync(fixture.Manager.SystemContext,
            Attributes.Value, default, new DataValue(new Variant(false))).ConfigureAwait(false);
        ServiceResult intervalResult = await interval.WriteAttributeAsync(fixture.Manager.SystemContext,
            Attributes.Value, default, new DataValue(new Variant((ushort)250))).ConfigureAwait(false);
        enableResult.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfService);
        intervalResult.StatusCode.Should().Be((StatusCode)StatusCodes.BadOutOfService);
        enabled.Value.GetBoolean().Should().BeTrue();
        interval.Value.GetUInt16().Should().Be(1000);
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ReferenceAddressSpace_Delete_DrainsSimulationBeforeRemovingNodesAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var variable = fixture.Find<BaseDataVariableState>("Scalar_Simulation_UInt32");
        var nextVariable = fixture.Find<BaseDataVariableState>("Scalar_Simulation_UInt64");
        DateTimeUtc originalTimestamp = nextVariable.Timestamp;
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        variable.StateChanged += (context, node, changes) =>
        {
            if ((changes & NodeStateChangeMasks.Value) == 0)
            {
                return;
            }
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Simulation callback was not released.");
            }
        };
        Task deletion = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            deletion = fixture.Manager.DeleteAddressSpaceAsync().AsTask();
            deletion.Wait(TimeSpan.FromMilliseconds(250)).Should().BeFalse(
                "node deletion must wait for the active simulation callback");
            fixture.Find<NodeState>("ReferenceTest").Should().NotBeNull();
        }
        finally
        {
            release.Set();
            await (deletion ?? fixture.Manager.DeleteAddressSpaceAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        nextVariable.Timestamp.Should().Be(originalTimestamp, "the stopped iteration must not update later nodes");
        fixture.Find<NodeState>("ReferenceTest").Should().BeNull();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ReferenceAddressSpace_DisablePreservesFinalTickAndEnableRestartsAsync()
    {
        var fixture = await ReferenceFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var enabled = fixture.Find<BaseDataVariableState>("Scalar_Simulation_Enabled");
        var variable = fixture.Find<BaseDataVariableState>("Scalar_Simulation_UInt32");
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int updates = 0;
        variable.StateChanged += (context, node, changes) =>
        {
            if ((changes & NodeStateChangeMasks.Value) != 0)
            {
                if (Interlocked.Increment(ref updates) == 1)
                {
                    first.TrySetResult();
                }
                else
                {
                    second.TrySetResult();
                }
            }
        };

        ServiceResult stopped = await enabled.WriteAttributeAsync(fixture.Manager.SystemContext,
            Attributes.Value, default, new DataValue(new Variant(false))).ConfigureAwait(false);
        ServiceResult.IsGood(stopped).Should().BeTrue();
        await first.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        second.Task.Wait(TimeSpan.FromMilliseconds(300)).Should().BeFalse();

        ServiceResult started = await enabled.WriteAttributeAsync(fixture.Manager.SystemContext,
            Attributes.Value, default, new DataValue(new Variant(true))).ConfigureAwait(false);
        ServiceResult.IsGood(started).Should().BeTrue();
        await second.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ReferenceAddressSpace_WireReadsWritesMethodsAndMonitoringAsync()
    {
        var fixture = new PlcSimulatorFixture(["--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var session = await fixture.CreateSessionAsync("NativeReference").ConfigureAwait(false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var manager = fixture.Server.SimulationNodeManager;
            manager.Should().NotBeNull().And.BeAssignableTo<AsyncCustomNodeManager>();
            fixture.Server.CurrentInstance.NodeManager.AsyncNodeManagers.Should().Contain(manager);
            NodeId Id(string path) => NodeId.Create(path, OpcPlc.Namespaces.OpcPlcReferenceTest, session.NamespaceUris);
            var owner = await fixture.Server.CurrentInstance.NodeManager.GetManagerHandleAsync(
                Id("Scalar_Static_Int32"), deadline.Token).ConfigureAwait(false);
            owner.nodeManager.Should().BeSameAs(manager);
            owner.handle.Should().NotBeNull();

            var writes = await session.WriteAsync(null,
            [
                new WriteValue
                {
                    NodeId = Id("Scalar_Static_Int32"), AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(42))
                },
                new WriteValue
                {
                    NodeId = Id("Scalar_Static_Arrays_UInt32"), AttributeId = Attributes.Value,
                    Value = new DataValue(Variant.From(new uint[] { 2, 4, 8 }.ToArrayOf()))
                },
                new WriteValue
                {
                    NodeId = Id("Scalar_Simulation_Enabled"), AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant("invalid"))
                }
            ], deadline.Token).ConfigureAwait(false);
            writes.Results.ToArray().Should().Equal(
                (StatusCode)StatusCodes.Good, (StatusCode)StatusCodes.Good, (StatusCode)StatusCodes.BadTypeMismatch);
            (await session.ReadValueAsync(Id("Scalar_Static_Int32"), deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetInt32().Should().Be(42);
            (await session.ReadValueAsync(Id("Scalar_Static_Arrays_UInt32"), deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetUInt32Array().ToArray().Should().Equal(2u, 4u, 8u);
            (await session.ReadValueAsync(Id("Scalar_Simulation_Enabled"), deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetBoolean().Should().BeTrue();

            var calls = await session.CallAsync(null,
            [
                new CallMethodRequest
                {
                    ObjectId = Id("Methods"), MethodId = Id("Methods_Hello"),
                    InputArguments = [new Variant("PLC")]
                },
                new CallMethodRequest
                {
                    ObjectId = Id("Methods"), MethodId = Id("Methods_Output"), InputArguments = []
                },
                new CallMethodRequest
                {
                    ObjectId = Id("Methods"), MethodId = Id("Methods_Add"),
                    InputArguments = [new Variant(1.5f), new Variant(2u)]
                }
            ], deadline.Token).ConfigureAwait(false);
            calls.Results.ToArray().Should().OnlyContain(result => result.StatusCode == StatusCodes.Good);
            calls.Results[0].OutputArguments[0].GetString().Should().Be("hello PLC");
            calls.Results[1].OutputArguments[0].GetString().Should().Be("Output");
            calls.Results[2].OutputArguments[0].GetFloat().Should().Be(3.5f);

            using var subscription = new Opc.Ua.Client.Subscription(session.DefaultSubscription)
            {
                PublishingInterval = 100
            };
            session.AddSubscription(subscription);
            try
            {
                await subscription.CreateAsync(deadline.Token).ConfigureAwait(false);
                var item = new Opc.Ua.Client.MonitoredItem(subscription.DefaultItem)
                {
                    StartNodeId = Id("Scalar_Simulation_UInt32"), AttributeId = Attributes.Value,
                    SamplingInterval = 0, QueueSize = 10
                };
                var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                int notifications = 0;
                item.Notification += (monitoredItem, notification) =>
                {
                    foreach (DataValue value in monitoredItem.DequeueValues())
                    {
                        if (StatusCode.IsGood(value.StatusCode) && value.WrappedValue.TryGetValue(out uint _))
                        {
                            if (Interlocked.Increment(ref notifications) >= 2)
                            {
                                updated.TrySetResult();
                            }
                        }
                    }
                };
                subscription.AddItem(item);
                await subscription.ApplyChangesAsync(deadline.Token).ConfigureAwait(false);
                await updated.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
            }
            finally
            {
                await subscription.DeleteAsync(true, CancellationToken.None).ConfigureAwait(false);
                await session.RemoveSubscriptionAsync(subscription, CancellationToken.None).ConfigureAwait(false);
            }
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }

    private sealed class ReferenceFixture : IAsyncDisposable
    {
        public CaptureLogger Logger { get; } = new();
        public ReferenceNodeManager Manager { get; }
        public Dictionary<NodeId, IList<IReference>> ExternalReferences { get; } = new();

        public ReferenceFixture()
        {
            ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:server");
            var typeTable = new TypeTable(namespaces);
            var server = new Mock<IServerInternal>();
            server.SetupGet(instance => instance.Telemetry).Returns(telemetry);
            server.SetupGet(instance => instance.NamespaceUris).Returns(namespaces);
            server.SetupGet(instance => instance.ServerUris).Returns(new StringTable());
            server.SetupGet(instance => instance.TypeTree).Returns(typeTable);
            server.SetupGet(instance => instance.Factory).Returns(EncodeableFactory.Create());
            server.SetupGet(instance => instance.DefaultSystemContext).Returns(new ServerSystemContext(server.Object));
            var master = new Mock<IMasterNodeManager>();
            server.SetupGet(instance => instance.NodeManager).Returns(master.Object);
            Manager = new ReferenceNodeManager(server.Object,
                new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() }, Logger, telemetry);
            master.SetupGet(instance => instance.AsyncNodeManagers).Returns([Manager]);
        }

        public static async Task<ReferenceFixture> CreateAsync()
        {
            var fixture = new ReferenceFixture();
            try
            {
                await fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public NodeId Id(string path) => new(path, Manager.NamespaceIndex);
        public T Find<T>(string path) where T : NodeState => Manager.FindPredefinedNode<T>(Id(path));

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
            }
            finally
            {
                Manager.Dispose();
            }
        }
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<string> Errors { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(formatter(state, exception) + ": " + exception);
            }
        }
    }

    private sealed class ModelFixture
    {
        public SystemContext Context { get; }
        public FolderState Parent { get; } = new(null) { NodeId = new NodeId("DataItems", 2) };

        public AnalogItemState Analog(BuiltInType type) => ReferenceDataItemFactory.CreateAnalog(
            Context, 2, Parent, "Analog", "Analog", new NodeId((uint)type), ValueRanks.Scalar);

        public ModelFixture()
        {
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:application");
            namespaces.GetIndexOrAppend("urn:opcplc:test:reference");
            var nodeIdFactory = new Mock<INodeIdFactory>();
            nodeIdFactory.Setup(factory => factory.New(It.IsAny<ISystemContext>(), It.IsAny<NodeState>()))
                .Returns((ISystemContext _, NodeState node) =>
                    node is BaseInstanceState instance && instance.Parent != null &&
                    instance.Parent.NodeId.TryGetValue(out string parentId)
                        ? new NodeId(parentId + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex)
                        : node.NodeId);
            Context = new SystemContext(null)
            {
                NamespaceUris = namespaces,
                TypeTable = new TypeTable(namespaces),
                NodeIdFactory = nodeIdFactory.Object
            };
        }
    }
}