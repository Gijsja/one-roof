# Resident emotes and activity captions

## Change

- Replaced the 24 existing emote families (base plus three frames each) and
  `emotes_sheet.png` with Kenney Emotes Pack Style 1 pixel assets. Existing
  resource paths, content IDs, and Unity GUIDs stay stable.
- Added small contrast-backed captions above visible residents. Captions come
  from read-only projections: activity, lift wait/ride destination, or travel.
  Long elevator waits display “Lift's late” alongside the anger emote.
- Cleared captions on pooled view release and resident reinitialization.
- `NpcView` now resolves its rig during setup; the pooled binding test exposed
  that direct binding previously skipped rig-based presentation.
- Recorded CC0 source and license in `Art/SourceArt/KENNEY_EMOTES.md` and
  `Art/SourceArt/KENNEY_EMOTES_LICENSE.txt`.

## Validation

- Isolated worktree: `/home/geisha/.codex/worktrees/resident-emotes-validation/one-roof`.
- Headless compile (Unity 6000.3.24f1, `-batchmode -nographics -quit`): exit 0.
  Final log: `/tmp/one-roof-emotes-compile-final.log`.
- EditMode `-testFilter Emote`: exit 0, 9 passed, 0 failed.
  XML: `/tmp/one-roof-emotes-asset-tests.xml`.
- EditMode `-testFilter ResidentActivityCaptionTests`: exit 0, 4 passed,
  0 failed. This includes pooled view binding and release.
  XML: `/tmp/one-roof-emotes-caption-tests-final.xml`.
- `git diff --check`: exit 0. All 96 replaced individual icons retain 16 × 16 dimensions.

## Risk and next safe action

- Headless validation does not show the captions at actual camera zoom. Check
  the Tower scene visually in Play Mode if an editor display is available;
  adjust the caption scale or width if dense floors overlap.
- Kenney has no exact symbol for every historical key; the source note records
  the closest-icon mapping. The caption communicates the concrete behavior.
