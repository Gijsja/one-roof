# Original User Request

## 2026-09-24T18:53:08Z

Use a full team of agents.

Solve missing exterior windows and exterior volumetric lighting, and enrich the tower exterior facade, skyline, and ambient environment with additive architectural details using existing catalogs and procedural sprite/quad generation.

Working directory: /home/geisha/Vibecode/UnityAI/one-roof
Integrity mode: development

## Requirements

### R1. Exterior Windows & Volumetric Lighting
Restore and enhance exterior building windows and volumetric illumination:
- Ensure exterior wall window cutouts and frames render with proper depth, glass reflectivity/transparency, and night-time emissive glow tied to the day/night cycle.
- Fix exterior volumetric light cones (sunlight shafts during the day, warm interior spill at night) so they cast cleanly into the surrounding air without depth clipping or shader fallback artifacts under URP 17.3 LTS.

### R2. Rooftop Infrastructure & Skyline Profile
Enrich the building's roof silhouette with realistic skyscraper infrastructure:
- Place procedural or catalog-backed rooftop assets: HVAC chillers/cooling towers, water storage tanks, satellite dishes, and antenna arrays.
- Ensure the aviation hazard beacons blink or illuminate accurately against the skyline atmosphere.

### R3. Facade Architectural Dressing & Ambient City Liveliness
Make the exterior facade feel active, weathered, and alive:
- Add exterior architectural fixtures: modular fire escapes, vertical utility conduits/piping, exterior window AC units, and building signage or subtle neon accents.
- Enhance the ambient outside environment (e.g., drifting low-altitude cloud/smog wisps, distant traffic streaks in the parallax city backdrop).

### R4. Strictly Exterior & Purely Additive Constraint
- Strictly zero interior changes: do not modify interior room cutaways, interior backdrops, door placements, room furnishings, or resident pathfinding/living state.
- Strictly additive: do not destroy or remove existing simulation structures, floor slabs, elevator shafts, or functional utility networks.

### R5. Asset Generation Standards
- Use existing project fixture/prop catalogs and procedural mesh quad / sprite generation with OneRoofWorldMaterial or URP-compliant unlit shaders.
- Preserve domain purity (OneRoof.Domain with 0 UnityEngine references).

## Acceptance Criteria

### Visual & Architectural Presentation
- [ ] Exterior windows are visibly rendered on external building walls with clear frame definition and day/night window glow.
- [ ] Volumetric light cones project visibly from occupied windows without z-fighting, clipping errors, or magenta shader artifacts.
- [ ] Rooftop features (HVAC, water tanks, antennas, beacons) are visible at standard gameplay zoom levels across building roof tiers.
- [ ] Facade dressing (fire escapes, utility conduits, AC units) cleanly attaches to exterior building edges without penetrating interior cutaways.
- [ ] Ambient parallax/sky elements integrate smoothly with OutsideCityPresenter and TowerAtmospherePresenter.

### Structural Integrity & Non-Destruction
- [ ] Zero alterations to interior room layout, props, or resident logic.
- [ ] Existing floor slabs, elevator car/shafts, and utility overlays remain 100% intact and functional.

### Programmatic & Technical Verification
- [ ] Headless Unity compile (/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit) exits with code 0.
- [ ] EditMode test suite passes with 0 failures (-runTests -testPlatform EditMode).
- [ ] PlayMode test suite passes with 0 failures (-runTests -testPlatform PlayMode).
- [ ] git diff --check exits with code 0 (zero whitespace/formatting errors).
