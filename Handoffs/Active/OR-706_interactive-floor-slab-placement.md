# OR-706 — Interactive floor-slab placement

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-21
**Updated:** 2026-09-21

## Objective

Make Floor Slab reliably preview the next valid expansion level in Build mode, then dispatch the normal domain command on confirmation.

## Completed behavior

- Selecting `floor:slab` in Build mode resolves any overview hover to the first unbuilt floor, rather than producing a red preview over an existing slab.
- The existing placement path continues to snap the slab footprint to the structural column, validates through `TowerSimulation.CanExecute`, deducts the normal treasury cost, emits the placement event, and updates structure/camera presentation.
- The PlayMode lifecycle test covers Build mode selection, next-floor resolution, visible valid ghost, command confirmation, topology mutation, treasury deduction, and rendered slab creation.

## Changed files

- `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/GridPlacementControllerTests.cs`
- `Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs`
- `Planning/BACKLOG.md`

## Validation

| Check | Result |
| --- | --- |
| Static review | PASS — focused diff is limited to placement floor resolution and its tests. |
| Unity 6000.3.24f1 isolated batch compilation | PASS — first import completed successfully; no C# compiler diagnostics in `/tmp/one-roof-or706-grid-editmode.log`. |
| Targeted EditMode test | BLOCKED — Unity exited successfully without producing `/tmp/one-roof-or706-grid-editmode.xml`, including on warm rerun. |
| Targeted PlayMode test and screenshot | NOT RUN — no reliable test-run XML mechanism was available in this host; `-nographics` cannot provide visual screenshot evidence. |

## Commits

- `dd67be3 feat(build): target next floor slab interactively`

## Next safe action

Run `GoldenExpansionPlayModeTests.BuildMode_FloorSlabPreviewAndConfirmation_ExtendsTheRenderedTower` in a desktop Unity test runner, capture the Tower frame after confirmation, and attach the resulting screenshot to this handoff.
