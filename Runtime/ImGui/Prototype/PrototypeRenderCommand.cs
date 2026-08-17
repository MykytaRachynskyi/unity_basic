using UnityEngine;

namespace Basic.ImGui.Prototype
{
    /// <summary>PROTOTYPE — throwaway rect command for canvas batching benchmark (#12).</summary>
    public struct PrototypeRenderCommand
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public Color32 Color;
    }
}
