# MAINT-001 — Review document merge and cleanup

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-19
**Updated:** 2026-09-19

## Objective

Merge the accepted, forward-compatible material from `Docs/review/` into canonical project and agent documentation, preserving completed work and decision-log history.

## Acceptance criteria

- [x] Canonical vision, contracts, UX guidance, backlog, and decision log reflect the durable review decisions.
- [x] Stale draft instructions do not overwrite OR-601 completion or collide with existing ADR-047.
- [x] The cause-chain inspector has one canonical numbered document.
- [x] Review drafts are removed after their material is merged.

## Scope and ownership

- `AGENTS.md`
- `Docs/01_GAME_VISION.md`
- `Docs/03_DATA_CONTRACTS.md`
- `Docs/04_UX_CONTRACT.md`
- `Docs/07_DECISION_LOG.md`
- `Docs/09_CAUSE_CHAIN_INSPECTOR.md`
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`
- `Docs/review/`

## State at handoff

The review drafts proposed useful tone, wellbeing, pressure, and inspector guidance, but their replacement backlog was stale: OR-601 is already DONE, and ADR-047 is already assigned to the implemented needs system.

## Changes made

- Merged optimistic-but-fragile tone, systems-only control, resident wellbeing, and Scrutiny into the vision.
- Added explanation-ready wellbeing and Scrutiny contracts.
- Added the canonical cause-chain inspector contract and linked it from UX and agent guidance.
- Added forward backlog work OR-601B through OR-605 and realigned later dependencies and acceptance criteria without compressing prior completion history.
- Aligned the M6–M10 roadmap with the canonical backlog and non-colour overlay guidance.
- Added ADR-048 through ADR-055 after the existing ADR-047.
- Removed the temporary review inbox.

## Decisions

- Review draft ADR identifiers were renumbered after the existing ADR-047; completed OR-601 remains closed.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Documentation reference check | `rg` search for canonical docs, task IDs, and ADR IDs | PASS |
| Documentation diff check | `git diff --check` | PASS |
| Unity compilation and tests | Not applicable: documentation-only change | NOT RUN |

## Known risks or failures

- The new wellbeing projections are conceptual contracts. OR-601B and OR-602 must record exact domain fields, ranges, and migrations in a follow-up ADR.

## Next safe action

Begin ART-004 or OR-601B; use `Docs/09_CAUSE_CHAIN_INSPECTOR.md` when defining wellbeing projections.

## References

- Backlog: OR-601B, OR-602, OR-604, OR-605
- Decisions: ADR-048 through ADR-055
