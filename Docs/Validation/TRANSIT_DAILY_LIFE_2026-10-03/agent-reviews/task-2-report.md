# Task 2 implementation report — source frozen for independent review

2026-10-03. No commits, subagents, Unity invocations, scene edits, or Task 1 semantic edits. Initial freeze portable Domain validation: **684 passed / 0 failed / 0 skipped**. The fix-round evidence appended below supersedes that initial source snapshot. Native Unity, presentation and complete C0 acceptance remain controller-owned and are not claimed here.

## Implemented behavior

- Default and tier elevators use exact integer seconds: tier 1–2 10 seconds/floor, tier 3–4 8, tier 5 6; opening/closing 5 seconds each, loading 10. Bank/execution outer ticks remain one minute. Twelve bounded 5-second advances consume phase budgets while preserving the remaining integer seconds, so an 8-second floor is not rounded to two substeps. Queue and rider minute counters advance once per outer tick.
- `ElevatorTimingConfig(int ticks, ...)` retains custom authored minute phase semantics. `FromSeconds(...)` explicitly selects the physical clock. Applying an upgrade preserves remaining elapsed-phase work; changing a legacy phase to the physical clock converts its remaining minute ticks to seconds before applying new timing.
- `TransitEdge.Cost` remains routing preference. New topology-produced edges opt into physical durations: horizontal cells at one second each, upward stairs 45 seconds/floor, downward stairs 30. Existing four-argument authored edges retain their minute-cost durations. Execution carries a per-leg second remainder across edges without rounding every hall/stair leg to a minute; restored old temporal progress keeps its legacy semantics.
- Full arrival estimates account for phase remainder, direction/turnaround, committed stops, directional queue counts and seats. Public `EstimateArrivalTicks(origin, destination)` returns the routine minute clock; mode comparison uses the unrounded internal seconds. A same-landing car already opening does not charge its door work twice.
- Ordinary stairs are limited to cumulative two upward/four downward floors, including the whole alternative route, and require an advantage over elevator arrival plus remaining halls. Directional effort adds 30 seconds/up floor and 15/down floor to generalized cost, without altering physical duration. Reconsideration starts at 15 live queued minutes, runs at most every 5 minutes, requires 180 seconds of improvement and retains 360-minute feasibility. Long stairs remain an unavailable-service escape. Failed planning retains exact FIFO and wait credit; successful diversion plans first, removes the queue passenger successfully, then commits the route and credits wait once.
- Dispatch now keeps directional pickup commitments, assigns actual compatible seat demand, keeps FIFO within each direction and uses stable car-ID ties. Reservations use configured `MinFloor`/`MaxFloor` and compatible committed demand ahead. One landing may use all three cars immediately. At 60 live wait ticks, an assigned empty car prioritizes its oldest call until pickup, bounding interference from fresh calls. Tier 1 retains a simple distance/stop-cost policy; higher tiers use ETA dispatch. The existing three-car cap and larger authored capacities remain unchanged.
- Aggregate saves preserve physical timing, exact car/leg remainder, legacy timing mode, directional commitments and reconsideration time. Versions explicitly distinguish an authored legacy duration from a physical zero-second edge. Older car saves lacking timing data retain their original tier minute timings; older route/progress data retains minute execution. New saves retain custom timings instead of silently applying default/tier values on reload.

## Owned files

Production: `Transit/ElevatorTimingConfig.cs`, `ElevatorTierProfile.cs`, `ElevatorCar.cs`, `ElevatorBank.cs`, `HierarchicalTransitGraph.cs`, `TransitEdge.cs`, `TransitExecutionSystem.cs`; `Persistence/TransitSaveData.cs`; only transit DTO portions of `Persistence/TowerSaveData.cs`.

Tests: new `TransitChoiceAndClockTests.cs` and `DirectionalDispatchTests.cs` with paired metas; controller-approved calibrated expectation updates in `ElevatorBankTests.cs`, `ElevatorTierPersistenceTests.cs`, `HierarchicalTransitGraphTests.cs`, `TowerSimulationCanExecuteTests.cs`. Portable inclusion added to `Tools/DesignValidation/CrisisCheckpointTests.csproj`.

The original custom minute timing precision tests remain intact. Obsolete default-tier tests now assert second values. An upgrade-during-travel test uses explicit old minute timing so its in-flight premise remains meaningful. The tier benefit fixture uses eleven floors because one-floor trips round to one minute at all physical tiers. The old one-floor no-car reroute expectation now asserts actual completion in one minute (3 hall seconds + 45 stair seconds + 4 hall seconds).

## RED / GREEN evidence

Artifacts are retained in `task-2-evidence/` beside this report, with originals under `/tmp/`.

