# Handoff — OR-1004 Outside implementation

## Changed
- Added a typed `WorldLocation` room/Outside endpoint to resident and trip state. The transit graph has one street-edge node connected only to the ground-floor lobby portal. Demand leases start residents Outside, create a routed move-in trip, and use an external workplace when no internal job exists. The daily schedule routes external work and return-home trips across that seam.
- Save DTOs persist location kinds for residents and active trips. Missing fields in older saves default to Room; in-flight routes are rebuilt from typed endpoints on load. Resident projections and the inspector expose Outside. The tower cutaway draws a compact street edge. The earlier frame-based walking interpolation remains in `TowerResidentPresenter`.
- Added route, move-in, external commute/return, and mid-trip save tests. Updated graph-size expectations and the related OR-1003 golden acceptance scope. ADR-072 records the additive save decision.

## Validation
- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-outside-validation` at `b00d715`, with only feature runtime/test files copied in; no connected editor was used. Removed after validation.
- `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof-outside-validation -logFile /tmp/one-roof-outside-compile.log` — exit 0.
- Focused EditMode `TripGenerationTests`: 11/11 passed (`/tmp/one-roof-outside-trip-tests.xml`). Focused `TowerSaveRoundTripTests`: 6/6 passed (`/tmp/one-roof-outside-save-tests.xml`). Commands used `timeout 1500 ... -batchmode -nographics -runTests -testPlatform EditMode -testFilter <suite> -testResults <path> -logFile <path>` without `-quit`.
- Full EditMode: 449/451 passed, exit 2 (`/tmp/one-roof-outside-editmode.xml`). Pristine HEAD run: 445/447 passed, exit 2 (`/tmp/one-roof-outside-baseline-editmode.xml`). Both fail the same `RoomPresenterTests.EnsureRoomViews_AdoptsAuthoredRoomInsteadOfCreatingDuplicate` and `TowerStructurePresenterTests.Initialize_RemovesDuplicateSlabAndPreservesCanonicalGeometry` tests.
- Full PlayMode: 5/5 passed, exit 0 (`/tmp/one-roof-outside-playmode.xml`). Commands used the same Unity executable, `timeout 1500`, `-runTests -testPlatform PlayMode`, result and log paths, without `-quit`.

## Risks and next safe action
- The compact street edge is cutaway presentation only; no full street view or city simulation is added. The external workplace rule applies to newly leased residents when no internal workplace exists. Residents already assigned internal workplaces retain them.
- The two baseline presentation failures remain open and are unrelated to OR-1004. OR-1003 should exercise Outside crossings under 300-resident save/replay load.
- Preserve the existing uncommitted package, settings, `.plastic`, and earlier handoff changes in the main checkout.
