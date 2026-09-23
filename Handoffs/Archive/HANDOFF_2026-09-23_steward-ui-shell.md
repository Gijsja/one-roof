# Steward UI shell refresh

**Status:** Implemented  
**Date:** 2026-09-23

## Scope

- Replaced the oversized debug dashboard with a compact steward status card showing day, treasury, residents, floors, transit pressure, and cause-chain entry points.
- Restyled the mode dock, added a grouped and scrollable Build palette, and made the seven implemented overlays selectable inside Data mode.
- Applied one shared visual language to congestion, deep inspection, and placement preview cards. Removed internal ticket IDs and raw content IDs from visible mode status copy.
- Preserved application-owned mode commands and inspector projections; no simulation or scene assets changed for this UI work.

## Validation

Validation used isolated sibling worktree `../one-roof-ui-validation` at `HEAD` with only the scoped UI diff copied in. Unity executable: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity`.

| Check | Result |
| --- | --- |
| One-shot `-batchmode -nographics -quit` compile | Exit 0; `/tmp/one-roof-ui-compile-final.log`; no C# errors. |
| `-runTests -testPlatform EditMode -testFilter OneRoof.UI.Tests.EditMode` | Exit 0; 14/14 passed in `/tmp/one-roof-ui-editmode-final.xml`. |
| `-runTests -testPlatform PlayMode -testFilter GoldenFirstPlayablePlayModeTests` | Exit 0; 1/1 passed in `/tmp/one-roof-ui-playmode.xml`. |
| `-runTests -testPlatform EditMode -testFilter GridPlacementControllerTests` | Exit 2; 41/42 passed in `/tmp/one-roof-ui-grid-editmode.xml`. Sole failure: `IsPointerOverUI_IdentifiesBottomAndTopUIRegions`, already recorded as pre-existing in `Planning/BACKLOG.md`. |
| Scoped `git diff --check` | Passed. |

## Review point

Open the `Tower` and `Tower_GroundStart` scenes in Game view at desktop resolutions to assess spacing, text legibility, and panel overlap. Headless validation could not provide a visual screenshot. The UI still uses the project's existing IMGUI shell; a future uGUI or UI Toolkit conversion would be a separate architecture change.
