# One Roof — Agent Guide

## Project & North Star

One Roof is a deterministic vertical-city simulation: the Steward shapes systems (architecture, zoning, infrastructure, leases, decrees), while autonomous residents respond.

**North Star**: Achieve the **Beta Boundary (City Status)**: 30 floors, 300 persistent residents, physical utilities, 4 factions, policy decrees, 8 data overlays, 6 adaptive crisis chains, and sustaining City Status for 30 in-game days under strict performance budgets (<4 ms tick, 60 FPS presentation).

## Start here

Read the relevant task in `Planning/BACKLOG.md`, its active handoff in `Handoffs/Active/`, and the canonical docs that affect the work:

- `Docs/01_GAME_VISION.md` — product intent and scope
- `Docs/02_ARCHITECTURE.md` and `Docs/03_DATA_CONTRACTS.md` — runtime boundaries and state
- `Docs/04_UX_CONTRACT.md` and `Docs/09_CAUSE_CHAIN_INSPECTOR.md` — overlays, inspectors, and responses
- `Docs/12_ECONOMY.md` — closed-loop unit economics and cash conservation
- `Docs/05_ASSET_PIPELINE.md` or `Docs/06_TEST_STRATEGY.md` when applicable

Treat numbered docs as canonical. `Docs/review/` is only a temporary merge inbox; historical docs live in `Docs/Archive/`.

## Working principles

- Players influence systems, never individual residents directly.
- Important failures must connect: symptom → overlay → cause → systems-level response → measured feedback.
- Keep simulation state independent of Unity views; presentation reads immutable projections and binds them by stable IDs.
- Preserve the pure-C# Domain boundary (`noEngineReferences: true`, 0 UnityEngine references).
- Avoid scene references or view state in save data; all serialization lives in domain aggregates.
- Keep beta work focused on the planned slice. Record meaningful architecture choices in `Docs/07_DECISION_LOG.md`.
- Tone is optimistic but fragile: clear about hardship, never cartoon-villain or hopeless.

## Unity workflow

- Target **Unity 6000.3 LTS**; the installed project version is `6000.3.24f1` (`ProjectSettings/ProjectVersion.txt`).
- This project uses a **headless Unity workflow**. Prefer one-shot `-batchmode -nographics -quit` runs and the `unity` CLI (`unity test`, `unity pipeline`, `unity command`) for validation; `com.unity.pipeline` is installed.
- Validate from an isolated worktree when no connected editor serves it, and never hijack another session's connected editor for heavy runs. See `Docs/10_DEVELOPMENT_WORKFLOW.md` for the required compile and test commands.
- Use the Package Manager API for package changes. Prefer Unity tooling over hand-editing serialized Unity assets.
- Keep generated Unity folders (`.plastic/`, `Library/`, `Temp/`) and secrets out of version control; commit paired `.meta` files with all new assets.

## Finish well

- Keep changes scoped, preserve unrelated work, and run the relevant headless validation.
- Maintain **one active handoff** per task in `Handoffs/Active/`. When accepted or completed, move prior handoffs to `Handoffs/Archive/`.
- Write concise handoffs with commands, exit codes, test totals, risks, and the next safe action.
- For documentation-only changes, state that Unity validation was not run.
