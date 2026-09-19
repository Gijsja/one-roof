# ART-003 — Spine Animation & Wardrobe Composition

**Status:** DONE  
**Owner:** Codex  
**Started:** 2026-09-19  
**Updated:** 2026-09-19

## Objective

Complete the shared 17-bone resident rig, six common animation states, and dynamic eight-layer wardrobe composition.

## Acceptance criteria

- [x] Runtime hierarchy indexes all 17 canonical rig bones.
- [x] Idle, Walk, QueueWait, Ride, Sit, and Sleep animation states are supported.
- [x] Every resident resolves an immutable, rig-compatible eight-layer wardrobe loadout from approved content records.
- [x] Eight ordered visual slot renderers compose procedural wardrobe swatches without storing Unity references in simulation data.
- [x] Live-Pipeline compilation and focused content/presentation tests pass.

## Changes made

- `Assets/OneRoof/Runtime/Content/NpcAnimationClip.cs` — six shared animation states.
- `Assets/OneRoof/Runtime/Content/NpcWardrobeLoadout.cs` — immutable 8-layer compatible loadout contract.
- `Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs` — completes 17-bone hierarchy, slot compositor, and procedural clip poses.
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs` — selects sleep/sit/idle for in-room activity.
- `Assets/OneRoof/Tests/EditMode/Content/NpcSkeletalRigTests.cs` and `Assets/OneRoof/Tests/EditMode/Presentation/ResidentSpriteCatalogTests.cs` — coverage.

## Validation

| Check | Command | Result |
| --- | --- | --- |
| Live Pipeline compile | `unity command recompile` then `recompile_status` | PASS — completed, no errors |
| Content Edit Mode assembly | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Content.Tests.EditMode` | PASS — 12 / 12 |
| Skeletal hierarchy | `unity command run_tests --mode editor --filter OneRoof.Presentation.Tests.EditMode.ResidentSpriteCatalogTests.SkeletalHierarchy_BuildsBonesAndSlotRenderers` | PASS — 1 / 1 |
| Animation/emote continuity | `unity command run_tests --mode editor --filter OneRoof.Presentation.Tests.EditMode.ResidentSpriteCatalogTests.SkeletalHierarchy_EmoteBubbleSetupAndAnimation` | PASS — 1 / 1 |

## Next safe action

Proceed to OR-601: resident needs and dynamic schedule arbitration.
