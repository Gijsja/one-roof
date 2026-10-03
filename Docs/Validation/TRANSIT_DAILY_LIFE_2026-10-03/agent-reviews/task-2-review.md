### Spec Compliance

- ❌ Issues found: remaining-arrival comparison during reconsideration does not respect the Resident's existing FIFO position (`Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:652`, `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:499`); corrupt temporal save payloads are accepted or silently changed (`Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:233`, `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:770`). These miss the coherent full-arrival comparison and explicit corrupt-payload validation requirements.
- ✅ The diff covers all requested production areas and both new test classes, with paired `.meta` files. Physical timings and retained authored minute timing, directional ordinary-stair bounds, live-wait reconsideration cadence, removal-before-route-commit, configured endpoint reservations, directional FIFO, pickup stability, and legal tier command comparison are implemented. The tests exercise these behaviors at `TransitChoiceAndClockTests.cs:29`, `TransitChoiceAndClockTests.cs:80`, `TransitChoiceAndClockTests.cs:98`, `TransitChoiceAndClockTests.cs:195`, `TransitChoiceAndClockTests.cs:215`, `TransitChoiceAndClockTests.cs:296`, `DirectionalDispatchTests.cs:16`, `DirectionalDispatchTests.cs:52`, and `DirectionalDispatchTests.cs:134` (all under `Assets/OneRoof/Tests/EditMode/Domain/`).
- ⚠️ Cannot verify from this task diff: complete C0 demand completion ≥99% within 360 ticks; terminal nearest-rank p95 wait ≤60 including zero/pre-stairs waits; live queue maximum ≤240; zero unresolved required unreachable demand; native tick/presentation performance. The controller should retain these exact Task 5 acceptance gates. Scoped unit tests and the reported portable suite do not establish those distributions or budgets.
- ✅ The change stays within domain transit/persistence code, introduces no engine dependency or per-Resident `Update`, changes no authored schedule/scene assets or enum ordering, and retains the three-car cap and the capacity-preserving tier application (`Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:14`, `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs:68`). Save DTO additions contain values and stable IDs rather than scene references (`Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs:354`, `Assets/OneRoof/Runtime/Domain/Persistence/TransitSaveData.cs:51`).

### Strengths

- Exact partial movement and causal reload are tested against phase, floor, direction, targets, queue size and deliveries, rather than only checking that a save loads (`Assets/OneRoof/Tests/EditMode/Domain/TransitChoiceAndClockTests.cs:195`, `Assets/OneRoof/Tests/EditMode/Domain/TransitChoiceAndClockTests.cs:215`).
- Dispatch tests isolate configured endpoint behavior and immediate use of all cars using matched authored timings; they include 5/30/40-floor and negative-floor ranges (`Assets/OneRoof/Tests/EditMode/Domain/DirectionalDispatchTests.cs:16`, `Assets/OneRoof/Tests/EditMode/Domain/DirectionalDispatchTests.cs:52`).
- Accepted diversion plans before removing the held passenger, then credits wait and commits the alternative only after successful removal (`Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:499`). The tier comparison uses real validated commands and holds peak demand/seats constant (`Assets/OneRoof/Tests/EditMode/Domain/DirectionalDispatchTests.cs:134`).
- The portable evidence is readable and pristine: `task-2-evidence/task2-all-domain-frozen.log:1` reports 684 passed, zero failed/skipped. Controller-supplied native evidence `Docs/Validation/TRANSIT_DAILY_LIFE_2026-10-03/task2-editmode.xml:2` reports the exact frozen scoped classes passed 44/44 with zero failed/skipped. No suite was rerun for this review.

### Issues

#### Critical (Must Fix)

- None.

#### Important (Should Fix)

