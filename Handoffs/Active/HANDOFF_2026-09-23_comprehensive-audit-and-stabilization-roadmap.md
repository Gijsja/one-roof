# Comprehensive Architecture, Quality, Reliability & Security Audit Handoff and Stabilization Roadmap

**Status:** ACTIVE  
**Date:** 2026-09-23  
**Auditors:** 6 Specialized Research Subagents (Security, Code Quality, Bugs & Logic, Concurrency & Race, Test Reliability, Maintainability)  
**Target Codebase:** `/home/geisha/Vibecode/UnityAI/one-roof`  

---

## 1. Executive Summary & Objective

This handoff captures the complete, unabridged results of a forensic codebase audit across six critical axes:
1. **Security Issues** (Path traversal, DoS/resource exhaustion, input validation, save integrity)
2. **Code Quality** (Assembly boundaries, dead subsystems, artificial minification, allocations, DRY)
3. **Bugs & Logic Flaws** (30-floor scale blocker, transit soft-locks, node shift, economic leaks)
4. **Concurrency, Threading & Race Conditions** (Lifecycle deadlocks, static dangling references, IMGUI fall-through)
5. **Test Flakiness & Reliability** (PlayMode concurrent tick races, static Unity object leaks, float precision)
6. **Maintainability & Technical Debt** (Hot-path GC churn, flat Dijkstra bloat, OCP string checks, lack of ScriptableObjects)

