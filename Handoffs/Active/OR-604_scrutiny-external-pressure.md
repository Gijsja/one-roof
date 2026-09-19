# OR-604 — Scrutiny External-Pressure Resource

**Status:** ACTIVE — validation blocked
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
| Focused Edit Mode suite | `unity test /home/geisha/Vibecode/UnityAI/one-roof --mode EditMode --filter "OneRoof.Application.Tests.EditMode.PopulationOverlayServiceTests;OneRoof.Domain.Tests.EditMode.ScrutinyStateTests;OneRoof.Presentation.Tests.EditMode.PopulationOverlayPresenterTests;OneRoof.Presentation.Tests.EditMode.ScrutinyOverlayPresenterTests;OneRoof.Presentation.Tests.EditMode.TowerPlayableControllerTests" --output /tmp/or603-or604-editmode-results.xml --timeout 300 --format json` | BLOCKED — Unity starts after stale-lock removal, then exits before compilation/tests because `LicenseClient-geisha` is unavailable and no X display is available. |

## Known risks or failures

- Neither OR-603 nor OR-604 can be marked done until the target suite runs on a licensed Unity host.
- The prior `Temp/UnityLockfile` had no owning process and was moved to `/tmp/one-roof-stale-UnityLockfile-20260919`; it is recoverable there.

## Next safe action

Restore the Unity license client / graphical session (or use a licensed headless runner), run the focused command above, then update the two backlog statuses from `ACTIVE` after passing results.
