# BUGFIX — Golden Elevator Cost Assertion

**Status:** DONE  
**Owner:** Codex  
**Started:** 2026-09-19  
**Updated:** 2026-09-19

## Objective

Bring the first-playable golden acceptance assertion into alignment with the canonical elevator placement cost.

## Acceptance criteria

- [x] The golden test asserts the predictor's canonical elevator cost rather than a stale literal.
- [x] Live-Pipeline compilation and Application Edit Mode assembly pass.

## Changes made

- `Assets/OneRoof/Tests/EditMode/Application/GoldenFirstPlayableTests.cs` — replaces stale `$500` assertion with `ElevatorPlacementPredictor.DefaultElevatorCost`.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Live Pipeline preflight | `unity pipeline list`, `unity status`, `unity command editor_status` | PASS — one ready editor on port 7800 |
| Compile | `unity command recompile`, followed by `unity command recompile_status` after the expected reload | PASS — completed; `failed: false` |
| Golden test | `unity command run_tests --mode editor --filter OneRoof.Application.Tests.EditMode.GoldenFirstPlayableTests.GoldenFirstPlayable_FullLoop_DemonstratesExplanationChainAndMeasurableImprovement` | PASS — 1 / 1 |
| Application Edit Mode | `unity command run_tests --mode editor --filter_type assembly --filter OneRoof.Application.Tests.EditMode` | PASS — 28 / 28 |

## Next safe action

Proceed to ART-003, the next unblocked roadmap item.
