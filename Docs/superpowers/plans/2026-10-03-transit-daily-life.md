# Transit and Daily Life Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox syntax for tracking. Do not implement before the human reviews this plan.

**Goal:** Make ordinary Resident life varied and vertical travel believable, without elevator waiting dominating the day; demonstrate the unchanged complete-demand C0 gates with trustworthy current-source evidence.

**Architecture:** Extend the existing pure-C# schedules, purposes, trip generator, transit execution and elevator bank. Keep authoritative demand before planning, distinguish required obligations from optional activity windows, and save every temporal decision needed for exact continuation. Keep presentation on immutable projections and fixed simulation ticks.

**Tech Stack:** Unity6000.3.24f1, C#, existing NUnit EditMode/PlayMode suites and standalone .NET10 Domain probes; no new packages.

**Spec:** `Docs/Design/TRANSIT_AND_DAILY_LIFE_REVIEW_2026-10-02.md`, approved for execution by the request “Execute on this with agents.” This plan fills implementation details and accounts for intervening commit `357e1d9`; it does not assume that commit completed the spec.

## Global Constraints

- Retain >=99% observed required routine demand completion within360 submission ticks, terminal nearest-rank p95 queue wait<=60 including zero waits and pre-stairs waits, live queue maximum<=240, and zero unresolved required unreachable demand.
- No suppression, synthetic local successes, teleporting, arbitrary seed exemptions, deadline relaxation, free meal replenishment or queue-driven staggering.
- Preserve three-car bank cap, authored20-seat reference cars, actual validated command interventions and paired metas.
- Domain/Application remain engine-free; no per-Resident Update; saves live in domain aggregates and contain no scene references.
- Preserve existing authored schedules and supported saves; append enum values; validate corrupt temporal payloads. New production routines must apply to leasing as well as reference fixtures.
- Retain exactly one active handoff. Preserve unrelated working edits to `CrisisDesignProbe.cs`, `Tools/run_editmode_tests.sh` and generated tool output. User subsequently requested a commit after completion: commit the reviewed, verified task changes after final documentation; no push/PR requested.
- Controller owns shared `TowerSimulation`, shared persistence DTO integration, documentation and Unity execution. Workers request ownership before editing those files. No concurrent Unity runners on one project.

## Review Focus

- A scheduled Work/Home obligation crossed during travel must remain observed once and cannot acquire fake local success.
- An unreachable nearest amenity must not prevent use of a reachable alternative; unavailable providers cannot restore needs.
- Opposite-direction calls and endpoint heights other than29 must receive fair service without capacity theft.
- Overnight work/sleep and choices spanning midnight must reproduce after save/load with stable demand episode identity.
- A good aggregate percentile must not hide all-day stair travel, poor elevator-user tails or excessive daily time waiting.

## Current-State Preflight

HEAD `357e1d9` supplies live queue timing, deadline guards, directional boarding and nearest-floor amenities. Retain useful changes, but source audit confirms:

- `ScheduleTripGenerator:137–150` skips changed obligations while Commuting; `170–177` credits null arbitration without destination equality.
- `TransitExecutionSystem:506` still admits ordinary long stairs after40 wait ticks if cost fits360; timing remains15 minutes/stair floor and5→3 minutes/elevator floor.
- `ElevatorBank:403` hardcodes29/25–28; capacity counts mix directions.
- `DailySchedule` still begins every Sleep block at midnight. Only the reference fixture gets extra wake staggering; needs rates and commute physiology are unchanged.
- Existing1265-pass XML predates this commit. New documented metrics have no checked-in matching source manifest/XML/JSON. Reproduce rather than inherit closure.

## Task 1: Required-intent continuity and honest outcomes

**Files:** `Population/DynamicScheduleArbitrator.cs`, `Trips/ScheduleTripGenerator.cs`, existing `Trips/TransitDemandObserver.cs` only if needed; new Domain tests `RoutineDemandContinuityTests.cs` plus meta. Paths are under `Assets/OneRoof/Runtime/Domain` and `Assets/OneRoof/Tests/EditMode/Domain` respectively.

**Interfaces:** Add `RoutineDecisionKind { None, LocalActivity, Travel }` and immutable `RoutineDecision` carrying nullable `TripPurpose`, required/local semantics and stable episode-start tick. Add `DynamicScheduleArbitrator.Decide(PersonRecord person, Tick tick, bool isBlockTransition)`; retain `ArbitrateDestination` as a compatibility wrapper. Generator consumes the explicit decision and existing observer methods. Distinguish an optional satisfied window from an actual required local attendance outcome.

