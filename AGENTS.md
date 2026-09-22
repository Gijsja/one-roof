# One Roof — Agent Guide

## Project

One Roof is a vertical-city simulation: the Steward changes systems, while autonomous residents respond. The five-floor, fifty-resident elevator-congestion loop is the baseline proof.

## Start here

Read the relevant task in `Planning/BACKLOG.md`, its latest handoff, and the canonical docs that affect the work:

- `Docs/01_GAME_VISION.md` — product intent and scope
- `Docs/02_ARCHITECTURE.md` and `Docs/03_DATA_CONTRACTS.md` — runtime boundaries and state
- `Docs/04_UX_CONTRACT.md` and `Docs/09_CAUSE_CHAIN_INSPECTOR.md` — overlays, inspectors, and responses
- `Docs/05_ASSET_PIPELINE.md` or `Docs/06_TEST_STRATEGY.md` when applicable

Treat numbered docs as canonical. `Docs/review/` is only a temporary merge inbox.

## Working principles

- Players influence systems, never individual residents directly.
- Important failures should connect symptom → overlay → cause → systems-level response.
- Keep simulation state independent of Unity views; presentation reads projections and binds them by stable IDs.
- Preserve the pure-C# Domain boundary and avoid scene references in save data.
- Keep beta work focused on the planned slice. Record meaningful architecture choices in `Docs/07_DECISION_LOG.md`.
- Tone is optimistic but fragile: clear about hardship, never cartoon-villain or hopeless.

## Unity workflow

- Target **Unity 6000.3 LTS**; the installed project version is `6000.3.24f1` (`ProjectSettings/ProjectVersion.txt`).
- This project uses a **headless Unity workflow**. Prefer one-shot `-batchmode -nographics -quit` runs and the `unity` CLI (`unity test`, `unity pipeline`, `unity command`) for validation; `com.unity.pipeline` is installed. Validate from an isolated worktree when no connected editor serves it, and never hijack another session's connected editor for heavy runs. See `Docs/10_DEVELOPMENT_WORKFLOW.md` for the required compile and test commands.
- Validate from an isolated worktree with the installed Unity executable in one-shot `-batchmode -nographics -quit` runs. See `Docs/10_DEVELOPMENT_WORKFLOW.md` for the required compile and test commands.
- Use the Package Manager API for package changes. Prefer Unity tooling over hand-editing serialized Unity assets.
- Keep generated Unity folders and secrets out of version control; include `.meta` files with new assets.

## Finish well

Keep changes scoped, preserve unrelated work, run the relevant validation, and write a concise handoff with commands, results, risks, and the next safe action. For documentation-only changes, state that Unity validation was not run.
