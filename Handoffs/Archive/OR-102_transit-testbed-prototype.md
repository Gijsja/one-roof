# OR-102 — Floors, cells, rooms, portals & transit testbed prototype

**Status:** DONE
**Owner:** Antigravity session
**Updated:** 2026-09-15

## Current outcome

1. Pure domain spatial topology primitives and fixture are established in `OneRoof.Domain`:
   - `CellCoordinate`: Discrete `(X, Floor)` coordinates.
   - `CellBounds`: Discrete 1D horizontal cell range on a specific floor with width, containment, and overlap detection.
   - `Portal` & `PortalType`: Typed spatial thresholds (`Door`, `ElevatorShaftDoor`, `StairwellDoor`) linking cells, rooms, and vertical shafts.
   - `Room`: Bounded spaces with `ContentId`, `Capacity`, and associated portals.
   - `FloorTopology`: Per-floor container validating non-overlapping room bounds and portal placement.
   - `BuildingTopology`: Multi-floor building model with cross-floor indexing and cell lookups.
   - `FiveFloorTopologyFixture`: Canonical 5-floor fixture with ground-floor lobby and commercial space, 4 residential floors with sufficient capacity for 50 residents, and vertically aligned elevator portals on all floors.
2. The prototype transit testbed is established in `Assets/Scenes/Testbed_Transit.unity` with:
   - Five visible tower floors and one initial elevator.
   - Fifty residents beginning queued in the lobby and moving through a deterministic morning commute.
   - A debug HUD showing queue size, arrivals, and average completed wait.
   - Player capacity adjustment demonstrating reduced wait times.

## Files owned

- `Assets/OneRoof/Runtime/Domain/Topology/`
- `Assets/OneRoof/Runtime/Domain/Transit/`
- `Assets/OneRoof/Runtime/Application/Transit/`
- `Assets/OneRoof/Runtime/Presentation/Transit/`
- `Assets/OneRoof/Editor/Transit/`
- `Assets/Scenes/Testbed_Transit.unity`
- `Assets/OneRoof/Tests/EditMode/Domain/TopologyPrimitivesTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/FiveFloorTopologyFixtureTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/TransitPrototypeSimulationTests.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/TransitPrototypeSceneCompositionTests.cs`
- `Assets/OneRoof/Editor/OneRoof.Editor.asmdef`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`

## Validation

| Check | Evidence | Result |
| --- | --- | --- |
| Scene generation | Headless Editor execute method `OneRoof.Editor.Transit.TransitPrototypeSceneBuilder.CreateScene` | PASS — `Assets/Scenes/Testbed_Transit.unity` created. |
| Deterministic commute | Direct headless Edit Mode runner filtered to `OneRoof.Domain.Tests` | PASS — 10 passed, 0 failed; `/tmp/one-roof-prototype-domain.xml`. |
| Scene and visual composition | Direct headless Edit Mode runner filtered to `OneRoof.Presentation.Tests` | PASS — 2 passed, 0 failed; `/tmp/one-roof-prototype-presentation.xml`. |
| Topology primitives & 5-floor fixture | Pure Domain tests `TopologyPrimitivesTests` and `FiveFloorTopologyFixtureTests` | PASS — pure C# validation without engine dependencies. |
| Whitespace & git hygiene | `git diff --check` | PASS. |
| Remote CI | GitHub Actions | NOT RUN — OR-005 remains blocked on `UNITY_LICENSE`. |

## Known limitations

- The transit testbed prototype uses simplified visual floor bands; replacing visual bands with full room geometry will connect via OR-201/OR-401.
- The HUD is a runtime debugging overlay, not the final Build/Inspect/Data UI shell.

## Next safe action

Begin **OR-201** (Hierarchical transit graph connecting room portals, floor-local paths, and vertical elevator shafts) or **OR-103** (Versioned save envelope).
