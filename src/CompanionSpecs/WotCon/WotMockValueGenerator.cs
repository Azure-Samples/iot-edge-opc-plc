// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Opc.Ua;
using System;

/// <summary>
/// Generates simulated values for materialized WoT-Con property variables: a seed at
/// materialization time (<see cref="Generate(NodeId, int)"/>) and a per-tick successor
/// (<see cref="Advance(NodeId, int, long, DateTime)"/>) that drives the value drift OPC UA
/// subscriptions observe.
/// </summary>
/// <remarks>
/// Both are pure functions of the requested type and the tick counter, so a given tick always
/// yields the same value. Tick 0 reproduces the seed for every type except String, keeping the
/// materialized value and the first simulated value continuous.
/// </remarks>
internal static class WotMockValueGenerator
{
    /// <summary>
    /// Sine mid-point for Double properties; also the Double seed.
    /// </summary>
    private const double DoubleBase = 42.0;

    /// <summary>
    /// Peak deviation of the Double sine from <see cref="DoubleBase"/>.
    /// </summary>
    private const double DoubleAmplitude = 10.0;

    /// <summary>
    /// Radians advanced per tick by the Double sine; 0.1 rad gives a ~63-tick period.
    /// </summary>
    private const double DoubleAngularStep = 0.1;

    /// <summary>
    /// Ramp start for Int32 properties; also the Int32 seed.
    /// </summary>
    private const int Int32Base = 100;

    /// <summary>
    /// Number of ticks before the Int32 ramp wraps back to <see cref="Int32Base"/>.
    /// </summary>
    private const int Int32RampPeriod = 100;

    /// <summary>
    /// Values a String property rotates through, one per tick.
    /// </summary>
    private static readonly string[] StringRotation = ["idle", "running", "paused", "fault"];

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

    /// <summary>
    /// Returns the value a property of the given type holds at <paramref name="tick"/>:
    /// Double follows a sine, Int32 a wrapping ramp, Boolean toggles, String rotates through a
    /// fixed list, and DateTime tracks <paramref name="utcNow"/>. Array ranks apply the same
    /// progression per element, offset by the element index so neighbours differ.
    /// </summary>
    /// <param name="dataTypeId">The variable's OPC UA built-in data type.</param>
    /// <param name="valueRank">The variable's value rank.</param>
    /// <param name="tick">Monotonic simulation tick; 0 reproduces the seed.</param>
    /// <param name="utcNow">Simulation clock reading used for DateTime properties.</param>
    public static object Advance(NodeId dataTypeId, int valueRank, long tick, DateTime utcNow)
    {
        if (valueRank == ValueRanks.OneDimension)
        {
            return AdvanceArray(dataTypeId, tick, utcNow);
        }

        return AdvanceScalar(dataTypeId, tick, utcNow);
    }

    private static object AdvanceScalar(NodeId dataTypeId, long tick, DateTime utcNow)
    {
        if (dataTypeId == DataTypeIds.Double)
        {
            return NextDouble(tick);
        }

        if (dataTypeId == DataTypeIds.Int32)
        {
            return NextInt32(tick);
        }

        if (dataTypeId == DataTypeIds.Boolean)
        {
            return NextBoolean(tick);
        }

        if (dataTypeId == DataTypeIds.DateTime)
        {
            return utcNow;
        }

        return StringRotation[(int)Modulo(tick, StringRotation.Length)];
    }

    private static Array AdvanceArray(NodeId elementDataTypeId, long tick, DateTime utcNow)
    {
        if (elementDataTypeId == DataTypeIds.Double)
        {
            return new[] { NextDouble(tick), NextDouble(tick + 1), NextDouble(tick + 2) };
        }

        if (elementDataTypeId == DataTypeIds.Int32)
        {
            return new[] { NextInt32(tick), NextInt32(tick + 1), NextInt32(tick + 2) };
        }

        if (elementDataTypeId == DataTypeIds.Boolean)
        {
            return new[] { NextBoolean(tick), NextBoolean(tick + 1) };
        }

        if (elementDataTypeId == DataTypeIds.DateTime)
        {
            return new[] { utcNow };
        }

        return new[] { StringRotation[(int)Modulo(tick, StringRotation.Length)] };
    }

    // Rounded so the value is stable across the float formatting clients apply.
    private static double NextDouble(long tick)
        => Math.Round(DoubleBase + (DoubleAmplitude * Math.Sin(tick * DoubleAngularStep)), 3);

    private static int NextInt32(long tick) => Int32Base + (int)Modulo(tick, Int32RampPeriod);

    private static bool NextBoolean(long tick) => Modulo(tick, 2) == 0;

    // A tick is never negative today, but % would yield a negative index if that ever changed.
    private static long Modulo(long value, long modulus)
    {
        long remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }

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
