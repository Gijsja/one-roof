# OR-520 — Congestion agitation aura

Status: DONE

## Owned files

- `Assets/OneRoof/Runtime/Presentation/Tower/VisualEffectsPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/ElevatorBankPresenter.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/ElevatorBankPresenterTests.cs`

## Result

Overcrowded cars pulse a warm aura from the immutable elevator capacity projection. Queued residents begin pulsing after ten wait ticks and reach full strength at thirty ticks. The existing emote progression remains intact and the effect has a transparent unlit fallback.

## Validation

- Unity compilation — user verified through the live Pipeline after the `VisualEffectsPresenter` namespace correction.
- Focused EditMode suite — NOT RUN from this session; the local CLI cannot discover the live endpoint on port 7800.

## Next safe action

Run the focused presenter tests after the Editor is available and visually verify both the car and queued-resident thresholds during a seeded rush.
