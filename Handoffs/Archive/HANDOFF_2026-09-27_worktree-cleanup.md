# Dirty worktree cleanup — 2026-09-27

## Disposition

- Processed and removed 16 old dirty validation worktrees. Their 393 status entries, including 131 untracked files, were preserved before removal.
- The local recovery archive is `/home/geisha/Vibecode/UnityAI/one-roof-worktree-archive/2026-09-27` (4.5 MB). Each directory contains the source commit, status list, SHA-256 checksums, a binary-capable tracked patch, and an archive of untracked files. All 16 archive checksums and reverse-patch checks passed before removal.
- Compared added code lines and untracked paths against current `main`. The distinct placement, transit, scrutiny, resident, and wardrobe hunks were older variants of current implementations; every untracked path existed in `main`. No old validation snapshot was applied over the newer code.
- The live project checkout and the two clean `phase2-acceptance` and `phase2-hotpath` worktrees were retained. No Unity process was using the removed paths.

## Removed dirty worktrees

`audit-validation`, `build-palette-alignment`, `build-placement-repair`, `ground-slab-expansion`, `phase1-stabilization`, `phase3-test-determinism`, `resident-activity-validation`, `resident-emotes-validation`, `scrutiny-build-gate`, `tower-hardening-baseline`, `tower-projection-perf-baseline`, `transit-extension-placement`, `validate-electrical-grid`, `validate-water-waste`, `one-roof-npc-validation`, and `one-roof-validation`.

## Recovery

The archive `index.json` maps each name to its original path and base commit. Restore into a new checkout at that base, apply `tracked.patch` with `git apply`, then extract `untracked.tar.gz`. Do not apply an old snapshot directly over current `main`.

This was repository housekeeping. Unity compile and tests were not run for this documentation-only change; the prior integration validation remains recorded in `HANDOFF_2026-09-27_side-branch-review.md`.
