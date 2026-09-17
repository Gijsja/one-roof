# ARCH-001 — Delete Parallel Prototype Simulation (Unjustified Seam)

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Remove the obsolete, parallel `TransitPrototypeSimulation` and `TransitPrototypeSession` which formed an unjustified architectural seam. Consolidate `TowerSimulationSession` as the single authoritative application session module providing read-only projections to Presentation.

---

## Acceptance Criteria

- [x] `TransitPrototypeSimulation.cs` and `TransitPrototypeSession.cs` removed; no redundant simulation state or parallel projection logic.
- [x] Shared presentation projection types (`TransitResidentStatus`, `TransitResidentProjection`, `ElevatorProjection`) and renamed `TowerProjection` retained in `Assets/OneRoof/Runtime/Application/Transit/TowerProjection.cs`.
- [x] `TowerSimulationSession` and `TowerPlayableController` migrated to `TowerProjection`.
- [x] Obsolete presentation and editor prototype controllers/builders deleted (`TransitPrototypeController.cs`, `TransitPrototypeSceneBuilder.cs`) along with their empty folders and `.meta` files.
- [x] Obsolete prototype-specific tests deleted (`TransitPrototypeSimulationTests.cs`, `TransitPrototypeSceneCompositionTests.cs`).
- [x] `GoldenFirstPlayablePlayModeTests` updated to run against `TowerSimulationSession`.
- [x] Zero references to `TransitPrototype` remain across all C# code and assembly definitions.
- [x] ADR-036 recorded in `Docs/07_DECISION_LOG.md`.

---

## Scope and Ownership

### Deleted Files
- `Assets/OneRoof/Runtime/Domain/Transit/TransitPrototypeSimulation.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Presentation/Transit/TransitPrototypeController.cs` (+ `.meta`, folder `.meta`)
- `Assets/OneRoof/Editor/Transit/TransitPrototypeSceneBuilder.cs` (+ `.meta`, folder `.meta`)
- `Assets/OneRoof/Tests/EditMode/Domain/TransitPrototypeSimulationTests.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Presentation/TransitPrototypeSceneCompositionTests.cs` (+ `.meta`)

### Renamed / Modified Files
- `Assets/OneRoof/Runtime/Application/Transit/TransitPrototypeSession.cs` → `TowerProjection.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Tests/PlayMode/GoldenFirstPlayablePlayModeTests.cs`
- `Docs/07_DECISION_LOG.md` (ADR-036)
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`

---

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| C# Reference Check | `grep -rn "TransitPrototype" Assets/OneRoof/ --include="*.cs"` | CLEAN (0 references) |
| Git Tracking & Meta Hygiene | `git status` verifying all moved/deleted assets have paired `.meta` handling | PASS |

---

## Next Safe Action

Proceed to **ARCH-002**: Split God Presenter (`TowerPlayableController.cs` → 4 deep presenters: `TowerStructurePresenter`, `ElevatorBankPresenter`, `RoomPresenter`, `NpcPopulationPresenter`).
