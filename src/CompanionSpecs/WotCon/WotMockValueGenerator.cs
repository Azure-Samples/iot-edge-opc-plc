// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Opc.Ua;
using System;

/// <summary>
/// Generates simulated initial values for materialized WoT-Con property variables. Today
/// returns a fixed seed per type; the per-tick simulation engine in a later plan item will
/// replace this with sine / ramp / random-walk generators.
/// </summary>
internal static class WotMockValueGenerator
{
    /// <summary>
    /// Returns a seed value appropriate for the given OPC UA built-in data type and value rank.
    /// Scalar ranks return a single typed value; <see cref="ValueRanks.OneDimension"/> returns
    /// a small typed array. Unknown types fall back to an empty string / empty <c>string[]</c>.
    /// </summary>
    public static object Generate(NodeId dataTypeId, int valueRank)
    {
        if (valueRank == ValueRanks.OneDimension)
        {
            return GenerateArray(dataTypeId);
        }

        return GenerateScalar(dataTypeId);
    }

    /// <summary>
    /// Convenience overload kept for callers that don't track <see cref="ValueRanks"/>;
    /// always returns a scalar seed.
    /// </summary>
    public static object Generate(NodeId dataTypeId) => GenerateScalar(dataTypeId);

    private static object GenerateScalar(NodeId dataTypeId)
    {
        if (dataTypeId == DataTypeIds.Double)
        {
            return 42.0;
        }

        if (dataTypeId == DataTypeIds.Int32)
        {
            return 100;
        }

        if (dataTypeId == DataTypeIds.Boolean)
        {
            return true;
        }

        if (dataTypeId == DataTypeIds.DateTime)
        {
            return DateTime.UtcNow;
        }

        return string.Empty;
    }

    private static Array GenerateArray(NodeId elementDataTypeId)
    {
        if (elementDataTypeId == DataTypeIds.Double)
        {
            return new[] { 1.0, 2.0, 3.0 };
        }

        if (elementDataTypeId == DataTypeIds.Int32)
        {
            return new[] { 1, 2, 3 };
        }

        if (elementDataTypeId == DataTypeIds.Boolean)
        {
            return new[] { true, false };
        }

        if (elementDataTypeId == DataTypeIds.DateTime)
        {
            return new[] { DateTime.UtcNow };
        }

        return Array.Empty<string>();
    }
}
