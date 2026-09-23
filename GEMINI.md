# One Roof — Gemini Instructions

Follow [AGENTS.md](./AGENTS.md) as the authoritative shared project guide.

## North Star
Achieve the **Beta Boundary (City Status)**: 30 floors, 300 persistent residents, physical utilities, 4 factions, policy decrees, 8 data overlays, 6 adaptive crisis chains, and sustaining City Status for 30 in-game days under strict performance budgets (<4 ms tick, 60 FPS presentation).

## Reading Order
1. [`AGENTS.md`](./AGENTS.md) & [`CONTEXT.md`](./CONTEXT.md)
2. [`Docs/01_GAME_VISION.md`](./Docs/01_GAME_VISION.md) & [`Docs/02_ARCHITECTURE.md`](./Docs/02_ARCHITECTURE.md)
3. [`Docs/03_DATA_CONTRACTS.md`](./Docs/03_DATA_CONTRACTS.md) & [`Docs/04_UX_CONTRACT.md`](./Docs/04_UX_CONTRACT.md)
4. [`Docs/09_CAUSE_CHAIN_INSPECTOR.md`](./Docs/09_CAUSE_CHAIN_INSPECTOR.md) & [`Docs/12_ECONOMY.md`](./Docs/12_ECONOMY.md)
5. Active task in [`Planning/BACKLOG.md`](./Planning/BACKLOG.md) and [`Handoffs/Active/`](./Handoffs/Active/)

## Core Boundaries & Invariants
- **Domain Purity**: `OneRoof.Domain` is pure C# (`noEngineReferences: true`, 0 UnityEngine references).
- **Projections Only**: Presentation and UI read immutable snapshots/projections; UI dispatches commands, never directly mutates simulation records.
- **Cause Chain**: Symptom → overlay → inspector cause → systems response → measured feedback. No direct resident micro-management.
- **Validation**: Headless Unity 6000.3.24f1 batchmode runs in isolated worktrees. Never pass `-quit` with `-runTests`. See [`Docs/10_DEVELOPMENT_WORKFLOW.md`](./Docs/10_DEVELOPMENT_WORKFLOW.md).
- **Handoff Hygiene**: Maintain exactly one active handoff in `Handoffs/Active/`. Move completed handoffs to `Handoffs/Archive/`. Always commit paired `.meta` files.

