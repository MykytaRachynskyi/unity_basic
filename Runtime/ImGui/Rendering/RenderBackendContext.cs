using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public readonly struct RenderBackendContext
    {
        public Matrix4x4 ViewProjection { get; }
        public bool FlipY { get; }

        public RenderBackendContext(Matrix4x4 viewProjection, bool flipY)
        {
            ViewProjection = viewProjection;
            FlipY = flipY;
        }
    }
}
