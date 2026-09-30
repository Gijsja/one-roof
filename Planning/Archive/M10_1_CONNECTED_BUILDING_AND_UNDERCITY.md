# M10.1 + Secret Undercity — Connected building and utilities

## Decision and boundary

M10.1 now deepens the interactive build system and connects the Secret Undercity to physical power and water. Floor blueprint copy/paste is deferred. The 30-floor, 300-resident beta scale and the <4 ms tick / 60 FPS presentation budgets remain acceptance gates, not assumed outcomes. The undercity remains an independent 32×12 board; it never becomes a negative tower floor.

Current topology derives electrical and plumbing service from ground sources and continuous same-column risers. The utility view draws inferred raceways and drops, and the underground generator adds scalar backup capacity without a visible physical route. The first implementation step corrects misleading lines and improves build hints; the complete integration requires explicit utility edges and service status across the lobby boundary.

## Player contract

1. **Choose a build action.** Keep the chosen tool visible, show its footprint and cost, and explain the next gesture. Tower rooms place with a click; underground rooms zone with a drag; excavation, floor, and corridor brushes paint while held. Right-click or Escape cancels. The palette remains reachable without losing the camera position.
2. **Preview the consequence.** A ghost and a text card identify the target, price, and reason a placement is blocked. A utility-dependent room reports whether power and water will reach it and which built segment or source is missing. Costs and connection status come from the same command/topology evaluation that commits the build. Predictions that depend on future load are labeled estimates.
3. **Read the installed network.** Power and water appear as continuous segments backed by built topology, including the lobby/core bridge and underground shaft/corridor branches. Unbuilt gaps are gaps on screen. Built but unserved segments remain visible as quiet stubs. Energized/pressurized segments carry directional motion only while service actually runs; the flow stops at the exact break. Text, shape, and color all convey state.
4. **Diagnose and respond.** Selecting a failing floor, segment, or underground room names the source, missing connection, capacity/pressure, equipment, or staffing/supply cause. The inspector offers the relevant build or management action, then shows the measured result after it changes.

The network borrows the useful clarity of connected coverage and network-specific status from [Factorio's electrical network](https://wiki.factorio.com/Electricity). Geometry and flow are derived from one authoritative graph.

## Implementation slices and exit tests

Slices A and B are implemented and pass the isolated Unity compile, EditMode, and PlayMode suites recorded in `Handoffs/Active/HANDOFF_2026-09-29_OR-1001-connected-building.md`. Slice B's path is a derived conduit through built core/corridor cells; no separate player-drawn underground cable or pipe command exists. Graphics PlayMode captures now show running power and water and a stopped power route after source removal. Slice C and the reference-hardware gate in D remain open.

| Slice | Deliverable | Required evidence |
| --- | --- | --- |
| A — truthful surface view and build feedback | Source/risers/drops only where built; running lines animate only with service; failed and disconnected pieces are distinguishable; accurate gestures, cost, and rejection hints | Focused EditMode projection/presenter tests, Unity compile, PlayMode capture of working and broken networks |
| B — explicit physical graph | Stable IDs for source, riser, floor branch, room port, lobby bridge, underground shaft/corridor, and underground port; edges built from domain topology; connected components and per-edge service state | Deterministic connect/disconnect and source-loss tests; no Unity references in Domain; save/load or derived-graph reconstruction test |
| C — undercity operation integration | Underground rooms have power/water requirements and named service causes; staffed/supplied generator feeds only a physically connected component; surface and undercity use the same projected path and overlay | Generator and water break/recovery tests, inspector cause-chain test, visual capture showing continuous and interrupted flow |
| D — scale and usability | Network geometry updates on topology/service revision, not every frame; palette and preview remain usable at 30 floors and across the undercity | 30 floors, 300 residents, fourteen underground rooms; repeatable reference-hardware p95 tick <4 ms, 60 FPS presentation, draw-call and view-cap checks |

## Domain and presentation boundary

- Domain owns build commands, topology, utility edges and connectivity, capacities, operational status, and stable failure causes. Utility geometry must be derivable after load from saved building and underground aggregates; any new persistent state needs a migration fixture.
- Application exposes immutable build-preview and network projections with stable IDs. Preview and commit share `CanExecute` rules. The selected network is presentation state.
- Presentation converts surface and underground grid coordinates to world positions, pools line/node views by edge ID, and animates flow without rebuilding geometry each frame. It never infers a live connection just because two endpoints are near each other.
- The underground lobby bridge is an explicit connection. Slice C must make a generator's capacity enter the surface electrical calculation only when its reverse-feed path is present and operating; current scalar backup behavior is retained until that migration is validated. Water crosses the same boundary through its own projected service route.

## Validation baseline and open gate

The Secret Undercity handoff reports Unity 6000.3.24f1 compile exit 0, 688 passing EditMode tests with one opt-in benchmark skipped, 9 passing PlayMode tests, and a 300-resident view-cap pass. Standalone tick p95 varied from 0.729 to 5.219 ms with host load. These numbers do not establish the M10.1 performance gate. Run the completed slice in an isolated worktree under `Docs/10_DEVELOPMENT_WORKFLOW.md` and record command, exit code, XML totals, captures, and reference hardware before marking OR-1001 done.

The combined 30-floor/300-resident/14-room opt-in fixture has now been run for 1,500 ticks. Two runs failed the 4 ms p95 target at 6.884 and 6.858 ms; a diagnostic rerun passed at 1.212 ms. Slow samples clustered on every tenth tick, but a bounded pure Domain profile found no floor-scaled hotspot in the periodic leasing subcalls. Unity-side phase and GC timing, repeated reference-hardware runs, and a 60 FPS presentation capture are needed before treating this gate as met or changing simulation code for a suspected bottleneck.
