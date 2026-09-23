# Handoff — Camera Zoom & Pan Preservation

**Session date:** 2026-09-18  
**Task scope:** Resolve the bug where camera zoom and pan are clobbered every frame in Play Mode, restoring interactive WASD pan, scroll zoom, and overview focus.

---

## Validation Status

- **Unity Compilation:** PASSED — 0 compiler errors, 0 compiler warnings.
- **EditMode Tests:**
  - `TowerCameraControllerTests`: 5 / 5 passed (100%).
  - `TowerPlayableControllerTests`: 7 / 7 passed (100%).
- **PlayMode Live Verification:** PASSED — Zoomed to `orthographicSize = 3.4` and panned to `(0.40, 1.80, -10.00)`; verified position and zoom persisted across simulation frames. Verified `FocusOverview()` restored overview bounds.

---

## Diagnosed Root Cause and Solution

### Problem
1. In `TowerPlayableController.cs:153`, `RenderVisualSnapshot()` called `UpdateCamera()` on every single frame.
2. Inside `TowerCameraController.cs:85-88`, `EnsureTowerCamera()` unconditionally reassigned `cam.orthographicSize` and `cam.transform.position` to default overview values.
3. As a result, whenever the user panned (WASD/middle-click drag) or zoomed (mouse scroll wheel), the camera was instantly snapped back to default overview on the subsequent frame.

### Solution
1. **Remove `UpdateCamera()` from `RenderVisualSnapshot()`**: The per-frame rendering loop only needs to sync dynamic presenter views (`_elevator.UpdateElevatorPositions`, `_resident.UpdateResidentPositions`).
2. **Add `resetView` Parameter to `EnsureTowerCamera()`**:
   - Signature: `EnsureTowerCamera(int floorCount, GridPlacementController gridPlacement = null, bool resetView = false)`
   - Only mutates `cam.transform.position` and `cam.orthographicSize` if the camera is newly instantiated (`isNew`) or if `resetView == true`.
   - Still updates `ctrl.SetOverviewDefaults()` and `ctrl.SetBounds()` so building expansions dynamically expand reachable pan/zoom bounds without interrupting the user's current view.
3. **Targeted Reset Calls**:
   - `CreateWorldGeometry()` and initial setup pass `resetView: true`.
   - `OnPlacement()` passes `resetView: false`.
4. **Unit Tests**:
   - Added `EnsureTowerCamera_PreservesPositionAndZoom_WhenResetViewIsFalse` verifying position and zoom remain intact across floor count changes while bounds expand.
   - Added `EnsureTowerCamera_ResetsPositionAndZoom_WhenResetViewIsTrue` verifying explicit reset restores overview defaults.

---

## Changed Files Summary

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerCameraController.cs` | Presentation | Added `resetView` parameter to `EnsureTowerCamera`; only snaps position/zoom when new or explicitly requested. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Presentation | Removed `UpdateCamera()` from `RenderVisualSnapshot()`; passed `resetView` appropriately on init, reset, and placement. |
| `Assets/OneRoof/Tests/EditMode/Presentation/TowerCameraControllerTests.cs` | Tests | Added unit tests verifying view preservation and reset behavior. |
