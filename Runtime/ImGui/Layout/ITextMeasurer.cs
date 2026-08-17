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
        void Measure(TextSlice text, FontId font, float fontSize, ref TextMetrics metrics);
    }
}
