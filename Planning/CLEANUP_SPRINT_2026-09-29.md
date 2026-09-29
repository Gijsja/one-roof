# Cleanup Sprint — Open Handoffs and Validation

**Prepared:** 2026-09-29
**Source:** Active handoffs, `Planning/BACKLOG.md`, connected Unity Pipeline MCP runs, and isolated Unity CLI validation on Unity 6000.3.24f1.
**Purpose:** Collect remaining test, visual, and performance work before closing or archiving the affected handoffs.
**Active handoff:** `Handoffs/Active/HANDOFF_2026-09-29_cleanup-sprint.md`.

## Recorded Validation Baseline

- At the initial connected-Editor check, compilation was idle and successful (`compilationFailed=false`) and Console showed **0 errors / 0 warnings**. Previous entries in the retained buffer included an earlier fixed `FloorThemeCatalog` compile error and a failed test-run attempt made while already in Play Mode. The shared Editor later became user-active; the final suite reruns below were made from an isolated project copy.
- Focused demolition validation: `CanExecute_DemolishRoom` **3/3 passed**, `DemolishRoom_OccupiedApartment_RejectsEvenWhenForced` **1/1 passed**.
- Failed utility equipment integration: `FailedUtilityEquipment_DisconnectsPowerWaterAndWasteService` **1/1 passed**.
- Floor deck and theme catalog tests: **9/9 passed**.
- Full EditMode: **751/754 passed; 3 failed**. Failures:
  1. `GridPlacementControllerTests.IsPointerOverUI_DoesNotReservePaletteAreaAfterToolSelection`
  2. `RoomPresenterTests.EnsureRoomViews_AssignsMaterialToBackdropAndFurnishingRenderers`
  3. `RoomPresenterTests.EnsureRoomViews_WhenRoomDemolished_DestroysRoomAndAllChildrenWithoutOrphans`
- Full PlayMode: **11/11 passed**. The Editor is currently stopped after validation.
- Important baseline limitation: the full EditMode run used the shared working checkout, which has uncommitted changes in presentation, domain, tests, settings, and proposed art. Do not attribute all three failures to the last committed change without reproducing them on a clean or otherwise controlled baseline. The pointer-area test resembles the pre-existing failure noted in the archived Steward UI shell handoff.

## Sprint Work

### 1. Restore a clear, green Editor test baseline

**Handoffs:** `Handoffs/Archive/HANDOFF_2026-09-29_demolition-orphaning-fix.md`; archived Steward UI shell handoff for the pointer-area baseline.
**Resolved tests:** The three full EditMode failures above were reproduced individually and corrected as test-contract/fixture issues (details below). The fog presenter received a visual-quality fix later in this sprint.

- Re-run each failure alone and record its result and current source/working-tree context.
- For the material assertion, decide whether the transparent fallback material is intended; if so, align the presenter contract and test. Preserve alpha-blended prop rendering.
- Investigate the demolition presenter child-count failure independently from the domain guard. The domain occupied-home and active-trip guards already pass focused checks.
- Check the pointer-area failure against the previous known failure before changing placement geometry or input behavior.
- After justified fixes, run all EditMode tests and the full PlayMode suite. Acceptance: no unexpected failures, current Console has no new errors or warnings, and the handoff records exact counts and artifacts.

**Earlier 2026-09-29 follow-up:** All three failures were reproduced individually and resolved as test-contract/fixture issues: the pointer test now fixes its viewport at 640×480; the furnishing assertion now checks the intentional transparent prop material and alpha blend state; and the demolition view test selects a room accepted by the domain guard. Unity recompile succeeded, focused regressions passed 3/3, full EditMode passed **754/754**, full PlayMode passed **11/11**, and the connected Editor then showed **0 errors / 0 warnings**. The MCP test runner did not provide XML artifact paths. Details are recorded in the archived demolition handoff.

**Late follow-up:** The manual weather cycle exposed opaque rectangular fog puffs. `PixelRainPresenter` now uses low-opacity radial meshes with transparent edges. Current full EditMode passed **754**, failed **0**, skipped **1 explicit performance test** (`/tmp/one-roof-cleanup-editmode-radialfog.xml`). Current full graphics PlayMode passed **11/11**, 0 failed, 0 skipped (`/tmp/one-roof-cleanup-playmode-full-gui.xml`); the fog visibility case ran with a non-Null graphics device. Its updated ground-level capture is `Handoffs/Evidence/cleanup-sprint-2026-09-29/weather-fog-automated-radial.png`; it shows no panel edges and keeps the scene readable, though the puffs remain sparse. The shared Editor was not used for this isolated run. Initial CLI attempts without display/UPM IPC access failed before tests; rerunning through the graphics-enabled route completed successfully. The latest live Editor status reported compilation idle, `compilationFailed=false`, and **0 current errors / 0 current warnings**; retained history has 15 errors and 1 warning, including three command errors from mistyped AssetDatabase paths and earlier unrelated entries. A Console clear was rejected by automatic review to preserve newly observed error evidence, and no alternate clearing method was used. Weather-cycle and fog captures are preserved in `Handoffs/Evidence/cleanup-sprint-2026-09-29/`.

