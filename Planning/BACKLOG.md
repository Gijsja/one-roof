# Backlog

Status values: `READY`, `ACTIVE`, `BLOCKED`, `DONE`. One agent owns one task at a time.

| ID | Status | Milestone | Task | Depends on | Acceptance |
| --- | --- | --- | --- | --- | --- |
| OR-001 | DONE | M0 | Confirm platform, monetization, Unity version, and VCS | — | Choices recorded in decision log |
| OR-002 | DONE | M0 | Create Unity project from a listed template | OR-001 | Project opens and imports cleanly |
| OR-003 | DONE | M0 | Install approved packages through Package Manager API | OR-002 | Manifest resolves; no package errors |
| OR-004 | READY | M0 | Create assemblies and test assemblies | OR-003 | Dependency-boundary tests compile |
| OR-005 | BLOCKED | M0 | Add CI compile plus Edit/Play Mode tests | OR-002 | Clean run on fresh checkout |
| OR-101 | BLOCKED | M1 | Implement IDs, clock, seed, command, and event primitives | OR-004 | Pure C# deterministic tests pass |
| OR-102 | BLOCKED | M1 | Implement floors, cells, rooms, portals | OR-101 | Fixture creates valid five-floor topology |
| OR-103 | BLOCKED | M1 | Implement versioned save envelope | OR-101 | Round-trip and corrupt-save tests pass |
| OR-201 | BLOCKED | M2 | Implement hierarchical transit graph | OR-102 | Known route fixtures pass |
| OR-202 | BLOCKED | M2 | Implement elevator bank state machine | OR-201 | Boarding, capacity, queue, and timing tests pass |
| OR-203 | BLOCKED | M2 | Add wait-time and congestion projections | OR-202 | Fixed scenario produces stable metrics |
| OR-301 | BLOCKED | M3 | Implement household/person/schedule records | OR-101 | Fifty-resident fixture is deterministic |
| OR-302 | BLOCKED | M3 | Generate trips from home/work/food routines | OR-201, OR-301 | Morning trip demand matches fixture |
| OR-303 | BLOCKED | M3 | Bind pooled NPC views to projections | OR-302 | 40-view cap holds while 50 persist |
| OR-401 | BLOCKED | M4 | Implement Build, Inspect, and Data mode shell | OR-102 | Modes switch without mutating state directly |
| OR-402 | BLOCKED | M4 | Implement elevator-wait flow overlay | OR-203, OR-401 | Bottleneck and cause are visible |
| OR-403 | BLOCKED | M4 | Implement elevator placement prediction | OR-203, OR-401 | Preview estimates before/after wait |
| OR-404 | BLOCKED | M4 | Complete golden first-playable test | OR-302, OR-402, OR-403 | Intervention improves agreed metrics |
