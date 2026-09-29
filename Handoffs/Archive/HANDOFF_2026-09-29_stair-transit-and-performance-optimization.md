# OR-1003B — Stair Transit Bugfix & Presentation Performance Optimization

## Delivered

1. **Stairwell Navigation & Elevator Decongestion**:
   - **Short Trip Preference**: In `ScheduleTripGenerator.cs`, short trips (1–2 floors delta) natively attempt routing via a dedicated stairs planner (`_stairsPlanner`) first, avoiding unnecessary elevator reliance.
   - **Queue Overflow & Impatience Diverting**: In `TransitExecutionSystem.cs`, queued passengers divert to stairs if elevator wait times become excessive (`WaitTicks >= 15`, or `>= 5` for short hops) or when elevator floors suffer long queues (`queueLength >= 3`).
   - **Clean Passenger Dequeueing**: Added `ElevatorBank.TryRemoveQueuedPassenger(EntityId personId, out ElevatorPassenger passenger)` to cleanly dequeue diverted passengers without state corruption.
   - **Stair Movement Interpolation Fix**: In `TransitExecutionSystem.cs`, stair walking legs previously assumed horizontal movement delta; zero delta clamped progress to 0 and locked residents to the lower floor. Fixed progress calculation using `leg.Cost` with lateral swaying at stairwell X=-13.2 (`Math.Sin(progress * Math.PI) * 0.5f`) and switching `CurrentFloor` halfway through vertical flight.

2. **NullReferenceException Fix**:
   - In `GoldCityEnvironment.cs`, domain reloads in Play Mode cleared non-serialized fields (`_properties`), causing unhandled `NullReferenceException` in `UpdateLighting()` every 0.2s (1,193+ error logs). Added null-coalescing `_properties ??= new MaterialPropertyBlock()` and renderer array guards. Console errors reduced to 0.

3. **Rendering & Frame Rate Performance Optimization**:
   - **Shader Multi-Pass Duplication**: `OneRoofUnlit.shader` previously defined redundant passes (`UniversalForward`, `UniversalForwardOnly`, `Universal2D`), causing URP's Forward Renderer to draw every object twice and breaking SRP Batcher compatibility on forward passes. Stripped down to the single canonical `SRPDefaultUnlit` pass, achieving 100% SRP Batcher compatibility (`Pass0 = OK ()`).
   - **GPU Instancing on NPC Materials**: `Npc_DefaultSharedMaterial` and `PooledNpc_SharedMaterial` in `NpcSkeletalHierarchy.cs` and `NpcViewPool.cs` had `enableInstancing = false`, forcing 3,120 SpriteRenderer components to render as unbatched draw calls. Enabled `enableInstancing = true`.
   - **Frame Loop Gating**: In `TowerPlayableController.cs`, guarded expensive projection refreshes (`_room.UpdateHousingConditions`, soundscape, lighting, weather) behind `tickAdvanced`, eliminating per-frame overhead.

## Performance & Live Simulation Metrics

Validated live in Unity 6000.3.24f1 playing `Tower_GoldStandard30` via Pipeline MCP (port 7800):

| Metric | Before Fix | After Fix | Delta |
|---|---|---|---|
| **Draw Calls** | 14,585 | 171 | **-98.8%** |
| **Batches** | 7,574 | 86 | **-98.9%** |
| **SetPass Calls** | 14,498 | 161 | **-98.9%** |
| **CPU Main Thread Frame Time** | 259.27 ms (~4 FPS) | 9.27 ms (~108 FPS) | **>25x speedup** |
| **Elevator Queue Length** | 232 queued | 42 queued | **-81.9%** |
| **Residents Using Stairs** | 0 | 92 concurrent | **Fixed** |
| **Console Errors** | 1,193 | 0 | **Fixed** |

## Test Suite Results

- `OneRoof.Domain.Tests.EditMode`: **303 passed / 0 failed** (1 explicit benchmark skipped).
- Presenter tests (`ElevatorBankPresenterTests`, `ElevatorWaitOverlayPresenterTests`): **10 passed / 0 failed**.
- Pure C# Domain boundary preserved: 0 UnityEngine references in `OneRoof.Domain`.

## Next Safe Action

Proceed with normal game evaluation and profiling. The running game is smooth, responsive, free of console errors, and NPCs actively use stairs throughout the tower.
