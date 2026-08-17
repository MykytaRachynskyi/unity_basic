using Unity.Mathematics;

namespace Basic.ImGui.Layout
{
    public struct BoundingBox
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public float4 AsFloat4 => new float4(X, Y, Width, Height);

        public BoundingBox(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
