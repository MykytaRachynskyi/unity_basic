using System;
using Basic.ImGui.Layout;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public sealed class FontRegistryTextMeasurer : ITextMeasurer
    {
        const float DefaultAdvanceFactor = 0.55f;
        const float LineHeightFactor = 1.2f;

        readonly IFontRegistry _fonts;

        public FontRegistryTextMeasurer(IFontRegistry fonts)
        {
            _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        }

        public void Measure(ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing, ref TextMetrics metrics)
        {
            metrics.Width = MeasureSpan(text, font, fontSize, letterSpacing);
            metrics.Height = fontSize * LineHeightFactor;
            metrics.LineCount = 1;
        }

        public float MeasureWord(ReadOnlySpan<char> word, FontId font, float fontSize, float letterSpacing) =>
            MeasureSpan(word, font, fontSize, letterSpacing);

        float MeasureSpan(ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing)
        {
            var width = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                width += GetGlyphAdvance(text[i], font, fontSize) + letterSpacing;
            }

            if (text.Length > 0)
            {
                width -= letterSpacing;
            }

            return Mathf.Max(0f, width);
        }

        float GetGlyphAdvance(char character, FontId font, float fontSize)
        {
            if (_fonts.TryGetGlyph(font, character, fontSize, out var glyph) && glyph.Found)
            {
                return glyph.Advance;
            }

            return fontSize * DefaultAdvanceFactor;
        }
    }
}
