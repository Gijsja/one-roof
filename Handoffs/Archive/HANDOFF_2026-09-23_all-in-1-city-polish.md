# All In 1 Shader city polish — 2026-09-23

## Outcome

- Outside-city windows and street lamps use one shared All In 1 glow material. Glow tracks the existing day/night projection; geometry and simulation state are unchanged.
- Construction and demolition use All In 1 textured fade, warm/cool burn edges, and short glow. Original renderer materials are restored after construction. Congestion aura uses the same shader when available.
- Added focused EditMode assertions for shader selection, fade progression, material restoration, and night glow.

## Validation

- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-allin-validation` at `bcda209`, with only the four scoped source/test files copied in before validation. Removed after validation.
- Unity 6000.3.24f1 `-batchmode -nographics -quit -projectPath ... -logFile /tmp/one-roof-allin-compile.log`: exit 0; no C# or shader errors in log.
- EditMode `-testFilter VisualEffectsPresenterTests`, result `/tmp/one-roof-allin-effects.xml`: exit 0, 4/4 passed.
- EditMode `-testFilter OutsideCityPresenterTests`, result `/tmp/one-roof-allin-city.xml`: exit 0, 3/3 passed.
- PlayMode `-testFilter OutsideCityPlayModeTests`, result `/tmp/one-roof-allin-playmode.xml`: exit 0, 1/1 passed.
- `git diff --check` for the four scoped files: exit 0.

## Risks and next safe action

- Headless validation cannot judge final color balance or overdraw on a display. Open the tower in an interactive editor for a visual pass at day and night, and adjust glow strength if needed.
- Existing uncommitted work elsewhere in the shared checkout was not changed by this task.
