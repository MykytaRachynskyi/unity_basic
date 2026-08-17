using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Basic.ImGui.Layout
{
    public sealed class ImGuiContext : IDisposable
    {
        const int MaxNodes = 256;
        const int MaxCommands = 512;
        const int MaxOpenDepth = 64;

        readonly ITextMeasurer _textMeasurer;
        readonly LayoutArena _arena = new LayoutArena();
        readonly HashSet<uint> _usedIds = new HashSet<uint>();
        readonly List<string> _frameStrings = new List<string>();
        readonly int[] _openStack = new int[MaxOpenDepth];

        LayoutNode[] _nodes;
        NativeArray<RenderCommand> _commands;
        int _nodeCount;
        int _openDepth;
        int _commandCount;
        bool _layoutOpen;

        Vector2 _layoutDimensions;
        Vector2 _pointerPosition;
        bool _pointerDown;
        RenderFrame _lastFrame;

        [ThreadStatic]
        static ImGuiContext s_current;

        public ImGuiContext(ITextMeasurer textMeasurer)
        {
            _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
            _nodes = new LayoutNode[MaxNodes];
            _commands = new NativeArray<RenderCommand>(MaxCommands, Allocator.Persistent);
        }

        internal static ImGuiContext Current => s_current;

        public RenderFrame LastFrame => _lastFrame;

        public void SetLayoutDimensions(Vector2 size) => _layoutDimensions = size;

        public void SetPointerState(Vector2 position, bool isPointerDown)
        {
            _pointerPosition = position;
            _pointerDown = isPointerDown;
        }

        public void UpdateScrollContainers(bool drag, Vector2 wheel, float deltaTime)
        {
            // Phase 0: scroll state is deferred to Phase 1.
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
            _frameStrings.Clear();
            _nodeCount = 0;
            _openDepth = 0;
            _commandCount = 0;
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

            ResolveStubLayout();
            EmitRenderCommands();

            _lastFrame = new RenderFrame(
                new RenderCommandBuffer { Commands = _commands, Length = _commandCount },
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

            var textSliceIndex = AddFrameString(text);
            var metrics = default(TextMetrics);
            var slice = CreateTextSlice(textSliceIndex);
            _textMeasurer.Measure(slice, config.Font, config.FontSize, ref metrics);

            var nodeIndex = AddNode(id, LayoutNodeKind.Text, default);
            ref var node = ref _nodes[nodeIndex];
            node.TextConfig = config;
            node.TextSliceIndex = textSliceIndex;
            node.TextMetrics = metrics;

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
            id = default;
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

            _arena.Dispose();
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

        int AddFrameString(ReadOnlySpan<char> text)
        {
            _frameStrings.Add(text.ToString());
            return _frameStrings.Count - 1;
        }

        TextSlice CreateTextSlice(int stringIndex)
        {
            var value = _frameStrings[stringIndex];
            return new TextSlice(0, value.Length);
        }

        void ResolveStubLayout()
        {
            var contentX = 0f;
            var contentY = 0f;
            var contentWidth = _layoutDimensions.x;
            var contentHeight = _layoutDimensions.y;

            for (var i = 0; i < _nodeCount; i++)
            {
                ref var node = ref _nodes[i];
                if (node.ParentIndex >= 0)
                {
                    continue;
                }

                LayoutSubtree(i, contentX, contentY, contentWidth, contentHeight);
            }
        }

        void LayoutSubtree(int nodeIndex, float x, float y, float width, float height)
        {
            ref var node = ref _nodes[nodeIndex];
            node.Bounds = new BoundingBox(x, y, width, height);

            if (node.Kind == LayoutNodeKind.Text)
            {
                return;
            }

            var innerX = x + node.Declaration.PaddingLeft;
            var innerY = y + node.Declaration.PaddingTop;
            var innerWidth = Mathf.Max(0f, width - node.Declaration.PaddingLeft - node.Declaration.PaddingRight);
            var innerHeight = Mathf.Max(0f, height - node.Declaration.PaddingTop - node.Declaration.PaddingBottom);
            var cursorY = innerY;

            for (var childIndex = node.FirstChildIndex; childIndex >= 0; childIndex = _nodes[childIndex].NextSiblingIndex)
            {
                ref var child = ref _nodes[childIndex];
                if (child.Kind == LayoutNodeKind.Text)
                {
                    var childHeight = child.TextMetrics.Height > 0f ? child.TextMetrics.Height : child.TextConfig.FontSize;
                    var childWidth = child.TextMetrics.Width > 0f ? child.TextMetrics.Width : innerWidth;
                    LayoutSubtree(childIndex, innerX, cursorY, childWidth, childHeight);
                    cursorY += childHeight + node.Declaration.ChildGap;
                    continue;
                }

                var remainingHeight = Mathf.Max(0f, innerY + innerHeight - cursorY);
                LayoutSubtree(childIndex, innerX, cursorY, innerWidth, remainingHeight);
                cursorY += _nodes[childIndex].Bounds.Height + node.Declaration.ChildGap;
            }
        }

        void EmitRenderCommands()
        {
            _commandCount = 0;

            for (var i = 0; i < _nodeCount; i++)
            {
                ref var node = ref _nodes[i];

                if (node.Kind == LayoutNodeKind.Container && node.Declaration.BackgroundColor.a > 0)
                {
                    AppendCommand(new RenderCommand
                    {
                        BoundingBox = node.Bounds,
                        CommandType = RenderCommandType.Rectangle,
                        ElementId = node.Id.Id,
                        RenderData = new RenderData
                        {
                            Rectangle = new RectangleRenderData
                            {
                                Background = node.Declaration.BackgroundColor,
                                CornerRadius = node.Declaration.CornerRadius
                            }
                        }
                    });

                    if (node.Declaration.ClipChildren)
                    {
                        AppendCommand(new RenderCommand
                        {
                            BoundingBox = node.Bounds,
                            CommandType = RenderCommandType.ScissorStart,
                            ElementId = node.Id.Id,
                            RenderData = new RenderData
                            {
                                Scissor = new ScissorRenderData { ClipRect = node.Bounds }
                            }
                        });
                    }
                }

                if (node.Kind == LayoutNodeKind.Text)
                {
                    AppendCommand(new RenderCommand
                    {
                        BoundingBox = node.Bounds,
                        CommandType = RenderCommandType.Text,
                        ElementId = node.Id.Id,
                        RenderData = new RenderData
                        {
                            Text = new TextRenderData
                            {
                                Font = node.TextConfig.Font,
                                FontSize = node.TextConfig.FontSize,
                                Color = node.TextConfig.Color,
                                Text = CreateTextSlice(node.TextSliceIndex)
                            }
                        }
                    });
                }

                if (node.Kind == LayoutNodeKind.Container && node.Declaration.ClipChildren)
                {
                    AppendCommand(new RenderCommand
                    {
                        CommandType = RenderCommandType.ScissorEnd,
                        ElementId = node.Id.Id
                    });
                }
            }
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
