using UnityEngine;

namespace Basic.ImGui.Layout
{
    public static class ElementPresets
    {
        public static ElementDeclaration Panel => new ElementDeclaration
        {
            BackgroundColor = new Color32(40, 40, 40, 255),
            CornerRadius = new Vector4(4f, 4f, 4f, 4f),
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Grow(),
            Direction = LayoutDirection.TopToBottom,
            PaddingLeft = 8f,
            PaddingTop = 8f,
            PaddingRight = 8f,
            PaddingBottom = 8f,
            ChildGap = 4f
        };

        public static ElementDeclaration FullScreen => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Grow(),
            Direction = LayoutDirection.TopToBottom,
            PaddingLeft = 0f,
            PaddingTop = 0f,
            PaddingRight = 0f,
            PaddingBottom = 0f
        };

        public static ElementDeclaration ScrollVertical => new ElementDeclaration
        {
            Width = LayoutSizing.Grow(),
            Height = LayoutSizing.Grow(),
            Direction = LayoutDirection.TopToBottom,
            ClipChildren = true,
            ClipVertical = true,
            PaddingLeft = 4f,
            PaddingTop = 4f,
            PaddingRight = 4f,
            PaddingBottom = 4f,
            ChildGap = 2f
        };

        public static ElementDeclaration Row => new ElementDeclaration
        {
            Width = LayoutSizing.Fit(),
            Height = LayoutSizing.Fit(),
            Direction = LayoutDirection.LeftToRight,
            ChildGap = 4f
        };
    }
}
