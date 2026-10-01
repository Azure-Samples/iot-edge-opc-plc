namespace OpcPlc.Reference;

using Opc.Ua;
using Opc.Ua.Test;
using System;
using Range = Opc.Ua.Range;

public static class ReferenceDataItemFactory
{
    public static BaseDataVariableState CreateVariable(
        ushort namespaceIndex, NodeState parent, string path, string name,
        NodeId dataType, int valueRank, Func<BaseVariableState, Variant> getInitialValue)
    {
        var variable = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId(path, namespaceIndex),
            BrowseName = new QualifiedName(path, namespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
            UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
            DataType = dataType,
            ValueRank = valueRank,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Historizing = false,
        };
        variable.Value = getInitialValue(variable);
        variable.StatusCode = StatusCodes.Good;
        variable.Timestamp = DateTime.UtcNow;
        if (valueRank == ValueRanks.OneDimension)
        {
            variable.ArrayDimensions = [0];
        }
        else if (valueRank == ValueRanks.TwoDimensions)
        {
            variable.ArrayDimensions = [0, 0];
        }
        parent?.AddChild(variable);
        return variable;
    }

    public static Variant GetNewValue(DataGenerator generator, BaseVariableState variable, ITypeTable typeTable)
    {
        ArgumentNullException.ThrowIfNull(generator);
        Variant value = Variant.Null;
        int retryCount = 0;
        while (value.IsNull && retryCount < 10)
        {
            value = generator.GetRandom(variable.DataType, variable.ValueRank, [10], typeTable);
            retryCount++;
        }
        return value;
    }

    public static DataItemState Create(
        ISystemContext context,
        ushort namespaceIndex,
        NodeState parent,
        string path,
        string name,
        BuiltInType dataType,
        int valueRank)
    {
        var variable = new DataItemState(parent);
        variable.ValuePrecision = PropertyState<double>.With<VariantBuilder>(variable);
        variable.Definition = PropertyState<string>.With<VariantBuilder>(variable);
        variable.Create(context, NodeId.Null, variable.BrowseName, default, true);

        variable.SymbolicName = name;
        variable.ReferenceTypeId = ReferenceTypeIds.Organizes;
        variable.NodeId = new NodeId(path, namespaceIndex);
        variable.BrowseName = new QualifiedName(path, namespaceIndex);
        variable.DisplayName = new LocalizedText("en", name);
        variable.WriteMask = AttributeWriteMask.None;
        variable.UserWriteMask = AttributeWriteMask.None;
        variable.DataType = new NodeId((uint)dataType);
        variable.ValueRank = valueRank;
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.Historizing = false;
        variable.Value = valueRank >= ValueRanks.TwoDimensions
            ? Variant.Null
            : Variant.CreateDefault(TypeInfo.Create(dataType, valueRank));
        variable.StatusCode = StatusCodes.Good;
        variable.Timestamp = DateTime.UtcNow;

        if (valueRank == ValueRanks.OneDimension)
        {
            variable.ArrayDimensions = [0];
        }
        else if (valueRank == ValueRanks.TwoDimensions)
        {
            variable.ArrayDimensions = [0, 0];
        }

        variable.ValuePrecision.Value = 2;
        variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.Definition.Value = string.Empty;
        variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

        parent?.AddChild(variable);
        return variable;
    }

    public static AnalogItemState CreateAnalog(
        ISystemContext context, ushort namespaceIndex, NodeState parent, string path, string name,
        NodeId dataType, int valueRank, object initialValues = null, Range customRange = null)
    {
        var variable = new AnalogItemState(parent) { BrowseName = new QualifiedName(path, namespaceIndex) };
        variable.EngineeringUnits = PropertyState<EUInformation>.With<StructureBuilder<EUInformation>>(variable);
        variable.InstrumentRange = PropertyState<Range>.With<StructureBuilder<Range>>(variable);
        variable.Create(context, new NodeId(path, namespaceIndex), variable.BrowseName, default, true);
        SetMetadata(variable, namespaceIndex, path, name, dataType, valueRank);
        variable.NodeId = new NodeId(path, namespaceIndex);
        variable.DisplayName = new LocalizedText("en", name);
        variable.WriteMask = AttributeWriteMask.None;
        variable.UserWriteMask = AttributeWriteMask.None;

        BuiltInType builtInType = TypeInfo.GetBuiltInType(dataType, context.TypeTable);
        Range instrumentRange = GetAnalogRange(builtInType);
        instrumentRange.High = Math.Min(instrumentRange.High, 120);
        instrumentRange.Low = Math.Max(instrumentRange.Low, -10);
        variable.InstrumentRange.Value = instrumentRange;
        variable.EURange.Value = customRange ?? new Range(100, 0);
        variable.Value = initialValues switch
        {
            null => valueRank >= ValueRanks.TwoDimensions
                ? Variant.Null
                : Variant.CreateDefault(TypeInfo.Create(builtInType, valueRank)),
            byte[] bytes when builtInType == BuiltInType.ByteString && valueRank == ValueRanks.Scalar
                => Variant.From((ByteString)bytes),
            byte[][] byteStrings => Variant.From(byteStrings.ToArrayOf(bytes => (ByteString)bytes)),
            DateTime[] dates => Variant.From(dates.ToArrayOf(date => (DateTimeUtc)date)),
            _ => VariantHelper.CastFrom(initialValues)
        };
        variable.EngineeringUnits.Value = new EUInformation(
            "mV", "millivolt", "http://www.opcfoundation.org/UA/units/un/cefact") { UnitId = 12890 };
        variable.OnWriteValue = OnWriteAnalog;
        variable.EURange.OnWriteValue = OnWriteAnalogRange;
        variable.InstrumentRange.OnWriteValue = OnWriteAnalogRange;
        SetWritable(variable.EURange);
        SetWritable(variable.EngineeringUnits);
        SetWritable(variable.InstrumentRange);
        parent?.AddChild(variable);
        return variable;
    }

