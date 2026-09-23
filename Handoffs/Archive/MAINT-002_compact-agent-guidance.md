# MAINT-002 — Compact agent guidance

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-19
**Updated:** 2026-09-19

## Objective

Reduce repository agent guidance to durable, non-conflicting principles while documenting the supported Unity workflow.

## Changes made

- Replaced the long rule list in `AGENTS.md` with compact project, boundary, and completion guidance.
- Recorded Unity 6000.3 LTS (`6000.3.24f1`), Unity CLI, and Unity Pipeline as the preferred live-Editor workflow.
- Reduced `CLAUDE.md` and added `GEMINI.md` as pointers to the shared guide, preventing duplicate and divergent instructions.

## Validation

| Check | Result |
| --- | --- |
| Version source | `ProjectSettings/ProjectVersion.txt` confirms `6000.3.24f1` |
| Documentation diff | `git diff --check` passed |
| Unity compilation and tests | NOT RUN — documentation-only change |

## Next safe action

Use `unity pipeline list` before the next live Unity task, then follow the smallest relevant edit → recompile → test loop.
