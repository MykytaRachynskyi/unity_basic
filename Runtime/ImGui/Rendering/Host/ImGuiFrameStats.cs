namespace Basic.ImGui.Rendering
{
    public readonly struct ImGuiFrameStats
    {
        public int ElementCount { get; }
        public int CommandCount { get; }
        public int BatchCount { get; }
        public int VertexCount { get; }
        public double LayoutMilliseconds { get; }
        public double RenderMilliseconds { get; }
        public long GcAllocBytes { get; }
        public int FrameIndex { get; }

        public double CombinedMilliseconds => LayoutMilliseconds + RenderMilliseconds;

        public ImGuiFrameStats(
            int elementCount,
            int commandCount,
            int batchCount,
            int vertexCount,
            double layoutMilliseconds,
            double renderMilliseconds,
            long gcAllocBytes,
            int frameIndex)
        {
            ElementCount = elementCount;
            CommandCount = commandCount;
            BatchCount = batchCount;
            VertexCount = vertexCount;
            LayoutMilliseconds = layoutMilliseconds;
            RenderMilliseconds = renderMilliseconds;
            GcAllocBytes = gcAllocBytes;
            FrameIndex = frameIndex;
        }
    }
}
