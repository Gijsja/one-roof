# OR-522 — Furniture anchor docking

Status: DONE

## Owned files

- `Assets/OneRoof/Runtime/Presentation/Furnishings/RoomFurnishingPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/VisualEffectsPresenterTests.cs`

## Result

Resident placement maps sleep/work/seat activities to OR-516 interaction points when a room declares them. It converts local cell offsets to tower coordinates without Unity references in domain records. Rooms without declared points gracefully dock to their rendered bed, desk, sofa, or booth; only then do they use the former interior-slot fallback.

## Validation

- Unity compilation — user verified through the live Pipeline after the `VisualEffectsPresenter` namespace correction.
- Focused EditMode suite — NOT RUN from this session; the local CLI cannot discover the live endpoint on port 7800.

## Next safe action

Run the focused suite and inspect residential, office, and diner rooms with both declared-anchor and fallback layouts.
