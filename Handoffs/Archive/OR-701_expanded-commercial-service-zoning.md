# OR-701 — Expanded Commercial & Service Zoning

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-20

## Objective

Add retail shops, clinics, maintenance workshops, and security stations to the player-facing building catalog without introducing direct resident assignment.

## Progress

- Added the pure-C# `BuildingCatalog`, which owns stable room content IDs, footprints, and capacities.
- Wired `GridPlacementController` to dispatch catalogued zones as existing `BuildRoomCommand` instances.
- Added the four zones to the Build palette and expanded its pointer exclusion region to cover the third palette row.
- Added focused placement tests for each catalogued zone's content ID, footprint, and capacity.

## Validation

| Check | Result |
| --- | --- |
| `git diff --check` | PASS |
| Unity batch compilation | PASS — no C# compiler errors reported |
| Focused EditMode tests | PASS — 30/30 `GridPlacementControllerTests` passed via the one-shot Unity runner |

## Recovery performed

No live Editor process owned the project. Confirmed stale session locks were preserved under `/tmp/one-roof-stale-locks/`; do not restore them into the project. The CLI's Pipeline discovery still cannot retain a one-shot batch endpoint, so validation used the installed Editor's one-shot runner instead.

## Next safe action

Begin OR-702: model businesses as domain state, lease catalogued commercial/service rooms, and match residents through the existing soft specialist roles without direct assignment.
