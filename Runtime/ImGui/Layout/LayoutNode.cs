using UnityEngine;

namespace Basic.ImGui.Layout
{
    internal enum LayoutNodeKind : byte
    {
        Container = 0,
        Text = 1
    }

    internal struct LayoutFrameHeader
    {
        public int NodeCount;
    }

    internal struct LayoutNode
    {
        public ElementId Id;
        public LayoutNodeKind Kind;
        public int ParentIndex;
        public int FirstChildIndex;
        public int NextSiblingIndex;
        public ElementDeclaration Declaration;
        public TextConfig TextConfig;
        public int TextSliceIndex;
        public TextMetrics TextMetrics;
        public BoundingBox Bounds;
    }
}
