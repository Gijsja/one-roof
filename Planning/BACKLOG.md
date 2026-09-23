# Backlog

Status values: `READY`, `ACTIVE`, `BLOCKED`, `DONE`. One agent owns one task at a time.

| ID | Status | Milestone | Task | Depends on | Acceptance |
| --- | --- | --- | --- | --- | --- |
| OR-001 | DONE | M0 | Confirm platform, monetization, Unity version, and VCS | — | Choices recorded in decision log |
| OR-002 | DONE | M0 | Create Unity project from a listed template | OR-001 | Project opens and imports cleanly |
| OR-003 | DONE | M0 | Install approved packages through Package Manager API | OR-002 | Manifest resolves; no package errors |
| OR-004 | DONE | M0 | Create assemblies and test assemblies | OR-003 | Dependency-boundary tests compile |
| OR-005 | BLOCKED | M0 | Add CI compile plus Edit/Play Mode tests | OR-002 | Clean run on fresh checkout; requires GitHub `UNITY_LICENSE` secret |
| OR-101 | DONE | M1 | Implement IDs, clock, seed, command, and event primitives | OR-004 | Pure C# deterministic tests pass |
| OR-102 | DONE | M1 | Implement floors, cells, rooms, portals | OR-101 | Fixture creates valid five-floor topology |
| OR-103 | DONE | M1 | Implement versioned save envelope | OR-101 | Round-trip and corrupt-save tests pass |
| OR-201 | DONE | M2 | Implement hierarchical transit graph | OR-102 | Known route fixtures pass |
| OR-202 | DONE | M2 | Implement elevator bank state machine | OR-201 | Boarding, capacity, queue, and timing tests pass |
| OR-203 | DONE | M2 | Add wait-time and congestion projections | OR-202 | Fixed scenario produces stable metrics |
| OR-301 | DONE | M3 | Implement household/person/schedule records | OR-101 | Fifty-resident fixture is deterministic |
| OR-302 | DONE | M3 | Generate trips from home/work/food routines | OR-201, OR-301 | Morning trip demand matches fixture |
| OR-303 | DONE | M3 | Bind pooled NPC views to projections | OR-302 | 40-view cap holds while 50 persist |
| OR-401 | DONE | M4 | Implement Build, Inspect, and Data mode shell | OR-102 | Modes switch without mutating state directly |
| OR-402 | DONE | M4 | Implement elevator-wait flow overlay | OR-203, OR-401 | Bottleneck and cause are visible |
| OR-403 | DONE | M4 | Implement elevator placement prediction | OR-203, OR-401 | Preview estimates before/after wait |
| OR-404 | DONE | M4 | Complete golden first-playable test | OR-302, OR-402, OR-403 | Intervention improves agreed metrics |
| OR-501 | DONE | M5.1 | Implement domain building commands & dynamic topology | OR-102, OR-201, OR-401 | Commands validate constraints, mutate topology, emit events, and update transit graph |
| OR-502 | DONE | M5.1 | Implement unified TowerSimulation & leg-by-leg trip execution | OR-501, OR-202, OR-302 | Master simulation coordinates clock, routines, and discrete movement without allocations |
| OR-503 | DONE | M5.1 | Implement tower economy, treasury, and demand-driven leasing | OR-501, OR-301 | Build costs deduct cash, rent collects, and demand spawns new households/workers |
| OR-504 | DONE | M5.1 | Implement comprehensive save/load envelope for unified state | OR-502, OR-503, OR-103 | Round-trip exact save/load preserves topology, residents, and in-flight commute queues |
| OR-505 | DONE | M5.1 | Implement interactive build grid interaction & ghost preview | OR-501, OR-502, OR-401 | Grid hover/drag validates placement visually and dispatches build commands |
| OR-506 | DONE | M5.1 | Complete golden expansion & persistence acceptance test | OR-501, OR-502, OR-503, OR-504, OR-505 | Dynamic expansion creates congestion, 2nd elevator improves wait >40%, save/load round-trips |
| OR-511 | DONE | M5.2 | Implement Demolish / Bulldozer tool & dynamic cell reclamation | OR-501, OR-505 | Bulldozer identifies room under cursor, validates safety, executes DemolishRoomCommand, refunds 50% salvage cash, updates transit graph and clears view |
| OR-512 | DONE | M5.2 | Implement 2-cell Stairwell transit construction | OR-501, OR-505 | Stairwell spans adjacent floors, creates amenity:stairwell and StairwellDoor portals, adds walkable vertical edges, and renders stair flight visuals |
| OR-513 | DONE | M5.2 | Implement Workplace / Office room zoning & commute integration | OR-501, OR-503, OR-505 | 8-cell office zoning with tech/corporate visuals, workforce capacity, and leasing employment integration |
| OR-514 | DONE | M5.2 | Implement Room Backdrop Slicing Contract & 9-Slice Presenter | OR-506 | 9-sliceable backdrops for residential, office, diner, and lobby; auto-expands across 1-8 cells without distortion |
| OR-515 | DONE | M5.2 | Interactive Tower Camera Pan & Zoom Navigation | OR-505 | Middle-mouse drag, WASD/arrow pan, scroll-wheel zoom clamped by floor count and slab bounds |
| OR-516 | DONE | M5.2 | Domain Interaction Point & Furniture Anchor Schema | OR-513 | Domain records for seat/sleep/cook/work/browse, room interaction points, and resident anchor docking |
| ART-001 | DONE | M5.2 | First-playable architectural dressing (backdrops, doors, windows, cabin) | OR-514 | Sliced 9-sliceable backdrops and doors in runtime content; zero primitive rects in Tower scene |
| ART-002 | DONE | M5.3 | Environment prop families & themed room furnishings | ART-001, OR-516 | 16-prop sheet normalized, collision & interaction anchors declared for residential, diner, and office |
| OR-517 | DONE | M5.3 | AllIn1SpriteShader integration & holographic placement ghost | OR-505 | `PlacementGhostPresenter` renders animated holographic scanlines with dynamic validity outline/glow, procedural border texture, and URP fallback; zero compiler errors |
| OR-521 | DONE | M5.3 | Resident room living & leg-by-leg corridor-elevator transit | OR-516, OR-502 | Residents live inside assigned apartments/rooms with interior slot spacing, walk along corridors, queue at floor-specific elevator landings, and enter rooms upon transit delivery |
| **ARCH-001** | **DONE** | **M5.3a** | **Delete parallel prototype simulation (unjustified seam)** | OR-521 | `TransitPrototypeSimulation.cs` and `TransitPrototypeSession.cs` deleted; `Testbed_Transit` uses `TowerSimulationSession` + `FiftyResidentFixture`; all 173+ tests pass; no projection duplication |
| **ARCH-002** | **DONE** | **M5.3a** | **Split God Presenter: TowerPlayableController → 4 deep presenters** | ARCH-001 | `TowerPlayableController.cs` ≤150 lines; `TowerStructurePresenter`, `ElevatorBankPresenter`, `RoomPresenter`, `TowerResidentPresenter`, and `TowerDashboardHudView` exist with EditMode tests; all tests pass |
| **ARCH-003** | **DONE** | **M5.3a** | **Domain CanExecute seam for placement validation** | ARCH-001 | `TowerSimulation.CanExecute()` exists; `GridPlacementController` contains zero domain validation logic; CanExecute domain tests + placement ghost tests pass |
| **ARCH-004** | **DONE** | **M5.3a** | **Push serialization into aggregate ToSaveData/FromSaveData** | ARCH-001 | Each aggregate owns round-trip serialization; `TowerSimulation.ExportSaveData()` delegates; golden acceptance + save tests pass |
| **ARCH-005** | **DONE** | **M5.3a** | **Encapsulate ElevatorBank queues behind Snapshot()** | ARCH-001 | `FloorQueues`/`DeliveredPassengers` internal; `ElevatorBank.Snapshot()` returns `ElevatorBankSnapshot`; session + congestion tests pass |
| OR-518 | DONE | M5.3 | Inspect mode selection and hover outline shader presenter | OR-517, **ARCH-002** | Selected and hovered rooms, residents, and elevator shafts render pixel-perfect outline and soft glow silhouettes *(targets split presenters)* |
| OR-519 | DONE | M5.3 | Demolition dissolve and construction scanline shader transitions | OR-511, OR-517, **ARCH-002** | Bulldozing rooms/slabs triggers animated dissolve shader effect; construction fades in with digital blueprint wireframe *(targets `RoomPresenter` + `TowerStructurePresenter`)* |
| OR-520 | DONE | M5.3 | Elevator congestion & resident agitation visual shader aura | OR-203, OR-517, **ARCH-002** | Residents waiting beyond congestion threshold and overcrowded elevator doors display pulsing agitation aura *(targets `ElevatorBankPresenter` + `NpcPopulationPresenter`)* |
| OR-522 | DONE | M5.4 | Presentation furniture anchor docking | OR-516, OR-521, **ARCH-002** | Residents visibly dock onto sofas, beds, desks, and booths using OR-516 interaction points *(targets split presenters)* |
| OR-523 | DONE | M5.4 | Inspect mode deep cards for residents, rooms, and elevator banks | OR-518, OR-521, **ARCH-005** | Clicking entities in Inspect mode opens comprehensive symptom/cause drill-down cards *(uses `ElevatorBankSnapshot` for elevator card data)* |
| ART-003 | DONE | M6.0 | Spine 2D skeletal animation & 8-layer wardrobe composition | ART-002 | Shared 17-bone rig animated with 6 clips; dynamic 8-layer wardrobe compositor operational |
| OR-601 | DONE | M6.1 | Resident needs & dynamic schedule arbitration | OR-521, ART-003 | Five core needs (Hunger, Energy, Social, Hygiene, Purpose) drive autonomous destination choices |
| ART-004 | DONE | M6.1 | AssetLab validation tooling & Addressables packaging | ART-003 | Standalone AssetLab scene runs automated seam, rig, and anchor checks; Addressables bundles build cleanly |
| OR-601B | DONE | M6.1 | Personality facets for resident wellbeing | OR-601 | Four to eight high-impact facets filter strain accumulation and thought strength; domain-only records; deterministic tests demonstrate different reactions to the same event |
| OR-602 | DONE | M6.2 | Satisfaction, grievances, strain & satisfaction overlay | OR-601, OR-601B | Satisfaction aggregates commute, crowding, noise, rent burden, service access, and recent events; low satisfaction creates grievances; personality-filtered strain accumulates; persistent high strain triggers move-out or mental-strain events; overlay and immutable inspector projections explain contributors |
| OR-603 | DONE | M6.2 | Population density & demographic distribution overlay | OR-601 | Overlay 3 visualizes resident density and income/age demographics across the tower, with non-colour encoding and drill-down to the cause-chain inspector |
| OR-604 | DONE | M6.3 | Scrutiny external-pressure resource | OR-602 | Domain state and overlay-readable trend rise with expansion speed, inequality, unresolved crises, and aggressive policy; balanced response and service investment reduce it; high scrutiny raises inspection-event pressure (never an instant build veto; see ADR-065) |
| ART-005 | DONE | M6.2 | Spatial audio soundscapes & environmental lighting atmosphere | ART-004 | Footstep surface audio, elevator mechanical foley, roomtones, and volumetric window lighting |
| OR-605 | DONE | M6.3 | Soft specialist roles & training capacity | OR-601, OR-513 | Training and service capacity enable residents to acquire Maintenance, Security, Service, and later Knowledge roles; no direct individual assignment; roles improve efficiency and crisis response |
| OR-701 | DONE | M7.1 | Expanded commercial & service zoning | OR-513, OR-501 | Retail shops, clinics, maintenance workshops, and security stations added to building catalog |
| OR-702 | DONE | M7.2 | Commercial lease lifecycle & resident employment matching | OR-701, OR-503, OR-605 | Businesses recruit resident workers through systems rather than direct orders, pay wages, collect customer revenue, risk insolvency, and prefer relevant specialist roles |
| OR-703 | DONE | M7.3 | Business health & foot traffic flow overlays | OR-702, OR-514 | Overlays 1 & 6 render pedestrian flow vectors and tenant financial solvency indicators |
| OR-704 | DONE | M7.4 | Repair and validate volumetric window lighting | ART-005 | In `Tower`, each non-transit room has a visible, correctly layered warm window-light treatment; a Presentation test asserts the intended material/render state and a PlayMode screenshot verifies it is not hidden by room geometry. |
| OR-705 | DONE | M7.4 | Limit elevator banks to three cars | OR-202, OR-403 | `ElevatorBank.MaxCarsPerBank` is 3; command validation, placement prediction, UI messaging, and tests reject a fourth car; a presenter test/screenshot verifies three cars remain entirely inside the shaft without clipping into rooms. |
| OR-706 | DONE | M7.4 | Restore interactive floor-slab placement | OR-501, OR-515 | In Build mode, selecting Floor Slab, previewing the next valid floor, and confirming it creates the slab, updates topology and treasury, and makes the new floor visible; add an interaction-level PlayMode test and screenshot in addition to existing domain/controller tests. |
| OR-707 | DONE | M7.4 | Expand ground slab and repair structure rendering | OR-706 | Ground-slab expansion validates affordability and bounds, updates topology and presentation geometry, and canonical slab/line quads are adopted/deduplicated with repaired palette blocks; presenter regression tests pass. |
| OR-801 | DONE | M8.1 | Physical electrical grid network | OR-501, OR-502 | Ground substation, vertical riser ducts, floor transformers, and voltage drop / brownouts |
| OR-802 | DONE | M8.1 | Plumbing water & gravity waste networks | OR-801 | Ground pumps, vertical pressure head, booster pumps, and gravity trash chute collection |
| OR-803 | DONE | M8.2 | Infrastructure degradation, technician jobs & utilities overlay | OR-801, OR-802, OR-605 | Equipment wear, specialist technician response, failure disruption chains, and Overlay 8 utilities flow |
| OR-1004 | DONE | M10.4 | Outside world boundary and resident arrivals | OR-502, OR-521 | Typed, saveable `Outside` endpoint and lobby-only street-edge route; demand move-ins and external workers cross the boundary; layered parallax city and smooth walking presentation. |
| **ECON-001** | READY | ECON | Closed-loop household cash & daily settlement | OR-503, OR-601 | `long cashBalance` and `arrearsDays` on `HouseholdRecord`; daily rent collection at `tick % 1440 == 0` deducts household and credits treasury; 30-day cash conservation test fixture |
| **ECON-002** | READY | ECON | Commercial rent & demand-capped business revenue | ECON-001, OR-702 | Businesses pay per-cell rent and tax; revenue capped by staffed capacity and occupancy; insolvency flags vacancy after 7 days and appears on Overlay 6 |
| **ECON-003** | READY | ECON | Policy decrees domain value object (`PolicyDecreeState`) | ECON-001, ARCH-003, OR-604 | Rent caps, commercial tax, transit subsidies, quiet hours; validates via `CanExecute`, emits domain events, and hooks into `ScrutinyState` |
| **ECON-004** | READY | ECON | Economic explanation wiring & golden acceptance proof | ECON-002, ECON-003 | Wire `TreasuryFlowProjection`, tenant margin badges on Overlay 6, and resident rent burden on Overlay 4; end-to-end 30-day ledger conservation proof |
| OR-901 | READY | M9.1 | Inter-resident relationship graph & 4 faction archetypes | OR-602, OR-702, ECON-001 | Friendship/rivalry networks and four faction allegiances (Tenant Union, Corporate, Merchants, Eco Council) fed by shared grievances, strain, and arrears; aggregation-safe at 300 residents |
| OR-902 | READY | M9.2 | Steward policy decree management panel | OR-901, ECON-003 | Manage mode panel for rent caps, transit subsidies, quiet hours, and commercial tax rates; decrees affect satisfaction, strain, scrutiny, and faction approval |
| OR-903 | READY | M9.3 | Faction tension & noise overlays with civil action events | OR-901, OR-902 | Overlays 5 & 7 render acoustic contours and regional tension; protests and strikes arise through faction strain plus scrutiny thresholds |
| OR-1001 | READY | M10.1 | Blueprints & rapid multi-floor expansion tooling | OR-505, OR-801 | Floor copy/paste blueprints and 30-floor scaling under strict 60 FPS / <4ms tick budget |
| OR-1002 | READY | M10.2 | Adaptive crisis pressure & personal consequences | OR-903, OR-803, OR-604 | Systems-driven cable, fire, heatwave, epidemic, strike, and inspection crises land on individual residents through needs and strain; scrutiny modulates frequency and severity |
| OR-1003 | READY | M10.3 | Beta boundary golden acceptance test (City Status) | OR-1001, OR-1002, ECON-004 | 30 floors and 300 residents run deterministically, sustain City Status for 30 in-game days, exercise Outside move-ins and external commutes across save/load, and keep needs, satisfaction, strain, and scrutiny legible through the cause chain |

