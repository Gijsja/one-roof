# OR-201 — Hierarchical transit graph

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-15  
**Updated:** 2026-09-15

## Objective

Implement the hierarchical transit graph in `OneRoof.Domain` connecting room portals, floor-local walkways, and vertical elevator shafts, along with a deterministic shortest-path route planner.

## Acceptance criteria

- [x] Hierarchical transit graph models room portals, walkways, elevator stops, and stair landings.
- [x] Graph automatically derives floor-local walk edges and inter-floor elevator shaft edges from `BuildingTopology`.
- [x] Deterministic route planner calculates shortest paths with explicit walk and vertical transit legs.
- [x] Two-floor minimal fixture passes same-floor and multi-floor routing tests.
- [x] Five-floor fixture passes deterministic morning commute routing (Lobby to 4th-floor residential units).
- [x] Unreachable / non-existent destinations return null without throwing unhandled exceptions.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/Domain/Transit/TransitNodeType.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitNode.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitEdge.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitRoute.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TransitRoutePlanner.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/TwoFloorTransitFixture.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/HierarchicalTransitGraphTests.cs`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`

Do not modify:

- Presentation or UI assemblies.
- Scenes or ProjectSettings files.

## State at handoff

The pure Domain transit graph and planner are implemented and tested:
- `HierarchicalTransitGraph.FromBuildingTopology` parses floors and portals, connects horizontal walkways, and wires elevator shafts across floors.
- `TransitRoutePlanner` uses deterministic Dijkstra search to generate sequential `TransitRoute` legs with cost and mode (`Walk`, `Elevator`, `Stairs`).
- Pure Edit Mode tests verify two-floor and five-floor routing, leg classification, and failure handling.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Transit/TransitNodeType.cs` — `TransitNodeType` and `TransitMode` enums.
- `Assets/OneRoof/Runtime/Domain/Transit/TransitNode.cs` — Node representing a portal, walkway, or transit stop.
- `Assets/OneRoof/Runtime/Domain/Transit/TransitEdge.cs` — Directed edge with cost and mode.
- `Assets/OneRoof/Runtime/Domain/Transit/TransitRoute.cs` — Sequential legs, total cost, and vertical transit flag.
- `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs` — Hierarchical graph structure and builder from `BuildingTopology`.
- `Assets/OneRoof/Runtime/Domain/Transit/TransitRoutePlanner.cs` — Deterministic shortest path planner.
- `Assets/OneRoof/Runtime/Domain/Transit/TwoFloorTransitFixture.cs` — Test fixture per `Docs/06_TEST_STRATEGY.md`.
- `Assets/OneRoof/Tests/EditMode/Domain/HierarchicalTransitGraphTests.cs` — Unit tests for routing across fixtures.
- `Docs/07_DECISION_LOG.md` — Appended ADR-016.
- `Planning/BACKLOG.md` — Marked OR-201 DONE; unblocked OR-202 to READY.

## Decisions

- ADR-016: Connect horizontal walk edges and vertical elevator shaft edges into a unified hierarchical transit graph.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Route planner & fixtures | Pure Domain test `HierarchicalTransitGraphTests` | PASS |
| Git hygiene | `git diff --check` | PASS |

## Next safe action

Begin **OR-202** (Implement elevator bank state machine: boarding, capacity, queue, and timing tests).

## References

- Backlog: `OR-201`
- Decisions: `ADR-016`