- [x] Write `WorkBoundaryDuringCommute_IsObservedOnce`, `HomeBoundaryDuringCommute_IsObservedOnce`, `NullDecisionAwayFromDestination_IsNotLocalSuccess`, `SatisfiedOptionalWindow_DoesNotCreateDemand`, and `ExistingSamePurposeTrip_RetainsOriginalEpisode`. Assert required attempt counts, exact terminal outcomes, outstanding identity and no duplicate retry entries.
- [x] Run new cases against current behavior; retain failing results. Add them to the portable test runner's existing project inclusion and run it with artifacts in `/tmp`.
- [x] Implement explicit decision handling; observe required transitions before commuting/commit/cooldown skips. Only actual local attendance can complete locally; optional no-action does not enter the required denominator. Required missed/suppressed demand remains pending or terminally failed under the existing deadline policy.
- [x] Test blocked Outside meal, missing destination, departed Resident, exact360 deadline and save/load during crossed obligations. Run all portable checkpoint regressions.
- [x] Independent spec/correctness review before Task2; record changed paths, tests and remaining findings in this plan's execution ledger.

## Task 2: Coherent movement, credible stairs and general dispatch

**Files:** `Transit/ElevatorTimingConfig.cs`, `ElevatorTierProfile.cs`, `ElevatorCar.cs`, `ElevatorBank.cs`, `HierarchicalTransitGraph.cs`, `TransitExecutionSystem.cs`, relevant transit save DTOs through controller; new `TransitChoiceAndClockTests.cs` and `DirectionalDispatchTests.cs` plus metas.

**Interfaces:** Existing outer `ElevatorBank.Tick` and simulation tick remain one in-game minute. Add fixed integer movement timing/progress internally, using bounded5-second substeps (12 per outer tick); wait counters increment once per outer tick. Preserve explicit legacy/custom timings through documented conversion. Add `ElevatorBank.EstimateArrivalTicks(int originFloor, int destinationFloor)` using committed stops, seats and direction; execution uses it for alternative comparison. Store substep remainder and reconsideration tick through aggregate saves.

- [x] Write failing tests for F29→F0 with queues0/3/20 keeping a usable elevator,1–2 floor choice,3–4 floor downward advantage, long no-car escape, failed diversion FIFO/wait invariance and exact replay of partial movement.
- [x] Implement deliberate baseline physical timing:10 seconds/floor at tier1–2,8 at tier3–4,6 at tier5;5 seconds each opening/closing,10 seconds loading; horizontal movement1 cell/second; stairs30 seconds/down floor and45 seconds/up floor. These are chosen gameplay calibration inputs, not claims about real-world elevators. Route preference and actual duration must be separate, with correct rounding only at the outer tick boundary.
- [x] Ordinary stair alternatives are limited to2 floors upward or4 downward; compare full arrival ETA plus directional effort. Reconsider after15 live queue ticks at5-tick cadence, requiring at least3 ticks of generalized-cost improvement and remaining360 feasibility. Long stairs are an escape only when usable elevator service is unavailable. Preserve queue position until route acceptance and passenger removal both succeed.
- [x] Replace hardcoded endpoint reservations with configured serviced endpoints and actual directional counts. Keep compatible FIFO within each direction, stable pickup commitments, seat estimates and bounded oldest-call service. Avoid reserving an idle car when only one landing has demand.
- [x] Add5/30/40-floor and negative-floor bank tests, mixed-direction queues, full cars, one-landing peak, direction reversal and no-starvation cases. Compare legal tier1→5 commands against the same demand.
- [x] Run targeted regressions and causal save replay; independent review. Do not tune by changing the C0 denominator or fixture obligations.

## Task 3: Production daily routines and appointment continuity

**Files:** `Population/DailySchedule.cs`, `ScheduleBlock.cs`, `PersonRecord.cs`, `ResidentPurposeSystem.cs`, `DynamicScheduleArbitrator.cs`, `Trips/ScheduleTripGenerator.cs`, `Economy/LeasingDemandSystem.cs`, `Topology/GoldStandardCityFixture.cs`, `DiversifiedTowerFixture.cs`; population save integration through controller; new `EmploymentRoutineTests.cs` plus meta.

**Interfaces:** Keep `DailySchedule.Standard` for explicit legacy/test use. Add employment-aware schedule creation used by both leasing and fixtures; cohort derives from actual workplace content and stable Resident identity (Outside employment receives stable early/day/late alternatives). Persist cohort, appointment times and deterministic daily choices in the population aggregate. Add appointment-start/end and anticipated commute departure semantics to routine decisions, preserving original demand submission/deadline.

- [x] Write cohort tests showing early/day/late/overnight work where jobs support those shifts, varied bedtime and no universal midnight Home boundary. Assert coverage of1440 ticks, non-overlap, overnight episode identity and eight-hour employment obligations.
- [x] Introduce preparation, real meal opportunities, work/lunch, home time, optional outing windows and shift-relative sleep. Use stable seeded individual offsets and bounded daily optional-window choices; never use queue length to stagger obligations. Preserve existing authored schedules on load rather than regenerating them.
- [x] Depart for appointments using route estimates before work starts; record lateness against scheduled start. Arrival cannot reset an eight-hour work obligation or erase a missed window. Overnight blocks split at midnight while retaining one logical episode.
- [x] Exercise production leasing and the reference fixture through the same routine factory. Derive initial needs from routine phase rather than giving all Residents identical fresh reserves or randomizing reserves to make gates green.
- [x] Add save/reload tests before/after midnight, departure, late arrival, overnight sleep/work, day choice and urgent interruption. Compare exact future demand, activities and decisions.
- [x] Run relevant population/economy tests; independent review before shared integration.