## Latest validation

- Comprehensive Audit & Stabilization Roadmap: 6-agent forensic review completed across Security, Code Quality, Bugs, Concurrency, Test Flakiness, and Maintainability. Prioritized 6-phase executable stabilization roadmap authored. See `Handoffs/Active/HANDOFF_2026-09-23_comprehensive-audit-and-stabilization-roadmap.md`.
- ONI-style Utility Network Layer: isolated Unity 6000.3.24f1 compile passed; focused tests 8/8 passed, full EditMode 173/175 (two baseline presentation failures). See `Handoffs/Archive/HANDOFF_2026-09-23_oni-utility-network-layer.md`.
- Steward UI shell refresh: isolated Unity 6000.3.24f1 compile passed; focused UI tests 14/14, golden playmode 1/1, grid tests 41/42 passed (1 pre-existing pointer test failure). See `Handoffs/Archive/HANDOFF_2026-09-23_steward-ui-shell.md`.
- Outside City parallax: isolated Unity 6000.3.24f1 compile passed; focused presenter tests 2/2, full EditMode 451/453 (same two pristine-HEAD presentation failures), and PlayMode 6/6 passed. See `Handoffs/Archive/HANDOFF_2026-09-23_outside-city-parallax.md`.
- OR-1004 isolated Unity 6000.3.24f1 compile passed; focused trip tests 11/11 and save tests 6/6 passed; full EditMode 449/451 (two identical pristine-HEAD presentation failures) and PlayMode 5/5 passed. See `Handoffs/Archive/HANDOFF_2026-09-23_OR-1004-outside-implementation.md`.
- Full EditMode: 450+ passed; pre-existing presentation pointer/region failures documented.
- Full PlayMode: 5/5 passed.
