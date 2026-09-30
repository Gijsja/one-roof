# Architecture & Data Systems — One Roof (v0.10+)

This document is the authoritative engineering specification for One Roof's runtime architecture, data contracts, simulation layers, and unit economics.

---

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
5. **Aggregate Persistence**: Sub-aggregates own `ToSaveData()` / `FromSaveData()` serialization. Zero scene references in saves.

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
   - **Elevators**: Multi-phase kinematics state machine (`Idle`, `Moving`, `DoorsOpening`, `DoorsOpen`, `DoorsClosing`). Clamped to `MaxCarsPerBank = 3` per bank. Dynamic tier profiles (Tiers 1–5) adjust travel speed, door cycles, and ETA-based dispatching.
   - **Stairs**: 2-cell vertical stairwells (`StairwellDoor` portals). Short-hop trips (1–2 floors delta) route natively via stairs. Queued elevator passengers divert to stairs upon excessive wait (`WaitTicks >= 15` or queue length `>= 3`).
4. **Destination Floor Graph**: Corridor traversal and room entry.

### World Seams
- **Outside World Seam**: `Outside` is a persistent Domain world location, not an invalid room ID. Connected through the ground lobby entrance. External workers and demand move-ins cross this boundary. Rendered via a 3-depth parallax skyline (`OutsideCityPresenter`).
- **Subterranean Undercity Seam**: An independent 32×12 excavation grid (1m pitch) owning `UndergroundDigState`. An access core links the surface lobby to underground service corridors and 14 specialized room types. Power and water networks bridge continuously across the lobby boundary.

---

## 4. Entity Records & Data Contracts

### Identity Primitives
```text
EntityId      integer, unique within one save
ContentId     namespaced string, immutable across releases
Tick          integer simulation time (1440 ticks/day)
SchemaVersion integer save/content version
```

### Core Records
| Record | Required Fields |
|---|---|
| **Person** | ID, household, home, workplace, schedule, 5 needs, focus, satisfaction, grievances, strain, personality facets, specialist role/training, social traits, thought memory buffers, relationships. |
| **Household** | ID, members, home room, cash balance, arrears days, rolling budget window. |
| **Room** | ID, content type, floor, bounds, portals, capacity, owner/tenant, utility connection state. |
| **Business** | ID, archetype, leased rooms, employees, cash balance, arrears days, demand cap, margin. |
| **Faction** | ID, members/support, goals, grievances, regional influence, civil action state. |
| **Trip** | Person, origin and destination endpoints (Room or Outside), purpose, departure tick, route state. |
| **Underground Cell** | Coordinate on 32×12 board; tracks excavation and floor status. |
| **Underground Room** | ID, type, rectangular bounds, corridor access, required staff, upkeep, utility status. |

### Wellbeing & Explanation Projections
- **Needs (Short-Term)**: Hunger, Energy, Social, Hygiene, Purpose. Focus is derived from need fulfillment.
- **Satisfaction (Medium-Term)**: Aggregates commute quality, crowding, noise, rent burden, service access, and recent events into bounded grievances.
- **Strain (Long-Term)**: Cumulative pressure filtered through 4–8 personality facets. Persistent high strain triggers move-outs or radicalization.
- **Scrutiny (External Pressure)**: Tower-level pressure resource rising with expansion speed, inequality, and unresolved crises; reduced by balanced policy and services.

### Command & Snapshot Rules
- Commands express intent and validate constraints via `TowerSimulation.CanExecute(ICommand)`. Successful commands mutate domain state and emit events; rejected commands return machine-readable reasons.
- Presentation and UI receive immutable snapshots (`ElevatorBankSnapshot`, `TowerDataOverlays`, `TreasuryFlowProjection`). Background calculation must never retain mutable scene references.

---

## 5. Closed-Loop Unit Economics

One Roof enforces strict integer cash conservation across `treasury + Σhouseholds + Σbusinesses + OutsideMarketState`:
- **Single Conserved Currency**: Ledgers use integer `long` cash units. Normalized `0–1` values exist only as derived presentation projections.
- **Systems-Only Levers**: The Steward adjusts room zoning, capacity additions, leasing, and 4 policy decrees (Rent Cap, Commercial Tax, Transit Subsidy, Quiet Hours).

### Daily Settlement (`tick % 1440 == 0`)
1. **Payroll**: Solvent businesses pay `wageRate[role]` to active workers. Unfunded staff halt output.
2. **Outside Wages**: Residents with outside jobs receive 24 cash/day from `OutsideMarketState`.
3. **Household Spend**:
   - Rent deducted from household cash and credited to treasury (`12/resident × rentMultiplier`).
   - Essential meals (5/resident) transfer to open diners/clinics (or Outside without consuming tower allowances).
