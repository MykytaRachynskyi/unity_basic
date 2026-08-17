# Canvas-integrated batching prototype

Prototype for [Canvas-integrated batching prototype](https://github.com/MykytaRachynskyi/unity_basic/issues/12) (wayfinder map [#2](https://github.com/MykytaRachynskyi/unity_basic/issues/2)).

**Branch:** `research/canvas-batching-prototype`  
**Code:** `Runtime/ImGui/Prototype/` (throwaway), benchmark: `Editor/ImGui/Prototype/CanvasBatchingBenchmark.cs`

## Question

Does the `ImGuiGraphic` + `OnPopulateMesh` canvas path meet v1 perf gates (≤2 ms render, ≤16 draws, 0 GC at 8k same-material rects)?

## Method

Unity 6000.3.9f1, Editor batchmode, 8k axis-aligned rectangle commands, 60-frame warmup, 120-frame average.

Three upload strategies compared:

| Path | What it models |
| --- | --- |
| **Quad** | `VertexHelper.AddUIVertexQuad` per command (naive) |
| **FillMesh** | `BatchBuilder` → `Mesh` → `CanvasRenderer.SetMesh` / `FillMesh` (optimized canvas) |
| **Direct mesh** | Same mesh build as FillMesh (RendererFeature submission would reuse this) |

## Results (2026-08-17)

| Metric | Quad (naive) | FillMesh (optimized canvas) | v1 gate |
| --- | --- | --- | --- |
| CPU upload (render only) | **3.15 ms** | **0.35 ms** | ≤2 ms combined layout+render |
| Draw calls (1 batch) | 1 | 1 | ≤16 |
| GC / frame (120-frame loop) | 0 B | ~3 MB total (`mesh.vertices` setter) | 0 B |

Benchmark command:

```bash
unity run . -- -nographics -executeMethod Basic.ImGui.Prototype.Editor.CanvasBatchingBenchmark.Run -logFile -
```

## Verdict

**Yes — commit to `CanvasRenderBackend` as v1 default**, with constraints:

1. **Draw calls:** One batched `MaskableGraphic` ⇒ **1 draw** for 8k same-material rects. Well under the ≤16 gate.
2. **Frame time:** Batched mesh build + upload at **~0.35 ms** (render CPU only). Leaves headroom for layout within the ≤2 ms combined gate. The naive per-quad `VertexHelper` path at **~3.1 ms** must **not** be used.
3. **GC:** `mesh.vertices` / `mesh.colors32` property setters allocate internally — **unsuitable for hot path**. Production `BatchBuilder` must upload via **`Mesh.SetVertexBufferData`** on a `MarkDynamic` mesh (or equivalent zero-copy path). The naive quad path is GC-free but too slow.
4. **RendererFeature fallback:** Not needed for performance. Direct mesh and canvas FillMesh paths are equal (~0.35 ms). Canvas path keeps uGUI sorting-layer interleaving from the [#6](https://github.com/MykytaRachynskyi/unity_basic/issues/6) decision.

## v1 implementation guidance

```
RenderCommandBuffer
  → BatchBuilder (persistent NativeArray / exact-size buffers, 1 batch key for solid rects)
  → MarkDynamic Mesh + SetVertexBufferData + SetIndexBufferData
  → ImGuiGraphic.OnPopulateMesh OR CanvasRenderer.SetMesh
  → 1 CanvasRenderer draw
```

**Do not:** one `AddUIVertexQuad` per command, or `mesh.vertices` assignment per frame.

**Defer:** RendererFeature `OverlayCameraRenderBackend` unless canvas integration fails in practice (sorting, masking, or multi-canvas edge cases).

## Open follow-up

- Validate zero-GC `SetVertexBufferData` upload in production `BatchBuilder` (prototype used property setters for speed of implementation).
- Play Mode Frame Debugger confirmation of draw count with real canvas + material.
