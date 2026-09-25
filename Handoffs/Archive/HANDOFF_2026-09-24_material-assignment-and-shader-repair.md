# Material Assignment, Shader Repair & Scene Material Normalization

## Delivered

- **URP 17.3 LTS Shader Repair (`OneRoofUnlit.shader`)**:
  - Added `HLSLINCLUDE` consolidating vertex/fragment passes (`SRPDefaultUnlit`, `UniversalForward`, `UniversalForwardOnly`).
  - Added `Universal2D` pass (`LightMode = "Universal2D"`) for full 2D renderer support.
  - Added `[PerRendererData] [HideInInspector] _MainTex` and sample product (`SAMPLE_TEXTURE2D(_BaseMap) * SAMPLE_TEXTURE2D(_MainTex)`), enabling `SpriteRenderer` textures while preserving mesh base maps.
  - Added full constant buffer properties (`_Cutoff`, `_SrcBlend`, `_DstBlend`, `_ZWrite`, `_Cull`) to `UnityPerMaterial` for SRP Batcher compliance.
  - Added `#pragma shader_feature_local_fragment _ALPHATEST_ON` and `clip(finalColor.a - cutoff)` alpha test cutout support.
  - Corrected zero-color handling so transparent vertex colors do not snap to solid opaque white.

- **Plugin Shader & Texture Fixes**:
  - Repointed broken texture GUID `32e8df0008bebe746b1681a71c61f31c` in `Assets/Plugins/AllIn1SpriteShader/Materials/EmptyMaterial.mat` to valid `fire2.png` (`guid: 677cca399782dea41aedc1d292ecb67d`).
  - Prioritized URP shader `"AllIn1SpriteShader/AllIn1SpriteShaderSRPBatch"` over legacy built-in pipeline shader `"AllIn1SpriteShader/AllIn1SpriteShader"` across `OutsideCityPresenter`, `InspectOutlinePresenter`, `PlacementGhostPresenter`, `UtilitiesNetworkLayerPresenter`, `VisualEffectsPresenter`, eliminating magenta/pink fallbacks in URP 17.3 LTS.

- **Runtime Presenter Material Propagation**:
  - `NpcViewPool` & `NpcSkeletalHierarchy`:
    - `NpcViewPool.CreateNewViewInstance` passes `GetOrCreateSharedMaterial()` to `skeletal.Initialize(...)`.
    - `NpcSkeletalHierarchy.Initialize` accepts `Material defaultMaterial = null` (with static fallback `GetOrCreateDefaultSharedMaterial()`).
    - `NpcSkeletalHierarchy.ApplySharedMaterial` propagates material across `MainRenderer`, `StatusPlateRenderer`, `EmoteRenderer`, `CaptionBackground`, all `_wardrobeSlots`, and procedural `_limbRenderers`.
    - Added EditMode test `Pool_Acquire_AssignsMaterialToSkeletalHierarchyRenderers`.
  - `RoomPresenter`, `RoomBackdropPresenter`, `RoomFurnishingPresenter`:
    - `RoomPresenter` passes `_worldMaterial` into `RoomBackdropPresenter.Setup(...)` and `RoomFurnishingPresenter.FurnishRoom(...)`.
    - `RoomBackdropPresenter` assigns `_sharedMaterial` to `BackdropRenderer` (9-sliced), `DoorRenderer`, `WindowRenderer`, and `SconceRenderer`.
    - `RoomFurnishingPresenter` assigns `_sharedMaterial` to all spawned interior props in `SpawnProp`.
    - `RoomPresenter.CreateExitSign` assigns `_worldMaterial` to exit sign `SpriteRenderer`.
    - Added EditMode test `EnsureRoomViews_AssignsMaterialToBackdropAndFurnishingRenderers`.
  - `PlacementGhostPresenter`: Fixed check to `_ghostRenderer.sharedMaterial != _ghostMaterial` ensuring primitive quad default material is replaced.
  - `BuildingExteriorPresenter` & `ElevatorBankPresenter`: Guaranteed `_worldMaterial` assignment on adopted and created mesh quads.
  - `TowerPlayableController`: Removed passing opaque `_worldMat` to `PixelRainPresenter`, allowing rain to generate its dedicated transparent rain material.

- **Scene File Material Normalization**:
  - In `Scenes/Tower.unity`: normalized 77 MeshRenderers from internal embedded `{fileID: 628950773}` to project asset `{fileID: 2100000, guid: 57ba4a881eaa410e88c04ba0bfe96108, type: 2}` (`OneRoofWorldMaterial.mat`).
  - In `Scenes/Tower_GroundStart.unity`: normalized 16 MeshRenderers from internal embedded `{fileID: 546002247}` to `{fileID: 2100000, guid: 57ba4a881eaa410e88c04ba0bfe96108, type: 2}` (`OneRoofWorldMaterial.mat`).

## Validation Evidence

- **Isolated worktree**: `/home/geisha/.codex/worktrees/material-assignment-validation/one-roof`
- **Headless Unity compile**: Unity 6000.3.24f1 batchmode compile exited with code 0 (`/tmp/one-roof-compile.log`).
- **EditMode test suite**: Exited with code 0. **525 passed, 0 failed, 0 skipped** (`/tmp/one-roof-editmode.xml`).
- **PlayMode test suite**: Exited with code 0. **7 passed, 0 failed, 0 skipped** (`/tmp/one-roof-playmode.xml`).
- **Whitespace / formatting audit**: `git diff --check` exited with code 0.
- Scratch worktree cleanly pruned and removed via `git worktree remove --force`.

## Next Safe Action

- Review the visual results in Play Mode in a connected/visible Unity Editor.
- Verify that resident sprites, wardrobe layers, room backdrops, and props render with expected lighting, textures, and palettes under URP.
