# Task 3 review

Spec: **FAIL**. Quality: **Needs fixes**.

## Spec compliance

- Required corruption validation is incomplete in the new routine save format: `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs:434` and `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs:147` accept invalid metadata rather than rejecting it (Important finding 1).
- Production leasing and both fixtures share the employment factory (`LeasingDemandSystem.cs:115`, `GoldStandardCityFixture.cs:81`, `DiversifiedTowerFixture.cs:142`). Workplace content selects supported shifts, identity selects the cohort, and the seed/trait supplies bounded offsets (`EmploymentRoutineFactory.cs:17`). The factory supplies contiguous full-day windows and exactly 480 Work minutes around a 30-minute lunch (`EmploymentRoutineFactory.cs:34`).
- Work segments retain the whole appointment and a separate lunch-return episode (`EmploymentRoutineFactory.cs:37`, `PersonRecord.cs:120`); predeparture submission identity is retained by `DynamicScheduleArbitrator.cs:132`. Anticipation uses physical durations and the bank estimate (`ScheduleTripGenerator.cs:267`), without changing authored appointment times.
- Initial needs enter only creation paths (`LeasingDemandSystem.cs:117`, `GoldStandardCityFixture.cs:85`, `DiversifiedTowerFixture.cs:144`). Generated job changes retain recorded appointment fields and do not regenerate legacy schedules (`PersonRecord.cs:197`, `EmploymentRoutineTests.cs:286`). Exact authored block times are serialized (`PopulationState.cs:214`) and the legacy load path remains (`PopulationState.cs:425`).
- The optional reference seed API retains default seed and the authored three 20-seat cars (`GoldStandardCityFixture.cs:19`, `GoldStandardCityFixture.cs:91`). New source and test assets have paired metas; changes contain no engine references, scene references in saved state, enum reordering, or per-Resident Update.
- Cannot verify from this task diff: integrated >=99% completion within 360 submission ticks; terminal nearest-rank p95 queue wait <=60 including zero/pre-stairs waits; live maximum <=240; zero unresolved required unreachable demand; full physiology/performance and validated command interventions. These remain Tasks 4/5 acceptance work, not additional Task 3 defects.

## Strengths

- `EmploymentRoutineTests.cs:109` checks that a successful morning attendance cannot mask a missing lunch return; `EmploymentRoutineTests.cs:133` checks expired arrival, and `EmploymentRoutineTests.cs:175` checks late return against the fixed appointment end.
- `EmploymentRoutineTests.cs:317` compares 700 future ticks of activities, needs, choices, work evidence and real demand state across eight full-simulation save checkpoints. This is substantive replay coverage.
- `AggregateSerializationTests.cs:118` makes the existing three-minute restore premise explicitly use authored minute timings, retaining round-trip assertions and adding units/timer checks at lines149-150. The retained after-task2 reproduction has the same pre-existing failure; this narrow granted correction is justified.

## Issues

### Critical

None found.

### Important

1. **[P1] Validate the complete versioned routine payload before restoring it.** `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs:434`, `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs:439`, `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs:147`, and `Assets/OneRoof/Runtime/Domain/Population/ScheduleBlock.cs:32` accept invalid new temporal data. A focused probe through `TowerSimulation.RestoreFromSaveData` against the frozen compiled sources accepted all of these exact corruptions of an otherwise valid reference save:
   - `routineVersion=1` with all three logical boundary arrays null (defaults discard overnight identity).
   - `employmentCohort=999` (silently downgraded to Legacy).
   - `routineVersion=2` with `scheduleLabels=null` (unsupported-version validation is bypassed by the legacy fallback).
   - Appointment `[-100,-50]`, segment `-500`, demand episode `-1`, departure1000, arrival2 (incoherent sentinel/chronology relationships).
   - At saved tick450, choice day20, appointment `[100,610]`, segment100, departure3000 and arrival4000 (future observations).
   - Every logical appointment end set to `-10000` (end before its start/block).

   This violates the explicit corrupt-temporal-payload constraint and can change overnight obligations or retained lateness after reload. Validate version independently of schedule-array presence, reject unknown cohorts, require complete equally sized v1 arrays, validate block/logical-boundary consistency and a coherent unset/set appointment checkpoint, and validate observed timestamps against the restored clock. Preserve valid version-zero schedules and legitimate negative logical offsets for midnight continuation. Add focused malformed-payload tests; do not weaken supported legacy migration. The production restore caller at `TowerSimulation.cs:1494` currently supplies no authoritative clock to population restoration.

2. **[P2] Generated production routines fall into the compressed five-floor clock.** `Assets/OneRoof/Runtime/Domain/Population/EmploymentRoutineFactory.cs:48` introduces midnight-split production blocks, but `Assets/OneRoof/Runtime/Domain/Population/ResidentPurposeSystem.cs:57` identifies a compressed fixture solely by the first block's duration. A real reference Late cohort whose first midnight Eat fragment is23 ticks therefore gets a five-tick meal commitment from line59/line77. The focused probe confirmed `production Late meal firstblock=Eat/23 meal commitment=5`. Other home/leisure commitments also use the five-tick clock for these Residents, making production behavior depend on an incidental midnight fragment. Explicitly exclude employment routines from the legacy compression heuristic and test real production Late cohorts alongside the preserved compressed legacy fixture. This is a clock-selection integration defect, independent of Task 4's need-rate/provider calibration.

### Minor

None requiring action.

## Checks and assessment

- Read the exact 19-file after-task2 -> frozen Task3 diff once in bounded sections. The `ResidentPurposeSystem.AdvancePerson` hunk ends mid-function; read its continuation solely to check the production meal-duration risk.
- Focused outside-diff caller checks: `TowerSimulation.cs:260` permits trips based on move-out eligibility, not routine commitment; `Transit/TransitExecutionSystem.cs:660` physically completes arrivals before invoking `ResidentPurposeSystem`, so the scheduled-end logic is on the real arrival path. `TowerSimulation.cs:1494` confirms absence of population-clock validation. No broader source crawl, subagents, Git mutations, or source edits.
- Parsed retained final XMLs: `task-3-evidence/green-frozen.xml`162/162 and `all-domain-green.xml`751/751, both0 failed/0 skipped. Paired logs contain only clean count summaries. Parsed retained after-task2 aggregate reproduction:4 total/3 passed/1 failed. Historical missing `/tmp` counts were not treated as fresh evidence. Controller additionally reports scoped native26/26 before fixes; final integrated native/performance evidence is still pending.
- Ran only `/home/geisha/.local/bin/dotnet run --project /tmp/task3-review-probe/probe.csproj`, a focused malformed-save/production-clock probe referencing the frozen existing assembly. No suite rerun.

**Task quality: Needs fixes.** Routine structure and replay coverage are strong, but corrupt new checkpoints are accepted and some new production cohorts inherit the fixture's compressed clock. Both require focused fixes before Task 3 is accepted.
