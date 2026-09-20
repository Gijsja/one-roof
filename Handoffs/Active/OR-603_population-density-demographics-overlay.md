# OR-603 — Population Density & Demographic Distribution Overlay

**Status:** DONE
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
| Recompile | `unity command recompile --project-path /home/geisha/Vibecode/UnityAI/one-roof` | PASS — up to date |
| Edit Mode | `unity command run_tests --project-path /home/geisha/Vibecode/UnityAI/one-roof --mode editor` | PASS — 318 / 318 |

## Known risks or failures

- None known.

## Next safe action

Proceed to `ART-005` or `OR-605`.

## References

- Backlog: `OR-603`
- Roadmap: `M6.2`
- Decisions: `ADR-056`
