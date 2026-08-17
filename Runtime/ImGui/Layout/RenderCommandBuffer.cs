using Unity.Collections;

namespace Basic.ImGui.Layout
{
    public struct RenderCommandBuffer
    {
        public NativeArray<RenderCommand> Commands;
        public int Length;

        public bool IsCreated => Commands.IsCreated;
    }
}
