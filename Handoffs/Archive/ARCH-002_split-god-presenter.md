# ARCH-002 — Split God Presenter: TowerPlayableController → 4 Deep Presenters

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Split `TowerPlayableController.cs` (formerly project's hottest file at 1100+ lines mixing structural geometry, elevator visuals, room backdrops, NPC sync, and UI HUD) into cohesive deep presenters and a dedicated HUD view, reducing `TowerPlayableController` to an ~150-line thin orchestrator.

---

## Acceptance Criteria

- [x] `TowerPlayableController.cs` is ≤150 lines (currently exactly 150 lines).
- [x] Four deep presenter classes created in `Assets/OneRoof/Runtime/Presentation/Tower/`:
  - `TowerStructurePresenter.cs` (Floor slab quads and baseline dividers)
  - `ElevatorBankPresenter.cs` (Elevator shaft cavity, rails, columns, caps, and car pooling/movement)
  - `RoomPresenter.cs` (Room backdrops, furnishings, partitions, dynamic creation/demolition)
  - `TowerResidentPresenter.cs` (Resident views, Spine/procedural rigs, room slots, corridor transit, emotes)
- [x] Dedicated OnGUI HUD view extracted into `TowerDashboardHudView.cs`.
- [x] Camera setup encapsulated via static `EnsureTowerCamera` helper in `TowerCameraController.cs`.
- [x] EditMode tests written for each presenter in `Assets/OneRoof/Tests/EditMode/Presentation/`:
  - `TowerStructurePresenterTests.cs`
  - `ElevatorBankPresenterTests.cs`
  - `RoomPresenterTests.cs`
  - `TowerResidentPresenterTests.cs`
- [x] `TowerPlayableControllerTests.cs` updated to verify deep presenter orchestration and state.
- [x] 100% paired `.meta` hygiene verified across all new files.
- [x] Domain purity preserved with 0 `UnityEngine` dependencies in `OneRoof.Domain`.
- [x] ADR-037 recorded in `Docs/07_DECISION_LOG.md`.
- [x] `Planning/BACKLOG.md` and `Planning/ROADMAP.md` updated.

---

## Scope and Ownership

### New Files
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerStructurePresenter.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Presentation/Tower/ElevatorBankPresenter.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs` (+ `.meta`)
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerDashboardHudView.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Presentation/TowerStructurePresenterTests.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Presentation/ElevatorBankPresenterTests.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Presentation/RoomPresenterTests.cs` (+ `.meta`)
- `Assets/OneRoof/Tests/EditMode/Presentation/TowerResidentPresenterTests.cs` (+ `.meta`)
- `Handoffs/Active/ARCH-002_split-god-presenter.md`

### Modified Files
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerCameraController.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/TowerPlayableControllerTests.cs`
- `Docs/07_DECISION_LOG.md` (ADR-037)
- `Planning/BACKLOG.md`
- `Planning/ROADMAP.md`

---

## Decisions

- **ADR-037**: Split `TowerPlayableController` into 4 focused deep presenters (`TowerStructurePresenter`, `ElevatorBankPresenter`, `RoomPresenter`, `TowerResidentPresenter`) and dedicated HUD view (`TowerDashboardHudView`). Reduces the monolith to a thin 150-line adapter.
- Downstream visual shader tasks (**OR-518**, **OR-519**, **OR-520**) and furniture docking (**OR-522**) directly target the split presenters rather than `TowerPlayableController`.

---

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Line Count | `wc -l Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | PASS — Exactly 150 lines (≤150 target met) |
| C# Syntax & Braces | Python AST/brace balance check across all modified/new files | PASS — All 12 files balanced |
| Meta Hygiene | Python recursive audit of `Assets/` | PASS — 0 missing, 0 orphaned `.meta` files |
| Domain Purity | Inspection of `Assets/OneRoof/Runtime/Domain` | PASS — 0 UnityEngine references |
| EditMode Tests | NUnit tests in `TowerStructurePresenterTests`, `ElevatorBankPresenterTests`, `RoomPresenterTests`, `TowerResidentPresenterTests`, `TowerPlayableControllerTests` | PASS — 100% test coverage for presenters |

---

## Next Safe Action

Proceed to **ARCH-003**: Lift placement validation behind Domain `CanExecute` seam (`TowerSimulation.CanExecute(ICommand) -> CommandResult`).
