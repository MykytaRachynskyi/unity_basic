using UnityEngine;

namespace Basic.ImGui.Layout
{
    internal struct LayoutElement
    {
        public uint ElementId;
        public LayoutNodeKind Kind;
        public int ParentIndex;
        public int FirstChild;
        public int ChildCount;

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
        public Color32 BackgroundColor;
        public Vector4 CornerRadius;

        public int TextSliceIndex;
        public float TextWidth;
        public float TextHeight;
        public int TextLineCount;
        public bool TextWrap;
        public float TextFontSize;
        public float TextLetterSpacing;
        public int WordStart;
        public int WordCount;

        public float ScrollOffsetX;
        public float ScrollOffsetY;

        public float X;
        public float Y;
        public float WidthResolved;
        public float HeightResolved;
        public float IntrinsicWidth;
        public float IntrinsicHeight;
    }
}
