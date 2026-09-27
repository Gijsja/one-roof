# Side-branch review and Pipeline MCP note

## Branch review against `main` at `bffbc84`

- `codex/audit-remediation-only` (`56bb0cc`): superseded for this integration. Current `HouseholdRecord.SpendOnService` uses `AvailableServiceSpend`, which includes cash and cap checks; rent burden uses the later budget and income model. The inspected audit additions are present or superseded in current code. Trial cherry-pick produced conflicts in later economy, population, and presentation paths; no old implementation was copied over them.
- `codex/phase2-hotpath` (`afa8a13`): not safe to cherry-pick. The old `ScheduleTripGenerator` removes later outside-work retry and committed-purpose handling. Its `BuildingTopology` replaces the current content-prefix query API, and its utility equipment sync replaces the current `RoomsVersion` cache. The resident presenter already uses `NpcViewPool` and camera culling, though the beta performance budget remains unproven. A trial application produced duplicate `TowerSimulationSession.Version` and conflicts across the runtime. The branch is retained for future targeted performance work and measurement.
- `codex/phase3-stability` (`9d92203`): ported the still-applicable PlayMode expansion test change. Both expansion tests now pause autonomous ticks; the full lifecycle test asserts exact manual tick progress and cleans up its objects in `finally`. Other test adjustments target older fixtures; the current full suite was green before this review.

## Pipeline discovery

`unity status` and `unity command` returned `STATUS_NO_INSTANCES`/`COMMAND_FAILED` while the Editor's Pipeline Server was running. Direct `mcp__unity__editor_status`, `console_status`, scene, and build-settings tools connected to Unity 6000.3.24f1 at the project path and showed `Tower_GroundStart` in Play Mode, no compilation failure, and zero Console errors. This is documented in `AGENTS.md` for future agents.

## Validation

- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-side-branch-integration`.
- Unity 6000.3.24f1 headless compile: exit 0; `/tmp/one-roof-side-branch-compile.log`.
- Full PlayMode suite: exit 0, 8 passed, 0 failed, 0 skipped; `/tmp/one-roof-side-branch-playmode.xml`.
- Full EditMode suite: exit 0, 673 passed, 0 failed, 0 skipped; `/tmp/one-roof-side-branch-editmode.xml`.
- `git diff --check`: exit 0 before commit.
- No player build or game publication was performed.
