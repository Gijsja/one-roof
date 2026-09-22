# ARCH-006 — Deepen Tower session and Data overlays

**Status:** DONE
**Date:** 2026-09-22

## Scope

- `TowerSimulationSession` creates its simulation through owned fixtures; callers use commands and read-only projections rather than mutable Domain aggregates. Congestion and transit projections use a private version counter. `TowerTopologyProjection` copies immutable rooms/slabs only when structural commands change the tower.
- Placement, tower views, HUD, and inspectors use this seam. Resident inspection captures copied facts, not a mutable `PersonRecord`; grid placement's hover cache keys on topology-projection identity instead of just room/floor counts.
- `TowerDataOverlays` constructs the seven typed diagnostic projections behind one module; the tower coordinator and inspector share that module. Existing projection types and cause-chain copy remain intact.
- Added focused regression tests for versioned cache reuse, structural projection stability, rejected-command cache preservation, and resident inspection snapshot stability across ticks. Existing presentation and acceptance tests were migrated to observable session outcomes.
- `CONTEXT.md` records Tower, Steward, Resident, and cause-chain; ADR-069/070 record the architecture trade-offs.

## Validation

- Isolated worktree: `../one-roof-architecture-validation` (removed after validation).
- Compile: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath <worktree> -logFile /tmp/opencode/one-roof-architecture-compile2.log` — exit 0.
- EditMode: `timeout 1500 .../Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testResults /tmp/opencode/one-roof-architecture-editmode-complete.xml -logFile /tmp/opencode/one-roof-architecture-editmode-complete.log` — exit 0, 442 passed, 0 failed.
- PlayMode: `timeout 1500 .../Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform PlayMode -testResults /tmp/opencode/one-roof-architecture-playmode-complete.xml -logFile /tmp/opencode/one-roof-architecture-playmode-complete.log` — exit 0, 5 passed, 0 failed.

## Risks and next safe action

The immutable topology projection still carries immutable Domain `Room` values while existing furnishing presenters use room metadata. Continue to consume the session projection; if furnishings move to a dedicated presentation model later, replace those room values at the same seam. Next: proceed with the next planned beta-slice task after validating both suites.
