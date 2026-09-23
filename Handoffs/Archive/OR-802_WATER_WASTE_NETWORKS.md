# OR-802 — Plumbing Water & Gravity Waste Networks

## Completed

- Added buildable ground water pumps, vertical water risers, booster pumps, waste chutes, and ground waste-collection rooms to the catalog and Build palette.
- Added the pure-C# `WaterWasteNetworkState` evaluator. It derives water demand, pressure, riser continuity, booster pressure resets, gravity-chute continuity, and collection availability directly from topology.
- Exposes immutable per-floor projections with explicit repairable causes: missing ground pump, disconnected riser, low pressure, missing waste collection, and disconnected chute.
- Added ground-only command validation for water pumps and waste collection, plus an application-facing `WaterWasteNetworkProjection()` on `TowerSimulationSession`.
- Added deterministic EditMode coverage for normal supply/collection, pressure boosters, disconnected vertical links, missing sources, and ground-only construction.

## Commits

- `1e7201b feat(utilities): add water and waste networks`

## Validation

- `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/.codex/worktrees/validate-water-waste/one-roof -logFile /tmp/one-roof-water-waste-compile.log` completed with exit code 0 and no compiler errors.
- The required EditMode invocation compiled the project successfully but produced no test XML because Unity 6000.3.24f1 honored `-quit` before starting the test runner, matching the known OR-801 workflow limitation. The test-run log contains no compiler errors.

## Risks / next action

- OR-803 should consume `WaterWasteNetworkProjection()` beside the electrical projection for Overlay 8, then apply degradation and technician response as a separate mutable operational layer.
