# 24/7 NPC daily cycle — resolve

**Status:** DONE (uncommitted working-tree change, validated in isolated worktree then removed)
**Owner:** OpenCode
**Started:** 2026-09-22
**Updated:** 2026-09-22

## Objective

Close the 24/7 loop: the modulo schedule repeated, but stranded commuters missed
boundaries, satisfied eaters sat in the diner mid-block, overnight Hygiene decayed
with no recovery, Hygiene arrivals landed Idle instead of resting, and all
Food/Leisure demand pinned a single room.

## Changed files

- `Assets/OneRoof/Runtime/Domain/Population/DynamicScheduleArbitrator.cs` — mid-block
  meal-complete returns + stranded Sleeping/Working/Leisure catch-up; Idle stays
  silent mid-block (ADR-020 statelessness preserved).
- `Assets/OneRoof/Runtime/Domain/Population/ResidentNeedsSystem.cs` — Hygiene recovers
  slowly while Sleeping at home; still decays when sleeping away.
- `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs` — public
  `ReconcileArrivalActivity` (Sleep-at-home→Sleeping, Work-at-work→Working);
  applied on trip completion, local-trip completion, and same-room generator path.
- `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs` — deterministic
  person-ID spread of Food/Leisure across all matching diners/lobbies.
- `Assets/OneRoof/Tests/EditMode/Domain/DailyCycleTests.cs` (+`.meta`) — 9 tests.
- `Docs/07_DECISION_LOG.md` — ADR-067 appended.

Untouched: other session's in-progress Scrutiny/placement/UI files left as-is.

## Validation (isolated worktree `../one-roof-247-validation`, since removed)

| Check | Result |
| --- | --- |
| Unity 6000.3.24f1 batch compile | PASS exit 0, zero `error CS` (`/tmp/opencode/247-compile.log`) |
| New `DailyCycleTests` (9) | PASS 9/9 (`/tmp/opencode/247-final-check.xml`) |
| Full EditMode (406) | 403 pass, 3 fail — all 3 proven pre-existing on pristine HEAD via `git stash -u` + `ControllerTests` baseline rerun (`/tmp/opencode/247-baseline.xml`): `GridPlacementControllerTests.IsPointerOverUI_*` (2) and `ModeShellBarControllerTests.Controller_OnBuildModeRequested_ReopensPaletteByClearingExistingToolSelection` (1). Unrelated Presentation/UI areas. |

## Findings

- Two test-setup corrections during validation: the five-floor diner doubles as the
  workplace (meal-complete resolves in place there), and a single car cannot drain
  synchronized Eat-block demand — the stability test asserts liveness + bounded
  needs instead of a full drain.
- Thundering-herd note: all 50 residents share near-identical block edges, so Eat
  transitions submit ~50 simultaneous trips. The sim survives (queues drain,
  deliveries grow day over day), but staggered shifts remain the real fix for
  OR-1003 scale.

## Next safe action

Commit this change separately from the in-progress Scrutiny work, then consider
staggered shift templates (day/night, weekend Leisure) as a follow-up task.
