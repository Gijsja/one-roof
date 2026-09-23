# OR-502 — Unified TowerSimulation & Leg-by-Leg Trip Execution

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-501, OR-202, OR-302  
**Owner:** Antigravity session  

## Outcome

Implement the master pure C# `TowerSimulation` and `TransitExecutionSystem` in `OneRoof.Domain`, orchestrating the simulation clock, routine schedule transitions, hierarchical transit routing, discrete leg-by-leg movement (walk progress and elevator queuing/boarding), and real-time spatial resident positions without managed allocations during steady-state ticks.

## Context

- Previous milestones used `TransitPrototypeSimulation`, an isolated mock fixture.
- Real Domain systems (`BuildingTopologyState`, `HierarchicalTransitGraph`, `ElevatorBank`, `PersonRecord`, `DailySchedule`, `ScheduleTripGenerator`) are now wired together into an integrated, deterministic simulation loop.
- `TransitExecutionSystem` advances trips leg-by-leg, enqueueing passengers into the `ElevatorBank` state machine, boarding moving cars, alighting at destination floors, and tracking wait ticks.
- `TowerSimulationSession` provides read-only projections for the Application layer (`ElevatorBankCongestionProjection`, `TransitPrototypeProjection`).

## Files Owned and Created / Modified

### Domain Layer (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs` [MODIFIED: added `TryTakeDeliveredPassenger`, `IsPassengerInCar`, `IsPassengerInQueue`]
- `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs` [MODIFIED: added `CurrentRoomId`, `UpdateLocation`]

### Application Layer (`OneRoof.Application`)
- `Assets/OneRoof/Runtime/Application/Tower.meta` [NEW directory meta]
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs` (+ `.meta`) [NEW]

### Test Assembly (`OneRoof.Domain.Tests.EditMode`)
- `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationTests.cs` (+ `.meta`) [NEW]

### Documentation and Tracking
- `Planning/BACKLOG.md`: Marked OR-502 as `DONE`, marked OR-503 as `ACTIVE`.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-026`.

## Acceptance Criteria

- [x] `TowerSimulation` compiles in `OneRoof.Domain` without UnityEngine references.
- [x] Advances simulation clock deterministically across ticks.
- [x] Generates routine trips at schedule block boundaries via `ScheduleTripGenerator`.
- [x] `TransitExecutionSystem` processes walk legs and elevator legs with exact queuing, boarding, and delivery.
- [x] Adding an elevator car dynamically increases throughput and reduces queue wait times.
- [x] Six pure C# unit tests in `TowerSimulationTests.cs` pass deterministically.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of asmdefs and referenced namespaces | PASS (no UnityEngine references in Domain) |
| Clean Git tree & meta files | `git status -u` | PASS (all files and folders paired with `.meta`) |
| Domain unit tests | Edit Mode tests in `TowerSimulationTests.cs` | PASS (covers initialization, clock advance, rush hour queues, car capacity, and trip completion) |

## Next Safe Action

Proceed with **OR-503**: Implement `TowerEconomyState` (treasury, construction costs, rental collection, sandbox toggle) and `LeasingDemandSystem` for autonomous tenant move-ins.
