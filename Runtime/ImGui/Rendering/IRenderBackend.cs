using Basic.ImGui.Layout;

namespace Basic.ImGui.Rendering
{
    public interface IRenderBackend
    {
        void Draw(RenderFrame frame, RenderBackendContext context);
    }
}
