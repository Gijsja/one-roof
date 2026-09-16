# OR-505 — Interactive Build Mode Grid Interaction & Ghost Preview

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-501, OR-502, OR-503  
**Owner:** Antigravity session  

## Outcome

Implement interactive grid cell hover, drag/click placement, footprint sizing, and visual ghost preview in the First Playable presentation layer. Connect `TowerPlayableController` directly to `TowerSimulationSession`, allowing players to build floor slabs, apartments, and elevator infrastructure while viewing instant visual feedback and dynamic geometry updates.

## Context

- In M5.1, the first playable slice transitions from a read-only transit prototype into a fully interactive building simulation.
- `GridPlacementController` converts continuous world raycast coordinates into discrete `CellCoordinate` positions (floor, cellX), evaluates room footprints (width 6 for residential apartments, 10 for commercial diners, 24 for floor slabs, 2 for transit), validates against slab bounds and room collisions, checks treasury affordability, and dispatches domain commands.
- `PlacementGhostPresenter` renders a visual bounding box with dynamic color coding (green for valid/affordable, red for invalid/unaffordable).
- `TowerPlayableController` dynamically instantiates world quads for floor slabs, room division posts, elevator cars, and residents as the tower expands beyond the initial five floors.

## Files Owned and Created / Modified

### Presentation Layer (`OneRoof.Presentation`)
- `Assets/OneRoof/Runtime/Presentation/Tower/PlacementGhostPresenter.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` [MODIFIED: bound to `TowerSimulationSession`, wired grid controller & ghost presenter, added dynamic geometry expansion]
- `Assets/OneRoof/Runtime/Presentation/OneRoof.Presentation.asmdef` [MODIFIED: added `OneRoof.Domain` reference]

### Application Layer (`OneRoof.Application`)
- `Assets/OneRoof/Runtime/Application/Modes/ModeShellSession.cs` [MODIFIED: added `SetPlacementTarget`]
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs` [MODIFIED: assembled full `ElevatorBankCongestionProjection`, exposed building commands]

### Domain Layer (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/Population/FiftyResidentFixture.cs` [MODIFIED: added parameterless `Create()` overload]
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` [MODIFIED: added `enqueueMorningRush` support to `CreateStandardFiveFloor`]

### Test Assemblies
- `Assets/OneRoof/Tests/EditMode/Presentation/GridPlacementControllerTests.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Tests/EditMode/Presentation/OneRoof.Presentation.Tests.EditMode.asmdef` [MODIFIED: added `OneRoof.Domain` reference]

### Documentation and Tracking
- `Planning/BACKLOG.md`: Marked OR-505 as `DONE`, marked OR-506 as `ACTIVE`.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-029`.

## Acceptance Criteria

- [x] Mouse hover over tower cells computes correct discrete `(floor, cellX)` coordinates.
- [x] Placement ghost dynamically adjusts size to match tool footprint (6 for apartments, 2 for shafts/cars, 24 for floor slabs).
- [x] Visual ghost tints green when placement is valid and affordable, and red when invalid (out of bounds, overlapping room, or insufficient funds).
- [x] Left-clicking dispatches validated building commands (`BuildFloorSlabCommand`, `BuildRoomCommand`, `AddElevatorShaftCommand`, `AddCapacity`).
- [x] Dynamic geometry in `TowerPlayableController` updates immediately when new floors or cars are built.
- [x] All 8 tests in `GridPlacementControllerTests.cs` and all tests in `TowerPlayableControllerTests.cs` pass cleanly.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of asmdefs and referenced namespaces | PASS |
| Clean Git tree & meta files | `git status -u` | PASS (all files paired with `.meta`) |
| Grid placement tests | Edit Mode tests in `GridPlacementControllerTests.cs` | PASS (coordinate roundtrip, tool width calculation, out-of-bounds rejection, overlap rejection, treasury check, execution) |
| Controller tests | Edit Mode tests in `TowerPlayableControllerTests.cs` | PASS |

## Next Safe Action

Proceed with **OR-506**: Implement the Golden Milestone 5.1 Acceptance Test verifying the dynamic expansion of the tower to Floor 5, autonomous leasing of apartments, morning rush hour congestion, elevator intervention reducing wait time by >40%, and full save/load round-trip settlement.
