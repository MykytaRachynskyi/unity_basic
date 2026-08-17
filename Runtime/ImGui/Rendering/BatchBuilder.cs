using System;
using Basic.ImGui.Layout;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Basic.ImGui.Rendering
{
    public sealed class BatchBuilder : IDisposable
    {
        const int InitialCapacity = 256;

        NativeArray<Vector3> _positions;
        NativeArray<Color32> _colors;
        NativeArray<Vector2> _uv0;
        NativeArray<ushort> _indices;
        int _capacity;

        readonly ClipStack _clipStack = new ClipStack();
        BatchKey _currentBatchKey;
        bool _hasCurrentBatch;

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
            Mesh mesh)
        {
            ResetStats();
            if (!commands.IsCreated || commands.Length <= 0)
            {
                ClearMesh(mesh);
                return;
            }

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
                        EmitRectangle(command, defaultMaterialId, layoutDimensions, flipY);
                        break;
                    case RenderCommandType.Text:
                        EmitText(command, strings, fonts, layoutDimensions, flipY);
                        break;
                }
            }

            UploadMesh(mesh);
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

        void EmitRectangle(RenderCommand command, int materialId, Vector2 layoutDimensions, bool flipY)
        {
            var key = CreateBatchKey(materialId, 0);
            TrackBatch(key);

            var box = command.BoundingBox;
            var color = command.RenderData.Rectangle.Background;
            var y0 = TransformY(box.Y, layoutDimensions.y, flipY);
            var y1 = TransformY(box.Y + box.Height, layoutDimensions.y, flipY);

            var minY = Mathf.Min(y0, y1);
            var maxY = Mathf.Max(y0, y1);
            AddSolidQuad(
                new Vector3(box.X, minY, 0f),
                new Vector3(box.X, maxY, 0f),
                new Vector3(box.X + box.Width, maxY, 0f),
                new Vector3(box.X + box.Width, minY, 0f),
                color);
        }

        void EmitText(
            RenderCommand command,
            FrameStringBuffer strings,
            IFontRegistry fonts,
            Vector2 layoutDimensions,
            bool flipY)
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

                var top = TransformY(yTop, layoutDimensions.y, flipY);
                var bottom = TransformY(yBottom, layoutDimensions.y, flipY);
                var minY = Mathf.Min(top, bottom);
                var maxY = Mathf.Max(top, bottom);

                var uvMin = new Vector2(glyph.UvRect.x, glyph.UvRect.y);
                var uvMax = new Vector2(glyph.UvRect.z, glyph.UvRect.w);

                AddTextQuad(
                    new Vector3(x0, minY, 0f),
                    new Vector3(x0, maxY, 0f),
                    new Vector3(x1, maxY, 0f),
                    new Vector3(x1, minY, 0f),
                    textData.Color,
                    uvMin,
                    uvMax);

                penX += glyph.Advance;
            }
        }

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
            WriteQuad(bottomLeft, topLeft, topRight, bottomRight, color, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

        void AddTextQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Color32 color,
            Vector2 uvBottomLeft,
            Vector2 uvTopRight)
        {
            var uvTopLeft = new Vector2(uvBottomLeft.x, uvTopRight.y);
            var uvBottomRight = new Vector2(uvTopRight.x, uvBottomLeft.y);
            WriteQuad(bottomLeft, topLeft, topRight, bottomRight, color, uvBottomLeft, uvTopLeft, uvTopRight, uvBottomRight);
        }

        void WriteQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Color32 color,
            Vector2 uvBottomLeft,
            Vector2 uvTopLeft,
            Vector2 uvTopRight,
            Vector2 uvBottomRight)
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

        void UploadMesh(Mesh mesh)
        {
            if (VertexCount <= 0 || IndexCount <= 0)
            {
                ClearMesh(mesh);
                return;
            }

            mesh.SetVertexBufferParams(
                VertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, stream: 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream: 2));

            mesh.SetVertexBufferData(_positions, 0, 0, VertexCount, 0, MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetVertexBufferData(_colors, 0, 0, VertexCount, 1, MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetVertexBufferData(_uv0, 0, 0, VertexCount, 2, MeshUpdateFlags.DontRecalculateBounds);

            mesh.SetIndexBufferParams(IndexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(_indices, 0, 0, IndexCount, MeshUpdateFlags.DontRecalculateBounds);

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, IndexCount), MeshUpdateFlags.DontRecalculateBounds);
            mesh.RecalculateBounds();
        }

        static void ClearMesh(Mesh mesh)
        {
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
