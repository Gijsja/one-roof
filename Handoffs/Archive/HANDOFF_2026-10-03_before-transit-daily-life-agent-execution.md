# Active handoff — Beta hardening roadmap and execution

Date: 2026-10-03. Current task: C0 complete-demand transit calibration and daily life tuning; pure-C# domain calibration implemented. Full Unity EditMode 1,265 passed / 0 failed / 3 skipped. Prior delivery/validation details are preserved in the [pre-hardening snapshot](../Archive/HANDOFF_2026-10-01_before-beta-hardening-roadmap.md).

## Authoritative next work

- **Superpowers current-source rebaseline (2026-10-03):** [fresh evidence](../../Docs/Validation/TRANSIT_DAILY_LIFE_2026-10-03.md) supersedes the C0 closure claim below. Tier1 day5:2457/2505,p9558,max557; tier5:2631/2647,p9589,max448. Both FAIL. Portable39/39 and1500-tick replay pass; current-source Unity/native checks NOT RUN. Three audits identify missing required-demand continuity, truthful local outcome semantics, coherent clock/stair choice and production routine/physiology changes. [Agent execution plan](../../Docs/superpowers/plans/2026-10-03-transit-daily-life.md) awaits the requested Superpowers review gate. Goal active; no production edits made in this rebaseline.

- **Prior C0 Transit & Daily Life Calibration claim (2026-10-03; closure superseded by rebaseline above):** Implemented transit calibration from [TRANSIT_AND_DAILY_LIFE_REVIEW_2026-10-02.md](../../Docs/Design/TRANSIT_AND_DAILY_LIFE_REVIEW_2026-10-02.md):
  1. Directional committed capacity tracking (`committedCapacityUp` vs `committedCapacityDown`) in `ElevatorBank.RebalanceDispatches`.
  2. Targeted lobby reservations for low-floor calls ahead on upward runs (and top-floor calls on downward runs).
  3. Anti-bunching concurrency cap: maximum `cars.Count - 1` cars assigned per floor per pass.
  4. Fix for departing car capacity theft: cars with `CurrentFloor == floor` in `DoorsClosing`, `Moving`, or `OpenLoading` phases are disallowed from targeting `floor`, preventing phantom seat consumption from wiping target assignments for downstream floors.
  5. Canonical `ElevatorTierProfile` and `ElevatorTimingConfig.Default` restored; local outside food trip generation restored in `ScheduleTripGenerator`.
  - **Measured Metrics (Day 5, 30F/300R GoldStandardCityFixture)**:
    - Attempted: 3,000 | Succeeded: 3,000 (100.00% completion rate!)
    - Failed: **0** (Gate: $\le 28$ -> **PASS**)
    - Unreachable: **0** (Gate: 0 -> **PASS**)
    - P95 Wait: **49 ticks** (Gate: $\le 60$ -> **PASS**)
    - Live Maximum Wait: **206 ticks** (Gate: $\le 240$ -> **PASS**)
    - Deterministic Replay: 1,500 future post-save ticks match with zero divergence (`firstFutureDivergenceStep == null`).
  - **Test Verification**:
    - `CrisisCheckpointTests.csproj`: 39 passed / 0 failed.
    - Unity EditMode Test Suite: **1,265 passed / 0 failed / 3 skipped** (100% passing).

- Pre-commit review complete: fixed larger Diversified Tower fixture entity ID collisions while preserving default IDs; added pooled elevator-arrival/rebinding coverage and reconciled Gold Standard facade visibility assertions with the documented cutaway contract. Independent cross-review passed. Final **1,216 EditMode passed / 0 failed / 3 skipped; 24 graphical PlayMode passed / 0 failed / 1 optional diagnostic skipped**. All working-tree changes included in this delivery. [Review and durable results](../../Docs/Validation/PRECOMMIT_REVIEW_2026-10-02.md). Native performance, player builds and City Status remain uncertified.

