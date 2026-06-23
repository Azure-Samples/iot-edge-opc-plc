// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Opc.Ua;

/// <summary>
/// Generates simulated initial values for materialized WoT-Con property variables. Today
/// returns a fixed seed per type; the per-tick simulation engine in plan item 7 will
/// replace this with sine / ramp / random-walk generators.
/// </summary>
internal static class WotMockValueGenerator
{
    /// <summary>
    /// Returns a seed value appropriate for the given OPC UA built-in data type. Unknown
    /// types fall back to an empty string.
    /// </summary>
    public static object Generate(NodeId dataTypeId)
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

        return string.Empty;
    }
}
