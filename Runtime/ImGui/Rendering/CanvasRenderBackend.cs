using Basic.ImGui.Layout;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public sealed class CanvasRenderBackend : IRenderBackend
    {
        readonly ImGuiGraphic _graphic;

        public CanvasRenderBackend(ImGuiGraphic graphic)
        {
            _graphic = graphic;
        }

        public void Draw(RenderFrame frame, RenderBackendContext context)
        {
            if (_graphic == null)
            {
                return;
            }

            _graphic.SetFrame(frame, context);
        }
    }
}