- `task2-red-initial.xml/log`: **63 total / 55 passed / 8 failed** before production changes. Clock journey and hallway movement failures were expected. The initial one-landing failures combined the old slower default doors and dispatch; they were subsequently isolated below.
- Initial thirteen-floor tier-3 expectation of two minutes was a fixture arithmetic error: 13×8 + origin 5/10/5 + destination opening 5 = 129 seconds, requiring three minute boundaries. The calibrated fixture was corrected to eleven floors: 135/113/91 seconds at tier 1/3/5, giving 3/2/2 outer ticks. Separate literal floor/remainder assertions after 60 seconds discriminate exact tier-3 (floor 7, 4 seconds remaining) and tier-5 (floor 10, 6 seconds remaining) motion.
- `task2-red-compatibility.xml/log`: **74 total / 73 passed / 1 failed**, the unfinished legacy door phase was incorrectly left at 120 seconds after its minute tick instead of 60 on upgrade. Conversion fixed this from the failing test.
- `task2-red-eta.xml/log`: **89 total / 88 passed / 1 failed**, an already-opening eleven-floor tier-3 pickup was estimated at three minutes instead of the correct 113-second/two-minute arrival. Current-phase door handling fixed it.
- `task2-all-domain-red.xml/log`: **677 total / 663 passed / 14 failed**. The failures were obsolete default-clock expectations or premises in the four test files listed above. They were updated to approved calibrated behavior, retaining authored minute precision checks.
- `task2-dispatch-red.xml/log`: **4/4 passed characterization**, despite the exploratory filename. At two legacy ticks the old dispatcher had time to recruit a third car after first boarding; that fixture did not isolate immediate reserve behavior and is not claimed as RED.
- `task2-dispatch-isolated-red.xml/log`: **9 total / 1 passed / 8 failed** against the controller's retained `after-task1` Domain snapshot, with matched explicit minute timings. Four one-landing fixtures leave 8 Residents queued after one tick in the old dispatcher; new dispatch boards all 24. Mixed directions leave the second car idle instead of accepting its pickup. Configured-top reservations fail at 5-floor, 40-floor and negative-floor banks; the 30-floor/29 endpoint passes as the intentional hardcoded-endpoint control. This isolates dispatch defects from speed changes.
- `task2-all-domain-frozen.xml/log`: **684 total / 684 passed / 0 failed / 0 skipped** against frozen current sources, including the isolated dispatch fixtures and all engine-free Domain test files. Excludes only `AssemblyBoundaryTests.cs` (Unity assembly topology) and `UndergroundTickBudgetTests.cs` (Unity API); native validation is required for these. `git diff --check` passed; changed transit files have no UnityEngine/UnityEditor references.

Final portable command:

```sh
DOTNET_CLI_HOME=/tmp/task2-dotnet-home /home/geisha/.local/bin/dotnet run --no-restore --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-all-domain/task2-all-domain.csproj -- /tmp/task2-all-domain-frozen.xml
```

The full runner project, isolated snapshot fixture/project, and SHA-256 hashes of all owned source/test files are retained in `task-2-evidence/`. The existing checked-in checkpoint runner includes both new test classes. Initial silent portable runs were interrupted too early before evidence; this was a diagnostic error, not an environment blocker. Successful runs required no sandbox escalation.

## Coverage and review limits

Tests cover F29→F0 queues 0/3/20 after prolonged waits; eligible 1–2-floor journeys and 3–4 downward advantage/no-advantage; forbidden three-up/five-down ordinary alternatives; long no-car escape with actual 15-minute arrival; deadline rejection, FIFO/wait invariance, live first-leg patience, accepted single wait credit, cadence save replay; exact 8/6-second movement, custom timing/save migration, partial stairs and car causal replay; 5/30/40-floor and negative-floor endpoints, mixed directions/FIFO, full-car ETA, stable pickups, continuing fresh-demand fairness, and the same 30-person peak through actual legal tier-1→5 upgrade commands with unchanged ten seats.

ETA is a deterministic bounded estimate, not an oracle for future arrivals or prospective passengers. Batch penalties use serviced span and committed demand; complete C0 fairness/arrival distributions and native <4ms tick budgets still need measurement. Substeps perform bounded additional dispatch work and allocate temporary collections; native performance should be reviewed before acceptance. Clock advancement is retained at outer-tick save boundaries; no asynchronous mid-method checkpoint contract is claimed. The 60-tick oldest-call protection test demonstrates continuing-demand service for its fixture, not a proof for every adversarial queue sequence. Broader mobility constraints remain outside this task's scope.

Sources are frozen at the hashes in `task-2-evidence/source-hashes.json`, ready for independent review. No C0 denominator, fixture obligations or acceptance thresholds were changed.


