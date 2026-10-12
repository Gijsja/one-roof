# Task 1 implementation report

2026-10-03. Implementation complete in the approved shared checkout; independent spec/correctness review remains with the controller. No commits, Unity runners, scene changes or save-schema changes.

## Requirements and resulting behavior

- Added immutable `RoutineDecision` and `RoutineDecisionKind { None, LocalActivity, Travel }`. The decision carries nullable `Purpose`, `IsRequired`, `IsLocal` and `EpisodeStartTick`. `DynamicScheduleArbitrator.Decide(person, tick, isBlockTransition)` supplies the explicit result. The nullable `ArbitrateDestination` compatibility wrapper retains its previous null behavior during commuting and for local activity.
- Scheduled Work/Home appointments enter the observer before commuting, external commitment eligibility, Outside shift commitment and failed-route cooldown skips. An existing unresolved same-purpose trip retains its original sequence, submission tick and episode start. Repeated boundary calls do not manufacture demand or retries.
- Crossing an appointment while on a different trip leaves the appointment pending. Actual local attendance can subsequently complete this pending sequence mid-block, including at the inclusive360 deadline. A departed Resident's obligation remains pending until expiration; absence from population does not remove the denominator.
- Suppressed required commitments retain the existing terminal failure policy unless an accepted same-purpose trip already serves the obligation. Missing destinations/routes remain unreachable failures. Required cooldown-skipped attempts stay pending for the existing deadline policy.
- `None` from a satisfied optional Eat/Leisure window or a nullable ongoing-activity decision creates no attempted demand and no local success. Only physically matching required attendance completes locally. No fallback from null to scheduled Food/Leisure is used.
- Blocked Outside Food demand is observed/rejected once under the selected decision's stable episode start. It cannot fall through to local completion.
- Urgent travel decisions remain observed demand. Per controller clarification, low needs do not create fresh urgent episodes every commuting tick; need state survives the commute and the first eligible arbitration selecting Food/Hygiene/Home observes that intent. The critical-hunger test verifies this policy.
- Existing observer capacity, terminal outcomes, attempt denominator integrity, inclusive360 deadlines and checkpoint formats remain unchanged. Decisions are derived from the saved schedule/purpose; the generator's existing retry records and observer's saved episodes retain exact continuation. Task3 will extend episode identity for overnight appointments.

## Changed paths

- `Assets/OneRoof/Runtime/Domain/Population/DynamicScheduleArbitrator.cs`
- `Assets/OneRoof/Runtime/Domain/Population/RoutineDecision.cs` and paired `.meta`
- `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs`
- `Assets/OneRoof/Runtime/Domain/Trips/TransitDemandObserver.cs` — added read-only `FindOutstandingSequence` so actual local attendance can finalize crossed unsubmitted obligations without creating mid-block attempts.
- `Assets/OneRoof/Tests/EditMode/Domain/RoutineDemandContinuityTests.cs` and paired `.meta`
- `Tools/DesignValidation/CrisisCheckpointTests.csproj` — added the new test file to existing portable inclusion. This runner project was already untracked at dispatch; only its inclusion was edited.
- This implementation report. Controller-owned shared docs, ledger, TowerSimulation and persistence files were not edited.

## RED evidence

Initial command:

```bash
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj --artifacts-path /tmp/task1-red-build -- /tmp/task1-red.xml
```

Exit1:45 total,40 passed,5 failed. Expected behavioral failures:

- `WorkBoundaryDuringCommute_IsObservedOnce`: attempted0 instead of1.
- `HomeBoundaryDuringCommute_IsObservedOnce`: attempted0 instead of1.
- `NullDecisionAwayFromDestination_IsNotLocalSuccess`: succeeded1 instead of0.
- `SatisfiedOptionalWindow_DoesNotCreateDemand("Eat")`: attempted1 instead of0.
- `SatisfiedOptionalWindow_DoesNotCreateDemand("Leisure")`: attempted1 instead of0.

The initial test-only build had a corrected test method-name typo (`SetNeedSatisfaction` → existing `UpdateNeed`) before the executable RED run. The retained XML above is a runtime assertion failure run, not a compiler error.

Additional narrow runtime RED runs used `/tmp/task1-focused.csproj`, referencing the same Domain sources, new test file, existing NUnit DLL and portable runner:

```bash
/home/geisha/.local/bin/dotnet run --project /tmp/task1-focused.csproj --artifacts-path /tmp/task1-focused-build -- /tmp/task1-red-additional.xml
/home/geisha/.local/bin/dotnet run --project /tmp/task1-focused.csproj --artifacts-path /tmp/task1-focused-build -- /tmp/task1-red-deadline.xml
/home/geisha/.local/bin/dotnet run --project /tmp/task1-focused.csproj --artifacts-path /tmp/task1-focused-build -- /tmp/task1-red-wrapper.xml
```

- Additional:10 total,9 passed,1 failed; blocked Outside meal used episode1 instead of scheduled10.
- Deadline:10 total,8 passed,2 failed; the meal identity failure plus real post-load attendance failed to complete crossed Work at tick370.
- Wrapper:10 total,7 passed,3 failed; those two failures plus nullable wrapper incorrectly returned Work during commuting. All fixed before final verification.

## GREEN verification

```bash
/home/geisha/.local/bin/dotnet run --project /tmp/task1-focused.csproj --artifacts-path /tmp/task1-focused-build -- /tmp/task1-focused-final.xml
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj --artifacts-path /tmp/task1-green-build -- /tmp/task1-green.xml
/home/geisha/.local/bin/dotnet run --project /tmp/task1-legacy-routine.csproj --artifacts-path /tmp/task1-legacy-build -- /tmp/task1-legacy.xml
git diff --check
```

All exit0:

- Focused12/12,0 failed/0 skipped. Includes named five required cases, blocked Outside meal, missing destination, departed Resident, save/load with real attendance at inclusive360, urgent need after commute and cooldown denominator continuity.
- Full portable checkpoint51/51,0 failed/0 skipped (existing39 plus new12). Preserves existing demand, transit persistence, population need persistence and elevator pickup continuation checks, including replay regressions.
- Legacy routine compatibility54/54,0 failed/0 skipped. Temporary standalone project includes existing `PersonScheduleTests`, `ResidentPurposeTests`, `DailyCycleTests`, `ResidentNeedsSystemTests` and `OutsideWorkerSimulationTests` against the same full Domain source.
- `git diff --check`: no whitespace errors. No UnityEngine references in the changed Domain sources.

All runtime/build artifacts and temporary focused/legacy projects are in `/tmp`; no generated build artifacts added to the repository.

## Concerns and remaining validation

- Independent task review is pending with controller; Task2 should follow that review.
- Native Unity compilation/EditMode/PlayMode and graphical/performance validation were not run, per dispatch. Portable results do not close C0 acceptance.
- This task retains the existing deadline behavior and derived scheduled episode start. Overnight/split-midnight appointment identity is Task3 scope; that task must update both pre-skip observation and the explicit decision's identity consistently.
- Meal/provider selection and need calibration remain later tasks; this change neither grants free needs nor claims provider availability is certified.
