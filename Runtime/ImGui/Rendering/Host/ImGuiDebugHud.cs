using System;
using System.Collections.Generic;
using Basic.ImGui.Layout;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/ImGui Debug HUD")]
    public sealed class ImGuiDebugHud : MonoBehaviour
    {
        public const int StressRectCount = ImGuiPerfGates.StressElementCount;

        static readonly Color32 GatePass = new(80, 200, 80, 255);
        static readonly Color32 GateFail = new(220, 80, 80, 255);
        static readonly Color32 ButtonNormal = new(70, 70, 70, 255);
        static readonly Color32 ButtonHover = new(100, 100, 140, 255);
        static readonly Color32 ButtonPressed = new(60, 90, 140, 255);

        static readonly ElementId StatsPanelId = ElementId.From("StatsPanel");
        static readonly ElementId InteractivePanelId = ElementId.From("InteractivePanel");
        static readonly ElementId BenchmarkPanelId = ElementId.From("BenchmarkPanel");
        static readonly ElementId CompositingPanelId = ElementId.From("CompositingPanel");
        static readonly ElementId ButtonAId = ElementId.From("ButtonA");
        static readonly ElementId ButtonBId = ElementId.From("ButtonB");
        static readonly ElementId ScrollListId = ElementId.From("ScrollList");

        [SerializeField] ImGuiHost _host;
        [SerializeField] int _logLineCount = 40;
        [SerializeField] bool _enableStressBenchmark;

        readonly List<string> _logLines = new();
        int _buttonAClicks;
        int _buttonBClicks;
        string _lastClicked = "none";

        public int ButtonAClicks => _buttonAClicks;
        public int ButtonBClicks => _buttonBClicks;
        public string LastClicked => _lastClicked;

        void Awake()
        {
            if (_host == null)
            {
                _host = GetComponent<ImGuiHost>();
            }

            SeedLogLines();
        }

        void OnEnable()
        {
            if (_host != null)
            {
                _host.DeclareLayout += DeclareHud;
            }
        }

        void OnDisable()
        {
            if (_host != null)
            {
                _host.DeclareLayout -= DeclareHud;
            }
        }

        void SeedLogLines()
        {
            _logLines.Clear();
            for (var i = 0; i < _logLineCount; i++)
            {
                _logLines.Add($"Log line {i + 1}: scroll + pointer input demo");
            }
        }

        void DeclareHud(ImGuiContext ctx, float deltaTime)
        {
            TrackButtonClicks(ctx);

            using (ctx.Element(ElementId.From("HudRoot"), ElementPresets.FullScreen))
            {
                using (ctx.Element(ElementId.From("HudColumns"), ElementPresets.Row))
                {
                    using (ctx.Element(ElementId.From("LeftColumn"), ColumnPanel()))
                    {
                        DeclareStatsPanel(ctx);
                        DeclareBenchmarkPanel(ctx);
                    }

                    using (ctx.Element(ElementId.From("RightColumn"), ColumnPanel()))
                    {
                        DeclareInteractivePanel(ctx);
                        DeclareCompositingPanel(ctx);
                    }
                }
            }
        }

        static ElementDeclaration ColumnPanel() => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Grow(),
            Direction = LayoutDirection.TopToBottom,
            ChildGap = 8f,
            PaddingLeft = 8f,
            PaddingTop = 8f,
            PaddingRight = 8f,
            PaddingBottom = 8f
        };

        void DeclareStatsPanel(ImGuiContext ctx)
        {
            var host = _host;
            var stats = host.LastFrameStats;
            var gatesReady = host.GatesReady ? "yes" : $"warmup {host.FrameIndex}/{ImGuiPerfGates.WarmupFrames}";

            using (ctx.Element(StatsPanelId, SectionPanel()))
            {
                ctx.Text(ElementId.From("StatsTitle"), "Live Stats", TextPresets.Heading);
                ctx.Text(ElementId.From("StatsElements"), $"Elements: {stats.ElementCount}", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsLayout"), $"Layout: {stats.LayoutMilliseconds:F3} ms", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsRender"), $"Render: {stats.RenderMilliseconds:F3} ms", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsCombined"), $"Combined: {stats.CombinedMilliseconds:F3} ms", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsBatches"), $"Batches: {stats.BatchCount}", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsVertices"), $"Vertices: {stats.VertexCount}", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsGc"), $"GC alloc: {stats.GcAllocBytes} B", TextPresets.Stat);
                ctx.Text(ElementId.From("StatsWarmup"), $"Gates ready: {gatesReady}", TextPresets.Body);
            }
        }

        void DeclareInteractivePanel(ImGuiContext ctx)
        {
            using (ctx.Element(InteractivePanelId, SectionPanel()))
            {
                ctx.Text(ElementId.From("InteractiveTitle"), "Interactive", TextPresets.Heading);
                ctx.Text(ElementId.From("ClickSummary"), $"Last click: {_lastClicked} (A:{_buttonAClicks} B:{_buttonBClicks})", TextPresets.Body);

                DeclareButton(ctx, ButtonAId, "Button A");
                DeclareButton(ctx, ButtonBId, "Button B");

                using (ctx.Element(ScrollListId, ElementPresets.ScrollVertical))
                {
                    for (var i = 0; i < _logLines.Count; i++)
                    {
                        using (ctx.Element(ElementId.Indexed("LogLine", (uint)i), ElementPresets.Row))
                        {
                            ctx.Text(ImGuiContext.Local("Text"), _logLines[i], TextPresets.Body);
                        }
                    }
                }
            }
        }

        void DeclareBenchmarkPanel(ImGuiContext ctx)
        {
            var gates = _host.PerfGates;
            var ready = _host.GatesReady;

            using (ctx.Element(BenchmarkPanelId, SectionPanel()))
            {
                ctx.Text(ElementId.From("BenchmarkTitle"), "Stress Benchmark", TextPresets.Heading);
                ctx.Text(ElementId.From("BenchmarkCount"), $"{StressRectCount} rects", TextPresets.Body);
                DeclareGateRow(ctx, 0, "Element capacity (8k)", ready && gates.ElementCapacityPass);
                DeclareGateRow(ctx, 1, "Frame time (<=2 ms)", ready && gates.FrameTimePass);
                DeclareGateRow(ctx, 2, "GC (0 B/frame)", ready && gates.GcAllocPass);
                DeclareGateRow(ctx, 3, "Draw calls (<=16)", ready && gates.DrawCallsPass);

                if (_host.GatesReady && _enableStressBenchmark)
                {
                    DeclareStressRects(ctx, StressRectCount);
                }
            }
        }

        void DeclareCompositingPanel(ImGuiContext ctx)
        {
            using (ctx.Element(CompositingPanelId, SectionPanel()))
            {
                ctx.Text(ElementId.From("CompositingTitle"), "uGUI Compositing", TextPresets.Heading);
                ctx.Text(
                    ElementId.From("CompositingHint"),
                    "Back panel (blue) < ImGui HUD < front strip (gold). See Tier 4 checklist.",
                    TextPresets.Body);
            }
        }

        static ElementDeclaration SectionPanel()
        {
            var panel = ElementPresets.Panel;
            panel.Height = LayoutSizing.Grow();
            return panel;
        }

        void DeclareGateRow(ImGuiContext ctx, uint index, string label, bool pass)
        {
            using (ctx.Element(ElementId.Indexed("GateRow", index), ElementPresets.Row))
            {
                var indicator = pass ? "PASS" : readyLabel(pass);
                var color = pass ? GatePass : GateFail;
                using (ctx.Element(ElementId.Indexed("GateIndicator", index), GateIndicatorDeclaration(color)))
                {
                    ctx.Text(ElementId.Indexed("GateText", index), indicator, TextPresets.Stat);
                }

                ctx.Text(ElementId.Indexed("GateLabel", index), label, TextPresets.Body);
            }
        }

        static string readyLabel(bool pass) => pass ? "PASS" : "FAIL";

        static ElementDeclaration GateIndicatorDeclaration(Color32 color) => new ElementDeclaration
        {
            Width = LayoutSizing.Fixed(56f),
            Height = LayoutSizing.Fixed(22f),
            BackgroundColor = color,
            CornerRadius = new Vector4(3f, 3f, 3f, 3f),
            Direction = LayoutDirection.LeftToRight,
            PaddingLeft = 4f,
            PaddingTop = 2f,
            PaddingRight = 4f,
            PaddingBottom = 2f
        };

        void DeclareButton(ImGuiContext ctx, ElementId id, string label)
        {
            var hovered = ctx.TryGetHoveredId(out var hoveredId) && hoveredId == id;
            var pressed = ctx.IsPressed(id);
            var color = pressed ? ButtonPressed : hovered ? ButtonHover : ButtonNormal;

            using (ctx.Element(id, ButtonDeclaration(color)))
            {
                ctx.Text(ImGuiContext.Local("Label"), label, TextPresets.Body);
            }
        }

        static ElementDeclaration ButtonDeclaration(Color32 color) => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Fixed(32f),
            BackgroundColor = color,
            CornerRadius = new Vector4(4f, 4f, 4f, 4f),
            Direction = LayoutDirection.LeftToRight,
            PaddingLeft = 8f,
            PaddingTop = 6f,
            PaddingRight = 8f,
            PaddingBottom = 6f
        };

        static void DeclareStressRects(ImGuiContext ctx, int count)
        {
            using (ctx.Element(ElementId.From("StressRoot"), InvisibleGrowPanel()))
            {
                for (var i = 0; i < count; i++)
                {
                    using (ctx.Element(ElementId.Indexed("StressRect", (uint)i), StressRectDeclaration()))
                    {
                    }
                }
            }
        }

        static ElementDeclaration InvisibleGrowPanel() => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Fixed(1f),
            Direction = LayoutDirection.TopToBottom,
            ChildGap = 0f,
            ClipChildren = true,
            ClipVertical = true
        };

        static ElementDeclaration StressRectDeclaration() => new ElementDeclaration
        {
            Width = LayoutSizing.Fixed(4f),
            Height = LayoutSizing.Fixed(1f),
            BackgroundColor = new Color32(50, 50, 50, 255),
            Direction = LayoutDirection.LeftToRight
        };

        void TrackButtonClicks(ImGuiContext ctx)
        {
            if (ctx.WasReleasedThisFrame(ButtonAId))
            {
                _buttonAClicks++;
                _lastClicked = "Button A";
            }

            if (ctx.WasReleasedThisFrame(ButtonBId))
            {
                _buttonBClicks++;
                _lastClicked = "Button B";
            }
        }
    }
}