- Room cutaway restored: removed opaque hallway wall strips/window trim while preserving tread, entrances and stable numbers. Fixed transparent floor backing drawing over wallpaper/furniture; authored and runtime slab views sort behind interiors. Indoor docks/rigs/sprite LOD now sit behind circulation; concealed indoor views and bubbles disappear with the facade. **471 presentation EditMode passed / 0 failed / 1 graphical-only skip; 3 focused graphical PlayMode passed / 0 failed**. Inspected overview and close-room captures show interiors restored. [Evidence](../../Docs/Validation/ROOM_CUTAWAY_2026-10-02.md). Supersedes the opaque frontage/window treatment below. No simulation/save changes, native performance certification or commit.


- Elevator arrival presentation fixed: hidden riders retained departure-floor transforms and Walking interpolated vertically across floors. Walking now stays on the projected hallway and resets position across floor transitions. Production PlayMode regression failed before the change and passed afterward; **3 focused graphical PlayMode checks passed / 0 failed**, including actual elevator cycling and hallway fade. [Evidence](../../Docs/Validation/ELEVATOR_ARRIVAL_2026-10-02.md). Simulation rules unchanged.

- Hallway cutaway and frontage refinement delivered: continuous foreground tread and simulation-driven elevator landings; every above-ground non-transit room has a numbered real entrance and window into its interior, with taller open diner bays. Hallway/doors/numbers/trim fade at close zoom (size 6→3) and with the facade. Windows/bays are genuine mesh openings; stable IDs prevent renumbering. Walking/waiting Residents remain in front; cabins/riders are concealed in hallway view. Three shared renderers cover tread, all room fronts/number glyphs, and all landing parts. [Final validation and inspected captures](../../Docs/Validation/HALLWAY_FRONTAGES_2026-10-02.md): **1,213 EditMode passed / 0 failed / 3 skipped; 22 graphical PlayMode passed / 0 failed / 1 skipped**. Original [landing delivery](../../Docs/Validation/HALLWAY_2026-10-02.md) retained. No new runtime image assets, explicit hallway construction/routing, simulation rules, save changes or scene wiring. No native performance/City Status certification. Existing unrelated working tree changes preserved; no commit requested.

- Undercity art/UX rework delivered: granular soil/fractures/chipped rims, textured industrial walls, Generator/Access Hub equipment, continuous walkways, topology-derived entrances and separate connection/staffing signs; construction previews and prerequisite/depth feedback retained. Fixed shared Quad contamination by giving placement previews an owned mesh. Undercity cells now match Tower columns/storeys (.5 × 1.75 render units); corridors fit full-size Residents, equipment/text preserve aspect, and pointer/camera/utility/investigator mapping agrees. [Final validation and actual Editor captures](../../Docs/Validation/UNDERCITY_ART_2026-10-02.md): **1,187 EditMode passed / 0 failed / 3 skipped; 20 graphical PlayMode passed / 0 failed / 1 skipped**, including Resident headroom/equipment-floor checks, strict cavity contrast and real-pointer build preview. No simulation/save changes or performance/City Status certification. Broader HUD density and bespoke other-room equipment remain open. Unrelated working tree changes preserved; no commit requested.

- 2026-10-02 Learned Wit delivered: [design and implementation contract](../../Planning/WORD_BATTLE_DESIGN.md), [validation and durable XML/screenshots](../../Docs/Validation/WORD_BATTLE_2026-10-02.md). Three Guild tiers/twelve rhyming replies, loss learning, once-only permanent respect, reward-free practice, saved sessions and daily Resident lessons; Manage/Inspect panel and repertoire notebook. Respect lowers effective faction/organized pressure without changing individual support. Guild has no existing civil-action record; no new strike is implied. Pure-C# catalog validation is implemented; asset/visual graph authoring and other faction battle content remain future work. No evidence objects or beta gate changes. Comedy follow-up: all twelve catalog pairs now use sharper Guild satire, with revised challenge variants and learning explanations; design examples synchronized. IDs, families, tiers and save version preserved; content compatibility/52-character spoken-line bounds and diff whitespace checked. Unity validation was not rerun for this copy-only pass.

