namespace Basic.ImGui.Rendering
{
    public static class ImGuiPerfGates
    {
        public const int WarmupFrames = 60;
        public const int StressElementCount = 8000;
        public const int MaxElementCapacity = 8192;
        public const double MaxCombinedFrameMilliseconds = 2.0;
        public const long MaxGcAllocBytesPerFrame = 0;
        public const int MaxDrawBatches = 16;

        public static ImGuiPerfGateResults Evaluate(ImGuiFrameStats stats, bool stressRanWithoutOverflow)
        {
            return new ImGuiPerfGateResults(
                elementCapacity: stressRanWithoutOverflow && stats.ElementCount >= StressElementCount,
                frameTime: stats.CombinedMilliseconds <= MaxCombinedFrameMilliseconds,
                gcAlloc: stats.GcAllocBytes <= MaxGcAllocBytesPerFrame,
                drawCalls: stats.BatchCount <= MaxDrawBatches);
        }
    }

    public readonly struct ImGuiPerfGateResults
    {
        public bool ElementCapacityPass { get; }
        public bool FrameTimePass { get; }
        public bool GcAllocPass { get; }
        public bool DrawCallsPass { get; }

        public bool AllPass =>
            ElementCapacityPass && FrameTimePass && GcAllocPass && DrawCallsPass;

        public ImGuiPerfGateResults(bool elementCapacity, bool frameTime, bool gcAlloc, bool drawCalls)
        {
            ElementCapacityPass = elementCapacity;
            FrameTimePass = frameTime;
            GcAllocPass = gcAlloc;
            DrawCallsPass = drawCalls;
        }
    }
}
