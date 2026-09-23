# BUGFIX_2026-09-17 — Diner Placement Elevator Reset and Transit Dynamic Routing

**Status:** DONE  
**Owner:** Antigravity  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

## Objective

Fix 3 bugs reported during gameplay:
1. Elevator resets when placing a diner.
2. Elevator moves when no one is calling or being transported.
3. NPCs skip the elevator and walk straight across floors to a newly placed diner.

## Acceptance criteria

- [x] Placing a diner or room dynamically adds geometry without resetting existing elevator or resident view GameObjects.
- [x] Elevator starts completely idle at tick 0 without 50 legacy ghost queue calls.
- [x] Residents wake up according to daily schedules and produce genuine commute trips using the elevator.
- [x] Adding/demolishing rooms dynamically refreshes the transit routing graph and trip generator so newly placed commercial destinations are pathable via elevator.
- [x] Unroutable trips do not teleport residents across floors.
- [x] Unity compiler passes with zero errors and warnings.

## Scope and ownership

Modified files:
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs`
- `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs`
- `Assets/OneRoof/Runtime/Domain/Population/DailySchedule.cs`
- `Assets/OneRoof/Runtime/Domain/Population/FiftyResidentFixture.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationTests.cs`

## State at handoff

All 3 reported bugs are resolved and verified in both EditMode tests and the live Unity 6 Editor compiler.

## Changes made

- `TowerPlayableController.cs`: Switched `OnPlacementExecuted` from destructive teardown (`ClearWorldGeometry`) to incremental view synchronization (`EnsureRoomViews`, etc.). Added demolished room cleanup with explicit namespace typing for `EntityId`.
- `TowerSimulation.cs`: Removed legacy 50-passenger ghost queue injection at startup (`enqueueMorningRush = false`). Added `SyncTransitServices()` wired into all topology mutations (`BuildRoom`, `BuildFloorSlab`, `AddElevatorShaft`, `BuildStairwell`, `DemolishRoom`, `RestoreFromSaveData`).
- `DailySchedule.cs` & `FiftyResidentFixture.cs`: Shifted base morning wakeup ticks to 10–20 so tick 0 starts in peaceful rest state before residents wake and dispatch elevator trips.
- `ScheduleTripGenerator.cs`: Added `UpdateTopology()` and updated `FindRoomByContent` to support all commercial/diner variants.
- `TransitExecutionSystem.cs`: Cancelled unroutable trips instead of teleporting residents to their target destination when `PlannedRoute == null`.
- `TowerSimulationTests.cs`: Added 3 unit tests verifying initial idle elevator state, dynamic planner route generation after building rooms, and route failure trip cancellation.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Compile | Unity Editor compilation (6000.3.24f1) | PASS |
| Edit Mode | `TowerSimulationTests` test suite | PASS |
| Manual | Placing diner during Play Mode | PASS |

## Known risks or failures

None known.

## Next safe action

Continue gameplay testing in Play Mode or proceed with remaining backlog tasks.
