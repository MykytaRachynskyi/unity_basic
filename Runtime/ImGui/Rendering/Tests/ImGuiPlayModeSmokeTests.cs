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
        public IEnumerator Smoke_ImGuiHost_RendersHudWithBatchedRects()
        {
            using var fixture = ImGuiHostTestFixture.CreateWithDebugHud(new Vector2(1920f, 1080f));
            yield return fixture.WarmupFrames(5);

            Assert.That(fixture.Host.Graphic.LastVertexCount, Is.GreaterThan(0));
            Assert.That(fixture.Host.LastFrameStats.ElementCount, Is.GreaterThan(50));
        }

        [UnityTest]
        public IEnumerator Smoke_ImGuiHost_ButtonsRespondToPointer()
        {
            using var fixture = ImGuiHostTestFixture.CreateWithDebugHud(new Vector2(1920f, 1080f));
            yield return fixture.WarmupFrames(3);

            var hud = fixture.Hud;
            var initialClicks = hud.ButtonAClicks;
            var clicked = false;

            for (var y = 80f; y <= 400f && !clicked; y += 20f)
            {
                for (var x = 980f; x <= 1200f && !clicked; x += 20f)
                {
                    fixture.Host.SetTestPointer(new Vector2(x, y), isDown: true);
                    yield return null;
                    fixture.Host.SetTestPointer(new Vector2(x, y), isDown: false);
                    yield return null;

                    if (hud.ButtonAClicks > initialClicks)
                    {
                        clicked = true;
                    }
                }
            }

            Assert.That(clicked, Is.True, "Expected Button A to register a click via test pointer sweep.");
        }

        [UnityTest]
        public IEnumerator Smoke_ImGuiHost_ScrollListAcceptsWheelInput()
        {
            using var fixture = ImGuiHostTestFixture.CreateWithDebugHud(new Vector2(1920f, 1080f));
            yield return fixture.WarmupFrames(3);

            fixture.Host.SetTestPointer(new Vector2(1400f, 500f), isDown: false);
            yield return null;

            Assert.That(
                fixture.Host.Context.TryGetScrollOffset(ElementId.From("ScrollList"), out var before),
                Is.True);

            fixture.Host.SetTestPointer(new Vector2(1400f, 500f), isDown: false, scrollDelta: new Vector2(0f, 5f));
            yield return null;
            yield return null;

            Assert.That(
                fixture.Host.Context.TryGetScrollOffset(ElementId.From("ScrollList"), out var after),
                Is.True);
            Assert.That(after.y, Is.GreaterThan(before.y));
        }

        sealed class ImGuiHostTestFixture : System.IDisposable
        {
            public GameObject Root { get; }
            public ImGuiHost Host { get; }
            public ImGuiDebugHud Hud { get; }
            public ImGuiGraphic Graphic { get; }

            ImGuiHostTestFixture(GameObject root, ImGuiHost host, ImGuiDebugHud hud, ImGuiGraphic graphic)
            {
                Root = root;
                Host = host;
                Hud = hud;
                Graphic = graphic;
            }

            public static ImGuiHostTestFixture CreateWithDebugHud(Vector2 layoutSize)
            {
                var root = new GameObject("ImGuiHostTestRoot");
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(root.transform, false);

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

                var graphicGo = new GameObject("ImGuiGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(ImGuiGraphic));
                graphicGo.transform.SetParent(canvasGo.transform, false);
                var graphicRect = graphicGo.GetComponent<RectTransform>();
                graphicRect.anchorMin = Vector2.zero;
                graphicRect.anchorMax = Vector2.one;
                graphicRect.sizeDelta = layoutSize;

                var hostGo = new GameObject("ImGuiHost");
                hostGo.SetActive(false);
                hostGo.transform.SetParent(canvasGo.transform, false);
                var host = hostGo.AddComponent<ImGuiHost>();
                var hud = hostGo.AddComponent<ImGuiDebugHud>();

                var measurer = new FakeTextMeasurer(8f);
                var context = new ImGuiContext(measurer);
                host.ConfigureForTests(graphicGo.GetComponent<ImGuiGraphic>(), canvas, context, measurer, FontRegistry.CreateWithDefaultFont());
                hostGo.SetActive(true);

                return new ImGuiHostTestFixture(root, host, hud, graphicGo.GetComponent<ImGuiGraphic>());
            }

            public IEnumerator WarmupFrames(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                }
            }

            public void Dispose() => Object.DestroyImmediate(Root);
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
