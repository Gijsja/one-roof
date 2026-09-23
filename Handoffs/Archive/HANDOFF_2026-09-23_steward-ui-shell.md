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

## Screenshot follow-up — 2026-09-23

The `Tower_GroundStart` Game view screenshot showed the Build palette overlapping the dashboard, horizontal overflow in the tool grid, a single floor rendered too small, and congestion actions shown before any residents had arrived. Commit `24e8ba3` includes the follow-up: the palette is anchored below the dashboard at the shown Game view height, tool cells fit without horizontal overflow, the one-floor camera opens closer to the lobby, and the empty-state dashboard points to homes and utilities.

Validation was repeated on exact committed `HEAD` (`0ccd3ae`) in isolated worktree `../one-roof-ui-review-validation` using Unity 6000.3.24f1:

| Check | Result |
| --- | --- |
| `-batchmode -nographics -quit` | Exit 0; `/tmp/one-roof-ui-committed-compile.log`; no C# errors. |
| EditMode `-testFilter TowerCameraControllerTests` | Exit 0; 7/7 in `/tmp/one-roof-ui-camera-editmode.xml`. |
| EditMode `-testFilter OneRoof.UI.Tests.EditMode` | Exit 0; 15/15 in `/tmp/one-roof-ui-committed-editmode.xml`. |
| PlayMode `-testFilter GoldenFirstPlayablePlayModeTests` | Exit 0; 1/1 in `/tmp/one-roof-ui-committed-playmode.xml`. |
| EditMode `-testFilter GridPlacementControllerTests` | Exit 0; 42/42 in `/tmp/one-roof-ui-committed-grid.xml`. The earlier pointer-region failure no longer reproduces. |

The next safe action is a visual Game view check at the same resolution; headless runs verify behavior and layout bounds but cannot produce a rendered comparison screenshot.
