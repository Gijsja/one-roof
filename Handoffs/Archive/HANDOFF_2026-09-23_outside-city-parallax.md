# Handoff — Outside city parallax

## Changed
- Added `OutsideCityPresenter`: a collider-free, procedural skyline behind the tower with distant, middle, and street silhouette layers, irregular windows, roof details, lamps, and sky/horizon colour bands. The three layer roots follow horizontal camera pans at different rates. Window and sky colours respond to the existing day/night clock in half-hour buckets.
- The city anchors to the ground slab and rebuilds when that edge or floor count changes. Overview framing shifts slightly right to show the city beside the five-floor cutaway. No simulation or save state was added.
- Added EditMode geometry, parallax, lighting, and rebuild tests plus a PlayMode tower integration test. ADR-073 records the presentation choice.

## Validation
- Isolated worktree `/home/geisha/Vibecode/UnityAI/one-roof-outside-city-validation` at `b00d715`, with the uncommitted Outside feature and city runtime/test files copied in. No connected editor was used; the worktree was removed after validation.
- One-shot `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof-outside-city-validation -logFile /tmp/one-roof-outside-city-compile.log` — exit 0.
- `OutsideCityPresenterTests` EditMode: 2/2 passed, exit 0 (`/tmp/one-roof-outside-city-tests.xml`). Full EditMode: 451/453 passed, exit 2 (`/tmp/one-roof-outside-city-editmode.xml`). The only failures are `RoomPresenterTests.EnsureRoomViews_AdoptsAuthoredRoomInsteadOfCreatingDuplicate` and `TowerStructurePresenterTests.Initialize_RemovesDuplicateSlabAndPreservesCanonicalGeometry`, reproduced on pristine HEAD during OR-1004 validation.
- Full PlayMode: 6/6 passed, exit 0 (`/tmp/one-roof-outside-city-playmode.xml`), including the tower-level parallax test. Test runs used `timeout 1500` and `-runTests` without `-quit`.

## Limits
- Headless tests verify geometry, face direction, camera movement, lighting colours, and tower integration. They cannot substitute for a visual composition pass in a rendered player window. The skyline is deliberately a cutaway backdrop, not a navigable street or simulated city.

## Screenshot follow-up — 2026-09-23
- The ground-start screenshot showed bright overlapping facades, buildings much taller than the lobby, and a hard-edged sky rectangle. The presenter now removes orphaned generated roots after an `ExecuteAlways` domain reload, starts facades in their night tint, scales building and roof heights to street proportions, and draws against the camera background. ADR-073 reflects the revised composition.
- Isolated worktree `/tmp/one-roof-city-visual-validation` contained byte-identical presenter and test files to the committed checkout at `0ccd3ae`. Headless compile command: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /tmp/one-roof-city-visual-validation -logFile /tmp/one-roof-city-visual-compile.log`; the log ends with `Exiting batchmode successfully now!`.
- Focused EditMode command: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-city-visual-validation -runTests -testPlatform EditMode -testFilter OutsideCityPresenterTests -testResults /tmp/one-roof-city-visual-editmode.xml -logFile /tmp/one-roof-city-visual-editmode.log` — exit 0, 3/3 passed.
- Focused PlayMode command: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-city-visual-validation -runTests -testPlatform PlayMode -testFilter OutsideCityPlayModeTests -testResults /tmp/one-roof-city-visual-playmode.xml -logFile /tmp/one-roof-city-visual-playmode.log` — exit 0, 1/1 passed. The scratch worktree was removed after validation.
- No connected Unity Editor was available to `unity status`, including outside the sandbox. Next safe action: inspect the ground-start Game view after Unity reloads the committed presenter to judge the final composition. Headless tests cannot verify rendered colours and framing visually.
