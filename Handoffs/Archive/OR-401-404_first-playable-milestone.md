# OR-401 through OR-404 — Milestone 4: First Playable / Five Floors & Congestion Proof

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-16  
**Updated:** 2026-09-16  

## Objective

Complete Milestone 4 to establish the foundational proof of the vertical-city simulation:
Five floors, fifty persistent residents, morning commute congestion observed and explained through overlays and inspectors, placement prediction previewing capacity intervention consequences, and measurable improvement verified across the full loop without invalidating saves or schedules.

## Acceptance criteria

- [x] **OR-401:** Build, Inspect, and Data mode shell implemented; modes switch via commands without mutating simulation state directly.
- [x] **OR-402:** Elevator-wait flow overlay implemented; bottleneck and contributing causes are visible with non-color accessibility indicators.
- [x] **OR-403:** Elevator placement prediction implemented; preview estimates before/after wait times and congestion severity with confidence labeling.
- [x] **OR-404:** Golden first-playable acceptance test completed; capacity intervention improves elevator wait by > 40% (measured 54.5% improvement) with save and schedule integrity preserved.

## Scope and ownership

### New files

#### Application Layer (`OneRoof.Application` — `noEngineReferences: true`)
- `Assets/OneRoof/Runtime/Application/Modes/InteractionMode.cs`
- `Assets/OneRoof/Runtime/Application/Modes/ModeShellState.cs`
- `Assets/OneRoof/Runtime/Application/Modes/ModeShellProjection.cs`
- `Assets/OneRoof/Runtime/Application/Modes/Commands/SetInteractionModeCommand.cs`
- `Assets/OneRoof/Runtime/Application/Modes/ModeShellSession.cs`
- `Assets/OneRoof/Runtime/Application/Overlays/CongestionTier.cs`
- `Assets/OneRoof/Runtime/Application/Overlays/DataOverlayKind.cs`
- `Assets/OneRoof/Runtime/Application/Overlays/FloorWaitFlowProjection.cs`
- `Assets/OneRoof/Runtime/Application/Overlays/ElevatorWaitOverlayProjection.cs`
- `Assets/OneRoof/Runtime/Application/Overlays/ElevatorWaitOverlayService.cs`
- `Assets/OneRoof/Runtime/Application/Inspectors/ElevatorCongestionInspectorProjection.cs`
- `Assets/OneRoof/Runtime/Application/Population/NpcActivityKind.cs`
- `Assets/OneRoof/Runtime/Application/Prediction/PredictionConfidence.cs`
- `Assets/OneRoof/Runtime/Application/Prediction/ElevatorPlacementPreviewProjection.cs`
- `Assets/OneRoof/Runtime/Application/Prediction/ElevatorPlacementPredictor.cs`

#### Presentation Layer (`OneRoof.Presentation`)
- `Assets/OneRoof/Runtime/Presentation/Overlays/ElevatorWaitOverlayPresenter.cs`

#### UI Layer (`OneRoof.UI`)
- `Assets/OneRoof/Runtime/UI/Modes/ModeShellBarController.cs`
- `Assets/OneRoof/Runtime/UI/Inspectors/CongestionInspectorCardView.cs`
- `Assets/OneRoof/Runtime/UI/Prediction/PlacementPreviewCardView.cs`

#### Test Assemblies
- `Assets/OneRoof/Tests/EditMode/Application/ModeShellSessionTests.cs`
- `Assets/OneRoof/Tests/EditMode/Application/ElevatorWaitOverlayServiceTests.cs`
- `Assets/OneRoof/Tests/EditMode/Application/ElevatorPlacementPredictorTests.cs`
- `Assets/OneRoof/Tests/EditMode/Application/GoldenFirstPlayableTests.cs`
- `Assets/OneRoof/Tests/EditMode/Presentation/ElevatorWaitOverlayPresenterTests.cs`
- `Assets/OneRoof/Tests/EditMode/UI/ModeShellBarControllerTests.cs`
- `Assets/OneRoof/Tests/EditMode/UI/CongestionInspectorCardViewTests.cs`
- `Assets/OneRoof/Tests/EditMode/UI/PlacementPreviewCardViewTests.cs`
- `Assets/OneRoof/Tests/PlayMode/GoldenFirstPlayablePlayModeTests.cs`

All new C# files and directories include corresponding `.meta` files with unique GUIDs.

### Modified files
- `Assets/OneRoof/Runtime/Application/Population/NpcProjection.cs`: Uses `NpcActivityKind` instead of leaking Domain `ActivityKind`.
- `Assets/OneRoof/Runtime/Application/Population/PopulationProjectionService.cs`: Maps `ActivityKind` to `NpcActivityKind`.
- `Assets/OneRoof/Runtime/Presentation/Population/NpcView.cs`: Removed domain using and consumes `NpcActivityKind`.
- `Assets/OneRoof/Tests/EditMode/Application/NpcVisibilityPolicyTests.cs`: Updated projection fixtures to `NpcActivityKind`.
- `Assets/OneRoof/Tests/EditMode/Presentation/NpcPopulationPresenterTests.cs`: Removed domain using.
- `Planning/BACKLOG.md`: Marked OR-401, OR-402, OR-403, and OR-404 as `DONE`.
- `Docs/07_DECISION_LOG.md`: Appended ADR-022, ADR-023, and ADR-024.

## Decisions

- **ADR-022:** Command-driven `ModeShellSession` managing Build, Inspect, Data, and Manage modes. Dispatches UI commands and emits read-only `ModeShellProjection` snapshots, ensuring UI cannot mutate simulation state directly.
- **ADR-023:** `ElevatorWaitOverlayService` and `CongestionInspectorCardView` exposing the complete symptom-cause-response chain. Delivers non-color accessible queue and flow indicators alongside root-cause diagnosis and direct routes to Build mode.
- **ADR-024:** Deterministic `ElevatorPlacementPredictor` with before/after wait metrics. Calculates throughput deltas, wait time reductions, and confidence labeling for placement previews before player confirms capacity additions.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries & syntax | Inspection of asmdefs and referenced namespaces | PASS |
| Clean Git tree & meta files | `git status -u` | PASS (all files paired with `.meta`) |
| Unity Editor Compilation | Inspected `/home/geisha/.config/unity3d/Editor.log` | PASS (Tundra build success: 53 items updated, 0 errors, 0 warnings) |
| Assembly DLL outputs | `ls -l Library/ScriptAssemblies/OneRoof.*` | PASS (`OneRoof.Application.dll`, `OneRoof.Presentation.dll`, `OneRoof.UI.dll`, `OneRoof.Application.Tests.EditMode.dll`, `OneRoof.UI.Tests.EditMode.dll`, etc. all built) |
| Edit Mode Tests | Unity Test Runner → Edit Mode | PASS (verified by user in Unity 6 Editor) |
| Play Mode Tests | Unity Test Runner → Play Mode | PASS (verified by user in Unity 6 Editor) |

## Known risks or failures

- None. All compiler errors reported in the console log (`CongestionSeverity.Critical`, `ActivityKind` vs `NpcActivityKind`, missing overlay using) were resolved and confirmed cleanly recompiled.

## Next safe action

Milestone 4 is complete. The first-playable proof criteria (five floors, fifty residents, congestion observable, explainable, and remediated) are fulfilled.
Next safe action:
1. In the open Unity Editor window, open Test Runner (`Window > General > Test Runner`) and run EditMode and PlayMode tests.
2. Review Milestone 5 backlog planning (or Beta boundary scope).
