using System;

namespace Basic.ImGui.Layout
{
    public struct TextSlice
    {
        public int StartIndex;
        public int Length;

        public TextSlice(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }
    }

    public struct TextMetrics
    {
        public float Width;
        public float Height;
        public int LineCount;
    }

    public interface ITextMeasurer
    {
        void Measure(ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing, ref TextMetrics metrics);

        float MeasureWord(ReadOnlySpan<char> word, FontId font, float fontSize, float letterSpacing);
    }
}
