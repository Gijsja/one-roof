# Cleanup sprint weather captures — 2026-09-29

Captured from `Tower_GroundStart` while exercising the in-game weather override cycle.

- `weather-clear.png`, `weather-drizzle.png`, `weather-rain.png`, `weather-storm.png`, `weather-fog.png`, `weather-snow.png`, `weather-auto.png`: initial cycle, in that order. HUD labels and precipitation/snow VFX are visible. The original fog capture shows broad opaque panels and motivated the fog mesh/opacity fix.
- `weather-fog-refined.png`, `weather-fog-refined-final.png`: manual captures made after the first fog code change; they still show panel-like artifacts.
- `weather-fog-automated-radial.png`: refreshed graphics PlayMode camera capture from the isolated validation copy after replacing each rectangular fog patch with a radial mesh. Full graphics PlayMode passed 11/11, including the capture test on a non-Null graphics device. The image has no panel edges and keeps the facade/interior readable; the puffs remain sparse and this ground-level view is not a complete weather-art acceptance pass.

Exterior captures made from the actual serialized `Tower_GroundStart` scene in the isolated graphics-enabled copy:

- `tower-groundstart-0600-dawn.png` — dawn transition at 06:00.
- `tower-groundstart-1200-day.png` — full daylight at 12:00.
- `tower-groundstart-2200-night.png` — night at 22:00.

The temporary capture fixture passed 1/1 (`/tmp/one-roof-daynight-capture.xml`) against the real serialized scene without saving scene assets. A separate window-light A/B passed 1/1 (`/tmp/one-roof-windowlight-ab-confirm.xml`), holding facade cones enabled while toggling only `WindowLightRenderers`; captures `windowlight-ab-*.png` show the 12:00 stripes appear when those renderers are disabled and 22:00 output is unchanged. This falsifies that renderer list as the source of the issue: disabled noon rendering reveals the pattern and both night images are pixel-identical. The normal camera renders a tightly cropped ground-level slice. The light cone and some room illumination change across states, while the facade and sky remain in a similarly dark palette. The earlier noon/night captures showed wavy/striped interior patterns in the lobby, but they did not reproduce consistently in the A/B runs. The crop shows the full single-floor facade and ground cutaway but cannot establish multistory composition. These captures document the issue and do not close the exterior visual gate.

Room backdrop A/B from the actual serialized scene at 12:00, with atmosphere window-light renderers disabled and facade cones left enabled:

- `room-backdrop-ab-1200-backdrop-enabled.png`: baseline with the authored `RoomView_14/Backdrop` SpriteRenderer enabled.
- `room-backdrop-ab-1200-backdrop-disabled.png`: same state after disabling only that SpriteRenderer.

The fixture passed 1/1 (`/tmp/one-roof-room-backdrop-ab.xml`) and restored the SpriteRenderer without saving the scene. The broad wavy stripes were absent in both captures, so this A/B does not identify their source. Disabling the backdrop made the plant and room contents more visible, with only a small pixel difference; keep the stripe source unproven.

Runtime build and placement captures from the actual serialized scene at 12:00:

- `placement-valid-floor-ghost-1200.png` and `placement-invalid-existing-floor-ghost-1200.png`: green valid preview for a new floor and red invalid preview over the existing ground floor. The fixture checked each `ValidatePlacement` result against the visible `PlacementGhostPresenter` state.
- `ground-slab-expanded-1200.png`: after an accepted runtime ground-slab expansion; the test checked floor deck bounds and exterior scupper/entrance alignment against the expanded slab.
- `new-floor-slab-built-1200.png`: after an accepted new-floor slab placement; the test checked floor, deck, and both facade wall elevations and slab bounds.

The focused PlayMode capture passed 1/1 (`/tmp/one-roof-build-placement-capture-final.xml`). The fixture hashes `Tower_GroundStart.unity` before and after, with matching values, and performs no scene save. The green/red previews are visible but cover most of the floor slice, leaving practical readability and pointer-driven hover unverified. The slab expansion and added-floor captures show the runtime geometry aligned. The added-floor image was captured immediately after placement and shows a wavy pattern; a separate runtime-room A/B found a similar pattern only during the 0.55-second construction transition. That does not directly prove the placement image is a transition state, and neither runtime test identifies the separate authored-ground day/night artifact.

