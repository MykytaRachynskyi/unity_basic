using System.Collections;
using Basic.ImGui.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering.Tests
{
    [TestFixture]
    public class ImGuiPlayModeBenchmarkTests
    {
        [UnityTest]
        public IEnumerator Benchmark_8kStress_PassesPerfGatesAfterWarmup()
        {
            using var fixture = StressHostFixture.Create(new Vector2(1920f, 1080f));
            yield return fixture.WarmupFrames(ImGuiPerfGates.WarmupFrames + 5);

            var stats = fixture.Host.LastFrameStats;
            var gates = fixture.Host.PerfGates;

            Assert.That(fixture.Host.GatesReady, Is.True);
            Assert.That(stats.ElementCount, Is.GreaterThanOrEqualTo(ImGuiPerfGates.StressElementCount));
            Assert.That(gates.ElementCapacityPass, Is.True, "Element capacity gate failed.");
            Assert.That(gates.FrameTimePass, Is.True, $"Frame time gate failed ({stats.CombinedMilliseconds:F3} ms).");
            Assert.That(gates.GcAllocPass, Is.True, $"GC gate failed ({stats.GcAllocBytes} B).");
            Assert.That(gates.DrawCallsPass, Is.True, $"Draw call gate failed ({stats.BatchCount} batches).");

            fixture.Dispose();
        }

        sealed class StressHostFixture : System.IDisposable
        {
            public GameObject Root { get; }
            public ImGuiHost Host { get; }

            StressHostFixture(GameObject root, ImGuiHost host)
            {
                Root = root;
                Host = host;
            }

            public static StressHostFixture Create(Vector2 layoutSize)
            {
                var root = new GameObject("ImGuiBenchmarkRoot");
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
                hostGo.AddComponent<StressOnlyHud>();

                var measurer = new FakeTextMeasurer(8f);
                var context = new ImGuiContext(measurer);
                host.ConfigureForTests(
                    graphicGo.GetComponent<ImGuiGraphic>(),
                    canvas,
                    context,
                    measurer,
                    FontRegistry.CreateWithDefaultFont());
                hostGo.SetActive(true);

                return new StressHostFixture(root, host);
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

        sealed class StressOnlyHud : MonoBehaviour
        {
            ImGuiHost _host;

            void Awake() => _host = GetComponent<ImGuiHost>();

            void OnEnable() => _host.DeclareLayout += DeclareStressOnly;

            void OnDisable()
            {
                if (_host != null)
                {
                    _host.DeclareLayout -= DeclareStressOnly;
                }
            }

            static void DeclareStressOnly(ImGuiContext ctx, float deltaTime)
            {
                using (ctx.Element(ElementId.From("StressRoot"), ElementPresets.FullScreen))
                {
                    for (var i = 0; i < ImGuiPerfGates.StressElementCount; i++)
                    {
                        using (ctx.Element(ElementId.Indexed("StressRect", (uint)i), StressRectDeclaration()))
                        {
                        }
                    }
                }
            }

            static ElementDeclaration StressRectDeclaration() => new ElementDeclaration
            {
                Width = LayoutSizing.Fixed(8f),
                Height = LayoutSizing.Fixed(4f),
                BackgroundColor = new Color32(80, 80, 80, 255),
                Direction = LayoutDirection.LeftToRight
            };
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