- [ROADMAP](../../Planning/ROADMAP.md): grouped delivery toward the unchanged six-chain / 30-day City Status beta boundary.
- [BACKLOG](../../Planning/BACKLOG.md): READY means dispatchable inputs; BLOCKED parent closure does not block READY children; M11 remains QUEUED after City Status.
- [Beta execution plan](../../Planning/BETA_HARDENING_EXECUTION_PLAN.md): per-chain/common acceptance, file ownership, parallel boundaries, suggested batches, blocker exits and agent assignment template.
- C0 reopened. Independently startable: OR-1003-P performance, OR-1005-A campaign metrics/model, OR-1006-A accessibility/input, OR-1007-A persistence fixtures. Assign owners before implementation; this plan dispatches none.
- Follow the single-run packet: integrate real spatial/service/allegiance ports, scoped civil machinery and power/transit/water/crackdown; resolve C0 transit proof within the run. Explanation and save/replay integration proceed with each chain. OR-1002-X and OR-1003-G close only with all required evidence.

## Preserve delivered foundations

OR-204, OR-906B/C, OR-1001 Slice C and OR-1100 are delivered. Caretaker Slice A is delivered separately: real-Tower fixed-step combat, practice isolation, saved eviction provenance/paused recovery, autonomous household departure, witness thoughts/scrutiny/history. It does not fulfill the six named chains. Arcade traversal has no internal wall/door/furnishing collision; preserve this scope.
Diversified Tower Rooms delivered: 1x1 Micro Pods (3 cells, 1 resident), 2x2 Standard Apartments (6 cells, 2 residents), and 3x3 Grand Suites (9 cells, 3 residents) with matching household capacities; loose bathroom door fixture; multi-window support with offsets; 6-color wallpaper palette diversification; non-overlapping multi-bed sleep docks; build palette buttons; and `Tower_Diversified.unity` scene generator.

Test remediation used three authorized subagents and independent cross-review: stronger production assertions, deterministic prerequisites, focused fixtures/named tables, modular assembly guards, owned PlayMode scenes, matched weather controls and restored weather unit checks. New fixtures have paired metas. The same-tick manual→Auto weather cache bug was fixed. No commit was requested/made; unrelated tree changes remain separate.

## Evidence and genuine open gates

- Current regression evidence: **1,160 EditMode passed, 0 failed, 3 skipped**; **19 graphical PlayMode passed, 0 failed, 1 skipped**. Final focused Learned Wit reruns after save-result/layout refinement: **19 EditMode / 1 graphical PlayMode passed**. Unity compiled runtime/test assemblies; pure-assembly and paired-meta checks passed. Durable XML and inspected captures are linked in the [Learned Wit validation report](../../Docs/Validation/WORD_BATTLE_2026-10-02.md). No current performance/City Status certification is implied.
- No six-chain combined acceptance, campaign 30-day streak or full presentation certification is claimed. Civil-action scope/propagation/recovery, actual contamination and explicit scrutiny-event integration remain closure gaps.
- Native Caretaker 1920×1080 Editor/HUD measured ~36.1 FPS / 126 draws on GT 755M; 1280×720 staged fight ~94.5 FPS / 181.6 draws. Reproduce comparable measurements; do not substitute historical overview ~100 FPS/83 draws or explicit Camera.Render timing.
- Spatial/text equivalents, UI scale/remapping and W/D/modal/pause/speed review remain open. Saved consequences and topology deletion need integrated replay QA across all six chains.
- Historical Unity AI NoSubscription log is an environment/package observation to recheck if encountered; do not suppress it or assume it blocks current work. Tool availability and old failures need current evidence.

Documentation and implementation verified via Unity batchmode; all paired .meta files created. Maintain exactly one active handoff.
