# Handoff — Weather System Debug, Redesign & Multi-Season Atmospheric VFX

**Date:** 2026-09-29  
**Status:** ACTIVE  
**Task:** Weather System Debug & Redesign (Multi-Season, World-Seed Determinism, Fog & Snow VFX)

---

## 1. Summary of Changes

We identified 6 critical issues in the previous weather system and overhauled both the Domain and Presentation layers to deliver a deterministic, multi-season atmospheric simulation.

### 1.1 Bugs Identified & Remediated
- **B1 (Per-Frame Resampling)**: `TowerPlayableController.RenderVisualSnapshot()` was re-evaluating and re-sampling the `MonthlyWeatherCycle` every render frame (60 Hz). Now gated behind `_lastWeatherSampleTick`, sampling strictly once per simulation tick.
- **B2 (World-Seed Disconnect)**: `MonthlyWeatherCycle` was constructed with a hardcoded static seed (`0x5EED_C0DE_1337UL`). It is now dynamically instantiated using the simulation session's unique seed, ensuring distinct, deterministic weather per save.
- **B3 (Clear-to-Rain Transition Flicker)**: Intensity easing during front transitions previously forced `WeatherCondition.Clear` whenever intensity dropped below 0.02, causing brief single-frame drops to clear skies mid-storm. The condition now strictly preserves active precipitation fronts unless the current event itself is Clear.
- **B4 (Degenerate Triangle Quad Cluster)**: `PixelRainPresenter.RebuildMeshGeometry()` wrote `_triangles[t] = 0` for all inactive particle slots, creating hundreds of degenerate triangles sharing vertex 0. Now writes zero-area degenerate triangles referencing `degenerateVertex = vertexIndex > 0 ? vertexIndex - 1 : 0`, and gates bounds recalculation.
- **B5 (Headless-Unsafe Audio DeltaTime)**: `UpdateAudioVolume()` directly sampled `UnityEngine.Time.deltaTime`, which was unreliable in batchmode/headless test execution. It now accepts the simulation's forwarded `deltaTime`.
- **B6 (Missing Atmospheric Conditions & Visuals)**: Expanded `WeatherCondition` with `Fog = 4` and `Snow = 5`, plus full procedural visual particle support in `PixelRainPresenter`.
- **B7 (Weather label without visible rain)**: `InitSubcomponents()` created the rain mesh before `CreateWorldGeometry()` immediately cleared it. Simulation and HUD weather continued updating with a null mesh renderer. Rain initialization now runs after the world clear and camera setup, including on simulation reset.

### 1.2 Domain Layer Enhancements (`OneRoof.Domain.Weather`)
- `WeatherCondition`: Added `Fog = 4` and `Snow = 5` enum entries preserving integer compatibility.
- `WeatherSample`: Added `IsReducedVisibility` computed property (true during Fog, Snow, or heavy Storms). Added default descriptions `"Morning Fog"` and `"Light Snowfall"`.
- `MonthlyWeatherCycle`:
  - Added seasonal climate rules: winter months (12, 1, 2) generate snowfall fronts and cold flurries.
  - Added deterministic Fog front generation between Clear and Drizzle states.
  - Implemented anti-flicker transition thresholding.
  - Preserved pure C# domain boundary (0 UnityEngine references).

### 1.3 Presentation Layer Enhancements (`OneRoof.Presentation.Tower`)
- `PixelRainPresenter`:
  - Extended dynamic mesh buffer to support 128 `SnowFlake` and 32 `FogPuff` quads alongside rain drops, drips, and splashes (total 672 quads, 2688 vertices, single draw call, 0 GC in steady state).
  - Snowflakes feature gentle horizontal sinusoidal sway (`Mathf.Sin`) and soft blue-tinted day/night shading.
  - Fog puffs drift across the viewport with smooth alpha ramps.
  - Snow and Fog suppress rain drop and roof drip generation.
- `TowerPlayableController`:
  - Per-tick sampling gating via `_lastWeatherSampleTick`.
  - Weather cycle re-seeded from simulation session.
  - Manual override expanded to cycle through 0–5 (Clear, Drizzle, Rain, Storm, Fog, Snow) using `W` key.
- `TowerDashboardHudView`:
  - Added color coding for Snow (soft ice cyan `#B8E0FF`) and Fog (muted haze `#A6A6A6`).
  - Added live precipitation intensity bar (`█`) to the dashboard header.
  - Displays current month number in auto mode (`AUTO M1 (W)`).

---

## 2. Validation & Test Evidence

### Headless Unity Batchmode Compile
- **Command**: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/Vibecode/UnityAI/one-roof -logFile /tmp/weather-compile.log`
- **Result**: `EXIT:0`, 0 compile errors, 0 warnings.

### EditMode Test Suites Run
1. **`MonthlyWeatherCycleTests`**:
   - **Command**: `... -runTests -testPlatform EditMode -testFilter "MonthlyWeatherCycleTests"`
   - **Result**: **10 passed, 0 failed** (`total="10" passed="10"` in 0.87s).
   - Validates calendar calculation, seed determinism, front diversity, transition easing, zero-flicker boundary sampling, Fog & Snow valid states, and 12-month contiguous coverage.

2. **`PixelRainPresenterTests`**:
   - **Command**: `... -runTests -testPlatform EditMode -testFilter "PixelRainPresenterTests"`
   - **Result**: **10 passed, 0 failed** (`total="10" passed="10"` in 0.42s).
   - Validates envelope sync, building interior occlusion, roof eave generation, Snow & Fog simulation without crashes, and zero active particles on clear sky fade-out.

3. **`WeatherSystemIntegrationTests`**:
   - **Command**: `... -runTests -testPlatform EditMode -testFilter "WeatherSystemIntegrationTests"`
   - **Result**: **5 passed, 0 failed** (`total="5" passed="5"` in 0.13s).
   - Validates world-seed distinctiveness across instances, determinism under identical seeds, complete description mapping for all 6 conditions, zero-seed fallback promotion, and intensity clamping.

### Invariants
- `OneRoof.Domain` purity verified: 0 UnityEngine references.
- All new files committed with paired `.meta` files (unique GUIDs).

### Ground-start visual regression (B7)
- Isolated worktree: `/home/geisha/.codex/worktrees/rain-visibility-validation/one-roof`, Unity 6000.3.24f1 with graphics enabled.
- Before fix: `/tmp/rain-visibility-baseline.xml`, exit 2; active rain drops existed, but the rain mesh renderer was null and the camera capture showed clear skies.
- After fix: `/tmp/rain-visibility-pixels.xml`, exit 0, 1 passed. The camera-pixel assertion and visual review show rain in `/tmp/one-roof-rain-visibility/rain.png` versus `/tmp/one-roof-rain-visibility/clear.png`.
- Final graphics PlayMode suite: `/tmp/rain-visibility-full-playmode.xml`, exit 0, 11 passed, 0 failed.
- The previous EditMode weather tests only checked particle simulation and therefore did not catch this presentation lifecycle failure. `WeatherVisibilityPlayModeTests` now covers the ground-start camera image.

---

## 3. Next Safe Action
- Open `Tower` or `Tower_GroundStart` scene in Unity Editor.
- Press `W` key repeatedly during Play Mode to cycle through: Clear → Drizzle → Rain → Storm → Fog → Snow → Auto.
- Observe the HUD intensity bar and corresponding weather VFX in the outside stage. The ground-start rain capture now covers the missing-renderer regression.
