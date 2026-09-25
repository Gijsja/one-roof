# M9 factions and management implementation handoff

## Scope and result

Completed OR-901–905 against `Planning/M9_FACTIONS_AND_MANAGEMENT_EXECUTION.md` using three subagents and integration work in the shared tree. Four factions and a capped, deterministic relationship graph update at daily settlement and survive save/load. Manage mode drafts and enacts the four existing decrees, shows qualitative tradeoffs and measured daily changes, and links to faction tension. Noise and faction overlays now draw world-aligned contours with non-colour labels and inspector routes. Rent strikes, lobby protests, and work slowdowns have warning, active, recovery, and cooldown phases with concrete rent, transit, and business effects. A bounded saved decision timeline links representative residents/businesses and observed balances. A keyboard-accessible, skippable guide completes the five-floor symptom → overlay → inspector → preview → build → measured queue improvement → Manage path.

The Domain assembly remains free of UnityEngine references. New assets have paired `.meta` files. ADR-077 records the daily social and civil-action ownership decision. `Planning/BACKLOG.md` and `Planning/ROADMAP.md` mark M9 complete.

## Validation

- Worktree: `/home/geisha/Vibecode/UnityAI/one-roof-m9-validation` at HEAD `98ed640`, with the scoped M9 source and test files copied from the shared tree. The controller file also carried pre-existing unrelated presentation work from the shared tree; no scene files were copied. Unity changed `.vscode/settings.json` and `ProjectSettings/ProjectSettings.asset` only inside the validation worktree. Removed this scratch worktree after validation (`git worktree remove --force`, exit 0).
- Compile: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof-m9-validation -logFile /tmp/one-roof-m9-contour-compile.log` → exit 0.
- EditMode: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /home/geisha/Vibecode/UnityAI/one-roof-m9-validation -runTests -testPlatform EditMode -testResults /tmp/one-roof-m9-editmode-exit.xml -logFile /tmp/one-roof-m9-editmode-exit.log` → exit 0, **547/547 passed**. This includes deterministic faction, rivalry, civil action, persistence, decree, overlay, decision, and controller tests.
- PlayMode: same `timeout 1500 ... -runTests -testPlatform PlayMode` with `/tmp/one-roof-m9-playmode-exit.xml` and `/tmp/one-roof-m9-playmode-exit.log` → exit 0, **8/8 passed**, including the complete new management guide path.
- Focused integrated proof: `/tmp/one-roof-m9-integrated-action.xml` → 1/1 passed; seeded high-rent/high-tax and arrears fixture forms faction members, reaches an active rent strike, records it, and preserves social/civil/decision/economy state through save/load.
- `git diff --check` → exit 0. New M9 C# files have paired `.meta` files.

## Limits and next safe action

Headless tests verify the overlay data, presenter binding, and PlayMode flow, but do not provide a screenshot-level visual review. The 300-resident edge-cap test passes; a reference-hardware profile proving the full `<4 ms` tick and 60 FPS presentation budgets is still required for Beta exit. Complete spatial visual/accessibility QA in OR-1006 and reference-hardware profiling in OR-1003. The unrelated shared-tree scene/presentation edits remain untouched.
