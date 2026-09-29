# Delivered milestone bug review and fixes

## Delivered

- Room demolition is rejected while a resident still uses the room as a home or current location, or an active trip references the room as an endpoint or route node. `force` does not bypass this state-integrity guard.
- After safe demolition, workers whose business was removed are reassigned through the existing outside-work fallback.
- Added regression coverage for forced demolition of an occupied home and a room targeted by an active trip.
- OR-803 utility evaluation now consumes an immutable operational snapshot. Failed substations, risers, transformers, pumps, boosters, waste chutes, and collectors no longer contribute capacity or connectivity; backup power remains additive.
- Added a simulation integration regression that wears installed utilities into failure and checks power, water, and waste service projections.

## Validation

- `git diff --check` passed.
- Connected Unity Pipeline MCP on Unity 6000.3.24f1: Editor reports `compiling=false`, `compilationFailed=false`, and the current Console reports 0 errors / 0 warnings after the follow-up namespace fix in `FloorThemeCatalog.cs`.
- Focused EditMode via Pipeline MCP: `CanExecute_DemolishRoom` 3/3 passed; `DemolishRoom_OccupiedApartment_RejectsEvenWhenForced` 1/1 passed; `FailedUtilityEquipment_DisconnectsPowerWaterAndWasteService` 1/1 passed.
- Full EditMode via Pipeline MCP: 754 total, 751 passed, 3 failed. Failures: `GridPlacementControllerTests.IsPointerOverUI_DoesNotReservePaletteAreaAfterToolSelection`; `RoomPresenterTests.EnsureRoomViews_AssignsMaterialToBackdropAndFurnishingRenderers`; `RoomPresenterTests.EnsureRoomViews_WhenRoomDemolished_DestroysRoomAndAllChildrenWithoutOrphans`.
- Full PlayMode via Pipeline MCP: 11/11 passed. The Editor is now stopped; the previously observed test-runner exceptions were from an earlier attempt made during Play Mode.

## Cleanup sprint follow-up — 2026-09-29

- Reproduced all three EditMode failures individually before changing tests.
- `GridPlacementControllerTests.IsPointerOverUI_DoesNotReservePaletteAreaAfterToolSelection` depended on the live Game view height: at this Editor resolution its test point overlapped the top-left HUD. The test now pins a 640×480 viewport, matching its intended palette/context geometry.
- The material test expected opaque `_worldMaterial` on every furnishing prop, contradicting the alpha-safe `Prop_DefaultTransparentMaterial` used by `RoomFurnishingPresenter`. It now checks the transparent queue, alpha blend factors, and disabled ZWrite while retaining world-material checks for backdrops, doors, and windows.
- The demolition presentation test selected the first residential room, which is occupied/protected in the standard fixture. It now selects a room only after `CanExecute` confirms the forced demolition is safe, preserving the domain guard and independently exercising view cleanup.
- After script recompile: each adjusted regression passed individually; full EditMode passed **754/754**; full PlayMode passed **11/11**. Editor compilation succeeded and current Console reports **0 errors / 0 warnings**. The PlayMode suite ran asynchronously because the connected Editor requires a domain reload to enter Play Mode.
- `git diff --check` passed. Pipeline MCP did not return a persisted XML artifact path for these test runs.

## Re-review findings

- Independent review identified the OR-803 utility gap and this task now wires failed equipment into service evaluation. The earlier stabilization roadmap also records the issue as BUG-13.
- The follow-up review found another high-confidence risk: leasing can commit an apartment before checking that an Outside-to-home route exists. If move-in trip creation fails, the resident can remain outside without a retry. Evidence: `LeasingDemandSystem.EvaluateLeasingDemand()`, `TowerSimulation.AdvanceOneTick()`, and `ScheduleTripGenerator.CreateMoveInTrip()`. This risk is transferred to the OR-1003 City Status acceptance test in `Planning/BACKLOG.md`, which now requires deferred/retried leasing when no Outside-to-home route exists.

## Post-fog validation — 2026-09-29

- Focused fog regression on the final mesh change: **1/1 passed**, `/tmp/one-roof-cleanup-fog-editmode.xml`.
- Isolated full EditMode rerun: **754 passed, 0 failed, 1 explicit performance test skipped**, `/tmp/one-roof-cleanup-editmode-final.xml`.
- Isolated full PlayMode rerun: **11/11 passed**, `/tmp/one-roof-cleanup-playmode-graphics.xml`.
- The demolition and utility fixes are complete, and their outstanding move-in-route risk is now owned by OR-1003. This handoff can be archived.

## Next safe action

The cleanup sprint EditMode/PlayMode baseline is green and the remaining move-in route risk is assigned to OR-1003. The related interactive and performance gates remain open in their owning handoffs.
