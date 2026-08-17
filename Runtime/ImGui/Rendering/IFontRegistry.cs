using Basic.ImGui.Layout;
using TMPro;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public struct FontGlyph
    {
        public float Advance;
        public float BearingX;
        public float BearingY;
        public float Width;
        public float Height;
        public Vector4 UvRect;
        public bool Found;
    }

    public struct FontResources
    {
        public Material Material;
        public Texture Atlas;
        public TMP_FontAsset FontAsset;
        public float PointSize;
    }

    public interface IFontRegistry
    {
        bool TryGetFont(FontId fontId, out FontResources resources);

        bool TryGetGlyph(FontId fontId, char character, float fontSize, out FontGlyph glyph);
    }
}
