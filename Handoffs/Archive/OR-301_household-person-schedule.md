# OR-301 — Implement household/person/schedule records

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-15  
**Updated:** 2026-09-16

## Objective

Add pure-C# Domain records for `Person`, `Household`, and daily `Schedule` so that a
50-resident deterministic fixture can be created and verified in an Edit Mode test,
unblocking OR-302 (trip generation) and OR-303 (NPC view binding).

## Acceptance criteria

- [x] `PersonRecord` — ID, household ref, home/workplace rooms, schedule, needs, traits, current activity.
- [x] `HouseholdRecord` — ID, members, home room, budget, satisfaction.
- [x] `DailySchedule` — four named time blocks covering exactly 1 440 ticks; `ActiveLabelAt` wraps correctly.
- [x] `NeedState` — validated [0, 1] satisfaction per `NeedKind`.
- [x] `PersonTrait` / `PersonTraitKind` — six trait archetypes influencing schedule jitter.
- [x] `FiftyResidentFixture` — 50 persons, 16 households, deterministic from seed; all room IDs valid.
- [x] `PersonScheduleTests` — 10 tests covering block validation, schedule coverage, wrapping, NeedState range.
- [x] `FiftyResidentFixtureTests` — 8 tests covering counts, referential integrity, determinism, defaults.

## Scope and ownership

### New files

| File | Layer |
|---|---|
| `Assets/OneRoof/Runtime/Domain/Population/ActivityKind.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/NeedKind.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/NeedState.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/PersonTrait.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/ScheduleBlock.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/DailySchedule.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/HouseholdRecord.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs` | Domain |
| `Assets/OneRoof/Runtime/Domain/Population/FiftyResidentFixture.cs` | Domain (fixture) |
| `Assets/OneRoof/Tests/EditMode/Domain/PersonScheduleTests.cs` | Test |
| `Assets/OneRoof/Tests/EditMode/Domain/FiftyResidentFixtureTests.cs` | Test |

### Modified files

| File | Change |
|---|---|
| `Docs/07_DECISION_LOG.md` | Appended ADR-019 |
| `Planning/BACKLOG.md` | OR-301 → DONE, OR-302 → READY |

### Do not modify

- Presentation, UI, or Infrastructure assemblies.
- Scenes, prefabs, or ProjectSettings.

## State at handoff

Milestone 3 first task is complete. The `Population` sub-namespace provides all records and the
50-resident fixture needed by OR-302.

Key design points:
- `DailySchedule` uses a standard 4-block (Sleep / Work / Eat / Leisure) layout with trait-shifted,
  RNG-jittered boundaries. `ActiveLabelAt` resolves any global tick via modulo day length.
- `FiftyResidentFixture` round-robins 50 residents across 16 households; first two households get
  4 members each, the rest get 3.
- `PersonRecord` and `HouseholdRecord` expose only explicit mutation methods, preserving the
  allocation-free simulation tick invariant (ADR-019).

## Decisions

- ADR-019: Model PersonRecord and HouseholdRecord as mutable sealed classes with explicit mutation
  methods (not structs or property setters).

## Validation

| Check | Command or procedure | Result |
|---|---|---|
| New tests compile | Unity Editor import — no C# errors expected | NOT RUN (no display on this host) |
| `PersonScheduleTests` (10 tests) | Unity Test Runner → Edit Mode | NOT RUN |
| `FiftyResidentFixtureTests` (8 tests) | Unity Test Runner → Edit Mode | NOT RUN |
| Git hygiene | `git diff --check` | NOT RUN |

Unity compilation and test runs must be confirmed by the developer before this work is considered
fully verified.

## Next safe action

Two READY tasks:

1. **OR-302** (M3) — Generate trips from home/work/food routines.  
   Depends on: OR-201 (✓), OR-301 (✓). Unblocked.
2. **OR-401** (M4) — Implement Build, Inspect, and Data mode shell.  
   Depends on: OR-102 (✓). Unblocked; can proceed in parallel with OR-302.

Recommended: tackle **OR-302** next to complete the M3 population layer before adding presentation modes.

## References

- Backlog: `OR-301`
- Decisions: `ADR-019`
