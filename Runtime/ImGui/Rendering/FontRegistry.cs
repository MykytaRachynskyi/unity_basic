using System.Collections.Generic;
using Basic.ImGui.Layout;
using TMPro;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public sealed class FontRegistry : IFontRegistry
    {
        readonly Dictionary<uint, FontResources> _fonts = new Dictionary<uint, FontResources>();

        public void Register(FontId fontId, TMP_FontAsset fontAsset, Material material = null)
        {
            if (fontAsset == null)
            {
                return;
            }

            _fonts[fontId.Value] = new FontResources
            {
                FontAsset = fontAsset,
                Material = material != null ? material : fontAsset.material,
                Atlas = fontAsset.atlasTexture,
                PointSize = fontAsset.faceInfo.pointSize,
            };
        }

        public bool TryGetFont(FontId fontId, out FontResources resources) =>
            _fonts.TryGetValue(fontId.Value, out resources);

        public bool TryGetGlyph(FontId fontId, char character, float fontSize, out FontGlyph glyph)
        {
            glyph = default;
            if (!_fonts.TryGetValue(fontId.Value, out var resources) || resources.FontAsset == null)
            {
                return false;
            }

            if (!resources.FontAsset.characterLookupTable.TryGetValue(character, out var characterData)
                || characterData.glyph == null)
            {
                return false;
            }

            var sourceGlyph = characterData.glyph;
            var face = resources.FontAsset.faceInfo;
            var scale = fontSize / Mathf.Max(1f, face.pointSize);
            var glyphRect = sourceGlyph.glyphRect;

            glyph.Found = true;
            glyph.Advance = sourceGlyph.metrics.horizontalAdvance * scale;
            glyph.BearingX = sourceGlyph.metrics.horizontalBearingX * scale;
            glyph.BearingY = sourceGlyph.metrics.horizontalBearingY * scale;
            glyph.Width = glyphRect.width * scale;
            glyph.Height = glyphRect.height * scale;

            var atlasWidth = Mathf.Max(1f, resources.FontAsset.atlasWidth);
            var atlasHeight = Mathf.Max(1f, resources.FontAsset.atlasHeight);
            glyph.UvRect = new Vector4(
                glyphRect.x / atlasWidth,
                glyphRect.y / atlasHeight,
                (glyphRect.x + glyphRect.width) / atlasWidth,
                (glyphRect.y + glyphRect.height) / atlasHeight);

            return true;
        }

        public static FontRegistry CreateWithDefaultFont()
        {
            var registry = new FontRegistry();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                registry.Register(FontId.Default, defaultFont);
            }

            return registry;
        }
    }
}
