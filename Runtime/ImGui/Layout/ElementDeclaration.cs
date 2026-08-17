using UnityEngine;

namespace Basic.ImGui.Layout
{
    public struct ElementDeclaration
    {
        public Color32 BackgroundColor;
        public Vector4 CornerRadius;
        public float PaddingLeft;
        public float PaddingTop;
        public float PaddingRight;
        public float PaddingBottom;
        public float ChildGap;
        public bool ClipChildren;

        public static ElementDeclaration Empty => default;

        public ElementDeclaration OnHover(ElementHoverCallback callback)
        {
            HoverCallback = callback;
            return this;
        }

        internal ElementHoverCallback HoverCallback;
    }

    public delegate void ElementHoverCallback(ElementId id, PointerData data);

    public readonly struct PointerData
    {
        public readonly Vector2 Position;
        public readonly bool IsPointerDown;

        public PointerData(Vector2 position, bool isPointerDown)
        {
            Position = position;
            IsPointerDown = isPointerDown;
        }
    }
}
