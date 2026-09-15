# OR-001 — Platform and foundation decisions

**Status:** DONE
**Owner:** Codex session
**Started:** 2026-09-15
**Updated:** 2026-09-15

## Objective

Confirm the platform, monetization, Unity version, and version-control system.

## Acceptance criteria

- [x] Choices are recorded in the decision log.

## Scope and ownership

Expected files/directories:

- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`
- `Handoffs/Active/OR-001_platform-and-foundation.md`

Do not modify:

- Runtime Unity assets, packages, or scenes.

## State at handoff

The project foundation is on `setup/unity-project` and has been pushed to GitHub. The project uses Unity `6000.3.24f1` with URP. No Unity Editor with the Pipeline package was connected during this task.

## Changes made

- `Docs/07_DECISION_LOG.md` — Added ADR-008 to record the confirmed release and tooling choices.
- `Planning/BACKLOG.md` — Marked OR-001 done and unblocked OR-002.
- `Handoffs/Active/OR-001_platform-and-foundation.md` — Recorded scope, validation, and next action.

## Decisions

- Premium single-player desktop is the initial release target, as established in `Docs/01_GAME_VISION.md`.
- Unity `6000.3.24f1` with URP is the exact project version, read from `ProjectSettings/ProjectVersion.txt`.
- Git hosted on GitHub is the VCS, using `Gijsja/one-roof` and the `setup/unity-project` branch.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Project version | `sed -n '1,220p' ProjectSettings/ProjectVersion.txt` | PASS — `6000.3.24f1` |
| Repository | `git status --short --branch` | PASS — setup branch tracks `origin/setup/unity-project` |
| Editor connection | `unity status --format json` | NOT RUN — no Unity Editor instance with the Pipeline package is connected |
| Edit Mode | Not run | NOT RUN — no connected Editor |
| Play Mode | Not run | NOT RUN — no connected Editor |

## Known risks or failures

- A connected Unity Editor still needs the Pipeline package before live Editor control and compile/test validation are available.

## Next safe action

Open this project in Unity, install/confirm the Pipeline package, then complete OR-002 by validating the project imports cleanly.

## References

- Backlog: OR-001
- Decisions: ADR-008
- Evidence: `ProjectSettings/ProjectVersion.txt`, `Docs/01_GAME_VISION.md`