### 2. Finish interactive visual checks

**Handoffs:** `HANDOFF_2026-09-25_exterior-windows-lighting-facade.md`, `HANDOFF_2026-09-29_weather-system-redesign.md`, `HANDOFF_2026-09-27_secret-undercity-expansion.md`.

- **Captured 2026-09-29:** The actual serialized `Tower_GroundStart` was rendered at 06:00 dawn, 12:00 day, and 22:00 night in an isolated graphics-enabled fixture. The light cone and room lighting change by phase. The current camera shows the full single-floor facade/ground cutaway but cannot establish multistory composition. The facade and sky remain similarly dark across states, and noon/night captures showed wavy/striped patterns in the lobby interior. A 1/1 window-light renderer A/B (`/tmp/one-roof-windowlight-ab-confirm.xml`) falsified that renderer list as the stripe source under the test conditions. A second 1/1 A/B (`/tmp/one-roof-room-backdrop-ab.xml`) hid only the authored `RoomView_14/Backdrop` renderer, but the pattern was absent in both states; hiding it made plants/interior contents more visible. A separate runtime-room pattern appeared during the construction transition, but the steady-state repeat after `IsTransitioning == false` did not show it; targeted MPB clearing also did not change that artifact crop. These runtime probes do not identify the separate authored-ground artifact. Keep the cases separate and continue a narrow reproduction before production edits. Retake broader exterior views to verify window/glass/cone contrast, lobby framing, foundation/earth depth, and composition. Captures: `tower-groundstart-0600-dawn.png`, `tower-groundstart-1200-day.png`, `tower-groundstart-2200-night.png` in the cleanup evidence folder.
- Exercise valid and invalid placement hover, expand the ground slab, and add a floor to check facade/deck alignment.
- **Completed 2026-09-29:** Called `CycleWeatherOverride` in `Tower_GroundStart` through Clear, Drizzle, Rain, Storm, Fog, Snow, then Auto. HUD condition/time labels updated and rain/storm precipitation plus snow were visible. Captures are in `Handoffs/Evidence/cleanup-sprint-2026-09-29/`. The initial fog capture showed broad opaque panels; radial low-opacity geometry replaced them. The current full graphics PlayMode suite passes and its focused fog camera capture shows no panel artifact with readable architecture, but only in a ground-level camera. An isolated temporary capture test also passed 1/1 (`/tmp/one-roof-daynight-capture.xml`) and recorded 06:00, 12:00, and 22:00 states; these reveal the cropped view and lobby transparency artifacts. This checked the manual state cycle, not W-key input. Broader exterior weather readability and the W-key cycle remain open.
- **Still open:** Identify and fix the lobby rendering artifact; complete pointer-driven placement hover/readability checks; improve undercity entrance-crossing and route-stop framing; broaden exterior and utility-overlay framing; verify weather key input, reference-hardware timing, 60 FPS presentation, social calibration, and remaining City Status gates. Do not infer these checks from ground-level captures.
- **Undercity capture update:** an isolated r4 fixture passed 1/1 while asserting two rooms, 1/1 AccessHub staffing, and the investigator's entrance/route phases. The staffed AccessHub view is readable. The entrance-crossing frame is obscured by a broad light cone and overlapping resident labels; the route-stop frame clips/overlaps the room caption. A wider r5 camera passed 1/1 but made those views less useful, so r4 remains the evidence set. Improve entrance/route framing.
- **Placement/expansion and rendering diagnostics:** focused graphics PlayMode passed 1/1 (`/tmp/one-roof-build-placement-capture-final.xml`). Assertions verified green valid/red invalid presenter states, ground expansion bounds/deck/facade alignment, and new-floor slab/deck/facade alignment; the serialized scene SHA-256 was unchanged. The screenshots show the preview spans most of the floor slice, so pointer-driven hover and preview readability remain open. The added-floor screenshot was taken immediately after placement and shows a wavy pattern; a separate runtime-room A/B suggests a similar effect is tied to the 0.55-second construction transition, but did not directly retest that placement capture after transition. A targeted shared-MPB texture A/B passed 1/1 (`/tmp/one-roof-mpb-texture-ab-final2.xml`): clearing runtime floor-line/room-quad blocks changed 0 pixels in the artifact crop, weakening that mechanism. The first runtime apartment backdrop A/B (`/tmp/one-roof-runtime-room-backdrop-ab.xml`) exposed a strong pattern during the construction transition. A steady-state repeat (`/tmp/one-roof-runtime-room-backdrop-steady-ab.xml`) waited for `IsTransitioning == false`; that pattern was absent and the two states changed 0 pixels in the crop. This supports a transient construction-effect explanation for that runtime-room pattern, but does not identify the separate authored-ground day/night artifact. Keep the cases separate and compare them before production changes.
- **Utilities capture:** focused graphics PlayMode passed 1/1 (`/tmp/one-roof-m10-utilities-visual-daytime.xml`). A Day 1 noon Water overlay view shows the cyan path without broad opaque panels; faded exterior cones remain at the top edge but do not obscure the route. This closes route readability in the isolated fixture; full-tower utility-overlay framing remains open. Evidence: `utilities-water-flow-daytime.png`.
- Record screenshots and exact scene/state in the relevant handoff. Do not save scenes unless a scene change is explicitly required.

