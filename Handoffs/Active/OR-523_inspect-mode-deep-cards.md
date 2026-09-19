# OR-523 — Inspect Mode Deep Cards

**Status:** DONE  
**Owner:** Codex  
**Started:** 2026-09-19  
**Updated:** 2026-09-19

## Objective

Clicking residents, rooms, and elevator banks in Inspect mode opens comprehensive, read-only symptom/cause drill-down cards backed by application projections.

## Acceptance criteria

- [x] Resident cards show current movement/activity, household, home/work rooms, needs, traits, and wait time where applicable.
- [x] Room cards show type, floor, footprint, current occupancy/capacity, portals, and interaction-point counts.
- [x] Elevator-bank cards show car state, queue counts by floor, and average wait using `ElevatorBankSnapshot`.
- [x] Inspect selection opens the appropriate card and cancellation/mode changes close it.
- [x] Dedicated Application, UI, and selection-bridge Edit Mode tests were added.
- [x] Unity recompiles with no errors; focused Application, UI, and Presentation Edit Mode coverage passes through the live Pipeline.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/Application/Inspectors/`
- `Assets/OneRoof/Runtime/UI/Inspectors/`
- `Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs`
- matching Edit Mode tests, decision log, backlog, and this handoff

Do not modify:

- mutable Domain state, topology, transit execution, scenes, or prefabs

## State at handoff

`InspectSelectionController` now routes selected residents, rooms, and the elevator shaft into a generic `DeepInspectionCardView`. `TowerInspectionService` derives immutable `InspectorDetailProjection` instances from `TowerSimulationSession`; its elevator path reads `ElevatorBank.Snapshot()` rather than queue internals.

## Changes made

- `Assets/OneRoof/Runtime/Application/Inspectors/InspectorDetailProjection.cs` — immutable card projection contract.
- `Assets/OneRoof/Runtime/Application/Inspectors/TowerInspectionService.cs` — resident, room, and elevator-bank projection builder.
- `Assets/OneRoof/Runtime/UI/Inspectors/DeepInspectionCardView.cs` — reusable immediate-mode detail card.
- `Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs` — opens and closes cards on selection lifecycle.
- `Assets/OneRoof/Tests/EditMode/Application/TowerInspectionServiceTests.cs` — projection coverage.
- `Assets/OneRoof/Tests/EditMode/UI/DeepInspectionCardViewTests.cs` — card lifecycle coverage.
- `Assets/OneRoof/Tests/EditMode/Presentation/InspectSelectionControllerTests.cs` — selection-to-card bridge coverage.
- `Docs/07_DECISION_LOG.md` — ADR-045.
- `Planning/BACKLOG.md` — OR-523 marked `DONE` after live-Pipeline validation.

## Decisions

- ADR-045: UI receives immutable application projections, not mutable domain records. Elevator detail uses the ARCH-005 snapshot seam.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Pipeline preflight | `unity pipeline list --format json`, `unity status --project-path /home/geisha/Vibecode/UnityAI/one-roof --format json`, `unity command editor_status --project-path /home/geisha/Vibecode/UnityAI/one-roof --caller plugin --skill unity-pipeline --format json` | PASS — one ready Editor at port 7800; not compiling or reloading |
| Compile | `unity command set_autotick --enable true ...`; `unity command recompile ...`; `unity command recompile_status ...` | PASS — completed, `failed: false`, no compilation errors |
| Focused Edit Mode | Six `unity command run_tests --mode editor --filter <exact test name> ...` invocations for new resident/room/elevator/UI/selection tests | PASS — 6 / 6 |
| UI assembly | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.UI.Tests.EditMode ...` | PASS — 10 / 10 |
| Presentation assembly | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Presentation.Tests.EditMode ...`, plus final added resident/room selection test | PASS — 105 / 105 plus 1 / 1 |
| Application assembly | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Application.Tests.EditMode ...` | PASS — 28 / 28 after the follow-up golden-cost correction |
| Static checks | `git diff --check` | PASS |
| New-file meta pairs | `rg --files Assets/OneRoof | rg -v '\\.meta$' ...` | PASS — new source/test assets have paired `.meta` files |

## Known risks or failures

- None known.

## Next safe action

Proceed to ART-003 after the follow-up golden acceptance correction completes its Application assembly run.

## References

- Backlog: `OR-523`
- Decisions: `ADR-045`
- Evidence: `Assets/OneRoof/Tests/EditMode/Application/TowerInspectionServiceTests.cs`
