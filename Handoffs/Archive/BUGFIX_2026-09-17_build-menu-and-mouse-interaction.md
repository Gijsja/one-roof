# Handoff — Build Menu Mouse Click Interaction & Placement Preview Repair

**Session date:** 2026-09-17  
**Task scope:** Fix build menu mouseclick interaction, enable interactive tool selection palette in ModeShellBarController, resolve Camera.main null reference blocking grid hover/placement, prevent UI click passthrough, and support right-click cancellation.

---

## Validation Status

- **Unity Compilation:** PASSED — Code recompiled cleanly with 0 compiler errors.
- **Unit Tests:** EditMode tests added for:
  - Build Palette selection and preservation (`ModeShellBarControllerTests`).
  - UI filtering and Camera fallback (`GridPlacementControllerTests`).
  - Domain validation rules: duplicate floor slabs, unsupported upper slabs, elevator shaft floor/column alignment, elevator car bank capacity limit, and invalid placement rejection (`GridPlacementControllerTests`).
- **Interactive Mouse Controls:**
  - Clicking **Build [B]** opens the dedicated Build Palette sub-menu directly above the Mode bar.
  - Automatically selects `residential:apartment` if no tool is active upon entering Build mode.
  - Clicking any palette button (`Studio Apt`, `Diner`, `Shaft`, `Elevator Car`, `Floor Slab`) selects and highlights the tool.
  - Clicking the active tool button toggles it off.
  - Hovering over grid cells shows the dynamic ghost preview with valid (green) or invalid (red) feedback.
  - Left-clicking on valid grid cells executes placement and updates world geometry immediately.
  - Placements are strictly blocked when invalid (red ghost) — clicking when red cannot place objects.
  - After placing an object, the red overlap ghost is suppressed while the cursor lingers on the newly placed object, cleanly reappearing once the cursor moves to another cell.
  - Right-clicking with the mouse cancels/deselects the active build tool without requiring the keyboard.
  - Clicking on UI windows, buttons, and inspection cards is filtered out (`IsPointerOverUI`), preventing accidental placement clicks through the UI into the world.

---

## Root Causes & Implemented Solutions

### 1. `Camera.main` Returned `null`, Permanently Blocking Grid Raycasts
- **Root Cause:**
  - `TowerPlayableController.UpdateCamera()` dynamically created `"Tower Camera"`, but left it untagged (`"Untagged"` instead of `"MainCamera"`).
  - `GridPlacementController.TryGetCellFromScreen()` queried `Camera.main`. Because the camera was untagged and `_gridPlacement.Camera` was never explicitly assigned, `cam` evaluated to `null`, causing `TryGetCellFromScreen` to return `false` on every single frame.
  - Grid cell hover, ghost rendering, and left-click placements were completely inert.
- **Fix:**
  - Tagged `"Tower Camera"` with `"MainCamera"` in `TowerPlayableController.UpdateCamera()`.
  - Added explicit wiring `_gridPlacement.Camera = cam;` during initialization and camera updates.
  - Upgraded `GridPlacementController.Camera` property to fall back to `FindAnyObjectByType<Camera>()` if `Camera.main` is untagged or null.

### 2. Missing Build Palette / Menu in `ModeShellBarController`
- **Root Cause:**
  - Clicking `Build [B]` on the bottom navigation bar switched mode to `InteractionMode.Build`, but `ModeShellBarController` had no tool buttons or palette UI.
  - `SelectedBuildTool` remained `null`, leaving the player with no visual build options and no active tool.
  - The only build buttons were three small text buttons buried in the top-left debug HUD.
- **Fix:**
  - Added a horizontal Build Palette directly above the bottom mode bar in `ModeShellBarController`:
    - `Studio Apt ($8k, 6 cells)`
    - `Diner ($12k, 10 cells)`
    - `Shaft ($3k/fl, 2 cells)`
    - `Elevator Car ($15k, 2 cells)`
    - `Floor Slab ($5k, 24 cells)`
  - Added auto-selection of `residential:apartment` when entering Build mode if no tool is active.
  - Added active tool highlighting and toggle-off on re-click.
  - Shifted the status context overlay above the palette to prevent overlapping.
  - Added `+ Diner` to the top-left HUD so both menus remain consistent.

### 3. Red Ghost Placement & Incomplete Placement Validation
- **Root Cause:**
  - `GridPlacementController.Update()` previously invoked `TryExecutePlacement()` on primary pointer down regardless of whether `isValid` was true.
  - `ValidatePlacement()` only checked treasury affordability for floor slabs, elevator shafts, and elevator cars, omitting domain validation rules:
    - Floor slabs could be placed on existing floors (duplicate) or in mid-air (unsupported).
    - Elevator shafts could be placed at floor 0 (invalid floor range) or misaligned from the elevator column.
    - Elevator cars could be placed beyond the bank maximum capacity (4 cars) or outside shaft columns.
    - Rooms were not checked for continuous lower slab support.
  - Additionally, after successfully placing an object, the cursor remains at the same coordinates. On the next frame, `ValidatePlacement` detected overlap with the newly placed object, instantly rendering a red error ghost directly over the fresh room.
