using UnityEngine;

namespace Basic.ImGui.Layout
{
    public static class TextPresets
    {
        public static TextConfig Heading => new TextConfig
        {
            Font = FontId.Default,
            FontSize = 20f,
            Color = new Color32(255, 255, 255, 255),
            Wrap = false
        };

        public static TextConfig Body => new TextConfig
        {
            Font = FontId.Default,
            FontSize = 14f,
            Color = new Color32(220, 220, 220, 255),
            Wrap = true
        };

        public static TextConfig Stat => new TextConfig
        {
            Font = FontId.Default,
            FontSize = 16f,
            Color = new Color32(180, 255, 180, 255),
            Wrap = false
        };
    }
}
