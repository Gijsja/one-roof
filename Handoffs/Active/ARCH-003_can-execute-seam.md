# ARCH-003 — Domain CanExecute Seam for Placement Validation

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Eliminate duplicated domain placement rules in `GridPlacementController.cs` (~200 lines of checks for treasury funds, slab bounds, continuous structural support, room collisions, and max car limits) by lifting placement validation into a clean domain `ICommand` seam with `TowerSimulation.CanExecute(ICommand)`.

---

## Acceptance Criteria

- [x] `ICommand` interface declared in `OneRoof.Domain.Commands`.
- [x] All building and transit construction commands implement `ICommand` (`BuildFloorSlabCommand`, `BuildRoomCommand`, `AddElevatorShaftCommand`, `BuildStairwellCommand`, `DemolishRoomCommand`, `AddElevatorCarCommand`).
- [x] `CommandResult` expanded with `Ok`, `Reason`, `Success()`, and `Fail()`.
- [x] `BuildingTopologyState` exposes typed `CanExecute` methods for `BuildFloorSlabCommand`, `BuildRoomCommand`, `AddElevatorShaftCommand`, `BuildStairwellCommand`, `DemolishRoomCommand` reused by `Execute`.
- [x] `TowerSimulation.CanExecute(ICommand)` and `ExecuteCommand(ICommand)` implemented as the single domain source of truth.
- [x] `GridPlacementController` refactored to use `TryCreateCommand` and delegate validation entirely to `_simulationSession.CanExecute(command)` / `ExecuteCommand(command)`.
- [x] Comprehensive EditMode domain unit tests added in `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationCanExecuteTests.cs`.
- [x] 100% paired `.meta` hygiene verified across all assets.
- [x] Domain purity preserved with 0 `UnityEngine` dependencies in `OneRoof.Domain`.
- [x] ADR-038 recorded in `Docs/07_DECISION_LOG.md`.
- [x] `Planning/BACKLOG.md` and `Planning/ROADMAP.md` updated.

---

## Scope and Ownership

### New Files
- `Assets/OneRoof/Runtime/Domain/Commands/ICommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Commands/AddElevatorCarCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationCanExecuteTests.cs` (+ `.meta`)
- `Handoffs/Active/ARCH-003_can-execute-seam.md`

### Modified Files
- `Assets/OneRoof/Runtime/Domain/Commands/CommandResult.cs`
- `Assets/OneRoof/Runtime/Domain/Commands/BuildFloorSlabCommand.cs`
- `Assets/OneRoof/Runtime/Domain/Commands/BuildRoomCommand.cs`
- `Assets/OneRoof/Runtime/Domain/Commands/AddElevatorShaftCommand.cs`
- `Assets/OneRoof/Runtime/Domain/Commands/BuildStairwellCommand.cs`
- `Assets/OneRoof/Runtime/Domain/Commands/DemolishRoomCommand.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs`
- `Docs/07_DECISION_LOG.md` (ADR-038)
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`

---

## Verification Evidence

- **Syntax & Brackets:** All modified/new `.cs` files validated via Python brace matching.
- **Domain Purity:** 0 `UnityEngine` references found across 65 domain files.
- **Meta Hygiene:** 100% of files have matching `.meta` files.
- **Lines Removed:** ~214 lines of duplicate validation logic deleted from `GridPlacementController.cs`.

---

## Next Safe Action

Proceed to ARCH-004: Push serialization formatting into aggregates (`ToSaveData` / `FromSaveData`).