## Review fix round 1 — corrected source frozen and released

Both Important findings in `task-2-review.md` are addressed. No commits, subagents or Unity runs; no all-Domain sweep was repeated. One narrowly granted aggregate hook in `TowerSimulation.cs:1518` now passes `sim.Clock.CurrentTick` to active-transit restoration.

### FIFO estimate

Added a held-passenger overload of `EstimateArrivalTicks` and an optional held ID for the internal second estimate. Reconsideration now counts only compatible FIFO predecessors at that landing; it excludes the Resident and later callers. Initial/newcaller mode choice retains the existing origin/destination API. Committed stops, seats, direction and current phase remain part of both estimates.

The reviewed F4→F0 fixture puts the Resident first among 24 passengers with 14 live wait ticks. The newcomer API still predicts 33 minutes; the held head predicts two minutes, stays with elevator service, boards during tick 15 and arrives at tick 16 with exactly 15 minutes of queue credit. The route remains an elevator route. An intermediate test assertion requiring it to remain physically queued throughout tick 15 was too strict, because successful boarding during the minute is valid; the final regression accepts queued or riding service, verifies the elevator route and checks actual arrival at tick 16.

### Temporal checkpoint contract

- Bank/car versions, phases and direction enums, positive timing, nonnegative counters/progress, floor/target bounds and passenger capacity are validated before installation. New authored minute timings must divide by 60 exactly; 179-second travel and 119-second door/loading fields reject instead of truncating.
- Timer second/minute projections must agree for physical saves and new authored phase saves. Moving requires a valid movement direction, positive remainder and a next floor within the serviced span. Idle timers are canonical zero. New phase metadata records the *original* phase duration, so its remainder must fit that duration while legitimately exceeding a newly upgraded tier's shorter timing. Previous version-1 phase records lacking provenance retain compatibility and establish a non-shrinking phase-duration fallback on load.
- Movement saves now use version 2 and retain an execution checkpoint tick for direct aggregate-free replay. Restoration supports versions 0/1/2; the optional actual world clock is authoritative in production. New checkpoints cannot be after that clock or before departure; reconsideration must fall between departure and the recorded execution checkpoint. Earlier version-1 saves use the supplied authoritative clock when available; without one they cannot establish arbitrary historical chronology, but reject invalid sentinels such as `long.MaxValue`. Version-0 saves keep their legacy no-reconsideration path.
- Trip state/purpose/location enums, leg/mode flags, route duration metadata, exact minute projections, remaining≤duration and legacy whole-minute progress are checked. Legacy progress uses its route preference's authored minute duration, preserving earlier temporal semantics instead of a newly generated physical edge duration.
- Strict validation exposed an existing permitted custom-timing boundary: zero legacy opening/loading/closing times could leave an idle timer at -1. A focused failing compatibility test preceded the fix; legacy transitions now canonicalize idle timer to zero. The configured zero-time journey still completes and replays.

Positive cases retain custom minute timing, old save migration, exact partial stairs/cars, cadence replay, earlier physical movement version 1 with an authoritative clock, and upgrade progress of 120 seconds remaining from an original 180-second phase after applying 6 seconds/floor.

### Retained RED / GREEN

Artifacts are copied to `task-2-evidence/` and retain `/tmp/` originals:

- `task2-fix1-red.xml/log`: **113 total / 95 passed / 18 failed** before fixes: FIFO regression plus 17 corrupt temporal cases.
- `task2-fix1-green.xml/log`: **113 total / 112 passed / 1 failed** after temporal fixes; the remaining assertion incorrectly required queue membership after legitimate boarding. The final test checks retained elevator service and actual tick-16 arrival instead.
- `task2-fix1-covering.xml/log`: **121/121 passed**, including upgrade phase provenance and chronology/old-version positive tests.
- `task2-fix1-zero-boundary-red.xml/log`: **122 total / 121 passed / 1 failed**, the valid zero-duration authored phase produced an idle -1 timer. Canonicalization fixed this compatibility edge.
- `task2-fix1-scoped.xml/log`: **182/182 passed**, adding existing ElevatorBank, tier persistence, graph, command validation and save-envelope tests to the checkpoint runner's classes.
- `task2-fix1-chronology-red.xml/log`: **123 total / 122 passed / 1 failed**, reconsideration at tick 2 was incorrectly accepted when execution checkpoint was tick 1 and world clock tick 2. The final chronology bound uses the execution checkpoint and keeps the authoritative world ceiling.
- `task2-fix1-frozen.xml/log`: **183 total / 183 passed / 0 failed / 0 skipped**, final covering verification against released sources. `git diff --check` passed; changed transit remains engine-free.

Exact final command:

