// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

public partial class SlowFastNodeSimulation(ISystemContext context, TimeService timeService, ILogger logger)
{
    private readonly ISystemContext _context = context;
    private readonly TimeService _timeService = timeService;
    private readonly ILogger _logger = logger;
    private readonly Random _random = new();
    private uint _badNodesCycle;

    public void UpdateNodes(
        BaseDataVariableState[] nodes,
        BaseDataVariableState[] badNodes,
        NodeType nodeType,
        BaseDataVariableState numberOfUpdates,
        bool updateNodes)
    {
        if (!ShouldUpdateNodes(numberOfUpdates) || !updateNodes)
        {
            return;
        }

        if (nodes != null)
        {
            UpdateNodes(nodes, nodeType, StatusCodes.Good, addBadValue: false);
        }

        if (badNodes != null)
        {
            (StatusCode status, bool addBadValue) = BadStatusSequence[_badNodesCycle++ % BadStatusSequence.Length];
            UpdateNodes(badNodes, nodeType, status, addBadValue);
        }
    }

    private void UpdateNodes(BaseDataVariableState[] nodes, NodeType type, StatusCode status, bool addBadValue)
    {
        if (nodes == null || nodes.Length == 0)
        {
            LogInvalidArgument(nodes?.ToString());
            return;
        }

        for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
        {
            var extendedNode = (BaseDataVariableStateExtended)nodes[nodeIndex];

            Variant value = Variant.Null;
            if (StatusCode.IsNotBad(status) || addBadValue)
            {
                switch (type)
                {
                    case NodeType.Double:
                        double minDoubleValue = (double)extendedNode.MinValue;
                        double maxDoubleValue = (double)extendedNode.MaxValue;
                        double extendedDoubleNodeValue = extendedNode.Value.IsNull
                            ? minDoubleValue
                            : (double)extendedNode.Value;

                        if (extendedNode.Randomize)
                        {
                            if (minDoubleValue != maxDoubleValue)
                            {
                                if (minDoubleValue < 0 && maxDoubleValue > 0)
                                {
                                    while (value.IsNull || extendedDoubleNodeValue == (double)value)
                                    {
                                        double positiveValue = _random.NextDouble() * maxDoubleValue;
                                        double negativeValue = _random.NextDouble() * minDoubleValue;
                                        value = _random.Next(10) % 2 == 0 ? positiveValue : negativeValue;
                                    }
                                }
                                else
                                {
                                    while (value.IsNull || extendedDoubleNodeValue == (double)value)
                                    {
                                        value = minDoubleValue +
                                            (_random.NextDouble() * (maxDoubleValue - minDoubleValue));
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException(
                                    $"Range {minDoubleValue} to {maxDoubleValue}does not have provision for randomness.");
                            }
                        }
                        else
                        {
                            if (minDoubleValue >= 0 && maxDoubleValue > 0)
                            {
                                value = (extendedDoubleNodeValue % maxDoubleValue) < minDoubleValue
                                    ? minDoubleValue
                                    : ((extendedDoubleNodeValue % maxDoubleValue) + (double)extendedNode.StepSize)
                                        > maxDoubleValue
                                        ? minDoubleValue
                                        : ((extendedDoubleNodeValue % maxDoubleValue) + (double)extendedNode.StepSize);
                            }
                            else if (maxDoubleValue <= 0 && minDoubleValue < 0)
                            {
                                value = (extendedDoubleNodeValue % minDoubleValue) > maxDoubleValue
                                    ? maxDoubleValue
                                    : ((extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize)
                                        < minDoubleValue
                                        ? maxDoubleValue
                                        : (extendedDoubleNodeValue % minDoubleValue) - (double)extendedNode.StepSize;
                            }
                            else
                            {
                                throw new ArgumentException(
                                    $"Negative to positive range {minDoubleValue} to {maxDoubleValue} " +
                                    "for sequential node values is not supported currently.");
                            }
                        }
                        break;

                    case NodeType.Bool:
                        value = extendedNode.Value.IsNull || !(bool)extendedNode.Value;
                        break;

                    case NodeType.UIntArray:
                        uint[] arrayValue;
                        if (!extendedNode.Value.IsNull)
                        {
                            arrayValue = extendedNode.Value.GetUInt32Array().ToArray();
                            for (int arrayIndex = 0; arrayIndex < arrayValue.Length; arrayIndex++)
                            {
                                arrayValue[arrayIndex]++;
                            }
                        }
                        else
                        {
                            arrayValue = new uint[32];
                        }
                        value = Variant.From(arrayValue.ToArrayOf());
                        break;

                    case NodeType.UInt:
                    default:
                        uint minUIntValue = (uint)extendedNode.MinValue;
                        uint maxUIntValue = (uint)extendedNode.MaxValue;
                        uint extendedUIntNodeValue = extendedNode.Value.IsNull
                            ? minUIntValue
                            : (uint)extendedNode.Value;

                        if (extendedNode.Randomize)
                        {
                            if (minUIntValue != maxUIntValue)
                            {
                                while (value.IsNull || extendedUIntNodeValue == (uint)value)
                                {
                                    value = (uint)(minUIntValue + (_random.NextDouble() *
                                        ((maxUIntValue == uint.MaxValue ? maxUIntValue : maxUIntValue + 1)
                                            - minUIntValue)));
                                }
                            }
                            else
                            {
                                throw new ArgumentException(
                                    $"Range {minUIntValue} to {maxUIntValue} does not have provision for randomness.");
                            }
                        }
                        else
                        {
                            value = (extendedUIntNodeValue % maxUIntValue) < minUIntValue
                                ? minUIntValue
                                : ((extendedUIntNodeValue % maxUIntValue) + (uint)extendedNode.StepSize) > maxUIntValue
                                    ? minUIntValue
                                    : ((extendedUIntNodeValue % maxUIntValue) + (uint)extendedNode.StepSize);
                        }

                        break;
                }
            }

            extendedNode.StatusCode = status;
            SetValue(extendedNode, value);
        }
    }

    private void SetValue(BaseVariableState variable, Variant value)
    {
        variable.Value = value;
        variable.Timestamp = _timeService.Now();
        variable.ClearChangeMasks(_context, includeChildren: false);
    }

    private bool ShouldUpdateNodes(BaseDataVariableState numberOfUpdatesVariable)
    {
        int value = (int)numberOfUpdatesVariable.Value;
        if (value == 0)
        {
            return false;
        }

        if (value > 0)
        {
            SetValue(numberOfUpdatesVariable, value - 1);
        }

        return true;
    }

    private readonly (StatusCode, bool)[] BadStatusSequence =
    [
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.UncertainLastUsableValue, true),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.Good, true ),
        ( StatusCodes.UncertainLastUsableValue, true),
        ( StatusCodes.BadDataLost, true),
        ( StatusCodes.BadNoCommunication, false)
    ];

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid argument {Argument} provided")]
    partial void LogInvalidArgument(string argument);
}