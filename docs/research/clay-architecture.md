# Clay Architecture Research

Research for [unity_basic #5](https://github.com/MykytaRachynskyi/unity_basic/issues/5): Clay-faithful immediate-mode UI inside the `unity_basic` package (layout core + URP backend).

**Primary sources**

- [Clay README](https://github.com/nicbarker/clay/blob/main/README.md)
- [clay.h](https://github.com/nicbarker/clay/blob/main/clay.h)
- Renderer examples: [raylib](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c), [sokol](https://github.com/nicbarker/clay/blob/main/renderers/sokol/sokol_clay.h), [examples directory](https://github.com/nicbarker/clay/tree/main/examples)

---

## Executive summary

Clay is a single-file, arena-allocated, immediate-mode layout library. Each frame: set layout size and pointer state, update scroll, declare a tree with nested `CLAY()` blocks, then consume a sorted `Clay_RenderCommandArray`. The C# port should preserve that frame contract, struct shapes, element-ID hashing, and render-command semantics; swap C macros for scoped C# builders and wire input/text/rendering through Unity (Input System, TextCore, URP).

---

## Frame lifecycle (canonical order)

From the [README quick start](https://github.com/nicbarker/clay/blob/main/README.md#quick-start) and [sokol renderer `sclay_new_frame`](https://github.com/nicbarker/clay/blob/main/renderers/sokol/sokol_clay.h):

| Step | Clay API | Purpose |
| --- | --- | --- |
| 1 | `Clay_SetLayoutDimensions` | Root viewport size (screen / canvas) |
| 2 | `Clay_SetPointerState(position, isPointerDown)` | Mouse/touch position + held state |
| 3 | `Clay_UpdateScrollContainers(enableDrag, scrollDelta, deltaTime)` | Wheel, drag, momentum scroll |
| 4 | `Clay_BeginLayout()` | Reset ephemeral state; open root container |
| 5 | `CLAY()` / `CLAY_TEXT()` … | Declare hierarchy |
| 6 | `Clay_EndLayout(deltaTime)` | Layout pass → `Clay_RenderCommandArray` |
| 7 | Iterate render commands | Backend draws primitives |

Init (once): `Clay_MinMemorySize` → arena allocation → `Clay_Initialize` → `Clay_SetMeasureTextFunction`.

| Maps to C#/Unity | Needs adaptation | Defer v1 |
| --- | --- | --- |
| Same ordered frame pipeline in a `ClayContext` service | C# has no `CLAY()` macro; use `using (Clay.Element(id, config)) { … }` or equivalent scope builder | Multi-context switching (`Clay_SetCurrentContext`) |
| Hook from `MonoBehaviour.Update` or render feature | `deltaTime` from `Time.deltaTime`; layout dims from canvas / camera pixel rect | Running layout twice per frame for frame-accurate pointer (document as opt-in) |

---

## 1. Layout model

### Clay behavior

Flex-like box model on a single axis per container ([README — Configuring Layout](https://github.com/nicbarker/clay/blob/main/README.md#configuring-layout-and-styling-ui-elements), [clay.h `Clay_LayoutConfig`](https://github.com/nicbarker/clay/blob/main/clay.h)):

- **Direction**: `CLAY_LEFT_TO_RIGHT` (default) or `CLAY_TOP_TO_BOTTOM`
- **Sizing per axis** ([clay.h sizing macros](https://github.com/nicbarker/clay/blob/main/clay.h)):
  - `FIT(min, max)` — wrap content
  - `GROW(min, max)` — share remaining parent space
  - `FIXED(px)` — exact pixels
  - `PERCENT(0..1)` — fraction of parent inner size
- **Padding**, **childGap**, **childAlignment** (x/y)
- **Styling on declaration** (`Clay_ElementDeclaration`): `backgroundColor`, `cornerRadius`, `border`, `image`, `aspectRatio`, `floating`, `clip`, `custom`, `transition`, `userData`
- **Text**: `CLAY_TEXT` + injected `Clay_SetMeasureTextFunction` (hot path; internal word cache)
- **Floating**: out-of-flow elements with attach points, z-index, pointer capture ([README — Floating](https://github.com/nicbarker/clay/blob/main/README.md#floating-elements-absolute-positioning))
- **Visibility culling**: enabled by default; only visible elements emit render commands ([README — Visibility Culling](https://github.com/nicbarker/clay/blob/main/README.md#visibility-culling))

Hierarchy is built by nesting `CLAY(id, config) { children }` — plain C control flow (loops, functions) works inside blocks ([README — Building UI Hierarchies](https://github.com/nicbarker/clay/blob/main/README.md#building-ui-hierarchies)).

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| Struct fields → C# `struct` / `readonly record` mirrors (`LayoutConfig`, `Sizing`, `ElementDeclaration`) | Declarative syntax via scoped builders instead of macros | Full floating attach-to-element-by-id tooltips/modals |
| Same sizing enum semantics and percent 0–1 rule | Text measure via TextCore / `FontAsset` + cache keyed like Clay | Aspect-ratio elements (unless needed for first demo) |
| `backgroundColor` 0–255 RGBA convention | Y-axis: Clay top-left origin; Unity UI often bottom-left — document coordinate flip in URP backend | Built-in debug inspector panel (`Clay_SetDebugModeEnabled`) |
| Border / corner radius render data in commands | Image `userData` → `Texture`/handle id | `betweenChildren` border mode |

---

## 2. Arena memory

### Clay behavior

Static bump allocator; no `malloc`/`free` during layout ([README features](https://github.com/nicbarker/clay/blob/main/README.md#major-features)):

```c
typedef struct Clay_Arena {
    uintptr_t nextAllocation;
    size_t capacity;
    char *memory;
} Clay_Arena;
```

- `Clay_MinMemorySize()` — bytes required for current `maxElementCount` + text cache settings ([clay.h ~4024](https://github.com/nicbarker/clay/blob/main/clay.h))
- Default max elements: **8192** (~3.5 MB arena) ([clay.h `Clay__defaultMaxElementCount`](https://github.com/nicbarker/clay/blob/main/clay.h))
- `Clay_Initialize(arena, dimensions, errorHandler)` — persistent allocations from arena
- Each `Clay_BeginLayout()` calls `Clay__InitializeEphemeralMemory` — frame-scoped reset while retaining scroll/transition state ([clay.h `Clay_BeginLayout`](https://github.com/nicbarker/clay/blob/main/clay.h))
- Errors: `CLAY_ERROR_TYPE_ARENA_CAPACITY_EXCEEDED`, `ELEMENTS_CAPACITY_EXCEEDED`, etc.

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| Single pre-sized buffer per `ClayContext` | `NativeArray<byte>` or `UnsafeUtility.Malloc` + bump pointer; align like C | Dynamic arena grow (Clay requires re-init instead) |
| `MinMemorySize` + configurable caps | Expose `SetMaxElementCount` / `SetMaxMeasureTextCacheWordCount` on context | WASM export surface |
| Error handler callback | Log via Unity `Debug.LogError` + optional exception policy | — |

---

## 3. Element IDs

### Clay behavior

IDs are **hashed strings**, not sequential handles ([README — Element IDs](https://github.com/nicbarker/clay/blob/main/README.md#element-ids), [clay.h `Clay_ElementId`](https://github.com/nicbarker/clay/blob/main/clay.h)):

```c
typedef struct Clay_ElementId {
    uint32_t id;       // hash + 1 (0 reserved = null)
    uint32_t offset;   // for CLAY_IDI / indexed ids
    uint32_t baseId;
    Clay_String stringId;
} Clay_ElementId;
```

Macros:

- `CLAY_ID("Name")` — stable across frames
- `CLAY_IDI("Item", index)` — loop-friendly stable ids
- `CLAY_ID_LOCAL` / `CLAY_SIDI_LOCAL` — hash with parent id as seed (hierarchy-scoped)
- `CLAY_AUTO_ID()` — id from hierarchy position; **unstable** when tree changes

Uses: `Clay_PointerOver`, `Clay_GetElementData`, `Clay_GetScrollContainerData`, render command `.id`, transitions, floating `parentId`. **Duplicate IDs in one layout are an error.**

Hash implementation: `Clay__HashString` / `Clay__HashStringWithOffset` ([clay.h ~1435](https://github.com/nicbarker/clay/blob/main/clay.h)) — port verbatim for compatibility.

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| `uint32_t id` on render commands | `ClayId.FromString("Sidebar")`, `ClayId.Indexed("Item", i)` | `CLAY_AUTO_ID` equivalent (debug-only) |
| Stable ids required for scroll + pointer queries | UTF-8 string hashing; `ReadOnlySpan<char>` overloads | Dynamic runtime strings without `GetElementId` |
| `found` flag on query structs | Same API shape on `GetElementData` / `GetScrollContainerData` | — |

---

## 4. Scroll containers

### Clay behavior

Scrolling = **clip** + **child offset** ([README — Scrolling](https://github.com/nicbarker/clay/blob/main/README.md#scrolling-elements), [clay.h `Clay_ClipElementConfig`](https://github.com/nicbarker/clay/blob/main/clay.h)):

```c
.clip = {
    .horizontal = true,
    .vertical = true,
    .childOffset = Clay_GetScrollOffset()  // or manual Vector2
}
```

Built-in scroll state ([clay.h `Clay_UpdateScrollContainers`](https://github.com/nicbarker/clay/blob/main/clay.h)):

- Wheel: `scrollDelta * 10` on innermost pointer-over scroll container
- Drag scroll when `enableDragScrolling && pointer down`
- Momentum decay (`*= 0.95`) after release
- Clamps offset to `[0, -(contentSize - viewportSize)]`
- `Clay_GetScrollContainerData(id)` returns `scrollPosition` pointer (mutable), dimensions, `found`
- Optional `Clay_SetQueryScrollOffsetFunction` for external scroll ownership

Render side: clip emits `SCISSOR_START` / `SCISSOR_END` around children ([README render commands](https://github.com/nicbarker/clay/blob/main/README.md#clay_rendercommand)).

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| `.clip` config + internal offset storage | Feed `Input.mouseScrollDelta` / touch delta into `UpdateScrollContainers` | `SetQueryScrollOffsetFunction` (external scroll bars) |
| `Clay_GetScrollOffset()` during open element | URP: scissor rect from `SCISSOR_*` commands (viewport Y flip) | Momentum tuning parity tests |
| Scroll retained across frames in context | Drag scroll on touch / pen | Horizontal-only scroll polish |

---

## 5. Render command array

### Clay behavior

Output of `Clay_EndLayout(float deltaTime)` ([clay.h](https://github.com/nicbarker/clay/blob/main/clay.h)):

```c
typedef struct Clay_RenderCommandArray {
    int32_t capacity;
    int32_t length;
    Clay_RenderCommand *internalArray;
} Clay_RenderCommandArray;

typedef struct Clay_RenderCommand {
    Clay_BoundingBox boundingBox;
    Clay_RenderData renderData;  // union by commandType
    void *userData;
    uint32_t id;
    int16_t zIndex;
    Clay_RenderCommandType commandType;
} Clay_RenderCommand;
```

**Command types** (process in array order; already sorted for z-order):

| Type | Renderer action |
| --- | --- |
| `RECTANGLE` | Fill rounded rect (`backgroundColor`, `cornerRadius`) |
| `BORDER` | Inset border sides + corner arcs |
| `TEXT` | Draw string slice (`fontId`, `fontSize`, `letterSpacing`, `textColor`) |
| `IMAGE` | Textured quad (`imageData`, tint, corner radius) |
| `SCISSOR_START` / `SCISSOR_END` | Clip stack |
| `OVERLAY_COLOR_START` / `END` | `mix(src, overlay.rgb, overlay.a)` on subtree ([raylib shader example](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c)) |
| `CUSTOM` | User interprets `customData` |
| `NONE` | Skip |

**Renderer pattern** (all official backends): single forward loop, switch on `commandType`, maintain scissor/overlay stacks ([sokol `sclay_render`](https://github.com/nicbarker/clay/blob/main/renderers/sokol/sokol_clay.h), [raylib `Clay_Raylib_Render`](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c)).

Retained-mode hint: compare consecutive frames by `id` + command memcmp ([README — Retained Mode](https://github.com/nicbarker/clay/blob/main/README.md#retained-mode-rendering)).

### Unity / URP mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| Command list as backend input | `ClayUrpRenderer` iterates commands each frame | Retained mesh cache / dirty detection |
| `boundingBox` in layout space | Convert to screen space; flip Y for Unity | `CUSTOM` 3D-in-UI (raylib demo) |
| Alpha blending pipeline | URP unlit UI shader + rounded rect SDF or tessellated quads (sokol-style) | Nested overlay stack optimization |
| `fontId` index → font table | Register `FontAsset` list like `sclay_font_t[]` | HTML renderer parity |
| `imageData` opaque pointer | `Texture` or sprite handle registry | Cairo/PDF backends |

**v1 backend scope**: `RECTANGLE`, `TEXT`, `SCISSOR_*`, solid colors. Add `BORDER`, `IMAGE`, `OVERLAY_*` once core loop is stable.

---

## 6. Transition API

### Clay behavior

Declarative tweens on elements with **stable IDs** ([README — Transitions](https://github.com/nicbarker/clay/blob/main/README.md#transitions), [example: raylib-transitions](https://github.com/nicbarker/clay/tree/main/examples/raylib-transitions)):

```c
.transition = {
    .handler = Clay_EaseOut,
    .duration = 0.5f,
    .properties = CLAY_TRANSITION_PROPERTY_WIDTH | CLAY_TRANSITION_PROPERTY_POSITION | ...,
    .enter = { .setInitialState = EnterExitSlideUp, .trigger = ... },
    .exit = { .setFinalState = EnterExitSlideUp, .trigger = ..., .siblingOrdering = ... },
    .interactionHandling = CLAY_TRANSITION_DISABLE_INTERACTIONS_WHILE_TRANSITIONING_POSITION,
}
```

- `Clay_EndLayout(deltaTime)` advances transition state; may **re-insert exiting subtrees** as floating clones ([clay.h ~4445](https://github.com/nicbarker/clay/blob/main/clay.h))
- Property flags: position, dimensions, colors, corner radius, border ([clay.h `Clay_TransitionProperty`](https://github.com/nicbarker/clay/blob/main/clay.h))
- Built-in easing: `Clay_EaseOut`
- Pointer hit-testing skips elements in exit/enter per config

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| Concept of stable-id animation | Could delegate to Unity tween later | **Entire transition subsystem for v1** |
| `deltaTime` on `EndLayout` | — | Exit transitions / sibling ordering |
| — | — | Custom `setInitialState` / `setFinalState` callbacks |

**Recommendation**: Stub `TransitionElementConfig` in layout structs but no-op handler in v1; implement layout + render first.

---

## 7. Pointer / input model

### Clay behavior

Clay does **not** read OS input; host feeds state ([README — Pointer](https://github.com/nicbarker/clay/blob/main/README.md#mouse-touch-and-pointer-interactions)):

```c
Clay_SetPointerState(position, isPointerDown);  // before layout
```

- **`isPointerDown`**: true for entire hold, not edge-triggered
- Internal click detection → `Clay_PointerData.state`:
  - `PRESSED_THIS_FRAME`, `PRESSED`, `RELEASED_THIS_FRAME`, `RELEASED`
- **During layout**: `Clay_Hovered()`, `Clay_OnHover(callback, userData)`
- **Outside layout**: `Clay_PointerOver(id)`, `Clay_GetPointerOverIds()`
- Hit boxes use **previous frame** layout ([README note](https://github.com/nicbarker/clay/blob/main/README.md#mouse-touch-and-pointer-interactions))
- Floating elements: `pointerCaptureMode` CAPTURE vs PASSTHROUGH
- Scroll drag requires pointer down + `UpdateScrollContainers(true, …)`

[sokol input wiring](https://github.com/nicbarker/clay/blob/main/renderers/sokol/sokol_clay.h): mouse move/down/up + scroll accumulated per frame → `sclay_new_frame`.

### Unity mapping

| Maps directly | Adaptation | Defer v1 |
| --- | --- | --- |
| Single pointer position in layout space | Map from `InputSystem`/UI Toolkit coords; handle canvas scale | Multi-touch beyond primary pointer |
| `OnHover` callback per element | C# `Action<ClayElementId, ClayPointerData, object>` | Pointer capture on floating layers (partial v1) |
| Press/hold/release state machine | Left button / primary touch only initially | Gamepad focus |
| One-frame lag documented | Optional double-layout pass | — |

---

## Recommended v1 scope for unity_basic

### Adopt (Clay-faithful core)

1. Frame lifecycle API surface matching Clay order
2. Layout structs + sizing/flex algorithm port
3. Arena + ephemeral per-frame reset
4. Element ID hashing (`CLAY_ID` / `CLAY_IDI` equivalents)
5. Clip/scissor scroll containers with built-in wheel scroll
6. Pointer state + `Hovered` / `OnHover` / `PointerOver`
7. `Clay_RenderCommandArray` shape and v1 URP backend: rectangles, text, scissor
8. Pluggable `MeasureText` with cache
9. Visibility culling toggle

### Adapt for Unity

- Scoped C# element builders vs C macros
- TextCore measurement + font id table
- Coordinate system + DPI / canvas scale factor
- URP draw path (not immediate-mode GL)
- `userData` / `imageData` as typed handles or `GCHandle` (avoid raw pointers in managed code where possible)

### Defer post-v1

- Transition API (enter/exit, easing, exit subtree cloning)
- Debug inspector overlay
- Floating attach-to-id, z-index compositing beyond basics
- CUSTOM render commands / 3D-in-panel
- Retained-mode diff rendering
- Multi Clay contexts
- `Clay_SetQueryScrollOffsetFunction`
- Aspect ratio, image elements, border rendering (unless required by first vertical slice)

---

## Reference: minimal host loop

From [Clay README](https://github.com/nicbarker/clay/blob/main/README.md#quick-start) + [sokol_clay.h](https://github.com/nicbarker/clay/blob/main/renderers/sokol/sokol_clay.h):

```c
Clay_SetLayoutDimensions((Clay_Dimensions){ width, height });
Clay_SetPointerState(pointerPos, pointerDown);
Clay_UpdateScrollContainers(true, scrollDelta, deltaTime);

Clay_BeginLayout();
// CLAY(...) { ... }
Clay_RenderCommandArray cmds = Clay_EndLayout(deltaTime);

for (int i = 0; i < cmds.length; i++) {
    Clay_RenderCommand *cmd = &cmds.internalArray[i];
    switch (cmd->commandType) { /* backend */ }
}
```

Unity equivalent: one `ClayContext` per UI root, called from gameplay/update or `ScriptableRendererFeature`, then URP backend consumes commands.

---

## Issue tracker

- Wayfinder / GitHub: [MykytaRachynskyi/unity_basic#5](https://github.com/MykytaRachynskyi/unity_basic/issues/5)
