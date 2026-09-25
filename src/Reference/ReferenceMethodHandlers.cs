namespace OpcPlc.Reference;

using Opc.Ua;
using System.Collections.Generic;

public static class ReferenceMethodHandlers
{
    public static PropertyState<ArrayOf<Argument>> CreateArguments(
        MethodState method, bool input, params (string Name, NodeId DataType)[] arguments)
    {
        string browseName = input ? BrowseNames.InputArguments : BrowseNames.OutputArguments;
        var property = PropertyState<ArrayOf<Argument>>.With<StructureBuilder<Argument>>(method);
        property.NodeId = new NodeId(method.BrowseName.Name + (input ? "InArgs" : "OutArgs"),
            method.BrowseName.NamespaceIndex);
        property.BrowseName = new QualifiedName(browseName);
        property.DisplayName = new LocalizedText(browseName);
        property.TypeDefinitionId = VariableTypeIds.PropertyType;
        property.ReferenceTypeId = ReferenceTypeIds.HasProperty;
        property.DataType = DataTypeIds.Argument;
        property.ValueRank = ValueRanks.OneDimension;
        property.Value = arguments.ToArrayOf(argument => new Argument
        {
            Name = argument.Name,
            Description = new LocalizedText(argument.Name),
            DataType = argument.DataType,
            ValueRank = ValueRanks.Scalar
        });
        return property;
    }

    public static ServiceResult OnVoidCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        return ServiceResult.Good;
    }

    public static ServiceResult OnAddCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return StatusCodes.BadArgumentsMissing;
        }
        if (!inputArguments[0].TryGetValue(out float first) || !inputArguments[1].TryGetValue(out uint second))
        {
            return StatusCodes.BadInvalidArgument;
        }
        try
        {
            outputArguments[0] = first + second;
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }

    public static ServiceResult OnMultiplyCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return StatusCodes.BadArgumentsMissing;
        }
        if (!inputArguments[0].TryGetValue(out short first) || !inputArguments[1].TryGetValue(out ushort second))
        {
            return StatusCodes.BadInvalidArgument;
        }
        try
        {
            outputArguments[0] = first * second;
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }

    public static ServiceResult OnDivideCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return StatusCodes.BadArgumentsMissing;
        }
        if (!inputArguments[0].TryGetValue(out int first) || !inputArguments[1].TryGetValue(out ushort second))
        {
            return StatusCodes.BadInvalidArgument;
        }
        try
        {
            outputArguments[0] = (float)first / second;
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }

    public static ServiceResult OnSubtractCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 2)
        {
            return StatusCodes.BadArgumentsMissing;
        }
        if (!inputArguments[0].TryGetValue(out short first) || !inputArguments[1].TryGetValue(out byte second))
        {
            return StatusCodes.BadInvalidArgument;
        }
        try
        {
            outputArguments[0] = (short)(first - second);
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }

    public static ServiceResult OnHelloCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        if (inputArguments.Count < 1)
        {
            return StatusCodes.BadArgumentsMissing;
        }
        string value = null;
        if (!inputArguments[0].IsNull && !inputArguments[0].TryGetValue(out value))
        {
            return StatusCodes.BadInvalidArgument;
        }
        try
        {
            outputArguments[0] = "hello " + value;
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }

    public static ServiceResult OnInputCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        return inputArguments.Count < 1 ? StatusCodes.BadArgumentsMissing : ServiceResult.Good;
    }

    public static ServiceResult OnOutputCall(
        ISystemContext context, MethodState method, ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        try
        {
            outputArguments[0] = "Output";
            return ServiceResult.Good;
        }
        catch
        {
            return StatusCodes.BadInvalidArgument;
        }
    }
}