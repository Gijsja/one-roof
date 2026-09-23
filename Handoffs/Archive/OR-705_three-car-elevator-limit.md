# OR-705 — Three-Car Elevator Limit

**Status:** DONE
**Owner:** Codex
**Started:** 2026-09-20
**Updated:** 2026-09-20

## Objective

Limit elevator banks to three cars so the presentation never clips into rooms.

## Acceptance criteria

- [x] The domain cap is 3 and construction/restoration cannot exceed it.
- [x] Command, session/UI, and prediction paths reject a fourth car without deducting money.
- [x] Presenter and PlayMode checks keep three car bounds within the shaft.

## Scope and ownership

Changed files:

- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Runtime/Domain/TowerSimulation.cs`
- `Assets/OneRoof/Runtime/Application/Tower/TowerSimulationSession.cs`
- `Assets/OneRoof/Runtime/Application/Prediction/ElevatorPlacementPredictor.cs`
- `Assets/OneRoof/Runtime/Presentation/Tower/TowerPlayableController.cs`
- Related EditMode and PlayMode tests.

## State at handoff

`ElevatorBank.MaxCarsPerBank` is the single source of truth. Direct public capacity additions now execute the same command validation as UI placement; the bank additionally rejects invalid direct additions. Prediction reads the domain cap, and the UI only refreshes car views when placement succeeds.

## Changes made

- Reduced `ElevatorBank.MaxCarsPerBank` from 4 to 3 and enforced it in construction and `AddCar`.
- Routed `TowerSimulation.AddElevatorCar` and `TowerSimulationSession.AddCapacity` through command validation.
- Bound prediction capacity to the domain constant and protected the confirm UI from failed placement.
- Added fourth-car rejection, prediction, UI, layout, and PlayMode regression coverage.

## Decisions

- Do not silently allow a fourth direct API car: the domain owns this permanent temporary cap, while the presentation test independently checks the three-car geometry.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| EditMode domain | `TowerSimulationCanExecuteTests` | PASS — 12/12; `/tmp/one-roof-or705-domain.xml` |
| EditMode prediction | `ElevatorPlacementPredictorTests` | PASS — 4/4; `/tmp/one-roof-or705-predictor.xml` |
| EditMode controller | `TowerPlayableControllerTests` | PASS — 11/11; `/tmp/one-roof-or705-controller.xml` |
| EditMode layout | `ElevatorBankPresenterTests` | PASS — 9/9; `/tmp/one-roof-or705-presenter.xml` |
| PlayMode integration | `TowerAtmosphereAndElevatorLimitsPlayModeTests` | PASS — 1/1; `/tmp/one-roof-or704-or705-playmode.xml` |

## Known risks or failures

- Existing saves containing four cars now reject on elevator-bank reconstruction instead of loading an invalid visual layout. No in-repository fixture contains such a bank; a migration should be added before shipping a version that has distributed four-car saves.

## Next safe action

Proceed with OR-706 interactive floor-slab placement, keeping its test at the Build-mode interaction boundary.

## References

- Backlog: `OR-705`
- Evidence: `/tmp/one-roof-or705-domain.xml`, `/tmp/one-roof-or705-predictor.xml`, `/tmp/one-roof-or705-controller.xml`, `/tmp/one-roof-or705-presenter.xml`, `/tmp/one-roof-or704-or705-playmode.xml`
