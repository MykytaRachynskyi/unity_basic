using System;
using Basic.ImGui.Layout;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Basic.ImGui.Rendering.Tests
{
    [TestFixture]
    public class BatchBuilderTests
    {
        BatchBuilder _batchBuilder;
        Mesh _mesh;
        Material _defaultMaterial;

        [SetUp]
        public void SetUp()
        {
            _batchBuilder = new BatchBuilder();
            _mesh = new Mesh { name = "BatchBuilderTestMesh" };
            _mesh.MarkDynamic();
            _defaultMaterial = new Material(Shader.Find("UI/Default"));
        }

        [TearDown]
        public void TearDown()
        {
            _batchBuilder.Dispose();
            UnityEngine.Object.DestroyImmediate(_mesh);
            UnityEngine.Object.DestroyImmediate(_defaultMaterial);
        }

        [Test]
        public void Build_EightThousandSameMaterialRects_ProducesSingleBatchAndExpectedCounts()
        {
            const int rectCount = 8000;
            var nativeCommands = CreateRectangleCommandsNative(rectCount, Color.red);
            try
            {
                var commands = new RenderCommandBuffer { Commands = nativeCommands, Length = rectCount };

                _batchBuilder.Build(
                    commands,
                    default,
                    null,
                    _defaultMaterial,
                    new Vector2(1920f, 1080f),
                    flipY: false,
                    _mesh);

                Assert.That(_batchBuilder.BatchCount, Is.EqualTo(1));
                Assert.That(_batchBuilder.VertexCount, Is.EqualTo(rectCount * 4));
                Assert.That(_batchBuilder.IndexCount, Is.EqualTo(rectCount * 6));
                Assert.That(_batchBuilder.TriangleCount, Is.EqualTo(rectCount * 2));
                Assert.That(_mesh.vertexCount, Is.EqualTo(rectCount * 4));
            }
            finally
            {
                nativeCommands.Dispose();
            }
        }

        [Test]
        public void Build_ScissorSplit_ProducesMultipleBatches()
        {
            var nativeCommands = new NativeArray<RenderCommand>(4, Allocator.Temp);
            try
            {
                nativeCommands[0] = ScissorStart(0f, 0f, 100f, 100f);
                nativeCommands[1] = Rectangle(10f, 10f, 20f, 20f, Color.blue);
                nativeCommands[2] = new RenderCommand { CommandType = RenderCommandType.ScissorEnd };
                nativeCommands[3] = Rectangle(40f, 40f, 20f, 20f, Color.green);

                var commands = new RenderCommandBuffer { Commands = nativeCommands, Length = 4 };

                _batchBuilder.Build(commands, default, null, _defaultMaterial, new Vector2(200f, 200f), false, _mesh);

                Assert.That(_batchBuilder.BatchCount, Is.EqualTo(2));
                Assert.That(_batchBuilder.VertexCount, Is.EqualTo(8));
                Assert.That(_batchBuilder.LastBatchKey.HasClip, Is.False);
            }
            finally
            {
                nativeCommands.Dispose();
            }
        }

        [Test]
        public void Build_TextAndRectangle_ProducesSeparateBatches()
        {
            var nativeCommands = new NativeArray<RenderCommand>(2, Allocator.Temp);
            try
            {
                nativeCommands[0] = Rectangle(0f, 0f, 50f, 50f, Color.white);
                nativeCommands[1] = TextCommand(60f, 0f, 40f, 20f, FontId.Default, 16f, new TextSlice(0, 1));

                var strings = CreateStringBuffer('A');
                var fonts = new FakeFontRegistry(_defaultMaterial);

                _batchBuilder.Build(
                    new RenderCommandBuffer { Commands = nativeCommands, Length = 2 },
                    strings,
                    fonts,
                    _defaultMaterial,
                    new Vector2(200f, 200f),
                    false,
                    _mesh);

                Assert.That(_batchBuilder.BatchCount, Is.EqualTo(2));
                Assert.That(_batchBuilder.VertexCount, Is.EqualTo(8));
            }
            finally
            {
                nativeCommands.Dispose();
            }
        }

        [Test]
        public void Build_FlipY_TransformsRectangleVertices()
        {
            var nativeCommands = CreateRectangleCommandsNative(1, Color.cyan);
            try
            {
                var commands = new RenderCommandBuffer { Commands = nativeCommands, Length = 1 };

                _batchBuilder.Build(commands, default, null, _defaultMaterial, new Vector2(100f, 100f), flipY: true, _mesh);

                var vertices = _mesh.vertices;
                Assert.That(vertices[0].y, Is.EqualTo(90f).Within(0.01f));
                Assert.That(vertices[2].y, Is.EqualTo(100f).Within(0.01f));
            }
            finally
            {
                nativeCommands.Dispose();
            }
        }

        static NativeArray<RenderCommand> CreateRectangleCommandsNative(int count, Color color)
        {
            var nativeCommands = new NativeArray<RenderCommand>(count, Allocator.Temp);
            for (var i = 0; i < count; i++)
            {
                nativeCommands[i] = Rectangle(i, i, 10f, 10f, color);
            }

            return nativeCommands;
        }

        static RenderCommand Rectangle(float x, float y, float width, float height, Color color) =>
            new RenderCommand
            {
                CommandType = RenderCommandType.Rectangle,
                BoundingBox = new BoundingBox(x, y, width, height),
                RenderData = new RenderData
                {
                    Rectangle = new RectangleRenderData
                    {
                        Background = color,
                        CornerRadius = Vector4.zero,
                    },
                },
            };

        static RenderCommand ScissorStart(float x, float y, float width, float height) =>
            new RenderCommand
            {
                CommandType = RenderCommandType.ScissorStart,
                RenderData = new RenderData
                {
                    Scissor = new ScissorRenderData { ClipRect = new BoundingBox(x, y, width, height) },
                },
            };

        static RenderCommand TextCommand(
            float x,
            float y,
            float width,
            float height,
            FontId font,
            float fontSize,
            TextSlice slice) =>
            new RenderCommand
            {
                CommandType = RenderCommandType.Text,
                BoundingBox = new BoundingBox(x, y, width, height),
                RenderData = new RenderData
                {
                    Text = new TextRenderData
                    {
                        Font = font,
                        FontSize = fontSize,
                        Color = Color.white,
                        Text = slice,
                    },
                },
            };

        static FrameStringBuffer CreateStringBuffer(char character)
        {
            var chars = new NativeArray<char>(1, Allocator.Temp);
            chars[0] = character;
            return new FrameStringBuffer { Chars = chars, Length = 1 };
        }

        sealed class FakeFontRegistry : IFontRegistry
        {
            readonly FontResources _resources;
            readonly FontGlyph _glyph;

            public FakeFontRegistry(Material material)
            {
                var atlas = Texture2D.whiteTexture;
                var fontMaterial = new Material(material);
                _resources = new FontResources
                {
                    Material = fontMaterial,
                    Atlas = atlas,
                    PointSize = 16f,
                };

                _glyph = new FontGlyph
                {
                    Found = true,
                    Advance = 8f,
                    BearingX = 0f,
                    BearingY = 12f,
                    Width = 8f,
                    Height = 12f,
                    UvRect = new Vector4(0f, 0f, 1f, 1f),
                };
            }

            public bool TryGetFont(FontId fontId, out FontResources resources)
            {
                resources = _resources;
                return true;
            }

            public bool TryGetGlyph(FontId fontId, char character, float fontSize, out FontGlyph glyph)
            {
                glyph = _glyph;
                return true;
            }

            public bool TryGetSolidFillUv(FontId fontId, out Vector2 uvMin, out Vector2 uvMax)
            {
                uvMin = Vector2.zero;
                uvMax = Vector2.one;
                return true;
            }
        }
    }
}
