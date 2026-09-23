# BUGFIX_2026-09-19 — Visual Anomalies, Shaft Clipping, and Topology Fixes

**Status:** DONE
**Owner:** Antigravity
**Started:** 2026-09-19
**Updated:** 2026-09-19

## Objective

Resolve visual anomalies and gameplay/UX issues discovered during Play Mode inspection:
1. Elevator cars clipping into adjacent rooms when multiple cars (2–4) were active in the elevator bank.
2. White bar / white rectangle visual artifact appearing in the elevator shaft across floor additions.
3. Rooms being allowed to be placed inside the elevator shaft column.
4. Demolished rooms and stairwells leaving orphaned decorative child quads in the scene.
5. Inconsistency in elevator car purchase cost between placement predictor and economy state.
6. Stairwell graph connecting distant, non-adjacent floors directly via walk edges.
7. HUD clutter with redundant mini build buttons and inconsistent shortcut label hints.

## Acceptance Criteria

- [x] Multi-car horizontal layout in `ElevatorBankPresenter` dynamically calculates width and horizontal offset so all 1–4 cars stay strictly inside shaft boundaries `[-2.32 .. -1.48]`.
- [x] Shaft penthouse cap and pit cap updated in-place without leaking orphaned white quads; unparented or obsolete cap quads cleaned up.
- [x] Elevator shaft view spans only the floors serviced by the `ElevatorBank` (`MinFloor..MaxFloor`) rather than all building floors.
- [x] `BuildingTopologyState.CanExecute(BuildRoomCommand)` rejects rooms overlapping existing elevator shaft columns (`transit:shaft_overlap`).
- [x] Room and stairwell decorative quads (walls, tags, awnings, stair treads, rails) parented directly under `RoomView_{room.Id}` so demolition removes all geometry cleanly.
- [x] `ElevatorPlacementPredictor` uses `$2,500` cost matching `TowerEconomyState.ElevatorCarCost`.
- [x] `HierarchicalTransitGraph` connects stairwell landings only between vertically adjacent floors (`|floorA - floorB| == 1`).
- [x] Top-left HUD build buttons simplified; shortcut hint harmonized to `[Space] Pause  [1/B] Build  [2/I] Inspect  [3/D] Data  [4/M] Manage  [Esc] Cancel`.
- [x] 100% paired `.meta` hygiene maintained.
- [x] 0 `UnityEngine` dependencies in `OneRoof.Domain` and `OneRoof.Application`.
- [x] EditMode tests pass 100%.
- [x] Play Mode visual verification confirms cars and shaft are visually correct with 1 and 4 cars.

## Scope and Ownership

Modified files:
- `Assets/OneRoof/Runtime/Presentation/Tower/ElevatorBankPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerDashboardHudView.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`
- `Assets/OneRoof/Runtime/Application/Prediction/ElevatorPlacementPredictor.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/ElevatorBankPresenterTests.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/RoomPresenterTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/BuildingCommandTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/HierarchicalTransitGraphTests.cs`
- `Docs/07_DECISION_LOG.md`

## Changes Made

- **`ElevatorBankPresenter.cs`**:
  - Implemented `CalculateCarLayout(int carIndex, int totalCars, out float x, out float width)` clamping car width between `0.16f` and `0.44f` with `0.03f` gap, keeping all cars inside `[-2.32 .. -1.48]`.
  - Added `EnsureShaftViews(int minFloor, int maxFloor)` to update penthouse and pit cap positions and floor spans dynamically, applying `MaterialPropertyBlock` styling to prevent default unlit white material artifacts.
  - Added `CleanupOrphanedObjects()` to remove any untracked or orphaned shaft/car quads.
- **`RoomPresenter.cs`**:
  - Parented all decorative quads (`Room Wall L/R`, `Apt Tag`, `Diner Awning`, `Stair Bg`, `Stair Tread`, `Stair Rail`) to the root `RoomView_{room.Id}` GameObject.
  - Ensuring room views upon demolition destroys the root `RoomView_{room.Id}` and all children atomically.
- **`TowerPlayableController.cs`**:
  - Updated `SyncPresenterGeometry()` to pass `ElevatorBank.MinFloor` and `ElevatorBank.MaxFloor` to `_elevator.EnsureShaftViews()`.
- **`BuildingTopologyState.cs`**:
  - Added overlap check against `FiveFloorTopologyFixture.ElevatorShaftContentId` and `transit:elevator_shaft` in `CanExecute(BuildRoomCommand)`.
  - Replaced hardcoded stairwell check with dynamic shaft room check in `CanExecute(BuildStairwellCommand)`.
- **`HierarchicalTransitGraph.cs`**:
  - Enforced adjacent floor constraint `Math.Abs(landingA.Floor - landingB.Floor) == 1` when creating stair vertical walk edges.
- **`ElevatorPlacementPredictor.cs`**:
  - Updated `DefaultElevatorCost` from 1500 to 2500, aligning with `TowerEconomyState.ElevatorCarCost`.
- **`TowerDashboardHudView.cs`**:
  - Removed duplicate mini build buttons when in Build mode.
  - Harmonized shortcut bar text.
- **EditMode Test Suite**:
  - Added tests in `ElevatorBankPresenterTests.cs`, `RoomPresenterTests.cs`, `BuildingCommandTests.cs`, and `HierarchicalTransitGraphTests.cs`.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Domain & App Purity | `grep -rn "UnityEngine" Assets/OneRoof/Runtime/Domain/ Assets/OneRoof/Runtime/Application/` | PASS (0 occurrences) |
| Meta Hygiene | `git status` check for unpaired `.meta` files | PASS (Clean, no untracked `.meta`) |
| Unity Compilation | `unity compile` via Unity CLI | PASS (0 errors, 0 warnings) |
| EditMode Tests | `unity test --test-platform EditMode` | PASS (100% passing in 9.91s) |
| Play Mode Visual Check | Visual verification with 1 car and 4 cars in live Play Mode | PASS (zero shaft clipping, zero white artifacts, correct caps) |

## Next Safe Action

Commit changes to Git repository.
