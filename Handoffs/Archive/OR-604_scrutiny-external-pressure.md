# OR-604 — Scrutiny External-Pressure Resource

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-19
**Updated:** 2026-09-19

## Objective

Create deterministic tower-level Scrutiny state that makes external pressure explainable, projects it to Data mode, and constrains expansion at high pressure.

## State at handoff

`ScrutinyState` is pure Domain state owned by `TowerSimulation`. It tracks value, trend, recent construction, inequality, unresolved grievances/strain, a forward-compatible aggressive-policy input, service/capacity relief, external-event pressure, and the expansion constraint threshold. It is persisted in `TowerSaveData`, surfaced by immutable `ScrutinyOverlayProjection`, and linked through a Data-mode overlay to a cause-chain card.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Static diff | `git diff --check` | PASS |
| Recompile | `unity command recompile --project-path /home/geisha/Vibecode/UnityAI/one-roof` | PASS — up to date |
| Full Edit Mode suite | `unity command run_tests --project-path /home/geisha/Vibecode/UnityAI/one-roof --mode editor` | PASS — 318 / 318 (9.62s) |

## Known risks or failures

- None known.

## Next safe action

Proceed to `ART-005` or `OR-605`.
