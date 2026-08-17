using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Basic.ImGui.Layout
{
    [BurstCompile]
    internal struct LayoutResolveJob : IJob
    {
        public NativeArray<LayoutElement> Elements;
        [ReadOnly] public NativeArray<int> ChildIndices;
        [ReadOnly] public NativeArray<int> RootIndices;
        [ReadOnly] public NativeArray<MeasuredWord> Words;
        public float2 LayoutDimensions;

        public void Execute()
        {
            for (var i = Elements.Length - 1; i >= 0; i--)
            {
                ComputeIntrinsicSize(i);
            }

            for (var r = 0; r < RootIndices.Length; r++)
            {
                ResolveWidths(RootIndices[r], LayoutDimensions.x, LayoutDimensions.y);
            }

            TextWrapPass();

            for (var i = Elements.Length - 1; i >= 0; i--)
            {
                if (Elements[i].Kind == LayoutNodeKind.Container)
                {
                    ComputeIntrinsicSize(i);
                }
            }

            for (var r = 0; r < RootIndices.Length; r++)
            {
                ResolveAndPosition(RootIndices[r], 0f, 0f, LayoutDimensions.x, LayoutDimensions.y);
            }
        }

        void ResolveWidths(int index, float width, float height)
        {
            var element = Elements[index];
            element.WidthResolved = ResolveAxis(element.Width, width, element.IntrinsicWidth);
            element.HeightResolved = ResolveAxis(element.Height, height, element.IntrinsicHeight);
            Elements[index] = element;

            var innerWidth = math.max(
                0f,
                element.WidthResolved - element.PaddingLeft - element.PaddingRight);
            var innerHeight = math.max(
                0f,
                element.HeightResolved - element.PaddingTop - element.PaddingBottom);

            for (var c = 0; c < element.ChildCount; c++)
            {
                var childIndex = ChildIndices[element.FirstChild + c];
                var child = Elements[childIndex];
                var childWidth = ResolveAxis(child.Width, innerWidth, child.IntrinsicWidth);
                var childHeight = ResolveAxis(child.Height, innerHeight, child.IntrinsicHeight);
                child.WidthResolved = childWidth;
                child.HeightResolved = childHeight;
                Elements[childIndex] = child;
                ResolveWidths(childIndex, childWidth, childHeight);
            }
        }

        void ResolveAndPosition(int index, float x, float y, float width, float height)
        {
            var element = Elements[index];
            element.X = x;
            element.Y = y;
            element.WidthResolved = ResolveAxis(element.Width, width, element.IntrinsicWidth);
            element.HeightResolved = ResolveAxis(element.Height, height, element.IntrinsicHeight);
            Elements[index] = element;

            var childCount = element.ChildCount;
            if (childCount == 0)
            {
                return;
            }

            var innerX = x + element.PaddingLeft;
            var innerY = y + element.PaddingTop;
            var innerWidth = math.max(0f, element.WidthResolved - element.PaddingLeft - element.PaddingRight);
            var innerHeight = math.max(0f, element.HeightResolved - element.PaddingTop - element.PaddingBottom);
            var scrollX = element.ClipHorizontal ? element.ScrollOffsetX : 0f;
            var scrollY = element.ClipVertical ? element.ScrollOffsetY : 0f;
            var cursorX = innerX - scrollX;
            var cursorY = innerY - scrollY;
            var firstChild = element.FirstChild;
            var isRow = element.Direction == LayoutDirection.LeftToRight;

            if (isRow)
            {
                var fixedWidth = 0f;
                var growCount = 0;
                for (var c = 0; c < childCount; c++)
                {
                    var childIndex = ChildIndices[firstChild + c];
                    var child = Elements[childIndex];
                    if (child.Width.Type == SizingType.Grow)
                    {
                        growCount++;
                        continue;
                    }

                    var childWidth = ResolveAxis(child.Width, innerWidth, child.IntrinsicWidth);
                    child.WidthResolved = childWidth;
                    Elements[childIndex] = child;
                    fixedWidth += childWidth;
                }

                fixedWidth += math.max(0, childCount - 1) * element.ChildGap;
                var growWidth = growCount > 0
                    ? math.max(0f, innerWidth - fixedWidth) / growCount
                    : 0f;

                for (var c = 0; c < childCount; c++)
                {
                    var childIndex = ChildIndices[firstChild + c];
                    var child = Elements[childIndex];
                    var childWidth = child.WidthResolved > 0f
                        ? child.WidthResolved
                        : ResolveAxis(child.Width, growWidth > 0f ? growWidth : innerWidth, child.IntrinsicWidth);
                    var childHeight = ResolveAxis(child.Height, innerHeight, child.IntrinsicHeight);
                    var childX = cursorX;
                    var childY = AlignOnAxis(element.ChildAlignmentY, innerY, innerHeight, childHeight);
                    ResolveAndPosition(childIndex, childX, childY, childWidth, childHeight);
                    cursorX += childWidth + element.ChildGap;
                }

                return;
            }

            var fixedHeight = 0f;
            var growRowCount = 0;
            for (var c = 0; c < childCount; c++)
            {
                var childIndex = ChildIndices[firstChild + c];
                var child = Elements[childIndex];
                var childWidth = ResolveAxis(child.Width, innerWidth, child.IntrinsicWidth);
                child.WidthResolved = childWidth;

                if (child.Height.Type == SizingType.Grow)
                {
                    Elements[childIndex] = child;
                    growRowCount++;
                    continue;
                }

                var childHeight = ResolveAxis(child.Height, innerHeight, child.IntrinsicHeight);
                child.HeightResolved = childHeight;
                Elements[childIndex] = child;
                fixedHeight += childHeight;
            }

            fixedHeight += math.max(0, childCount - 1) * element.ChildGap;
            var growHeight = growRowCount > 0
                ? math.max(0f, innerHeight - fixedHeight) / growRowCount
                : 0f;

            for (var c = 0; c < childCount; c++)
            {
                var childIndex = ChildIndices[firstChild + c];
                var child = Elements[childIndex];
                var childWidth = child.WidthResolved > 0f
                    ? child.WidthResolved
                    : ResolveAxis(child.Width, innerWidth, child.IntrinsicWidth);
                var childHeight = child.HeightResolved > 0f
                    ? child.HeightResolved
                    : ResolveAxis(child.Height, growHeight > 0f ? growHeight : innerHeight, child.IntrinsicHeight);
                var childX = AlignOnAxis(element.ChildAlignmentX, innerX, innerWidth, childWidth);
                var childY = cursorY;
                ResolveAndPosition(childIndex, childX, childY, childWidth, childHeight);
                cursorY += childHeight + element.ChildGap;
            }
        }

        void ComputeIntrinsicSize(int index)
        {
            var element = Elements[index];
            if (element.Kind == LayoutNodeKind.Text)
            {
                element.IntrinsicWidth = element.TextWidth;
                element.IntrinsicHeight = element.TextHeight;
                Elements[index] = element;
                return;
            }

            var horizontalPadding = element.PaddingLeft + element.PaddingRight;
            var verticalPadding = element.PaddingTop + element.PaddingBottom;
            var childCount = element.ChildCount;
            if (childCount == 0)
            {
                element.IntrinsicWidth = horizontalPadding;
                element.IntrinsicHeight = verticalPadding;
                Elements[index] = element;
                return;
            }

            var maxChildWidth = 0f;
            var maxChildHeight = 0f;
            var totalWidth = 0f;
            var totalHeight = 0f;
            var firstChild = element.FirstChild;
            for (var c = 0; c < childCount; c++)
            {
                var childIndex = ChildIndices[firstChild + c];
                var child = Elements[childIndex];
                maxChildWidth = math.max(maxChildWidth, child.IntrinsicWidth);
                maxChildHeight = math.max(maxChildHeight, child.IntrinsicHeight);
                totalWidth += child.IntrinsicWidth;
                totalHeight += child.IntrinsicHeight;
            }

            var gapTotal = math.max(0, childCount - 1) * element.ChildGap;
            if (element.Direction == LayoutDirection.TopToBottom)
            {
                element.IntrinsicWidth = maxChildWidth + horizontalPadding;
                element.IntrinsicHeight = totalHeight + gapTotal + verticalPadding;
            }
            else
            {
                element.IntrinsicWidth = totalWidth + gapTotal + horizontalPadding;
                element.IntrinsicHeight = maxChildHeight + verticalPadding;
            }

            Elements[index] = element;
        }

        void TextWrapPass()
        {
            for (var i = 0; i < Elements.Length; i++)
            {
                var element = Elements[i];
                if (element.Kind != LayoutNodeKind.Text || !element.TextWrap || element.WordCount == 0)
                {
                    continue;
                }

                var maxWidth = element.WidthResolved > 0f
                    ? element.WidthResolved
                    : element.IntrinsicWidth;
                if (element.ParentIndex >= 0)
                {
                    var parent = Elements[element.ParentIndex];
                    var parentInner = parent.WidthResolved > 0f
                        ? parent.WidthResolved - parent.PaddingLeft - parent.PaddingRight
                        : parent.IntrinsicWidth - parent.PaddingLeft - parent.PaddingRight;
                    maxWidth = math.min(maxWidth, math.max(0f, parentInner));
                }

                if (maxWidth <= 0f)
                {
                    continue;
                }

                var lineCount = 0;
                var lineWidth = 0f;
                var maxLineWidth = 0f;
                var spaceWidth = element.TextFontSize * 0.25f + element.TextLetterSpacing;

                for (var w = 0; w < element.WordCount; w++)
                {
                    var word = Words[element.WordStart + w];
                    var wordWidth = word.Width;
                    var needsSpace = lineWidth > 0f;
                    var candidate = lineWidth + (needsSpace ? spaceWidth : 0f) + wordWidth;
                    if (lineCount == 0 || candidate <= maxWidth)
                    {
                        lineWidth = candidate;
                        if (lineCount == 0)
                        {
                            lineCount = 1;
                        }
                    }
                    else
                    {
                        maxLineWidth = math.max(maxLineWidth, lineWidth);
                        lineCount++;
                        lineWidth = wordWidth;
                    }
                }

                if (lineCount == 0)
                {
                    lineCount = 1;
                }

                maxLineWidth = math.max(maxLineWidth, lineWidth);
                var lineHeight = element.TextFontSize * 1.2f;
                element.TextWidth = maxLineWidth;
                element.TextHeight = lineCount * lineHeight;
                element.TextLineCount = lineCount;
                element.IntrinsicWidth = maxLineWidth;
                element.IntrinsicHeight = element.TextHeight;
                element.WidthResolved = 0f;
                element.HeightResolved = 0f;
                Elements[i] = element;
            }
        }

        static float AlignOnAxis(ChildAlignment alignment, float origin, float outer, float inner)
        {
            return alignment switch
            {
                ChildAlignment.Center => origin + (outer - inner) * 0.5f,
                ChildAlignment.End => origin + outer - inner,
                _ => origin
            };
        }

        static float ResolveAxis(LayoutSizing sizing, float parentSize, float intrinsic)
        {
            switch (sizing.Type)
            {
                case SizingType.Fixed:
                    return sizing.Value;
                case SizingType.Percent:
                    return parentSize * sizing.Value;
                case SizingType.Grow:
                    return math.clamp(parentSize, sizing.Min, sizing.Max);
                default:
                    return math.clamp(intrinsic, sizing.Min, sizing.Max);
            }
        }
    }
}
