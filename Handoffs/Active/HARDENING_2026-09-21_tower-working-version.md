# Tower working-version consolidation and optimization

**Status:** DONE
**Owner:** Codex
**Updated:** 2026-09-21

## Objective

Consolidate and verify the existing Tower slice as a working version in which building and the core game loop are usable, then remove a measured presentation hot-path allocation without changing simulation authority or behavior.

## Outcome

- Verified the existing five-floor Tower, fifty-resident loop, building placement, expansion, leasing, elevator intervention, overlays, lighting, and three-car constraint through the complete EditMode and PlayMode suites.
- Cached the immutable elevator-congestion projection per simulation tick in `TowerSimulationSession`.
- Centralized projection-cache invalidation after simulation ticks, resets, and accepted commands, including commands that mutate state without advancing the clock.
- Added regression coverage for same-tick reuse, tick invalidation, and same-tick command invalidation.

## Performance evidence

The same Unity EditMode measurement retained 1,000 same-tick congestion projection results so the generated object graph could be compared under equivalent conditions:

- Before: 188,416 retained managed bytes.
- After: 0 retained managed bytes.

This targets the HUD and overlay path, where IMGUI and presentation code can request the same projection multiple times between simulation ticks. No frame-time or GPU claim is made.

## Changed files

- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Assets/OneRoof/Tests/EditMode/Application/TowerSimulationSessionSpatialTests.cs`
- `Docs/AI/UnityProjectContext.md`
- `Handoffs/Active/HARDENING_2026-09-21_tower-working-version.md`

## Validation

All Unity execution used isolated worktrees and Unity 6000.3.24f1.

| Check | Result | Evidence |
| --- | --- | --- |
| Clean baseline compile | PASS | `/tmp/one-roof-tower-hardening-baseline-compile.log` |
| Baseline EditMode | PASS — 356/356 | `/tmp/one-roof-tower-hardening-baseline-editmode.xml` |
| Baseline PlayMode | PASS — 5/5 | `/tmp/one-roof-tower-hardening-baseline-playmode.xml` |
| Focused cache tests | PASS — 6/6 | `/tmp/one-roof-tower-hardening-targeted-editmode.xml` |
| Final EditMode | PASS — 360/360 | `/tmp/one-roof-tower-hardening-final-editmode.xml` |
| Final PlayMode | PASS — 5/5 | `/tmp/one-roof-tower-hardening-final-playmode.xml` |
| Standalone Linux build | PASS | `/tmp/one-roof-tower-hardening-final-build/OneRoof.x86_64` |
| Build provenance | Produced | `/tmp/one-roof-tower-hardening-final-build/OneRoof.provenance.json` |
| Static diff | PASS | `git diff --check` |

The final PlayMode suite includes interactive floor-slab preview/confirmation, full expansion and leasing, elevator capacity intervention, resident delivery, volumetric window lighting, and the three-car shaft constraint.

## Risks and next safe action

- The first clean Linux player build took about 16 minutes because installed AI inference/Sentis shaders compiled a large variant set. This is a package/build-pipeline cost, not a runtime regression from this change. Audit whether the AI packages are required before removing or stripping them.
- Headless PlayMode proves interaction and scene composition behavior but is not a human visual/usability review. The next safe action is a short desktop playtest of the built player covering Build → Inspect → Data → response feedback.
- Unrelated existing changes under `.agents/`, `.codex/`, `Packages/`, `ProjectSettings/`, `.plastic/`, and generated `Assets/OneRoof/Runtime/Library/` were preserved and not modified by this task.
