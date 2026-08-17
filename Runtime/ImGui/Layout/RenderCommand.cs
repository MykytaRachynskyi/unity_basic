namespace Basic.ImGui.Layout
{
    public enum RenderCommandType : byte
    {
        Rectangle = 0,
        Text = 1,
        ScissorStart = 2,
        ScissorEnd = 3
    }

    public struct RectangleRenderData
    {
        public UnityEngine.Color32 Background;
        public UnityEngine.Vector4 CornerRadius;
    }

    public struct TextRenderData
    {
        public FontId Font;
        public float FontSize;
        public UnityEngine.Color32 Color;
        public TextSlice Text;
    }

    public struct ScissorRenderData
    {
        public BoundingBox ClipRect;
    }

    public struct RenderData
    {
        public RectangleRenderData Rectangle;
        public TextRenderData Text;
        public ScissorRenderData Scissor;
    }

    public struct RenderCommand
    {
        public BoundingBox BoundingBox;
        public RenderCommandType CommandType;
        public RenderData RenderData;
        public uint ElementId;
        public short ZIndex;
    }
}
