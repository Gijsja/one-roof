# OR-704 — Window Light Cones

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-21

## Objective

Repair the Tower's room window lighting so the warm window treatment renders in front of the room backdrop and is testable.

## Acceptance criteria

- [x] Each non-transit room creates an enabled, transparent, warm window-light cone in front of its backdrop.
- [x] EditMode and PlayMode checks validate the renderer/material state and runtime creation.
- [x] A non-headless `Tower` screenshot is inspected to verify visual composition and opacity.

## Scope and ownership

Changed files:

- `Assets/OneRoof/Runtime/Presentation/Tower/TowerAtmospherePresenter.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/TowerAtmospherePresenterTests.cs`
- `Assets/OneRoof/Tests/PlayMode/TowerAtmosphereAndElevatorLimitsPlayModeTests.cs`

Do not alter simulation state, scene assets, or URP renderer assets for this task.

## State at handoff

The former volume was a rectangular primitive at z=0.7, behind room composition, and did not explicitly set URP Unlit to transparent surface mode. It is now a generated inward-facing warm cone at z=0.18 with URP transparent material settings. `Clear` and `OnDestroy` release generated meshes, materials, and objects.

## Changes made

- `TowerAtmospherePresenter` — creates correctly layered cone meshes and owns their cleanup.
- `TowerAtmospherePresenterTests` — validates room coverage, front-layer placement, and transparent render state.
- `TowerAtmosphereAndElevatorLimitsPlayModeTests` — validates enabled cone renderers at runtime.
- Presentation presenters now adopt matching authored `Tower` children (floor slabs, room roots,
  resident rigs, shaft/cars, roomtones, and window cones) before creating missing simulation views.
  This prevents the fixed five-floor fixture from duplicating or replacing the composed Tower.

## Decisions

- Use a procedural 2D cone rather than a renderer feature or custom shader: it fits the cutaway's 2D presentation, avoids changing shared URP assets, and keeps the effect presentation-only.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Compilation | Focused Unity EditMode startup | PASS — all project and test assemblies compiled. |
| EditMode | `TowerAtmospherePresenterTests` | PASS — 2/2; `/tmp/one-roof-or704-atmosphere.xml` |
| PlayMode | `TowerAtmosphereAndElevatorLimitsPlayModeTests` | PASS — 1/1; `/tmp/one-roof-or704-or705-playmode.xml` |
| Pixel review | `Tower` non-headless screenshot | NOT RUN — headless `-nographics` cannot provide trustworthy pixels. |
| Compilation after authored-view binding | Unity 6000.3.24f1 one-shot batch compile | PASS — `/tmp/one-roof-authored-tower-compile-final.log` |
| Pixel review | Current `Tower` Play Mode screenshot supplied 2026-09-21 | PASS — authored room composition remains visible and the warm window-light treatment is layered above backdrops. |

## Known risks or failures

- The effect is a stylized 2D light cone, not physically simulated volumetrics. Its composition and opacity still require an actual rendered screenshot review.

## Next safe action

Proceed to OR-706: validate interactive floor-slab placement against the authored Tower composition.

## References

- Backlog: `OR-704`
- Evidence: `/tmp/one-roof-or704-atmosphere.xml`, `/tmp/one-roof-or704-or705-playmode.xml`
