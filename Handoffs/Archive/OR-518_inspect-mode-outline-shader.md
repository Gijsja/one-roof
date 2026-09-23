# OR-518 — Inspect Mode Selection & Hover Outline Shader Presenter

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-18  
**Updated:** 2026-09-18  

---

## Objective

Deliver the Inspect mode visual feedback experience:
- Hovering and selecting rooms, elevator shafts, and residents in Inspect mode renders pixel-perfect outline silhouettes and soft glow auras (`OUTBASE_ON`, `GLOW_ON`).
- Direct all hit-testing, bounds calculation, and target lookup through the modular split presenters (`RoomPresenter`, `ElevatorBankPresenter`, `TowerResidentPresenter`) established in **ARCH-002**.
- Keep `TowerPlayableController.cs` ≤150 lines (currently 147 lines).

---

## Acceptance Criteria

- [x] High-contrast pixel-perfect outline silhouettes (`OUTBASE_ON`, `GLOW_ON`) when hovering or selecting rooms, elevator shafts, and residents in Inspect mode.
- [x] Distinct visual styling between hover and selection:
  - **Hover**: Cyan/soft white outline (`#59D9FF`) with soft cyan glow aura.
  - **Selection**: Radiant gold/amber outline (`#FFD933`) with intense warm amber glow aura.
- [x] Room and elevator shaft highlights render sharp rectangular boundary frames with clear interiors.
- [x] Resident highlights render pixel-perfect character contours using `ONLYOUTLINE_ON`, hugging hair, clothing, and footwear silhouettes without obscuring the character.
- [x] Dynamic silhouette tracking: resident outline follows moving residents seamlessly across corridors and elevator landings.
- [x] Split presenters extended cleanly without leaking into domain:
  - `RoomPresenter`: `TryGetRoomAt(worldPos, topology, out id, out bounds)` and `TryGetRoomBounds(id, topology, out bounds)`.
  - `ElevatorBankPresenter`: `IsPointerInShaft(worldPos, out bounds)` and `GetShaftBounds()`.
  - `TowerResidentPresenter`: `TryGetResidentAt(worldPos, radius, out id, out bounds, out sprite, out tr)` and `TryGetResidentView(id, out bounds, out sprite, out tr)`.
- [x] Deselection support: clicking empty space, right-clicking, or pressing Escape clears selection and hides outlines.
- [x] Automatic URP Unlit transparent fallback for environments where `AllIn1SpriteShader` is absent.
- [x] 100% test pass rate: 277 EditMode tests passing (0 failures).
- [x] `TowerPlayableController.cs` line count is strictly ≤150 lines (147 lines).
- [x] 100% `.meta` hygiene verified across all assets.

---

## Scope and Ownership

### New Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Presentation/Tower/InspectOutlinePresenter.cs` | Presentation | Component rendering outline and glow silhouettes using `AllIn1SpriteShader` (`OUTBASE_ON`, `GLOW_ON`, `ONLYOUTLINE_ON`). |
| `Assets/OneRoof/Runtime/Presentation/Tower/InspectOutlinePresenter.cs.meta` | Meta | Meta asset. |
| `Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs` | Presentation | Controller managing pointer hit-testing, hover state, and selection dispatch in Inspect mode. |
| `Assets/OneRoof/Runtime/Presentation/Tower/InspectSelectionController.cs.meta` | Meta | Meta asset. |
| `Assets/OneRoof/Tests/EditMode/Presentation/InspectOutlinePresenterTests.cs` | Test | Unit tests for outline materials, box/sprite silhouettes, and clearing. |
| `Assets/OneRoof/Tests/EditMode/Presentation/InspectOutlinePresenterTests.cs.meta` | Meta | Meta asset. |
| `Assets/OneRoof/Tests/EditMode/Presentation/InspectSelectionControllerTests.cs` | Test | Unit tests for hit-test prioritization (Resident > Shaft > Room) and selection dispatch. |
| `Assets/OneRoof/Tests/EditMode/Presentation/InspectSelectionControllerTests.cs.meta` | Meta | Meta asset. |
| `Handoffs/Active/OR-518_inspect-mode-outline-shader.md` | Docs | Task handoff. |

### Modified Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Application/Modes/ModeShellSession.cs` | Application | Added explicit `ClearSelection()` method. |
| `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs` | Presentation | Added `TryGetRoomBounds` and `TryGetRoomAt`. |
| `Assets/OneRoof/Runtime/Presentation/Tower/ElevatorBankPresenter.cs` | Presentation | Added `GetShaftBounds` and `IsPointerInShaft`. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs` | Presentation | Added `TryGetResidentView` and `TryGetResidentAt`. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Presentation | Integrated `InspectSelectionController` and `InspectOutlinePresenter` (147 lines). |
| `Assets/OneRoof/Tests/EditMode/Presentation/RoomPresenterTests.cs` | Test | Added tests for room bounds and hit-testing. |
| `Assets/OneRoof/Tests/EditMode/Presentation/ElevatorBankPresenterTests.cs` | Test | Added tests for elevator shaft bounds and hit-testing. |
| `Assets/OneRoof/Tests/EditMode/Presentation/TowerResidentPresenterTests.cs` | Test | Added tests for resident view and spatial hit-testing. |
| `Assets/OneRoof/Tests/EditMode/Presentation/TowerPlayableControllerTests.cs` | Test | Added assertions verifying `InspectSelection` and `InspectOutline` subcomponents. |
| `Planning/BACKLOG.md` | Planning | Updated `OR-518` status to `DONE`. |

---

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Compilation | `unity command recompile` | PASS — 0 compilation errors, 0 warnings |
| EditMode Tests | `unity command run_tests --mode editor` | PASS — 277 / 277 passed (100%) |
| Presentation Suite | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Presentation.Tests.EditMode` | PASS — 33 / 33 passed (100%) |
| Meta Hygiene | Recursive Python audit of `Assets/` | PASS — 0 missing, 0 orphaned `.meta` files |
| Line Count | `wc -l Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | PASS — 147 lines (≤150 target met) |
| Play Mode Visual Verification | Live Play Mode eval + camera capture | PASS — Room, elevator shaft, and resident silhouettes verified in live engine |

---

## Next Safe Action

Proceed to **OR-519**: Demolition dissolve and construction scanline shader transitions (`RoomPresenter` + `TowerStructurePresenter`).
