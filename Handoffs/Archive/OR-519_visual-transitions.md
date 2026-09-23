# OR-519 — Visual construction and demolition transitions

Status: DONE

## Owned files

- `Assets/OneRoof/Runtime/Presentation/Tower/VisualEffectsPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerStructurePresenter.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/VisualEffectsPresenterTests.cs`

## Result

New room roots and floor slabs start a presentation-only construction transition. Demolished room roots remain briefly in play mode and receive a dissolve/fade transition before destruction. The component writes standard AllIn1-compatible dissolve/fade/glow properties while retaining a URP-unlit fallback.

## Validation

- User verified Unity compilation through the live Pipeline after correcting the namespace ambiguity in `VisualEffectsPresenter.cs`.
- Focused EditMode test command — NOT RUN from this session: its CLI cannot discover the live endpoint on port 7800.

## Known risk / next safe action

Close or restore the open Unity Editor, run the focused EditMode suite, then inspect construction and demolition in `Testbed_Transit`.