### 3. Close or carry forward performance gates with evidence

**Handoffs:** `HANDOFF_2026-09-27_secret-undercity-expansion.md`, `HANDOFF_2026-09-29_OR-1001-connected-building.md`, `HANDOFF_2026-09-28_OR-906A-social-ties.md`.

- The underground 300-resident p95 benchmark was skipped in routine suites; standalone 14-room p95 varied from **0.729 to 5.219 ms** with host load. Run repeatable measurements on reference hardware and compare paired no-room / fourteen-room cases.
- The OR-1001 30-floor / 300-resident tick gate produced p95 **6.884 ms** and **6.858 ms** failures, followed by a **1.212 ms** diagnostic pass. Test-only per-tick duration/allocation and GC collection attribution is implemented and exercised. Three additional same-host runs measured p95 **0.924**, **1.150**, and **0.921 ms**, worst **35.372**, **35.955**, and **36.343 ms**, and per-thread allocation percentiles of zero; GC collection counts were respectively **0/0/0**, **1/1/1**, and **0/0/0**. Timing still varies substantially across previous runs; this is not a reference-hardware profile.
- The 50-resident social formation/recovery calibration and 300-resident reference-hardware performance check remain open. Keep these separate from ordinary EditMode completion.
- Acceptance: measured scenario, hardware, warmup, sample count, p50/p95/worst, allocations, and test result are recorded. Do not claim the <4 ms / 60 FPS gate from a single passing run.

### 4. Track adjacent milestone blockers without folding feature work into cleanup

**Handoffs:** `HANDOFF_2026-09-28_economy-livelihoods.md`, `HANDOFF_2026-09-28_OR-906A-social-ties.md`, `HANDOFF_2026-09-29_OR-1001-connected-building.md`, `Handoffs/Archive/HANDOFF_2026-09-29_demolition-orphaning-fix.md`.

- ECON-006, ECON-007, and ECON-008 are **DONE** in `Planning/BACKLOG.md`. OR-1003 is the remaining City Status gate: 30 floors, 300 residents, 30 sustained days, service coverage, cash/recovery, save/load, and reference-hardware performance.
- OR-906A still needs a concrete saved resident-to-resident conflict event tied to an attributable grievance, plus repeated-conflict, recovery, and migration tests before its phase gate.
- OR-1001 still needs a defined generator reverse-feed and utility-dependent underground production contract before derived routes can govern those operations.
- The demolition re-review recorded an unreachable-apartment move-in risk: leasing may commit before checking an Outside-to-home route. That risk is now part of OR-1003 acceptance; add regression coverage and deferred/retry behavior there.

These items are product/architecture work, not validation chores. Keep them visible in their owning backlog items and handoffs; do not archive those handoffs solely because the current test suites pass.

## Handoff Disposition

| Handoff | Current disposition |
|---|---|
| Exterior windows, lighting, facade | Keep active: interactive day/night and build/expansion checks remain. |
| Secret Undercity expansion | Keep active: reference-hardware timing and visual review remain. |
| OR-906A social ties | Keep active: attributable conflict phase gate and calibration remain. |
| Economy livelihoods/recovery | Keep active for OR-1003: ECON-006–008 are done; the City Status acceptance fixture remains. |
| OR-1001 connected building | Keep active: timing gate and generator/underground utility contract remain. |
| Demolition/orphaning and utility failures | Archived: prior test failures are fixed; the unreachable move-in route case is transferred to OR-1003 acceptance in `Planning/BACKLOG.md`. |
| Weather redesign | Keep active: automated graphics PlayMode suite passes and weather cycle was captured; the new fog mesh needs graphics review, and exterior day/night remains open. |

Archive each handoff only after its own remaining acceptance criteria are met or explicitly transferred to a tracked backlog item with a clear acceptance criterion and next action.
