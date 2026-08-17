using System;
using Unity.Collections;

namespace Basic.ImGui.Layout
{
    public struct FrameStringBuffer
    {
        public NativeArray<char> Chars;
        public int Length;

        public bool IsCreated => Chars.IsCreated;

        public ReadOnlySpan<char> GetSpan(TextSlice slice)
        {
            if (!Chars.IsCreated || slice.Length <= 0)
            {
                return ReadOnlySpan<char>.Empty;
            }

            return Chars.AsReadOnlySpan().Slice(slice.StartIndex, slice.Length);
        }
    }
}
