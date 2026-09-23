# OR-605 — Soft Specialist Roles & Training Capacity

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-20

## Objective

Let specialist roles emerge from available tower capacity and resident conditions, without permitting direct individual assignment.

## Changes made

- Added saveable, person-owned `SpecialistRoleState` with role, in-progress target, and progress.
- Added deterministic `SpecialistRoleSystem` and `SpecialistTrainingCapacity`.
- Diner/retail capacity provides Service slots; offices provide Knowledge slots; a future generic training room provides all role slots; maintenance and security spaces are recognized for the later zoning tasks.
- Residents are automatically ranked by unmet Purpose, trait affinity, then stable ID. Capacity limits both training and completed roles.
- Service and Knowledge roles improve the existing service-access contributor to wellbeing. Maintenance, Security, and Knowledge improve Scrutiny response readiness.
- Added resident inspector role/training text and persistence fields with backwards-compatible defaults.
- Added focused role capacity, autonomous progression, multiplier, and save round-trip tests.

## Validation

| Check | Result |
| --- | --- |
| Static diff | `git diff --check` — PASS |
| Unity compile / tests | NOT RUN — `unity pipeline list` reports the open Editor has no Pipeline package and no reachable server |

## Next safe action

Restore the Pipeline connection, compile, and run `OneRoof.Domain.Tests.EditMode`, then continue to OR-701 when service zoning is ready.
