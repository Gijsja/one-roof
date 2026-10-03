# Cutoff integration regression review

## Golden cause-chain fixture

**Spec PASS / Quality APPROVED.** `Assets/OneRoof/Tests/EditMode/Application/GoldenFirstPlayableTests.cs:47` gives the synthetic congestion scenario an explicit two-seat car; line112 uses that same capacity after the intervention. Adding the second car remains the sole service variable between scenarios. The exact diff changes neither production cars/timing nor assertion thresholds.

The retained test checks50 queued Residents and severe congestion at lines57-60, accessible overlay/inspector causes at lines68-84, prediction and confidence at lines92-99, complete delivery and >20-minute congestion at lines103-109, complete intervention delivery at lines114-119, measured >=40% improvement at lines124-127, and schedule/save integrity at lines134-151. Every original assertion is preserved. This synthetic fixture correction does not alter the authored three-car/20-seat reference Tower or certify its later complete-demand gates.

Parsed retained `checkpoint-golden-red.xml`:75 total/74 passed/1 failed/0 skipped; the sole Golden failure reports baseline8.92 against >20. Parsed `checkpoint-golden-green.xml`:**75/75 passed**,0 failed/0 skipped. The report's measured two-seat baseline24.52/intervention12.04 is consistent with the retained passing congestion/improvement assertions; exact measured values are report claims, not XML measurements.

## Outside cached-room snapshot restoration

**Spec PASS / Quality APPROVED.** `Assets/OneRoof/Runtime/Domain/Population/PopulationState.cs:382` restores the saved cached indoor room before restoring the Outside endpoint at line384. The existing nonpositive-room fallback remains. Activity, purpose and routine restoration continue afterward, so endpoint initialization does not erase their saved observations.

Named contract check: `PersonRecord.UpdateLocation(WorldLocation)` at `Assets/OneRoof/Runtime/Domain/Population/PersonRecord.cs:289` retains `CurrentRoomId` for Outside and changes only the record's location/purpose fields. Restoring both saved location components in the constructor/load phase therefore matches the live-record semantics without a runtime transit intervention.

`Assets/OneRoof/Tests/EditMode/Domain/EmploymentRoutineTests.cs:487` directly checks the Outside endpoint, saved cached room and Working activity. The new90-tick reference test at line503 compares two identical seeds and the restored full snapshot; the recursive helper at line515 checks all public saved fields, nested DTOs and arrays. Existing native snapshot assertions are untouched.

Parsed retained `task-3-evidence/cutoff-outside-red.xml`:189 total/188 passed/1 failed/0 skipped; the focused cached-room test expected5 but restored13. Parsed `cutoff-outside-green.xml`:**190/190 passed**,0 failed/0 skipped; both new regression tests pass and paired logs contain clean count summaries.

## Issues and scope

No Critical, Important or Minor findings in either narrow diff. Read each exact diff once and inspected only the Golden function continuation needed to verify preserved assertions and the named location-mutator contract. No suite/Unity reruns, broader Task3 review, subagents, source edits or Git mutations.

**Integration review APPROVED.** Controller full native validation is running against the final frozen snapshot; its result remains pending. Tasks4/5 complete-demand, physiological and performance acceptance remain future work, with no closure claimed here.
