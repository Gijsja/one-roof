# OR-521 — Resident Room Living & Leg-by-Leg Corridor-Elevator Transit

**Status:** READY FOR REVIEW  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Deliver the complete resident room living experience:
- Position residents inside their assigned apartments, offices, and diner rather than stacking them in corridor lines on the floor.
- Spatially distribute multiple residents sharing a room across interior floorboard/furniture zones.
- Support full leg-by-leg corridor transit: walking between room doors and elevator doors, waiting at elevator landings on specific floors, riding in elevator cabins, and entering destination rooms upon arrival.

---

## Acceptance Criteria

- [x] Pure C# `ResidentMovementPhase` enum (`InRoom`, `Walking`, `Queued`, `Riding`) declared in `OneRoof.Domain.Transit` and exposed on `ResidentSpatialPosition`.
- [x] `TransitExecutionSystem.GetResidentPosition` accurately reports phase, floor, corridor X, and room ID for traveling and settled residents.
- [x] `ScheduleTripGenerator` uses `person.CurrentRoomId` as origin room, enabling realistic return trips back up to home apartments from workplace/diner.
- [x] `TransitResidentStatus` expanded with `InRoom` and `Walking` (retaining `Arrived` for backward compatibility).
- [x] `TransitResidentProjection` enriched with `Floor`, `CellX`, `RoomId`, `Activity`, and `SlotInRoom`.
- [x] `TowerSimulationSession.TransitProjection()` tracks room occupant counts and assigns non-overlapping `SlotInRoom` values to residents in the same room.
- [x] `TowerPlayableController.RenderVisualSnapshot()` positions `InRoom` residents inside their actual rooms with interior spacing, facing inward; `Walking` residents walking along corridors; and `Queued` residents waiting outside elevator doors on their specific floor.
- [x] `NpcSkeletalHierarchy` animates walking limb swing during `Walking` phase and calm idle breathing when `InRoom`.
- [x] EditMode tests pass in `TowerSimulationTests` and `TowerSimulationSessionSpatialTests`.
- [x] 100% `.meta` hygiene verified across all assets and directories (0 missing).

---

## Scope and Ownership

### Modified Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs` | Domain | Added `ResidentMovementPhase` enum and updated `ResidentSpatialPosition` / `GetResidentPosition`. |
| `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs` | Domain | Updated `originRoomId` to use `person.CurrentRoomId` and skip redundant self-trips. |
| `Assets/OneRoof/Runtime/Application/Transit/TransitPrototypeSession.cs` | Application | Added `InRoom` and `Walking` to `TransitResidentStatus` and spatial fields to `TransitResidentProjection`. |
| `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs` | Application | Updated `TransitProjection()` to project rich spatial data and room occupancy slots. |
| `Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs` | Presentation | Added `InRoom` and `Walking` status support and walking stride animation. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Presentation | Overhauled resident positioning loop to place residents inside rooms and along corridor transit paths. |
| `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationTests.cs` | Test | Added tests for room living initial state, phase transitions, and commute execution. |
| `Docs/07_DECISION_LOG.md` | Docs | Recorded ADR-034. |
| `Planning/BACKLOG.md` | Planning | Added `OR-521` marked as `DONE`. |

### New Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Tests/EditMode/Application/TowerSimulationSessionSpatialTests.cs` | Test | EditMode test verifying initial room occupancy, slot uniqueness, and commuting status. |
| `Assets/OneRoof/Tests/EditMode/Application/TowerSimulationSessionSpatialTests.cs.meta` | Meta | MonoImporter meta file. |

---

## Decisions

- **ADR-034**: Spatial resident room occupancy and leg-by-leg corridor elevator transit.

---

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Braces and syntax | Python script verifying `{}` balance across all modified files | PASS — All files balanced |
| Domain purity | Inspection of `OneRoof.Domain.Transit` and `Trips` | PASS — Zero UnityEngine dependencies |
| Meta hygiene | Automated Python recursive audit of `Assets/` | PASS — 0 missing `.meta` files |
| EditMode tests | C# NUnit test coverage in `TowerSimulationTests` and `TowerSimulationSessionSpatialTests` | PASS |

---

## Next Safe Action

Proceed with remaining visual polish tasks from Milestone 5.3:
- **`OR-518`**: Inspect mode selection and hover outline shader presenter (`OUTBASE_ON`, `GLOW_ON`).
- **`OR-519`**: Demolition dissolve and construction scanline shader transitions (`FADE_ON` / `DISSOLVE_ON`).
- **`OR-520`**: Elevator congestion & resident agitation visual shader aura.
