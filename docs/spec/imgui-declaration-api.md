# Basic.ImGui — declaration API ergonomics

Decision for [wayfinder #13](https://github.com/MykytaRachynskyi/unity_basic/issues/13).  
Inputs: [Clay architecture](research/clay-architecture.md), [Burst flex layout](research/burst-flex-layout.md), [layout↔renderer boundary](imgui-layout-renderer-boundary.md), [v1 feature contract](imgui-v1-feature-contract.md).

---

## Decision summary

v1 uses **scoped block builders** on `ImGuiContext` — the C# substitute for Clay's `CLAY()` macros. Tree structure is declared with `using` scopes and plain C# control flow. Configuration is **struct-first** (`ElementDeclaration`, `TextConfig`) with **optional fluent overrides** on the scope token. **No source generators** and **no fluent tree nesting** in v1.

---

## Paradigm

| Approach | v1 |
| --- | --- |
| Scoped block builders (`using` + open/close stack) | **Primary** |
| Fluent chaining for tree structure | **Rejected** |
| Source generators / declarative DSL | **Deferred** (v1.1+ experiment only) |
| Fluent config overrides on open scope | **Optional convenience** |

**Why:** Clay's immediate-mode model depends on nesting, loops, and `if` inside declaration blocks. Burst research splits managed declaration from Burst resolution — the builder must stay managed and idiomatic; only `EndLayout` runs under Burst. Fluent tree APIs hide control flow and encourage retained-style thinking. Source generators add compile-time complexity without helping per-frame re-declaration.

---

## Core types

| Type | Role |
| --- | --- |
| `ImGuiContext` | Entry point: frame lifecycle + `Element()` / `Text()` |
| `ElementScope` | `ref struct` scope token; `using` calls `CloseElement` |
| `ElementDeclaration` | Blittable open-time config (layout, color, clip, scroll) |
| `TextConfig` | Font id, size, color, wrap, letter spacing |
| `ElementId` | `readonly struct` — Clay-compatible hashed id (`uint` + optional offset/base) |

---

## `ElementId`

Clay-parity macros as static factories (runtime hash, **no source generator in v1**):

```csharp
ElementId.From("Sidebar")           // CLAY_ID
ElementId.Indexed("Item", index)      // CLAY_IDI
ElementId.Local("Label")              // CLAY_ID_LOCAL — parent-seeded hash
```

- Hash algorithm ports Clay `Clay__HashString` / `Clay__HashStringWithOffset` for cross-reference compatibility.
- `ReadOnlySpan<char>` overloads avoid unnecessary string allocations when callers already hold spans.
- `ElementId.Auto()` — debug-only; unstable when tree shape changes (Clay `CLAY_AUTO_ID` parity). Not for scroll/pointer ids.
- Dynamic runtime strings: `ctx.GetElementId(string)` pre-registers id before open (Clay `GetElementId`).

Duplicate ids in one layout → dev error (Clay parity).

---

## Opening elements

```csharp
ElementScope Element(ElementId id, ElementDeclaration declaration);
ElementScope Element(ElementId id);  // defaults to ElementDeclaration.Empty
```

`ElementScope` is a **`ref struct`** implementing disposal → `CloseElement`. Zero per-scope GC in release builds.

Optional fluent overrides on the scope (mutate the open element's declaration before children):

```csharp
using (var panel = ctx.Element(ElementId.From("Panel"), ElementPresets.Panel))
{
    panel.Padding(8).ChildGap(4);   // fluent overrides — struct fields updated on open record
    // children ...
}
```

Fluent overrides are **config-only** — they do not open nested elements.

Static presets (`ElementPresets.Panel`, `ElementPresets.ScrollVertical`, etc.) are named `ElementDeclaration` values for common HUD patterns.

---

## Text

Text is a leaf declaration — not a container scope:

```csharp
void Text(ElementId id, ReadOnlySpan<char> text, TextConfig config);
void Text(ElementId id, string text, TextConfig config);  // convenience
```

Measurement runs on the main thread during declaration via `ITextMeasurer` (see layout↔renderer boundary). `Text()` writes into the arena and triggers measure/cache fill before `EndLayout`.

---

## Interaction during declaration

`OnHover` attaches at open time:

```csharp
using (ctx.Element(id, decl.OnHover(OnStatsHover))
```

or

```csharp
using (var scope = ctx.Element(id, decl))
{
    scope.OnHover(OnStatsHover);
```

Callback shape: `void (ElementId id, PointerData data)` — no managed `object` userData in v1 (deferred with custom elements).

Pointer queries after `EndLayout`: `TryGetHoveredId`, pressed/released-this-frame on `ImGuiContext` (see v1 feature contract).

---

## Canonical HUD example

```csharp
void BuildDebugHud(ImGuiContext ctx, float fps, IReadOnlyList<string> logLines)
{
    using (ctx.Element(ElementId.From("HudRoot"), ElementPresets.FullScreen))
    {
        using (ctx.Element(ElementId.From("Stats"), ElementPresets.Panel))
        {
            ctx.Text(ElementId.From("Fps"), $"FPS: {fps:F0}", TextPresets.Stat);
        }

        using (var scroll = ctx.Element(ElementId.From("Log"), ElementPresets.ScrollVertical))
        {
            scroll.Padding(4).ChildGap(2);

            for (int i = 0; i < logLines.Count; i++)
            {
                using (ctx.Element(ElementId.Indexed("LogLine", i), ElementPresets.Row))
                {
                    ctx.Text(ElementId.Local("Text"), logLines[i], TextPresets.Body);
                }
            }
        }
    }
}
```

Plain `for` / `if` / local functions inside scopes are the intended style — same as Clay C examples.

---

## What v1 does not ship

| Item | When |
| --- | --- |
| Source generators (UI tree, id tables) | v1.1+ if ergonomics gap remains |
| Fluent nested tree builders (`Panel().Child(...)`) | Rejected |
| `ref struct` scope pooling | Unnecessary — ref structs are stack-only |
| Manual `OpenElement` / `CloseElement` without `using` | Internal only; not public API |

---

## Implementation notes

- Declaration phase: managed, main thread, arena bump — profile separately from Burst resolve.
- `ElementScope` must not escape the `using` block (ref struct safety).
- Validation (duplicate id, unclosed element): dev-only throws or `Clay__Error` buffer parity — release builds lean toward assert/minimal checks per boundary spec.
