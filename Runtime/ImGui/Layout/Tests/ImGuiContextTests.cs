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
            using var context = new ImGuiContext(new FakeTextMeasurer(48f, 16f));
            context.SetLayoutDimensions(new Vector2(320f, 240f));
            context.SetPointerState(Vector2.zero, false);
            context.UpdateScrollContainers(false, Vector2.zero, 0.016f);
            context.BeginLayout();

            using (context.Element(ElementId.From("Panel"), ElementPresets.Panel))
            {
                context.Text(ElementId.From("Title"), "Hello", TextPresets.Body);
            }

            var frame = context.EndLayout(0.016f);
            var commands = frame.Commands;

            Assert.That(commands.IsCreated, Is.True);
            Assert.That(commands.Length, Is.EqualTo(2));
            Assert.That(commands.Commands[0].CommandType, Is.EqualTo(RenderCommandType.Rectangle));
            Assert.That(commands.Commands[0].ElementId, Is.EqualTo(ElementId.From("Panel").Id));
            Assert.That(commands.Commands[1].CommandType, Is.EqualTo(RenderCommandType.Text));
            Assert.That(commands.Commands[1].ElementId, Is.EqualTo(ElementId.From("Title").Id));
            Assert.That(frame.LayoutDimensions, Is.EqualTo(new Vector2(320f, 240f)));
        }

        sealed class FakeTextMeasurer : ITextMeasurer
        {
            readonly float _width;
            readonly float _height;

            public FakeTextMeasurer(float width, float height)
            {
                _width = width;
                _height = height;
            }

            public void Measure(TextSlice text, FontId font, float fontSize, ref TextMetrics metrics)
            {
                metrics.Width = _width;
                metrics.Height = _height;
                metrics.LineCount = 1;
            }
        }
    }
}
