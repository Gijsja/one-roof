# One Roof — Agent Guide (v0.10)

## Project & North Star

One Roof is a deterministic vertical-city simulation: the Steward shapes systems (architecture, zoning, infrastructure, leases, decrees), while autonomous residents respond.

- **Baseline (v0.10 Prototype Complete)**: 765 passing tests (754 EditMode, 11 PlayMode), 30-floor / 300-resident reference scale proven in `Tower_GoldStandard30.unity` at ~108 FPS and 171 draw calls, closed-loop unit economics, 8 data overlays, 4 factions, 12-month weather cycle, and secret undercity expansion.
- **North Star (Beta Boundary / City Status)**: Sustaining City Status for 30 consecutive in-game days under strict performance budgets (<4.0 ms tick, 60 FPS presentation), 6 adaptive crisis chains, and campaign board pacing.

---

## Canonical Vocabulary

- **Tower**: An enormous mixed-use vertical building the Steward constructs floor by floor. (*Avoid*: building, city, level)
- **Steward**: The player role: developer, mayor, and systems designer shaping structure, services, leases, policy, and capacity. (*Avoid*: player, mayor, user)
- **Resident**: An autonomous person living and working in the Tower, governed by needs, satisfaction, grievances, and strain. (*Avoid*: NPC, agent, citizen, customer)
- **Cause-chain**: The explanation contract: world symptom → overlay → inspector cause → systems-level response → measured feedback. (*Avoid*: alert chain, diagnostic flow)

---

## Agent Context Shield: Read on Demand

> [!IMPORTANT]
> **DO NOT read all files in `Docs/` or `Planning/`.** Ingesting the entire documentation suite wastes over 50,000 tokens of context.
> Check the active task in [`Planning/BACKLOG.md`](./Planning/BACKLOG.md) and [`Handoffs/Active/`](./Handoffs/Active/), then consult **only the single canonical doc** relevant to your task:
> - **Architecture & Assembly boundaries**: [`Docs/02_ARCHITECTURE.md`](./Docs/02_ARCHITECTURE.md)
> - **Entity Records & Projections**: [`Docs/03_DATA_CONTRACTS.md`](./Docs/03_DATA_CONTRACTS.md)
> - **Overlays, Mode Shell & Previews**: [`Docs/04_UX_CONTRACT.md`](./Docs/04_UX_CONTRACT.md)
> - **Rig, Wardrobe & Assets**: [`Docs/05_ASSET_PIPELINE.md`](./Docs/05_ASSET_PIPELINE.md) & [`Docs/08_ASSET_CATALOG.md`](./Docs/08_ASSET_CATALOG.md)
> - **Testing & Performance Budgets**: [`Docs/06_TEST_STRATEGY.md`](./Docs/06_TEST_STRATEGY.md)
> - **Active Architecture Decisions**: [`Docs/07_DECISION_LOG.md`](./Docs/07_DECISION_LOG.md) (ADR-070+)
> - **Cause-Chain Inspector UX**: [`Docs/09_CAUSE_CHAIN_INSPECTOR.md`](./Docs/09_CAUSE_CHAIN_INSPECTOR.md)
> - **Headless Batchmode & Commands**: [`Docs/10_DEVELOPMENT_WORKFLOW.md`](./Docs/10_DEVELOPMENT_WORKFLOW.md)
> - **Closed-Loop Economy & Decrees**: [`Docs/12_ECONOMY.md`](./Docs/12_ECONOMY.md)

---

## Core Boundaries & Invariants

1. **Systems over individuals**: Players influence systems (zoning, capacity, leases, decrees), never individual residents directly.
2. **Cause-chain explanation**: Every failure connects: `Symptom → Overlay → Inspector Cause → Systems Response → Measured Feedback`.
3. **Pure-C# Domain purity**: `OneRoof.Domain` and `OneRoof.Application` strictly maintain `noEngineReferences: true` (0 `UnityEngine` references).
4. **Projections only**: Presentation and UI consume immutable projections bound by stable entity IDs. UI dispatches validated commands via `TowerSimulation.CanExecute()`, never directly mutating simulation state.
5. **No per-agent Update loops**: Simulation runs on a fixed discrete tick (1440 ticks/day). Views are pooled and presentation-interpolated.
6. **Aggregate persistence**: All serialization lives in domain sub-aggregates (`ToSaveData()` / `FromSaveData()`). Zero scene references in saves.
7. **Strict Handoff Hygiene**: Maintain **strictly ONE active handoff** in `Handoffs/Active/`. When accepted or completed, move prior handoffs to `Handoffs/Archive/`.

---

## Tooling & Validation Workflow

- Target **Unity 6000.3 LTS** (`6000.3.24f1`).
- **Headless Validation**: Validate from an isolated worktree via batchmode. **Never pass `-quit` with `-runTests`**. See [`Docs/10_DEVELOPMENT_WORKFLOW.md`](./Docs/10_DEVELOPMENT_WORKFLOW.md).
- **Python & Pipeline Tooling**: When Unity Editor runs on port 7800 with `com.unity.pipeline`, Python scripts (e.g. `pipeline_client.py` or MCP clients) provide live queries:
  - `python3 pipeline_client.py cmd get_performance_stats`: queries draw calls, batches, frame times.
  - `python3 pipeline_client.py cmd console_status`: queries console errors.
  - `python3 pipeline_client.py eval "<expression>"`: evaluates C# state.
- **Commit hygiene**: Always commit paired `.meta` files for any new assets. Keep generated directories (`Library/`, `Temp/`, `.plastic/`) untracked.
- For documentation-only changes, state that Unity validation was not run.
