using UnityEngine;

namespace Basic.ImGui.Layout
{
    public readonly struct RenderFrame
    {
        public RenderCommandBuffer Commands { get; }
        public Vector2 LayoutDimensions { get; }

        public RenderFrame(RenderCommandBuffer commands, Vector2 layoutDimensions)
        {
            Commands = commands;
            LayoutDimensions = layoutDimensions;
        }
    }
}
