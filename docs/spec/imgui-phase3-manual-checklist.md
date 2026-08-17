# Basic.ImGui — Phase 3 manual checklist (Tier 4)

Use scene `Assets/Scenes/ImGuiDebugHudDemo.unity` in **Editor Play mode** at **1920×1080**.

## uGUI compositing

1. Enter Play mode — demo bootstrap creates three canvas layers:
   - **Back** (sort order 0): large blue panel behind everything.
   - **ImGui HUD** (sort order 10): stats, interactive, benchmark, compositing panels.
   - **Front** (sort order 20): gold strip along the top edge.
2. Confirm the blue back panel is visible **behind** ImGui panels.
3. Confirm the gold front strip draws **on top of** ImGui content (top ~12% of screen).
4. ImGui text and panels remain readable between the two uGUI layers.

## Frame Debugger draw count

1. Open **Window → Analysis → Frame Debugger**.
2. Enable recording and capture a frame after **60+ frames** of warmup.
3. Locate ImGui mesh draws (`ImGuiGraphic` / `ImGuiUpload` mesh).
4. For the 8k stress rects, expect **1 batch** (same material) in the benchmark panel; total canvas draws should stay **≤ 16** per perf gate.

## Profiler sanity (optional)

1. Open **Profiler** → CPU / Memory.
2. After warmup, verify **GC Alloc** on the ImGui hot path is **0 B** per frame.
3. Combined layout + render time shown in the Live Stats panel should be **≤ 2 ms** on target hardware.

## Sign-off

| Check | Pass |
| --- | --- |
| Back uGUI visible behind ImGui | ☐ |
| Front uGUI visible above ImGui | ☐ |
| Frame Debugger batch count sane (≤ 16) | ☐ |
| HUD perf gates green after warmup | ☐ |
