namespace Basic.ImGui.Layout
{
    public readonly struct FontId
    {
        public readonly uint Value;

        public FontId(uint value) => Value = value;

        public static FontId Default => new FontId(0);
    }
}
