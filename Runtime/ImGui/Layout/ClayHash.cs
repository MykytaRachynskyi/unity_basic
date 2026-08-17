using System;

namespace Basic.ImGui.Layout
{
    internal static class ClayHash
    {
        public static uint HashString(ReadOnlySpan<char> key, uint seed)
        {
            var hash = seed;

            for (var i = 0; i < key.Length; i++)
            {
                hash += key[i];
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;

            return hash + 1;
        }

        public static uint HashStringWithOffset(ReadOnlySpan<char> key, uint offset, uint seed, out uint baseId)
        {
            var baseHash = seed;

            for (var i = 0; i < key.Length; i++)
            {
                baseHash += key[i];
                baseHash += baseHash << 10;
                baseHash ^= baseHash >> 6;
            }

            var hash = baseHash;
            hash += offset;
            hash += hash << 10;
            hash ^= hash >> 6;

            hash += hash << 3;
            baseHash += baseHash << 3;
            hash ^= hash >> 11;
            baseHash ^= baseHash >> 11;
            hash += hash << 15;
            baseHash += baseHash << 15;

            baseId = baseHash + 1;
            return hash + 1;
        }
    }
}