4. **Commercial Settlement**:
   - Customer revenue: `customers = min(paid active staff × serveRate, demandCap)`.
   - Rent (`8/cell` office/retail, `5/cell` diner/clinic/workshop) and commercial taxes paid to treasury.
   - Fixed operating upkeep (35/day) deducted.
5. **Treasury Settlement**: Aggregates gross rent, tax, utility upkeep (`1/utility-cell + 2/elevator-car`), and subsidies into `TreasuryFlowProjection`.
6. **Delinquency & Insolvency**:
   - Households with cash < 0 accumulate `ArrearsDays`. `ArrearsDays > 30` creates grievances and move-out notices.
   - Businesses with cash < -100 become insolvent. After 7 days, operations halt until re-leased at a viable projected margin.

---

## 6. Rendering, Batching & Presentation Architecture

- **Shader Model**: `OneRoofUnlit.shader` uses a single canonical `SRPDefaultUnlit` pass (100% SRP Batcher compatible).
- **GPU Instancing**: Enabled across shared resident materials (`Npc_DefaultSharedMaterial`, `PooledNpc_SharedMaterial`).
- **3-Tier Resident LOD**:
  1. *Skeletal Rig Tier* (orthographic size <6.5): up to 60 nearby residents use full 17-bone Spine rigs with 8-layer wardrobe composition. Inspected-resident priority is enforced.
  2. *Full-Body Sprite Tier* (sizes 6.5–8.0): crossfades into lightweight directional sprite quads.
  3. *Combined Silhouette Mesh Tier* (sizes 16.0–18.0): transforms distant residents into a single combined silhouette mesh batch.
- **Semantic Zoom Facade**: Upper room interiors dissolve behind an exterior curtain wall, storefront glazing, and spandrel envelope at sizes 8–18. Windows reflect resident occupancy and voltage. The `F` key toggles between OUTSIDE envelope and CUTAWAY interior.
- **Presentation Settings & View Pooling**: `ResidentPresentationSettings` configures skeletal capacity (default 60), prewarming, and zoom thresholds. Transient resident roots and scratch dictionaries are reused across frames and turnover without GC allocations.
- **Dynamic Procedural VFX**: `PixelRainPresenter` uses a single dynamic mesh buffer (672 quads, 1 draw call, 0 GC steady state) simulating multi-season precipitation and fog.
- **Performance Budget & Measured Overview**:
  - *Reference Overview (`Tower_GoldStandard30` @ 30 floors / 300 residents)*: **83 draw calls**, **86 batches**, **10.0 ms CPU frame time (~100 FPS)**, **1.7 ms average tick**.
  - *Hard Budget Limit*: <180 draw calls, <100 batches, <16.6 ms frame time (60 FPS), <4.0 ms tick p95.

---

## 7. Persistence & Save Architecture

- **Save Envelope**: Versioned `SaveEnvelope` containing typed metadata, schema version, and embedded payload string.
- **Aggregate Push Pattern**: Each domain sub-aggregate implements `ToSaveData()` / `FromSaveData()`. Eliminates runtime reflection.
- **Forward Migrations**: Schema changes provide explicit forward migrators and fixture coverage.
- **Atomic Replace**: Saves are written to `.tmp` files and swapped into place only upon verified completion.

---

## 8. Canonical Scenes Catalog

| Scene | Role & Setup |
|---|---|
| `Assets/Scenes/Tower_GoldStandard30.unity` | **30-Floor / 300-Resident City Playground (`TowerStartMode.GoldStandardCity`)**. Fully populated reference scale scene running at ~100 FPS and 83 draw calls overview. |
| `Assets/Scenes/Tower_GroundStart.unity` | **Dynamic Ground Start Playground (`TowerStartMode.GroundFloorStart`)**. Starts at Floor 0 foundation; exercises vertical expansion and excavation. |
| `Assets/Scenes/Tower.unity` | **Authored 5-Floor Playground (`TowerStartMode.StandardFiveFloor`)**. Legacy first-playable baseline scene. |
| `Assets/Scenes/Testbed_Transit.unity` | **Isolated Transit Harness**. Lightweight 5-floor vertical transit harness wired to `FiftyResidentFixture`. |
| `Assets/Scenes/AssetLab.unity` | **Artist Validation Lab**. Automated seam, rig, wardrobe, and anchor validation. |
