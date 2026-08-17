using System;
using System.Diagnostics;
using Basic.ImGui.Prototype;
using Basic.UnityEditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Prototype.Editor
{
    /// <summary>
    /// PROTOTYPE — batchmode benchmark for canvas OnPopulateMesh vs direct mesh upload (#12).
    /// </summary>
    public static class CanvasBatchingBenchmark
    {
        const int ElementCount = 8000;
        const int WarmupFrames = 60;
        const int MeasureFrames = 120;
        const double FrameTimeGateMs = 2.0;
        const int DrawCallGate = 16;

        /// <summary>
        /// Unity -batchmode -nographics -quit -projectPath &lt;path&gt;
        ///   -executeMethod Basic.ImGui.Prototype.Editor.CanvasBatchingBenchmark.Run
        /// </summary>
        public static void Run()
        {
            try
            {
                var result = Execute();
                Log.CliInfo($"[ImGui/Prototype] Canvas quad path: {result.CanvasQuadPopulateMs:F3} ms, canvas FillMesh path: {result.CanvasFillMeshMs:F3} ms, direct mesh: {result.DirectMeshMs:F3} ms, batches: {result.BatchCount}, verts: {result.VertexCount}, canvasDraws: {result.CanvasDrawCalls}, gcBytes: {result.GcBytes}");
                Log.CliInfo($"[ImGui/Prototype] Gates — frame≤{FrameTimeGateMs}ms: {(result.PassesFrameGate ? "PASS" : "FAIL")}, draws≤{DrawCallGate}: {(result.PassesDrawGate ? "PASS" : "FAIL")}, gc=0: {(result.PassesGcGate ? "PASS" : "FAIL")}");

                if (!result.PassesFrameGate || !result.PassesDrawGate)
                    CliRunner.ExitFailure("Canvas batching prototype did not meet frame time or draw-call gates.");
                if (!result.PassesGcGate)
                    Log.CliInfo("[ImGui/Prototype] GC gate not met in throwaway (mesh.vertices allocates); production BatchBuilder must use SetVertexBufferData.");
                CliRunner.ExitSuccess("Canvas batching prototype meets v1 frame time and draw-call gates.");
            }
            catch (Exception ex)
            {
                CliRunner.ExitFailure($"Canvas batching benchmark failed: {ex}");
            }
        }

        public static BenchmarkResult Execute()
        {
            var commands = CreateCommands(ElementCount);
            var batchBuilder = new PrototypeBatchBuilder();
            var vertexHelper = new VertexHelper();
            var mesh = new Mesh { name = "PrototypeImGuiMesh" };
            mesh.MarkDynamic();

            WarmupCanvasPopulate(batchBuilder, commands, vertexHelper);
            WarmupCanvasFillMesh(batchBuilder, commands, mesh);
            WarmupDirectMesh(batchBuilder, commands, mesh);

            var canvasQuadMs = MeasureCanvasPopulate(batchBuilder, commands, vertexHelper);
            var canvasFillMeshMs = MeasureCanvasFillMesh(batchBuilder, commands, mesh);
            var meshMs = MeasureDirectMesh(batchBuilder, commands, mesh);

            var gcBefore = GC.GetTotalMemory(true);
            for (var i = 0; i < MeasureFrames; i++)
                batchBuilder.BuildFillMesh(commands, commands.Length, mesh);
            var gcAfter = GC.GetTotalMemory(false);
            var gcDelta = System.Math.Max(0, gcAfter - gcBefore);

            var drawCalls = EstimateCanvasDrawCalls(batchBuilder.BatchCount);

            vertexHelper.Dispose();
            UnityEngine.Object.DestroyImmediate(mesh);

            return new BenchmarkResult
            {
                ElementCount = ElementCount,
                VertexCount = batchBuilder.VertexCount,
                BatchCount = batchBuilder.BatchCount,
                CanvasQuadPopulateMs = canvasQuadMs,
                CanvasFillMeshMs = canvasFillMeshMs,
                DirectMeshMs = meshMs,
                CanvasDrawCalls = drawCalls,
                GcBytes = gcDelta,
                PassesFrameGate = canvasFillMeshMs <= FrameTimeGateMs,
                PassesDrawGate = drawCalls <= DrawCallGate,
                PassesGcGate = gcDelta == 0, // throwaway uses mesh.vertices; production needs SetVertexBufferData
            };
        }

        static PrototypeRenderCommand[] CreateCommands(int count)
        {
            var commands = new PrototypeRenderCommand[count];
            const int columns = 100;
            const float size = 8f;
            const float gap = 2f;

            for (var i = 0; i < count; i++)
            {
                var col = i % columns;
                var row = i / columns;
                commands[i] = new PrototypeRenderCommand
                {
                    X = col * (size + gap),
                    Y = row * (size + gap),
                    Width = size,
                    Height = size,
                    Color = new Color32((byte)(i & 0xFF), 80, 120, 255),
                };
            }

            return commands;
        }

        static void WarmupCanvasPopulate(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, VertexHelper vh)
        {
            for (var i = 0; i < WarmupFrames; i++)
                builder.Build(commands, commands.Length, vh);
        }

        static void WarmupCanvasFillMesh(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, Mesh mesh)
        {
            for (var i = 0; i < WarmupFrames; i++)
                builder.BuildFillMesh(commands, commands.Length, mesh);
        }

        static void WarmupDirectMesh(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, Mesh mesh)
        {
            for (var i = 0; i < WarmupFrames; i++)
                builder.BuildToMesh(commands, commands.Length, mesh);
        }

        static double MeasureCanvasPopulate(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, VertexHelper vh)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < MeasureFrames; i++)
                builder.Build(commands, commands.Length, vh);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds / MeasureFrames;
        }

        static double MeasureCanvasFillMesh(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, Mesh mesh)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < MeasureFrames; i++)
                builder.BuildFillMesh(commands, commands.Length, mesh);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds / MeasureFrames;
        }

        static double MeasureDirectMesh(PrototypeBatchBuilder builder, PrototypeRenderCommand[] commands, Mesh mesh)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < MeasureFrames; i++)
                builder.BuildToMesh(commands, commands.Length, mesh);
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds / MeasureFrames;
        }

        static int EstimateCanvasDrawCalls(int batchCount)
        {
            // One MaskableGraphic with one material batch => one CanvasRenderer draw.
            return batchCount;
        }

        public struct BenchmarkResult
        {
            public int ElementCount;
            public int VertexCount;
            public int BatchCount;
            public double CanvasQuadPopulateMs;
            public double CanvasFillMeshMs;
            public double DirectMeshMs;
            public int CanvasDrawCalls;
            public long GcBytes;
            public bool PassesFrameGate;
            public bool PassesDrawGate;
            public bool PassesGcGate;
        }
    }
}
