# BUGFIX_2026-09-18 — Post-Commit Bug Hunt Fixes

**Status:** DONE  
**Owner:** Antigravity  
**Started:** 2026-09-18  
**Updated:** 2026-09-18  

## Objective

Fix bugs identified during post-commit code review of commits `a0fa648` and `c7f8516`:
1. `ElevatorCar.ChooseNextTarget()` floor-0 heuristic fired without a direction guard, affecting cars in non-upward phases.
2. Dead private method `DispatchCarToFloor` left behind in `ElevatorBank.cs` after rebalancing refactor.
3. `BuildingTopologyState.cs` stairwell overlap guard contained a hardcoded coordinate condition `(cmd.StairMinX <= 1 && cmd.StairMaxX >= 0)` combined with `||` that falsely blocked stairwell placement at columns 0–1 even without an elevator shaft.
4. `TowerPlayableController.SeedMorningRush()` directly mutated domain state with hardcoded entity IDs 1–50 from Presentation, risking ID collisions and violating architectural layering.

## Acceptance Criteria

- [x] Floor-0 heuristic in `ElevatorCar.ChooseNextTarget()` only triggers when the car is idle (`Direction == ElevatorDirection.None`) or traveling up (`Direction == ElevatorDirection.Up`).
- [x] Unused `DispatchCarToFloor(int floor)` in `ElevatorBank.cs` removed.
- [x] Stairwell overlap check in `BuildingTopologyState.cs` checks only `existing.ContentType == FiveFloorTopologyFixture.ElevatorShaftContentId`, removing false-positive column checks.
- [x] `SeedMorningRush()` moved to `TowerSimulation` domain aggregate using `_nextEntityId` counter (safe from collisions) and exposed via `TowerSimulationSession` application boundary; Presentation layer delegates without directly touching `ElevatorBank`.
- [x] 100% paired `.meta` hygiene maintained.
- [x] 0 `UnityEngine` dependencies in `OneRoof.Domain` and `OneRoof.Application`.
- [x] All edited files syntactically validated (braces, parens, brackets balanced).

## Scope and Ownership

Modified files:
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`

## Changes Made

- **`ElevatorCar.cs`**: Added `(Direction == ElevatorDirection.None || Direction == ElevatorDirection.Up)` check to the floor 0 `_targetFloors.Max` pickup branch so it only triggers when departing the ground floor upwards.
- **`ElevatorBank.cs`**: Deleted dead private method `DispatchCarToFloor(int floor)`.
- **`BuildingTopologyState.cs`**: Removed hardcoded coordinate clause `|| (cmd.StairMinX <= 1 && cmd.StairMaxX >= 0)` from the stairwell-shaft collision check.
- **`TowerSimulation.cs`**: Added `public void SeedMorningRush()` drawing entity IDs safely from `_nextEntityId`.
- **`TowerSimulationSession.cs`**: Added application forwarding method `public void SeedMorningRush() => _simulation.SeedMorningRush()`.
- **`TowerPlayableController.cs`**: Updated `SeedMorningRush()` to delegate to `_sim?.SeedMorningRush()` instead of directly manipulating `ElevatorBank`.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Syntax & Balance | Python balance validation for braces, parens, brackets | PASS (0 imbalances) |
| Domain Purity | Grep for `UnityEngine` in `Assets/OneRoof/Runtime/Domain/` | PASS (0 occurrences) |
| Application Purity | Grep for `UnityEngine` in `Assets/OneRoof/Runtime/Application/` | PASS (0 occurrences) |
| Meta Hygiene | Check `.meta` file existence for all modified files | PASS (All present) |
| Unity CLI / Tests | Automated test run via Unity CLI | NOT RUN (No local Unity Editor binary on headless runner PATH; CI runs in Docker via `unity-validation.yml`) |

## Next Safe Action

Commit changes or run CI workflow to verify full test suite on Ubuntu runner.
