using System;

namespace OpcPlc.Helpers
{
    public static class DeterministicGuid
    {
        private static Random _rnd = new Random(1234); // Seeded (deterministic) random number generator.

        public static Guid NewGuid()
        {
            return new Guid($"{GetRandHex(4)}-{GetRandHex(2)}-{GetRandHex(2)}-{GetRandHex(2)}-{GetRandHex(6)}");
        }

        private static object GetRandHex(int length)
        {
            string hexString = string.Empty;

            for (int i = 0; i < length; i++)
            {
                hexString += $"{_rnd.Next(0, 255):x2}";
            }

            return hexString;
        }
    }
}
