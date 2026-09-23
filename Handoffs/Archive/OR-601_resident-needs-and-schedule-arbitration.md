# OR-601 — Resident Needs & Dynamic Schedule Arbitration

**Status:** DONE  
**Owner:** Codex  
**Started:** 2026-09-19  
**Updated:** 2026-09-19  

## Objective

Replace static, clock-only schedule transitions with living resident psychology. Residents track five core needs (**Hunger**, **Energy**, **Social**, **Hygiene**, **Purpose**) that deplete and recover according to daily activity and personality traits. Critical need depletion dynamically arbitrates destination choices, overriding rigid schedule boundaries to seek food, rest, hygiene, or social connection.

## Acceptance criteria

- [x] Five core needs (**Hunger**, **Energy**, **Social**, **Hygiene**, **Purpose**) modeled in pure C# domain records.
- [x] Need decay and recovery system (`ResidentNeedsSystem`) advances per simulation tick based on activity (`Sleeping`, `Eating`, `Working`, `Leisure`, `Commuting`, `Idle`) and traits (`Introvert`, `Extrovert`).
- [x] Dynamic arbitration service (`DynamicScheduleArbitrator`) arbitrates destination decisions, allowing critical needs to override rigid circadian schedules.
- [x] `TripPurpose.Hygiene` added to allow residents to return home to clean up.
- [x] `FiftyResidentFixture` initializes residents with all five core needs.
- [x] Save data serialization round-trips all five needs with backwards-compatible defaults for legacy saves.
- [x] 194 / 194 Edit Mode tests pass across all assemblies (Domain, Application, UI, Presentation, Content).

## Scope and ownership

Owned files:
- `Assets/OneRoof/Runtime/Domain/Population/NeedKind.cs`
- `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs`
- `Assets/OneRoof/Runtime/Domain/Population/ResidentNeedsSystem.cs`
- `Assets/OneRoof/Runtime/Domain/Population/DynamicScheduleArbitrator.cs`
- `Assets/OneRoof/Runtime/Domain/Population/FiftyResidentFixture.cs`
- `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs`
- `Assets/OneRoof/Runtime/Domain/Trips/TripPurpose.cs`
- `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/ResidentNeedsSystemTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/TowerSimulationCanExecuteTests.cs`
- `Docs/07_DECISION_LOG.md` (ADR-047)
- `Planning/BACKLOG.md` (OR-601 marked DONE)

## State at handoff

`ResidentNeedsSystem` runs inside `TowerSimulation.AdvanceOneTick()`, updating each resident's five core needs. `DynamicScheduleArbitrator` evaluates need pressures against circadian schedule blocks; critical deficits in Hunger (< 0.35), Energy (< 0.25), or Hygiene (< 0.25) initiate immediate targeted trips to diners or home rooms, while normal circadian transitions operate cleanly during comfortable conditions.

## Changes made

- `NeedKind.cs` — expanded with canonical 5 core needs (`Hunger`, `Energy`, `Social`, `Hygiene`, `Purpose`) with `Rest` alias.
- `PersonRecord.cs` — added `GetNeedSatisfaction()` and `HasNeed()` query helpers.
- `ResidentNeedsSystem.cs` — evaluates activity-based need decay/replenishment and trait scaling per tick.
- `DynamicScheduleArbitrator.cs` — arbitrates physiological need urgencies against schedule blocks.
- `TripPurpose.cs` — added `Hygiene` trip purpose.
- `ScheduleTripGenerator.cs` — evaluates arbitration on block changes and urgent physiological pressures.
- `TransitExecutionSystem.cs` — maps `TripPurpose.Hygiene` to in-room idle/refresh activity.
- `TowerSimulation.cs` — integrates `ResidentNeedsSystem` into simulation tick.
- `TowerSaveData.cs` & `PopulationState.cs` — serializes and deserializes all five needs with legacy fallbacks.
- `ResidentNeedsSystemTests.cs` — comprehensive unit tests for need dynamics, traits, and arbitration.
- `TowerSimulationCanExecuteTests.cs` — corrected room overlap test bounds to avoid shaft collision.

## Decisions

- **ADR-047**: Dynamic schedule arbitration driven by five core resident needs (Hunger, Energy, Social, Hygiene, Purpose).

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Pipeline preflight | `unity pipeline list --format json` | PASS — 1 ready Editor at port 7800 |
| Recompile | `unity command recompile`, `unity command recompile_status` | PASS — completed with 0 errors |
| Needs system tests | `unity command run_tests --mode editor --filter OneRoof.Domain.Tests.EditMode.ResidentNeedsSystemTests` | PASS — 11 / 11 |
| Trip generation tests | `unity command run_tests --mode editor --filter OneRoof.Domain.Tests.EditMode.TripGenerationTests` | PASS — 8 / 8 |
| Domain assembly tests | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Domain.Tests.EditMode` | PASS — 38 / 38 |
| Application assembly tests | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Application.Tests.EditMode` | PASS — 28 / 28 |
| UI assembly tests | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.UI.Tests.EditMode` | PASS — 10 / 10 |
| Presentation assembly tests | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Presentation.Tests.EditMode` | PASS — 106 / 106 |
| Content assembly tests | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Content.Tests.EditMode` | PASS — 12 / 12 |

## Known risks or failures

- None known.

## Next safe action

Proceed to **ART-004** (AssetLab validation tooling & Addressables packaging) or **OR-602** (Resident satisfaction scoring, grievances & satisfaction overlay).

## References

- Backlog: `OR-601`
- Roadmap: `M6.1`
- Decisions: `ADR-047`
