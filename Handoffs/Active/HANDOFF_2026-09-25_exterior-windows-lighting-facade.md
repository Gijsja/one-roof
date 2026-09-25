# Exterior Architectural Presentation — Windows, Lighting & Facade Liveliness

## Delivered

### M1: Exterior Window Cutouts & Frames (F1)
- Per-floor window frames, sills, and recessed glass quads on both left and right facades
- `_leftWindowFrames`, `_leftWindowGlasses`, `_leftWindowSills`, `_rightWindow*` lists track all generated GOs
- Quads named: `Left Window Frame {f}`, `Left Window Sill {f}`, `Left Window Glass {f}` (and matching Right)
- Glass panes recessed at Z=0.00m behind cladding face at Z=-0.04m

### M1: Day/Night Emissive Glow (F2)
- `UpdateWindowLighting(DayPhase)` smoothly lerps glass color between `WindowDayColor` (cool blue) and `WindowNightColor` (warm amber)
- **Fixed dawn discontinuity**: Previous formula snapped 0.5→0.0 at 06:00 boundary. Replaced with smooth ramp: dawn 5.5h→6.5h, dusk 19.5h→20.5h (mirrors `TowerAtmospherePresenter.NightFactor`)
- Same fix applied to `OutsideCityPresenter.UpdateLighting` (same broken formula fixed)

### M1: Volumetric Light Cones (F3)
- Per-floor trapezoid mesh cones projecting outward from glass panes; vertex alpha 0.5→0.0 gradient at tips
- `UpdateOrCreateLightCone()` builds/updates trapezoid mesh per-floor at Z=-0.20m
- Cones tracked in `_volumetricCones` list and `_windowLightingEntries`

### M2: Rooftop Infrastructure (F4-F9)
- `BuildRooftopFixtures()`: Elevator penthouse housing, HVAC chiller + two fan cowlings (±0.20m offset), communications mast (2.40m), mast cross arm (0.40m), aviation warning beacon at Z=-0.45m
- `BuildSetbackTerraces()`: Per-floor stepback detection → `Terrace Slab R/L {f}` + `Terrace Railing R/L {f}`
- `CalculateStageBounds()`: `OutsideStageBounds` with left/right exterior zones, rooftop zone, interior enclosure

### M3 Partial: Facade Dressing (F11, F13)
- Left/right downspouts: `Left Facade/Left Downspout`, `Right Facade/Right Downspout`
- Lobby entrance portal frame + cantilever canopy (1.40m×0.14m) on right facade

### Single-Material Architecture
- Removed `_volumetricMaterial` — ALL MeshRenderers share `_worldMaterial`
- `Initialize()` configures `_worldMaterial` for SrcAlpha/OneMinusSrcAlpha + Transparent queue
- Cones fade via vertex color alpha gradient; walls remain opaque via alpha=1.0 in color blocks
- Passes `T1_F17_SingleDrawCallMaterialSharing_AllStructuralQuadsShareWorldMaterial`

### New E2E Test Suite (68 tests, 4 tiers)
- `Assets/OneRoof/Tests/EditMode/Presentation/ExteriorArchitecturalPresentationE2ETests.cs`
- Added `using OneRoof.Application.Transit;` to fix CS0246 (`ElevatorProjection`, `TransitResidentProjection`)

## Validation Evidence

- **Headless compile**: exit code 0, zero error CS lines
  - Log: `.agents/ext-validate/compile.log`
- **EditMode tests**: **621 passed / 0 failed / 0 skipped**
  - Results: `.agents/ext-validate/editmode-results.xml`, duration 17.9s
- **PlayMode tests**: `.agents/ext-validate/playmode-results.xml` (pending at handoff time)

## Files Changed

| File | Change |
|---|---|
| `BuildingExteriorPresenter.cs` | Remove `_volumetricMaterial`; alpha-blend `_worldMaterial`; smooth dawn/dusk ramp; remove `glassMat` from windows |
| `OutsideCityPresenter.cs` | Same dawn/dusk discontinuity fix |
| `ExteriorArchitecturalPresentationE2ETests.cs` | NEW: 68 E2E tests; added `using OneRoof.Application.Transit` |

## Next Safe Action

- Confirm PlayMode results (`.agents/ext-validate/playmode-results.xml`)
- Visual review in Unity Editor: window frames/glass, smooth day/night glow, volumetric cones, rooftop fixtures, paralax skyline
- M3 full facade dressing (fire escapes, AC units, exterior signage) as next task
