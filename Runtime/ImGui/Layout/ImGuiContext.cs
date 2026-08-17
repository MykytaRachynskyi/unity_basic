using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Basic.ImGui.Layout
{
    public sealed class ImGuiContext : IDisposable
    {
        const int MaxNodes = 8192;
        const int MaxCommands = 16384;
        const int MaxOpenDepth = 64;

        readonly ITextMeasurer _textMeasurer;
        readonly LayoutArena _arena = new LayoutArena();
        readonly FrameStringTable _frameStrings = new FrameStringTable();
        readonly ScrollStateMap _scrollStates = new ScrollStateMap();
        readonly HashSet<uint> _usedIds = new HashSet<uint>();
        readonly int[] _openStack = new int[MaxOpenDepth];

        LayoutNode[] _nodes;
        NativeArray<RenderCommand> _commands;
        NativeArray<LayoutElement> _layoutElements;
        NativeArray<int> _childIndices;
        NativeArray<int> _rootIndices;
        NativeList<MeasuredWord> _measuredWords;
        NativeParallelHashMap<uint, BoundingBox> _previousBounds;

        int _nodeCount;
        int _openDepth;
        int _commandCount;
        int _childIndexCount;
        int _rootCount;
        bool _layoutOpen;

        Vector2 _layoutDimensions;
        Vector2 _pointerPosition;
        Vector2 _pointerDelta;
        Vector2 _lastPointerPosition;
        bool _pointerDown;
        bool _pointerWasDown;
        uint _hoveredId;
        uint _scrollTargetId;
        uint _pressedId;
        uint _pressedThisFrameId;
        uint _releasedThisFrameId;
        RenderFrame _lastFrame;

        [ThreadStatic]
        static ImGuiContext s_current;

        public ImGuiContext(ITextMeasurer textMeasurer)
        {
            _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
            _nodes = new LayoutNode[MaxNodes];
            _commands = new NativeArray<RenderCommand>(MaxCommands, Allocator.Persistent);
            _layoutElements = new NativeArray<LayoutElement>(MaxNodes, Allocator.Persistent);
            _childIndices = new NativeArray<int>(MaxNodes, Allocator.Persistent);
            _rootIndices = new NativeArray<int>(MaxNodes, Allocator.Persistent);
            _measuredWords = new NativeList<MeasuredWord>(Allocator.Persistent);
            _previousBounds = new NativeParallelHashMap<uint, BoundingBox>(MaxNodes, Allocator.Persistent);
        }

        internal static ImGuiContext Current => s_current;

        public RenderFrame LastFrame => _lastFrame;

        public int LastElementCount { get; private set; }

        public void SetLayoutDimensions(Vector2 size) => _layoutDimensions = size;

        public void SetPointerState(Vector2 position, bool isPointerDown)
        {
            _pointerDelta = position - _pointerPosition;
            _pointerWasDown = _pointerDown;
            _lastPointerPosition = _pointerPosition;
            _pointerPosition = position;
            _pointerDown = isPointerDown;
        }

        public void UpdateScrollContainers(bool drag, Vector2 wheel, float deltaTime)
        {
            if (_scrollTargetId == 0 || !_scrollStates.TryGetValue(_scrollTargetId, out var state))
            {
                return;
            }

            if (wheel != Vector2.zero)
            {
                state.ScrollPosition += new float2(wheel.x, wheel.y) * 10f;
            }

            if (drag && _pointerDown)
            {
                state.ScrollPosition -= new float2(_pointerDelta.x, _pointerDelta.y);
            }

            if (!_pointerDown && math.lengthsq(state.Momentum) > 0.01f)
            {
                state.ScrollPosition += state.Momentum * deltaTime;
                state.Momentum *= 0.95f;
            }
            else if (_pointerDown && drag)
            {
                state.Momentum = new float2(_pointerDelta.x, _pointerDelta.y);
            }

            ClampScroll(ref state);
            _scrollStates.Set(_scrollTargetId, state);
        }

        public void BeginLayout()
        {
            if (_layoutOpen)
            {
                throw new InvalidOperationException("BeginLayout called while a layout pass is already open.");
            }

            _arena.Reset();
            ref var frameHeader = ref _arena.AllocateRef<LayoutFrameHeader>();
            frameHeader.NodeCount = 0;
            _usedIds.Clear();
            _frameStrings.Reset();
            _measuredWords.Clear();
            _nodeCount = 0;
            _openDepth = 0;
            _commandCount = 0;
            _childIndexCount = 0;
            _rootCount = 0;
            _pressedThisFrameId = 0;
            _releasedThisFrameId = 0;
            _layoutOpen = true;
            s_current = this;
        }

        public RenderFrame EndLayout(float deltaTime)
        {
            if (!_layoutOpen)
            {
                throw new InvalidOperationException("EndLayout called without BeginLayout.");
            }

            if (_openDepth != 0)
            {
                throw new InvalidOperationException("EndLayout called with unclosed elements.");
            }

            FlattenDeclarationTree();
            RunLayoutResolveJob();
            UpdateScrollContentSizes();
            EmitRenderCommands();
            UpdatePointerState();

            LastElementCount = _nodeCount;
            _lastFrame = new RenderFrame(
                new RenderCommandBuffer { Commands = _commands, Length = _commandCount },
                new FrameStringBuffer { Chars = _frameStrings.Chars, Length = _frameStrings.Count },
                _layoutDimensions);

            _layoutOpen = false;
            s_current = null;
            return _lastFrame;
        }

        public ElementScope Element(ElementId id) => Element(id, ElementDeclaration.Empty);

        public ElementScope Element(ElementId id, ElementDeclaration declaration)
        {
            EnsureLayoutOpen();
            id = ResolveElementId(id);
            RegisterId(id);
            InvokeHoverCallback(id, declaration.HoverCallback);

            var nodeIndex = AddNode(id, LayoutNodeKind.Container, declaration);
            AttachToOpenParent(nodeIndex);
            PushOpen(nodeIndex);

            return new ElementScope(this);
        }

        public void Text(ElementId id, ReadOnlySpan<char> text, TextConfig config)
        {
            EnsureLayoutOpen();
            id = ResolveElementId(id);
            RegisterId(id);

            var textSliceIndex = _frameStrings.Add(text);
            var textSlice = _frameStrings.GetSlice(textSliceIndex);
            var textSpan = _frameStrings.GetSpan(textSlice);
            var metrics = default(TextMetrics);
            _textMeasurer.Measure(textSpan, config.Font, config.FontSize, config.LetterSpacing, ref metrics);

            var wordStart = _measuredWords.Length;
            var wordCount = TokenizeAndMeasureWords(textSpan, config, textSlice.StartIndex);

            var nodeIndex = AddNode(id, LayoutNodeKind.Text, default);
            ref var node = ref _nodes[nodeIndex];
            node.TextConfig = config;
            node.TextSliceIndex = textSliceIndex;
            node.TextMetrics = metrics;
            node.WordStart = wordStart;
            node.WordCount = wordCount;

            AttachToOpenParent(nodeIndex);
        }

        public void Text(ElementId id, string text, TextConfig config) => Text(id, text.AsSpan(), config);

        public static ElementId Local(ReadOnlySpan<char> label)
        {
            if (s_current == null)
            {
                throw new InvalidOperationException("ElementId.Local requires an active ImGuiContext layout pass.");
            }

            return s_current.CreateLocalId(label);
        }

        public static ElementId Local(string label) => Local(label.AsSpan());

        public ElementId GetElementId(string label) => ElementId.From(label);

        public bool TryGetHoveredId(out ElementId id)
        {
            if (_hoveredId == 0)
            {
                id = default;
                return false;
            }

            id = ElementId.FromResolved(_hoveredId);
            return true;
        }

        public bool TryGetPressedId(out ElementId id)
        {
            if (_pressedId == 0)
            {
                id = default;
                return false;
            }

            id = ElementId.FromResolved(_pressedId);
            return true;
        }

        public bool WasPressedThisFrame(ElementId id) => id.Id != 0 && id.Id == _pressedThisFrameId;

        public bool WasReleasedThisFrame(ElementId id) => id.Id != 0 && id.Id == _releasedThisFrameId;

        public bool IsPressed(ElementId id) => id.Id != 0 && _pointerDown && id.Id == _pressedId;

        public bool TryGetScrollOffset(ElementId id, out Vector2 offset)
        {
            if (_scrollStates.TryGetValue(id.Id, out var state))
            {
                offset = new Vector2(state.ScrollPosition.x, state.ScrollPosition.y);
                return true;
            }

            offset = default;
            return false;
        }

        internal void CloseElement()
        {
            if (_openDepth == 0)
            {
                throw new InvalidOperationException("CloseElement called with no open elements.");
            }

            _openDepth--;
        }

        internal void OverrideOpenPadding(float left, float top, float right, float bottom)
        {
            ref var node = ref GetOpenNode();
            node.Declaration.PaddingLeft = left;
            node.Declaration.PaddingTop = top;
            node.Declaration.PaddingRight = right;
            node.Declaration.PaddingBottom = bottom;
        }

        internal void OverrideOpenChildGap(float gap)
        {
            ref var node = ref GetOpenNode();
            node.Declaration.ChildGap = gap;
        }

        internal void OverrideOpenHover(ElementHoverCallback callback)
        {
            ref var node = ref GetOpenNode();
            node.Declaration.HoverCallback = callback;
        }

        internal ElementId CreateLocalId(ReadOnlySpan<char> label)
        {
            var parentSeed = _openDepth > 0 ? _nodes[_openStack[_openDepth - 1]].Id.Id : 0u;
            return ElementId.FromParentSeed(label, parentSeed);
        }

        public void Dispose()
        {
            if (_commands.IsCreated)
            {
                _commands.Dispose();
            }

            if (_layoutElements.IsCreated)
            {
                _layoutElements.Dispose();
            }

            if (_childIndices.IsCreated)
            {
                _childIndices.Dispose();
            }

            if (_rootIndices.IsCreated)
            {
                _rootIndices.Dispose();
            }

            if (_measuredWords.IsCreated)
            {
                _measuredWords.Dispose();
            }

            if (_previousBounds.IsCreated)
            {
                _previousBounds.Dispose();
            }

            _frameStrings.Dispose();
            _arena.Dispose();
            _scrollStates.Dispose();
            s_current = null;
        }

        void EnsureLayoutOpen()
        {
            if (!_layoutOpen)
            {
                throw new InvalidOperationException("Declaration APIs require an open layout pass. Call BeginLayout first.");
            }
        }

        ElementId ResolveElementId(ElementId id) => id;

        void RegisterId(ElementId id)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_usedIds.Add(id.Id))
            {
                throw new InvalidOperationException($"Duplicate element id {id.Id} in one layout pass.");
            }
#endif
        }

        int AddNode(ElementId id, LayoutNodeKind kind, ElementDeclaration declaration)
        {
            if (_nodeCount >= MaxNodes)
            {
                throw new InvalidOperationException($"Exceeded maximum layout node count ({MaxNodes}).");
            }

            var index = _nodeCount++;
            _nodes[index] = new LayoutNode
            {
                Id = id,
                Kind = kind,
                ParentIndex = -1,
                FirstChildIndex = -1,
                NextSiblingIndex = -1,
                Declaration = declaration
            };

            return index;
        }

        void AttachToOpenParent(int childIndex)
        {
            if (_openDepth == 0)
            {
                return;
            }

            var parentIndex = _openStack[_openDepth - 1];
            ref var parent = ref _nodes[parentIndex];
            ref var child = ref _nodes[childIndex];

            child.ParentIndex = parentIndex;

            if (parent.FirstChildIndex < 0)
            {
                parent.FirstChildIndex = childIndex;
                return;
            }

            var siblingIndex = parent.FirstChildIndex;
            while (_nodes[siblingIndex].NextSiblingIndex >= 0)
            {
                siblingIndex = _nodes[siblingIndex].NextSiblingIndex;
            }

            _nodes[siblingIndex].NextSiblingIndex = childIndex;
        }

        void PushOpen(int nodeIndex)
        {
            if (_openDepth >= MaxOpenDepth)
            {
                throw new InvalidOperationException($"Exceeded maximum open element depth ({MaxOpenDepth}).");
            }

            _openStack[_openDepth++] = nodeIndex;
        }

        ref LayoutNode GetOpenNode()
        {
            if (_openDepth == 0)
            {
                throw new InvalidOperationException("No open element to override.");
            }

            return ref _nodes[_openStack[_openDepth - 1]];
        }

        int TokenizeAndMeasureWords(ReadOnlySpan<char> text, TextConfig config, int baseStartIndex)
        {
            var wordCount = 0;
            var index = 0;
            while (index < text.Length)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                if (index >= text.Length)
                {
                    break;
                }

                var start = index;
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                var wordSpan = text.Slice(start, index - start);
                var width = _textMeasurer.MeasureWord(
                    wordSpan,
                    config.Font,
                    config.FontSize,
                    config.LetterSpacing);

                _measuredWords.Add(new MeasuredWord
                {
                    StartIndex = baseStartIndex + start,
                    Length = wordSpan.Length,
                    Width = width
                });
                wordCount++;
            }

            return wordCount;
        }

        void InvokeHoverCallback(ElementId id, ElementHoverCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            if (_previousBounds.TryGetValue(id.Id, out var bounds) &&
                LayoutHitTest.Contains(bounds, new float2(_pointerPosition.x, _pointerPosition.y)))
            {
                callback(id, new PointerData(_pointerPosition, _pointerDown));
            }
        }

        void FlattenDeclarationTree()
        {
            _rootCount = 0;
            _childIndexCount = 0;

            for (var i = 0; i < _nodeCount; i++)
            {
                ref var node = ref _nodes[i];
                var declaration = node.Kind == LayoutNodeKind.Container ? node.Declaration : default;
                if (node.Kind == LayoutNodeKind.Container && declaration.ClipChildren)
                {
                    declaration.ClipVertical = declaration.ClipVertical || declaration.ClipChildren;
                }

                float scrollX = 0f;
                float scrollY = 0f;
                if (node.Kind == LayoutNodeKind.Container && declaration.ClipChildren &&
                    _scrollStates.TryGetValue(node.Id.Id, out var scrollState))
                {
                    scrollX = scrollState.ScrollPosition.x;
                    scrollY = scrollState.ScrollPosition.y;
                }

                _layoutElements[i] = new LayoutElement
                {
                    ElementId = node.Id.Id,
                    Kind = node.Kind,
                    ParentIndex = node.ParentIndex,
                    Direction = declaration.Direction,
                    Width = node.Kind == LayoutNodeKind.Text
                        ? LayoutSizing.Fit()
                        : declaration.Width,
                    Height = node.Kind == LayoutNodeKind.Text
                        ? LayoutSizing.Fit()
                        : declaration.Height,
                    PaddingLeft = declaration.PaddingLeft,
                    PaddingTop = declaration.PaddingTop,
                    PaddingRight = declaration.PaddingRight,
                    PaddingBottom = declaration.PaddingBottom,
                    ChildGap = declaration.ChildGap,
                    ChildAlignmentX = declaration.ChildAlignmentX,
                    ChildAlignmentY = declaration.ChildAlignmentY,
                    ClipChildren = declaration.ClipChildren,
                    ClipHorizontal = declaration.ClipHorizontal,
                    ClipVertical = declaration.ClipVertical,
                    BackgroundColor = declaration.BackgroundColor,
                    CornerRadius = declaration.CornerRadius,
                    TextSliceIndex = node.TextSliceIndex,
                    TextWidth = node.TextMetrics.Width,
                    TextHeight = node.TextMetrics.Height,
                    TextLineCount = node.TextMetrics.LineCount,
                    TextWrap = node.TextConfig.Wrap,
                    TextFontSize = node.TextConfig.FontSize,
                    TextLetterSpacing = node.TextConfig.LetterSpacing,
                    WordStart = node.WordStart,
                    WordCount = node.WordCount,
                    ScrollOffsetX = scrollX,
                    ScrollOffsetY = scrollY
                };

                if (node.ParentIndex < 0)
                {
                    _rootIndices[_rootCount++] = i;
                }
            }

            for (var i = 0; i < _nodeCount; i++)
            {
                ref var node = ref _nodes[i];
                if (node.Kind != LayoutNodeKind.Container || node.FirstChildIndex < 0)
                {
                    _layoutElements[i] = WithChildren(_layoutElements[i], -1, 0);
                    continue;
                }

                var firstChildSlot = _childIndexCount;
                var childCount = 0;
                for (var childIndex = node.FirstChildIndex; childIndex >= 0; childIndex = _nodes[childIndex].NextSiblingIndex)
                {
                    _childIndices[_childIndexCount++] = childIndex;
                    childCount++;
                }

                _layoutElements[i] = WithChildren(_layoutElements[i], firstChildSlot, childCount);
            }
        }

        static LayoutElement WithChildren(LayoutElement element, int firstChild, int childCount)
        {
            element.FirstChild = firstChild;
            element.ChildCount = childCount;
            return element;
        }

        void RunLayoutResolveJob()
        {
            var elementsSlice = _layoutElements.GetSubArray(0, _nodeCount);
            var childSlice = _childIndices.GetSubArray(0, math.max(1, _childIndexCount));
            var rootSlice = _rootIndices.GetSubArray(0, math.max(1, _rootCount));

            var job = new LayoutResolveJob
            {
                Elements = elementsSlice,
                ChildIndices = childSlice,
                RootIndices = rootSlice,
                Words = _measuredWords.AsArray(),
                LayoutDimensions = new float2(_layoutDimensions.x, _layoutDimensions.y)
            };

            job.Schedule().Complete();
        }

        void UpdateScrollContentSizes()
        {
            for (var i = 0; i < _nodeCount; i++)
            {
                var element = _layoutElements[i];
                if (element.Kind != LayoutNodeKind.Container || !element.ClipChildren)
                {
                    continue;
                }

                var contentWidth = 0f;
                var contentHeight = 0f;
                for (var c = 0; c < element.ChildCount; c++)
                {
                    var child = _layoutElements[_childIndices[element.FirstChild + c]];
                    contentWidth = math.max(contentWidth, child.X + child.WidthResolved - element.X - element.PaddingLeft);
                    contentHeight = math.max(contentHeight, child.Y + child.HeightResolved - element.Y - element.PaddingTop);
                }

                var state = _scrollStates.TryGetValue(element.ElementId, out var existing)
                    ? existing
                    : default;
                state.ViewportSize = new float2(element.WidthResolved, element.HeightResolved);
                state.ContentSize = new float2(contentWidth, contentHeight);
                ClampScroll(ref state);
                _scrollStates.Set(element.ElementId, state);
            }
        }

        void EmitRenderCommands()
        {
            _commandCount = 0;
            _previousBounds.Clear();

            for (var r = 0; r < _rootCount; r++)
            {
                EmitNodeCommands(_rootIndices[r]);
            }
        }

        void EmitNodeCommands(int nodeIndex)
        {
            var element = _layoutElements[nodeIndex];
            var bounds = new BoundingBox(element.X, element.Y, element.WidthResolved, element.HeightResolved);
            _previousBounds[element.ElementId] = bounds;

            if (element.Kind == LayoutNodeKind.Container && element.BackgroundColor.a > 0)
            {
                AppendCommand(new RenderCommand
                {
                    BoundingBox = bounds,
                    CommandType = RenderCommandType.Rectangle,
                    ElementId = element.ElementId,
                    RenderData = new RenderData
                    {
                        Rectangle = new RectangleRenderData
                        {
                            Background = element.BackgroundColor,
                            CornerRadius = element.CornerRadius
                        }
                    }
                });
            }

            var openedScissor = element.Kind == LayoutNodeKind.Container && element.ClipChildren;
            if (openedScissor)
            {
                AppendCommand(new RenderCommand
                {
                    BoundingBox = bounds,
                    CommandType = RenderCommandType.ScissorStart,
                    ElementId = element.ElementId,
                    RenderData = new RenderData
                    {
                        Scissor = new ScissorRenderData { ClipRect = bounds }
                    }
                });
            }

            if (element.Kind == LayoutNodeKind.Container)
            {
                for (var c = 0; c < element.ChildCount; c++)
                {
                    EmitNodeCommands(_childIndices[element.FirstChild + c]);
                }
            }
            else
            {
                AppendCommand(new RenderCommand
                {
                    BoundingBox = bounds,
                    CommandType = RenderCommandType.Text,
                    ElementId = element.ElementId,
                    RenderData = new RenderData
                    {
                        Text = new TextRenderData
                        {
                            Font = _nodes[nodeIndex].TextConfig.Font,
                            FontSize = element.TextFontSize,
                            Color = _nodes[nodeIndex].TextConfig.Color,
                            Text = _frameStrings.GetSlice(element.TextSliceIndex)
                        }
                    }
                });
            }

            if (openedScissor)
            {
                AppendCommand(new RenderCommand
                {
                    CommandType = RenderCommandType.ScissorEnd,
                    ElementId = element.ElementId
                });
            }
        }

        void UpdatePointerState()
        {
            var pointer = new float2(_pointerPosition.x, _pointerPosition.y);
            var elementsSlice = _layoutElements.GetSubArray(0, _nodeCount);
            _hoveredId = LayoutHitTest.FindTopmost(elementsSlice, _childIndices, _rootIndices, _rootCount, pointer);
            _scrollTargetId = LayoutHitTest.FindTopmostScrollContainer(
                elementsSlice,
                _childIndices,
                _rootIndices,
                _rootCount,
                pointer);

            if (_pointerDown && !_pointerWasDown && _hoveredId != 0)
            {
                _pressedId = _hoveredId;
                _pressedThisFrameId = _hoveredId;
            }

            if (!_pointerDown && _pointerWasDown)
            {
                if (_pressedId != 0)
                {
                    _releasedThisFrameId = _pressedId;
                }

                _pressedId = 0;
            }
        }

        static void ClampScroll(ref ScrollState state)
        {
            var maxX = math.max(0f, state.ContentSize.x - state.ViewportSize.x);
            var maxY = math.max(0f, state.ContentSize.y - state.ViewportSize.y);
            state.ScrollPosition = new float2(
                math.clamp(state.ScrollPosition.x, 0f, maxX),
                math.clamp(state.ScrollPosition.y, 0f, maxY));
        }

        void AppendCommand(RenderCommand command)
        {
            if (_commandCount >= _commands.Length)
            {
                throw new InvalidOperationException($"Exceeded maximum render command count ({_commands.Length}).");
            }

            _commands[_commandCount++] = command;
        }
    }
}