1. **[P2] Reconsideration estimates a new caller's wait instead of the held FIFO position.** `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:652` initializes `ahead` from the entire directional queue, and `:660` charges full bank-span batch cycles for it. This is a reasonable input for a newcomer joining behind that queue, but `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:499` calls `ChooseStairs` for an already queued Resident, and `:557` calls the same origin/destination-only estimator. It counts the Resident itself and everyone behind it as preceding demand. A focused probe put the Resident first among 24 same-direction passengers at F4, with an empty eight-seat car at F0 in a 0–29 bank and 14 live wait ticks. At tick 15 the Resident abandons the elevator because the shared ETA is 33 minutes. The identical bank without diversion delivers that first Resident at tick 16; stairs deliver at tick 17. Thus the claimed ≥3-minute improvement is artificial, queue position is discarded for a slower route, and new demand behind a Resident can destabilize its choice. Preserve the newcomer API if needed, but add a passenger-aware remaining-arrival estimate for reconsideration using actual compatible FIFO predecessors and known seat commitments. Add a regression distinguishing the first queued Resident from a new caller and asserting the first Resident stays on the faster elevator.

2. **[P1] Temporal checkpoint validation accepts impossible progress and silently truncates authored timing.** `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:233` checks only nonnegative second values; `:285` installs them without validating `remaining <= duration`, consistency with the current leg/mode, or reconsideration chronology. A valid partial stair checkpoint with duration 45 seconds and remaining 33 seconds still loads after replacing its remaining seconds with 6000 and its reconsideration tick with `long.MaxValue`. It can invent extensive travel time and suppress future reconsideration. Separately, `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:770` integer-divides saved legacy seconds by 60 without requiring exact divisibility: a corrupted authored 179-second floor timing loads as 120 seconds. The same restoration path at `:785` accepts undefined phase 999; `ElevatorCar.RestoreState` (`Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs:170`) installs it directly, and the new bounded advance loop (`:396`) leaves it unchanged after advancement. That can permanently strand riders while treating the car as usable. Validate all new temporal fields and their relationships before installation: exact minute conversion, supported versions/enums, legal phase/direction/progress combinations, route-leg remainder consistency, and chronology. Preserve the documented legitimate upgrade remainder that can exceed the new tier's phase duration. Add focused rejection tests alongside the existing valid old/custom/partial replay cases.

#### Minor (Nice to Have)

- None.

### Checks and focused investigation

- The initial combined brief/report/diff output was truncated. The unseen portions were read in bounded sections; no source diff was regenerated, no git command was run, and no broader repository crawl or subagent was used.
- Named risk: the frozen diff cuts off `RestoreState` and legacy phase transitions, which could conceal temporal validation or alter direction at boarding. Read the complete bounded `ElevatorCar.cs:125–343` context; it confirms no phase/direction validation occurs in `RestoreState`.
- Named risk: stable opposite-direction commitments depend on `CanTargetFloorNow`, whose function is cut between diff hunks. Read only `ElevatorBank.cs:433–505` to establish the complete eligibility contract. No additional dispatch defect is asserted from that check.
- Named risk: no existing reported fixture distinguishes an existing FIFO head's ETA from a newcomer ETA, or rejects the inconsistent positive temporal payloads above. Ran one temporary focused console probe against the implementer's retained frozen Domain/test assembly; repository source was untouched. Probe files: `/tmp/task2-review-probe/Probe.csproj` and `/tmp/task2-review-probe/Program.cs`. Command: `DOTNET_CLI_HOME=/tmp/task2-review-dotnet-home /home/geisha/.local/bin/dotnet run --disable-build-servers -p:UseSharedCompilation=false --project /tmp/task2-review-probe/Probe.csproj`. Exit 0; result:

```text
Newcomer ETA minutes=33 queued=24
FIFO head diverted=True active route elevator=False
FIFO head staying elevator delivered tick=16 wait=15
Diverted trip completed tick=17
Valid partial stair duration=45 remaining=33
Corrupt movement payload ACCEPTED
Corrupt custom temporal payload ACCEPTED and changed to 120 seconds
Corrupt car phase ACCEPTED: 999
```

### Assessment

**Task quality:** Needs fixes.

**Reasoning:** The physical clock, directional dispatch, and replay coverage are substantial and the supplied test evidence passes. However, the verified queue-rank error violates the mode-choice improvement requirement, and incomplete temporal validation can change timing or strand required travel, so Task 2 should not pass its gate until both are fixed.
