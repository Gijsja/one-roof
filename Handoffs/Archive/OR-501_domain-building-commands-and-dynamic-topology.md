# OR-501 — Domain Building Commands & Dynamic Topology

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-102, OR-201, OR-401  
**Owner:** Antigravity session  

## Outcome

Provide validated domain building commands (`BuildFloorSlabCommand`, `BuildRoomCommand`, `DemolishRoomCommand`, `AddElevatorShaftCommand`, `BuildStairwellCommand`) and an aggregate root `BuildingTopologyState` that mutates building topology, emits immutable domain events, and lazily synchronizes the `HierarchicalTransitGraph`.

## Context

- Pure C# domain layer (`OneRoof.Domain` with `noEngineReferences: true`).
- Follows the command-query separation principles established in `Docs/02_ARCHITECTURE.md` and `Docs/03_DATA_CONTRACTS.md`.
- Commands validate preconditions: floor level continuity, slab bounds containment, lower floor contiguous structural support, room overlap detection, minimum room widths, and elevator shaft vertical alignment across all spanned floors.
- Dynamic changes to rooms and portals immediately update the `HierarchicalTransitGraph` so routing systems always operate on live topology.

## Files Owned and Created

### Domain Layer (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/Commands/BuildFloorSlabCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Commands/BuildRoomCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Commands/DemolishRoomCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Commands/AddElevatorShaftCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Commands/BuildStairwellCommand.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs` (+ `.meta`)

### Test Assembly (`OneRoof.Domain.Tests.EditMode`)
- `Assets/OneRoof/Tests/EditMode/Domain/BuildingCommandTests.cs` (+ `.meta`)

### Documentation and Tracking
- `Planning/BACKLOG.md`: Added M5.1 tasks (OR-501 to OR-506), marked OR-501 as `DONE`.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-025`.

## Acceptance Criteria

- [x] Domain building commands compile in `OneRoof.Domain` without UnityEngine references.
- [x] Floor slabs enforce sequential upward growth (Floor N+1 requires Floor N).
- [x] Room building validates floor slab bounds, lower-floor structural support, room overlap, and minimum width.
- [x] Elevator shafts and stairwells create continuous portal doors and rooms across all floors in their span.
- [x] Room demolition cleans up associated portals and removes transit graph references.
- [x] Command execution emits immutable domain events with unique IDs and affected entity IDs.
- [x] `BuildingTopologyState` synchronizes the `HierarchicalTransitGraph` dynamically.
- [x] All 15 unit tests in `BuildingCommandTests.cs` demonstrate deterministic pure C# behavior.

## Decisions

- **ADR-025:** Command-driven dynamic topology mutation and graph synchronization via `BuildingTopologyState`. Validates floor continuity, slab bounds, room overlap, and structural support before mutating state, emitting domain events and lazily synchronizing `HierarchicalTransitGraph`.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of `OneRoof.Domain` and referenced namespaces | PASS (no UnityEngine references) |
| Git working tree & `.meta` pairing | `git status -u` | PASS (all files have matching `.meta` GUIDs) |
| Domain unit tests | Edit Mode tests in `BuildingCommandTests.cs` | PASS (covers floor slab rules, overlap, support, demolition, shafts, and stairs) |

## Next Safe Action

Proceed with **OR-502**: Implement unified `TowerSimulation` and leg-by-leg discrete trip execution in `OneRoof.Domain`, connecting daily schedules, route planning, `ElevatorBank` queues, and resident states.
