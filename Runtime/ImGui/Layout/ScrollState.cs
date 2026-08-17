using Unity.Mathematics;

namespace Basic.ImGui.Layout
{
    public struct ScrollState
    {
        public float2 ScrollPosition;
        public float2 Momentum;
        public float2 ContentSize;
        public float2 ViewportSize;
    }
}
