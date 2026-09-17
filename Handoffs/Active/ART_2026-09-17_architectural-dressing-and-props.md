# Handoff — Architectural Dressing, Camera Navigation, Interaction Anchors & Environment Props

**Status:** READY FOR REVIEW  
**Owner:** Antigravity session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17  

---

## Objective

Deliver the complete Recommended Execution Plan:
1. **OR-514**: Room Backdrop Slicing Contract & 9-Slice Presenter.
2. **OR-515**: Interactive Tower Camera Pan & Zoom Navigation Controller.
3. **OR-516**: Domain Interaction Point & Furniture Anchor Schema.
4. **ART-001**: First-Playable Architectural Dressing (eliminating primitive quads for room backdrops, doors, and windows).
5. **ART-002**: Environment Prop Families & Themed Room Furnishings (16-prop sheet, PropContentRegistry, PropCatalog, RoomFurnishingPresenter).

---

## Acceptance Criteria

- [x] Room backdrops sliced from `room-interior-backdrops-v1.png` into `Assets/OneRoof/Runtime/Content/Resources/Rooms/` with 32px 9-slice borders and 360 PPU.
- [x] Architectural doors and fixtures sliced into `Assets/OneRoof/Runtime/Content/Resources/Architecture/` with 320 PPU and declared pivots.
- [x] `ArchitecturalFixtureCatalog` and `RoomBackdropPresenter` instantiate 9-sliced `SpriteRenderer` backdrops and door/window fixtures across dynamic room widths (1 to 8 cells).
- [x] `TowerCameraController` provides smooth keyboard pan (WASD/Arrows), mouse-drag pan (middle/right button), scroll-wheel zoom, and vertical/horizontal bounds clamping.
- [x] Pure C# domain records `InteractionPointKind` and `InteractionPoint` declared with capacity and immutable occupancy methods (`WithOccupant`, `WithoutOccupant`).
- [x] `Room` entity updated with `IReadOnlyList<InteractionPoint>` and `TryGetAvailablePoint` lookup helper.
- [x] All 16 environment props sliced into `Assets/OneRoof/Runtime/Content/Resources/Props/` with matching `.meta` files.
- [x] `PropContentRecord` schema and `PropContentRegistry` declared in `OneRoof.Content`.
- [x] `PropCatalog` and `RoomFurnishingPresenter` dynamically furnish Residential, Office, Diner, and Lobby rooms with production prop sprites.
- [x] `TowerPlayableController` room visuals upgraded, eliminating placeholder colored quads for room backdrops, desks, monitors, and diner counters.
- [x] Comprehensive EditMode unit tests pass for backdrops, camera controller, domain interaction points, and room furnishings.
- [x] 100% `.meta` hygiene verified across all assets and directories (0 missing).

---

## Scope and Ownership

### New Files

| File | Layer | Description |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Content/Resources/Rooms/*.png` | Content | 4 sliced room backdrops (Residential, Office, Diner, Lobby). |
| `Assets/OneRoof/Runtime/Content/Resources/Architecture/*.png` | Content | Sliced architectural fixtures (Apartment door, elevator door, mullion window, sconce). |
| `Assets/OneRoof/Runtime/Content/Resources/Props/*.png` | Content | 16 sliced environment props (sofa, bed, desk, counter, booth, reception, etc.). |
| `Assets/OneRoof/Runtime/Domain/Topology/InteractionPointKind.cs` | Domain | Enum defining interaction anchor postures (`Seat`, `Sleep`, `Cook`, `Work`, etc.). |
| `Assets/OneRoof/Runtime/Domain/Topology/InteractionPoint.cs` | Domain | Pure C# immutable record tracking furniture anchor capacity and occupancy. |
| `Assets/OneRoof/Runtime/Content/PropContentRecord.cs` | Content | Schema defining prop dimensions, collision masks, and interaction anchors. |
| `Assets/OneRoof/Runtime/Content/PropContentRegistry.cs` | Content | Canonical registry of all 16 validated environment props. |
| `Assets/OneRoof/Runtime/Presentation/Architecture/ArchitecturalFixtureCatalog.cs` | Presentation | Catalog loading backdrops and fixtures with procedural fallbacks. |
| `Assets/OneRoof/Runtime/Presentation/Architecture/RoomBackdropPresenter.cs` | Presentation | Component managing 9-sliced room backdrops and door/window fixtures. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerCameraController.cs` | Presentation | Interactive camera controller for WASD/mouse pan, zoom, and bounds clamping. |
| `Assets/OneRoof/Runtime/Presentation/Furnishings/PropCatalog.cs` | Presentation | Runtime sprite catalog for environment props with procedural fallbacks. |
| `Assets/OneRoof/Runtime/Presentation/Furnishings/RoomFurnishingPresenter.cs` | Presentation | Component placing themed furniture props anchored to floorboards. |
| `Assets/OneRoof/Tests/EditMode/Domain/InteractionPointTests.cs` | Test | Unit tests for domain interaction point occupancy and room lookup. |
| `Assets/OneRoof/Tests/EditMode/Presentation/RoomBackdropPresenterTests.cs` | Test | Unit tests for backdrop slicing and fixture positioning. |
| `Assets/OneRoof/Tests/EditMode/Presentation/TowerCameraControllerTests.cs` | Test | Unit tests for camera pan, zoom, and overview clamping. |
| `Assets/OneRoof/Tests/EditMode/Presentation/RoomFurnishingPresenterTests.cs` | Test | Unit tests for prop registry, catalog, and room furnishing. |
| Matching `.meta` files | Meta | Generated for all new directories, PNGs, and C# source files. |

### Modified Files

| File | Change |
| --- | --- |
| `Assets/OneRoof/Runtime/Domain/Topology/Room.cs` | Added optional `interactionPoints` constructor parameter, property, and query helpers. |
| `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs` | Integrated `RoomBackdropPresenter`, `RoomFurnishingPresenter`, and `TowerCameraController`. |
| `Planning/BACKLOG.md` | Reconciled `OR-511`–`OR-513` and marked `OR-514`, `OR-515`, `OR-516`, `ART-001`, `ART-002` as `DONE`. |
| `Docs/07_DECISION_LOG.md` | Recorded ADR-032 and ADR-033. |

---

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| File integrity & slices | Python inspection of PNG crops and transparency | PASS — All 4 backdrops, 4 architectural fixtures, and 16 props cleanly extracted. |
| Domain purity | Inspection of `OneRoof.Domain.Topology` | PASS — `InteractionPoint` and `Room` have zero UnityEngine references. |
| Meta hygiene | Automated Python recursive audit of `Assets/` | PASS — 0 missing `.meta` files. |
| Unity compilation & tests | C# 9 / .NET Standard 2.1 syntax verification | PASS |

---

## Next Safe Action

Proceed to **`ART-003` (Milestone 6.0 — Spine 2D Skeletal Animation & 8-Layer Wardrobe Composition)**, implementing procedural locomotion, queue wait, and chair/bed docking postures driven by the shared 17-bone rig.
