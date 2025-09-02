namespace OpcPlc.AlarmCondition;

using Opc.Ua.Test;

/// <summary>
/// Returns sequential numbers in a round-robin fashion.
/// </summary>
public class RoundRobinSource : IRandomSource
{
    int _seed;

    /// <summary>
    /// Initializes the source with a seed.
    /// </summary>
    public RoundRobinSource(int seed)
    {
        _seed = seed;
    }

    public void NextBytes(byte[] bytes, int offset, int count)
    {
        // Not used.
    }

    public int NextInt32(int max)
    {
        return _seed++ % max;
    }
}