```sh
DOTNET_CLI_HOME=/tmp/task2-dotnet-home /home/geisha/.local/bin/dotnet run --no-restore --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-fix1-scoped/task2-fix1-scoped.csproj -- /tmp/task2-fix1-frozen.xml
```

The scoped project definition is retained as `task-2-evidence/task2-fix1-scoped.csproj`; it includes the existing checkpoint classes and new transit classes plus `ElevatorBankTests`, `ElevatorTierPersistenceTests`, `HierarchicalTransitGraphTests`, `TowerSimulationCanExecuteTests` and `SaveEnvelopeTests`. All owned source/test hashes, including the single aggregate hook, are in `task-2-evidence/fix1-source-hashes.json`.

Fix-round sources, DTOs and tests are frozen and released for independent review. ETA remains a bounded estimate for future committed demand; the identified FIFO-rank defect is covered directly. Native compilation/scoped EditMode and complete C0/performance/presentation acceptance still belong to the controller; this fix round makes no fresh native or full-Domain claim.


## Review fix round 2 — compatibility sources frozen and released

Addressed only the two Important supported-save regressions in `task-2-fix1-review.md`. Production changes are limited to `ElevatorBank.cs` and `TransitExecutionSystem.cs`; regression additions are in `TransitChoiceAndClockTests.cs`. The aggregate clock hook and all other fix-round-1 source remain unchanged. No commits, subagents, Unity runs or all-Domain sweep.

The controller's compatibility probe used the preceding frozen assembly (`/tmp/task2-all-domain/bin/Debug/net10.0/task2-all-domain.dll`) to emit and successfully reload unmodified `bank-v1.json` and `trip-v1.json`. Tests reconstruct those exact field values, including absent provenance/checkpoint fields defaulting to zero. The emitted JSON, old producer and reader source/projects, and SHA-256 provenance are retained in `task-2-evidence/fix2-old-writer/`; original files remain in `/tmp/task2-fix1-compat-probe/`.

- **Old idle boundary:** normalize minute timer -1 to zero only for bank/car timing version 1 with absent phase provenance, idle/no direction, no targets/riders, zero second timer and phase duration, a valid positive whole-minute travel configuration and zero closing-door duration. Loading duration must still be a valid nonnegative whole-minute value. This covers the old zero-door writer's terminal decrement. Validation and both restoration passes use the normalized value without changing the input DTO. New provenance, other negative values, physical clocks, nonzero closing duration and moving state retain strict rejection. The migrated `(1,0,0)` custom configuration keeps minute timing and replays a subsequent return journey through a fresh save.
- **Old legacy duration:** only movement version 1 with legacy minute progress on a physical edge recognizes the former metadata convention `max(60, edge.DurationSeconds)`. Restoration canonicalizes the duration to `max(60, Cost*60)` while retaining the exact whole-minute remainder and legacy mode. The emitted elevator fixture's duration becomes 600 seconds, while its one tick/60-second remainder remains unchanged. It re-saves as version 2, reloads under strict validation and completes the same held elevator journey. Version 2, physical progress, unrelated durations, inconsistent tick projections, an overrun and future reconsideration still reject. Existing FIFO, chronology, original phase-duration/upgrade and temporal corruption regressions remain in the covering suite.

Retained evidence:

- `task2-fix2-red.xml/log`: **196 total / 194 passed / 2 failed / 0 skipped**, before production edits. The only failures are `PreviousWriterIdleBoundaryMigratesWithoutChangingFutureCustomTiming` (`Invalid elevator car temporal checkpoint`) and `PreviousWriterLegacyDurationMigratesAndReplaysTheSameHeldElevatorTrip` (`Inconsistent active transit temporal progress`). All eleven new near-miss rejection controls passed already.
- `task2-fix2-green.xml/log`: **196 total / 196 passed / 0 failed / 0 skipped**, exit 0 against final sources (40.832 seconds). `git diff --check` passed; neither changed production file references UnityEngine/UnityEditor.

Exact covering command:

```sh
DOTNET_CLI_HOME=/tmp/task2-dotnet-home /home/geisha/.local/bin/dotnet run --no-restore --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-fix1-scoped/task2-fix1-scoped.csproj -- /tmp/task2-fix2-green.xml > /tmp/task2-fix2-green.log 2>&1
```

The project definition is retained as `task-2-evidence/task2-fix2-scoped.csproj`; covering classes are unchanged from round 1. All owned source/test SHA-256 values are in `task-2-evidence/fix2-source-hashes.json`, superseding the preceding freezes. Sources, DTOs and tests are frozen and released for scoped independent review. No further production changes are pending. Native validation and complete C0/performance/presentation acceptance remain controller gates; this round claims only focused portable coverage.
