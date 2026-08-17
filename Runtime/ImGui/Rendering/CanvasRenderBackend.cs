using Basic.ImGui.Layout;

namespace Basic.ImGui.Rendering
{
    public sealed class CanvasRenderBackend : IRenderBackend
    {
        public void Draw(RenderFrame frame, RenderBackendContext context)
        {
            // Phase 0: renderer submission is deferred to Phase 2.
        }
    }
}
