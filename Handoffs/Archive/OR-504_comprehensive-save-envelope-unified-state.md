# OR-504 — Comprehensive Save/Load Envelope for Unified State

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-502, OR-503, OR-103  
**Owner:** Antigravity session  

## Outcome

Implement the comprehensive `TowerSaveData` serializable data transfer model, complete export/restore pipelines in `TowerSimulation`, and verified round-trip save tests proving exact bit-level restoration of building topology, population records, treasury balances, active in-flight commute trip legs, and elevator car/queue passengers.

## Context

- A core decision made during planning (ADR-028) was that saving mid-commute must preserve in-flight transit legs and queue states so reloading during morning rush hour does not reset elevator traffic or teleport residents.
- `TowerSaveData` is serializable via Unity's `JsonUtility` and wrapped inside the versioned `SaveEnvelope<TowerSaveData>`.
- `TowerSimulation.ExportSaveData()` extracts full snapshot state, and `TowerSimulation.RestoreFromSaveData()` reconstructs clock, topology, population, elevator bank, economy, and active trip legs.

## Files Owned and Created / Modified

### Domain Layer (`OneRoof.Domain`)
- `Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs` [MODIFIED: added `RestoreFromData`]
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs` [MODIFIED: added `RestoreState`]
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs` [MODIFIED: exposed `FloorQueues`]
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs` [MODIFIED: added `RestoreTripExecution`, `ClearActiveTrips`]
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` [MODIFIED: added `ExportSaveData`, `RestoreFromSaveData`]

### Test Assembly (`OneRoof.Infrastructure.Tests.EditMode`)
- `Assets/OneRoof/Tests/EditMode/Infrastructure/TowerSaveRoundTripTests.cs` (+ `.meta`) [NEW]

### Documentation and Tracking
- `Planning/BACKLOG.md`: Marked OR-504 as `DONE`, marked OR-505 as `ACTIVE`.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-028`.

## Acceptance Criteria

- [x] `TowerSaveData` serializes all floors, rooms, portals, households, residents, economy, elevator cars, and active trips.
- [x] Round-trip serialization through `JsonSaveSerializer` and `SaveEnvelope<TowerSaveData>` restores exact state.
- [x] In-flight commute trips preserve current leg index, remaining ticks, queue status, and wait times.
- [x] Restored simulation and original simulation advance deterministically with identical delivered passengers.
- [x] Three comprehensive tests in `TowerSaveRoundTripTests.cs` pass cleanly.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of asmdefs and referenced namespaces | PASS |
| Clean Git tree & meta files | `git status -u` | PASS (all files paired with `.meta`) |
| Serialization tests | Edit Mode tests in `TowerSaveRoundTripTests.cs` | PASS (fresh tower, mid-commute congestion, expanded floors/rooms) |

## Next Safe Action

Proceed with **OR-505**: Implement interactive build mode grid interaction and placement ghost preview in `TowerPlayableController` and presentation components.
