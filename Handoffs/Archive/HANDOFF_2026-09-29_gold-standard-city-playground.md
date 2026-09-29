# OR-1003A — Gold Standard 30-floor city playground

## Delivered

- New `Assets/Scenes/Tower_GoldStandard30.unity`; open and press Play. Separate from existing startup scenes and Build Settings.
- Deterministic populated fixture: 30 floors, 100 households / 300 residents, ground lobby/retail/diner, four upper service/workplace floors, three lifts, continuous stairs, electrical/water/waste risers, floor transformers and boosters every four floors. Starts at 07:30 with 250,000 treasury; existing simulation and economy remain authoritative.
- Enum value 2 selects the city start. Reset recreates the city; it does not fall back to five floors. Gold-city weather uses a fixed seed. Startup construction fades are completed before the prebuilt scene's first frame; subsequent player placements retain normal effects.
- `GoldStandardPlayground` instantiates the environment prefab, skips the five-floor tutorial and provides camera shortcuts 5/6/7 (overview/street/rooftop). The camera's maximum zoom now accommodates its calculated overview for 30 floors.
- Nine Unity-authored mesh assets and two shared URP materials in `Assets/OneRoof/Art/GoldCity`, plus `GoldCityEnvironment.prefab`: three skyline depths, roof details, storefronts, signs, streets, crossings, lamps, benches, trees, transit shelters, 12 vehicles, seven clouds and seven birds. No imported art, additional packages or vendor edits. The existing All In 1 construction effects remain in use for player-built rooms.
- Environment lighting reads the existing day/weather projection, and ambient animation pauses with the simulation. Gold-level light-cone strengths are reduced to keep the dense furnished cutaway legible; other scenes retain default strengths. Ground continues behind the underground cutaway instead of exposing sky below the road.
- Generated assets preserve their `.meta` identities when rebuilt. The small scene stores a bootstrap and an inactive tower, avoiding serialized runtime view hierarchies. Source/provenance and integration contract: `Art/SourceArt/Proposed/GoldCity/ASSET_SPEC.md`. Usage: `Planning/GOLD_STANDARD_30_PLAYGROUND.md`. ADR-084 records the boundary.

## Validation

Unity 6000.3.24f1. Isolated worktree: `/home/geisha/.codex/worktrees/gold-city-validation/one-roof`, base `555ea10`.

Commands use `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity`:

1. Initial headless compile: `-batchmode -nographics -quit -projectPath <worktree> -logFile /tmp/gold-city-compile.log`; exit 0.
2. Final Unity asset/scene authoring: `-batchmode -nographics -quit -projectPath <worktree> -executeMethod OneRoof.Editor.Tower.GoldStandardLevelBuilder.Build -logFile /tmp/gold-city-build-final.log`; exit 0, `GOLD_CITY_BUILT` emitted.
3. Final full EditMode: `-batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testResults /tmp/gold-city-final-editmode.xml -logFile /tmp/gold-city-final-editmode.log`; exit 0, **757 passed / 0 failed / 1 explicit benchmark skipped**.
4. Final full graphics PlayMode: `-batchmode -projectPath <worktree> -runTests -testPlatform PlayMode -testResults /tmp/gold-city-final-playmode.xml -logFile /tmp/gold-city-final-playmode.log`; exit 2, **11 passed / 1 failed** (ground-start fog pixel threshold; new playground test passed).

Each final test process is bounded by a 1500-second Python subprocess timeout; no `-quit` is passed with `-runTests`. The earlier full run passed 757 EditMode tests, one explicit benchmark skipped, and all 12 graphics PlayMode tests.

New coverage checks room non-overlap, unique room/person/household/car IDs, every home's transit route to the lobby, all-floor electrical/water/waste service, deterministic initial simulation, snapshot restoration, city reset, scene boot, ambient motion/pause, 60-view pool cap, camera overview and environment uniqueness after reset. Graphics captures exercise the scene at 07:30 and after advancing its normal simulation to 20:30; these are world-camera captures, not GUI screenshots.

Final review: all 47 then-current changed/new asset and code files matched the isolated validation checkout byte-for-byte; new code/shaders have paired metadata; Domain has zero UnityEngine/UnityEditor references; `git diff --check` passed. The environment has 36,528 authored mesh vertices and no added textures. World-camera previews are stored in `Art/SourceArt/Proposed/GoldCity/{overview,overview-night,street,street-night}.png`.

## Findings and limits

- First PlayMode run exposed an unloaded prefab reference while the builder switched scenes. The builder now reloads the prefab after scene creation and checks it before saving; the fixed scene passes loading and reset.
- Initial captures caught transient construction fades hiding the prebuilt interiors. Startup now completes those effects immediately. Final captures were reviewed at overview and street scale, in day and night.
- A stronger exploratory assertion failed after save/load: continuous versus restored runs diverged in generated trip IDs (`196` versus `1`) after another 60 ticks. Evidence: `/tmp/gold-city-editmode.xml`, `CitySeedAndSaveContinuation_AreDeterministic`. `ScheduleTripGenerator._nextTripId` is initialized in its constructor and absent from `TowerSaveData`; no persistence/schema change is included in this visual-level task. The retained test explicitly verifies seeded initial determinism, exact snapshot restoration and continued operation, not byte-identical post-load replay. OR-1003 must address that before claiming deterministic campaign continuation.
- This task does not claim a 30-day City Status hold, <4 ms p95 simulation, 60 FPS, or <120 draw calls on reference hardware. The routine suite skips the explicit benchmark. The populated scenario is live and can develop economic/service pressure over time.
- The final full graphics run failed `WeatherVisibilityPlayModeTests.GroundStart_RainAppearsInCameraCapture`: 107,171 changed fog pixels versus a <92,160 threshold. Both focused pristine-HEAD checks passed (`/tmp/gold-city-fog-baseline-1.xml` and `-2.xml`, exit 0, 1/1 each), so this cannot be claimed as a reproduced baseline failure. An unchanged full-suite repeat also failed with 109,637 pixels (`/tmp/gold-city-final-playmode-repeat.xml`, exit 2, 11/12 passed). Direct inspection of its clear/fog images showed the clear reference still using the lobby construction material while the fog frame used the restored backdrop. The test's fixed 0.5-second wait was shorter than the 0.55-second construction transition. `WeatherVisibilityPlayModeTests` now waits, with a three-second deadline, until all construction effects finish before taking the clear reference; its original >100 visible-fog-pixel and <10% coverage thresholds remain unchanged. This is test isolation from an unrelated room animation, not a fog-rendering change. Full suite after that fix: running; results at `/tmp/gold-city-settled-playmode.xml`, log `/tmp/gold-city-settled-playmode.log`. The user requested an immediate handoff, so the isolated worktree is retained until this run is inspected and can then be removed.
- Original untracked `Art/SourceArt/Proposed/Concepts/` was preserved.

## Next safe action

Open `Tower_GoldStandard30`, press Play, then use 5/6/7, W and the existing mode shell to explore. Profile this populated scene on reference hardware and continue OR-1003's persistence and 30-day acceptance work independently of the visual playground.
