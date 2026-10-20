namespace OpcPlc.AlarmCondition;

using Opc.Ua.Test;
using System;

/// <summary>
/// Returns sequential numbers in a round-robin fashion.
/// </summary>
public class RoundRobinSource : ISecureRandomSource
{
    int _seed;
    private readonly Random _byteRandom;

    /// <summary>
    /// Initializes the source with a seed.
    /// </summary>
    public RoundRobinSource(int seed)
    {
        _seed = seed;
        _byteRandom = new Random(seed);
    }

    public void NextBytes(byte[] bytes, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        _byteRandom.NextBytes(bytes.AsSpan(offset, count));
    }

    public int NextInt32(int max)
    {
        return _seed++ % max;
    }
}
