using UnityEngine;

namespace Basic.ImGui.Layout
{
    public struct TextConfig
    {
        public FontId Font;
        public float FontSize;
        public Color32 Color;
        public bool Wrap;
        public float LetterSpacing;

        public static TextConfig Default => new TextConfig
        {
            Font = FontId.Default,
            FontSize = 16f,
            Color = new Color32(255, 255, 255, 255),
            Wrap = false,
            LetterSpacing = 0f
        };
    }
}
