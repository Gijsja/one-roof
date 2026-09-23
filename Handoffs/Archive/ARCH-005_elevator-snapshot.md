# ARCH-005 — Encapsulate ElevatorBank Queue Internals Behind Snapshot()

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Encapsulate `ElevatorBank` internal mutable queue data structures (`FloorQueues` and `DeliveredPassengers`) so external layers (specifically `TowerSimulationSession` in Application) do not iterate raw collections across architectural boundaries. Expose a clean, immutable `ElevatorBankSnapshot` with fast lookup helpers and queue metrics.

---

## Acceptance Criteria

- [x] `ElevatorBankSnapshot` created as an immutable record in `OneRoof.Domain.Transit`:
  - Per-floor queues, cars, delivered passengers, wait ticks.
  - Quick queue count accessors (`GetQueueLength`, `GetFloorQueue`).
  - Indexed passenger lookup helpers (`TryGetQueuedPassenger`, `TryGetRidingPassenger`, `TryGetDeliveredPassenger`).
- [x] `ElevatorBank.Snapshot()` implemented to capture queue and car state immutably.
- [x] Visibility of `ElevatorBank.FloorQueues` and `ElevatorBank.DeliveredPassengers` changed from `public` to `internal` (Domain-internal access for `TransitExecutionSystem`).
- [x] `TowerSimulationSession.TransitProjection` refactored to consume `ElevatorBank.Snapshot()` rather than directly iterating `FloorQueues` and `DeliveredPassengers`.
- [x] Dedicated EditMode unit tests added in `Assets/OneRoof/Tests/EditMode/Domain/ElevatorBankSnapshotTests.cs` testing snapshot capture, immutability against mutations, and passenger lookups.
- [x] 100% paired `.meta` hygiene verified across all assets.
- [x] Domain purity preserved with 0 `UnityEngine` dependencies in `OneRoof.Domain`.
- [x] ADR-040 recorded in `Docs/07_DECISION_LOG.md`.
- [x] `Planning/BACKLOG.md` and `Planning/ROADMAP.md` updated.

---

## Scope and Ownership

### New Files
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBankSnapshot.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Domain/ElevatorBankSnapshotTests.cs` (+ `.meta`)
- `Handoffs/Active/ARCH-005_elevator-snapshot.md`

### Modified Files
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Docs/07_DECISION_LOG.md` (ADR-040)
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`

---

## Verification Performed

- **Bracket / Syntax Integrity:** Validated balanced braces, brackets, and parentheses on all modified files via AST/token scan.
- **Domain Purity:** Confirmed 0 occurrences of `UnityEngine` in `Assets/OneRoof/Runtime/Domain`.
- **Meta Hygiene:** 0 missing and 0 orphan `.meta` files across all `Assets/`.

---

## Known Risks and Next Safe Action

- **Risks:** None. The encapsulation leaves internal domain transit execution logic intact while shielding external projections from collection schema changes.
- **Next Safe Action:** Proceed with Milestone 5.3 shader and presentation tasks (`OR-518`, `OR-519`, `OR-520`) using the newly extracted modular presenters.
