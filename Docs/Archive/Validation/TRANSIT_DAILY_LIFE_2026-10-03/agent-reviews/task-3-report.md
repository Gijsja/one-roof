# Task 3 implementation report

Status: Task3 fix1 implementation complete and sources frozen for scoped re-review. Fresh post-fix focused verification is 188/188 passing, 0 failures and 0 skips. Pre-fix full Domain verification was 751/751; no broad repeat was requested after this focused correction. Controller owns re-review, native validation, docs and cutoff checkpoint commit. No Unity runs, scene edits, commits, pushes, stashes or worktree operations performed by this worker.

## Implemented behavior

- `EmploymentRoutineFactory` is shared by production leasing, `GoldStandardCityFixture` and `DiversifiedTowerFixture`. Actual workplace content permits office day, diner early/late, retail and Outside early/day/late, and clinic/security/maintenance early/day/late/overnight cohorts. Stable identity selects the supported cohort; saved seed, bounded individual offset and trait adjust appointment times. Queue length never alters schedule obligations.
- Each routine covers all 1440 minutes contiguously: preparation and breakfast, 240 minutes work, a real 30-minute lunch opportunity, another 240 minutes work, home time, dinner, a bounded optional outing window, home time, and 480 minutes shift-relative sleep. These are authored routine design values, not measured C0 tuning or acceptance changes.
- Midnight-split blocks retain logical episode start and appointment start/end. Lunch return is a new required travel/attendance episode inside the original work appointment; a successful morning arrival cannot mask a missing return. Arrival does not restart or extend the scheduled workday.
- The generator anticipates work departure from physical hallway/stairs durations plus the bank's initial-arrival ETA overload. Routine route cache is bounded at 2048 entries and cleared when topology changes. Required appointment observation remains before travel/commitment/retry suppression; a predeparture attempt retains its original episode and submission at the work boundary.
- Person state persists appointment and segment identity, departure, arrival/lateness, individual seed, daily optional choice and choice day. Optional satisfied/no-action windows remain outside required-demand accounting. A chosen optional outing may travel without creating required demand.
- Initial snapshot needs derive from routine phase and elapsed time since scheduled sleep/meals, using the existing needs-system rate constants. These are creation-time snapshot reserves only. Production job changes replace generated routines while preserving needs, location, active travel and accrued appointment evidence; legacy/authored schedules are not regenerated.
- `GoldStandardCityFixture.Create(ulong seed = Seed)` allows deterministic reference seed comparisons without changing topology or capacity. Existing three cars / 20 seats each remain unchanged.

## Exact worker file ownership

Modified:

