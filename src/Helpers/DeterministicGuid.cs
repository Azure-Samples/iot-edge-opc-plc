namespace OpcPlc.Helpers;

using System;

public static class DeterministicGuid
{
    private static Random _rnd = new Random(1234); // Seeded (deterministic) random number generator.

    public static Guid NewGuid()
    {
        // https://en.wikipedia.org/wiki/Universally_unique_identifier#Format
        // xxxxxxxx-xxxx-4xxx-[8|9|a|b]xxx-xxxxxxxxxxxx
        return new Guid($"{GetRandHexExp(0, 16, 8)}-{GetRandHexExp(0, 16, 4)}-{GetRandHex(16_384, 20_479, 4)}-{GetRandHex(32_768, 49_151, 4)}-{GetRandHexExp(0, 16, 12)}");
    }

    private static string GetRandHexExp(long minIncl, int maxExclBase, int maxExclExponent)
    {
        return GetRandHex(minIncl, (long)Math.Pow(maxExclBase, maxExclExponent), maxExclExponent);
    }

    private static string GetRandHex(long minIncl, long maxExcl, int digits)
    {
        return string.Format($"{{0:x{digits}}}", _rnd.NextInt64(minIncl, maxExcl));
    }
}