While One Roof exhibits an exceptionally clean architectural boundary (`OneRoof.Domain` is 100% pure C# with 0 `UnityEngine` references and `"noEngineReferences": true`), the current implementation **cannot achieve the North Star Beta Boundary (30 floors, 300 residents, physical utilities, 8 overlays, <4 ms tick, 60 FPS presentation)** without executing the stabilization roadmap detailed herein.

Below is the complete registry of all 49 audited findings across all 6 domains, followed by a prioritized, sequenced, and executable 6-phase engineering roadmap with exact files, line numbers, and acceptance tests.

---

## 2. Complete Unabridged Audit Findings Registry

### 2.1. Security Issues (SEC-01 through SEC-09)

* **SEC-01 [High Severity] Arbitrary File Read, Write, and Deletion via Path Traversal**
  * **File**: [`Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs:17-49`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs#L17-L49)
  * **Symbols**: `AtomicFileSaveStore.Save`, `AtomicFileSaveStore.Load`, `AtomicFileSaveStore.Delete`
  * **Root Cause**: Accepts arbitrary `filePath` strings without canonicalization or validation against an authorized base directory (e.g., `Application.persistentDataPath` or `Saves/`). Traversal paths (`../../../../path`) permit arbitrary reading, overwriting, and deletion across the filesystem.
  * **Remediation**: Implement `TryGetSafeSavePath` constraining paths to `Path.GetFullPath(Application.persistentDataPath)`. Reject paths containing traversal sequences or invalid path characters.

* **SEC-02 [High Severity] Denial of Service via Integer Overflow & Infinite Loop in `ElevatorBank`**
  * **File**: [`Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:39-42, 68-74, 518-520`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs#L39-L74)
  * **Symbols**: `ElevatorBank.ElevatorBank`, `ElevatorBank.ExpandFloorRange`, `ElevatorBank.FromSaveData`
  * **Root Cause**: When `maxFloor == int.MaxValue`, the loop `for (var floor = minFloor; floor <= maxFloor; floor++)` wraps around to `int.MinValue` on overflow, resulting in an infinite loop that allocates `Queue<ElevatorPassenger>` until an `OutOfMemoryException` crashes the process. Furthermore, `FromSaveData` lacks bounds checks on deserialized `maxFloor`.
  * **Remediation**: Enforce strict architectural bounds (e.g. `minFloor >= -10 && maxFloor <= 100`). Prevent integer overflow by checking `floor == maxFloor` before incrementing.

* **SEC-03 [Medium Severity] Predictable Temporary File Naming & Symlink Race Condition**
  * **File**: [`Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs:24-47`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs#L24-L47)
  * **Symbols**: `AtomicFileSaveStore.Save`, `AtomicFileSaveStore.Delete`
  * **Root Cause**: Temporary files use deterministic `filePath + ".tmp"`. `File.WriteAllText` follows symlinks. In shared directory environments, an attacker can create a symlink at `<target>.tmp` to truncate and corrupt arbitrary files. Time-of-check to time-of-use (TOCTOU) gap between `File.Exists` and `File.Move` also risks unhandled `IOException`.
  * **Remediation**: Use randomized GUID temporary names (`Path.Combine(dir, $"{fileName}.{Guid.NewGuid():N}.tmp")`) opened with `FileStream(..., FileMode.CreateNew)`.

* **SEC-04 [Medium Severity] Unhandled ArgumentException Crash in `TowerSimulation.CanExecute`**
  * **File**: [`Assets/OneRoof/Runtime/Domain/TowerSimulation.cs:172-182`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L172-L182)
  * **Symbols**: `TowerSimulation.CanExecute(ICommand)`
  * **Root Cause**: Evaluating `Economy.CalculateFloorSlabCost(slabCmd.Bounds)` accesses `.Bounds` before delegating to `Topology.CanExecute`. In `BuildFloorSlabCommand`, `BuildRoomCommand`, and `ExpandGroundSlabCommand`, `.Bounds` invokes `new CellBounds(floor, minX, maxX)`. [`CellBounds`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Topology/CellBounds.cs#L8-L12) throws an unhandled `ArgumentException` if `minX > maxX`, crashing UI hover inspection instead of returning a clean rejection.
  * **Remediation**: Validate `cmd.MinX <= cmd.MaxX` in `CanExecute` before evaluating `.Bounds`.

* **SEC-05 [Medium Severity] NullReferenceException Halting Simulation on Corrupted Active Trips**
  * **File**: [`Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs:168-198, 318-324`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs#L168-L198)
  * **Symbols**: `TransitExecutionSystem.RestoreFromSaveData`, `TransitExecutionSystem.Advance`
  * **Root Cause**: If a saved active trip has no valid route (`trip.PlannedRoute == null`), `RestoreTripExecution` unconditionally enqueues it. On the subsequent simulation tick, `Advance()` evaluates `execution.CurrentLegIndex >= trip.PlannedRoute.Legs.Count`, crashing the master simulation loop.
  * **Remediation**: Add defensive null-handling: discard trips with null routes upon load, and verify `trip.PlannedRoute?.Legs != null` in `Advance()`.

* **SEC-06 [Low Severity] Integer Overflow in Economy Construction Formulas**
  * **File**: [`Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs:101-104, 136-139`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs#L101-L104)
  * **Symbols**: `CalculateFloorSlabCost`, `CalculateElevatorShaftCost`, `CalculateRoomCost`
  * **Root Cause**: Cost calculations return 32-bit signed `int` values while `CashBalance` is `long`. Extremely large spans or widths can overflow to negative integers, bypassing affordability checks.
  * **Remediation**: Perform calculations using 64-bit `long` within `checked` expressions.

* **SEC-07 [Low Severity] Room Demolition Leaves Residents in Phantom Trapped State**
  * **Files**: [`Assets/OneRoof/Runtime/Domain/TowerSimulation.cs:500-532`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L500-L532), [`TransitExecutionSystem.cs:420-425`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs#L420-L425)
  * **Root Cause**: When a room is demolished, residents inside or in transit to it complete trips to `WorldLocation.InRoom(demolishedRoomId)`. In subsequent ticks, transit node lookups return null and residents are permanently frozen.
  * **Remediation**: Relocate residents of demolished rooms to Ground Floor Lobby/Outside and cancel active trips targeting the demolished room.

* **SEC-08 [Low Severity] Lack of Cryptographic Integrity on Save Envelopes**
  * **File**: [`Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs)
  * **Root Cause**: Save files are unauthenticated plaintext JSON (`EnvelopeDto`). While `JsonUtility` prevents RCE, save tampering (modifying treasury, IDs, coordinates) is undetected.
  * **Remediation**: Append an HMAC-SHA256 checksum to save envelopes to detect tampering before deserialization.

* **SEC-09 [Informational] Secret Management & Memory Safety Verification (PASS)**
  * **Status**: **CLEAN**. Zero hardcoded credentials, API keys, or private certificates exist in version control. All 16 `.asmdef` files set `"allowUnsafeCode": false`. Zero occurrences of `unsafe`, `DllImport`, or unmanaged pointers exist. `AllIn1SpriteShader` contains pure C# scripts and HLSL shaders without native compiled binaries.

---

### 2.2. Code Quality Issues (CQ-01 through CQ-08)

* **CQ-01 [Critical Severity] Dead / Orphaned Resident View Pooling Subsystem**
  * **Files**: [`NpcPopulationPresenter.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Population/NpcPopulationPresenter.cs), [`NpcViewPool.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Population/NpcViewPool.cs), [`NpcView.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Population/NpcView.cs), [`PopulationProjectionService.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Application/Population/PopulationProjectionService.cs), [`TowerPlayableController.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs)
  * **Root Cause**: `Docs/02_ARCHITECTURE.md` mandates a pooled 40–60 NPC view cap. A full pooling infrastructure was authored, but is completely bypassed by `TowerPlayableController`. Instead, [`TowerResidentPresenter.cs:152-167`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs#L152-L167) instantiates 1 unpooled GameObject per resident with zero camera frustum culling. At 300 residents, 300 GameObjects and 2,400 SpriteRenderers exist simultaneously.
  * **Remediation**: Wire `TowerResidentPresenter` to use `NpcViewPool` or integrate the pooling logic directly, enforcing the 40–60 view cap and culling off-screen residents.

* **CQ-02 [High Severity] Artificial Line-Count Compliance via Code Minification in `TowerPlayableController`**
  * **File**: [`TowerPlayableController.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs)
  * **Root Cause**: Backlog task `ARCH-002` mandated `<= 150 lines`. To meet this metric, lines 31, 53, 86–88, 138–146, and 235 crammed multiple statements, variable declarations, lifecycle hooks, and loops onto single lines. The file is 239 compressed lines; formatted normally, it exceeds 380 lines.
  * **Remediation**: Unpack minified statements into standard C# conventions. Decompose `TowerPlayableController` into dedicated coordinators (`TowerInputCoordinator`, `TowerOverlayCoordinator`, `TowerGeometryCoordinator`).

* **CQ-03 [High Severity] Steady-State Allocations & Native Memory Leaks in Presentation Loop**
  * **Files**: [`TowerResidentPresenter.cs:175-176`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs#L175-L176), [`TowerResidentPresenter.cs:372-374`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs#L372-L374), [`TowerDataOverlays.cs:34-39`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Application/Overlays/TowerDataOverlays.cs#L34-L39), [`NpcSkeletalHierarchy.cs:320, 735-741`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs#L320)
  * **Root Cause**:
    1. `UpdateResidentPositions()` allocates two new `int[]` arrays every frame (120 allocations/sec at 60 FPS).
    2. Executes `GetComponent<VisualEffectsPresenter>()` every frame for each resident (~3,000 calls/sec at 50 residents; ~18,000 calls/sec at 300 residents).
    3. `TowerDataOverlays` regenerates uncached projections (`Satisfaction`, `Population`, `Scrutiny`, `FootTraffic`, `BusinessHealth`, `Utilities`) every frame an overlay is active.
    4. `CreateWardrobeSwatch` creates procedural `Texture2D` instances that are never destroyed, leaking native memory.
  * **Remediation**: Preallocate array buffers in `TowerResidentPresenter`; cache `VisualEffectsPresenter` on `NpcSkeletalHierarchy`; memoize all overlays by `(tick, version)`; pool/share 2x2 textures and implement `OnDestroy()`.

* **CQ-04 [Medium Severity] Hardcoded 5-Floor Screen Coordinate Formulas**
  * **Files**: [`ElevatorWaitOverlayPresenter.cs:71-76`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Overlays/ElevatorWaitOverlayPresenter.cs#L71-L76), [`BusinessHealthOverlayPresenter.cs:54`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Overlays/BusinessHealthOverlayPresenter.cs#L54), [`PopulationOverlayPresenter.cs:54`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Overlays/PopulationOverlayPresenter.cs#L54)
  * **Root Cause**: `FloorScreenY` computes `normalized = (4 - floor) / 4.5f`. At 30 floors, `(4 - floor)` becomes negative (`-26`), rendering badges far above the screen. Card lists also render at fixed pixel offsets without scrollviews.
  * **Remediation**: Use dynamic `maxFloor` or world-to-screen projections; wrap floor lists in IMGUI scrollviews.

* **CQ-05 [Medium Severity] Pre-Existing Presentation Test Failures Caused by Real Bugs**
  * **Files**: [`RoomPresenter.cs:116, 295-299`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs#L116), [`TowerStructurePresenter.cs:39`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerStructurePresenter.cs#L39)
  * **Root Cause**:
    1. In `RoomPresenter.HasMatchingTheme`, authored rooms lacking a `RoomBackdropPresenter` return false and are destroyed as stale.
    2. In `TowerStructurePresenter.Initialize`, `AdoptSingle` calls `_parent.Find(...)` which finds only the first matching child, leaving duplicate slabs in the hierarchy.
  * **Remediation**: Fix `HasMatchingTheme` to adopt rooms without backdrops, and make `AdoptSingle` iterate and destroy all extra matching children.

* **CQ-06 [Medium Severity] Double Training Tick Advancement in `SpecialistRoleSystem`**
  * **File**: [`SpecialistRoleSystem.cs:41-65`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Population/SpecialistRoleSystem.cs#L41-L65)
  * **Root Cause**: In `TrainForRole`, newly selected candidates advance once in the candidate loop, and advance a second time in the subsequent trainee loop during the exact same tick.
  * **Remediation**: Exclude newly selected candidates from the subsequent loop so all trainees advance exactly once per tick.

* **CQ-07 [Medium Severity] Code Duplication in Coordinate Transforms & Tool Metadata**
  * **Files**: [`ModeShellBarController.cs:173-224`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs#L173-L224), [`GridPlacementController.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs), and 8 Presenter classes
  * **Root Cause**: The expression `-2.4f + cellX * 0.5f + 0.25f` is duplicated across 8 classes. Tool costs in `ModeShellBarController` are hardcoded strings that drift from `BuildingCatalog` and `TowerEconomyState`.
  * **Remediation**: Extract a centralized `TowerGridCoordinates` utility; query `BuildingCatalog` dynamically for UI tool buttons.

* **CQ-08 [Low Severity] Missing Nullable Reference Types & Swallowed Exceptions**
  * **Files**: Entire codebase; [`AtomicFileSaveStore.cs:109`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs#L109)
  * **Root Cause**: `#nullable enable` is not enabled in any runtime file. `AtomicFileSaveStore.Delete` catches all exceptions and silently returns `false` without logging.
  * **Remediation**: Enable `#nullable enable` across `OneRoof.Domain` and log file I/O exceptions in `Infrastructure`.

---

### 2.3. Bugs & Logic Flaws (BUG-01 through BUG-13)

* **BUG-01 [Critical Severity] Mathematical Blackout at 30 Floors (North Star Blocker)**
  * **File**: [`ElectricalGridState.cs:15-16, 43`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/ElectricalGridState.cs#L15-L43)
  * **Bug**: `DefaultRiserLossPerFloor = 0.04f`. Voltage on floor $F$ is $1 - 0.04 \times F$. At Floor 6, voltage drops to $0.76\text{V} < 0.80\text{V}$ (triggering permanent brownout). At Floor 25, voltage reaches $0.00\text{V}$ (total blackout). Floors 6–30 cannot be powered at Beta scale because no booster transformer room exists.
  * **Remediation**: Add a step-up transformer / booster room (similar to `FindHighestBoosterFloor` in `WaterWasteNetworkState`), or rescale `DefaultRiserLossPerFloor` to `0.005f`.

* **BUG-02 [Critical Severity] Permanent Resident Soft-Lock in Zero-Car Elevator Shafts**
  * **Files**: [`HierarchicalTransitGraph.cs:191-207`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs#L191-L207), [`TransitExecutionSystem.cs:350-377`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs#L350-L377)
  * **Bug**: The transit graph generates elevator edges whenever shaft portals exist, even if `ElevatorBank.Cars.Count == 0`. Residents route to the elevator, enqueue, and are stuck forever with no wait timeout or stair fallback.
  * **Remediation**: Only generate elevator edges if `ElevatorBank.Cars.Count > 0`. Add an elevator wait timeout (e.g., 120 ticks) that cancels or re-routes stranded commuters via stairs.

* **BUG-03 [Critical Severity] Ephemeral Node IDs Shift In-Flight Routes**
  * **File**: [`HierarchicalTransitGraph.cs:119-136`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs#L119-L136)
  * **Bug**: Rebuilding the transit graph assigns node IDs sequentially starting at 1000 (`new EntityId(nextNodeId++)`). Active trips hold prior node IDs; if a portal is built or demolished on a lower floor, all subsequent node IDs shift, causing residents in transit to route to wrong floors or teleport.
  * **Remediation**: Derive `TransitNode.Id` deterministically from the persistent `portal.Id`: `new TransitNode(portal.Id, ...)`.

* **BUG-04 [Critical Severity] Complete Absence of Closed-Loop Cash Conservation**
  * **Files**: [`TowerEconomyState.cs:146-166`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs#L146-L166), [`BusinessState.cs:135-147`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Economy/BusinessState.cs#L135-L147), [`TowerSimulation.cs:135-140`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L135-L140)
  * **Bug**: Rent is minted directly into treasury cash without deducting anything from households. Businesses mint revenue out of thin air. Settlement runs every 50 ticks (~29 times per in-game day) instead of daily (1440 ticks), inflating currency.
  * **Remediation**: Implement `ECON-001..004`: deduct rent from household `long CashBalance`, deduct commercial rent from businesses, and synchronize settlement to `tick % 1440 == 0`.

* **BUG-05 [High Severity] Grid Placement Off-By-One Hit Testing**
  * **File**: [`GridPlacementController.cs:363`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs#L363)
  * **Bug**: Converts world Y to floor using `Mathf.RoundToInt((worldPos.y - _floorOriginY) / _floorHeight)`. Clicking in the top 50% of any room calculates as the floor *above* it.
  * **Remediation**: Use `Mathf.FloorToInt((worldPos.y - _floorOriginY) / _floorHeight)`.

* **BUG-06 [High Severity] Demolishing Rooms Leaves Phantom Residents Trapped**
  * **Files**: [`BuildingTopologyState.cs:339-368`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs#L339-L368), [`TowerSimulation.cs:500-532`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L500-L532)
  * **Bug**: Demolishing a room does not evict or relocate residents inside. When their current room entity ID is deleted, transit graph lookups fail and residents are permanently frozen.
  * **Remediation**: Evict/relocate residents to the lobby and cancel targeting trips prior to room deletion.

* **BUG-07 [High Severity] `AddElevatorCar` Allows Placement Outside Shaft Span**
  * **File**: [`TowerSimulation.cs:294-321`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L294-L321)
  * **Bug**: Validates `StartingFloor < Topology.FloorCount`, but does not verify that an elevator shaft exists on that floor or that it falls within `[ElevatorBank.MinFloor .. ElevatorBank.MaxFloor]`.
  * **Remediation**: Verify `carCmd.StartingFloor >= MinFloor && carCmd.StartingFloor <= MaxFloor` and verify the floor contains an `elevator_shaft` room.

* **BUG-08 [High Severity] 29x Financial Rate Incoherence (50 Ticks vs 1440 Ticks)**
  * **File**: [`TowerSimulation.cs:135-140`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L135-L140)
  * **Bug**: Rent and wage cycles execute every 50 ticks, running 28.8 times per in-game day (1440 ticks), hyper-inflating the economy.
  * **Remediation**: Change condition to `currentTick.Value % DailySchedule.TicksPerDay == 0`.

* **BUG-09 [High Severity] Unreachable Apartment Leases Trap Residents Outside**
  * **Files**: [`LeasingDemandSystem.cs:49-70`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Economy/LeasingDemandSystem.cs#L49-L70), [`ScheduleTripGenerator.cs:149-155`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs#L149-L155)
  * **Bug**: Vacant apartments are leased without checking path connectivity to `Outside`. Newly spawned residents cannot pathfind home and remain stranded on the street forever.
  * **Remediation**: Verify route feasibility from `TransitGraph.OutsideNode` to the apartment portal before signing a lease.

* **BUG-10 [High Severity] Gravity Waste Chute Floor 0 Connectivity Failure**
  * **File**: [`WaterWasteNetworkState.cs:50, 106-125`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/WaterWasteNetworkState.cs#L50-L125)
  * **Bug**: `FindContinuousColumn` checks for `utility:waste_chute` on Floor 0, but ground waste infrastructure is `utility:waste_collection`. Floor 0 cannot host both, causing waste networks to fail validation.
  * **Remediation**: In `FindContinuousColumn`, allow Floor 0 to terminate at `WasteCollectionContentId` and verify column bounds.

* **BUG-11 [Medium Severity] Stairwell Edges Set as Walk Mode with Zero Travel Distance**
  * **Files**: [`HierarchicalTransitGraph.cs:236`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs#L236), [`TransitExecutionSystem.cs:330-336`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs#L330-L336)
  * **Bug**: Stairwell edges are created as `TransitMode.Walk` instead of `TransitMode.Stairs`. Because adjacent stair doors share the same X column, horizontal distance `dx = 0`. Residents stand motionless on the lower floor for 8 ticks and then abruptly teleport upstairs.
  * **Remediation**: Set `TransitMode.Stairs` on stair edges; implement vertical floor interpolation during stair travel.

* **BUG-12 [Medium Severity] Downward Turnaround Heuristic Fails at Floor 0**
  * **File**: [`ElevatorCar.cs:323-327`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs#L323-L327)
  * **Bug**: The check `Direction == ElevatorDirection.None || Direction == ElevatorDirection.Up` fails when a car arrives at Floor 0 with `Direction == Down`, falling back to Floor 1 instead of sweeping from the top floor down.
  * **Remediation**: Remove the direction constraint when `CurrentFloor == 0`.

* **BUG-13 [Medium Severity] Utility Degradation Layer Disconnected from Simulation**
  * **Files**: [`ElectricalGridState.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/ElectricalGridState.cs), [`WaterWasteNetworkState.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/WaterWasteNetworkState.cs), [`UtilityOperationsState.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs)
  * **Bug**: `UtilityOperationsState` models equipment degradation and failure (`condition <= 0.30f`), but `ElectricalGridState.Evaluate(topology)` and `WaterWasteNetworkState.Evaluate(topology)` do not take `UtilityOperationsState`. Failed equipment continues operating at 100% capacity.
  * **Remediation**: Pass `UtilityOperationsState` into grid evaluation; treat failed equipment as disconnected.

---

### 2.4. Concurrency, Threading & Race Conditions (RACE-01 through RACE-09)

* **RACE-01 [Critical Severity] Event Unsubscription & Permanent Listener Death on Disable/Enable**
  * **Files**: [`TowerPlayableController.cs:74, 86-87`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs#L74-L87), [`InspectSelectionController.cs:110-115`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs#L110-L115)
  * **Hazard**: `TowerPlayableController.OnDisable()` unregisters all application events (`ModeChanged`, `PlacementExecuted`, `InspectionRequested`, etc.). `OnEnable()` calls `Initialize()`, which immediately aborts with `if (_sim != null) return;` without resubscribing. Toggling the controller off and on permanently severs all UI interactions and building tools. `InspectSelectionController` lacks `OnEnable()` entirely.
  * **Remediation**: Separate session instantiation (`EnsureSimulationSession()`) from event binding (`SubscribeEvents()` / `UnsubscribeEvents()`). Add `OnEnable()` to `InspectSelectionController`.

* **RACE-02 [High Severity] Dangling Native Unity Objects via C# `??` on Static References**
  * **Files**: [`StewardTheme.cs:19-27`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/UI/StewardTheme.cs#L19-L27), [`NpcSkeletalHierarchy.cs:57-61, 681-690`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs#L57-L61)
  * **Hazard**: Static textures and sprites use `_surface ?? (_surface = Solid(...))`. Because C# `??` checks CLR null rather than Unity's overloaded `== null`, destroyed C++ native objects survive scene unloads or domain reloads, throwing `MissingReferenceException` when drawn.
  * **Remediation**: Replace `??` with `_surface != null ? _surface : (...)` and provide static cleanup methods invoked on domain reload.

* **RACE-03 [High Severity] IMGUI Pointer Click Fall-Through in Inspect Mode**
  * **File**: [`InspectSelectionController.cs:311-315`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs#L311-L315)
  * **Hazard**: `IsPointerOverUI` calls `EventSystem.current.IsPointerOverGameObject()`. Because the UI is built with IMGUI (`OnGUI`), this check always returns `false`. Clicking on UI buttons (e.g. "CLOSE" on inspection cards) penetrates through and selects or deselects entities behind the UI.
  * **Remediation**: Replace with manual IMGUI panel bounds checks (as implemented in `GridPlacementController.IsPointerOverUI`).

* **RACE-04 [Medium Severity] Uncontrolled Script Execution Order & Split-Brain Session**
  * **Files**: [`ModeShellBarController.cs:28-32`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs#L28-L32), [`TowerPlayableController.cs:77, 102`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs#L77)
  * **Hazard**: `ModeShellBarController.Session` lazily creates a new `ModeShellSession` if accessed before `TowerPlayableController.Awake()`. Subsequent assignments overwrite `_modeBar.Session`, orphaning early subscribers on a discarded session instance.
  * **Remediation**: Explicitly inject `ModeShellSession` during initialization; remove lazy static-like fallback in `ModeShellBarController`.

* **RACE-05 [Medium Severity] Unsynchronized `AtomicFileSaveStore` & File-Lock Hazards**
  * **File**: [`AtomicFileSaveStore.cs:24-47`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs#L24-L47)
  * **Hazard**: Hardcoded `filePath + ".tmp"` causes collisions if an autosave timer and manual save trigger concurrently. On Windows, `File.Replace` throws unhandled `IOException` if another process holds an open read handle.
  * **Remediation**: Use GUID temporary names and wrap `File.Replace` in a retry loop with exponential backoff.

* **RACE-06 [Medium Severity] Static Procedural Texture & Sprite Leaks in Catalogs**
  * **Files**: `ResidentSpriteCatalog.cs`, `EmoteSpriteCatalog.cs`, `WardrobePartCatalog.cs`, `ArchitecturalFixtureCatalog.cs`, `PropCatalog.cs`
  * **Hazard**: `ClearCache()` executes `SpriteCache.Clear()`, which drops dictionary references but does not call `UnityEngine.Object.Destroy` on the procedural assets, leaking native memory across tests and sessions.
  * **Remediation**: Iterate and explicitly call `Object.Destroy` / `DestroyImmediate` on cached textures and sprites before clearing dictionaries.

* **RACE-07 [Low to Medium Severity] Redundant Event Flooding on Mouse Hover**
  * **Files**: [`GridPlacementController.cs:190`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs#L190), [`ModeShellSession.cs:101-105`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Application/Modes/ModeShellSession.cs#L101-L105)
  * **Hazard**: In `Update()`, `SetPlacementTarget` fires `ModeChanged?.Invoke(Projection())` on every frame during hover, even if coordinates have not changed, allocating 60 projections/sec and triggering unnecessary presenter updates.
  * **Remediation**: Guard `SetPlacementTarget` to only fire if `TargetFloor` or `TargetCellX` actually changed.

* **RACE-08 [Medium Severity] Scene Serialization Pollution from `[ExecuteAlways]`**
  * **Files**: [`TowerPlayableController.cs:24, 72-85`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs#L24), [`TowerSceneBuilder.cs:45-48`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Editor/Tower/TowerSceneBuilder.cs#L45-L48)
  * **Hazard**: Dynamically generated GameObjects created in EditMode are serialized into `.unity` scene files, causing scenes like `Tower_GroundStart.unity` to inherit baked 5-floor objects unless cleaned up.
  * **Remediation**: Set `HideFlags.DontSave` on procedural preview geometry created in EditMode.

* **RACE-09 [Low to Medium Severity] Mid-Transit Destination Demolition Desynchronization**
  * **Files**: [`TowerSimulation.cs:500-522`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs#L500-L522), [`TransitExecutionSystem.cs:324-336`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs#L324-L336)
  * **Hazard**: Demolishing a room mid-transit leaves in-flight residents heading towards a non-existent destination room.
  * **Remediation**: Scan and cancel active trips targeting a room upon demolition, redirecting residents to the lobby.

---

### 2.5. Test Flakiness & Reliability (FLK-01 through FLK-08)

* **FLK-01 [Critical Severity] Concurrent Simulation Stepping in PlayMode**
  * **File**: [`GoldenExpansionPlayModeTests.cs:56-112`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs#L56-L112)
  * **Failure Scenario**: The test coroutine advances ticks manually (`controller.SimulationSession.AdvanceOneTick()`) while `TowerPlayableController.Update()` simultaneously advances ticks via `Time.deltaTime`. Slow CI runners execute extra ticks, causing test assertions on delivered passengers and resident counts to fail intermittently.
  * **Remediation**: Expose `controller.SetPaused(true)` and pause autonomous ticking during PlayMode tests.

* **FLK-02 [High Severity] Scene Pollution & Leaked GameObjects**
  * **File**: [`GoldenExpansionPlayModeTests.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs)
  * **Failure Scenario**: Lacks `try...finally` teardown. When assertions fail, the `"Tower Camera"` and holder GameObjects remain in the scene, polluting subsequent tests.
  * **Remediation**: Wrap test bodies in `try...finally` with `DestroyImmediate`.

* **FLK-03 [High Severity] Pseudo-Null Lifecycle Trap on Static Assets**
  * **Files**: `NpcSkeletalHierarchy.cs`, `StewardTheme.cs`
  * **Failure Scenario**: Static sprites/textures cached with `??` hold destroyed native pointers across test fixtures, throwing `MissingReferenceException`.
  * **Remediation**: Use Unity overloaded `== null` and add `ResetStaticCaches()` hooks.

* **FLK-04 [Medium Severity] Global Singleton `"Tower Camera"` Pollution**
  * **Files**: `TowerCameraController.cs`, `GridPlacementControllerTests.cs`
  * **Failure Scenario**: `"Tower Camera"` tagged `"MainCamera"` persists across test boundaries and breaks untagged camera fallback tests.
  * **Remediation**: Ensure teardowns destroy `"Tower Camera"` explicitly.

* **FLK-05 [Medium Severity] Teardown Cascade Failures in Presenter Tests**
  * **Files**: [`InspectSelectionControllerTests.cs:57`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/EditMode/Presentation/InspectSelectionControllerTests.cs#L57), [`RoomPresenterTests.cs:30`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/EditMode/Presentation/RoomPresenterTests.cs#L30)
  * **Failure Scenario**: `[TearDown]` calls `_presenter.Clear()` without a null guard. If `[SetUp]` fails prematurely, `_presenter` is null, causing `NullReferenceException` in `TearDown` and skipping `Object.DestroyImmediate(_holder)`.
  * **Remediation**: Use `_presenter?.Clear()`.

* **FLK-06 [Low to Medium Severity] Exact Floating-Point Comparisons**
  * **Files**: [`AggregateSerializationTests.cs:106`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs#L106), [`ElectricalGridStateTests.cs:68`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/EditMode/Domain/ElectricalGridStateTests.cs#L68)
  * **Failure Scenario**: Uses exact `Is.EqualTo(2.5f)` and `Is.EqualTo(0.75f)` without `.Within(...)` tolerances, making assertions vulnerable to floating-point epsilon differences across platforms.
  * **Remediation**: Always specify tolerances (`.Within(0.001f)`).

* **FLK-07 [Low Severity] Asynchronous Destruction in PlayMode Teardown**
  * **Files**: `OutsideCityPlayModeTests.cs`, `TowerAtmosphereAndElevatorLimitsPlayModeTests.cs`
  * **Failure Scenario**: Uses `Object.Destroy(holder)` instead of `DestroyImmediate`, leaving GameObject alive at start of next test.
  * **Remediation**: Use `DestroyImmediate` or add `yield return null`.

* **FLK-08 [Low Severity] Transient OS File Locks in `AtomicFileSaveStoreTests`**
  * **File**: [`AtomicFileSaveStoreTests.cs:20-26`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Tests/EditMode/Infrastructure/AtomicFileSaveStoreTests.cs#L20-L26)
  * **Failure Scenario**: `Directory.Delete(_testDirectory, true)` throws `IOException` if OS file handle closing is deferred.
  * **Remediation**: Wrap `Directory.Delete` in `try...catch(IOException)`.

---

### 2.6. Maintainability & Technical Debt (MAIN-01 through MAIN-07)

* **MAIN-01 [Critical Severity] Steady-State Allocations Violating <4 ms Tick Budget**
  * **Files**: [`ResidentWellbeingSystem.cs:15-33`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Population/ResidentWellbeingSystem.cs#L15-L33), [`ScheduleTripGenerator.cs:73-115`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs#L73-L115), [`UtilityOperationsState.cs:20-34`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs#L20-L34)
  * **Hotspot**: `ResidentWellbeingSystem` allocates 300 `List<string>` instances every tick. `ScheduleTripGenerator` allocates lists and performs quadratic room scans across all floors every tick. `UtilityOperationsState` reallocates equipment lists on every tick.
  * **Remediation**: Use reusable grievance bitmasks; index rooms by `ContentId` in topology for $O(1)$ lookups; execute `SyncEquipment` only on topology changes.

* **MAIN-02 [Critical Severity] Flat Dijkstra & $O(F^2)$ Transit Graph Bloat**
  * **Files**: [`HierarchicalTransitGraph.cs:151-207`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs#L151-L207), [`TransitRoutePlanner.cs:30-79`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Transit/TransitRoutePlanner.cs#L30-L79)
  * **Hotspot**: Connects every node on a floor to every other node with walk edges (complete bipartite clique), and connects every elevator landing to every other landing ($O(F^2)$ edges). Flat `SortedSet` Dijkstra runs across the entire building even for single-floor local walks.
  * **Remediation**: Split into a two-tier graph (1D horizontal corridors + vertical trunk). Route local trips without invoking whole-tower Dijkstra.

* **MAIN-03 [High Severity] Per-Frame Hierarchy Scans at 60 FPS**
  * **File**: [`TowerPlayableController.cs:235`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs#L235)
  * **Hotspot**: `RenderVisualSnapshot()` calls `SyncPresenterGeometry()` on every frame, executing `EnsureRoomViews` (HashSets, string parsing) and `EnsureFloorViews` (`Transform.Find`) at 60 FPS instead of reacting to building events.
  * **Remediation**: Remove `SyncPresenterGeometry()` from per-frame `RenderVisualSnapshot()`. Invoke geometry updates only when building commands execute or topology changes.

* **MAIN-04 [High Severity] Procedural Unique Materials Defeating SRP Batching**
  * **File**: [`TowerAtmospherePresenter.cs:151-179`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/TowerAtmospherePresenter.cs#L151-L179)
  * **Hotspot**: Instantiates a unique `Material` instance and a dedicated `AudioSource` for every room. At 30 floors (~300 rooms), this destroys SRP batching and exceeds the draw-call and audio source budgets.
  * **Remediation**: Use `MaterialPropertyBlock` for window light volumes; pool audio sources.

* **MAIN-05 [High Severity] Open-Closed Principle (OCP) Violations via String Checks**
  * **Files**: `BuildingCatalog.cs`, `BuildingTopologyState.cs`, `TowerEconomyState.cs`, `GridPlacementController.cs`, and 8+ presenter classes
  * **Hotspot**: Adding a new room type requires modifying at least 12 distinct files due to hardcoded string inspections (`StartsWith("residential:")`).
  * **Remediation**: Replace string prefix checks with typed capability enums or records (`RoomCategory`, `IsHabitation`, `IsCommercial`, `IsUtility`).

* **MAIN-06 [High Severity] Zero Gameplay ScriptableObjects**
  * **Files**: `BuildingCatalog.cs`, `PropContentRegistry.cs`, `TowerEconomyState.cs`, `NpcContentRegistry.cs`
  * **Hotspot**: All balance tuning, costs, room definitions, and wardrobe catalogs are hardcoded in static C# files. Designers cannot adjust balance in the Unity Editor without C# recompilation.
  * **Remediation**: Convert balance and content registries into ScriptableObjects in `OneRoof.Content`.

* **MAIN-07 [Medium Severity] Monster Classes**
  * **Files**: [`TowerSimulation.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/TowerSimulation.cs) (784 lines), [`BuildingTopologyState.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Domain/Topology/BuildingTopologyState.cs) (738 lines), [`GridPlacementController.cs`](file:///home/geisha/Vibecode/UnityAI/one-roof/Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs) (771 lines)
  * **Hotspot**: High cyclomatic complexity; mixed responsibilities (spatial data + command validation + serialization).
  * **Remediation**: Extract command validation handlers (`BuildCommandHandler`, `DemolishCommandHandler`).

---

## 3. Prioritized & Executable Stabilization Roadmap

The 49 findings are grouped into 6 sequenced, dependency-ordered engineering phases.

```
[Phase 1: Critical Fixes — Soft-Locks, Scale Blockers & Security]
       │
       ├── Fix 30-floor electrical blackout (BUG-01)
       ├── Guard zero-car elevator shafts & add timeout (BUG-02)
       ├── Use stable portal EntityIds for transit nodes (BUG-03)
       ├── Constrain save paths to persistentDataPath (SEC-01, SEC-03)
       ├── Fix elevator loop overflow & bounds crash (SEC-02, SEC-04, SEC-05)
       └── Fix disable/enable lifecycle event deadlock (RACE-01, RACE-03)
       │
[Phase 2: Hot-Path Performance, Zero-Allocation & NPC View Pooling]
       │
       ├── Wire TowerResidentPresenter to NpcViewPool (40-60 view cap) (CQ-01)
       ├── Eliminate steady-state allocations in wellbeing & trip generator (MAIN-01, CQ-03)
       ├── Eliminate per-frame transform scans & geometry syncing (MAIN-03)
       ├── Memoize all 7 data overlay projections (CQ-03)
       └── Replace C# ?? on static Unity objects with overloaded == null (RACE-02, FLK-03)
       │
[Phase 3: Test Suite Determinism & PlayMode De-Flaking]
       │
       ├── Pause autonomous ticking during GoldenExpansionPlayModeTests (FLK-01)
       ├── Wrap PlayMode tests in try...finally & clean "Tower Camera" (FLK-02, FLK-04)
       ├── Add null guards to presenter test teardowns (FLK-05)
       ├── Enforce floating-point tolerances (.Within) across tests (FLK-06)
       └── Fix pre-existing presentation test bugs (CQ-05)
       │
[Phase 4: Closed-Loop Economy & Financial Integrity (ECON-001..004)]
       │
       ├── Implement long CashBalance & daily 1440-tick settlement (BUG-04, BUG-08)
       ├── Deduct household rent & commercial rent/taxes from cash ledgers (BUG-04)
       ├── Validate path connectivity before signing apartment leases (BUG-09)
       └── Connect utility equipment degradation to network evaluation (BUG-13)
       │
[Phase 5: Transit Graph Modernization & Spatial Logic]
       │
       ├── Split transit graph into two-tier hierarchical structure (MAIN-02)
       ├── Fix stairwell TransitMode.Stairs and vertical walk interpolation (BUG-11)
       ├── Fix GridPlacementController RoundToInt -> FloorToInt hit testing (BUG-05)
       ├── Fix waste chute ground terminal connectivity (BUG-10)
       └── Reconcile stranded residents upon room demolition (BUG-06, SEC-07, RACE-09)
       │
[Phase 6: Code Quality, Refactoring & Data-Driven Content Pipeline]
       │
       ├── Unpack minified TowerPlayableController into dedicated coordinators (CQ-02)
       ├── Generalize overlay screen projections for 30 floors (CQ-04)
       ├── Centralize grid-to-world transforms & dynamic tool pricing (CQ-07)
       ├── Fix specialist training double-advancement (CQ-06)
       ├── Transition content & balance constants to ScriptableObjects (MAIN-06)
       └── Replace string-based room checks with polymorphic capabilities (MAIN-05)
```

---

### Detailed Task Specifications

#### Phase 1: Critical Fixes — Soft-Locks, Scale Blockers & Security

* **Task 1.1: Fix 30-Floor Electrical Blackout Math (`BUG-01`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Infrastructure/ElectricalGridState.cs`
  * **Changes**: Add step-up transformer / booster room support, or rescale `DefaultRiserLossPerFloor` from `0.04f` to `0.005f` so voltage at floor 30 remains $\ge 0.85\text{V}$.
  * **Acceptance**: `ElectricalGridStateTests` verifying floor 30 operates at nominal voltage with adequate supply.

* **Task 1.2: Guard Zero-Car Elevator Shafts & Timeout (`BUG-02`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`, `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs`
  * **Changes**: Omit elevator edges from `HierarchicalTransitGraph` if `ElevatorBank.Cars.Count == 0`. In `TransitExecutionSystem`, add a 120-tick wait timeout that cancels the trip or re-routes via stairs.
  * **Acceptance**: New unit test verifying residents do not enqueue in zero-car shafts and re-route via stairs.

* **Task 1.3: Stable Portal Entity IDs for Transit Nodes (`BUG-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`
  * **Changes**: Replace `new EntityId(nextNodeId++)` with `portal.Id`: `new TransitNode(portal.Id, nodeType, portal.Location, portal.RoomId)`.
  * **Acceptance**: Dynamic graph rebuild test verifying in-flight trip leg node IDs remain stable after adding/removing unrelated portals.

* **Task 1.4: Constrain Save Paths & Fix Temp Symlink Race (`SEC-01`, `SEC-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs`
  * **Changes**: Canonicalize paths against `Application.persistentDataPath`. Use randomized GUID temporary file names (`$"{name}.{Guid.NewGuid():N}.tmp"`).
  * **Acceptance**: Unit tests attempting directory traversal (`../`) assert failure; verify saves write only to authorized paths.

* **Task 1.5: Fix ElevatorBank Loop Overflow & CanExecute Bounds Crash (`SEC-02`, `SEC-04`, `SEC-05`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`, `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`, `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs`
  * **Changes**: Clamp floor ranges (`[-10..100]`) in `ElevatorBank`. Check `minX <= maxX` in `TowerSimulation.CanExecute` before evaluating `.Bounds`. Add null check on `trip.PlannedRoute` in `TransitExecutionSystem`.
  * **Acceptance**: Boundary tests for `ElevatorBank(0, int.MaxValue)` reject safely; `CanExecute` with inverted bounds returns rejection without throwing.

* **Task 1.6: Fix Disable/Enable Lifecycle Event Deadlock (`RACE-01`, `RACE-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`, `Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs`
  * **Changes**: Decouple `EnsureSimulationSession()` from `SubscribeEvents()` / `UnsubscribeEvents()`. Resubscribe on `OnEnable()`. Replace `EventSystem.current.IsPointerOverGameObject()` in `InspectSelectionController` with IMGUI panel bounds checking.
  * **Acceptance**: EditMode/PlayMode tests verifying disabling and re-enabling `TowerPlayableController` retains full mode switching and click selection.

---

#### Phase 2: Hot-Path Performance, Zero-Allocation & NPC View Pooling

* **Task 2.1: Wire `TowerResidentPresenter` to `NpcViewPool` (`CQ-01`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs`, `Assets/OneRoof/Runtime/Presentation/Population/NpcViewPool.cs`
  * **Changes**: Enforce 40–60 view cap. Lease views only to residents within camera vertical viewport; off-screen residents remain lightweight simulation records.
  * **Acceptance**: Fixture with 300 residents verifies no more than 60 GameObjects are instantiated.

* **Task 2.2: Eliminate Steady-State Allocations in Hot Tick Path (`MAIN-01`, `CQ-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Population/ResidentWellbeingSystem.cs`, `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs`, `Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs`
  * **Changes**: Preallocate grievance buffers; index rooms by `ContentId` in topology; execute `SyncEquipment` only on topology changes.
  * **Acceptance**: Profiler assertion proving 0 bytes allocated during steady-state ticks.

* **Task 2.3: Eliminate Per-Frame Transform Scans & Geometry Syncs (`MAIN-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
  * **Changes**: Remove `SyncPresenterGeometry()` from per-frame `RenderVisualSnapshot()`. Invoke geometry updates strictly on `BuildingCommandExecuted` or topology change events.
  * **Acceptance**: 60 FPS visual snapshot runs without `Transform.Find` or collection allocations.

* **Task 2.4: Memoize Data Overlay Projections (`CQ-03`)**
  * **Files**: `Assets/OneRoof/Runtime/Application/Overlays/TowerDataOverlays.cs`
  * **Changes**: Cache all 7 overlay projections keyed by `(_session.CurrentTick, _session.Version)`.
  * **Acceptance**: Multiple reads in same tick return cached reference without allocating.

* **Task 2.5: Replace C# `??` on Static Unity Objects (`RACE-02`, `FLK-03`)**
  * **Files**: `Assets/OneRoof/Runtime/UI/StewardTheme.cs`, `Assets/OneRoof/Runtime/Presentation/Population/NpcSkeletalHierarchy.cs`
  * **Changes**: Replace `??` with `_surface != null ? _surface : (...)`. Add `ResetStaticCaches()` hooked into subsystem registration.
  * **Acceptance**: Tests pass across domain reload simulations without `MissingReferenceException`.

---

#### Phase 3: Test Suite Determinism & PlayMode De-Flaking

* **Task 3.1: Pause Autonomous Ticking in PlayMode Tests (`FLK-01`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`, `Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs`
  * **Changes**: Expose `controller.SetPaused(bool)`. Pause ticking in `GoldenExpansionPlayModeTests` so only test-controlled `AdvanceOneTick()` advances the simulation.
  * **Acceptance**: `GoldenExpansionPlayModeTests` passes with deterministic tick counts across varying frame rates.

* **Task 3.2: Wrap PlayMode Tests in Teardown Guards (`FLK-02`, `FLK-04`)**
  * **Files**: `Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs`, `Assets/OneRoof/Tests/PlayMode/OutsideCityPlayModeTests.cs`
  * **Changes**: Wrap test logic in `try...finally`; explicitly destroy holder and `"Tower Camera"`.
  * **Acceptance**: Subsequent camera tests execute cleanly without leaked camera interference.

* **Task 3.3: Presenter Test Teardown Null Guards (`FLK-05`)**
  * **Files**: `Assets/OneRoof/Tests/EditMode/Presentation/InspectSelectionControllerTests.cs`, `RoomPresenterTests.cs`, `ElevatorBankPresenterTests.cs`
  * **Changes**: Change `_presenter.Clear()` to `_presenter?.Clear()`.
  * **Acceptance**: Teardown does not throw `NullReferenceException` when `SetUp` fails.

* **Task 3.4: Floating-Point Tolerances (`FLK-06`)**
  * **Files**: `Assets/OneRoof/Tests/EditMode/Domain/AggregateSerializationTests.cs`, `ElectricalGridStateTests.cs`, `NpcPopulationPresenterTests.cs`
  * **Changes**: Add `.Within(0.001f)` to float assertions.
  * **Acceptance**: 100% pass rate under varying JIT/FPU compiler optimizations.

* **Task 3.5: Fix Pre-Existing Presentation Test Bugs (`CQ-05`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs`, `TowerStructurePresenter.cs`
  * **Changes**: In `RoomPresenter.HasMatchingTheme`, adopt rooms lacking backdrops. In `TowerStructurePresenter.Initialize`, iterate and destroy all duplicate slabs.
  * **Acceptance**: Baseline EditMode suite achieves 100% pass (resolving the 2 pristine-HEAD failures).

---

#### Phase 4: Closed-Loop Economy & Financial Integrity (ECON-001..004)

* **Task 4.1: Household Cash Ledger & 1440-Tick Settlement (`BUG-04`, `BUG-08`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Economy/TowerEconomyState.cs`, `HouseholdRecord.cs`, `TowerSimulation.cs`
  * **Changes**: Add `long CashBalance` to `HouseholdRecord`. Shift settlement cycle from `tick % 50 == 0` to `tick % 1440 == 0`. Deduct rent from household cash to treasury.
  * **Acceptance**: 30-day economic conservation test proves cash is conserved across households and treasury.

* **Task 4.2: Commercial Rent & Tax Remittance (`BUG-04`, `ECON-002`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Economy/BusinessState.cs`
  * **Changes**: Businesses pay per-cell rent and tax; revenue is capped by customer visits and staffing.
  * **Acceptance**: Solvent businesses pay rent; insolvent businesses flag vacancy after 7 days.

* **Task 4.3: Lease Path Connectivity Validation (`BUG-09`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Economy/LeasingDemandSystem.cs`
  * **Changes**: Verify route exists from `Outside` to the apartment portal before signing a lease.
  * **Acceptance**: No residents spawn stranded at `Outside` when unreachable apartments are present.

* **Task 4.4: Connect Utility Degradation to Simulation (`BUG-13`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Infrastructure/ElectricalGridState.cs`, `WaterWasteNetworkState.cs`
  * **Changes**: Pass `UtilityOperationsState` into `Evaluate()`; disconnect equipment with `condition <= 0.30f`.
  * **Acceptance**: Equipment failure triggers localized power/water outages.

---

#### Phase 5: Transit Graph Modernization & Spatial Logic

* **Task 5.1: Two-Tier Hierarchical Transit Graph (`MAIN-02`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`, `TransitRoutePlanner.cs`
  * **Changes**: Partition graph into 1D floor walk corridors and vertical elevator/stair trunks. Replace flat whole-building Dijkstra with two-tier A* search.
  * **Acceptance**: 300 residents pathfinding across 30 floors executes in `<1 ms`.

* **Task 5.2: Stairwell `TransitMode.Stairs` & Vertical Walk (`BUG-11`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs`, `TransitExecutionSystem.cs`
  * **Changes**: Use `TransitMode.Stairs` for stair edges; interpolate vertical floor position across travel ticks.
  * **Acceptance**: Resident views smoothly climb stairs over 8 ticks without freezing or teleporting.

* **Task 5.3: Grid Placement Floor Hit-Testing (`BUG-05`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/GridPlacementController.cs`
  * **Changes**: Replace `Mathf.RoundToInt` with `Mathf.FloorToInt` in `TryGetCellFromWorld`.
  * **Acceptance**: Clicking anywhere within a room's vertical bounds targets that exact floor.

* **Task 5.4: Waste Chute Ground Terminal Compatibility (`BUG-10`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Infrastructure/WaterWasteNetworkState.cs`
  * **Changes**: Allow waste chute continuous column to terminate at `utility:waste_collection` on Floor 0.
  * **Acceptance**: Multi-floor waste chute stacks validate successfully above ground waste collection rooms.

* **Task 5.5: Room Demolition Resident Eviction & Trip Cancellation (`BUG-06`, `SEC-07`, `RACE-09`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`, `BuildingTopologyState.cs`
  * **Changes**: On room demolition, cancel active targeting trips and relocate occupants to the lobby.
  * **Acceptance**: Demolishing occupied rooms leaves zero phantom residents or null node lookups.

---

#### Phase 6: Code Quality, Refactoring & Data-Driven Content Pipeline

* **Task 6.1: Decompose & Unpack `TowerPlayableController` (`CQ-02`, `MAIN-07`)**
  * **Files**: `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
  * **Changes**: Unpack minified single-line statements into clean C# syntax. Extract `TowerInputCoordinator`, `TowerOverlayCoordinator`, and `TowerGeometryCoordinator`.
  * **Acceptance**: Clean formatting complying with standard C# style guidelines; components each `<= 150 lines`.

* **Task 6.2: Dynamic Overlay Screen Projections for 30 Floors (`CQ-04`)**
  * **Files**: `ElevatorWaitOverlayPresenter.cs`, `BusinessHealthOverlayPresenter.cs`, `PopulationOverlayPresenter.cs`
  * **Changes**: Replace fixed 5-floor screen math with dynamic floor scaling and scrollviews.
  * **Acceptance**: Overlays render legibly across 30 floors without clipping off-screen.

* **Task 6.3: Centralize Coordinate Transforms & Dynamic Tool Metadata (`CQ-07`)**
  * **Files**: `TowerGridCoordinates.cs` (new), `ModeShellBarController.cs`
  * **Changes**: Centralize cell-to-world formulas; bind tool buttons dynamically to `BuildingCatalog`.
  * **Acceptance**: Zero duplicate coordinate formulas; tool costs update automatically from domain catalog.

* **Task 6.4: Fix Specialist Training Double-Advancement (`CQ-06`)**
  * **Files**: `Assets/OneRoof/Runtime/Domain/Population/SpecialistRoleSystem.cs`
  * **Changes**: Exclude newly selected candidates from the subsequent trainee loop.
  * **Acceptance**: New trainees advance by exactly `TrainingProgressPerTick` on their initial tick.

* **Task 6.5: Data-Driven Content via ScriptableObjects (`MAIN-06`)**
  * **Files**: `OneRoof.Content` assembly, ScriptableObject definitions
  * **Changes**: Convert static C# registries (`BuildingCatalog`, `PropContentRegistry`, `TowerEconomyState`) into ScriptableObjects.
  * **Acceptance**: Balance values and costs can be edited in Unity Inspector without code recompilation.

* **Task 6.6: Replace String-Based Room Checks with Typed Capabilities (`MAIN-05`)**
  * **Files**: Domain, Application, and Presentation assemblies
  * **Changes**: Replace `.StartsWith("residential:")` with typed `RoomCategory` enum and capability flags.
  * **Acceptance**: Adding a new room type requires modifying only its definition record/asset.

---

## 4. Validation Status

- **Type**: Documentation & Roadmap Architecture Handoff.
- **Headless Unity Validation**: Not executed for this documentation-only change (per `AGENTS.md` guidelines).
- **Working Tree**: Prior active handoff `HANDOFF_2026-09-23_oni-utility-network-layer.md` successfully moved to `Handoffs/Archive/`. This document is the sole active handoff in `Handoffs/Active/`.

---

## 5. Risks and Next Safe Action

### Identified Technical Risks
1. **Save Envelope Compatibility**: Deriving transit node IDs from portal IDs (`BUG-03`) and adding `CashBalance` to `HouseholdRecord` (`ECON-001`) modifies save DTOs. Migration paths or version bumps (`SaveEnvelope.Version`) must be tested with `TowerSaveRoundTripTests`.
2. **Transit Routing Regressions**: Splitting `HierarchicalTransitGraph` into a two-tier graph (`Task 5.1`) affects resident commuting behavior. Ensure `Known route fixtures` and `FiftyResidentFixtureTests` pass before expanding test scale.
3. **Presenter Timing Changes**: Pausing autonomous simulation ticking during `GoldenExpansionPlayModeTests` (`Task 3.1`) alters frame-to-tick ratios in test assertions. Verify and lock tick assertions explicitly.

### Immediate Next Safe Action
Begin **Phase 1, Task 1.1 through Task 1.6** in an isolated branch/worktree:
1. Rescale electrical loss rate (`ElectricalGridState.cs`) to unlock the 30-floor North Star boundary.
2. Guard zero-car elevator shafts in `HierarchicalTransitGraph.cs`.
3. Use stable portal IDs in `TransitNode`.
4. Constrain save paths to `Application.persistentDataPath` in `AtomicFileSaveStore.cs`.
5. Fix `TowerPlayableController.cs` event unsubscription deadlock on disable/re-enable.
6. Run headless Unity compile and EditMode test suite to verify baseline stability.