- **Fix:**
  - Enforced strict placement blocking in `Update()`: `if (isValid && TryExecutePlacement(...))` so clicks when `isValid == false` (red ghost) are ignored.
  - Guarded `TryExecutePlacement()` with `if (!ValidatePlacement(...)) return false;` to guarantee commands are never executed for invalid configurations.
  - Aligned `ValidatePlacement()` with all domain rules:
    - `floor:slab`: Rejects `floor < 0`, duplicate floor slabs, and unsupported upper slabs without a slab below.
    - `transit:elevator_shaft`: Rejects `floor <= 0`, column misalignment outside `[0..1]`, missing slabs, and duplicate shafts.
    - `transit:elevator_car`: Rejects out-of-range floors, column placement outside shaft corridor, and car counts exceeding `ElevatorPlacementPredictor.MaxCarsPerBank` (4 cars).
    - Rooms: Rejects rooms without continuous lower floor slab support.
  - Added cell-based ghost suppression (`_suppressedCellFloor`, `_suppressedCellX`): upon successful placement, the red error ghost is suppressed while hovering the placed cell, cleanly reappearing once the cursor moves to a new cell.

### 4. Floor Slab Alignment & Snapping to Tower Column
- **Root Cause:**
  - `floor:slab` was hardcoded to a 24-cell width and used the raw hovered `cellX`, which floated left and right with the mouse.
  - Slabs placed on floor 5 were arbitrarily offset, only 24 cells wide (instead of the tower's standard 31 cells `[-14..16]`), and broke symmetry with the 5 floors below.
  - Off-center slabs prevented residential apartments (like Apartment 1 at `[-12..-7]`) from fitting, blocking construction on the newly added floor.
- **Fix:**
  - Added `TryGetToolPlacementBounds` to `GridPlacementController`:
    - `floor:slab` automatically snaps to the tower's structural column (`[-14..16]`, 31 cells), matching the lower floor slab below it.
    - `transit:elevator_shaft` and `transit:elevator_car` snap to the central elevator chute column `[0..1]`.
  - Updated `Update()` and `TryExecutePlacement()` to position ghosts and build commands with snapped bounds.
  - Updated Build Palette button label in `ModeShellBarController` to `Floor Slab ($3.1k, 31c)` to accurately reflect the 31-cell footprint and cost.

### 5. Domain Reload NullReferenceException in `PlacementGhostPresenter`
- **Root Cause:**
  - Upon script recompilation or playmode domain reload, non-serialized C# objects (`_colorBlock`) reset to `null` while scene GameObjects (`_ghostObject`) remained.
  - `EnsureGhostObject` skipped initialization if `_ghostObject != null`, leaving `_colorBlock` null and throwing a `NullReferenceException` at line 73 (`_colorBlock.SetColor`), interrupting `Update()`.
- **Fix:**
  - Hardened `EnsureGhostObject` to independently check and initialize `_colorBlock`, `_ghostMaterial`, `_ghostObject`, and `_ghostRenderer`.

### 6. UI Click Passthrough into World Grid
- **Root Cause:**
  - `GridPlacementController.Update()` checked `mouse.leftButton.wasPressedThisFrame` directly. When clicking on UI buttons, the raycast could hit grid cells behind the UI simultaneously.
- **Fix:**
  - Implemented `GridPlacementController.IsPointerOverUI(screenPos)` filtering against `EventSystem` and active IMGUI rects (bottom bar, build palette, top-left HUD, inspection cards).

### 7. Right-Click Cancellation
- **Root Cause:**
  - Canceling placement or deselecting a tool previously required pressing `[Escape]` on the keyboard.
- **Fix:**
  - Added `IsSecondaryPointerDown()` to `GridPlacementController`, calling `_modeSession.CancelOrEscape()` on right-click to deselect tools and hide the placement ghost cleanly.

---

## Changed Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs` | UI | Added interactive Build Palette sub-menu, auto-tool selection, tool toggle-off, updated Floor Slab label to $3.1k (31c), and status overlay positioning. |
| `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs` | Presentation | Added Camera fallback, pointer-over-UI filtering, right-click cancellation, strict invalid placement blocking, comprehensive domain validation, post-placement ghost suppression, and `TryGetToolPlacementBounds` for structural tower snapping (31-cell slabs, column [0..1] shafts). |
| `Assets/OneRoof/Runtime/Presentation/Tower/PlacementGhostPresenter.cs` | Presentation | Hardened `EnsureGhostObject` against domain reload `NullReferenceException` by independently initializing `_colorBlock`, `_ghostMaterial`, `_ghostRenderer`, and `_ghostObject`. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Presentation | Tagged `"Tower Camera"` with `"MainCamera"`, wired camera to grid placement, added Diner button to HUD, updated shortcut hints. |
| `Assets/OneRoof/Tests/EditMode/UI/ModeShellBarControllerTests.cs` | Tests | Added tests for Build mode request auto-selection and tool preservation. |
| `Assets/OneRoof/Tests/EditMode/Presentation/GridPlacementControllerTests.cs` | Tests | Added tests for `IsPointerOverUI` region filtering, Camera fallback, floor slab validation, elevator shaft alignment/floors, elevator car max capacity, invalid placement rejection, and tool placement bounds snapping. |
| `Handoffs/Active/BUGFIX_2026-09-17_build-menu-and-mouse-interaction.md` | Documentation | This handoff document. |

---

## Next Safe Action

Return to Unity Editor, enter Play Mode in `Assets/Scenes/Tower.unity`, click **Build [B]** at the bottom, select **Floor Slab**, and verify:
1. Hovering over Floor 5 previews a full-width 31-cell green ghost perfectly aligned with the tower column `[-14..16]`.
2. Left-clicking places the Floor 5 slab; the tower view expands smoothly to 6 floors.
3. Selecting **Studio Apt** and hovering over Floor 5 allows placing all standard apartments (`[-12..-7]`, `[-6..-1]`, `[2..7]`, `[8..13]`).
4. Selecting **Shaft** snaps to `[0..1]` and extends the elevator shaft chute to Floor 5.
5. Right-click deselects the current tool.
