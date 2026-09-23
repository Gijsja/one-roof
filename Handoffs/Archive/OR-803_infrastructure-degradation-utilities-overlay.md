# OR-803 — Infrastructure degradation, technician response & Utilities overlay

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-21
**Updated:** 2026-09-21

## Objective

Add deterministic utility-equipment wear, autonomous maintenance-specialist repair, and the eighth Data-mode overlay that connects electrical, water, waste, and equipment failures to a systems-level response.

## Acceptance criteria

- [x] Utility equipment condition wears deterministically and becomes a repairable failure.
- [x] Maintenance specialists repair failed equipment without direct resident commands.
- [x] Overlay 8 exposes per-floor power, water, waste, failed-equipment count, and text causes.
- [x] The inspector provides the symptom → cause → systems-level response chain.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs` — authoritative, pure-C# operational condition for topology-installed utility equipment.
- `Assets/OneRoof/Runtime/Application/Overlays/UtilitiesOverlayService.cs` — immutable combined electrical, plumbing, waste, and equipment-condition projection.
- `Assets/OneRoof/Runtime/Presentation/Overlays/UtilitiesOverlayPresenter.cs` — accessible Utilities Data-mode view and inspector entry point.
- `TowerSimulation`, `TowerSimulationSession`, `TowerInspectionService`, and `TowerPlayableController` — simulation tick integration and presentation wiring.
- Focused Domain, Application, and Presentation EditMode tests cover wear/failure, autonomous repair, overlay evidence, inspector text, and presenter state.

## Decisions

- Physical connectivity remains owned by OR-801/OR-802 topology evaluators. This task adds only mutable operational condition, avoiding a duplicate utility-network authority.
- A maintenance specialist is the autonomous repair capacity; the Steward can create conditions for that role through maintenance/training space but cannot target a resident.
- Overlay labels intentionally include values and named causes, so their meaning does not depend on colour.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Static diff | `git diff --check` | PASS |
| Compile | `Unity -batchmode -nographics -quit -projectPath /tmp/one-roof-or803-validation -logFile /tmp/one-roof-or803-compile.log` | PASS — `Tundra build success`, 1,349 items evaluated |
| Edit Mode | `Unity -batchmode -nographics -quit -projectPath /tmp/one-roof-or803-validation -runTests -testPlatform EditMode -testResults /tmp/one-roof-or803-editmode.xml -logFile /tmp/one-roof-or803-editmode.log` | COMPILED; Unity 6000.3.24f1 honored `-quit` before starting the test runner, so no XML was produced. No C# errors reported. |
| Play Mode | Not run | Presentation is covered by EditMode state tests; no scene/prefab asset changed. |

## Known risks or failures

- Operational condition is currently runtime-session state rather than save data. Persist it with the next save-schema version before utility failures are intended to survive save/load.
- The documented batch test limitation prevents an actual pass-count claim.

## Next safe action

Begin OR-901: introduce the aggregation-safe relationship graph and faction archetypes, using OR-602 wellbeing/grievances and OR-702 employment data as inputs.

## References

- Backlog: OR-803
- Prerequisites: `Handoffs/OR-801_ELECTRICAL_GRID.md`, `Handoffs/OR-802_WATER_WASTE_NETWORKS.md`
- Evidence: `/tmp/one-roof-or803-compile.log`, `/tmp/one-roof-or803-editmode.log`
