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
| ART-001 | READY | M5.2 | First-playable architectural dressing (backdrops, doors, windows, cabin) | OR-506 | Sliced 9-sliceable backdrops and doors in runtime content; zero primitive rects in Tower scene |
| ART-002 | READY | M5.3 | Environment prop families & themed room furnishings | ART-001 | 16-prop sheet normalized, collision & interaction anchors declared for residential, diner, and office |
| ART-003 | READY | M6.0 | Spine 2D skeletal animation & 8-layer wardrobe composition | ART-002 | Shared 17-bone rig animated with 6 clips; dynamic 8-layer wardrobe compositor operational |
| ART-004 | READY | M6.1 | AssetLab validation tooling & Addressables packaging | ART-003 | Standalone AssetLab scene runs automated seam, rig, and anchor checks; Addressables bundles build cleanly |
| ART-005 | READY | M6.2 | Spatial audio soundscapes & environmental lighting atmosphere | ART-004 | Footstep surface audio, elevator mechanical foley, roomtones, and volumetric window lighting |