## Task 4: Reachable amenities and daily physiological budgets

**Files:** `Trips/ScheduleTripGenerator.cs`, `Population/ResidentNeedsSystem.cs`, `Transit/TransitExecutionSystem.cs`, `TowerSimulation.cs` through controller; existing economy/provider APIs; new `ResidentDailyBudgetTests.cs` and `AmenityDestinationTests.cs` plus metas.

**Interfaces:** Candidate resolution consumes authoritative current location, topology and actual existing provider availability. Expose immutable movement phase/directional stair effort from transit for needs evaluation; no presentation dependency. Keep service demand ownership compatible with future foundation service work.

- [ ] Write unreachable-nearest/reachable-alternative tests, F0 plus upper diner demand distribution, unavailable/closed provider controls and stable tie-breaking. Genuine route/provider failure must stay observed.
- [ ] Rank feasible candidates by actual route duration, availability/capacity and stable preferences, rather than floor distance alone. Use existing real provider/economy facts; do not add free home meals or duplicate a service owner. Preserve Outside meal payment/retry behavior.
- [ ] Separate ordinary activity, queue, elevator ride, hallway walking, upward stairs and downward stairs fatigue. Calibrate the complete serviced workday so eight-hour work with real breakfast/lunch and adequate sleep does not obligatorily hit energy<.25, while absent meals and prolonged stair travel still produce deprivation. Record chosen rates and before/after daily budget evidence.
- [ ] Test meal payment/service attendance, full workday, absent-food control, prolonged-queue control and directional stairs. All same-source save continuation checks must pass.
- [ ] Independent review for resource accounting, service duplication and hot-path cost before acceptance runs.

## Task 5: Integrated evidence and completion audit

**Files:** Extend `Tools/DesignValidation/TransitDiagnosticProbe.cs` and probe project/runner; new durable directory `Docs/Validation/TRANSIT_DAILY_LIFE_2026-10-03/`; coordinator updates design implementation notes, ADR status, BACKLOG and the sole active handoff.

- [ ] Capture finalized cohorts at emission by explicit day number; do not call the last available cohort “Day5.” Include source SHA256 manifest and exact seed/tier/configuration/commands.
- [ ] Report attempts, actual travel arrivals, legitimate local outcomes, suppressed/expired/unreachable outcomes, mode/purpose/floor breakdown, terminal histogram, preboarding live maximum and unfinished demand. Include elevator-user tails, longest stair journey and Resident-time waiting/riding/walking/stairs/work/sleep/meals/leisure.
- [ ] Run300 Residents for multiple fully finalized days with at least3 deterministic seeds; retain authored reference topology and compare tiers plus effective/ineffective legal capacity/layout interventions. All selected healthy operating configurations must pass unchanged C0 gates; unsuitable/stressed controls must still visibly fail and recover only after effective systems changes.
- [ ] Verify at least1500 future ticks per save through active calls, stairs, midnight and settlement, comparing causal state and outcomes. Run fresh full Unity EditMode and affected graphical PlayMode suites against exact reviewed sources. Preserve failing evidence and investigate root cause.
- [ ] Inspect actual ordinary-day and stressed Tower presentation for believable queue/stair behavior. Measure native tick/frame cost against project budgets with matched hardware/resolution; report exact evidence separately from standalone diagnostic timing.
- [ ] Fresh whole-change agent review against the original spec, current plan, evidence and unresolved review ledger. Fix material gaps with scoped re-review. No package closes from historical XML or narrative metrics.
- [ ] Update documentation with proven behavior, chosen values and remaining limitations. Mark the goal complete only after all original behavior and validation requirements pass; retain open status where evidence is missing.

## Execution Ledger

- 2026-10-03: three read-only agent audits of357e1d9 completed; current code fails full review coverage despite C0 DONE narrative. Baseline portable checkpoint runner:39 passed/0 failed. Fresh finalized day5: tier1 2457/2505,p9558,max557; tier5 2631/2647,p9589,max448. Both FAIL selected C0 gates;1500-tick causal replay matches. Durable evidence in `Docs/Validation/TRANSIT_DAILY_LIFE_2026-10-03.md`.
- Plan approved2026-10-03. Execution method: agents. Approved workspace: current checkout, with explicit file ownership; isolated `/tmp` validation snapshot if the primary Editor is live. No worktree was created. The later explicit docs/commit request authorizes a local checkpoint commit after verification.

- 2026-10-03 cutoff requested: finish current Task3 agent/review, document and commit Tasks1–3 checkpoint. Tasks4/5 remain pending; C0 is not closed.
