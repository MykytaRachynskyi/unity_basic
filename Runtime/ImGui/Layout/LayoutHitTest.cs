using Unity.Collections;
using Unity.Mathematics;

namespace Basic.ImGui.Layout
{
    internal static class LayoutHitTest
    {
        public static bool Contains(BoundingBox box, float2 point) =>
            point.x >= box.X &&
            point.x <= box.X + box.Width &&
            point.y >= box.Y &&
            point.y <= box.Y + box.Height;

        public static uint FindTopmost(
            NativeArray<LayoutElement> elements,
            NativeArray<int> childIndices,
            NativeArray<int> rootIndices,
            int rootCount,
            float2 pointer)
        {
            uint hitId = 0;
            for (var r = 0; r < rootCount; r++)
            {
                HitTestSubtree(elements, childIndices, rootIndices[r], pointer, ref hitId);
            }

            return hitId;
        }

        public static uint FindTopmostScrollContainer(
            NativeArray<LayoutElement> elements,
            NativeArray<int> childIndices,
            NativeArray<int> rootIndices,
            int rootCount,
            float2 pointer)
        {
            uint hitId = 0;
            for (var r = 0; r < rootCount; r++)
            {
                HitTestScrollSubtree(elements, childIndices, rootIndices[r], pointer, ref hitId);
            }

            return hitId;
        }

        static void HitTestSubtree(
            NativeArray<LayoutElement> elements,
            NativeArray<int> childIndices,
            int index,
            float2 pointer,
            ref uint hitId)
        {
            var element = elements[index];
            var box = new BoundingBox(element.X, element.Y, element.WidthResolved, element.HeightResolved);
            if (!Contains(box, pointer))
            {
                return;
            }

            hitId = element.ElementId;
            for (var c = 0; c < element.ChildCount; c++)
            {
                HitTestSubtree(elements, childIndices, childIndices[element.FirstChild + c], pointer, ref hitId);
            }
        }

        static void HitTestScrollSubtree(
            NativeArray<LayoutElement> elements,
            NativeArray<int> childIndices,
            int index,
            float2 pointer,
            ref uint hitId)
        {
            var element = elements[index];
            var box = new BoundingBox(element.X, element.Y, element.WidthResolved, element.HeightResolved);
            if (!Contains(box, pointer))
            {
                return;
            }

            if (element.Kind == LayoutNodeKind.Container && element.ClipChildren)
            {
                hitId = element.ElementId;
            }

            for (var c = 0; c < element.ChildCount; c++)
            {
                HitTestScrollSubtree(elements, childIndices, childIndices[element.FirstChild + c], pointer, ref hitId);
            }
        }
    }
}
