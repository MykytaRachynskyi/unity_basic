# Basic.ImGui domain glossary

Ubiquitous language for the immediate-mode UI module. Implementation-free.

| Term | Definition |
| --- | --- |
| **ImGuiContext** | Per-session owner of frame lifecycle, arena memory, layout resolution, and pointer state. The deep module entry point for layout. |
| **RenderFrame** | Immutable snapshot of one frame's layout output: command buffer + layout dimensions. |
| **RenderCommand** | One drawable primitive emitted by layout (rectangle, text, scissor boundary). Ordered for correct z-order and clipping. |
| **RenderCommandBuffer** | Ordered list of render commands valid until the next `BeginLayout`. |
| **Arena** | Bump-allocated frame memory; reset each layout pass. No per-element GC. |
| **Declaration** | Immediate-mode phase where the host builds the element tree via scoped builders. |
| **ElementScope** | `ref struct` scope token from `Element()`; `using` disposes to close the element. Optional fluent config overrides only — not tree nesting. |
| **ElementDeclaration** | Blittable config passed when opening an element (layout, color, clip, scroll). |
| **Resolution** | Burst layout pass that sizes and positions elements and writes render commands. |
| **ElementId** | Stable hash identifying a logical UI element across frames (Clay-style). |
| **FontId / TextureId** | Renderer-owned handles; layout references assets by id only. |
| **IRenderBackend** | Swappable adapter that consumes a `RenderFrame` and submits GPU/canvas draws. |
| **BatchBuilder** | Renderer-internal step that merges render commands into few mesh draws. |
