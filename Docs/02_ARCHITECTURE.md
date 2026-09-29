# Unity Architecture — One Roof (v0.10)

## 1. Runtime Layers & Assembly Boundaries

```text
Bootstrap / Presentation Root (OneRoof.Presentation)
  ├── UI Shell & Inspectors (OneRoof.UI)
  ├── Application Projections & Commands (OneRoof.Application)
  │     ├── Domain Simulation Engine (OneRoof.Domain) [Pure C#, 0 UnityEngine refs]
  │     └── Content Catalogs & Regs (OneRoof.Content)
  └── Infrastructure Adapters & Persistence (OneRoof.Infrastructure)
```

### Governing Invariants
1. **Domain Purity**: `OneRoof.Domain` and `OneRoof.Application` strictly enforce `noEngineReferences: true` (zero `UnityEngine` references).
2. **Projections Only**: Presentation and UI read immutable snapshots and projections; they dispatch validated commands (`TowerSimulation.CanExecute()`) and never directly mutate simulation state.
3. **No Per-Agent Update Loops**: Simulation executes on a discrete fixed tick (1440 ticks/day). Presentation views are pooled, interpolated, and event-driven.
4. **Stable Identity**: Entities use stable runtime integer IDs (`EntityId`); assets and content definitions use namespaced immutable string IDs (`ContentId`).

---

## 2. Assemblies & Responsibilities

| Assembly | Engine Refs | Core Responsibilities |
|---|---|---|
| `OneRoof.Domain` | **False** (0) | Pure C# simulation models, topology state, hierarchical transit graph, elevator kinematics, resident needs/wellbeing, livelihoods, closed-loop economy, utility networks, factions, decrees, weather cycles. |
| `OneRoof.Application` | **False** (0) | Commands, use cases, ports, and immutable read-only projections/snapshots (`TowerDataOverlays`, `ElevatorBankSnapshot`, `TreasuryFlowProjection`, `InspectionFacts`). |
| `OneRoof.Infrastructure` | True | JSON serialization adapters, atomic file save store (`SaveEnvelope`), schema forward-migration pipeline. |
| `OneRoof.Content` | True | ScriptableObjects, Sprite atlases, 17-bone rig definitions, 8-layer wardrobe catalogs, prop definitions. |
| `OneRoof.Presentation` | True | Cutaway world renderers (`TowerStructurePresenter`, `ElevatorBankPresenter`, `RoomPresenter`, `TowerResidentPresenter`), pooled Spine views, parallax city, procedural rain/snow particle mesh, lighting, audio. |
| `OneRoof.UI` | True | `ModeShellBarController`, `StewardTheme`, deep inspection cards (`DeepInspectionCardView`), congestion card, placement previews, policy decree panel. |
| `OneRoof.Editor` | Editor | Scene builders, testbed harnesses, AssetLab validation tooling. |

Each runtime assembly has a corresponding `.Tests.EditMode` test assembly, plus `OneRoof.Tests.PlayMode`.

---

## 3. Movement & Hierarchical Transit

The transit simulation plans routes hierarchically across 4 discrete levels without continuous physics:
1. **Room / Interaction Point**: Floor-local slots (`InteractionPoint`).
2. **Floor Walk Graph**: Discrete horizontal cells (0.5m pitch) and portals.
3. **Vertical Transit Graph**:
   - **Elevators**: Multi-phase kinematics state machine (`Idle`, `Accelerating`, `Cruising`, `Decelerating`, `DoorsOpening`, `DoorsOpen`, `DoorsClosing`). Clamped to a hard 3-car limit per bank (`MaxCarsPerBank = 3`).
   - **Stairs**: 2-cell vertical stairwells (`StairwellDoor` portals). Short-hop trips (1–2 floors delta) route natively via stairs. Queued elevator passengers divert to stairs upon excessive wait (`WaitTicks >= 15` or queue length `>= 3`).
4. **Destination Floor Graph**: Corridor traversal and room entry.

