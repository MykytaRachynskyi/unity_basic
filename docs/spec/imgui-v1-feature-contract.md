# Basic.ImGui — v1 feature contract and acceptance criteria

Decision for [wayfinder #11](https://github.com/MykytaRachynskyi/unity_basic/issues/11).  
Inputs: map decisions on vertical slice (#4), Clay architecture (#5), layout↔renderer boundary (#8), Burst layout (#9), uGUI layering (#6).

---

## v1 is shippable when

A **debug HUD** play-mode demo proves the immediate-mode pipeline end-to-end: live perf stats, interactive panel (buttons + scrollable list), synthetic stress benchmark with pass/fail gates, uGUI compositing proof, and automated tests at layout and play-mode layers.

---

## Layout declaration surface (mandatory)

| Capability | v1 |
| --- | --- |
| Sizing: `FIT` / `GROW` / `FIXED` / `PERCENT` on both axes | In |
| Container: direction, padding, childGap, childAlignment | In |
| Scroll containers: wheel + drag-while-pointer-down | In |
| Text: multi-line wrap, measure cache, `letterSpacing` | In |
| Element styling: `backgroundColor`, `cornerRadius`, `clip` (scissor) | In |
| Visibility culling (on by default) | In |
| Border, image, floating, aspect-ratio | Out → v1.1 |
| Transition API (enter/exit, easing) | Out → stub/no-op only |
| Custom element data | Out |
| Clay debug inspector overlay | Out |

---

## Render command types (v1)

Per [layout↔renderer boundary](imgui-layout-renderer-boundary.md):

| In | Out (defer to v1.1+) |
| --- | --- |
| `Rectangle` (solid fill, corner radius) | `Border` |
| `Text` (SDF atlas glyph quads) | `Image` |
| `ScissorStart` / `ScissorEnd` | `OverlayColor`, `Custom` |

---

## Pointer and interaction (v1)

Clay-parity pointer API on `ImGuiContext`:

- `SetPointerState(position, isPointerDown)` before layout each frame
- Post-layout queries: `TryGetHoveredId`, pressed/released-this-frame states
- `OnHover` callbacks during declaration
- Primary mouse / primary touch only
- One-frame hit-test lag: accepted and documented
- Scroll: wheel delta + drag while pointer is down (`UpdateScrollContainers`)
- No gamepad focus, no multi-touch beyond primary pointer

---

## Vertical slice demo (debug HUD)

Must include:

1. **Live stats panel** — element count, frame time (layout + render), draw-call / batch count
2. **Interactive panel** — buttons (hover/press feedback), scrollable list (layout + input + text)
3. **Stress benchmark panel** — synthetic scene at **8k elements**; pass/fail indicators for perf gates below
4. **uGUI compositing proof** — sibling uGUI panels above and below the ImGui canvas on different sorting orders; ImGui HUD renders between them (visual check + Frame Debugger sort order)

---

## Performance gates (pass/fail)

Measured in **Editor play mode**, 1080p canvas, after **60-frame warmup**:

| Gate | Threshold | Tool |
| --- | --- | --- |
| Element capacity | 8k elements in stress panel without arena overflow | HUD + arena error handler |
| Frame time | ≤ **2 ms** combined layout + render | HUD stats + Profiler |
| GC allocations | **0 B / frame** on hot path | Profiler GC Alloc column |
| Draw calls | ≤ **16** for same-material 8k-rect stress scene | Frame Debugger / HUD batch count |

Gates are **self-certifying** in the HUD benchmark panel (green/red indicators).

---

## Acceptance test tiers

| Tier | Type | Scope |
| --- | --- | --- |
| 1 | **EditMode** (automated) | `ImGuiContext` + fake `ITextMeasurer`; assert `RenderCommandBuffer` contents and pointer queries — no GPU |
| 2 | **PlayMode smoke** (automated) | HUD loads; buttons respond; scroll list works |
| 3 | **HUD benchmark** (self-certifying) | Synthetic 8k-element scene; live pass/fail for perf gates |
| 4 | **Manual checklist** | uGUI layering proof; Frame Debugger batch count sanity check |

---

## Explicitly out of v1 contract

- Border and image render commands (v1.1 once core loop is green)
- Floating elements, transitions, aspect-ratio, custom data
- Gamepad / multi-touch beyond primary pointer
- Editor-only tooling
- Overlay-camera render backend (canvas path only unless canvas gate fails)
- ECS / Entities

---

## Open follow-ups (not this ticket)

- Canvas batching prototype (`OnPopulateMesh` perf validation)
- Clipping/masking batch-key details with canvas scissor
- Localization integration
- Full written spec + phased roadmap (destination artifact)
