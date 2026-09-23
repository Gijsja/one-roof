# Drift & UX-gap closure — 2026-09-21

**Status:** DONE
**Owner:** OpenCode
**Source:** Backlog/codebase drift scan (2026-09-21)

## Objective

Close the four safe, scoped gaps found in the drift scan without starting M9/M10 work.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Persistence/TowerSaveData.cs` — new nullable `utilityOperations` field plus `UtilityOperationsSaveData` / `UtilityEquipmentSaveData` DTOs.
- `Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs` — aggregate-owned `ToSaveData()` / `FromSaveData()` (ARCH-004 pattern); missing payload restores fresh defaults so pre-change saves still load.
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs` — `ExportSaveData` / `RestoreFromSaveData` wire the new field; `UtilityOperations` is now `private set` like `Businesses`.
- `Assets/OneRoof/Runtime/Application/Overlays/DataOverlayKind.cs` — per-value status comments; Noise / FactionTension marked READY (OR-903).
- `Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs` — extracted pure `BuildModeStatusText()`; Manage mode now names upcoming decree levers and OR-902 instead of showing a bare mode label.
- `Planning/ROADMAP.md` — M5.3→M8 marked DONE per BACKLOG, M9/M10 marked READY, overlay registry gains a Status column plus a note that presenters are still IMGUI debug views.
- `Docs/07_DECISION_LOG.md` — ADR-060 (utility-ops save persistence).
- Tests: aggregate round-trip + missing-payload defaults (`UtilityOperationsStateTests`); sim-level worn-condition persistence incl. post-restore tick parity (`TowerSaveRoundTripTests`); Manage/Inspect/Data status-text coverage (`ModeShellBarControllerTests`).

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Static diff | `git diff --check` | PASS |
| Compile | isolated worktree `/tmp/one-roof-validation` batch compile per `Docs/10_DEVELOPMENT_WORKFLOW.md` | PASS — exit 0, `LogAssemblyErrors (0ms)`, all test assemblies built |
| EditMode | isolated worktree `-runTests -testPlatform EditMode` (2 attempts) | BLOCKED (host) — exit 0 but no result XML; runner never starts before `-quit`, matching the documented host limitation. New tests follow adjacent passing-test patterns but are unverified by execution here; verify in CI or a licensed runner before closing. |
| Linux player | `-buildLinux64Player /tmp/one-roof-drift-build/OneRoof.x86_64` | (record on run) |

## Known risks / remaining gaps (not started)

- Inspector still lacks a business/tenant card and satisfaction-weight drill-down.
- Overlay presenters remain text/debug views, not contracted visual channels.
- M9/M10 (OR-901→OR-1003) have zero implementation — correctly READY.

## Next safe action

Begin OR-901: aggregation-safe relationship graph and faction archetypes on OR-602/OR-702 inputs.
