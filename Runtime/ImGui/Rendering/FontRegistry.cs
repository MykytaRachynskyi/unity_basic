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

            if (!TryGetCharacterData(resources.FontAsset, character, out var characterData))
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

        public bool TryGetSolidFillUv(FontId fontId, out Vector2 uvMin, out Vector2 uvMax)
        {
            uvMin = default;
            uvMax = default;
            if (!TryGetGlyph(fontId, '\u2588', 16f, out var glyph) || !glyph.Found)
            {
                if (!TryGetGlyph(fontId, 'M', 16f, out glyph) || !glyph.Found)
                {
                    return false;
                }
            }

            uvMin = new Vector2(glyph.UvRect.x, glyph.UvRect.y);
            uvMax = new Vector2(glyph.UvRect.z, glyph.UvRect.w);
            return true;
        }

        static bool TryGetCharacterData(TMP_FontAsset fontAsset, char character, out TMP_Character characterData)
        {
            if (fontAsset.characterLookupTable.TryGetValue(character, out characterData)
                && characterData.glyph != null)
            {
                return true;
            }

            if (!fontAsset.TryAddCharacters(character.ToString()))
            {
                characterData = null;
                return false;
            }

            return fontAsset.characterLookupTable.TryGetValue(character, out characterData)
                && characterData.glyph != null;
        }

        public static FontRegistry CreateWithDefaultFont()
        {
            var registry = new FontRegistry();
            var defaultFont = TryResolveDefaultFontAsset();
            if (defaultFont != null)
            {
                registry.Register(FontId.Default, defaultFont);
            }

            return registry;
        }

        public static TMP_FontAsset TryResolveDefaultFontAsset()
        {
            var settings = TMP_Settings.LoadDefaultSettings();
            if (settings != null && TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
            {
                return font;
            }

            font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            if (font != null)
            {
                return font;
            }

            return TryCreateRuntimeOsFontAsset();
        }

        static TMP_FontAsset TryCreateRuntimeOsFontAsset()
        {
            (string family, string style)[] candidates =
            {
                ("Arial", "Regular"),
                ("Liberation Sans", "Regular"),
                ("Segoe UI", "Regular"),
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                var font = TMP_FontAsset.CreateFontAsset(candidate.family, candidate.style, 90);
                if (font != null)
                {
                    return font;
                }
            }

            return null;
        }
    }
}
