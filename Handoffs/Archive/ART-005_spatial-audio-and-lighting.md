# ART-005 — Spatial Audio & Environmental Lighting Atmosphere

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-20

## Objective

Add a projection-driven atmosphere layer to the tower without putting presentation state in the simulation.

## Changes made

- Added `TowerAtmospherePresenter`, owned by `TowerPlayableController`.
- Room topology creates one spatial, looping procedural roomtone per non-transit room and a matching warm window-light volume.
- Elevator projections position and modulate mechanical foley; walking resident projections position footstep foley.
- Topology synchronization is cached by topology reference and room count, avoiding per-frame allocation and object churn during steady-state presentation.
- Added focused EditMode coverage for roomtone/window wiring and projection-to-footstep placement.
- Added `Docs/AI/UnityProjectContext.md` from the Unity onboarding workflow.

## Validation

| Check | Result |
| --- | --- |
| Static diff | `git diff --check` — PASS |
| Unity Pipeline availability | `unity pipeline list` confirms an open, non-Safe-Mode editor, but its Pipeline server is unreachable |
| Unity compile and EditMode tests | NOT RUN — `unity command recompile` cannot contact the unreachable Pipeline server |
| Batch-mode fallback | NOT RUN — Unity correctly refused a second instance because this project is already open |

## Known risks or failures

- Procedural tone clips are intentionally placeholder content. Replace them with Addressables-backed authored clips when the audio-content pipeline is scheduled; their source placement and projection contracts can remain unchanged.
- Runtime visual/audio verification remains required once the Editor Pipeline server is reachable.

## Next safe action

Restore the Unity Pipeline server, compile, run the Presentation EditMode assembly, and inspect `Tower` in Play Mode. Then continue with OR-605 (soft specialist roles and training capacity).
