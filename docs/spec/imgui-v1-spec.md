# Basic.ImGui — v1 system specification

Compiled destination artifact for [Map: Clay-inspired immediate-mode UI for Unity](https://github.com/MykytaRachynskyi/unity_basic/issues/2).  
Synthesizes all wayfinder decisions through [#15](https://github.com/MykytaRachynskyi/unity_basic/issues/15).

**Package:** `com.arcticlime.unitybasic` · **Module:** `Basic.ImGui` · **Unity:** 6000.x · **URP:** 17

---

## 1. Purpose

`Basic.ImGui` is a **Clay-faithful immediate-mode UI system** for Unity runtime game UI:

- Declare the full UI tree **every frame** with scoped C# builders.
- Layout resolves into a **render command buffer** backed by arena memory.
- A **renderer-agnostic backend** batches commands into few GPU/canvas draws.
- Composes with **uGUI** via Screen Space–Camera sorting layers.

v1 ships a **debug HUD vertical slice** that proves the pipeline end-to-end and self-certifies perf gates.

---

## 2. Design principles

| Principle | v1 choice |
| --- | --- |
| Immediate mode | Full tree re-declared each frame; no retained widget graph |
| Clay fidelity | Frame lifecycle, element IDs, flex sizing, pointer/scroll API mirror Clay |
| Idiomatic C# | `ref struct` scopes, `using` blocks, plain `for`/`if` inside declaration |
| DOTS-y memory | Burst + Collections + Mathematics; arena bump; native command output |
| No ECS | Entities package not used anywhere in this module |
| Deep modules | Layout core ↔ renderer split at `RenderFrame` / `IRenderBackend` |
| Perf parity | Layout throughput, zero GC, and batching efficiency are equal priorities |

---

## 3. Architecture

### 3.1 Module layout

```
Runtime/ImGui/
├── Layout/          Basic.ImGui.Layout
│   ImGuiContext, declaration API, arena, Burst resolve job, pointer/scroll
└── Rendering/       Basic.ImGui.Rendering
    IRenderBackend, CanvasRenderBackend, BatchBuilder, asset registries
```

```
┌─────────────────────────┐     RenderFrame      ┌────────────────────────────┐
│ ImGuiContext (Layout)   │ ───────────────────▶ │ IRenderBackend (Rendering) │
│  declaration + resolve  │                      │  CanvasRenderBackend (v1)  │
└─────────────────────────┘                      └─────────────┬──────────────┘
                                                               │
                                                               ▼
                                                    ImGuiGraphic + BatchBuilder
                                                               │
                                                               ▼
                                                    uGUI Canvas sort order
```

**Swappable renderer:** `IRenderBackend` is the only cross-module contract. v1 ships `CanvasRenderBackend` only. `OverlayCameraRenderBackend` (RendererFeature) is a documented fallback if canvas integration fails in practice — not needed for performance ([#12](https://github.com/MykytaRachynskyi/unity_basic/issues/12)).

### 3.2 Frame pipeline

Host (`ImGuiHost` or game code) orchestrates each frame:

| Step | API | Thread |
| --- | --- | --- |
| 1 | `SetLayoutDimensions(size)` | Main |
| 2 | `SetPointerState(pos, down)` | Main |
| 3 | `UpdateScrollContainers(drag, wheel, dt)` | Main |
| 4 | `BeginLayout()` — arena reset | Main |
| 5 | Declare tree — `Element()`, `Text()` | Main |
| 6 | `EndLayout(dt)` — Burst resolve → `RenderCommandBuffer` | Main (sync `Complete`) |
| 7 | `IRenderBackend.Draw(frame)` | Main |

`ImGuiContext` does **not** call the renderer — keeps layout testable without GPU.

### 3.3 Layout internals

Two phases (Clay parity):

1. **Declaration** — managed tree builder on main thread; arena bump; text measured via `ITextMeasurer`.
2. **Resolution** — `LayoutResolveJob` (Burst) over flat struct arrays; writes `RenderCommandBuffer`.

Flat arrays encode hierarchy (element indices, child lists, open stack). No pointer trees in the hot path. See [burst-flex-layout research](../research/burst-flex-layout.md).

### 3.4 Rendering internals

`BatchBuilder` consumes `RenderCommandBuffer`, groups by batch key (shader, texture, clip rect, blend), builds one or few dynamic meshes, uploads via **`Mesh.SetVertexBufferData`** on `MarkDynamic` mesh — **not** per-quad `VertexHelper` adds or `mesh.vertices` assignment ([#12](../research/canvas-batching-prototype.md)).

Text renders as **SDF atlas glyph quads** batched with rects. Atlases from TMP Font Asset Creator; no `TMP_Text` per line.

### 3.5 uGUI compositing

Primary: **Screen Space–Camera** canvas with sorting layer/order interleaving ImGui with sibling uGUI. `ImGuiGraphic` (`MaskableGraphic`) submits batched mesh via `CanvasRenderer`.

Fallback (deferred): separate overlay camera + RendererFeature if canvas path fails sorting/masking edge cases.

---

## 4. Public API

### 4.1 Declaration (scoped builders)

```csharp
using (var panel = ctx.Element(ElementId.From("Panel"), ElementPresets.Panel))
{
    panel.Padding(8).ChildGap(4);
    ctx.Text(ElementId.From("Title"), "Stats", TextPresets.Heading);
}
```

| Type | Role |
| --- | --- |
| `ImGuiContext` | Frame lifecycle + `Element()` / `Text()` |
| `ElementScope` | `ref struct`; `using` → `CloseElement` |
| `ElementDeclaration` | Blittable open config (layout, color, clip, scroll) |
| `TextConfig` | Font, size, color, wrap, letter spacing |
| `ElementId` | Stable hashed id (`From`, `Indexed`, `Local`, `Auto`) |

**Rejected in v1:** fluent tree nesting, source generators, public manual open/close without `using`.

Full API detail: [imgui-declaration-api.md](imgui-declaration-api.md).

### 4.2 Pointer and interaction

- `SetPointerState` / `UpdateScrollContainers` before layout.
- `OnHover` during declaration; post-layout `TryGetHoveredId`, press/release queries.
- Primary mouse/touch only; one-frame hit-test lag accepted.
- Scroll: wheel + drag-while-pointer-down.

### 4.3 Renderer seam

```csharp
public interface IRenderBackend
{
    void Draw(RenderFrame frame, RenderBackendContext context);
}
```

Layout output:

```csharp
public readonly struct RenderFrame
{
    public RenderCommandBuffer Commands;
    public Vector2 LayoutDimensions;
}
```

Cross-seam asset handles: `FontId`, `TextureId` — layout stores ids; renderer resolves to materials/textures/glyph UVs.

Full boundary detail: [imgui-layout-renderer-boundary.md](imgui-layout-renderer-boundary.md).

---

## 5. v1 feature surface

### 5.1 Layout capabilities

| In v1 | Deferred |
| --- | --- |
| Sizing: FIT / GROW / FIXED / PERCENT | Border, image, floating, aspect-ratio |
| Direction, padding, childGap, childAlignment | Transition API (stub/no-op only) |
| Scroll containers (wheel + drag) | Custom element data |
| Text: wrap, measure cache, letterSpacing | Clay debug inspector |
| backgroundColor, cornerRadius, clip (scissor) | |
| Visibility culling (default on) | |

### 5.2 Render commands

| In v1 | Deferred |
| --- | --- |
| `Rectangle` (fill + corner radius) | `Border`, `Image` |
| `Text` (SDF glyph quads) | `OverlayColor`, `Custom` |
| `ScissorStart` / `ScissorEnd` | |

### 5.3 Dependencies

| Package | v1 |
| --- | --- |
| `com.unity.burst` | Yes |
| `com.unity.collections` | Yes |
| `com.unity.mathematics` | Yes |
| `com.unity.entities` | **No** |

---

## 6. Performance targets

Measured in **Editor play mode**, 1080p canvas, **60-frame warmup**:

| Gate | Threshold | Notes |
| --- | --- | --- |
| Element capacity | 8k elements | Stress panel without arena overflow |
| Frame time | ≤ **2 ms** | Combined layout + render |
| GC | **0 B / frame** | Hot path; `SetVertexBufferData` upload |
| Draw calls | ≤ **16** | Same-material 8k-rect stress; expect **1** with batching |

Validated render CPU: **~0.35 ms** for 8k batched rects on canvas path ([#12](../research/canvas-batching-prototype.md)).

---

## 7. Vertical slice — debug HUD

v1 is **shippable** when a play-mode demo includes:

1. **Live stats** — element count, layout+render ms, batch/draw count.
2. **Interactive panel** — buttons (hover/press), scrollable list (layout + input + text).
3. **Stress benchmark** — 8k synthetic elements with green/red gate indicators.
4. **uGUI compositing proof** — ImGui canvas between sibling uGUI panels on different sort orders.

---

## 8. Acceptance criteria

| Tier | Type | Scope |
| --- | --- | --- |
| 1 | EditMode | `ImGuiContext` + fake `ITextMeasurer`; assert commands + pointer — no GPU |
| 2 | PlayMode smoke | HUD loads; buttons + scroll work |
| 3 | HUD benchmark | 8k stress; self-certifying perf gates |
| 4 | Manual | uGUI layering + Frame Debugger draw count |

Full contract: [imgui-v1-feature-contract.md](imgui-v1-feature-contract.md).

---

## 9. Phased roadmap

### Phase 0 — Foundation (current next step)

Implement module skeleton and seam types with no GPU dependency:

- `ImGuiContext` frame lifecycle stub
- `ElementId`, `ElementDeclaration`, `ElementScope`, `Text()`
- Arena allocator + flat layout element arrays
- `RenderCommand` / `RenderCommandBuffer` types
- `IRenderBackend` interface + empty `CanvasRenderBackend`
- EditMode tests for declaration → command output (synthetic measurer)

**Exit:** Tier-1 tests pass; project compiles with new asmdef/deps.

### Phase 1 — Layout core

- Managed declaration tree builder
- `LayoutResolveJob` (Burst): sizing, grow/shrink, padding/gap, text wrap pass, positioning
- Scroll state (persistent native map)
- Pointer hit-test + hover/press queries
- `ITextMeasurer` implementation (TextCore/TMP atlas lookup)

**Exit:** EditMode tests assert layout geometry and commands for panels, scroll, text wrap.

### Phase 2 — Renderer

- `BatchBuilder` with `SetVertexBufferData` zero-GC upload
- `ImGuiGraphic` + `CanvasRenderBackend`
- SDF text glyph quads in batch pipeline
- Scissor → batch splits or shader clip rects
- Font/texture registries

**Exit:** PlayMode smoke (Tier 2); Frame Debugger shows ≤16 draws at 8k rects.

### Phase 3 — Debug HUD + benchmark

- `ImGuiHost` MonoBehaviour wiring
- Full HUD demo scene per §7
- Self-certifying perf panel (Tier 3)
- uGUI compositing scene (Tier 4 checklist)

**Exit:** All perf gates green; manual compositing checklist signed off.

### Phase 4 — v1.1 backlog (post-ship)

| Item | Notes |
| --- | --- |
| Border + image commands | After core loop green |
| Floating elements, aspect-ratio | Clay parity |
| Transition API | Clay Transition parity |
| Localization | Integrate `unity_basic` localization |
| Source generators | Only if ergonomics gap remains |
| `OverlayCameraRenderBackend` | Only if canvas fails in production |
| Async layout / double-buffered arena | Profiling-driven |
| GPU instancing for solid rects | If mesh CPU becomes bottleneck |

---

## 10. Out of scope (entire effort)

- ECS / Unity Entities
- UI Toolkit support or migration
- Replacing all uGUI project-wide
- Editor-only tooling in v1

---

## 11. Source documents

| Document | Role |
| --- | --- |
| [imgui-v1-spec.md](imgui-v1-spec.md) | **This file** — compiled overview + roadmap |
| [imgui-declaration-api.md](imgui-declaration-api.md) | Declaration API detail |
| [imgui-layout-renderer-boundary.md](imgui-layout-renderer-boundary.md) | Layout ↔ renderer seam |
| [imgui-v1-feature-contract.md](imgui-v1-feature-contract.md) | Feature matrix + acceptance |
| [clay-architecture.md](../research/clay-architecture.md) | Clay reference model |
| [burst-flex-layout.md](../research/burst-flex-layout.md) | Burst layout feasibility |
| [urp-ui-rendering.md](../research/urp-ui-rendering.md) | URP rendering research |
| [canvas-batching-prototype.md](../research/canvas-batching-prototype.md) | Canvas batching validation |
| [CONTEXT.md](../../CONTEXT.md) | Domain glossary |

---

*Compiled: 2026-08-17. Map decisions #3–#13.*