Undercity framing captures from an isolated graphics-enabled temporary fixture:

- `undercity-lobby-entrance-crossing-r4.png`: investigator at the lobby entrance; the frame is obscured by a broad light cone and overlapping resident labels.
- `undercity-staffed-access-room-r4.png`: readable AccessHub view showing 1/1 staff. A second wider framing attempt passed its fixture assertions but did not improve the target views, so it was not retained.
- `undercity-investigator-route-stop-r4.png`: investigator is at the route stop, but the room caption is clipped/overlapped.

The r4 undercity fixture passed 1/1 (`/tmp/one-roof-undercity-framing-capture-r4.xml`); r5 also passed 1/1 (`/tmp/one-roof-undercity-framing-capture-r5.xml`). No scene was saved. Pointer-driven placement interaction, preview readability, and clearer undercity entrance/route framing remain open. A daytime utility-flow capture is now available; full-tower utility-overlay framing remains open. Multistate captures were produced in isolated graphics-enabled copies; the shared Editor was not used.

Utility overlay daylight capture:

- `utilities-water-flow-daytime.png` is a graphics PlayMode camera capture from the isolated M10 validation worktree. The fixture creates a runtime `TowerPlayableController` in `GroundFloorStart` mode (it does not load or save the serialized `Tower_GroundStart` scene), advances the simulation to **Day 1, 12:00**, enables Data mode and the Utilities overlay, selects **Water**, and captures the flowing pump/riser/core/corridor/access-room path. The world-space route overlay remains active in the image.
- The formerly gold triangles are authored exterior window-light cones (`BuildingExteriorPresenter`); daytime fades them to pale blue-white. They remain visible at the upper edge, but do not obscure the visible structure or cyan utility route. No broad opaque panels appear. This is evidence for utility-flow readability at daytime, not a replacement for the separately open exterior day/night or full-tower framing checks.
- The noon adjustment existed only in the temporary isolated capture fixture and was removed afterward. Command: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -projectPath /home/geisha/.codex/worktrees/m10-connected-validation/one-roof -runTests -testPlatform PlayMode -testFilter UndercityUtilitiesVisualCapture_RunningAndBrokenFlow -testResults /tmp/one-roof-m10-utilities-visual-daytime.xml -logFile /tmp/one-roof-m10-utilities-visual-daytime.log`. Result: **1 passed, 0 failed, 0 skipped**. The fixture also wrote the running-power and stopped-power comparison captures under `/tmp/one-roof-m10-utilities-visual-daytime/`.


Targeted shared MaterialPropertyBlock (MPB) texture A/B from the runtime floor placement fixture:

- `shared-mpb-baseline-runtime-floor-room.png`: baseline after expanding the ground slab, adding floor 1, and creating runtime room 2002.
- `shared-mpb-runtime-quads-cleared.png`: same state after clearing only the runtime `Floor Line L/R 1` and `RoomView_2002` apt-tag/wall renderer blocks. Facade cone renderers remained enabled.
- `shared-mpb-property-block-textures.txt`: `_BaseMap`/`_MainTex` inspection for exact runtime-created renderers. The new slab and floor structure lines have null texture overrides; room quads report UnityWhite in both slots; deck tread blocks were inspected but left untouched.

The graphics PlayMode run passed **1/1, 0 failed, 0 skipped** (`/tmp/one-roof-mpb-texture-ab-final2.xml`). The serialized `Tower_GroundStart.unity` hash check passed before/after; the fixture restored its runtime MPBs and did not save the scene. Exact command: `unity test /tmp/one-roof-cleanup-validation --mode PlayMode --filter TowerGroundStart_SharedMpbTextureLeakTargetedAB --output /tmp/one-roof-mpb-texture-ab-final2.xml`. Harness source SHA-256: `daea03dbf94a65e6d2125718403e9ae46961cd145563fa126f2514f235b548d3` (`/tmp/SharedMpbTextureLeakABPlayModeTests.cs`).

The affected cyan/gray flecks remain visible after clearing the runtime room/structure blocks. Pixel comparison found **0 changed pixels in the room artifact crop** `(x=1000..1279, y=280..329)`; the overall image difference is limited to 5,296 pixels on the floor-divider strip at y=244..248 (mean absolute RGB difference 0.95, 0.80, 0.61). This A/B does not support those runtime room/structure MPB texture overrides as the fleck source. The separate slab-block-cleared capture is retained only in `/tmp/one-roof-mpb-texture-ab/new-slab-block-cleared.png`: clearing the full slab block turns the upper region white and is confounded by non-texture properties, so it is not included as visual evidence. The MPB texture-leak hypothesis remains unproven for other renderers/shaders.


Runtime room backdrop A/B after the same expansion and floor/apartment setup at noon:

- `runtime-room-backdrop-enabled.png`: `RoomView_2002/Backdrop` enabled (baseline).
- `runtime-room-backdrop-disabled.png`: only that room's `RoomBackdropPresenter.BackdropRenderer` SpriteRenderer disabled; weather, time, lights, camera, floor, and other renderers stayed fixed.
- `runtime-room-backdrop-and-tread-identities.txt`: exact room hierarchy plus the `Floor Deck 1` tread renderer/material/texture identities; treads were inspection-only and untouched.
- `runtime-room-backdrop-ab.xml`: focused graphics PlayMode XML.

The fixture passed **1/1, 0 failed, 0 skipped**. It restored the backdrop enabled state in `finally`, asserted the serialized scene hash unchanged, and did not save the scene. Command: `unity test /tmp/one-roof-cleanup-validation --mode PlayMode --filter TowerGroundStart_RuntimeRoomBackdropTargetedAB --output /tmp/one-roof-runtime-room-backdrop-ab.xml`. Harness source SHA-256: `3cf12cb7f3d497a432fab0735a383ab415225bf41fcd20f41aa83a92dd4ac2bb` (`/tmp/RuntimeRoomBackdropABPlayModeTests.cs`).

Disabling the backdrop exposes strong cyan and pale wavy patterns inside this runtime-created apartment, which are largely hidden when the backdrop is enabled. This makes the backdrop state diagnostically relevant, but does not establish whether the pattern comes from the backdrop, underlying room renderers, or their draw order. The capture occurred only one frame after room creation, while `VisualEffectsPresenter` was still in its 0.55-second construction transition; the backdrop had an AllIn1 `city transition` material. Treat this as transition-time occlusion evidence, not a steady-state comparison. The earlier authored-room backdrop A/B had no stripes in either state. A steady-state rerun after `IsTransitioning` is false is recorded below; it removes the construction-effect confound and supersedes the apparent pattern in these first captures.


Steady-state runtime room backdrop A/B, after the construction effect ended:

- `runtime-room-backdrop-enabled-steady.png`: baseline with runtime `RoomView_2002/Backdrop` enabled.
- `runtime-room-backdrop-disabled-steady.png`: only that SpriteRenderer disabled, with lighting, camera, time, room, and all other renderers fixed.
- `runtime-room-backdrop-identities-steady.txt`: hierarchy and material shader/render-queue states for the backdrop and all room child renderers in both states, plus floor tread identities.
- `runtime-room-backdrop-steady-ab.xml`: focused PlayMode result.

The fixture waited at least 0.70 seconds after room creation and asserted `RoomView_2002.GetComponent<VisualEffectsPresenter>().IsTransitioning == false` before both captures. It passed **1/1, 0 failed, 0 skipped**, restored the backdrop enabled state in `finally`, and verified the serialized scene hash unchanged. Command: `unity test /tmp/one-roof-cleanup-validation --mode PlayMode --filter TowerGroundStart_RuntimeRoomBackdropSteadyStateTargetedAB --output /tmp/one-roof-runtime-room-backdrop-steady-ab.xml`. Harness SHA-256: `c6e9b40802ff9c779e581944251ae00e2d55f3feffc7be248d2c13ed4ad2838a` (`/tmp/RuntimeRoomBackdropSteadyStateABPlayModeTests.cs`).

Both steady-state captures use `OneRoof/Unlit`, render queue 2000 on the backdrop and room mesh/SpriteRenderers; furnishing props use `Universal Render Pipeline/2D/Sprite-Unlit-Default`, queue 3000. Disabling the backdrop removes the room's wallpaper/background while furnishings remain. The strong cyan/pale wavy pattern from the transition-state capture is absent: the previously affected crop `(x=1000..1279, y=280..329)` has **0 changed pixels** between steady-state captures. This steady-state A/B does not support the runtime backdrop as the wavy-pattern source.
