# Handoff — NPC Spine (skeletal) alignment fix

## What changed (8 files, all in current checkout as uncommitted edits)
- `Assets/OneRoof/Runtime/Content/NpcRigDefinition.cs` — new `GetParentBone(layer)` slot→bone map mirroring `resident-spine-setup-v1.json` (face/hair/accessory→head, upper/body→spine, lower/footwear-pair→hip, prop→hand_L).
- `Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs` — slots parent to spec bones (with authored-slot reuse + `RepointWardrobeSlots` migration); single anchor path (`FitSwatchSlot`, `ConfigureWardrobeSlot` deleted); anatomy sorting 12–14 under wardrobe 16+; facing/stature split (`SetFacing`, `FacingDirection`, `BodyStature`); emote anchor counter-mirrored.
- `Assets/OneRoof/Runtime/Presentation/Population/WardrobePartCatalog.cs` — bone-local `SlotAnchor` table shared by photo and swatch paths + `FitSwatchSlot`; added CarriedProp target size.
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs` — facing via `SetFacing` (room/queue/walk); walkers face latched travel direction (`_lastWalkX`/`_walkFacing`).
- 4 test files extended: rig map test, slot-parent test, anchor-parity test, 3 facing tests.

## Validation (isolated worktree `../one-roof-npc-spine-validation` @ efc0dfb, since removed)
- Compile: `Unity -batchmode -nographics -quit -projectPath …` → exit 0.
- EditMode suite: 427 total, 424 passed, 3 failed — all 3 proven pre-existing on pristine HEAD via stash + filtered re-runs:
  - `GridPlacementControllerTests.IsPointerOverUI_*` (2, baseline 38/36/2),
  - `ModeShellBarControllerTests…ReopensPalette…` (1, baseline 9/8/1).
- All 6 new tests pass in the full run. Results: `/tmp/opencode/one-roof-npc-spine-editmode.xml`.

## Follow-up pass — procedural clips rewritten (same checkout, uncommitted)
- `NpcSkeletalHierarchy.ApplyProceduralAnimation` was single-joint sway (peg-leg walk,
  58° diagonal sit, plank sleep). Rewrote animator-style: two-bone knees (push-off fold +
  swing fold, never forward), elbows (soft bend + back-swing fold), partial foot
  flattening, 2x-frequency pelvis bob (walk), lateral weight shift + transfer dip (queue),
  true seated fold 78/-72/-6 with hands to lap (sit), fetal-relaxed plank (sleep), braced
  rail-hold (ride), breathing + unlocked elbows (idle). Hip offsets compose on a captured
  bind pose (`_hipBasePos`) restored every frame.
- 3 new motion tests in `ResidentSpriteCatalogTests` (walk knees/bob/feet, sit fold sum,
  hip restore). Validation in `../one-roof-npc-anim-validation` (removed): compile exit 0,
  EditMode 430 total / 427 pass — only the same 3 pre-existing UI failures, all 9 NPC
  tests green. Results: `/tmp/opencode/one-roof-npc-anim-editmode.xml`.

## Risks / notes
- Footwear pair rides the hip, not per-foot bones (deliberate; documented in code).
- Scene-authored residents get reparented on next `EnsureHierarchy`; stale Spine-local slot offsets refresh via the `Initialize→ApplyWardrobe` call the presenters already make.
- Unrelated uncommitted work in the checkout was left untouched.

## Next safe action
- PlayMode smoke of a crowded landing + bedroom (walk swing, queue sway, sleep lie-flat) to eyeball shoes/hats/props, then merge.
