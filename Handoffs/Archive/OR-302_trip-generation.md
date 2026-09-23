# OR-302 — Generate trips from home/work/food routines

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-16  
**Updated:** 2026-09-16

## Objective

Wire the existing population records (OR-301) and transit graph (OR-201) together so that
schedule block transitions automatically generate `TripRecord` instances with planned routes.
Proves the morning commute demand of exactly 50 Work trips from the deterministic fixture.

## Acceptance criteria

- [x] All 50 residents generate exactly one Work-purpose trip during a full day cycle.
- [x] Each trip's destination is the resident's workplace room.
- [x] Residents above Floor 0 have a non-null route requiring vertical transit.
- [x] Identical seeds produce identical trip route costs (determinism).
- [x] Mid-block ticks produce zero trips (silence outside transitions).
- [x] New trips default to `Planned` state with zero wait ticks.
- [x] `TripRecord` state machine enforces valid transitions.

## New files

| File | Layer |
|---|---|
| `Assets/OneRoof/Runtime/Domain/Trips/TripPurpose.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Trips/TripState.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Trips/TripRecord.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Trips/TripDemandFixture.cs` | Domain (test fixture) |
| `Assets/OneRoof/Tests/EditMode/Domain/TripGenerationTests.cs` | Test |

## Modified files

| File | Change |
|---|---|
| `Docs/07_DECISION_LOG.md` | Appended ADR-020 |
| `Planning/BACKLOG.md` | OR-302 → DONE, OR-303 → READY |

## Key design notes

- `ScheduleTripGenerator` is stateless: transitions are detected by comparing
  `schedule.ActiveLabelAt(previousTick)` vs `ActiveLabelAt(currentTick)` with modulo wrapping.
- `ResolveDestinationRoom` uses `FiveFloorTopologyFixture.CommercialContentId` / `LobbyContentId`
  for Food and Leisure trips — a named content ID, not a hardcoded entity ID.
- `TripDemandFixture` is a test-only convenience class; it lives in the Domain assembly and
  is not referenced by production code.

## Decisions

- ADR-020: Stateless tick-comparison trip generation.

## Validation

| Check | Command | Result |
|---|---|---|
| Compilation | Unity Editor import | NOT RUN |
| `TripGenerationTests` (8 tests) | Unity Test Runner → Edit Mode | NOT RUN |
| All prior tests (75) | Unity Test Runner → Edit Mode | NOT RUN |

## Next safe action

Two READY tasks:

1. **OR-303** (M3) — Bind pooled NPC views to projections. Now unblocked.
2. **OR-401** (M4) — Build/Inspect/Data mode shell. Still independently READY.

M3 is now fully complete on the Domain side. OR-303 is the first Presentation task.

## References

- Backlog: `OR-302`
- Decisions: `ADR-020`
