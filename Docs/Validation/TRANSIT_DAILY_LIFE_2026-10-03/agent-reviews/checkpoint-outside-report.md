# GoldStandard cutoff snapshot correction

Status: implemented, focused portable GREEN, sources frozen for review/native validation. Owned source changes are PopulationState.cs and EmploymentRoutineTests.cs only. Diff: checkpoint-outside-restore.diff against the exact pre-fix native snapshot; hashes: task-3-evidence/cutoff-outside-frozen-sha256.txt.

Root cause: live Outside locations intentionally retain CurrentRoomId as the last indoor room. Population loading restored the Outside endpoint directly from a newly created PersonRecord, leaving its constructor HomeRoomId in that cached field. This changed a saved last room21 to home37 even though CurrentLocation remained Outside. The original native GoldStandardCityTests.CitySeed_IsDeterministic_AndSnapshotRestoresThePopulatedCity failed its full JsonUtility snapshot equality at line59 after90 ticks (provided /tmp/transit-routines-checkpoint-editmode.xml: expected806476 vs actual806502 characters, first difference currentRoomId21/37).

Fix: restore the persisted cached indoor room first, then the Outside endpoint, before restoring activity/purpose and routine observations. The existing legacy fallback to home for missing/nonpositive currentRoomId remains. No transit state, schedule, needs, validation assertion or runtime trip behavior is changed.

TDD and covering evidence:

- task-3-evidence/cutoff-outside-red.xml/.log:189 total,188 passed,1 expected failure, OutsideRoutineCheckpoint_PreservesLastIndoorRoomAlongsideWorldLocation (expected room5, restored home13).
- task-3-evidence/cutoff-outside-green.xml/.log:190 total,190 passed,0 failed,0 skipped.
- The second new test reproduces the precise GoldStandard90-tick snapshot sequence: two identical seeds advance90 ticks and compare every public saved field recursively; the restored snapshot is then compared against every original saved field, including all nested arrays/DTOs. This is the portable equivalent of the native full JsonUtility snapshot assertion, without an engine serializer dependency. The existing native integrity assertions are untouched.

Exact commands: /home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj -- .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/cutoff-outside-red.xml > .superpowers/sdd/2026-10-03-transit-daily-life/task-3-evidence/cutoff-outside-red.log 2>&1; same command with cutoff-outside-green paths for GREEN. git diff --check is clean. No Unity run, commit, subagent or unrelated edit performed by this worker. Controller owns final native rerun and cutoff checkpoint commit.
