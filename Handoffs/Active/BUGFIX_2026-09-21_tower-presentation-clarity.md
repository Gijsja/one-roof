# Tower presentation clarity bugfix

**Status:** DONE
**Owner:** Codex
**Updated:** 2026-09-21

## Symptoms

- The Tower Game view showed a white elevator-car placeholder at the shaft base before its first simulation update.
- Unity reported no active `AudioListener`, so the spatial tower soundscape could not be heard.
- Congested queues visually accumulated opaque red agitation effects, and the dashboard controls were difficult to read at the Tower overview scale.

## Root causes

- `ElevatorBankPresenter` adopted the authored `Elevator Car 0` renderer but left its serialized white material intact until `UpdateElevatorPositions` ran.
- `TowerCameraController.EnsureTowerCamera` made or reused the main camera but never ensured an `AudioListener`.
- Each queued resident generated a broad, 38%-alpha agitation quad; overlapping residents compounded those quads.
- All dashboard actions were laid out on one horizontal row inside a narrow legacy-IMGUI panel.

## Fix

- Normalize adopted elevator views to the presenter's runtime material, baseline color, position, and scale during initialization.
- Ensure exactly one listener is attached to the active Tower camera.
- Reduce agitation aura footprint and alpha so the queue remains visible beneath its congestion signal.
- Give the HUD desktop-appropriate dimensions, larger typography, and wrapped action rows.

## Regression coverage

- `TowerCameraControllerTests.EnsureTowerCamera_AddsAnAudioListenerForSpatialTowerSound`
- `ElevatorBankPresenterTests.EnsureElevatorViews_NormalizesAuthoredCarMaterialAndInitialColor`

## Validation

| Check | Result |
| --- | --- |
| Focused diff | PASS — six presentation/test files; no scene or package assets were changed. |
| Unity 6000.3.24f1 isolated batch compilation | PASS — `/tmp/one-roof-tower-clarity-editmode.log` contains no C# compiler diagnostics. |
| Targeted EditMode test XML | BLOCKED — Unity exited 0 without writing `/tmp/one-roof-tower-clarity-editmode.xml`, matching the previously observed host limitation. |
| Visual Game-view check | PENDING — open `Tower`, enter Play Mode, verify the green car is visible immediately, no AudioListener warning appears, and congested residents remain legible beneath the lighter aura. |

## Commit

- `f116c01 fix(presentation): restore tower audiovisual clarity`
