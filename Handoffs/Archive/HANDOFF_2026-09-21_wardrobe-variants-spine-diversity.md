# Handoff — Modular wardrobe sheets → Spine rig diversity (2026-09-21)

## What was done

The 5 new paperdoll sheets (`frontview`, `frontview1`, `sideview`, `sideview1`, `backview1`,
1536×1024, opaque gradient backgrounds) were processed onto the Spine resident model for in-game
diversity **without slicing raw pixels**: the sheets fail the Docs/05 transparency gate, so direct
slicing would bake gradient halos into the 0.58 m cutaway cast. Their diversity value ships as data.

- `Art/SourceArt/Proposed/resident-wardrobe-modular-v1.spec.json` (new) — documents all 5 views,
  row layouts, 12 profession columns, rig layer mapping, and the blocked/allowed runtime use.
  Status is `proposed-source-art`, never validated-candidate.
- `Assets/OneRoof/Runtime/Content/NpcWardrobeVariantCatalog.cs` (+`.meta`, new) — 12 immutable
  profession variants (firefighter, construction, chef, medic, security, pilot, corporate, police,
  service, maintenance, astronaut, casual) with per-layer hex palettes, 5 skin tones, ±5 % stature.
  Pure Content, no UnityEngine dependency. Content IDs untouched (ADR-002/061).
- `Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs` (modified) — `Initialize`
  picks a deterministic variant (`index % 12`), exposes `WardrobeVariantKey`, resolves all 8 wardrobe
  slot colors through the variant palette, tints base anatomy (skin → head/hands, upper → torso,
  lower → legs, footwear → feet), applies idempotent stature scale. Hash-hue fallback preserved.
- Tests (new): `NpcWardrobeVariantCatalogTests` (5) + `WardrobeVariantDiversityTests` (2).
- `Docs/07_DECISION_LOG.md` — ADR-062 recorded.

## Validation (isolated worktree, Unity 6000.3.24f1)

Synced new/changed files into `/home/geisha/Vibecode/UnityAI/one-roof-validation` (detached HEAD
a5e9992); main checkout untouched.

- Compile: `-batchmode -nographics -quit -projectPath ...-validation` → **EXIT 0**, no errors.
- EditMode (note: do NOT pass `-quit` with `-runTests`; it kills the run before the report exists):
  `-batchmode -nographics -projectPath ... -runTests -testPlatform EditMode -testResults ...`
  → **380/383 pass**. All 8 wardrobe tests pass. 3 failures are **pre-existing on clean HEAD**
  (verified: failing files clean in validation worktree): 2× `GridPlacementControllerTests`
  pointer-over-UI palette-region, 1× `ModeShellBarControllerTests` diner-palette reopen.
  Unrelated to this change (build-palette UI work in flight in main checkout).
- Result XML: `/tmp/opencode/one-roof-editmode.xml`; logs: `/tmp/opencode/one-roof-*.log`.

## Risks / notes

- Raw sheets intentionally NOT imported to `Resources/Residents` (opaque backgrounds, photorealistic
  scale mismatch, ~12 MB bloat). No `.meta`/Addressables changes; AssetLab residents gate unaffected.
- A concurrent agent session is active in this repo (uncommitted palette/utility-ops work in main
  checkout; another Unity run collided mid-validation). My files are untracked + 2 tracked modifies
  (`NpcSkeletalHierarchy.cs`, decision log) — no interference, but coordinate before commit.

## Next safe action

Transparent part slicing (accessory/hair/upper/lower/footwear PNGs, ≤256 px, bottom-center pivot,
512 PPU) after artist background-removal cleanup of the 5 sheets; then point variant slots at sliced
sprites and flip the spec to `validated-asset-candidate`.