    public static TwoStateDiscreteState CreateTwoState(
        ISystemContext context, ushort namespaceIndex, NodeState parent, string path, string name,
        string trueState, string falseState, Func<BaseVariableState, Variant> getInitialValue)
    {
        var variable = new TwoStateDiscreteState(parent)
        {
            NodeId = new NodeId(path, namespaceIndex),
            BrowseName = new QualifiedName(path, namespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None
        };
        variable.Create(context, NodeId.Null, variable.BrowseName, default, true);
        SetMetadata(variable, namespaceIndex, path, name, DataTypeIds.Boolean, ValueRanks.Scalar);
        variable.Value = (bool)getInitialValue(variable);
        variable.TrueState.Value = new LocalizedText(trueState);
        variable.FalseState.Value = new LocalizedText(falseState);
        SetWritable(variable.TrueState);
        SetWritable(variable.FalseState);
        parent?.AddChild(variable);
        return variable;
    }

    public static MultiStateDiscreteState CreateMultiState(
        ISystemContext context, ushort namespaceIndex, NodeState parent, string path, string name, params string[] values)
    {
        var variable = new MultiStateDiscreteState(parent)
        {
            NodeId = new NodeId(path, namespaceIndex),
            BrowseName = new QualifiedName(path, namespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None
        };
        variable.Create(context, NodeId.Null, variable.BrowseName, default, true);
        SetMetadata(variable, namespaceIndex, path, name, DataTypeIds.UInt32, ValueRanks.Scalar);
        variable.Value = 0u;
        variable.OnWriteValue = OnWriteDiscrete;
        var strings = new LocalizedText[values.Length];
        for (int index = 0; index < strings.Length; index++)
        {
            strings[index] = new LocalizedText(values[index]);
        }
        variable.EnumStrings.Value = strings.ToArrayOf();
        SetWritable(variable.EnumStrings);
        parent?.AddChild(variable);
        return variable;
    }

    public static MultiStateValueDiscreteState CreateMultiStateValue(
        ISystemContext context, ushort namespaceIndex, NodeState parent, string path, string name,
        NodeId dataType, params string[] enumNames)
    {
        var variable = new MultiStateValueDiscreteState(parent)
        {
            NodeId = new NodeId(path, namespaceIndex),
            BrowseName = new QualifiedName(path, namespaceIndex),
            DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None
        };
        variable.Create(context, NodeId.Null, variable.BrowseName, default, true);
        SetMetadata(variable, namespaceIndex, path, name,
            dataType.IsNull ? DataTypeIds.UInt32 : dataType, ValueRanks.Scalar);
        variable.Value = 0u;
        variable.OnWriteValue = OnWriteValueDiscrete;
        var values = new EnumValueType[enumNames.Length];
        for (int index = 0; index < values.Length; index++)
        {
            var text = new LocalizedText(enumNames[index]);
            values[index] = new EnumValueType { Value = index, Description = text, DisplayName = text };
        }
        variable.EnumValues.Value = values.ToArrayOf();
        SetWritable(variable.EnumValues);
        variable.ValueAsText.Value = variable.EnumValues.Value[0].DisplayName;
        parent?.AddChild(variable);
        return variable;
    }

    private static void SetMetadata(
        BaseVariableState variable, ushort namespaceIndex, string path, string name, NodeId dataType, int valueRank)
    {
        variable.SymbolicName = name;
        variable.ReferenceTypeId = ReferenceTypeIds.Organizes;
        variable.BrowseName = new QualifiedName(path, namespaceIndex);
        variable.DataType = dataType;
        variable.ValueRank = valueRank;
        SetWritable(variable);
        variable.Historizing = false;
        variable.StatusCode = StatusCodes.Good;
        variable.Timestamp = DateTime.UtcNow;
        if (valueRank == ValueRanks.OneDimension)
        {
            variable.ArrayDimensions = [0];
        }
        else if (valueRank == ValueRanks.TwoDimensions)
        {
            variable.ArrayDimensions = [0, 0];
        }
    }

    private static void SetWritable(BaseVariableState variable)
    {
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
    }

    private static Range GetAnalogRange(BuiltInType builtInType)
    {
        return builtInType switch
        {
            BuiltInType.UInt16 => new Range(ushort.MaxValue, ushort.MinValue),
            BuiltInType.UInt32 => new Range(uint.MaxValue, uint.MinValue),
            BuiltInType.UInt64 => new Range(ulong.MaxValue, ulong.MinValue),
            BuiltInType.SByte => new Range(sbyte.MaxValue, sbyte.MinValue),
            BuiltInType.Int16 => new Range(short.MaxValue, short.MinValue),
            BuiltInType.Int32 => new Range(int.MaxValue, int.MinValue),
            BuiltInType.Int64 => new Range(long.MaxValue, long.MinValue),
            BuiltInType.Float => new Range(float.MaxValue, float.MinValue),
            BuiltInType.Double => new Range(double.MaxValue, double.MinValue),
            BuiltInType.Byte => new Range(byte.MaxValue, byte.MinValue),
            _ => new Range(sbyte.MaxValue, sbyte.MinValue)
        };
    }

    private static ServiceResult OnWriteDiscrete(
        ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
        ref Variant value, ref StatusCode statusCode, ref DateTimeUtc timestamp)
    {
        var variable = (MultiStateDiscreteState)node;
        TypeInfo typeInfo = TypeInfo.IsInstanceOfDataType(
            value, variable.DataType, variable.ValueRank, context.NamespaceUris, context.TypeTable);
        if (typeInfo == TypeInfo.Unknown)
        {
            return StatusCodes.BadTypeMismatch;
        }
        if (!indexRange.IsNull)
        {
            return StatusCodes.BadIndexRangeInvalid;
        }
        double number = value.ConvertToDouble().GetDouble();
        if (number >= variable.EnumStrings.Value.Count || number < 0)
        {
            return StatusCodes.BadOutOfRange;
        }
        return ServiceResult.Good;
    }

    private static ServiceResult OnWriteValueDiscrete(
        ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
        ref Variant value, ref StatusCode statusCode, ref DateTimeUtc timestamp)
    {
        var variable = node as MultiStateValueDiscreteState;
        TypeInfo typeInfo = value.TypeInfo;
        if (variable == null || typeInfo == TypeInfo.Unknown || !TypeInfo.IsNumericType(typeInfo.BuiltInType))
        {
            return StatusCodes.BadTypeMismatch;
        }
        if (!indexRange.IsNull)
        {
            return StatusCodes.BadIndexRangeInvalid;
        }
        int number = value.ConvertToInt32().GetInt32();
        if (number >= variable.EnumValues.Value.Count || number < 0)
        {
            return StatusCodes.BadOutOfRange;
        }
        if (!node.SetChildValue(context, BrowseNames.ValueAsText, variable.EnumValues.Value[number].DisplayName, true))
        {
            return StatusCodes.BadOutOfRange;
        }
        node.ClearChangeMasks(context, true);
        return ServiceResult.Good;
    }

    private static ServiceResult OnWriteAnalog(
        ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
        ref Variant value, ref StatusCode statusCode, ref DateTimeUtc timestamp)
    {
        var variable = (AnalogItemState)node;
        TypeInfo typeInfo = TypeInfo.IsInstanceOfDataType(
            value, variable.DataType, variable.ValueRank, context.NamespaceUris, context.TypeTable);
        if (typeInfo == TypeInfo.Unknown)
        {
            return StatusCodes.BadTypeMismatch;
        }
        if (variable.ValueRank >= 0)
        {
            if (!indexRange.IsNull)
            {
                Variant target = variable.Value;
                ServiceResult result = indexRange.UpdateRange(ref target, value);
                if (ServiceResult.IsBad(result))
                {
                    return result;
                }
                value = target;
            }
        }
        else
        {
            if (!indexRange.IsNull)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }
            double number = value.ConvertToDouble().GetDouble();
            if (variable.InstrumentRange != null &&
                (number < variable.InstrumentRange.Value.Low || number > variable.InstrumentRange.Value.High))
            {
                return StatusCodes.BadOutOfRange;
            }
        }
        return ServiceResult.Good;
    }

    private static ServiceResult OnWriteAnalogRange(
        ISystemContext context, NodeState node, NumericRange indexRange, QualifiedName dataEncoding,
        ref Variant value, ref StatusCode statusCode, ref DateTimeUtc timestamp)
    {
        var variable = node as PropertyState<Range>;
        if (variable == null || !value.TryGetStructure(out Range newRange))
        {
            return StatusCodes.BadTypeMismatch;
        }
        var parent = variable.Parent as AnalogItemState;
        if (newRange == null || parent == null)
        {
            return StatusCodes.BadTypeMismatch;
        }
        if (!indexRange.IsNull)
        {
            return StatusCodes.BadIndexRangeInvalid;
        }
        Range parentRange = GetAnalogRange(parent.Value.TypeInfo.BuiltInType);
        if (parentRange.High < newRange.High || parentRange.Low > newRange.Low)
        {
            return StatusCodes.BadOutOfRange;
        }
        value = Variant.FromStructure(newRange);
        return ServiceResult.Good;
    }
}