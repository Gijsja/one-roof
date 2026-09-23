# ARCH-004 — Push Serialization Formatting into Aggregates

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Eliminate domain root bloat in `TowerSimulation.cs` (which previously mapped ~150 lines of aggregate fields into save data arrays) by pushing `ToSaveData()` and `FromSaveData()` round-trip serialization ownership directly into each aggregate (`BuildingTopologyState`, `PopulationState`, `TowerEconomyState`, `ElevatorBank`, and `TransitExecutionSystem`).

---

## Acceptance Criteria

- [x] Dedicated serialization DTOs created in `OneRoof.Domain.Persistence`:
  - `TopologySaveData.cs` (+ `.meta`)
  - `PopulationSaveData.cs` (+ `.meta`)
  - `EconomySaveData.cs` (+ `.meta`)
  - `TransitSaveData.cs` (+ `.meta`)
- [x] `BuildingTopologyState` implements `ToSaveData()` and `FromSaveData(TopologySaveData)`.
- [x] `PopulationState` implements `ToSaveData()` and `FromSaveData(PopulationSaveData)`.
- [x] `TowerEconomyState` implements `ToSaveData()` and `FromSaveData(EconomySaveData)`.
- [x] `ElevatorBank` implements `ToSaveData()` and `FromSaveData(ElevatorBankSaveData)`.
- [x] `TransitExecutionSystem` implements `ToSaveData()` and `RestoreFromSaveData(...)`.
- [x] `TowerSimulation.ExportSaveData()` and `RestoreFromSaveData(...)` reduced to clean, ~15-line orchestration methods.
- [x] Dedicated per-aggregate round-trip EditMode unit tests added in `Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs`.
- [x] 100% paired `.meta` hygiene verified across all assets.
- [x] Domain purity preserved with 0 `UnityEngine` dependencies in `OneRoof.Domain`.
- [x] ADR-039 recorded in `Docs/07_DECISION_LOG.md`.
- [x] `Planning/BACKLOG.md` and `Planning/ROADMAP.md` updated.

---

## Scope and Ownership

### New Files
- `Assets/OneRoof/Runtime/Domain/Persistence/TopologySaveData.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Persistence/PopulationSaveData.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Persistence/EconomySaveData.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Domain/Persistence/TransitSaveData.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs` (+ `.meta`)
- `Handoffs/Active/ARCH-004_aggregate-serialization.md`

### Modified Files
- `Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs`
- `Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs`
- `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Docs/07_DECISION_LOG.md` (ADR-039)
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`

---

## Verification Evidence

- **Syntax & Brackets:** All modified/new `.cs` files validated via Python brace matching.
- **Domain Purity:** 0 `UnityEngine` references found across 69 domain files.
- **Meta Hygiene:** 100% of files have matching `.meta` files.
- **Lines Removed:** ~330 lines trimmed from `TowerSimulation.cs` (reduced from 875 to 542 lines).

---

## Next Safe Action

Proceed to ARCH-005: Encapsulate ElevatorBank queues behind `Snapshot()`.