- `Assets/OneRoof/Runtime/Domain/Population/DailySchedule.cs`
- `Assets/OneRoof/Runtime/Domain/Population/ScheduleBlock.cs`
- `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs`
- `Assets/OneRoof/Runtime/Domain/Population/ResidentPurposeSystem.cs`
- `Assets/OneRoof/Runtime/Domain/Population/DynamicScheduleArbitrator.cs`
- `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs`
- `Assets/OneRoof/Runtime/Domain/Population/RoutineDecision.cs` (Task1 interface extension)
- `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs`
- `Assets/OneRoof/Runtime/Domain/Economy/LeasingDemandSystem.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/GoldStandardCityFixture.cs`
- `Assets/OneRoof/Runtime/Domain/Topology/DiversifiedTowerFixture.cs`
- `Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs` (PersonSaveData routine fields only)
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` (granted one-line TripGenerator constructor bank wiring only)
- `Tools/DesignValidation/CrisisCheckpointTests.csproj` (EmploymentRoutineTests inclusion)
- `Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs` (controller-granted narrow fixture correction: explicit authored minute timing, retained round-trip assertions, additional timer/unit assertions)

Added:

- `Assets/OneRoof/Runtime/Domain/Population/EmploymentRoutineFactory.cs` and paired `.meta`
- `Assets/OneRoof/Tests/EditMode/Domain/EmploymentRoutineTests.cs` and paired `.meta`
- This report and task-3 evidence artifacts.

## TDD commands and observed results

All targeted runs use `/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj -- <result.xml>`, with stdout/stderr redirected to the paired `.log`.

Historical results observed before the runtime/context change (the old `/tmp/task3-*` artifacts vanished and could not be recovered): baseline 136/136; factory RED 2 failures / 138, GREEN 138/138; appointment RED 4 / 142, GREEN 142/142; continuation RED 1 / 155 (satisfied lunch still Working), GREEN 155/155; production GREEN 159/159; reassignment RED 2 / 160 (unsupported stale cohort and non-reconstructible mixed seed), GREEN 160/160. These historical counts are observation records, not currently retained XML evidence.

Fresh retained evidence under `task-3-evidence`:

- `red-job-evidence.xml` / `.log`: 161 total, 160 passing, 1 expected failing test proving employment change erased accrued appointment evidence.
- `red-appointment-evidence.xml` / `.log`: 161 total, 159 passing, the preceding failure plus a new test proving a lunch-return decision reported return segment start rather than original appointment start.
- `green-production.xml` / `.log`: fresh 161/161 passing after preserving employment evidence and whole-appointment start.
- `red-available-route.xml` / `.log`: 162 total, 161 passing, expected failure demonstrating infinite no-car pickup triggered work departure 100 minutes too early. Fixed by estimating the available stairs path.
- `green-final.xml` / `.log`: fresh 162/162 passing (26 EmploymentRoutineTests cases plus the 136-test portable baseline).
- `all-domain.xml` / `.log`: 751 total, 750 passing, 1 failing: `AggregateSerializationTests.ElevatorBank_ToSaveData_And_FromSaveData_RoundTripsAccurately`. The fixture calls `car.RestoreState(...Moving, Up, 3,...)` before constructing a calibrated bank. Round-trip rejects `Invalid elevator phase progress` at ElevatorBank.cs:812. Task3 does not modify ElevatorCar/ElevatorBank or this legacy test. A fresh accepted-after-task2 snapshot reproduction (`after-task2-aggregate.csproj/.xml/.log`) confirms the same baseline failure: 4 total / 3 passed / 1 failed. Controller granted a narrow legacy fixture setup correction: use explicit 5/2/2 authored minute timings for the existing three-minute RestoreState premise. All existing round-trip assertions are retained, and timer/unit assertions are added. No loader/timing production invariants changed.
- Retained `all-domain.csproj` includes all Domain tests except `UndergroundTickBudgetTests` and `AssemblyBoundaryTests`, which require native/toolchain-specific validation. Source compilation uses the production pure-C# Domain glob.
- `green-frozen.xml` / `.log`: final focused rerun 162/162 passed, 0 failed / 0 skipped.
- `all-domain-green.xml` / `.log`: final full Domain rerun 751/751 passed, 0 failed / 0 skipped after the controller-granted legacy aggregate setup correction. No further broad runs needed.
- `git diff --check` completed with no diagnostics.
- `task-3-files.txt` is the exact 19-file source/test/project manifest; `task-3-evidence/frozen-sha256.txt` records the final source hashes for independent review.

The eight full-simulation checkpoint cases each compare 700 future ticks of exact activities, location, purposes, needs, work metadata, deterministic day choice, routine decisions, demand episode identity/submission/trip/terminal state and next trip ID. Cases cover pre/post midnight, overnight work and sleep, day choice, departure, late work phase and urgent hunger. Additional focused tests cover overnight logical identity, two separate required attendance episodes around lunch, missed/late return, expired arrival, satisfied lunch pause, initial phase needs, production leasing, generated employment reassignment and legacy schedule preservation.

## Save compatibility and concerns

Person routine metadata has its own `routineVersion = 1`; existing save-wide versions and transit formats are untouched. Version-zero authored label/start/end arrays are preserved and load as legacy routines, without replacing them with new employment schedules. Existing legacy compressed five-floor behavior remains on the existing code path. New routines save their exact block times and logical boundaries rather than reconstructing them on load.

The factory adds legitimate demand around real meal opportunities and shift boundaries; demand counts can change and must be published by later complete-demand measurements. Current needs rates may still cause deprivation/urgent interruption; Task4 owns physiological calibration and provider availability behavior. No free meals, reserve restoration, extra cars, relaxation of deadlines or claim of C0 closure was introduced. Native compilation, Unity test validation and performance measurement remain controller responsibilities.

## Handoff

Sources and tests are frozen at `frozen-sha256.txt`. Worker ownership is released to controller for independent review and native validation. The production factory and appointment work is complete; no further worker mutations are planned. Controller owns canonical docs, final native/performance/complete-demand evidence and the newly requested commit.

## Fix1: versioned checkpoint validation and ordinary production activity clock

Both Important findings from task-3-review.md are repaired. Changed fix1 source files: PopulationState.cs, PersonRecord.cs, ScheduleBlock.cs, ResidentPurposeSystem.cs, TowerSimulation.cs (granted population restore call only), and EmploymentRoutineTests.cs.

- Routine version validation runs before any schedule fallback. Version1 requires a supported cohort and all six schedule arrays, equally sized and non-empty. Generated routines additionally require full contiguous day coverage and supported activity labels. Explicit logical boundaries enforce appointment start <= episode start <= physical block start < physical block end <= appointment end, bounded one-day logical duration and legal midnight offsets. Version0 authored schedules retain their previous restore path.
- Routine checkpoint observations require coherent unset/set appointment state, supported choice/sentinel values, appointment/segment/demand/departure/arrival relationships, and bounded appointment duration. Decimal duration comparison prevents malicious long arithmetic overflow. Appointment observations retain legal negative logical start offsets and early departure before appointment start. Old-job evidence is not required to match the new job's schedule.
- PopulationState.FromSaveData adds an optional authoritative restoredTick. TowerSimulation passes its restored world clock so future choice days, observed demand/departure/arrival timestamps and appointments beyond the next daily cycle are rejected. Direct aggregate callers remain compatible: when no clock is supplied, structure/relationships are validated without inventing a current tick.
- The first validation run exposed an existing real recording inconsistency: local attendance at a required segment was recorded first, then a later urgent return trip attached its departure to the already-attended episode. A focused3300-tick probe recorded the values, and a dedicated RED caught the branch. RecordWorkDeparture now leaves the original local attendance's initial departure unset when attendance was already recorded. New lunch-return segments still start separate evidence. Strict paired timestamp validation remains in place, and accrued attendance/lateness is retained.
- Generated employment routines are explicitly excluded from the legacy compressed-fixture heuristic. Real Late midnight Eat fragments now commit30-minute meals and60-minute home/leisure activity; compressed legacy schedules retain5-minute commitments.

Fresh retained fix1 evidence, all under task-3-evidence:

| Artifact | Result |
| --- | --- |
| fix1-red.xml / .log | 180 total /162 passed /18 expected failures: sixteen malformed payloads and the production meal/home clock cases |
| fix1-green.xml / .log | 180 total /179 passed /1 failure: ReferencePartialDayCannotPassAndCompleteDayPersists exposed the local-attendance/later-departure recording issue |
| fix1-probe/probe.csproj, Program.cs; fix1-probe.log | Focused3300-tick source-record discriminator; confirms arrival at segment start precedes a later urgent-return departure |
| fix1-chronology-red.xml / .log | 185 total /183 passed /2 failures: explicit recording regression and existing3300-tick replay regression |
| fix1-bounds-red.xml / .log | 188 total /183 passed /5 failures: preceding two plus unknown generated activity, overflowed logical duration and implausible future appointment |
| fix1-final.xml / .log | **188 total /188 passed /0 failed /0 skipped** after all fixes |

Exact suite command for each phase: `/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj -- .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/<phase>.xml > .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/<phase>.log 2>&1`. Probe command: `/home/geisha/.local/bin/dotnet run --project .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/fix1-probe/probe.csproj > .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/fix1-probe.log 2>&1`.

Positive coverage includes valid negative midnight observations, early predeparture with an appointment still in the future, retained prior-job/lunch evidence, direct aggregate API clock compatibility, original version0 authored schedules, the existing eight700-tick full-simulation replay checkpoints, and the existing3300-tick reference restore. `git diff --check` is clean. No further broad run, Unity run, commit, transit source change or assertion relaxation was performed by this worker.

Fix1 sources are frozen in task-3-evidence/fix1-frozen-sha256.txt (same19-file Task3 manifest). Ownership is released for scoped re-review and controller cutoff validation/commit. Tasks4/5 were not started by this worker.
