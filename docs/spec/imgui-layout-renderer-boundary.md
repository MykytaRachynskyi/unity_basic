# Basic.ImGui — layout core ↔ renderer boundary

Decision for [wayfinder #8](https://github.com/MykytaRachynskyi/unity_basic/issues/8).  
Inputs: [clay-architecture](research/clay-architecture.md), [burst-flex-layout](research/burst-flex-layout.md), [urp-ui-rendering](research/urp-ui-rendering.md), map decisions on naming, uGUI layering, and no-ECS stack.

---

## Module split

Two **deep modules** at one seam. Layout knows nothing about Unity rendering; rendering knows nothing about flex math.

```
Basic.ImGui.Layout          Basic.ImGui.Rendering
┌─────────────────────┐     ┌──────────────────────────────┐
│ ImGuiContext        │     │ IRenderBackend (interface)   │
│  small public API   │────▶│  CanvasRenderBackend (v1)    │
│  arena + Burst job  │     │   └ ImGuiGraphic + BatchBuilder│
└─────────────────────┘     └──────────────────────────────┘
         │                              │
         │ RenderFrame                  │ Mesh / CanvasRenderer
         ▼                              ▼
   RenderCommandBuffer            uGUI sort order
```

| Module | Namespace | Folder | Owns |
| --- | --- | --- | --- |
| Layout core | `Basic.ImGui.Layout` | `Runtime/ImGui/Layout/` | Frame lifecycle, tree declaration, arena, Burst resolve job, pointer/scroll state, render-command **production** |
| Renderer | `Basic.ImGui.Rendering` | `Runtime/ImGui/Rendering/` | Asset registries (fonts/textures), batch builder, mesh upload, canvas submission |

**Swappable renderer:** `IRenderBackend` is the only cross-module contract. v1 ships `CanvasRenderBackend`; `OverlayCameraRenderBackend` (RendererFeature fallback from uGUI layering decision) is a second adapter if canvas path fails — not v1 default.

---

## Frame lifecycle

Owned entirely by **`ImGuiContext`** (single deep module entry point). Mirrors Clay order; host (`ImGuiHost` MonoBehaviour or game code) calls each step per frame.

| Step | Method | Thread | Notes |
| --- | --- | --- | --- |
| 1 | `SetLayoutDimensions(Vector2 size)` | Main | Root viewport in layout space (canvas pixel size) |
| 2 | `SetPointerState(Vector2 pos, bool down)` | Main | Before layout; drives hover/click |
| 3 | `UpdateScrollContainers(bool drag, Vector2 wheel, float dt)` | Main | Updates native `ScrollState` buffers |
| 4 | `BeginLayout()` | Main | Arena bump reset; open root container |
| 5 | Declare tree | Main | Scoped builders / `Element()`, `Text()` |
| 6 | `EndLayout(float deltaTime)` | Main | Schedules + completes `LayoutResolveJob`; fills `RenderCommandBuffer` |
| 7 | `IRenderBackend.Draw(RenderFrame frame)` | Main | Renderer consumes commands (same frame, after step 6) |

`ImGuiContext` does **not** call the renderer — the host or `ImGuiHost` orchestrates step 7. Keeps layout testable without a GPU.

**v1:** `EndLayout` uses synchronous `JobHandle.Complete()` inside the call. Async/double-buffering is a later optimization.

---

## Memory ownership

| Memory | Owner | Lifetime | Allocator |
| --- | --- | --- | --- |
| Arena bump buffer | `ImGuiContext` | Persistent; reset each `BeginLayout` | Pre-sized `NativeArray<byte>` or `UnsafeList` (Collections) |
| Layout elements, tree links, open stack | Layout | Frame (arena) | Bump |
| `ScrollState` | `ImGuiContext` | Persistent across frames | Native fixed map keyed by element id |
| `RenderCommand` output | Layout | Frame (arena tail or dedicated native slice) | Bump or append into arena |
| Text word cache / metrics | Layout + `ITextMeasurer` | Persistent + frame writes | Native hash map + arena |
| Mesh vertex/index buffers | `BatchBuilder` | Persistent; resized as needed | `Mesh.MarkDynamic` + `NativeArray` scratch |
| Font/texture GPU assets | `Rendering.AssetRegistry` | Persistent | Unity assets |

**Rule:** nothing crossing the seam holds managed references to Unity objects except the renderer module. Layout stores **handles** (`FontId`, `TextureId` as `uint` or `int`).

---

## Seam type: `RenderFrame`

Layout output is a **value-type snapshot** per frame — not an interface, not a live view into mutating state.

```csharp
public readonly struct RenderFrame
{
    public RenderCommandBuffer Commands;
    public Vector2 LayoutDimensions;
    // Pointer queries stay on ImGuiContext, not on RenderFrame
}

public readonly struct RenderCommandBuffer
{
    public NativeArray<RenderCommand> Commands; // valid until next BeginLayout
    public int Length;
}
```

Callers and `IRenderBackend` must consume commands before the next `BeginLayout`.

---

## `RenderCommand` (Clay-aligned, blittable)

Mirrors [Clay `Clay_RenderCommand`](https://github.com/nicbarker/clay/blob/main/clay.h). All fields blittable for Burst output and renderer consumption.

| Field | Type | Notes |
| --- | --- | --- |
| `BoundingBox` | `float4` or struct (x, y, w, h) | Layout space, top-left origin (Clay convention) |
| `CommandType` | `RenderCommandType` : byte | v1: Rectangle, Text, ScissorStart, ScissorEnd |
| `RenderData` | `RenderData` struct | Tagged union via `CommandType` |
| `ElementId` | `uint` | Stable hash id |
| `ZIndex` | `short` | Sort order within frame |

**`RenderData` variants (v1):**

| Type | Fields |
| --- | --- |
| Rectangle | `Color32 background`, `Vector4 cornerRadius` |
| Text | `FontId font`, `float fontSize`, `Color32 color`, `TextSlice` (index into frame string table) |
| Scissor | `BoundingBox clipRect` |

Deferred command types (not in buffer in v1): Border, Image, OverlayColor, Custom.

**String table:** managed declaration phase interns text into a frame-local `NativeTextBuffer` (indices + `FixedString` or UTF-16 blob). Renderer reads slices by index — no managed `string` on hot path.

---

## Asset handles (cross-seam references)

```csharp
public readonly struct FontId { public readonly uint Value; }
public readonly struct TextureId { public readonly uint Value; }
```

- **Registration:** `IFontRegistry` / `ITextureRegistry` in `Rendering`, populated at startup (TMP SDF atlas → `FontId`, sprites → `TextureId`).
- **Layout** only stores ids in `RenderData`.
- **Renderer** resolves ids to `Material`, `Texture`, glyph UVs via registries.

Layout never references `FontAsset`, `Texture2D`, or `Material`.

---

## `IRenderBackend` (renderer-agnostic contract)

```csharp
public interface IRenderBackend
{
    void Draw(RenderFrame frame, RenderBackendContext context);
}

public readonly struct RenderBackendContext
{
    public Matrix4x4 ViewProjection;  // canvas / camera space
    public bool FlipY;                // layout top-left → GPU
}
```

**v1 adapter — `CanvasRenderBackend`:**

1. `ImGuiGraphic` (`MaskableGraphic`) holds reference to `ImGuiContext` + `IRenderBackend` (or embeds `BatchBuilder` directly).
2. Canvas `willRenderCanvases` / `OnPopulateMesh`: read `context.LastFrame.Commands`, run `BatchBuilder`, upload to `VertexHelper` or direct mesh.
3. Sorting layer / order come from the **Canvas** component — interleaves with other Screen Space–Camera uGUI.

**Fallback adapter — `OverlayCameraRenderBackend`:** RendererFeature + RenderGraph at fixed event; same `BatchBuilder`, different submission. Only if canvas path fails perf/complexity gates.

**`BatchBuilder`:** renderer-internal, not part of `IRenderBackend` surface. Groups commands by batch key (shader variant, texture, clip rect, blend). One draw per batch — **not** one draw per command.

---

## Text measurement seam

```csharp
public interface ITextMeasurer
{
    void Measure(TextSlice text, FontId font, float fontSize, ref TextMetrics metrics);
}
```

- Injected into `ImGuiContext` at construction.
- Called during **declaration** (step 5) on main thread when `Text()` elements open.
- Writes `TextMetrics` (width, height, line count) into arena/native word cache.
- `LayoutResolveJob` reads metrics only — no managed calls in Burst path.
- v1 implementation: TextCore / TMP atlas lookup on main thread.

---

## Pointer / input seam

Pointer state lives on **`ImGuiContext`**, not in `RenderFrame`:

- `SetPointerState` / `UpdateScrollContainers` before layout.
- Query API after `EndLayout`: `TryGetHoveredId()`, `TryGetPressedId()`, scroll offsets.
- Renderer does **not** handle input.

Input routing with uGUI: `ImGuiGraphic` or companion `GraphicRaycaster` on the same canvas; topmost hit wins per canvas sort order. Document interaction with standard `GraphicRaycaster` on sibling canvases.

---

## Coordinate spaces

| Space | Origin | Used by |
| --- | --- | --- |
| Layout | Top-left, Y down (Clay) | `BoundingBox`, pointer, scroll |
| Canvas local | Unity UI convention | `BatchBuilder` output |
| GPU | Flip Y at renderer boundary | `RenderBackendContext.FlipY` |

Renderer owns the layout→canvas→GPU transform. Layout never flips Y.

---

## Testing

| Layer | Test surface |
| --- | --- |
| Layout | `ImGuiContext` + fake `ITextMeasurer`; assert `RenderCommandBuffer` contents and pointer queries without GPU |
| `BatchBuilder` | Feed synthetic `RenderCommandBuffer`; assert vertex counts and batch keys |
| `CanvasRenderBackend` | PlayMode / graphics test optional for v1 |

---

## v1 scope at the seam

| In | Out (defer) |
| --- | --- |
| Rectangle, Text, Scissor commands | Border, Image, Overlay, Custom |
| `CanvasRenderBackend` | Overlay camera fallback until needed |
| Sync `EndLayout` | Async layout / double-buffered arenas |
| `FontId` / `TextureId` handles | Managed asset refs in layout |
| `IRenderBackend` interface | Second backend implementation |

---

## Open follow-ups (not this ticket)

- Canvas batching prototype to validate `OnPopulateMesh` perf
- v1 acceptance criteria (#11)
