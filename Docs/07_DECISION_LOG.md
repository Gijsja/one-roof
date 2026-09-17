# Architecture Decision Log

Append decisions; do not silently rewrite history. Use `Handoffs/DECISION_TEMPLATE.md` for full records when needed.

| ID | Status | Decision | Reason |
| --- | --- | --- | --- |
| ADR-001 | Accepted | Pure C# domain separate from Unity presentation | Scale, testability, save stability |
| ADR-002 | Accepted | Stable integer runtime IDs and immutable string content IDs | Decouple simulation from assets and scenes |
| ADR-003 | Accepted | Hierarchical floor/transit routing | Make vertical travel scalable and inspectable |
| ADR-004 | Accepted | Persistent simulation entities with pooled visible views | Support many lives without many GameObjects |
| ADR-005 | Accepted | ScriptableObjects for definitions only | Prevent mutable campaign state leaking into assets |
| ADR-006 | Accepted | First playable precedes factions and asset generation | Prove the core cause-and-effect loop first |
| ADR-007 | Proposed | Unity 6 LTS + URP + 2D Animation + Addressables | Matches 2.5D presentation and content volume; confirm exact versions after project creation |
| ADR-008 | Accepted | Ship the initial release as a premium single-player desktop game using Unity 6000.3.24f1, URP, and GitHub-hosted Git | Confirms OR-001 from the product vision, installed project metadata, and established repository workflow |
| ADR-009 | Accepted | Use Unity Addressables 4.0.1 and 2D Animation 13.0.6 | These approved Package Manager resolutions implement ADR-007 for the current Unity 6000.3.24f1 project |
| ADR-010 | Accepted | Separate runtime concerns into named assemblies with pure Domain and Application layers | Compile-time references enforce the architecture boundary before simulation systems are added |
| ADR-011 | Accepted | Run Unity validation in GitHub Actions with GameCI v4 | The workflow tests Edit Mode and Play Mode separately, then compiles a Linux player from a clean checkout |
| ADR-012 | Accepted | Validate primitive values at construction; preserve random streams as xorshift64 seed/state/position snapshots | Gives save-local contracts unambiguous invalid-value boundaries and exact, portable replay after save/load |
| ADR-013 | Accepted | Expose transit simulation to presentation through Application projections | Keeps presentation dependent on read-only data and capacity actions without leaking mutable Domain state or Domain assembly types across the boundary |
| ADR-014 | Accepted | Model building spaces with discrete 1D cell bounds per floor and typed portal thresholds | Supports non-overlapping rooms, vertical and horizontal portal graphs, and deterministic pathfinding without continuous physics |
| ADR-015 | Accepted | Use a typed JSON envelope with embedded payload string, forward migrator pipeline, and atomic file replace | Guarantees clean corruption rejection, schema version evolution without full model reflection, and safe writes via temporary files |
| ADR-016 | Accepted | Connect horizontal walk edges and vertical elevator shaft edges into a unified hierarchical transit graph | Allows deterministic multi-floor route planning with explicit walk and elevator transit legs without continuous physics |
| ADR-017 | Accepted | Implement elevator banks as explicit multi-phase state machines with discrete travel, door cycle, and dwell ticks | Enforces strict car capacity, FIFO floor queuing, and deterministic transit timing |
| ADR-018 | Accepted | Project transit congestion through read-only floor and bank projections with discrete severity tiers | Enables explainable cause diagnosis (queue length and wait time) for presentation overlays and inspectors |
| ADR-019 | Accepted | Model PersonRecord and HouseholdRecord as mutable sealed classes with explicit mutation methods, not structs or property setters | Keeps simulation ticks allocation-free during steady-state while retaining simple object-graph semantics before NativeCollections are required |
| ADR-020 | Accepted | ScheduleTripGenerator detects block transitions by comparing adjacent tick labels rather than caching per-person previous-block state | Keeps the service stateless, safe to call at any tick, and trivially testable without session initialisation |
| ADR-021 | Accepted | NpcViewPool with 40-view cap bound to NpcProjection stream via NpcVisibilityPolicy | Enforces the 40-view performance budget (Docs/02_ARCHITECTURE.md) while 50 persistent simulation entities run in Domain, recycling pooled Unity GameObjects based on floor visibility and activity priority |
| ADR-022 | Accepted | Command-driven ModeShellSession managing Build, Inspect, and Data modes | Dispatches UI commands and emits read-only ModeShellProjection snapshots, ensuring UI cannot mutate simulation state directly |
| ADR-023 | Accepted | ElevatorWaitOverlayService and CongestionInspectorCardView exposing symptom-cause-response chain | Delivers non-color accessible queue and flow indicators alongside root-cause diagnosis and direct routes to Build mode |
| ADR-024 | Accepted | Deterministic ElevatorPlacementPredictor with before/after wait metrics | Calculates throughput deltas, wait time reductions, and confidence labeling for placement previews before player confirms capacity additions |
| ADR-025 | Accepted | Command-driven dynamic topology mutation and graph synchronization via BuildingTopologyState | Validates floor continuity, slab bounds, room overlap, and structural support before mutating state, emitting domain events and lazily synchronizing HierarchicalTransitGraph |
| ADR-026 | Accepted | Master TowerSimulation and TransitExecutionSystem for discrete leg-by-leg transit movement | Coordinates simulation clock, routine schedule transitions, hierarchical route planning, and discrete leg-by-leg execution (walk progress and elevator queuing/boarding) without allocations |
| ADR-027 | Accepted | Tower economy treasury and autonomous demand-driven leasing system | Deducts construction costs from a cash balance, collects periodic rent, and autonomously evaluates apartment vacancies against transit congestion to move in households |
| ADR-028 | Accepted | Comprehensive in-flight commute and dynamic architecture persistence via TowerSaveData | Serializes building topology, population records, economy treasury, active in-flight trip legs, and elevator car/queue passengers for exact deterministic replay |
| ADR-029 | Accepted | GridPlacementController and PlacementGhostPresenter for interactive build mode | Raycasts screen space to discrete CellCoordinates, validates against slabs, rooms, and treasury, renders colored visual ghosts, and dispatches validated domain commands |
| ADR-030 | Accepted | Golden expansion & persistence acceptance verification for M5.1 | Validates the complete vertical city loop: dynamic 6th-floor construction, demand-driven leasing expansion, commute congestion, >40% wait reduction on elevator intervention, and bit-exact save/load replay |
| ADR-031 | Accepted | Spine 2D skeletal setup, 8-layer NPC contract, and generated resident sprite assets | Replaces placeholder rectangular bars with normalized character sprites structured around a shared 17-bone Spine 4.2 rig (rig.npc.humanoid.2d.v1) and the 8-layer slot contract (Docs/05_ASSET_PIPELINE.md), using NpcContentRegistry, ResidentSpriteCatalog, and NpcSkeletalHierarchy with transit status plates |
| ADR-032 | Accepted | 9-Sliced room backdrop presenter, architectural fixtures, and interactive camera navigation | Replaces primitive room quads with 9-sliced SpriteRenderers (Rooms/), door/window fixtures (Architecture/), and implements TowerCameraController for WASD/mouse pan and scroll zoom |
| ADR-033 | Accepted | Domain interaction point anchors and environment prop furnishings | Declares InteractionPoint domain schema for resident docking and implements PropContentRegistry, PropCatalog, and RoomFurnishingPresenter placing 16 environment props across themed rooms |

