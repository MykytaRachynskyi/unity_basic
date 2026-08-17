# URP high-performance 2D immediate-mode UI rendering

Research for [unity_basic #7](https://github.com/MykytaRachynskyi/unity_basic/issues/7).  
Target stack: **Unity 6000**, **URP 17**, **RenderGraph** (default in new URP projects).

## Context

`unity_basic` is building a **Clay-inspired immediate-mode UI**: layout each frame, arena-backed memory, **render commands** as output, with a **renderer-agnostic layout core** and a **URP backend**.

Clay's reference renderers iterate `Clay_RenderCommandArray` and issue **one backend draw per command** (rectangles, borders, text, images, scissor pairs). That model is correct for portability, not for thousands of elements in Unity.

| Source | What it says |
| --- | --- |
| [Clay README](https://github.com/nicbarker/clay/blob/main/README.md) | Renderer-agnostic; outputs sorted render primitives; `Clay_EndLayout` returns `Clay_RenderCommandArray`. |
| [Clay `Clay_RenderCommand`](https://github.com/nicbarker/clay/blob/main/clay.h) | Command types: `RECTANGLE`, `BORDER`, `TEXT`, `IMAGE`, `SCISSOR_START/END`, `OVERLAY_COLOR`, `CUSTOM`. Each command has `boundingBox`, `commandType`, `renderData`. |
| [Clay Raylib renderer](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c) | Per-command `DrawRectangle` / `DrawTextEx` / `DrawTexturePro`; no batching. |
| [Clay SDL2 renderer](https://github.com/nicbarker/clay/blob/main/renderers/SDL2/clay_renderer_SDL2.c) | Rounded rects batch vertices into one `SDL_RenderGeometry` call per rect; still one draw per rect for axis-aligned fills. |

**Implication:** the URP backend must **re-batch** Clay-style commands; do not mirror reference renderer draw granularity.

---

## 1. URP integration: Renderer Features and RenderGraph (Unity 6)

### 1.1 Required API surface

Unity 6 / URP 17 expects custom passes on the **RenderGraph** path. The legacy non-RenderGraph pass API is no longer developed.

| Topic | Source |
| --- | --- |
| RenderGraph is default; rewrite custom passes | [Upgrade to URP 17 (Unity 6.0)](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/upgrade-guide-unity-6.html) |
| `RecordRenderGraph` registers passes; execution is separate | [Introduction to the render graph system in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-introduction.html) |
| Pass workflow: `ScriptableRenderPass` + `RecordRenderGraph` + `AddRasterRenderPass` | [Write a render pass using the render graph system in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html) |
| Inject via `ScriptableRendererFeature` + `EnqueuePass` | [Inject a render pass with a Scriptable Renderer Feature in URP](https://docs.unity3d.com/6/Documentation/Manual/urp/renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.html) |
| Custom pass workflow overview | [Custom render pass workflow in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/renderer-features/custom-rendering-pass-workflow-in-urp.html) |

**RenderGraph recording rules that matter for UI:**

1. **Recording vs execution** — populate `PassData` in `RecordRenderGraph`; issue GPU commands only in the static `SetRenderFunc` callback ([write render pass doc](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html)).
2. **Use static render funcs** — `SetRenderFunc` should use a `static` method or static lambda to avoid per-frame allocations ([write render pass doc](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html)).
3. **Target the camera color buffer** — `UniversalResourceData.activeColorTexture` is the usual write target for screen-space UI ([write render pass doc](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html)).
4. **PassData discipline** — only fields needed at execute time; extra fields hurt performance ([write render pass doc](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html)).
5. **Automatic optimization** — RenderGraph can cull unused passes, reuse GPU memory, and merge native render passes on TBDR mobile ([introduction](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-introduction.html)).

Samples: import **URP RenderGraph Samples** from package samples ([upgrade guide](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/upgrade-guide-unity-6.html)).

### 1.2 Injection points

**Universal (3D) renderer** — use `RenderPassEvent` on `ScriptableRenderPass.renderPassEvent` ([example Renderer Feature](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/renderer-features/create-custom-renderer-feature.html), [inject pass overview](https://docs.unity3d.com/Manual/urp/inject-a-render-pass.html)).

Practical choices for screen-space HUD:

| Injection point | Typical use |
| --- | --- |
| `BeforeRenderingPostProcessing` | UI under post-processing |
| `AfterRenderingPostProcessing` | UI over scene post-processing (still before final blit/FXAA/grading in many setups) |

**2D renderer** — use `ScriptableRendererFeature2D`, `ScriptableRenderPass2D`, and `RenderPassEvent2D` ([2D custom pass workflow](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/2d/renderer-features/custom-render-pass-workflow-urp-2d.html), [2D injection points](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/2d/renderer-features/injection-points-2d.html)).

| `RenderPassEvent2D` | Notes |
| --- | --- |
| `AfterRenderingSprites` | Composite with 2D scene content |
| `AfterRenderingPostProcessing` | Late overlay; before final blit/AA ([2D injection points](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/2d/renderer-features/injection-points-2d.html)) |
| `AfterRendering` | Last URP pass |

**Screen Space Overlay Canvas caveat:** Unity does **not** expose a URP injection point after built-in overlay UI. `AfterRendering` runs after URP scene rendering but **before** overlay UI in common configurations ([Discussions: after UI injection](https://discussions.unity.com/t/full-screen-renderer-feature-injection-point-for-after-ui-is-drawn/1607159)). For a custom immediate-mode stack that *is* the UI, this is usually fine; mixing with Overlay Canvas requires world-space / camera-space UI or accepting ordering limits.

### 1.3 Intermediate texture / Renderer Features

If a Renderer Feature does not declare inputs via `ScriptableRenderPass.ConfigureInput`, URP may force rendering through an **intermediate texture** ([upgrade guide — Intermediate Texture](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/upgrade-guide-unity-6.html)). Declare inputs correctly to avoid extra blits when drawing UI into `activeColorTexture`.

---

## 2. Draw-call optimization landscape in URP

Unity groups optimization into three families ([Introduction to optimizing draw calls](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls.html)):

1. **GPU instancing** — many copies of one mesh/material in one hardware instanced draw.
2. **SRP Batcher** — many draws sharing a shader variant with cheap per-material constant updates.
3. **Batching** — combine mesh geometry so one draw covers many quads.

URP recommendations ([Choose a method for optimizing draw calls](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls-choose-method.html)):

| Mechanism | URP recommendation |
| --- | --- |
| SRP Batcher | **Enable** |
| GPU Resident Drawer | **Enable** (hardware instancing path for compatible dynamic meshes) |
| BatchRendererGroup | Prefer GPU Resident Drawer except advanced cases |
| Material GPU Instancing checkbox | **Disable** (avoid extra shader variants) |
| Static / dynamic batching | **Disable** (incompatible with GPU Resident Drawer / BRG) |

General guidance also says: reuse materials, prefer **Material Variants** over many materials, and **avoid `MaterialPropertyBlock` in URP/HDRP** when possible ([choose method](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls-choose-method.html)).

**UI takeaway:** thousands of colored rects are **not** solved by SRP Batcher alone if each rect is still a separate draw. You need **geometry batching** (one mesh, many quads) and/or **instancing**. SRP Batcher helps when you must keep multiple material-property draws; it does not merge draws by itself ([SRP Batcher in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher.html)).

---

## 3. SRP Batcher vs GPU instancing

| Topic | Source |
| --- | --- |
| SRP Batcher reduces **render-state changes**, not draw count | [SRP Batcher in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher.html) |
| GPU instancing and SRP Batcher are **mutually exclusive per draw**; SRP Batcher wins for compatible GameObject paths | [Introduction to GPU instancing](https://docs.unity3d.com/6000.5/Documentation/Manual/GPUInstancing.html) |
| Manual instancing: `Graphics.RenderMeshInstanced` bypasses GameObject path | [GPU instancing — SRP Batcher section](https://docs.unity3d.com/6000.5/Documentation/Manual/GPUInstancing.html), [Remove SRP Batcher compatibility](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher-Incompatible.html) |
| `MaterialPropertyBlock` on a renderer makes it **SRP Batcher incompatible** | [Remove SRP Batcher compatibility](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher-Incompatible.html) |

For **scripted** UI draws:

- **`Graphics.RenderMesh`** — one mesh per call; changing material properties between calls uses `RenderParams.matProps` ([Graphics.RenderMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMesh.html)).
- **`Graphics.RenderMeshInstanced`** — up to **1023** instances per call (511 if full `worldToObject` is required); supports custom per-instance structs ([Graphics.RenderMeshInstanced](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html)).
- **`Graphics.RenderMeshIndirect`** — instance count from `GraphicsBuffer` indirect args; supports multi-command buffers; needs compute-capable platform ([Graphics.RenderMeshIndirect](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshIndirect.html)).
- **`Graphics.RenderMeshPrimitives`** — procedural instancing; shader uses `SV_InstanceID` ([Graphics.RenderMeshPrimitives](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshPrimitives.html)).

Legacy `DrawMeshInstanced*` APIs are obsolete; prefer the `RenderMesh*` family ([DrawMeshInstanced](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.DrawMeshInstanced.html), [DrawMeshInstancedProcedural](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.DrawMeshInstancedProcedural.html)).

---

## 4. Mesh generation vs procedural / instanced draws

### 4.1 Approach A — Per-command draws (Clay reference parity)

Iterate render commands; one `DrawMesh` / `RenderMesh` per rectangle, border edge, text line, image.

| Pros | Cons |
| --- | --- |
| Trivial scissor/overlay state mapping | **O(n) draw calls** — fails at 1–8k elements |
| Easy rounded corners per element | High CPU submit cost ([optimizing draw calls intro](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls.html)) |
| Matches Clay reference code | Per-call internal allocations possible ([Graphics.RenderMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMesh.html)) |

**Verdict:** reference-only; not a production URP backend.

### 4.2 Approach B — Dynamic mesh batching (recommended baseline)

Accumulate quads into **one or few meshes** per **batch key**, then **one draw per batch**.

**Batch key examples:** texture id, blend mode, clip rect id, shader variant (solid vs textured vs SDF text).

| Building geometry | Source |
| --- | --- |
| `Mesh.MarkDynamic()` before first upload | [Mesh.MarkDynamic](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.MarkDynamic.html) |
| Layout: `SetVertexBufferParams` once if vertex/index counts stable | [Mesh.SetVertexBufferData](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Mesh.SetVertexBufferData.html), [Mesh API overview](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Mesh.html) |
| Per-frame update: `SetVertexBufferData` / `SetIndexBufferData` | [Mesh.SetVertexBufferData](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Mesh.SetVertexBufferData.html) |
| Skip validation: `MeshUpdateFlags.DontRecalculateBounds`, `DontValidateIndices`, etc. | [MeshUpdateFlags](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Rendering.MeshUpdateFlags.html) |
| Issue draw: `Graphics.RenderMesh` or `CommandBuffer.DrawMesh` inside RenderGraph `SetRenderFunc` | [Graphics.RenderMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMesh.html), [CommandBuffer.DrawMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Rendering.CommandBuffer.DrawMesh.html) |

**Vertex budget:** 4 vertices + 6 indices per axis-aligned quad. 2,000 rects → 8,000 vertices, 12,000 indices — fits one dynamic mesh.

**Rounded rects / borders:** generate corner fans in the batch builder (Clay SDL2 does local geometry expansion per rect ([SDL2 renderer](https://github.com/nicbarker/clay/blob/main/renderers/SDL2/clay_renderer_SDL2.c))). Cost is CPU vertex generation, still **one draw per batch**.

**Scissor / clip:** map `SCISSOR_START`/`END` to either (a) split batches at scissor boundaries, or (b) shader clip rects / clip textures. Clay scissor commands are **not culled** ([Clay README — clip](https://github.com/nicbarker/clay/blob/main/README.md)).

**GC / memory:** feed `SetVertexBufferData` from arena-backed `NativeArray` or persistent buffers filled during command consumption — no per-frame `new Vector3[]` / `List<T>` growth.

**Analog:** UI Toolkit batches elements with identical GPU state into shared vertex buffers; exceeding buffer capacity **fragments batching** and increases draw calls ([UI Toolkit optimizing performance](https://docs.unity3d.com/6000.7/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html), [PanelSettings.vertexBudget](https://docs.unity3d.com/ScriptReference/UIElements.PanelSettings-vertexBudget.html)).

| Pros | Cons |
| --- | --- |
| **1 draw call per batch** (often tens, not thousands) | CPU cost to build vertices each frame |
| Works with standard URP unlit UI shaders + SRP Batcher-friendly single material per batch | Rounded geometry increases vertex count |
| Zero-GC path well documented | Scissor splits can multiply batches |
| Simple debugging in Frame Debugger | |

### 4.3 Approach C — GPU instancing (same quad mesh, per-instance data)

Use one unit quad mesh; per-instance color / transform / UV rect in a structured buffer or instancing array.

| API | Limit / note | Source |
| --- | --- | --- |
| `RenderMeshInstanced` | Max **1023** instances per call; custom instance structs supported | [Graphics.RenderMeshInstanced](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html) |
| `RenderMeshIndirect` | No 1023 cap; args in GPU buffer | [Graphics.RenderMeshIndirect](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshIndirect.html) |
| `RenderMeshPrimitives` | Procedural; shader derives instance from `SV_InstanceID` | [Graphics.RenderMeshPrimitives](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshPrimitives.html) |

Requires **instancing-enabled shader** and typically **SRP Batcher-incompatible** shader layout or instancing-only material ([GPU instancing](https://docs.unity3d.com/6000.5/Documentation/Manual/GPUInstancing.html), [SRP Batcher incompatible](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher-Incompatible.html)).

| Pros | Cons |
| --- | --- |
| Very low draw count for uniform rects | 1023 cap unless indirect |
| Per-instance color without vertex expansion | Shader complexity; matrix/color in instancing layout |
| Good for uniform grids / repeated widgets | Awkward for variable vertex counts (rounded rects, borders) |
| Indirect path scales to 8k+ | Compute buffer management; less Frame Debugger friendly |

### 4.4 Approach D — Compute-driven / GPU Resident Drawer / BRG

Unity 6 steers dynamic instancing toward **GPU Resident Drawer** and reserves **BatchRendererGroup** for advanced cases ([choose method](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls-choose-method.html)).

| Pros | Cons |
| --- | --- |
| Highest throughput for massive identical geometry | Heavy integration cost for a UI command stream |
| Multithreaded culling paths | Poor fit for Clay's heterogeneous commands (text + borders + images) in v1 |

**Verdict:** defer past v1 unless profiling shows CPU mesh build is the bottleneck.

---

## 5. Comparison — thousands of quads (1–8k targets)

Assumptions: immediate-mode UI each frame, mostly axis-aligned rects + some text/images, **zero per-frame GC** from the renderer.

| Approach | Draw calls (typical) | CPU cost | GC | Rounded rects / borders | Scissor | URP Unity 6 fit |
| --- | --- | --- | --- | --- | --- | --- |
| A. Per-command | ~n (1k–8k) | Low per element, huge submit overhead | Risk from strings/TMP | Easy | Trivial | Poor |
| B. Dynamic mesh batch | ~batch keys (5–50) | Mesh build each frame | **Zero** with arena/`NativeArray` | Good (expand verts) | Batch splits | **Excellent** |
| C. Instancing | ⌈n/1023⌉ per material | Low if data already structured | **Zero** with persistent buffers | Poor unless extra verts | Harder | Good for grids |
| C+. Indirect instancing | ~1 per material | Setup + GPU buffers | **Zero** after warmup | Poor | Harder | Good at scale |
| D. BRG / GPU Resident | Low | Pipeline complexity | Depends | Poor v1 fit | Hard | Future |

**Batch key drivers** (same as UI Toolkit batching philosophy — group by GPU state, not logical element count):

- Solid vs textured vs text shader
- Texture / atlas handle
- Blend mode (overlay color pass)
- Active clip rect (scissor)

---

## 6. Text rendering approaches

Clay emits **`CLAY_RENDER_COMMAND_TYPE_TEXT`** per wrapped line with a **string slice** (not always null-terminated) ([Clay README](https://github.com/nicbarker/clay/blob/main/README.md), [clay.h](https://github.com/nicbarker/clay/blob/main/clay.h)).

### 6.1 TextMeshPro / uGUI bridge

TMP uses **SDF atlases** for crisp scaling ([TMP SDF fonts](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsSDF.html), [TMP shaders](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/Shaders.html)). URP projects use TMP's URP SDF shader variants.

| Pros | Cons |
| --- | --- |
| Production-quality SDF, effects, font tooling ([Font Asset Creator](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsCreator.html)) | `TextMeshPro` / `TMP_Text` mesh generation is retained-mode oriented |
| Fast to prototype | Per-line `GameObject` or `TMP_Text` → many draws / GC |
| | Not aligned with immediate-mode arena model |

**v1 role:** use TMP **font assets / atlases** as data source, not `TMP_Text` rendering per command.

### 6.2 Custom SDF glyph cache + batched quads

Generate or import SDF atlas (TMP Font Asset Creator or custom). Layout measures glyphs via Clay-style callback. Renderer emits **one quad per glyph** into the same dynamic mesh batches as rects, with a **UI/SDF shader** sampling the atlas.

| Pros | Cons |
| --- | --- |
| Same batching pipeline as rects | Must implement glyph layout / kerning |
| Zero-GC with cached glyph metrics in arena | Atlas rebuild when font/range changes |
| Matches Clay per-line commands | Shader must handle SDF distance field |

### 6.3 Bitmap / dynamic font textures

Raylib/SDL reference paths rasterize text to surfaces/textures per command ([Raylib renderer](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c), [SDL2 renderer](https://github.com/nicbarker/clay/blob/main/renderers/SDL2/clay_renderer_SDL2.c)).

| Pros | Cons |
| --- | --- |
| Simple | Poor scale / quality; texture churn |

**Verdict:** avoid for v1.

### Text v1 recommendation

**Custom SDF atlas + batched glyph quads**, with atlases produced via **TMP Font Asset Creator** (SDF/SDFAA modes) ([Font Asset Creator](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsCreator.html)). Defer a live `TMP_Text` bridge.

---

## 7. Recommended v1 architecture

```
Layout (arena) → RenderCommandBuffer
       ↓
BatchBuilder (consume commands, zero GC)
  - keys: shader + texture + clip + blend
  - outputs: persistent Mesh + vertex/index counts
       ↓
UrpUiRendererFeature (ScriptableRendererFeature)
  - enqueue ScriptableRenderPass
  - renderPassEvent / renderPassEvent2D: AfterRenderingPostProcessing (tune per project)
       ↓
RecordRenderGraph → SetRenderFunc (static)
  - bind activeColorTexture
  - for each batch: update mesh if dirty, Graphics.RenderMesh / cmd.DrawMesh
```

### v1 scope

| In v1 | Defer |
| --- | --- |
| `RECTANGLE` (axis-aligned; optional simple corner radius) | `CUSTOM` 3D elements |
| `BORDER` (axis-aligned + corner rings as extra verts) | GPU instancing / indirect draws |
| `IMAGE` (textured quads, batch by texture) | GPU Resident Drawer / BRG |
| `TEXT` via SDF atlas quads | TMP_Text bridge |
| `SCISSOR_START/END` via batch splits + clip rects | Retained-mode diff rendering |
| `OVERLAY_COLOR` via separate blend batch or shader pass | After-Overlay Canvas injection |

### Performance targets (engineering checks)

| Check | Tool |
| --- | --- |
| Draw calls ≈ batch count, not element count | Frame Debugger |
| Zero GC from renderer | Profiler / GC Alloc column |
| Vertex buffer not fragmenting | Compare draws when exceeding ~8k verts |
| Correct composite order vs post-processing | Frame Debugger + injection point tuning |

### Shader / material notes

- Prefer **one unlit UI shader** with variants for solid, textured, SDF text.
- Keep materials **SRP Batcher compatible** for the single-draw-per-batch path ([SRP Batcher](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher.html)).
- Per-rect colors belong in **vertex attributes**, not per-draw material color changes, to avoid extra draws and `MaterialPropertyBlock` pressure ([choose method — avoid MPB](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls-choose-method.html)).

---

## 8. Open questions for follow-up tickets

1. **Injection point** — confirm 3D vs 2D URP renderer for the vertical slice; tune `RenderPassEvent` vs `RenderPassEvent2D`.
2. **HDR / intermediate targets** — validate UI pass writes correct target when HDR and post-processing are enabled.
3. **Input / y-flip** — screen-space projection must match URP camera target orientation in RenderGraph pass.
4. **Instancing upgrade** — if profiling shows mesh CPU cost dominates, evaluate `RenderMeshIndirect` for solid-color rect layers only.

---

## Sources (primary)

### Unity Manual — URP / RenderGraph

- [Upgrade to URP 17 (Unity 6.0)](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/upgrade-guide-unity-6.html)
- [Introduction to the render graph system in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-introduction.html)
- [Write a render pass using the render graph system in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/render-graph-write-render-pass.html)
- [Custom render pass workflow in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/renderer-features/custom-rendering-pass-workflow-in-urp.html)
- [Inject a render pass with a Scriptable Renderer Feature in URP](https://docs.unity3d.com/6/Documentation/Manual/urp/renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.html)
- [Add a 2D custom render pass to the frame rendering loop in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/2d/renderer-features/custom-render-pass-workflow-urp-2d.html)
- [2D injection points reference in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/urp/2d/renderer-features/injection-points-2d.html)

### Unity Manual — draw calls / batching

- [Introduction to optimizing draw calls](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls.html)
- [Choose a method for optimizing draw calls](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls-choose-method.html)
- [Scriptable Render Pipeline Batcher in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher.html)
- [Remove SRP Batcher compatibility for GameObjects in URP](https://docs.unity3d.com/6000.5/Documentation/Manual/SRPBatcher-Incompatible.html)
- [Introduction to GPU instancing](https://docs.unity3d.com/6000.5/Documentation/Manual/GPUInstancing.html)

### Unity Scripting API

- [Graphics.RenderMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMesh.html)
- [Graphics.RenderMeshInstanced](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html)
- [Graphics.RenderMeshIndirect](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshIndirect.html)
- [Graphics.RenderMeshPrimitives](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Graphics.RenderMeshPrimitives.html)
- [RenderParams](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/RenderParams.html)
- [Mesh.MarkDynamic](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.MarkDynamic.html)
- [Mesh.SetVertexBufferData](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Mesh.SetVertexBufferData.html)
- [Mesh](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Mesh.html)
- [MeshUpdateFlags](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Rendering.MeshUpdateFlags.html)
- [CommandBuffer.DrawMesh](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Rendering.CommandBuffer.DrawMesh.html)
- [PanelSettings.vertexBudget](https://docs.unity3d.com/ScriptReference/UIElements.PanelSettings-vertexBudget.html)

### TextMeshPro package docs

- [About SDF fonts](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsSDF.html)
- [Shaders](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/Shaders.html)
- [Font Asset Creator](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssetsCreator.html)

### UI Toolkit (batching analogy)

- [UI Toolkit — Optimizing performance](https://docs.unity3d.com/6000.7/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html)

### Clay (reference render command model)

- [Clay README](https://github.com/nicbarker/clay/blob/main/README.md)
- [clay.h](https://github.com/nicbarker/clay/blob/main/clay.h)
- [Raylib renderer](https://github.com/nicbarker/clay/blob/main/renderers/raylib/clay_renderer_raylib.c)
- [SDL2 renderer](https://github.com/nicbarker/clay/blob/main/renderers/SDL2/clay_renderer_SDL2.c)

### Unity Discussions (injection ordering caveat)

- [Full screen renderer feature injection point for after UI is drawn](https://discussions.unity.com/t/full-screen-renderer-feature-injection-point-for-after-ui-is-drawn/1607159)

---

*Research date: 2026-08-17. Unity doc version cited: 6000.5 (Unity 6.5) where available.*
