# OR-511-513 — Building & Spatial Suite

**Status:** DONE  
**Owner:** Antigravity  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

## Objective

Implement Milestone 5.2 Building & Spatial Suite:
1. **OR-511**: Demolish / Bulldozer tool (`demolish:room`) with cursor target resolution, safety checks, 50% salvage cash refund, and geometry view clearance.
2. **OR-512**: Stairwell construction tool (`transit:stairwell`) spanning adjacent floors (2 cells wide) with vertical walkable graph connectivity and architectural stair flight visuals.
3. **OR-513**: Workplace / Office zoning room (`commercial:office`) with 8-cell capacity, modern corporate/tech workstations, and leasing demand/commute integration.

## Acceptance criteria

- [x] Bulldozer tool highlights room under cursor with amber/orange footprint and displays salvage refund value.
- [x] Demolition protects lobby, elevator shafts, and occupied residences against accidental deletion.
- [x] Successful demolition refunds 50% construction cost to treasury and rebuilds presentation views, reclaiming cells for new construction.
- [x] Stairwell tool connects adjacent floors across 2 cells (`[cellX..cellX + 1]`), validating continuous slab coverage and shaft clearances.
- [x] Stairwell renders architectural stair flight with ascending rungs, yellow safety rail, and green exit light.
- [x] Office room zones 8 discrete cells with corporate cyan/slate backdrop, workstation desks, and glowing PC dual-monitors.
- [x] Office room integrates into `LeasingDemandSystem` as valid workplace assignment for incoming tenants.
- [x] Build Palette in `ModeShellBarController` presents an organized 2-row layout with 8 tools and distinct destructive styling.

## Scope and ownership

Expected files/directories:
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/PlacementGhostPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/GridPlacementControllerTests.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/TowerEconomyAndLeasingTests.cs`
- `Planning/BACKLOG.md`

Do not modify:
- Core ID generation, clock ticks, or save envelope schema contracts.

## State at handoff

All three building systems are implemented, styled, and validated with automated EditMode tests. The live game presents an 8-tool Build palette divided into Zoning & Structure and Transit & Demolition.

## Changes made

- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`: Enhanced `DemolishRoom` to enforce safety protection on lobby/shafts/active tenants and credit 50% salvage refund to treasury upon demolition.
- `Assets/OneRoof/Runtime/Presentation/Tower/PlacementGhostPresenter.cs`: Added optional `customColor` parameter to `ShowGhost` enabling amber/orange warning tint for bulldozer.
- `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs`:
  - Added tool dimensions for `commercial:office` (8c), `transit:stairwell` (2c), and `demolish:room` (dynamic room bounds).
  - Added 2-floor vertical ghost sizing for stairwell placement.
  - Implemented `ValidatePlacement` and `TryExecutePlacement` branches for bulldozer, stairwell, and office.
  - Synchronized `IsPointerOverUI` with the expanded 2-row palette bounds.
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`:
  - Added dedicated visual presentation for `commercial:office` (cyan/slate backdrop, carpet flooring, workstation desks, glowing PC monitors).
  - Added dedicated visual presentation for `amenity:stairwell` (dark industrial cavity, 4 ascending step rungs, yellow safety handrail, green exit sign).
- `Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs`:
  - Redesigned build palette into 2 rows of 4 buttons (Zoning & Structure / Transit & Demolition).
  - Added `_demolishActiveButtonStyle` with alert coral/amber styling.
- `Assets/OneRoof/Tests/EditMode/Presentation/GridPlacementControllerTests.cs`:
  - Added unit tests for tool dimensions, demolish targeting & safety rejections, demolish execution, stairwell validation & execution, and office placement.
- `Assets/OneRoof/Tests/EditMode/Domain/TowerEconomyAndLeasingTests.cs`:
  - Added unit tests for 50% salvage cash refund, occupied apartment eviction safety rejection, and office workplace leasing employment.

## Decisions

- **Salvage Value**: Demolition returns 50% of the original construction cost, rewarding spatial remodeling while retaining an economic friction sink.
- **Tenant Safety**: Unforced demolition rejects apartments occupied by active households (`cmd.Force == false`), avoiding orphaned residents.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Compile | Host Unity Editor compilation check in `~/.config/unity3d/Editor.log` | PASS (0 errors) |
| Edit Mode | Presentation and Domain tests in `GridPlacementControllerTests.cs` and `TowerEconomyAndLeasingTests.cs` | PASS |
| Visual Check | 2-row build palette, office desks/monitors, stairwell rungs, and bulldozer highlight | PASS |

## Known risks or failures

None known.

## Next safe action

Proceed to Milestone 5.2 / 5.3 tasks: `ART-001` (First-playable architectural dressing with 9-sliceable backdrops and doors) or Milestone 6 systems.

## References

- Backlog: `OR-511`, `OR-512`, `OR-513`
