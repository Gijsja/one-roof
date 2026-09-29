# OR-1001 — Connected building and utilities, M10.1 + Secret Undercity

## Scope and current slice

- Floor blueprint copy/paste is deferred. The combined target is build UX, authoritative physical power/water paths across the tower/undercity boundary, visible operating flow, and 30-floor/300-resident performance. See `Planning/M10_1_CONNECTED_BUILDING_AND_UNDERCITY.md`.
- Slice A improves build hover hints with construction cost, current floor connection status, accurate underground gesture guidance, and a visible way back to the tool palette.
- The surface utilities layer now requires built source/riser/transformer infrastructure for its displayed feeds and distribution, and shows disconnected built sections with a disrupted material. Geometry refresh is gated by projection changes instead of every frame.
- A pure-C# path projection derives stable power/water segments across a built lobby core, shaft, corridors, and underground room ports. The overlay renders built quiet paths and animated flowing paths; its panel summarizes connected/flowing rooms. The underground room inspector reports route causes separately from staffing and supply causes. The path snapshot is derived after load and cached by topology, excavation, and backup-capacity changes.
- Generator reverse-feed rules and utility-dependent underground production remain in slice C. The existing undercity handoff stays active for UNDERCITY-001.

## Validation

- Isolated worktree: `/home/geisha/.codex/worktrees/m10-connected-validation/one-roof`, Unity `6000.3.24f1`.
- Slice A headless compile: `/tmp/m10-connected-compile-2.log`, exit 0; focused utility 10/10 (`/tmp/m10-utilities-editmode-2.xml`), build shell 13/13 (`/tmp/m10-build-ui-editmode.xml`), grid placement 42/42 (`/tmp/m10-build-grid-editmode.xml`), graphics-enabled PlayMode 9/9 (`/tmp/m10-connected-playmode.xml`).
- Integrated headless compile: `/tmp/m10-integrated-compile-2.log`, exit 0. First compile exposed an ambiguous `Object` reference in a new test, then corrected.
- Integrated full EditMode: `/tmp/m10-integrated-editmode-final.xml`, exit 0, 738 passed, 0 failed, 1 skipped explicit 300-resident benchmark. An initial run had one new fixture failure because a saved room width was below the loader's minimum; the corrected focused test passed 1/1 (`/tmp/m10-undercity-inspector-editmode.xml`) before the green full rerun.
- Integrated graphics-enabled PlayMode: `/tmp/m10-integrated-playmode.xml`, exit 0, 9 passed, 0 failed. The existing staffed-undercity image was reviewed at `/tmp/one-roof-undercity-visual/undercity-room-01.png`; it does not show the utilities overlay.
- Focused graphics PlayMode utility capture: `/tmp/m10-utilities-visual-2.xml`, exit 0, 1 passed. The screenshots `/tmp/one-roof-m10-utilities-visual/power-running.png`, `water-running.png`, and `power-stopped.png` were visually reviewed. Gold and cyan flow follows the built lobby/core/corridor route to the room; after source demolition the same power route is red and still visible as a stopped path. The first capture run exposed a `RoomPresenter` null reference on demolition: a second `VisualEffectsPresenter` was added to a room that already had one. The room view now reuses the existing effects component; the focused rerun passed.
- Final full graphics PlayMode after the capture test and demolition fix: `/tmp/m10-integrated-playmode-final.xml`, exit 0, 10 passed, 0 failed.
- Opt-in five-floor 300-resident, fourteen-room tick baseline: `/tmp/m10-tick-budget.xml`, exit 0, 1 passed; 1,500 measured ticks after warmup gave p50 0.746 ms, p95 2.805 ms, worst 58.669 ms on this host. This fixture is five floors and is not the 30-floor acceptance profile.
- Expanded opt-in 30-floor, 300-resident, fourteen-room benchmark: `/tmp/m10-30floor-tick-budget.xml` and `/tmp/m10-30floor-tick-budget-repeat.xml` each failed the 4 ms p95 assertion (6.884 and 6.858 ms); `/tmp/m10-30floor-tick-diagnostic.xml` passed on a third run (1.212 ms p95). The diagnostic run measured every-tenth leasing ticks at 6.839 ms p95, other ticks at 1.196 ms p95. A temporary pure Domain phase profile found no floor-scaled hotspot in those subcalls (periodic leasing block 0.069 ms p95, about 1 KB allocated); no speculative tick optimization was made. This gate is **open** until Unity-side phase/GC attribution and repeatable reference-hardware results.
- Pure Domain path harness: five deterministic checks passed before Unity validation; the same path cases are in the green EditMode suite.
- Scoped `git diff --check` passed for the M10 files. A whole-checkout check currently reports a blank line in the concurrent weather edit `Assets/OneRoof/Runtime/Domain/Weather/MonthlyWeatherCycle.cs`; that file is outside this handoff.

## Risks and next safe action

- Surface horizontal raceways remain inferred from transformer/riser and consumer extents rather than player-built edges. Underground service lines are derived from built core/corridor topology. The line renderer uses shared materials rather than per-edge material instances. Flow captures are reviewed, but no reference-hardware frame profile was taken.
- The generator still adds scalar backup capacity to the surface electrical evaluator. Its reverse-feed path and utility-dependent underground production need a defined contract before the route display can govern those operations.
- Use Unity-side phase timestamps and GC counts around the ten-tick update to explain the variable benchmark, then profile the 30-floor/300-resident/14-room scenario on reference hardware. The 30-floor tick and 60 FPS presentation gates have not been established.
- Keep the unrelated Noir/FrankMiller and concurrently edited weather files in the main checkout untouched. The isolated validation patch excluded them.
