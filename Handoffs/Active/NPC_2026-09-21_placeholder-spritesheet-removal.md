# Handoff — Remove composite placeholder resident sprites (Spine rig canonical)

## What changed (main checkout, uncommitted)
- Deleted 7 runtime PNGs + 7 `.meta` via `git rm`: `Assets/OneRoof/Runtime/Content/Resources/Residents/resident_{01..06}_*.png` + `resident_spritesheet.png`.
- `Assets/OneRoof/Runtime/Content/NpcContentRegistry.cs`: all 6 `resourcePath` → `""` (content IDs immutable, unchanged); doc comment records Spine-only path.
- `Assets/OneRoof/Runtime/Presentation/Population/ResidentSpriteCatalog.cs`: skips `Resources.Load` on empty path, resolves procedural bottom-center-pivot fallback.
- `Assets/OneRoof/Editor/AssetLab/AssetLabValidator.cs`: `ValidateResidents` no longer requires a Resources asset; still enforces ID uniqueness, interaction anchors, rig dimensions.
- `Assets/AddressableAssetsData/AssetGroups/residents.asset`: 7 dangling entries removed (hand-edit; next `ConfigureAddressables` pass should confirm).
- `Docs/07_DECISION_LOG.md`: ADR-061 appended.
- `Art/SourceArt/Proposed/` untouched (proposed source art stays as reference).

## Validation (isolated worktree `../one-roof-npc-validation` @ a5e9992 + patch, Unity 6000.3.24f1)- Compile: `-batchmode -nographics -quit` → exit 0.
- EditMode full suite → 376 total, **373 passed, 3 failed** (`one-roof-npc-editmode.xml`):
  - `GridPlacementControllerTests.IsPointerOverUI_*` (2), `ModeShellBarControllerTests...ReopensPalette...` (1).
  - Same 3 fail on pristine HEAD (stashed-change baseline runs, `one-roof-head-baseline{1,2}.xml`) with zero symbol coupling to this diff → **pre-existing, unrelated**.
  - All 32 NPC tests pass: `NpcSkeletalRigTests` 8/8, `AssetLabValidatorTests`, `ResidentSpriteCatalogTests` 4/4, `NpcPopulationPresenterTests`, `NpcViewPoolTests`, `TowerResidentPresenterTests`.
- Logs/results: `/home/geisha/Vibecode/UnityAI/one-roof-npc-{compile,editmode,editmode3,head-baseline1,head-baseline2}.log`, `one-roof-npc-editmode.xml`, `one-roof-head-baseline{1,2}.xml`.
- Note: `-quit` combined with `-runTests` quits before executing tests in this Editor version (observed twice); runs without `-quit` (plus shell `timeout`) execute and self-terminate. Docs/10 shows `-quit` — may need an amendment.

## Risks / open items
- Parallel wardrobe work is active in the same checkout (untracked `NpcWardrobeVariantCatalog`, `WardrobeVariantDiversityTests`, modified `NpcSkeletalHierarchy`, new modular view PNGs, `D Art/SourceArt/Proposed/*.png` unstaged deletions). Verified compatible (no shared symbols with this diff; hierarchy still consumes `ResidentSpriteCatalog`), but coordinate before commit.
- `WardrobeVariantDiversityTests.cs` (foreign, untracked) is missing `using OneRoof.Presentation.Population;` — it broke compile in the other scratch worktree (`../one-roof-validation`, which foreign files leaked into; left untouched). Not my file; flag to its owner.
- `residents.asset` was hand-edited (preferred tooling is `ConfigureAddressables`); entries removed correspond 1:1 to deleted GUIDs.

## Next safe action
- Review `git status`/`git diff HEAD` in main checkout, then commit this set (or let the wardrobe work land first and rebase — registry/catalog touchpoints are stable either way).

## Post-validation notes (23:25)
- Main HEAD moved a5e9992 → 6ad9263 (utilities-commit, touches only `ElectricalGridState`/`WaterWasteNetworkState` + their tests — no overlap with this diff).
- The staged PNG deletions were transiently restored by another session's commit flow; re-applied with `git rm` and re-verified: all 4 code files are byte-identical to the validated worktree copies, deletions identical → validation result above transfers 1:1.
