# ARCH-007 — Build palette pointer shield

**Status:** DONE
**Date:** 2026-09-22

## Change

The palette is deliberately hidden once a build tool is selected, freeing its footprint for world placement. The placement controller previously disabled the entire bottom UI mask at that point, including the still-visible mode bar and context strip. Their clicks could therefore reach tower placement. `ModeShellBarController` now supplies the actual IMGUI rectangles to the pointer shield: the mode bar and context strip remain protected, and the palette rectangle is protected only while the chooser is shown. The placement tests cover both selected-tool UI shielding and world pass-through under the closed palette.

## Validation

- Isolated worktree: `../one-roof-palette-interaction-validation`, based on `e21940c`; only the two runtime controllers and `GridPlacementControllerTests.cs` were copied in. Removed after validation.
- `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof-palette-interaction-validation -logFile /tmp/opencode/one-roof-palette-compile.log` — exit 0.
- `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /home/geisha/Vibecode/UnityAI/one-roof-palette-interaction-validation -runTests -testPlatform EditMode -testResults /tmp/opencode/one-roof-palette-editmode.xml -logFile /tmp/opencode/one-roof-palette-editmode.log` — exit 0, 444 passed, 0 failed.
- `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /home/geisha/Vibecode/UnityAI/one-roof-palette-interaction-validation -runTests -testPlatform PlayMode -testResults /tmp/opencode/one-roof-palette-playmode.xml -logFile /tmp/opencode/one-roof-palette-playmode.log` — exit 0, 5 passed, 0 failed.

## Next safe action

If the Build palette's layout changes, continue using the shared rectangle methods for both IMGUI drawing and world-pointer masking. The remaining Build-placement module proposal can be evaluated against observed interaction problems rather than moving all placement logic at once.
