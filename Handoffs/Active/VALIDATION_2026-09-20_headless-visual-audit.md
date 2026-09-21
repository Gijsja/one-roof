# VALIDATION 2026-09-20 — Headless Test and Visual Audit

**Status:** BLOCKED
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-20

## Objective

Run the project headlessly, visually audit the Tower presentation, and turn confirmed gaps into testable backlog work.

## Acceptance criteria

- [x] Isolated validation checkout created.
- [x] Existing Tower screenshots inspected.
- [x] Reported window, elevator, and slab-placement gaps translated into acceptance-tested backlog tasks.
- [ ] Complete headless EditMode and PlayMode result XMLs produced.

## Scope and ownership

Changed files:

- `Planning/BACKLOG.md`
- `Handoffs/Active/VALIDATION_2026-09-20_headless-visual-audit.md`

Do not modify runtime code or Unity assets in this validation task.

## State at handoff

The visual audit of `Assets/Screenshots/visual_check_overview.png`, `upgraded_view.png`, and `floor5_built_view.png` shows a coherent cutaway, but not an acceptable completion of the reported features:

- `TowerAtmospherePresenter` creates transparent unlit quads, not a verified volumetric or reliably visible window-light treatment.
- `ElevatorBank.MaxCarsPerBank` is 4; four cars are therefore permitted even though the desired current cap is 3.
- Domain and controller tests exercise `floor:slab`, but the available captured UI does not prove the player can select, preview, and confirm the floor-slab tool in Build mode.

`OR-704`, `OR-705`, and `OR-706` record these as READY work with test and screenshot acceptance criteria.

## Changes made

- `Planning/BACKLOG.md` — added OR-704 through OR-706.

## Decisions

- Do not alter the reported behavior during a validation-only request; create narrowly scoped, testable tasks instead.
- Treat existing screenshots as historical visual evidence only, not proof of the current checkout after the newest changes.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Workspace baseline | `git status --short`; Unity-process check | PASS — source checkout clean; no human editor detected before validation. |
| Isolated checkout | Codex managed worktree at `/home/geisha/.codex/worktrees/headless-validation/one-roof` | PASS |
| Compile attempt | Unity 6000.3.24f1 one-shot `-batchmode -nographics -quit` | BLOCKED — script compilation completed (`Tundra build success` and `LogAssemblyErrors (0ms)`), but initial import did not exit cleanly under the host runner. |
| EditMode attempt | Unity one-shot `-runTests -testPlatform EditMode -testResults /tmp/one-roof-editmode-20260920.xml` | BLOCKED — no XML was produced. The host returned while Unity continued importing; later termination caused transient Burst `DirectoryNotFoundException` messages in the disposable worktree. |
| Visual audit | Inspected three stored Tower screenshots | PARTIAL — general cutaway composition looks coherent; it verifies none of the three requested fixes. |
| Documentation change | `git diff --check` | PASS |

## Known risks or failures

- Do not treat `/tmp/one-roof-compile-20260920.log` or `/tmp/one-roof-editmode-20260920.log` as a project failure. They contain runner-interruption artifacts (including missing `Temp/Burst` paths) and no completed result XML.
- A fresh headless run must be allowed to finish uninterrupted in a clean, isolated worktree before reporting test totals.

## Next safe action

Run the documented Unity compile, EditMode, and PlayMode commands in an external shell/CI runner that permits the first import to finish; then implement OR-704, OR-705, and OR-706 in dependency order, taking fresh Tower screenshots for each acceptance criterion.

## References

- Backlog: `OR-704`, `OR-705`, `OR-706`
- Workflow: `Docs/10_DEVELOPMENT_WORKFLOW.md`
- Evidence: `Assets/Screenshots/visual_check_overview.png`, `Assets/Screenshots/upgraded_view.png`, `Assets/Screenshots/floor5_built_view.png`, `/tmp/one-roof-compile-20260920.log`, `/tmp/one-roof-editmode-20260920.log`
