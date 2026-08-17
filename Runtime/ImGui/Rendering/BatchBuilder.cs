using System;
using Basic.ImGui.Layout;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering
{
    public sealed class BatchBuilder : IDisposable
    {
        const int InitialCapacity = 256;

        NativeArray<Vector3> _positions;
        NativeArray<Color32> _colors;
        NativeArray<Vector4> _uv0;
        NativeArray<ushort> _indices;
        int _capacity;

        readonly ClipStack _clipStack = new ClipStack();
        BatchKey _currentBatchKey;
        bool _hasCurrentBatch;
        Vector2 _layoutDimensions;
        Rect _localRect;
        bool _mapToLocalRect;
        bool _flipY;

        public int CommandCount { get; private set; }
        public int VertexCount { get; private set; }
        public int IndexCount { get; private set; }
        public int TriangleCount => IndexCount / 3;
        public int BatchCount { get; private set; }
        public BatchKey LastBatchKey { get; private set; }

        public BatchBuilder()
        {
            EnsureCapacity(InitialCapacity);
        }

        public void Build(
            RenderCommandBuffer commands,
            FrameStringBuffer strings,
            IFontRegistry fonts,
            Material defaultMaterial,
            Vector2 layoutDimensions,
            bool flipY,
            Mesh mesh,
            Rect localRect = default,
            bool mapToLocalRect = false)
        {
            ResetStats();
            if (!commands.IsCreated || commands.Length <= 0)
            {
                ClearMesh(mesh);
                return;
            }

            _layoutDimensions = layoutDimensions;
            _localRect = localRect;
            _mapToLocalRect = mapToLocalRect;
            _flipY = flipY;

            CommandCount = commands.Length;
            var defaultMaterialId = defaultMaterial != null ? defaultMaterial.GetInstanceID() : 0;

            for (var i = 0; i < commands.Length; i++)
            {
                var command = commands.Commands[i];
                switch (command.CommandType)
                {
                    case RenderCommandType.ScissorStart:
                        _clipStack.Push(command.RenderData.Scissor.ClipRect);
                        break;
                    case RenderCommandType.ScissorEnd:
                        _clipStack.Pop();
                        break;
                    case RenderCommandType.Rectangle:
                        EmitRectangle(command, defaultMaterialId, fonts);
                        break;
                    case RenderCommandType.Text:
                        EmitText(command, strings, fonts);
                        break;
                }
            }

            UploadMesh(mesh);
        }

        public void PopulateVertexHelper(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (VertexCount <= 0 || IndexCount <= 0)
            {
                return;
            }

            for (var i = 0; i < VertexCount; i++)
            {
                vertexHelper.AddVert(_positions[i], _colors[i], _uv0[i]);
            }

            for (var i = 0; i < IndexCount; i += 3)
            {
                vertexHelper.AddTriangle(_indices[i], _indices[i + 1], _indices[i + 2]);
            }
        }

        public void Build(
            RenderCommandBuffer commands,
            FrameStringBuffer strings,
            IFontRegistry fonts,
            Material defaultMaterial,
            Vector2 layoutDimensions,
            bool flipY,
            Rect localRect,
            bool mapToLocalRect,
            VertexHelper vertexHelper)
        {
            Build(commands, strings, fonts, defaultMaterial, layoutDimensions, flipY, null, localRect, mapToLocalRect);
            PopulateVertexHelper(vertexHelper);
        }

        public void Dispose()
        {
            DisposeBuffer(ref _positions);
            DisposeBuffer(ref _colors);
            DisposeBuffer(ref _uv0);
            DisposeBuffer(ref _indices);
        }

        void ResetStats()
        {
            CommandCount = 0;
            VertexCount = 0;
            IndexCount = 0;
            BatchCount = 0;
            _hasCurrentBatch = false;
            _clipStack.Clear();
        }

        void EmitRectangle(RenderCommand command, int materialId, IFontRegistry fonts)
        {
            var textureId = 0;
            var scale = 1f;
            if (fonts != null && fonts.TryGetFont(FontId.Default, out var fontResources))
            {
                if (fontResources.Material != null)
                {
                    materialId = fontResources.Material.GetInstanceID();
                }

                if (fontResources.Atlas != null)
                {
                    textureId = fontResources.Atlas.GetInstanceID();
                }

                scale = ComputeTmpScale(fontResources, 16f);
            }

            TrackBatch(CreateBatchKey(materialId, textureId));

            var box = command.BoundingBox;
            var color = command.RenderData.Rectangle.Background;
            var y0 = LayoutToLocalY(box.Y);
            var y1 = LayoutToLocalY(box.Y + box.Height);

            var minY = Mathf.Min(y0, y1);
            var maxY = Mathf.Max(y0, y1);
            var bottomLeft = new Vector3(LayoutToLocalX(box.X), minY, 0f);
            var topLeft = new Vector3(LayoutToLocalX(box.X), maxY, 0f);
            var topRight = new Vector3(LayoutToLocalX(box.X + box.Width), maxY, 0f);
            var bottomRight = new Vector3(LayoutToLocalX(box.X + box.Width), minY, 0f);

            if (fonts != null && fonts.TryGetSolidFillUv(FontId.Default, out var uvMin, out var uvMax))
            {
                AddTextQuad(bottomLeft, topLeft, topRight, bottomRight, color, uvMin, uvMax, scale);
                return;
            }

            AddSolidQuad(bottomLeft, topLeft, topRight, bottomRight, color);
        }

        void EmitText(
            RenderCommand command,
            FrameStringBuffer strings,
            IFontRegistry fonts)
        {
            if (fonts == null)
            {
                return;
            }

            var textData = command.RenderData.Text;
            if (!fonts.TryGetFont(textData.Font, out var fontResources))
            {
                return;
            }

            var textureId = fontResources.Atlas != null ? fontResources.Atlas.GetInstanceID() : 0;
            var materialId = fontResources.Material != null ? fontResources.Material.GetInstanceID() : 0;
            TrackBatch(CreateBatchKey(materialId, textureId));

            var text = strings.GetSpan(textData.Text);
            var penX = command.BoundingBox.X;
            var baselineY = command.BoundingBox.Y + textData.FontSize;
            var scale = ComputeTmpScale(fontResources, textData.FontSize);

            for (var i = 0; i < text.Length; i++)
            {
                if (!fonts.TryGetGlyph(textData.Font, text[i], textData.FontSize, out var glyph) || !glyph.Found)
                {
                    penX += textData.FontSize * 0.5f;
                    continue;
                }

                var x0 = penX + glyph.BearingX;
                var x1 = x0 + glyph.Width;
                var yTop = baselineY - glyph.BearingY;
                var yBottom = yTop - glyph.Height;

                var top = LayoutToLocalY(yTop);
                var bottom = LayoutToLocalY(yBottom);
                var minY = Mathf.Min(top, bottom);
                var maxY = Mathf.Max(top, bottom);

                var uvMin = new Vector2(glyph.UvRect.x, glyph.UvRect.y);
                var uvMax = new Vector2(glyph.UvRect.z, glyph.UvRect.w);

                AddTextQuad(
                    new Vector3(LayoutToLocalX(x0), minY, 0f),
                    new Vector3(LayoutToLocalX(x0), maxY, 0f),
                    new Vector3(LayoutToLocalX(x1), maxY, 0f),
                    new Vector3(LayoutToLocalX(x1), minY, 0f),
                    textData.Color,
                    uvMin,
                    uvMax,
                    scale);

                penX += glyph.Advance;
            }
        }

        static float ComputeTmpScale(FontResources fontResources, float fontSize)
        {
            var pointSize = Mathf.Max(1f, fontResources.PointSize);
            var faceScale = fontResources.FontAsset != null ? fontResources.FontAsset.faceInfo.scale : 1f;
            return fontSize / pointSize * faceScale;
        }

        static Vector4 TmpUv(Vector2 uv, float scale) => new Vector4(uv.x, uv.y, 0f, scale);

        BatchKey CreateBatchKey(int materialId, int textureId)
        {
            if (_clipStack.TryGetCurrent(out var clip))
            {
                return new BatchKey(
                    materialId,
                    textureId,
                    new Vector4(clip.X, clip.Y, clip.X + clip.Width, clip.Y + clip.Height),
                    true);
            }

            return BatchKey.Solid(materialId);
        }

        void TrackBatch(BatchKey key)
        {
            if (!_hasCurrentBatch || !key.Equals(_currentBatchKey))
            {
                BatchCount++;
                _currentBatchKey = key;
                _hasCurrentBatch = true;
                LastBatchKey = key;
            }
        }

        void AddSolidQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Color32 color) =>
            WriteQuad(
                bottomLeft,
                topLeft,
                topRight,
                bottomRight,
                color,
                Vector4.zero,
                Vector4.zero,
                Vector4.zero,
                Vector4.zero);

        void AddTextQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Color32 color,
            Vector2 uvBottomLeft,
            Vector2 uvTopRight,
            float scale)
        {
            var uvTopLeft = new Vector2(uvBottomLeft.x, uvTopRight.y);
            var uvBottomRight = new Vector2(uvTopRight.x, uvBottomLeft.y);
            WriteQuad(
                bottomLeft,
                topLeft,
                topRight,
                bottomRight,
                color,
                TmpUv(uvBottomLeft, scale),
                TmpUv(uvTopLeft, scale),
                TmpUv(uvTopRight, scale),
                TmpUv(uvBottomRight, scale));
        }

        void WriteQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Color32 color,
            Vector4 uvBottomLeft,
            Vector4 uvTopLeft,
            Vector4 uvTopRight,
            Vector4 uvBottomRight)
        {
            EnsureCapacity(VertexCount + 4);
            var baseVertex = VertexCount;

            _positions[baseVertex + 0] = bottomLeft;
            _positions[baseVertex + 1] = topLeft;
            _positions[baseVertex + 2] = topRight;
            _positions[baseVertex + 3] = bottomRight;

            _colors[baseVertex + 0] = color;
            _colors[baseVertex + 1] = color;
            _colors[baseVertex + 2] = color;
            _colors[baseVertex + 3] = color;

            _uv0[baseVertex + 0] = uvBottomLeft;
            _uv0[baseVertex + 1] = uvTopLeft;
            _uv0[baseVertex + 2] = uvTopRight;
            _uv0[baseVertex + 3] = uvBottomRight;

            _indices[IndexCount + 0] = (ushort)(baseVertex + 0);
            _indices[IndexCount + 1] = (ushort)(baseVertex + 1);
            _indices[IndexCount + 2] = (ushort)(baseVertex + 2);
            _indices[IndexCount + 3] = (ushort)(baseVertex + 0);
            _indices[IndexCount + 4] = (ushort)(baseVertex + 2);
            _indices[IndexCount + 5] = (ushort)(baseVertex + 3);

            VertexCount += 4;
            IndexCount += 6;
        }

        static float TransformY(float y, float layoutHeight, bool flipY) => flipY ? layoutHeight - y : y;

        float LayoutToLocalX(float layoutX) =>
            _mapToLocalRect ? _localRect.xMin + layoutX : layoutX;

        float LayoutToLocalY(float layoutY)
        {
            var y = TransformY(layoutY, _layoutDimensions.y, _flipY);
            return _mapToLocalRect ? _localRect.yMax - y : y;
        }

        void UploadMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            if (VertexCount <= 0 || IndexCount <= 0)
            {
                ClearMesh(mesh);
                return;
            }

            mesh.SetVertices(_positions.GetSubArray(0, VertexCount));
            mesh.SetColors(_colors.GetSubArray(0, VertexCount));
            mesh.SetUVs(0, _uv0.GetSubArray(0, VertexCount));

            mesh.SetIndexBufferParams(IndexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(_indices, 0, 0, IndexCount, MeshUpdateFlags.DontRecalculateBounds);

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, IndexCount), MeshUpdateFlags.DontRecalculateBounds);
            mesh.RecalculateBounds();
        }

        static void ClearMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            mesh.Clear();
        }

        void EnsureCapacity(int requiredVertices)
        {
            if (requiredVertices <= _capacity)
            {
                return;
            }

            var newCapacity = Mathf.Max(requiredVertices, _capacity * 2);
            if (_capacity == 0)
            {
                newCapacity = Mathf.Max(requiredVertices, InitialCapacity);
            }

            ResizeBuffer(ref _positions, newCapacity);
            ResizeBuffer(ref _colors, newCapacity);
            ResizeBuffer(ref _uv0, newCapacity);
            ResizeBuffer(ref _indices, newCapacity * 6 / 4);
            _capacity = newCapacity;
        }

        static void ResizeBuffer<T>(ref NativeArray<T> buffer, int length) where T : struct
        {
            var next = new NativeArray<T>(length, Allocator.Persistent);
            if (buffer.IsCreated)
            {
                var copyLength = Mathf.Min(buffer.Length, length);
                NativeArray<T>.Copy(buffer, next, copyLength);
                buffer.Dispose();
            }

            buffer = next;
        }

        static void DisposeBuffer<T>(ref NativeArray<T> buffer) where T : struct
        {
            if (buffer.IsCreated)
            {
                buffer.Dispose();
                buffer = default;
            }
        }

        sealed class ClipStack
        {
            readonly BoundingBox[] _stack = new BoundingBox[32];
            int _depth;

            public void Clear() => _depth = 0;

            public void Push(BoundingBox clip)
            {
                if (_depth >= _stack.Length)
                {
                    return;
                }

                _stack[_depth++] = clip;
            }

            public void Pop()
            {
                if (_depth > 0)
                {
                    _depth--;
                }
            }

            public bool TryGetCurrent(out BoundingBox clip)
            {
                if (_depth <= 0)
                {
                    clip = default;
                    return false;
                }

                clip = _stack[_depth - 1];
                return true;
            }
        }
    }
}