### Outside World Seam
`Outside` is a persistent world location, not a room or shortcut. It connects to the tower through the ground-floor lobby entrance. Demand move-ins and external workers cross this seam. Presentation renders a 3-depth parallax skyline (`OutsideCityPresenter`) responsive to camera panning and the day/night cycle.

### Subterranean Undercity Seam
Beneath Floor 0 lies an independent 32×12 cell excavation grid (1m pitch) owning `UndergroundDigState`. An access core connects the surface lobby to underground service shafts, corridors, and 14 specialized room types. Power and water networks bridge continuously from surface risers to undercity utility ports (`UndergroundUtilityPathState`).

---

## 4. Playable Scenes Catalog

The project hosts 5 canonical scenes under `Assets/Scenes/`:

| Scene | Role & Setup |
|---|---|
| `Assets/Scenes/Tower_GoldStandard30.unity` | **30-Floor / 300-Resident City Playground (`TowerStartMode.GoldStandardCity`)**. Fully populated reference scale scene running at ~108 FPS and 171 draw calls with active stair transit. |
| `Assets/Scenes/Tower_GroundStart.unity` | **Dynamic Ground Start Playground (`TowerStartMode.GroundFloorStart`)**. Starts at Floor 0 foundation; exercises dynamic vertical expansion and downward excavation. |
| `Assets/Scenes/Tower.unity` | **Authored 5-Floor Playground (`TowerStartMode.StandardFiveFloor`)**. Legacy first-playable baseline scene. |
| `Assets/Scenes/Testbed_Transit.unity` | **Isolated Transit Harness**. Lightweight 5-floor vertical transit harness wired to `FiftyResidentFixture`. |
| `Assets/Scenes/AssetLab.unity` | **Artist Validation Lab**. Automated seam, rig, wardrobe, and anchor validation. |

---

## 5. Rendering & Batching Architecture

- **Shader Model**: `OneRoofUnlit.shader` uses a single canonical `SRPDefaultUnlit` pass, maintaining 100% SRP Batcher compatibility.
- **GPU Instancing**: Enabled across NPC shared materials (`Npc_DefaultSharedMaterial`, `PooledNpc_SharedMaterial`).
- **Dynamic Procedural VFX**: `PixelRainPresenter` uses a single dynamic mesh buffer (672 quads, 2688 vertices, 1 draw call, 0 GC steady state) simulating rain, snow, splashes, eave drips, and radial fog puffs with alpha falloff.
- **Performance Budget**:
  - Draw calls: **<180** (currently 171 at 30 floors / 300 residents).
  - Batches: **<100** (currently 86).
  - Main thread frame time: **<16.6 ms (60 FPS)** (currently 9.27 ms / ~108 FPS).
  - Simulation tick: **<4.0 ms p95** on reference hardware.

---

## 6. Persistence & Save Architecture

- **Root Envelope**: Versioned `SaveEnvelope` with typed metadata and payload string.
- **Aggregate Push Pattern**: Sub-aggregates own `ToSaveData()` / `FromSaveData()` serialization (ARCH-004), eliminating reflection.
- **Forward Migrations**: Every schema change supplies a forward migration and fixture test.
- **Atomic Replace**: Saves write to a temporary file (`.tmp`) and replace the target file only upon verified completion.

---

## 7. Tooling & Live Pipeline Probing (Python & CLI)

The project supports both headless batchmode runs and live Editor probing:
- **Headless Batchmode**: Unity tests and compilation run via one-shot headless processes without UI.
- **Live Pipeline Server (`com.unity.pipeline`)**: Port 7800 serves live Editor sessions.
- **Python Probing Tools**: Python scripts (such as `pipeline_client.py` or MCP tools) communicate over localhost:7800:
  - `python3 pipeline_client.py cmd get_performance_stats`: Queries live frame times, batches, and draw calls.
  - `python3 pipeline_client.py cmd console_status`: Retrieves console log status.
  - `python3 pipeline_client.py eval "<expression>"`: Evaluates scene state.
- **Pipeline MCP**: Native agent tools (`mcp__unity__editor_status`, `mcp__unity__console_status`) query the same endpoint.
