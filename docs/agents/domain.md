# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Workspace vs package repo

This is a Unity package (`com.arcticlime.unitybasic`) checked out inside a larger Unity project workspace.

- **Workspace root** — the Unity project (scenes, project settings, test assets outside the package).
- **Package repo root** (`Assets/Basic/`) — git root, `package.json`, `Runtime/`, `Editor/`. All paths below are relative to this directory.

When a skill says "repo root", it means `Assets/Basic/`.

## Before exploring, read these

- **`CONTEXT.md`** at the package repo root (`Assets/Basic/CONTEXT.md`), or
- **`CONTEXT-MAP.md`** at the package repo root if it exists — it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`docs/adr/`** — read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

Single-context repo:

```
Assets/Basic/                    ← package repo root (git root)
├── CONTEXT.md
├── docs/adr/
│   ├── 0001-….md
│   └── 0002-….md
├── Runtime/
└── Editor/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders) — but worth reopening because…_
