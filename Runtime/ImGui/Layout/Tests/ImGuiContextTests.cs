using System;
using Basic.ImGui.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Basic.ImGui.Layout.Tests
{
    [TestFixture]
    public class ImGuiContextTests
    {
        [Test]
        public void EndLayout_EmitsRectangleAndTextCommands_ForPanelWithTextChild()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            BuildPanelWithTitle(context, out var panelId, out var titleId);

            var frame = context.EndLayout(0.016f);
            var commands = frame.Commands;

            Assert.That(commands.Length, Is.EqualTo(2));
            Assert.That(commands.Commands[0].CommandType, Is.EqualTo(RenderCommandType.Rectangle));
            Assert.That(commands.Commands[0].ElementId, Is.EqualTo(panelId.Id));
            Assert.That(commands.Commands[0].BoundingBox.X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(commands.Commands[0].BoundingBox.Y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(commands.Commands[0].BoundingBox.Width, Is.EqualTo(320f).Within(0.01f));
            Assert.That(commands.Commands[0].BoundingBox.Height, Is.EqualTo(240f).Within(0.01f));

            Assert.That(commands.Commands[1].CommandType, Is.EqualTo(RenderCommandType.Text));
            Assert.That(commands.Commands[1].ElementId, Is.EqualTo(titleId.Id));
            Assert.That(commands.Commands[1].BoundingBox.X, Is.EqualTo(8f).Within(0.01f));
            Assert.That(commands.Commands[1].BoundingBox.Y, Is.EqualTo(8f).Within(0.01f));
            Assert.That(commands.Commands[1].BoundingBox.Width, Is.EqualTo(50f).Within(0.01f));
            Assert.That(frame.LayoutDimensions, Is.EqualTo(new Vector2(320f, 240f)));
        }

        [Test]
        public void EndLayout_EmitsScissorCommands_ForScrollContainer()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            context.SetLayoutDimensions(new Vector2(200f, 100f));
            context.SetPointerState(Vector2.zero, false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Scroll"), ElementPresets.ScrollVertical))
            {
                context.Text(ElementId.From("Line0"), "0123456789", TextPresets.Body);
                context.Text(ElementId.From("Line1"), "0123456789", TextPresets.Body);
                context.Text(ElementId.From("Line2"), "0123456789", TextPresets.Body);
            }

            var frame = context.EndLayout(0.016f);
            var commands = frame.Commands;
            var types = new RenderCommandType[commands.Length];
            for (var i = 0; i < commands.Length; i++)
            {
                types[i] = commands.Commands[i].CommandType;
            }

            Assert.That(types, Does.Contain(RenderCommandType.ScissorStart));
            Assert.That(types, Does.Contain(RenderCommandType.ScissorEnd));
        }

        [Test]
        public void EndLayout_AppliesScrollOffset_ToChildPositions()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            context.SetLayoutDimensions(new Vector2(200f, 60f));
            context.SetPointerState(new Vector2(20f, 20f), false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Scroll"), ElementPresets.ScrollVertical))
            {
                for (var i = 0; i < 5; i++)
                {
                    context.Text(ElementId.Indexed("Line", (uint)i), "0123456789", TextPresets.Body);
                }
            }

            context.EndLayout(0.016f);

            context.SetPointerState(new Vector2(20f, 20f), false);
            context.UpdateScrollContainers(false, new Vector2(0f, 1f), 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Scroll"), ElementPresets.ScrollVertical))
            {
                for (var i = 0; i < 5; i++)
                {
                    context.Text(ElementId.Indexed("Line", (uint)i), "0123456789", TextPresets.Body);
                }
            }

            var frame = context.EndLayout(0.016f);
            var firstLineY = FindTextCommand(frame.Commands, ElementId.Indexed("Line", 0).Id).BoundingBox.Y;
            var thirdLineY = FindTextCommand(frame.Commands, ElementId.Indexed("Line", 2).Id).BoundingBox.Y;

            Assert.That(firstLineY, Is.LessThan(4f));
            Assert.That(thirdLineY - firstLineY, Is.EqualTo(37.6f).Within(1f));
            Assert.That(context.TryGetScrollOffset(ElementId.From("Scroll"), out var offset), Is.True);
            Assert.That(offset.y, Is.GreaterThan(0f));
        }

        [Test]
        public void EndLayout_WrapsText_ToMultipleLines()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            context.SetLayoutDimensions(new Vector2(160f, 120f));
            context.SetPointerState(Vector2.zero, false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Panel"), ElementPresets.Panel))
            {
                var wrapConfig = TextPresets.Body;
                wrapConfig.Wrap = true;
                context.Text(ElementId.From("Body"), "one two three four", wrapConfig);
            }

            var frame = context.EndLayout(0.016f);
            var textCommand = FindTextCommand(frame.Commands, ElementId.From("Body").Id);

            Assert.That(textCommand.BoundingBox.Width, Is.LessThanOrEqualTo(144f));
            Assert.That(textCommand.BoundingBox.Height, Is.GreaterThan(20f));
        }

        [Test]
        public void EndLayout_ReportsHoveredAndPressedIds()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            var buttonId = ElementId.From("Button");

            context.SetLayoutDimensions(new Vector2(200f, 100f));
            context.SetPointerState(new Vector2(30f, 30f), false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();
            using (context.Element(buttonId, ElementPresets.Panel))
            {
            }

            context.EndLayout(0.016f);

            context.SetPointerState(new Vector2(30f, 30f), true);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();
            using (context.Element(buttonId, ElementPresets.Panel))
            {
            }

            var frame = context.EndLayout(0.016f);

            Assert.That(context.TryGetHoveredId(out var hovered), Is.True);
            Assert.That(hovered, Is.EqualTo(buttonId));
            Assert.That(context.WasPressedThisFrame(buttonId), Is.True);
            Assert.That(context.IsPressed(buttonId), Is.True);

            context.SetPointerState(new Vector2(30f, 30f), false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();
            using (context.Element(buttonId, ElementPresets.Panel))
            {
            }

            context.EndLayout(0.016f);
            Assert.That(context.WasReleasedThisFrame(buttonId), Is.True);
        }

        [Test]
        public void OnHover_UsesPreviousFrameBounds()
        {
            using var context = new ImGuiContext(new FakeTextMeasurer(10f));
            var buttonId = ElementId.From("Button");
            var hovered = false;

            context.SetLayoutDimensions(new Vector2(200f, 100f));
            context.SetPointerState(new Vector2(30f, 30f), false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();
            using (context.Element(buttonId, ElementPresets.Panel))
            {
            }

            context.EndLayout(0.016f);

            context.SetPointerState(new Vector2(30f, 30f), false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();
            using (context.Element(buttonId, ElementPresets.Panel.OnHover((id, data) => hovered = true)))
            {
            }

            context.EndLayout(0.016f);
            Assert.That(hovered, Is.True);
        }

        static void BuildPanelWithTitle(ImGuiContext context, out ElementId panelId, out ElementId titleId)
        {
            panelId = ElementId.From("Panel");
            titleId = ElementId.From("Title");
            context.SetLayoutDimensions(new Vector2(320f, 240f));
            context.SetPointerState(Vector2.zero, false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(panelId, ElementPresets.Panel))
            {
                context.Text(titleId, "Hello", TextPresets.Body);
            }
        }

        static RenderCommand FindTextCommand(RenderCommandBuffer commands, uint elementId)
        {
            for (var i = 0; i < commands.Length; i++)
            {
                var command = commands.Commands[i];
                if (command.CommandType == RenderCommandType.Text && command.ElementId == elementId)
                {
                    return command;
                }
            }

            throw new InvalidOperationException($"Text command for element {elementId} was not found.");
        }

        sealed class FakeTextMeasurer : ITextMeasurer
        {
            readonly float _charWidth;

            public FakeTextMeasurer(float charWidth) => _charWidth = charWidth;

            public void Measure(ReadOnlySpan<char> text, FontId font, float fontSize, float letterSpacing, ref TextMetrics metrics)
            {
                metrics.Width = MeasureSpan(text, letterSpacing);
                metrics.Height = fontSize * 1.2f;
                metrics.LineCount = 1;
            }

            public float MeasureWord(ReadOnlySpan<char> word, FontId font, float fontSize, float letterSpacing) =>
                MeasureSpan(word, letterSpacing);

            float MeasureSpan(ReadOnlySpan<char> text, float letterSpacing)
            {
                if (text.Length == 0)
                {
                    return 0f;
                }

                return text.Length * _charWidth + Mathf.Max(0, text.Length - 1) * letterSpacing;
            }
        }
    }
}
