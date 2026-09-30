# Backlog

Status values: `READY`, `IN PROGRESS`, `BLOCKED`, `DONE`. One agent owns one task at a time.
For historical validation notes from earlier milestones, see [`Planning/Archive/VALIDATION_LOG_ARCHIVE.md`](./Archive/VALIDATION_LOG_ARCHIVE.md).

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
| **ARCH-001** | **DONE** | **M5.3a** | **Delete parallel prototype simulation (unjustified seam)** | OR-521 | `TransitPrototypeSimulation.cs` and `TransitPrototypeSession.cs` deleted; `Testbed_Transit` uses `TowerSimulationSession` + `FiftyResidentFixture`; all tests pass; no projection duplication |
| **ARCH-002** | **DONE** | **M5.3a** | **Split God Presenter: TowerPlayableController → 4 deep presenters** | ARCH-001 | `TowerPlayableController.cs` ≤150 lines; `TowerStructurePresenter`, `ElevatorBankPresenter`, `RoomPresenter`, `TowerResidentPresenter`, and `TowerDashboardHudView` exist with EditMode tests |
| **ARCH-003** | **DONE** | **M5.3a** | **Domain CanExecute seam for placement validation** | ARCH-001 | `TowerSimulation.CanExecute()` exists; `GridPlacementController` contains zero domain validation logic; CanExecute domain tests + placement ghost tests pass |
| **ARCH-004** | **DONE** | **M5.3a** | **Push serialization into aggregate ToSaveData/FromSaveData** | ARCH-001 | Each aggregate owns round-trip serialization; `TowerSimulation.ExportSaveData()` delegates; golden acceptance + save tests pass |
| **ARCH-005** | **DONE** | **M5.3a** | **Encapsulate ElevatorBank queues behind Snapshot()** | ARCH-001 | `FloorQueues`/`DeliveredPassengers` internal; `ElevatorBank.Snapshot()` returns `ElevatorBankSnapshot`; session + congestion tests pass |
| OR-518 | DONE | M5.3 | Inspect mode selection and hover outline shader presenter | OR-517, **ARCH-002** | Selected and hovered rooms, residents, and elevator shafts render pixel-perfect outline and soft glow silhouettes |
| OR-519 | DONE | M5.3 | Demolition dissolve and construction scanline shader transitions | OR-511, OR-517, **ARCH-002** | Bulldozing rooms/slabs triggers animated dissolve shader effect; construction fades in with digital blueprint wireframe |
| OR-520 | DONE | M5.3 | Elevator congestion & resident agitation visual shader aura | OR-203, OR-517, **ARCH-002** | Residents waiting beyond congestion threshold and overcrowded elevator doors display pulsing agitation aura |
| OR-522 | DONE | M5.4 | Presentation furniture anchor docking | OR-516, OR-521, **ARCH-002** | Residents visibly dock onto sofas, beds, desks, and booths using OR-516 interaction points |
| OR-523 | DONE | M5.4 | Inspect mode deep cards for residents, rooms, and elevator banks | OR-518, OR-521, **ARCH-005** | Clicking entities in Inspect mode opens comprehensive symptom/cause drill-down cards |
| ART-003 | DONE | M6.0 | Spine 2D skeletal animation & 8-layer wardrobe composition | ART-002 | Shared 17-bone rig animated with 6 clips; dynamic 8-layer wardrobe compositor operational |
| OR-601 | DONE | M6.1 | Resident needs & dynamic schedule arbitration | OR-521, ART-003 | Five core needs (Hunger, Energy, Social, Hygiene, Purpose) drive autonomous destination choices |
| ART-004 | DONE | M6.1 | AssetLab validation tooling & Addressables packaging | ART-003 | Standalone AssetLab scene runs automated seam, rig, and anchor checks; Addressables bundles build cleanly |
| OR-601B | DONE | M6.1 | Personality facets for resident wellbeing | OR-601 | Four to eight high-impact facets filter strain accumulation and thought strength; domain-only records; deterministic tests pass |
| OR-601C | DONE | M6.1 | Resident activity purpose and minimum duration | OR-601, OR-1004 | Outside workers remain away for 480 ticks after arrival; diner and home purposes have minimum dwell, survive save/load, and appear in inspector cards |
| OR-602 | DONE | M6.2 | Satisfaction, grievances, strain & satisfaction overlay | OR-601, OR-601B | Satisfaction aggregates commute, crowding, noise, rent burden, service access, and recent events; overlay and immutable inspector projections explain contributors |
| OR-603 | DONE | M6.2 | Population density & demographic distribution overlay | OR-601 | Overlay 3 visualizes resident density and income/age demographics across the tower, with non-colour encoding and drill-down |
| OR-604 | DONE | M6.3 | Scrutiny external-pressure resource | OR-602 | Domain state and overlay-readable trend rise with expansion speed, inequality, unresolved crises, and aggressive policy; balanced response and service investment reduce it |
| ART-005 | DONE | M6.2 | Spatial audio soundscapes & environmental lighting atmosphere | ART-004 | Footstep surface audio, elevator mechanical foley, roomtones, and volumetric window lighting |
| OR-605 | DONE | M6.3 | Soft specialist roles & training capacity | OR-601, OR-513 | Training and service capacity enable residents to acquire Maintenance, Security, Service, and Knowledge roles |
| OR-701 | DONE | M7.1 | Expanded commercial & service zoning | OR-513, OR-501 | Retail shops, clinics, maintenance workshops, and security stations added to building catalog |
| OR-702 | DONE | M7.2 | Commercial lease lifecycle & resident employment matching | OR-701, OR-503, OR-605 | Businesses recruit resident workers through systems, pay wages, collect revenue, risk insolvency, and prefer relevant specialist roles |
| OR-703 | DONE | M7.3 | Business health & foot traffic flow overlays | OR-702, OR-514 | Overlays 1 & 6 render pedestrian flow vectors and tenant financial solvency indicators |
| OR-704 | DONE | M7.4 | Repair and validate volumetric window lighting | ART-005 | In `Tower`, each non-transit room has visible, correctly layered warm window-light treatment |
| OR-705 | DONE | M7.4 | Limit elevator banks to three cars | OR-202, OR-403 | `ElevatorBank.MaxCarsPerBank` is 3; command validation, placement prediction, UI messaging, and tests reject a fourth car |
| OR-706 | DONE | M7.4 | Restore interactive floor-slab placement | OR-501, OR-515 | Selecting Floor Slab, previewing the next valid floor, and confirming creates slab, updates topology/treasury, and renders new floor |
| OR-707 | DONE | M7.4 | Expand ground slab and repair structure rendering | OR-706 | Ground-slab expansion validates affordability and bounds, updates topology and presentation geometry |
| OR-801 | DONE | M8.1 | Physical electrical grid network | OR-501, OR-502 | Ground substation, vertical riser ducts, floor transformers, and voltage drop / brownouts |
| OR-802 | DONE | M8.1 | Plumbing water & gravity waste networks | OR-801 | Ground pumps, vertical pressure head, booster pumps, and gravity trash chute collection |
| OR-803 | DONE | M8.2 | Infrastructure degradation, technician jobs & utilities overlay | OR-801, OR-802, OR-605 | Equipment wear, specialist technician response, failure disruption chains, and Overlay 8 utilities flow |
| OR-1004 | DONE | M10.4 | Outside world boundary and resident arrivals | OR-502, OR-521 | Typed, saveable `Outside` endpoint and lobby-only street-edge route; demand move-ins and external workers cross boundary; parallax city presentation |
| **ECON-001** | DONE | ECON | Closed-loop household cash & daily settlement | OR-503, OR-601 | `long CashBalance` and `ArrearsDays` on `HouseholdRecord`; daily rent collection at `tick % 1440 == 0`; 30-day cash conservation proof |
| **ECON-002** | DONE | ECON | Commercial rent & demand-capped business revenue | ECON-001, OR-702 | Businesses pay per-cell rent and tax; revenue capped by staffed capacity; insolvency flags vacancy after 7 days |
| **ECON-003** | DONE | ECON | Policy decrees domain value object (`PolicyDecreeState`) | ECON-001, ARCH-003, OR-604 | Rent caps, commercial tax, transit subsidies, quiet hours; validates via `CanExecute`, emits domain events |
| **ECON-004** | DONE | ECON | Economic explanation wiring & golden acceptance proof | ECON-002, ECON-003 | `TreasuryFlowProjection`, tenant margin badges on Overlay 6, and resident rent burden on Overlay 4; 30-day ledger conservation proof |
| **ECON-005** | DONE | ECON | Outside transactions, life-cycle hardship, and residential move-out | ECON-004, OR-1004 | Outside wages and essential meal transactions; conserving cash counterparty; rolling household resilience; rent-specific arrears; recoverable typed move-out |
| **ECON-006** | DONE | ECON | Fund productive work and rebalance basic livelihoods | ECON-005 | New businesses receive 500 opening capital; only paid active staff produce sales; outside wages cover basic rent and meal |
| **ECON-007** | DONE | ECON | Measure long-run City-scale economy margins | ECON-006 | Six deterministic 365-day scenarios at 30 floors / 300 residents reconcile cash and record margins |
| **ECON-008** | DONE | ECON | Close essential spend, employment, and re-lease gaps | ECON-007 | Outside essentials preserve tower service allowance; insolvent tenants pause after 7 days and re-lease only at viable margin |
| OR-901 | DONE | M9.1 | Inter-resident relationship graph & 4 faction archetypes | OR-602, OR-702, ECON-001 | Bounded relationship edges and 4 saveable faction allegiances respond to shared conditions; member, grievance, approval projections |
| OR-902 | DONE | M9.2 | Steward policy decree management panel | OR-901, ECON-003 | Manage mode edits 4 decree fields through `CanExecute`/`ExecuteCommand`; shows settlement cost, affected groups, faction response |
| OR-903 | DONE | M9.3 | Faction tension & noise overlays with civil action events | OR-901, OR-902 | Overlays 5 and 7 show acoustic and tension causes; saveable civil actions require sustained faction pressure |
| OR-904 | DONE | M9.4 | Manager decision record and resident consequences | OR-902, OR-903, ECON-004 | Saved dated decision/incident record with affected IDs and observed deltas; inspector links trace cause to outcome |
| OR-905 | DONE | M9.4 | Playable building-management onboarding | OR-523, OR-402, OR-403, ECON-004, OR-902 | Keyboard-accessible, skippable five-floor lesson completes symptom → overlay → inspector → preview → response → feedback |
| **DIG-001** | **DONE** | Underground | **Independent lair excavation grid** | OR-707 | **1 m underground board independent of tower slabs supports click-drag excavation and lair-floor placement into dug cells** |
| **UNDERCITY-001** | **DONE** | Underground | **Secret Undercity expansion** | DIG-001, OR-1004 | **32×12 connected excavation, lobby access, 14 operating room types, autonomous staffing, supplies, intel, investigator visits, connected utilities** |
| **WEATHER-001** | **DONE** | Atmosphere | **Weather system redesign & multi-season cycle** | ART-005, OR-1004 | **Deterministic 12-month climate cycle with 6 conditions, single-draw-call procedural rain/snow/fog particle mesh, volumetric window cones** |
| OR-1003A | DONE | M10 scale | Authored 30-floor gold-standard city playground | OR-1001 | Populated `Tower_GoldStandard30` scene with 300 residents, physical utility risers, living-city environment, camera presets |
| OR-1003B | DONE | M10 scale | Stair transit bugfix & presentation performance optimization | OR-1003A | Short-hop stair preference, elevator queue diversion, single SRPDefaultUnlit pass, GPU instancing, reducing draw calls from 14,585 to 171 and frame time to 9.27 ms (~108 FPS) |
| **DOC-010** | **DONE** | Documentation | **v0.10 Documentation Upgrade & Context Shield** | All prior | 3-tier documentation architecture, context shielding, 6 completed handoffs archived, phantom tools removed, single active handoff |
| OR-906A | DONE | M9.5 | Credible relationship ties | OR-901 | Neutral encounters, shared grievances, affinity return, conflict events, social traits, thought memories, and 12-tier relationship progression spectrum delivered. |
| OR-906B | READY | M9.5 | Issue-specific social influence | OR-906A, OR-602 | Strong recent ties share named grievances using prior-settlement state; inspectable influence separated from direct exposure |
| OR-906C | READY | M9.5 | Local faction organizing | OR-906B, OR-903 | Require sustained grievance pressure and connected local membership for civil action readiness |
| OR-1001 | IN PROGRESS | M10.1 | Connected building & utilities integration | OR-505, OR-801, OR-802, UNDERCITY-001 | Core power/water paths and diagnostics delivered. Slice C: Generator reverse-feed and utility-dependent production remain open. |
| OR-1002 | READY | M10.2 | Adaptive crisis pressure & personal consequences | OR-903, OR-803, OR-604, ECON-004 | 6 condition-driven crisis chains with warnings, propagation, management levers, and saved resident consequences |
| OR-1003 | READY | M10.3 | Beta boundary golden acceptance test (City Status) | OR-1001, OR-1002, OR-1005, OR-1006, OR-1007 | 30 floors and 300 residents run deterministically, sustaining all published City Status gates for 30 consecutive days under <4 ms tick and 60 FPS presentation |
| OR-1005 | READY | M10.3 | Campaign board, pacing, and end states | OR-1002, OR-904 | Daily trends, threatened gates, and 30-day streak are inspectable; failed streak gives reasons; sandbox continuation |
| OR-1006 | READY | M10.3 | Spatial overlay and accessibility beta pass | OR-903, OR-905 | All 8 overlays use contracted channels with non-colour equivalents; UI scale, remappable controls, pause/speed pass review |
| OR-1007 | READY | M10.3 | Save, recovery, and content QA | OR-1002, OR-1005 | Crisis and campaign state survive save/load; migrations, corrupt saves, partial tower demolition, and content review pass |

---

## Current Validation Baseline (v0.10)

- **Test Suite Status**: **765 automated tests passing** (754 EditMode, 11 PlayMode, 0 failures, 1 explicit benchmark skipped).
- **Scale Performance**: In `Tower_GoldStandard30` (30 floors, 300 residents), draw calls reduced to **171** (-98.8%), batches to **86** (-98.9%), and main thread frame time to **9.27 ms** (~108 FPS).
- **Domain Purity**: Zero `UnityEngine` references in `OneRoof.Domain` and `OneRoof.Application`.
- **Active Handoff**: Strictly 1 active handoff at [`Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md`](../Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md).
