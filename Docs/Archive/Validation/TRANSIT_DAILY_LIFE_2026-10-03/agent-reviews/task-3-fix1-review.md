# Task 3 fix1 scoped re-review

## Finding verdicts

- **[P1] Validate the complete versioned routine payload — ADDRESSED.** `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs:426` validates the version before fallback; lines433-451 reject unknown cohorts, incomplete/mismatched arrays, gaps, missing full-day coverage and unsupported generated activities. `ScheduleBlock.cs:35` validates explicit logical boundaries while permitting legal negative midnight offsets. `PersonRecord.cs:148` validates coherent sentinel state and chronology, uses decimal duration comparisons to avoid overflow, and checks recorded observations/daily choice against the optional authoritative restored clock. `TowerSimulation.cs:1494` supplies that clock. `EmploymentRoutineTests.cs:390` exercises19 malformed payloads through real simulation restoration, including the original reported corruptions and added bounds/chronology cases. Positive tests at lines444 and463 retain negative midnight starts, early departure, prior-job evidence and direct aggregate restoration without an invented world clock. Version-zero authored restoration remains on its previous path.

- **[P2] Production routines inherit the compressed fixture clock — ADDRESSED.** `Assets/OneRoof/Runtime/Domain/Population/ResidentPurposeSystem.cs:57` now limits the compression heuristic to legacy schedules. `EmploymentRoutineTests.cs:421` constructs a real Late cohort with a short midnight meal fragment, verifies30-minute Eating and60-minute Leisure commitments, and verifies that compressed legacy schedules still receive five-minute commitments.

## New breakage in the fix diff

None found. `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs:137` additionally prevents a later urgent-return departure from replacing the initial local-attendance evidence; the focused recording regression at `EmploymentRoutineTests.cs:474` verifies this directly. Initial physical commute departure still records while arrival remains unset, and each new Work segment retains its existing independent reset. The fix does not alter appointment times, observer deadlines, demand outcomes, needs or transit capacity.

## Out-of-scope observations

None. Tasks4/5 physiological calibration, complete-demand certification and performance acceptance remain future work; this review makes no C0/performance closure claim.

## Checks

- Read the exact six-file task3-fix0 -> frozen fix1 diff once, the appended fix report, original findings and the unchanged brief. No broader review, outside-source crawl, subagents, source edits, Git mutations or suite reruns.
- Parsed retained `task-3-evidence/fix1-red.xml`:180 total/162 passed/18 failed/0 skipped; chronology RED185/183/2/0; bounds RED188/183/5/0. These corroborate the reported failure progression.
- Parsed retained `task-3-evidence/fix1-final.xml`:**188/188 passed,0 failed/0 skipped**. All35 relevant cases passed:19 corruption cases,2 production/legacy clock cases,3 valid metadata cases, direct aggregate compatibility, local attendance, eight700-tick checkpoints and the3300-tick reference restore case. Paired logs contain clean count summaries.
- Native full-project validation is running under controller ownership against the frozen snapshot; its pending result is not inferred from portable tests.

## Verdict

**Fix round: All findings addressed, no new Critical/Important breakage.** Task3 spec **PASS** and quality **APPROVED** within its task scope; no open review findings.
