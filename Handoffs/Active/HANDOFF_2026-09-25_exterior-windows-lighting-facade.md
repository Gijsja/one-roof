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

## 2026-09-25 visual polish follow-up

- Kept the shared world material opaque and depth writing. Exterior light cones now have their own transparent material, which is disposed with the presenter. This avoids sorting the tower, lobby, and city as transparent geometry.
- Moved generated city buildings and street apron beyond the lobby edge. City windows and lamp lights use the reliable world shader with brighter day and warm night colors.
- The placement ghost selects `OneRoof/Unlit` first so its transparent preview renders consistently. Upper downspouts anchor to the first upper floor instead of stretching from a widened ground edge; facade slices still follow each floor slab.
- Validation used an isolated copy at `/tmp/one-roof-polish-validation` because the source checkout has an active Editor. Headless compile: exit 0, no `error CS` lines (`/tmp/one-roof-polish-compile-escalated.log`). EditMode: exit 0, 623 passed, 0 failed (`/tmp/one-roof-polish-editmode-rerun.xml`). PlayMode: exit 0, 8 passed, 0 failed (`/tmp/one-roof-polish-playmode.xml`).
- Remaining check: inspect `Tower_GroundStart` in the interactive Editor at day and night, hover a room tool over valid and invalid cells, expand the ground slab, then add a floor. Headless runs do not confirm final on-screen brightness or composition.

## 2026-09-25 ground, elevator, and entrance follow-up

- Confirmed the growing translucent rectangle was `TowerAtmospherePresenter.NightTint`: its width was 14 world units and its height increased with `floorCount`. Removed the quad and retire an old instance during play. Day/night window lighting remains.
- Added topology-aligned foundation soil, stone, green edge, piers, entrance paving, step, and planter as generated ground geometry. Added framed entrance glass, jambs, lintel, light, and sign treatment outside the lobby boundary.
- Enlarged the elevator cabin and moved it forward in the cutaway, with door panels, seam, roof, sill, and status light. The details follow the car position and width for one to three cars.
- Headless validation on an isolated copy: compile exit 0 (`/tmp/one-roof-ground-assets-compile.log`); EditMode exit 0, 625 passed / 0 failed (`/tmp/one-roof-ground-assets-editmode.xml`); PlayMode exit 0, 8 passed / 0 failed (`/tmp/one-roof-ground-assets-playmode.xml`).
- Next safe action: inspect `Tower_GroundStart` at 05:13 and midday in the interactive Editor, then expand the ground slab and add a floor. The headless test suite verifies geometry and behavior but cannot judge final on-screen composition.

## 2026-09-26 earth beneath the building

- Extended the topology-aligned soil cutaway from a thin foundation strip into a 3.2-unit earth layer directly beneath the building footprint, with subtle horizontal strata. It follows ground-slab expansion and leaves the building and above-ground level unchanged.
- Varied the soil with warmer earth coloring, three contrasting strata, and offset deposits so the cutaway reads as earth rather than a flat foundation fill.
- Validation: `git diff --check` passed. Unity compile and tests were not run; this checkout contains extensive in-progress changes and the required isolated validation checkout has not been prepared.
- Next safe action: inspect the earth depth and contrast in `Tower_GroundStart`; adjust the cutaway based on the intended underground digging view.
