# Handoff — Cutaway Visual Alignment & Apartment Identity

**Session date:** 2026-09-17  
**Task scope:** Fix elevator shaft visual alignment, shaft floor-crossing lines, and provide clear, high-contrast, recognizable architectural visual identity for residential apartments and commercial rooms in the vertical tower cutaway.

---

## Validation Status

- **Unity Compilation:** PASSED — 0 compiler errors, 0 compiler warnings.
- **In-Engine Live Verification:** PASSED — Verified with in-game screenshots (`floor5_built_view.png`) in live Play Mode.
- **Console Errors / Warnings:** 0 errors, 0 warnings.
- **Dynamic Construction:** PASSED — Executed dynamic construction of Floor 5 slab, extended elevator shaft (floors 0..5), and four residential studio apartments (`[-12..-7]`, `[-6..-1]`, `[2..7]`, `[8..13]`).

---

## Diagnosed Root Causes & Implemented Solutions

### 1. Elevator Shaft Misalignment & Horizontal Floor Line Intersections
- **Root Cause:**
  - In `FiveFloorTopologyFixture`, the elevator shaft occupies discrete grid cells `[0, 1]` spanning world coordinates `X = [-2.40f, -1.40f]` (center `-1.90f`, width `1.00f`).
  - Previously, `EnsureShaftViews()` positioned guide rails at `X = -2.45f` and `X = -1.35f` (a 0.05f misalignment with adjacent room boundaries at `-2.40f` and `-1.40f`), while the shaft cavity width was set to `1.20f` (overlapping into adjacent rooms).
  - Furthermore, `EnsureFloorViews()` rendered a solid horizontal line across the entire floor width at `Z = 0.0f` (in front of the shaft cavity at `Z = 0.8f`), drawing concrete floor lines straight through the elevator shaft on every single floor, breaking visual vertical continuity.
- **Fix:**
  - Recalibrated elevator shaft bounds to precisely match discrete grid cells `[0, 1]`: center `X = -1.90f`, width `1.00f`.
  - Added vertical structural columns at `X = -2.40f` and `X = -1.40f` framing the shaft and perfectly meeting the adjacent room dividing walls.
  - Placed inner steel guide rails at `X = -2.36f` and `X = -1.44f`, running continuously from the foundation buffer to the rooftop penthouse machine room.
  - Updated `EnsureFloorViews()` to split the horizontal floor baseline into Left (`worldLeft` to `-2.40f`) and Right (`-1.40f` to `worldRight`) segments, ensuring the elevator shaft remains a 100% continuous, unobstructed vertical opening from bottom to top.
  - Added a rooftop machine penthouse cap and foundation buffer pit.

### 2. Apartment Visual Identity & Visibility ("No Apartment to See")
- **Root Cause:**
  - Residential apartment quads were previously rendered with a flat dark navy color `new Color(0.14f, 0.20f, 0.29f)` on top of floor slabs with color `new Color(0.09f, 0.12f, 0.18f)`. On monitors, these dark tones were almost indistinguishable, making built apartments look identical to bare unbuilt slabs.
  - Units lacked interior distinguishing architectural features (no flooring, doors, windows, molding, or room numbers).
- **Fix:**
  - **Cozy Residential Wallpaper**: Upgraded apartment interior backdrop to high-contrast warm slate `new Color(0.20f, 0.26f, 0.36f)`.
  - **Warm Hardwood Flooring**: Added honey-oak floorboard trim `new Color(0.42f, 0.30f, 0.20f)` along the bottom of every unit.
  - **Illuminated Windows**: Added a framed window in each unit with warm amber lamp light `new Color(0.92f, 0.82f, 0.48f)` and structural mullions.
  - **Apartment Entrance Doors**: Added framed entrance doors with panels and brass doorknobs at room entrances.
  - **Unit Signage & Trim**: Added molded ceiling trim and unit plaques near doors.
  - **Structural Room Dividers**: Prominent dividing pillars `new Color(0.38f, 0.48f, 0.62f)` separating adjacent apartments.
  - **Commercial Diner & Lobby Enhancements**: Added terracotta dining counter and neon awning to Floor 0 Diner; added reception desk and polished floor to Floor 0 Lobby.

### 3. Discrete Grid Coordinate Alignment in `GridPlacementController`
- **Root Cause:**
  - `CellToWorld` and `TryGetCellFromWorld` used an inconsistent half-cell offset calculation that resulted in misaligned ghost preview snapping relative to `EnsureRoomViews`.
- **Fix:**
  - Standardized `CellToWorld` to compute `worldLeft = _cellOriginX + cellX * _cellWidth` and `worldRight = _cellOriginX + (cellX + widthInCells) * _cellWidth`, with center `(worldLeft + worldRight) * 0.5f`.
  - Configured `TryGetCellFromWorld` to use `Mathf.FloorToInt((worldPos.x - _cellOriginX) / _cellWidth)` for exact cell indexing.

---

## Changed Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Presentation | Enhanced shaft alignment, split floor lines around shaft, added architectural apartment visuals (windows, wood floors, doors, unit plaques, trim). |
| `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs` | Presentation | Synchronized discrete grid coordinate mapping and cell bounding math. |
| `Handoffs/Active/BUGFIX_2026-09-17_cutaway-visual-alignment-and-apartment-identity.md` | Documentation | This handoff document. |
