# OR-004 — Assembly and test boundaries

**Status:** DONE
**Owner:** Codex session
**Started:** 2026-09-15
**Updated:** 2026-09-15

## Objective

Create runtime and test assemblies that enforce the project's dependency boundaries.

## Acceptance criteria

- [x] Dependency-boundary tests compile.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/`
- `Assets/OneRoof/Editor/`
- `Assets/OneRoof/Tests/`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`
- `Handoffs/Active/OR-004_assembly-boundaries.md`

Do not modify:

- User-authored scene and project-setting changes.

## State at handoff

Runtime concerns are separated into `OneRoof.Domain`, `OneRoof.Application`, `OneRoof.Infrastructure`, `OneRoof.Presentation`, `OneRoof.UI`, `OneRoof.Content`, and `OneRoof.Editor` assemblies. Domain and Application do not reference UnityEngine. Matching Edit Mode test assemblies and a Play Mode test assembly are present.

## Changes made

- `Assets/OneRoof/Runtime/` — Added the seven named runtime assemblies and source markers.
- `Assets/OneRoof/Editor/` — Added the editor-only assembly and source marker.
- `Assets/OneRoof/Tests/` — Added matching test assembly definitions and a pure-domain UnityEngine boundary test.
- `Docs/07_DECISION_LOG.md` — Added ADR-010.
- `Planning/BACKLOG.md` — Marked OR-004 done and OR-005 ready.

## Decisions

- Domain and Application use `noEngineReferences` so simulation and use cases cannot gain a UnityEngine dependency accidentally.
- Presentation and UI depend on Application, rather than on mutable Domain state.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Editor availability | `unity status --project-path /home/geisha/Vibecode/UnityAI/one-roof --format json` | PASS — no live Editor remained after user closed it |
| Edit Mode | `unity test /home/geisha/Vibecode/UnityAI/one-roof --editor-version 6000.3.24f1 --mode EditMode --output /tmp/one-roof-or004-editmode.xml --timeout 180 --format json` | PASS — 2 tests passed, 0 failed |
| Play Mode | Not run | NOT RUN — this task adds no runtime behavior |

## Known risks or failures

- The GUI Editor Pipeline endpoint has previously been unreachable despite the package being installed. Headless Unity CLI validation succeeded after the Editor was closed.

## Next safe action

Complete OR-005: add GitHub Actions compile plus Edit and Play Mode test coverage for a fresh checkout.

## References

- Backlog: OR-004
- Decisions: ADR-010
- Evidence: `Assets/OneRoof/Runtime/`, `Assets/OneRoof/Tests/`, `/tmp/one-roof-or004-editmode.xml`
