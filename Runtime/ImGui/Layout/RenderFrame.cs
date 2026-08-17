using UnityEngine;

namespace Basic.ImGui.Layout
{
    public readonly struct RenderFrame
    {
        public RenderCommandBuffer Commands { get; }
        public FrameStringBuffer Strings { get; }
        public Vector2 LayoutDimensions { get; }

        public RenderFrame(RenderCommandBuffer commands, FrameStringBuffer strings, Vector2 layoutDimensions)
        {
            Commands = commands;
            Strings = strings;
            LayoutDimensions = layoutDimensions;
        }
    }
}
