# OR-503 — Tower Economy, Treasury & Demand-Driven Leasing

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-501, OR-301  
**Owner:** Antigravity session  

## Outcome

Implement the financial treasury domain model (`TowerEconomyState`) and autonomous leasing service (`LeasingDemandSystem`), enforcing construction costs for floor slabs, rooms, elevator shafts, and stairwells while collecting periodic tenant rent and autonomously leasing vacant apartments to new households and workers when transit capacity allows.

## Context

- The game vision requires consequential economics: build space -> attract residents -> collect rent -> fund tower expansion.
- Preconditions: build commands must verify treasury balance unless `SandboxMode` is enabled.
- Autonomous leasing responds to vacancy: newly built apartments on upper floors trigger new household move-ins with deterministic schedules, needs, and traits, dynamically increasing commute traffic.
- Congestion brake: severe elevator congestion (>40 queued, >50 average wait) halts move-in demand until capacity is improved.

## Files Owned and Created / Modified

### Domain Layer (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/Economy.meta` [NEW folder meta]
- `Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Domain/Economy/LeasingDemandSystem.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs` [MODIFIED: added `AddPerson`, `AddHousehold`, `ResidentCount`]
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` [MODIFIED: integrated `Economy`, `Leasing`, periodic leasing/rent ticks, economic command helpers]

### Test Assembly (`OneRoof.Domain.Tests.EditMode`)
- `Assets/OneRoof/Tests/EditMode/Domain/TowerEconomyAndLeasingTests.cs` (+ `.meta`) [NEW]

### Documentation and Tracking
- `Planning/BACKLOG.md`: Marked OR-503 as `DONE`, marked OR-504 as `ACTIVE`.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-027`.

## Acceptance Criteria

- [x] `TowerEconomyState` accurately computes costs for floor slabs, room types, and elevator shafts.
- [x] Insufficient funds reject build commands with machine-readable rejection code `economy:insufficient_funds`.
- [x] `SandboxMode` bypasses fund deductions and allows free construction.
- [x] Periodic rent collection increases treasury balance based on resident and commercial tenant counts.
- [x] `LeasingDemandSystem` detects vacant apartments and automatically moves in households and working residents.
- [x] Severe elevator congestion suppresses move-in demand.
- [x] Six pure C# unit tests in `TowerEconomyAndLeasingTests.cs` pass deterministically.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of asmdefs and referenced namespaces | PASS (pure Domain C#, no UnityEngine) |
| Clean Git tree & meta files | `git status -u` | PASS (all files and folders paired with `.meta`) |
| Domain unit tests | Edit Mode tests in `TowerEconomyAndLeasingTests.cs` | PASS (cost calculation, sandbox mode, rent collection, rejected commands, and leasing) |

## Next Safe Action

Proceed with **OR-504**: Implement the comprehensive `TowerSaveState` payload and round-trip serialization tests for the unified simulation state, persisting topology, population, economy, and active in-flight commute queues.
