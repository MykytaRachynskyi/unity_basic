using UnityEngine;

namespace Basic.ImGui.Layout
{
    public struct ElementDeclaration
    {
        public Color32 BackgroundColor;
        public Vector4 CornerRadius;
        public LayoutDirection Direction;
        public LayoutSizing Width;
        public LayoutSizing Height;
        public float PaddingLeft;
        public float PaddingTop;
        public float PaddingRight;
        public float PaddingBottom;
        public float ChildGap;
        public ChildAlignment ChildAlignmentX;
        public ChildAlignment ChildAlignmentY;
        public bool ClipChildren;
        public bool ClipHorizontal;
        public bool ClipVertical;

        public static ElementDeclaration Empty => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Grow(),
            Direction = LayoutDirection.TopToBottom,
            ChildAlignmentX = ChildAlignment.Start,
            ChildAlignmentY = ChildAlignment.Start
        };

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
