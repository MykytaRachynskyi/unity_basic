using System.Collections;
using Basic.ImGui.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering.Tests
{
    [TestFixture]
    public class ImGuiPlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator Smoke_ImGuiGraphic_RendersBatchedPanelRects()
        {
            var canvasGo = new GameObject("ImGuiCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var graphicGo = new GameObject("ImGuiGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(ImGuiGraphic));
            graphicGo.transform.SetParent(canvasGo.transform, false);

            var graphic = graphicGo.GetComponent<ImGuiGraphic>();
            graphic.rectTransform.anchorMin = Vector2.zero;
            graphic.rectTransform.anchorMax = Vector2.one;
            graphic.rectTransform.offsetMin = Vector2.zero;
            graphic.rectTransform.offsetMax = Vector2.zero;

            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            var backend = new CanvasRenderBackend(graphic);
            graphic.Configure(context);

            context.SetLayoutDimensions(new Vector2(320f, 240f));
            context.SetPointerState(Vector2.zero, false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Panel"), ElementPresets.Panel))
            {
                for (var i = 0; i < 8; i++)
                {
                    using (context.Element(ElementId.Indexed("Row", (uint)i), ElementPresets.Panel))
                    {
                    }
                }
            }

            var frame = context.EndLayout(0.016f);
            backend.Draw(frame, new RenderBackendContext(Matrix4x4.identity, flipY: false));

            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.That(graphic.LastVertexCount, Is.GreaterThan(0));
            Assert.That(graphic.LastBatchCount, Is.EqualTo(1));

            Object.DestroyImmediate(canvasGo);
        }

        sealed class FakeTextMeasurer : ITextMeasurer
        {
            readonly float _charWidth;

            public FakeTextMeasurer(float charWidth) => _charWidth = charWidth;

            public void Measure(System.ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing, ref TextMetrics metrics)
            {
                metrics.Width = text.Length * _charWidth;
                metrics.Height = fontSize;
                metrics.LineCount = 1;
            }

            public float MeasureWord(System.ReadOnlySpan<char> word, FontId font, float fontSize, float letterSpacing) =>
                word.Length * _charWidth;
        }
    }
}
