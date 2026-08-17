using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Basic.ImGui.Layout
{
    public sealed class TextCoreTextMeasurer : ITextMeasurer, IDisposable
    {
        const float DefaultAdvanceFactor = 0.55f;
        const float LineHeightFactor = 1.2f;

        bool _fontLoaded;
        int _loadedPointSize;

        public void Measure(ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing, ref TextMetrics metrics)
        {
            metrics.Width = MeasureSpan(text, fontSize, letterSpacing);
            metrics.Height = fontSize * LineHeightFactor;
            metrics.LineCount = 1;
        }

        public float MeasureWord(ReadOnlySpan<char> word, FontId font, float fontSize, float letterSpacing) =>
            MeasureSpan(word, fontSize, letterSpacing);

        public void Dispose()
        {
        }

        float MeasureSpan(ReadOnlySpan<char> text, float fontSize, float letterSpacing)
        {
            var width = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                width += GetGlyphAdvance(text[i], fontSize) + letterSpacing;
            }

            if (text.Length > 0)
            {
                width -= letterSpacing;
            }

            return math.max(0f, width);
        }

        float GetGlyphAdvance(char character, float fontSize)
        {
            EnsureFontLoaded(fontSize);
            if (FontEngine.TryGetGlyphWithUnicodeValue(character, GlyphLoadFlags.LOAD_RENDER, out var glyph))
            {
                return glyph.metrics.horizontalAdvance;
            }

            return fontSize * DefaultAdvanceFactor;
        }

        void EnsureFontLoaded(float fontSize)
        {
            var pointSize = Mathf.Max(1, Mathf.RoundToInt(fontSize));
            if (_fontLoaded && _loadedPointSize == pointSize)
            {
                return;
            }

            var fontNames = FontEngine.GetSystemFontNames();
            if (fontNames != null && fontNames.Length > 0)
            {
                FontEngine.LoadFontFace(fontNames[0], pointSize);
            }

            _fontLoaded = true;
            _loadedPointSize = pointSize;
        }
    }
}
