# Handoff — Test Suite Restoration and Transit Synchronization

**Session date:** 2026-09-18  
**Task scope:** Resolve items 1, 2, and 3: fix `EditorBuildSettings.asset` scene references, run the full test suite locally via Unity CLI resolving all compiler and test failures, and push clean commits ahead of `origin/main`.

---

## Validation Status

- **Unity Compilation:** PASSED — 0 compiler errors, 0 compiler warnings.
- **EditMode Tests:** PASSED — 259 / 259 passed (0 failed, 0 skipped, 100% green).
- **PlayMode Tests:** PASSED — 3 / 3 passed (0 failed, 0 skipped, 100% green).
- **Total Test Suite:** 262 / 262 tests passing (100%).
- **Git Sync:** Pushed cleanly to `origin/main` (branch is up to date, working tree clean).

---

## Diagnosed Root Causes and Solutions

### 1. `EditorBuildSettings.asset` Missing Scenes
- **Problem:** `EditorBuildSettings.asset` referenced `Assets/Scenes/SampleScene.unity` which did not exist.
- **Solution:** Updated `EditorBuildSettings.asset` to register `Assets/Scenes/Tower.unity` and `Assets/Scenes/Testbed_Transit.unity` with valid GUIDs.

### 2. Elevator Duplicate Trips & High-Floor Starvation (`GoldenExpansionAcceptanceTests`)
- **Problem:** When residents experienced high commute wait times during morning rush, schedule block transitions (Work → Eat → Leisure) continued to trigger in `ScheduleTripGenerator`, submitting duplicate simultaneous trips for residents already waiting in queue. This created duplicate ghost queue entries (`QueueLength=41` for 16 distinct residents), distorting queue mechanics. In addition, when departing Floor 0 to collect morning commuters, the elevator car lacked top-floor target selection, resulting in lower floors consuming capacity and stranding upper floors.
- **Solution:**
  - Added guards in `ScheduleTripGenerator.cs` to skip residents whose `CurrentActivity == ActivityKind.Commuting`.
  - Added guard in `TransitExecutionSystem.SubmitTrip` to reject new trips if the resident already has an active trip in `_activeTripsByPerson`.
  - In `ElevatorCar.ChooseNextTarget()`, prioritized `_targetFloors.Max` when departing Floor 0 to collect passengers, properly sweeping downward calls.
  - Result: `GoldenMilestone5_1_FullExpansion_InterventionReducesWaitOver40Percent_AndSaveStateRoundTrips` passes with >= 40% reduction.

### 3. Commercial Office Leasing Employment Priority (`TowerEconomyAndLeasingTests`)
- **Problem:** `OfficeZoning_LeasingDemand_EmploysNewResidentsAtOffice` failed because `LeasingDemandSystem` assigned workplaces by iterating unordered `potentialWorkplaces[i % count]`, which always assigned Room 5 (the Diner with 50 existing workers) to the first newly leased resident, leaving the newly zoned commercial office unstaffed.
- **Solution:**
  - Removed `amenity:` from `potentialWorkplaces` in `LeasingDemandSystem.cs`.
  - Sorted `potentialWorkplaces` by current employee count, ensuring newly zoned or understaffed commercial spaces (such as offices with 0 workers) recruit incoming tenants first.

### 4. `PlacementGhostPresenter` Shared Mesh Destruction in PlayMode
- **Problem:** `GoldenExpansionPlayModeTests` failed with an unhandled log error: `Destroying assets is not permitted to avoid data loss. If you really want to remove an asset use DestroyImmediate (theObject, true);`.
- **Solution:** `_ghostMesh` was assigned to `_ghostMeshFilter.sharedMesh` (the built-in Quad primitive asset). Calling `DestroyAsset(_ghostMesh)` in `OnDestroy()` attempted to destroy the project asset. Replaced with nulling `_ghostMesh = null` without attempting to destroy the shared mesh asset.

---

## Changed Files Summary

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Domain/Economy/LeasingDemandSystem.cs` | Domain | Sort potential workplaces by worker count, exclude amenities. |
| `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs` | Domain | Target highest floor on departure from ground floor. |
| `Assets/OneRoof/Runtime/Domain/Transit/TransitExecutionSystem.cs` | Domain | Guard against duplicate simultaneous trips for in-transit residents. |
| `Assets/OneRoof/Runtime/Domain/Trips/ScheduleTripGenerator.cs` | Domain | Skip trip generation for residents actively commuting. |
| `Assets/OneRoof/Runtime/Presentation/Tower/PlacementGhostPresenter.cs` | Presentation | Prevent destruction of shared mesh asset on teardown. |
| `ProjectSettings/EditorBuildSettings.asset` | Settings | Registered `Tower.unity` and `Testbed_Transit.unity`. |
| `.gitignore` | Config | Added `test-results.xml` ignore rule. |

---

## Next Steps

All 5 architectural refactor tasks (`ARCH-001` through `ARCH-005`) are completed, verified, and committed. All 262 EditMode and PlayMode tests pass. The codebase is fully green and ready for the next backlog milestones in `Planning/BACKLOG.md` (e.g. `OR-518`, `OR-519`, `OR-520` shader and presentation tasks).
