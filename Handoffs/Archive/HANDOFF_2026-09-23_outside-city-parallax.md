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
