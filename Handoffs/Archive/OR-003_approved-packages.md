# OR-003 — Approved package installation

**Status:** DONE
**Owner:** Codex session
**Started:** 2026-09-15
**Updated:** 2026-09-15

## Objective

Install the packages approved in ADR-007 through Unity's Package Manager API.

## Acceptance criteria

- [x] The manifest resolves with no package errors.

## Scope and ownership

Expected files/directories:

- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`
- `Handoffs/Active/OR-003_approved-packages.md`

Do not modify:

- User-authored scene and project-setting changes.

## State at handoff

Unity Package Manager resolved these direct dependencies for Unity `6000.3.24f1`:

- `com.unity.addressables` `4.0.1`
- `com.unity.2d.animation` `13.0.6`

The temporary Editor installer used to call the Package Manager API was removed after resolution. The user-created `Assets/Scenes/one-roof.unity` and related Editor settings remain uncommitted and unstaged.

## Changes made

- `Packages/manifest.json` — Added the two approved direct package dependencies.
- `Packages/packages-lock.json` — Captured their resolved dependency graph.
- `Docs/07_DECISION_LOG.md` — Recorded the accepted versions as ADR-009.
- `Planning/BACKLOG.md` — Marked OR-003 done and OR-004 ready.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Package installation | Unity `Client.AddAndRemove` Package Manager API | PASS |
| Resolve and compile | `Unity -batchmode -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof -logFile -` | PASS — package registration and script compilation completed |
| Edit Mode | Not run | NOT RUN — no runtime behavior changed |
| Play Mode | Not run | NOT RUN — no runtime behavior changed |

## Known risks or failures

- The GUI Editor's Pipeline endpoint remains unreachable, which blocks live Codex Editor control but does not block normal Unity use or headless validation.

## Next safe action

Complete OR-004: define the runtime and test assembly boundaries, then verify the dependency-boundary tests compile.

## References

- Backlog: OR-003
- Decisions: ADR-007, ADR-009
- Evidence: `Packages/manifest.json`, `Packages/packages-lock.json`
