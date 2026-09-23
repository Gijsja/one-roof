# Handoff — Front sheet sliced: 49 wardrobe parts live on the Spine rig (2026-09-21)

Follows `HANDOFF_2026-09-21_wardrobe-variants-spine-diversity.md` (palette phase). This pass
completes the approved next step: real transparent sprites on the rig.

## Pipeline (all scripted, reproducible)

1. **Grid measurement** — gradient-projection analysis of `frontview.png` proved a uniform designer
   grid: 9-col pitch 124.8 (heads), 8-col span x=[390,1512] pitch 140.25. Verified with overlay
   render before cutting. Trousers follow the 9-col grid; torso-low row holds 5 shirts in 8-col
   cols 0–4; alt-heads sit in cols 0–2 above the torso band.
2. **Per-cell rembg (u2net)** — 50 cells → whole-sheet models fail on paperdoll layouts, single-cell
   isolation works. Largest-N alpha-component filtering (N=2 for shoe pairs) kills neighbor
   intrusion; transparent trim; cap 256 px, never upscale. One fix applied: white-sneaker pair was
   split across two manual cells → re-extracted as one cell, boots renamed 07→06.
3. **Result: 49 parts** (8 headgear, 12 heads, 13 uppers, 9 lowers, 7 footwear pairs), 1.4 MB under
   `Assets/OneRoof/Runtime/Content/Resources/Residents/Wardrobe/Front/`, each with single-sprite
   `.meta` (custom pivot bottom-center, 512 PPU, Bilinear). Spec:
   `Art/SourceArt/Proposed/resident-wardrobe-parts-v1.spec.json` (`validated-asset-candidate`).

## Runtime wiring

- `NpcWardrobeVariant` gains per-layer part paths + `BareLegs` (casual shorts keep skin legs);
  all 12 variants mapped (12 distinct heads, profession-matched garments).
- `WardrobePartCatalog` (new, Presentation): cached `Resources` loads, per-layer target sizes,
  length-aware lower anchors (trousers → ankle, shorts → hip), aspect-preserving `FitSlot`.
- `NpcSkeletalHierarchy.ApplyWardrobe`: part present → true-color sprite + auto-fit; else palette
  swatch fallback (headless-safe). Photo-covered procedural boxes (head, torso) hide; hair swatch
  hides under photo heads. Fixed one self-inflicted stomp (diversity pass recolored part slots).
- ADR-063 recorded. Palette-only behavior preserved wherever parts are absent.

## Validation (isolated worktree `/home/geisha/Vibecode/UnityAI/one-roof-validation`, 6000.3.24f1)

- Compile `-batchmode -nographics -quit`: **EXIT 0**.
- EditMode (no `-quit` with `-runTests`): **384/387**. All 12 wardrobe/part tests pass, incl.
  `Parts_AllVariantReferencesResolveToImportedSprites` (49 sprites, pivots verified) and hierarchy
  photo-application tests. 3 remaining failures are **pre-existing on clean HEAD** (palette-UI
  pointer tests, files untouched by this change).
- Visual: 6-variant paperdoll lineup composited from shipped parts — professions read clearly,
  cutouts clean (incl. astro-helmet visor, white sneakers).
- XML: `/tmp/opencode/one-roof-editmode-parts3.xml`.
- **Pipeline CLI check** (`unity test`, direct editor runner — not the forbidden pipeline
  package): filtered `--filter Wardrobe` → exit 0, 12/12 pass
  (`/tmp/opencode/cli-wardrobe.xml`); full EditMode → exit 8 (`TESTS_FAILED` semantic),
  384/387 with only the same 3 pre-existing failures (`/tmp/opencode/cli-editmode-full.xml`).

## Next safe actions

- Side/back sheets stay reference; slice only when a facing-swap pass needs them.
- Optional polish: split shoe pairs into L/R foot renderers; arm-hole blending on sleeveless uppers.
- Do NOT commit the other session's in-flight palette/utility-ops modifications with this work.
