# OR-603 — Population Density & Demographic Distribution Overlay

**Status:** ACTIVE — validation blocked
**Owner:** Codex
**Started:** 2026-09-19
**Updated:** 2026-09-19

## Objective

Deliver Overlay 3: a non-colour population view that makes floor density and age/resource demographics legible, then routes a selected floor into the cause-chain inspector.

## Acceptance criteria

- [x] Every tower floor exposes resident count, capacity-derived density, and a textual density tier.
- [x] Age bands and household resource bands are deterministically projected without presentation state.
- [x] Labels carry all overlay meaning without relying on colour.
- [x] Each floor provides an explicit Inspect action that opens a focused immutable detail card with systems-level levers.

## Scope and ownership

- `Assets/OneRoof/Runtime/Application/Overlays/PopulationOverlay*`
- `Assets/OneRoof/Runtime/Presentation/Overlays/PopulationOverlayPresenter.cs`
- `Assets/OneRoof/Runtime/Application/Inspectors/TowerInspectionService.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerDashboardHudView.cs`
- Edit Mode tests, backlog/roadmap/decision log.

## State at handoff

The Population HUD action enters Data mode with `overlay:population`. The overlay reports every floor’s resident count, capacity-based density tier, three age cohorts, and three household-resource bands in text. Its per-floor Inspect action switches to Inspect mode and opens a `Floor N Population` card with the same immutable evidence and capacity/leasing/service response path.

## Decisions

- **ADR-056:** Age cohorts are stable roster metadata derived from resident IDs; resource tiers expose existing household budget state, avoiding a second mutable demographic model or invented wage values.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Static diff | `git diff --check` | PASS |
| Targeted Edit Mode tests | `unity test /home/geisha/Vibecode/UnityAI/one-roof --mode EditMode --filter "OneRoof.Application.Tests.EditMode.PopulationOverlayServiceTests;OneRoof.Presentation.Tests.EditMode.PopulationOverlayPresenterTests;OneRoof.Presentation.Tests.EditMode.TowerPlayableControllerTests" --output /tmp/or603-editmode-results.xml --timeout 300 --format json` | NOT RUN — Unity reports the project is already open, while its Pipeline server is unavailable for in-editor execution. |
| Pipeline preflight | `unity pipeline list --format json` | PASS — running editor detected; Pipeline server unreachable; Safe Mode not detected. |

## Known risks or failures

- The targeted tests need one run once the currently open editor exposes its Pipeline server or is closed, allowing the batch runner to acquire the project.
- Existing unrelated modification: `ProjectSettings/ProjectSettings.asset` was present before OR-603 work and was not changed intentionally.

## Next safe action

Restore the active Unity editor’s Pipeline connection (or close it), run the targeted Edit Mode command above, then proceed to `ART-005` or `OR-604`.

## References

- Backlog: `OR-603`
- Roadmap: `M6.2`
- Decisions: `ADR-056`
