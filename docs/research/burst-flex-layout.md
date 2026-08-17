# Burst-compiled flex layout feasibility

**Wayfinder:** [#9 Burst-compiled flex layout feasibility](https://github.com/MykytaRachynskyi/unity_basic/issues/9)  
**Status:** Research complete  
**Date:** 2026-08-17

## Executive summary

**Verdict: feasible** — a Clay-faithful flex layout pass can run under Burst with acceptable ergonomics, but only if the system is split into a **managed tree builder** (immediate-mode API) and a **Burst-compiled resolution pass** (EndLayout equivalent). Unity Entities is not required.

The Clay algorithm maps cleanly to flat struct arrays, explicit stacks, and arena-backed native memory. The main engineering seams are **text measurement** (function pointer or pre-measure on the main thread) and **scroll state** (persistent native state updated outside or inside Burst via function pointers). Parallelizing the layout tree itself is not a realistic win; Burst pays off on hot numeric loops inside the pass, not on multi-threading the hierarchy.

**Recommended dependency tier for v1:** `com.unity.burst` + `com.unity.collections` + `com.unity.mathematics` only. No Entities.

---

## Question

Can a Clay-like flexbox layout pass be implemented as Burst-compiled jobs over struct arrays / native collections, covering measure pass, flex grow/shrink, padding/gap, text measurement callbacks, and scroll container offsets — with acceptable C# ergonomics and DOTS-y memory management?

---

## Clay layout model (primary source)

Primary source: [nicbarker/clay `clay.h`](https://github.com/nicbarker/clay/blob/main/clay.h) and [Clay README](https://github.com/nicbarker/clay).

### Two-phase architecture

Clay separates **declaration** from **resolution**:

1. **Declaration phase** (`Clay_BeginLayout` … `Clay_EndLayout`): nested `CLAY()` macros build a tree via `Clay__OpenElement` / `Clay__CloseElement`. Each open pushes onto `openLayoutElementStack`; close aggregates child dimensions, applies padding/gap, and attaches children.
2. **Resolution phase** (`Clay__CalculateFinalLayout` inside `Clay_EndLayout`): multi-pass sizing and positioning over flat arrays.

This split is the key enabler for Burst: the immediate-mode API can stay managed and idiomatic; only the resolution pass needs HPC#.

### Data structures (flat arrays, not pointer trees)

Clay stores elements in `Clay_LayoutElementArray` (contiguous array). Hierarchy is encoded via:

| Structure | Role |
| --- | --- |
| `layoutElements` | Flat `Clay_LayoutElement` records |
| `layoutElementChildren` / `layoutElementChildrenBuffer` | Child index lists |
| `openLayoutElementStack` | Stack for open elements during declaration |
| `layoutElementTreeRoots` | Floating / attached roots |
| `layoutElementsHashMap` | ID → element lookup |
| `measureTextHashMap` / `measuredWords` | Text measurement cache |
| `scrollContainerDatas` | Persistent scroll offsets per clip container |

Children are referenced by index (`children.elements` points into `layoutElementChildren.internalArray`). There is **no recursive tree traversal via pointers** in the hot path — loops and explicit stacks only.

### Resolution algorithm

From `Clay__CalculateFinalLayout` and `Clay__SizeContainersAlongAxis`:

1. **Size along X** — BFS over tree roots; for each parent, resolve PERCENT sizing, then either **shrink** (content overflow) or **GROW** (distribute free space) along the layout axis.
2. **Text wrap pass** — re-measure/wrap text elements against container width.
3. **DFS post-order** — resize parents along the non-layout axis after children are known.
4. **Size along Y** — repeat axis sizing for vertical dimension.
5. **Aspect ratio adjustment** — scale widths from height × ratio.
6. **Position pass** — DFS with `layoutElementTreeNodeArray1`, applying padding, childGap, alignment, clip `childOffset` (scroll), and generating `Clay_RenderCommand` output.

Sizing modes (`FIT`, `GROW`, `FIXED`, `PERCENT`) match a reduced flexbox subset. GROW distribution and shrink-to-min logic are iterative loops over a `resizableContainerBuffer` — pure float arithmetic, Burst-friendly.

### Text measurement

Clay requires `Clay_SetMeasureTextFunction` — a **C function pointer** called during declaration (text open) and layout (wrap pass). Clay caches measurements in a word-level hash map; the README stresses this callback must be **extremely fast** because it is on the hot path.

Strings are `Clay_String` / `Clay_StringSlice` (pointer + length, not necessarily null-terminated) — not managed `string`.

### Scroll containers

Scroll is modeled as:

- **Clip config** on an element (`clip.horizontal` / `clip.vertical`).
- **`childOffset`** applied during positioning (from internal scroll state or user-provided offset).
- **Persistent state** in `scrollContainerDatas` across frames.
- **`Clay_UpdateScrollContainers`** (pointer + wheel input, momentum) runs **before** `BeginLayout` on the main thread.

Scroll offsets affect layout (content size for scroll panels expands to inner content on the off-axis) and final positions (offset subtracted from child positions during DFS).

---

## Unity Burst & Collections constraints (primary sources)

Primary sources:

- [Burst compilation (Manual)](https://docs.unity3d.com/Manual/script-compilation-burst.html)
- [HPC# overview](https://docs.unity3d.com/6000.7/Documentation/Manual/burst/csharp-hpc-overview.html)
- [C#/.NET type support](https://docs.unity3d.com/6000.7/Documentation/Manual/burst/csharp-type-support.html)
- [Function pointers (Burst package)](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-function-pointers.html)
- [String support (Burst package)](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-string-support.html)
- [Collection types (Collections package)](https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/collection-types.html)

### What Burst allows

- Structs, `ref`/`out`, unsafe pointers, `for`/`while`/`switch`, instance methods on structs.
- `NativeArray`, `NativeList`, `UnsafeList`, `FixedString*Bytes`, `NativeText` — unmanaged collections for job/Burst code.
- **Function pointers** (`BurstCompiler.CompileFunctionPointer`) as the Burst-safe alternative to delegates.
- Generic structs with interface constraints (static dispatch, no boxing).
- Limited `try`/`finally` and `throw` with static messages; no `catch`.

### What Burst forbids (relevant to layout)

| Restriction | Layout impact |
| --- | --- |
| No managed reference types (`string`, classes) in Burst code | Text IDs, labels, and user callbacks cannot live inside the layout job as managed objects |
| No C# delegates in Burst | Text measure must use `FunctionPointer<T>`, not `Action`/`Func` |
| Virtual/interface calls on boxed structs | Use `where T : struct, IMeasureText` generics instead |
| `Span<T>` across managed↔Burst boundary | Pass `NativeArray`/`unsafe` pointers into jobs |
| Managed arrays (except readonly static literals, with limits) | Use `NativeArray` / arena slices |
| `foreach` on generic `IEnumerable<T>` | Use concrete collection types (`NativeArray`, indexed `for`) |
| `Enum` methods (`HasFlag`, etc.) | Use raw flags / integers |

Burst docs explicitly state Burst works **independently of ECS** and is supplemental to Mono/IL2CPP — Entities is optional.

### Collections fit

The Collections package is designed for jobs and Burst-compiled code: resizable `NativeList`, fixed-capacity `FixedList*Bytes`, hash maps for ID lookup, and UTF-8 `FixedString` / `NativeText` for text slices. Multi-dimensional logical structures flatten to 1D arrays (Clay already does this).

---

## Feature-by-feature Burst mapping

### Measure pass (bottom-up FIT aggregation)

**Clay behavior:** On `CloseElement`, children are summed along `layoutDirection`, adding `padding` and `(childCount - 1) * childGap`. Min/max clamps applied. Text elements get dimensions from `Clay__MeasureTextCached` at open time.

**Burst fit:** Excellent. Pure struct math over index buffers. No blockers.

**Mitigation for text:** Pre-measure during managed declaration and store `width`/`height`/`minWidth` on the element record before scheduling the layout job; or invoke a Burst function pointer from within the job for cache misses only.

### Flex grow / shrink

**Clay behavior:** `Clay__SizeContainersAlongAxis` distributes `sizeToDistribute` across GROW children (expand toward `max`) or shrinks resizable children toward `minDimensions` when content overflows. Scroll/clip parents skip shrink on the clipped axis. Off-axis GROW uses parent inner size (scroll panels use content size as max).

**Burst fit:** Excellent. Iterative float loops, no allocations. The algorithm is **inherently sequential per parent** (children of a node depend on parent size), so `IJobParallelFor` over all elements is not correct without a level-synchronous schedule. A single `IJob` or `IJobBurstSchedulable` for the full pass matches Clay and is appropriate for 1–8k elements.

**Note:** Burst vectorization may help inner loops over children; the outer tree walk will not vectorize well. This is acceptable — Clay targets microsecond layout on CPU already.

### Padding / gap

**Clay behavior:** `Clay_Padding` (uint16 per side) and `uint16_t childGap` applied during close and positioning.

**Burst fit:** Trivial. Store as `ushort` or `float` in layout structs.

### Text measurement callbacks

**Clay behavior:** Function pointer `Clay_Dimensions (*)(Clay_StringSlice, Clay_TextElementConfig*, void* userData)` with internal word cache.

**Burst fit:** Partial — the **callback mechanism** works via Burst function pointers (`[BurstCompile]` + `[MonoPInvokeCallback]`). The **implementation** of text measurement depends on font backend:

| Approach | Burst-safe? | Ergonomics |
| --- | --- | --- |
| Monospace stub (width = chars × fontSize) | Yes | Easy; not production |
| Bitmap / SDF glyph atlas lookup (fontId → metrics table in `NativeArray`) | Yes | Good; matches game UI fonts |
| HarfBuzz / FreeType / Unity `Font` / TextCore on main thread | No inside Burst | Pre-measure in managed builder; layout job reads cached sizes |
| `UnityEngine.TextCore` via `FontEngine` | No in Burst job | Split: managed measure, Burst layout |

**Recommended v1 seam:** Managed declaration calls a `MeasureText` service that fills `TextMetrics` on each text node (and optional word cache in native memory). Layout job reads metrics only. Optionally promote atlas lookup to a Burst function pointer later.

Clay's word cache pattern ports directly to `NativeParallelMultiHashMap` or a custom open-addressing table in arena memory.

### Scroll offsets

**Clay behavior:** Persistent `scrollPosition` per scroll container; `childOffset` applied in positioning; `Clay_UpdateScrollContainers` handles wheel/drag/momentum before layout.

**Burst fit:** Good with separation of concerns:

- **Input / momentum integration** — keep on main thread (or a small managed/pre-Burst step); writes `float2` scroll offsets into `NativeArray<ScrollState>`.
- **Layout consumption** — Burst job reads scroll offsets as plain `float2` fields on clip elements; applies offset when computing child positions and content dimensions (mirror `Clay__CalculateFinalLayout` DFS).

External scroll (`Clay_SetQueryScrollOffsetFunction`) maps to a function pointer or pre-filled offset array — same pattern as text.

### Struct arrays vs trees

**Recommendation: Clay-style flat arrays (struct-of-arrays / array-of-structs hybrid).**

| Model | Pros | Cons |
| --- | --- | --- |
| **Flat `LayoutElement[]` + child index ranges** (Clay) | Cache-friendly, arena bump allocation, easy Burst, stable indices for IDs/render commands | Declaration must maintain stacks; random access by ID needs hash map |
| **Pointer tree (`LayoutNode` with child list)** | Familiar OOP API | Managed references or unsafe pointer graphs; poor Burst ergonomics; harder arena reset |
| **Entities components** | ECS integration | Heavy dependency; overkill for immediate-mode UI; archetype churn per frame |

Use `NativeList<LayoutElement>` or a fixed-capacity arena slice reset each frame. Child ranges: `(firstChildIndex, childCount)` or offset into `NativeArray<int> childIndices`.

---

## Blockers and mitigations

| Blocker | Severity | Mitigation |
| --- | --- | --- |
| **Virtual calls / interfaces on heap objects** | High if used | Struct-only layout records; generic `where T : struct` for plugins; function pointers for callbacks |
| **Managed `string` for text/IDs** | High in Burst path | `FixedString32Bytes` / UTF-8 slices for IDs; `NativeText` or interned string table indices; measure text from `ReadOnlySpan<byte>` on managed side |
| **C# recursion** | Low | Clay uses iterative DFS/BFS; prefer explicit stacks (matches Clay, avoids deep-stack risk). Burst supports recursion but it is unnecessary |
| **Delegates for measure/scroll query** | High | `FunctionPointer<MeasureTextDelegate>` compiled once; pass into job struct |
| **Immediate-mode nested API in Burst** | High | Do **not** Burst-compile the `CLAY()`-like builder; only the resolution job |
| **Error handlers / `Debug.Log` with dynamic strings** | Medium | `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]` guards; `ErrorCode` enums out of job; log on main thread after `Complete()` |
| **Parallel layout of tree** | Medium (expectation) | Document as single-threaded Burst job; parallelize renderer/quads, not layout |
| **Function pointer vs job performance** | Low–medium | Burst docs prefer jobs over function pointers for small calls; batch text measure or pre-cache |
| **IL2CPP** | Low | `[MonoPInvokeCallback]` on function pointer targets (Burst docs requirement) |

### Existing ecosystem

Unity UI Toolkit uses Yoga (C++) for flex layout and **does not support Burst** ([Unity staff, 2024](https://discussions.unity.com/t/burst-compatible-ui-toolkit/1556902)). No widely adopted Burst immediate-mode UI layout library was found; this project would be greenfield, but Clay's C implementation is a proven algorithm reference.

---

## Recommended dependency tier

| Tier | Verdict | Rationale |
| --- | --- | --- |
| **Burst + Mathematics + Collections only** | **Recommended for v1** | Sufficient for arena memory, layout job, render-command output buffers, ID hash maps. No ECS concept maps naturally to per-frame immediate-mode trees. Smallest install footprint. Testable without Entities playmode packages. |
| Entities for URP render path only | Defer | 2D UI quads are a buffer + draw-call problem, not an archetype problem. A `NativeArray<RenderCommand>` consumed by a URP feature does not need entities. |
| Full Entities end-to-end | Reject for v1 | High ceremony for重建 the tree every frame; fights Clay's immediate-mode model. |
| Managed-first, Burst fast-follow | Acceptable fallback | Lower upfront complexity, but risks painting into managed layout APIs that are hard to Burst later. Prefer designing the struct buffer API first even if v1 runs managed resolution. |

**Add to `package.json` when implementing:** `com.unity.burst`, `com.unity.collections`, `com.unity.mathematics` (patch versions aligned to Unity 6000.x).

---

## Proposed architecture seam

```
┌─────────────────────────────────────────────────────────────┐
│  Managed (main thread) — immediate-mode API                 │
│  BeginLayout / Element() / Text() / EndLayout               │
│  - Arena bump alloc into NativeArray / UnsafeList             │
│  - Push/pop open-element stack                                │
│  - Optional: MeasureText service → fill TextMetrics           │
│  - Scroll input → update NativeArray<ScrollState>             │
└──────────────────────────┬──────────────────────────────────┘
                           │ LayoutContext (blittable handles)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Burst IJob — LayoutResolveJob                                │
│  - SizeContainersAlongAxis (X, then Y)                        │
│  - Text wrap (reads pre-measured words or FP callback)        │
│  - Position DFS + write RenderCommand buffer                  │
│  - Apply scroll childOffset                                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ NativeArray<RenderCommand>
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  URP backend (managed or Burst mesh build — separate ticket)  │
└─────────────────────────────────────────────────────────────┘
```

**Ergonomics:** User-facing API remains nested scopes / `ref struct` builders (C# idiomatic). `LayoutElement` records are blittable. `EndLayout` schedules the job and optionally completes synchronously for v1 simplicity.

**Clay fidelity:** Port sizing modes, padding, gap, alignment, clip/scroll, floating attach, and flat render-command output. Defer transitions API and debug view to later tickets.

---

## Risks and open questions

1. **Text measurement latency** — If every frame re-measures all strings on the main thread, Burst layout savings may be lost. Need word-level cache (port Clay's) and/or Burst atlas lookup.
2. **Synchronous vs async layout** — `JobHandle.Complete()` in `EndLayout` is simplest; async requires double-buffering element arenas for interactive use.
3. **ID stability** — Clay hash IDs and `CLAY_ID` string literals need a Burst-safe ID strategy (`FixedString` hash or intern table).
4. **Validation parity** — Clay's extensive error reporting uses managed callbacks; decide on dev-only checks vs release `ErrorCode` buffer.
5. **Benchmark target** — Wayfinder map cites ~1–8k elements; profile managed builder + Burst resolve separately before optimizing parallelization.

---

## Conclusion

Burst-compiled flex layout is **feasible and aligned** with the project's DOTS-y, Clay-faithful goals. The Clay resolution pass is already written in a Burst-compatible style (flat arrays, stacks, function-pointer callbacks, no malloc). The immediate-mode declaration layer should remain managed for ergonomics.

**Ship v1 with Burst + Collections + Mathematics only.** Treat Entities as unrelated to layout unless a future render path explicitly benefits from ECS batching — which is unlikely for immediate-mode 2D quads.

---

## References

- [Clay repository](https://github.com/nicbarker/clay) — `clay.h`, README
- [Unity Manual: Burst compilation](https://docs.unity3d.com/Manual/script-compilation-burst.html)
- [Unity Manual: HPC# overview](https://docs.unity3d.com/6000.7/Documentation/Manual/burst/csharp-hpc-overview.html)
- [Unity Manual: C#/.NET type support](https://docs.unity3d.com/6000.7/Documentation/Manual/burst/csharp-type-support.html)
- [Burst package: Function pointers](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-function-pointers.html)
- [Burst package: String support](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-string-support.html)
- [Burst package: Memory aliasing](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/aliasing.html)
- [Collections package: Collection types](https://docs.unity3d.com/Packages/com.unity.collections@2.5/manual/collection-types.html)
- [Unity Discussions: Burst-compatible UI Toolkit](https://discussions.unity.com/t/burst-compatible-ui-toolkit/1556902) (negative reference — confirms UI Toolkit path is not Burst)
