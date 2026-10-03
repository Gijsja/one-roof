### Finding Verdicts

- **[P2] Reconsideration uses newcomer ETA rather than the held FIFO position** — **ADDRESSED**. `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:655` counts compatible predecessors until the requested Resident, excludes that Resident and later callers, and uses the resulting count at `:672`. `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:598` supplies the held ID only for reconsideration. `Assets/OneRoof/Tests/EditMode/Domain/DirectionalDispatchTests.cs:101` reproduces the original 24-person fixture, distinguishes newcomer ETA 33 from held-head ETA 2, retains elevator service, and asserts actual tick-16 arrival with 15 wait ticks.
- **[P1] Temporal saves accept impossible progress/undefined phases and truncate authored timing** — **ADDRESSED for the original corrupt-payload defects**. `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:795` rejects negative timers and validates enums; `:801` requires exact minute divisibility; `:812` checks original phase provenance while permitting a remainder longer than the upgraded tier's timing. `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:304` validates chronology and `:327` validates duration, remaining seconds, minute projection and mode. Rejection regressions are at `Assets/OneRoof/Tests/EditMode/Domain/TransitChoiceAndClockTests.cs:317` and `:346`; the legitimate 120-second upgraded remainder from an original 180-second phase replays at `:363`. The production world-clock hook is present at `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs:1518`. However, the stricter checks introduce the two supported-save regressions below.

### New Breakage in the Fix Diff

1. **Important [P2] Previously generated zero-door/loading custom saves no longer load.** `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:795` rejects every negative minute timer before migration. The prior frozen implementation legitimately produces `Idle`, `timerTicksRemaining=-1`, `timerSecondsRemaining=0`, `timingDataVersion=1`, and no phase provenance after completing a journey with `new ElevatorTimingConfig(1, 0, 0)`. Its own loader accepts that unmodified save. The fix's new transition canonicalization prevents future instances, but does not migrate existing ones; the new loader throws `Invalid elevator car temporal checkpoint`. This breaks preserved custom-timing/save compatibility. Normalize this precisely identified old idle boundary to zero before strict validation, gated by the old version/provenance and the valid zero-duration phase configuration; continue rejecting arbitrary negative timers in new checkpoints. The positive test at `Assets/OneRoof/Tests/EditMode/Domain/TransitChoiceAndClockTests.cs:437` creates its save after canonicalization and therefore does not cover old-payload migration.

2. **Important [P2] Version-1 legacy progress is validated against version-2 duration semantics.** `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:326–327` requires every legacy-progress checkpoint's stored duration to equal `max(60, Cost*60)`, including movement version 1. Before the fix, `RestoreTripExecution` generated legacy progress using `max(60, edge.DurationSeconds)`, and version-1 `ToSaveData` persisted that value. A genuine prior snapshot produced through those APIs contains `movementDataVersion=1`, `legacyLegProgress=true`, `legDurationSeconds=60`, `legRemainingSeconds=60`, `legRemainingTicks=1`, and a physical elevator edge with duration 10 seconds and cost 10. The old loader reloads it; the fix expects 600 seconds and throws `Inconsistent active transit temporal progress`. Version-1 physical compatibility is claimed and supported explicitly, so legitimate prior legacy-progress metadata must be migrated rather than rejected. Recognize the former version-1 duration convention and normalize its duration while retaining authored minute progress and mode; leave strict version-2 consistency checks intact. The existing positive version-1 test at `Assets/OneRoof/Tests/EditMode/Domain/TransitChoiceAndClockTests.cs:404` downgrades a new physical stair save and does not cover a prior legacy-progress payload.

### Out-of-Scope Observations

- None. Complete C0 acceptance and native performance remain Task 5 gates, as before.

### Checks and Evidence

- Read the seven-file fix diff once in bounded sections and compared it only with the two original findings and the unchanged task brief. No broad review, git operation, source edit, subagent, or repeated suite run occurred.
- Retained final covering evidence `.superpowers/sdd/2026-10-03-transit-daily-life/task-2-evidence/task2-fix1-frozen.log:1` reports `Total=183 Passed=183 Failed=0 Skipped=0`. The report names the FIFO/corrupt-payload RED cases and covering test classes. Controller-supplied exact frozen native evidence `Docs/Validation/TRANSIT_DAILY_LIFE_2026-10-03/task2-fix1-editmode.xml:2` reports 72 passed, zero failed/skipped. These runs answer the original findings; they do not test the two old-payload migration cases.
- Named uncovered doubt: would the fix accept valid save payloads actually emitted and accepted by the preceding frozen implementation? Ran only a focused old-writer/new-reader compatibility probe. The old project references `/tmp/task2-all-domain/bin/Debug/net10.0/task2-all-domain.dll`; the new project references `/tmp/task2-fix1-scoped/bin/Debug/net10.0/task2-fix1-scoped.dll`. Both exit 0. Source/project files are retained under `/tmp/task2-fix1-compat-probe/old/` and `/tmp/task2-fix1-compat-probe/current/`. The old writer saves full, unmodified JSON payloads to `/tmp/task2-fix1-compat-probe/bank-v1.json` and `/tmp/task2-fix1-compat-probe/trip-v1.json`, and successfully restores each with the old loader before the new reader tries it.

```sh
DOTNET_CLI_HOME=/tmp/task2-dotnet-home /home/geisha/.local/bin/dotnet run --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-fix1-compat-probe/old/Old.csproj
DOTNET_CLI_HOME=/tmp/task2-dotnet-home /home/geisha/.local/bin/dotnet run --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-fix1-compat-probe/current/Current.csproj
```

```text
Old generated custom save: phase=0, minuteTimer=-1, secondsTimer=0, timingVersion=1
Old loader accepts custom save: Idle
Old generated legacy trip: version=1, legacy=True, duration=60, remaining=60, cost=10, physicalDuration=10
Old loader accepts legacy trip: active=1
New loader REJECTS old custom save: Invalid elevator car temporal checkpoint. (Parameter 'data')
New loader REJECTS old legacy trip: Inconsistent active transit temporal progress. (Parameter 'save')
```

### Verdict

**Fix round:** Findings remain open — both original defects are addressed, but the fix introduces two Important supported-save regressions. Resolve the old idle-timer migration and version-1 legacy-duration migration before approving Task 2.
