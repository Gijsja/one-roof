# Handoff — Ground-floor from-scratch scene (Tower_GroundStart)

## What was built
New `Tower_GroundStart` scene plus a `TowerStartMode.GroundFloorStart` boot path so play
truly starts from a bare ground slab + lobby shell, exercising economy, utilities,
commute, routines, and vertical expansion from tick zero.

### Domain (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
  - Added `CreateGroundFloorStart(startingTreasury, randomStream)`: 1 slab (floor 0,
    cells −14..17), lobby shell (2..14), elevator shaft (0..1) with portals, empty
    population, one car (bank 0..0), funded treasury. Standard tick loop unchanged,
    so leasing, rent, needs, wellbeing, and scrutiny all run from tick zero.

### Application (`OneRoof.Application`)
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
  - Added `CreateGroundFloorStart()` factory and `ResetToGroundFloorStart()`.
    Existing `Reset()` behavior untouched.

### Presentation (`OneRoof.Presentation`)
- `TowerPlayableController.cs`: new `TowerStartMode` enum (`StandardFiveFloor = 0`,
  `GroundFloorStart = 1`), `[SerializeField] _startMode` (default 0, so `Tower.unity`
  is unaffected). Ground start skips `SeedMorningRush()`, boots in Build mode, and
  `ResetCommuteSimulation()` resets to the matching start.
- `TowerDashboardHudView.cs`: ground-start title + read-only 5-system checklist
  (Economy / Power / Water / Commute / Routine / Expansion) using live projections only.

### Scene
- `Assets/Scenes/Tower_GroundStart.unity` (+ `.meta`, fresh guid): copy of `Tower.unity`
  with `_startMode: 1` on the `TowerPlayableController` block.
- Registered in `ProjectSettings/EditorBuildSettings.asset` after `Testbed_Transit`.

### Tests
- `Assets/OneRoof/Tests/EditMode/Domain/GroundFloorStartTests.cs` (+ `.meta`):
  - Start state: 1 slab, lobby + shaft, 0 residents, funded treasury.
  - Empty sim ticks 60× safely with zero revenue.
  - Full loop: widen slab → diner + apartment (treasury deducts) → substation/riser/
    pump/water-riser → floor-1 slab + apartment + risers + transformer + shaft
    extension → floor 1 powered (V ≥ 0.8) and pressurised (P ≥ 0.5) → after 700 ticks
    both apartments lease via demand, rent collected, elevator trips/deliveries > 0.
  - Note: leased residents use default schedules (first Sleep→Work ≈ tick 480), so the
    loop test must run past tick 480 to observe commutes.

## Validation (isolated worktree `../one-roof-groundstart-validation`, Unity 6000.3.24f1)
- Compile (one-shot batchmode, no `-runTests`): exit 0, no `error CS`.
- `GroundFloorStart` filter: 3/3 pass.
- Full EditMode suite: 424 total, 421 pass, 3 fail — all 3 proven pre-existing on
  pristine HEAD via stash + targeted re-runs:
  - `GridPlacementControllerTests.IsPointerOverUI_*` (2) — fail on HEAD too.
  - `ModeShellBarControllerTests...ReopensPaletteByClearingExistingToolSelection` (1) —
    fails on HEAD too (9-test filter: 8/9, same single failure).
- Failure family is build-palette tool-selection expectations in untouched files; the
  only shared symbol (`TowerSimulationSession` ctor use in grid tests) is behaviorally
  unchanged (additive factory methods only).
- No scene/YAML errors in the Editor log; worktree removed after validation.
- Commands, exit codes, and XML/log paths:
  - `/tmp/opencode/one-roof-groundstart-compile.log` (exit 0)
  - `/tmp/opencode/one-roof-groundstart-tests2.xml` (3/3 pass, exit 0)
  - `/tmp/opencode/one-roof-groundstart-full.xml` (424/421/3, exit 2)
  - `/tmp/opencode/baseline-grid.xml`, `/tmp/opencode/baseline-modeshell.xml` (HEAD baselines)

## Risks / next safe actions
- Scene boot was validated by import + domain coverage, not a PlayMode run: opening
  `Tower_GroundStart` in the Editor and placing a substation → apartment → floor slab
  is the recommended smoke test.
- Golden PlayMode tests still target the five-floor fixture; a ground-start golden
  (expand → lease → commute → save/load) would be the natural follow-up.
- Pre-existing files touched by other sessions were left alone
  (`Docs/07_DECISION_LOG.md`, `Docs/12_ECONOMY.md`, wardrobe `.meta` files).

## Follow-up: shaft-column reservation, aligned expansion, stairs-only access
- `BuildingTopologyState` now reserves the central elevator column `[0..1]`
  (`ReservedShaftMinX/MaxX`): once any shaft exists tower-wide, `BuildRoomCommand`
  and `BuildStairwellCommand` overlapping the column are rejected with the existing
  `transit:shaft_overlap` code and a message naming the column. Shaft-less scratch
  topologies stay unconstrained. The placement ghost turns red automatically since it
  delegates to `CanExecute`.
- New tests: `ReservedShaftColumn_...` (full/partial room overlaps, stairs above the
  shaft top, shaft extension admitted with aligned rooms), `ExpansionFloor_...`
  (slab bounds mirror floor below, wall-to-wall zoning around the column),
  `StairsOnlyAccess_...` (2 residents lease, commute via stairwell Walk legs between
  StairLanding nodes, zero elevator use, rent flows). Presentation tests prove the
  slab tool mirrors the lower slab and the UI path blocks rooms in the column until
  the shaft claims it.
- Finding: the transit graph models stair climbs as `Walk`-mode legs between
  `StairLanding` nodes (`TransitMode.Stairs` is currently unused by the builder).
- Two older tests built rooms at `0..5` on an expansion floor and were relocated
  off the column with identical footprint/cost (`TowerEconomyAndLeasingTests`,
  `TowerSaveRoundTripTests`).
- Validation (worktree `../one-roof-shaftstairs-validation`, since removed): full
  EditMode suite 429 total / 426 pass; the only 3 failures are the previously
  baselined pre-existing palette failures. Results: `/tmp/opencode/shaftstairs-full2.xml`.

## Follow-up: palette failures fixed (was: 3 pre-existing)
- `ModeShellSession.ExecuteCommand` could never clear the build tool: null `toolId`
  meant "don't touch". Targeting Build now always applies the tool slot (even null),
  so `SelectBuildTool(null)` deselects and entering Build reopens the palette with
  nothing preselected. Verified safe: every `SwitchMode(Build)` caller enters fresh
  or re-selects a tool immediately after; no existing test asserted preservation.
- `IsPointerOverUI` tests assumed a large viewport, but the headless runner is
  640x480 (probed), where the 650px palette rect covers the screen center. Test
  points are now viewport-valid: world point (300, 375) sits above the palette top
  edge, right of the HUD column, outside card regions; palette point (100, 200)
  asserts both flag states. Production hit-areas untouched.
- Validation (worktrees since removed): targeted filters 12/12 pass; combined run
  of all local changes on latest HEAD — full EditMode suite **438/438, exit 0**.
  Results: `/tmp/opencode/combined-full.xml`.
