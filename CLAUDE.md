# One Roof — Claude Code Instructions

Always read and strictly follow [AGENTS.md](./AGENTS.md) as the primary instruction source.

## Mission
Build a vertical-city simulation in Unity 6 LTS. The first proof is five floors and fifty residents.

## Required Reading Order
1. `Docs/01_GAME_VISION.md`
2. `Docs/02_ARCHITECTURE.md`
3. `Docs/03_DATA_CONTRACTS.md`
4. The active task in `Planning/BACKLOG.md`
5. The latest relevant file in `Handoffs/Active/`

## Architecture Boundaries
- **Domain (`OneRoof.Domain`)**: Pure C#. No UnityEngine, scenes, prefabs, or UI.
- **Application (`OneRoof.Application`)**: Ports, use cases, read-only projections. No concrete Unity views (`noEngineReferences: true`).
- **Infrastructure (`OneRoof.Infrastructure`)**: Serialization, persistence, adapters.
- **Presentation (`OneRoof.Presentation`)**: Consumes Application projections. Pooled NPC views (40-view cap), no mutable Domain internals.

## Work Protocol
- Before editing: Check task ID, acceptance criteria, and `Handoffs/Active/`.
- During work: Keep tasks narrow, preserve unrelated changes, record decisions in `Docs/07_DECISION_LOG.md`.
- After work: Record handoff in `Handoffs/Active/<TASK-ID>_<short-name>.md`. Always commit `.meta` files with new assets.
