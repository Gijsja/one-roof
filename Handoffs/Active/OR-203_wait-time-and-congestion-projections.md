# OR-203 — Wait-time and congestion projections

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-15  
**Updated:** 2026-09-15

## Objective

Add read-only wait-time and congestion projections across floors and elevator banks, and prove that the fixed 50-resident morning commute scenario produces stable, reproducible metrics under baseline and expanded elevator capacities.

## Acceptance criteria

- [x] Floor-level and bank-level congestion projections expose queue counts, wait metrics, bottleneck floor, and tiered severity (`Clear`, `Moderate`, `Heavy`, `Severe`).
- [x] Fixed 50-resident commute scenario produces deterministic delivered counts and identical wait metrics across repeated runs.
- [x] Expanding elevator capacity (2 cars vs 1 car) measurably reduces completed wait ticks in the fixed scenario.
- [x] Morning commute bottleneck at Floor 0 is correctly identified and evaluated as `Severe`.
- [x] Projections are pure read-only models without leaking mutable domain internals or Unity engine objects.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/Domain/Transit/CongestionSeverity.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/CongestedElevatorScenario.cs`
- `Assets/OneRoof/Runtime/Application/Transit/FloorCongestionProjection.cs`
- `Assets/OneRoof/Runtime/Application/Transit/ElevatorBankCongestionProjection.cs`
- `Assets/OneRoof/Runtime/Application/Transit/TransitCongestionService.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/CongestedElevatorScenarioTests.cs`
- `Assets/OneRoof/Tests/EditMode/Application/TransitCongestionProjectionTests.cs`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`

Do not modify:

- Presentation or UI assemblies.
- Scenes or ProjectSettings files.

## State at handoff

Milestone 2 is completely finished:
- `CongestedElevatorScenario` models the canonical 50-resident morning peak with deterministic delivery and wait times.
- `TransitCongestionService` projects floor queues, overall severity, and the primary bottleneck floor into immutable DTOs.
- Adding capacity (2 cars) reduces average wait and completion ticks measurably.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Transit/CongestionSeverity.cs` — `CongestionSeverity` enum and threshold evaluator.
- `Assets/OneRoof/Runtime/Domain/Transit/CongestedElevatorScenario.cs` — Deterministic 50-resident commute fixture.
- `Assets/OneRoof/Runtime/Application/Transit/FloorCongestionProjection.cs` — Per-floor queue and wait projection.
- `Assets/OneRoof/Runtime/Application/Transit/ElevatorBankCongestionProjection.cs` — Root bank congestion projection.
- `Assets/OneRoof/Runtime/Application/Transit/TransitCongestionService.cs` — Projection service.
- `Assets/OneRoof/Tests/EditMode/Domain/CongestedElevatorScenarioTests.cs` — Scenario tests for stability and capacity improvement.
- `Assets/OneRoof/Tests/EditMode/Application/TransitCongestionProjectionTests.cs` — Projection validation tests.
- `Docs/07_DECISION_LOG.md` — Appended ADR-018.
- `Planning/BACKLOG.md` — Marked OR-203 DONE (completing Milestone 2).

## Decisions

- ADR-018: Project transit congestion through read-only floor and bank projections with discrete severity tiers.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| 50-resident scenario stability | Pure Domain test `CongestedElevatorScenarioTests` | PASS |
| Congestion projections | Edit Mode test `TransitCongestionProjectionTests` | PASS |
| Git hygiene | `git diff --check` | PASS |

## Next safe action

Begin Milestone 3 (**OR-301**: Implement household/person/schedule records) or Milestone 4 (**OR-401**: Implement Build, Inspect, and Data mode shell).

## References

- Backlog: `OR-203`
- Decisions: `ADR-018`
