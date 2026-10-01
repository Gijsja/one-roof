# One Roof — Active Backlog

Updated 2026-10-01. [ROADMAP](./ROADMAP.md) defines direction; [Beta hardening execution plan](./BETA_HARDENING_EXECUTION_PLAN.md) defines package contracts, ownership and acceptance. One owner per package; only the coordinator updates status during parallel work.

**Statuses:** READY = inputs exist and work can start; IN PROGRESS = active work under an assigned owner (parent rollups may contain delivered slices); BLOCKED = a named missing dependency prevents completion; QUEUED = intentionally deferred scope; DONE = stated acceptance has evidence. Delivered Slice A remains a distinct historical delivery. Planning does not dispatch agents.

Latest evidence imported from the 2026-10-01 test-remediation snapshot: **1,131 EditMode passed / 0 failed / 3 skipped; 17 graphical PlayMode passed / 0 failed / 1 optional diagnostic skipped**. Current performance and six-chain acceptance remain open. Rebaseline before code changes. Older counts and performance are historical, not current certification.

## 1. Parent outcomes and delivered foundations

| ID | Status | Outcome / remaining gate |
|---|---|---|
| OR-204 | DONE | Elevator widening/tier upgrades; retain regression coverage. |
| OR-906B / OR-906C | DONE | Issue influence and connected local organizing; reuse for civil chains. |
| OR-1001 Slice C | DONE | Connected undercity utility reverse-feed and production; reuse for infrastructure chains. |
| OR-1002 | IN PROGRESS | Six complete adaptive chains; child packages below define remaining work. Slice A alone cannot close parent. |
| OR-1002 Slice A | DELIVERED | Lobby Caretaker and faction-driven arcade eviction; preserve saved consequences. Shared input/performance hardening remains open. |
| OR-1005 | BLOCKED | Final campaign acceptance needs real chain inputs; A preparation is READY independently. |
| OR-1006 | IN PROGRESS | Spatial overlays/accessibility; finish non-colour channels, scale/remapping, input conflicts and modal behavior. |
| OR-1007 | BLOCKED | Final recovery acceptance needs all saved chain/campaign owners; A fixtures/contracts are READY now. |
| OR-1003 | BLOCKED | Final City Status acceptance needs six chains, campaign, accessibility, recovery and performance. Harness/profiling work is READY separately. |

Parent BLOCKED statuses describe final closure, not a prohibition on READY child work. Child rows are the dispatch source of truth; no implementation owner is assigned by this update.

### Dispatchable packages and concrete blockers

| Package | Status | Requires | Acceptance / blocker exit |
|---|---|---|---|
| OR-1002-C0 | READY | Existing system evidence | Specify six chains and exact City Status metrics; shared IDs/timing/scope/effect/save/explanation contracts and fixture matrix. |
| OR-1003-P | READY | Existing reference scene | Reproduce native overview/close/Caretaker views with HUD, resolution/hardware and tick/allocation/draw data; close measured budget failures. |
| OR-1005-A | READY | Existing metrics; reconcile with C0 | Define and implement eligibility, finalized-day trends, streak/failure reasons; use explicit adapters for unavailable chain inputs, never placeholder-green gates. |
| OR-1006-A | READY | Existing UI/input/overlays | Inventory and repair text/glyph/spatial equivalence, UI scale, remapping, W/D conflicts and exclusive modal/pause/speed behavior. |
| OR-1007-A | READY | Existing save owners; reconcile with C0 | Baseline old/corrupt saves and deterministic replay fixtures; define bounded migrations and deletion recovery for new state. |
| OR-1002-C1 | BLOCKED | C0 | Complete Rent Boycott lifecycle, scope/propagation, response, saved aftermath and integrated cause-chain evidence. |
| OR-1002-C2 | BLOCKED | C0; agreed civil APIs | Complete Labor Strike; explicit relationship to WorkSlowdown; distinct scoped effects and recovery evidence. |
| OR-1002-I1 | BLOCKED | C0 | Complete Power Cascade over existing physical grid/wear/backup systems. |
| OR-1002-I2 | BLOCKED | C0 | Complete Transit Collapse over existing routing/congestion/capacity systems. |
| OR-1002-I3 | BLOCKED | C0 | Complete Water Contamination, including a real source/exposure/containment contract distinct from low pressure. |
| OR-1002-E1 | BLOCKED | C0 | Complete Building Crackdown with explicit scrutiny/exposure/event relationship, scope, expiry and saved aftermath. |
| OR-1002-U | BLOCKED | First C0 chain projection | Integrate explanation per delivered chain; final closure requires all six. No extra overlay implicit. |
| OR-1005-B | BLOCKED | A; six accepted chain projections | Complete campaign pacing/end states/board and inspectable streak; sandbox continuation. |
| OR-1007-R | BLOCKED | A; six saved chain owners; campaign save contract | Old/new/corrupt saves, every phase, replay, overlapping crises and partial demolition pass integrated recovery QA. |
| OR-1002-X | BLOCKED | C1/C2/I1/I2/I3/E1/U | All six close individually and together; overlap/order/non-stacking/propagation/replay/response tests pass. |
| OR-1003-G | BLOCKED | X; 1005-B; 1006-A; 1007-R; 1003-P | Actual deterministic 30-day streak at 30 floors/300 Residents with published gates and all performance budgets. |

A BLOCKED child becomes READY when its concrete input is delivered; parent closure is not an extra blanket dependency. Within U/R, each available chain can be integrated/tested before the final all-six gate. Use the execution plan for safe parallel boundaries.

---

## 2. Planned Resident Expansion (M11, After City Status)

Requested 2026-09-30. [Execution plan](./RESIDENT_SYSTEM_EXECUTION_PLAN.md) and [copyable agent prompts](./Prompts/RESIDENT_SYSTEM_AGENT_PROMPTS.md) define package ownership, validation and dispatch gates. OR-1100 is immediate validation of existing resident work; new M11 implementation stays after City Status. These tasks are QUEUED after City Status; none is dispatchable until its declared gate passes. Scope and City Status gates remain in [Roadmap §4](./ROADMAP.md#4-planned-m11--modular-residents-faction-bodywear--limb-health); detailed contracts and acceptance scenarios are authoritative in the [Resident execution plan §§8–9](./RESIDENT_SYSTEM_EXECUTION_PLAN.md#8-system-and-art-contract). Existing beta gates remain unchanged.

| ID | Status | Milestone | Task | Depends on | Acceptance |
|---|---|---|---|---|---|
| **OR-1100** | **DONE** | Resident follow-up | **Verify current resident presentation and enforce artwork archive** | Existing resident working tree | Current Unity tests/render/preview/pool/LOD evidence; executable Resources/Addressables/serialized-reference archive guard; preserve unrelated changes and attribute baseline failures. |
| **OR-1101** | **QUEUED** | M11.1 | **Resident body, appearance and health contracts** | OR-1003, OR-1100 | Stable body-part/content IDs; separate identity, worn items and health aggregates; immutable projections; old-save healthy/default-outfit migration; no engine references in Domain/Application. |
| **OR-1102** | **QUEUED** | M11.2 | **Modular resident art, rig and bodywear** | OR-1101 | Shared pose/anchor contract, explicit animation selection/time and scrub/replay preview, approved modular attachments only, body compatibility, tint/coverage/occlusion, pooled views and LOD. |
| **OR-1103** | **QUEUED** | M11.3 | **Faction and occupation wardrobe preferences** | OR-1102, OR-906C | Actual faction/occupation/personality state deterministically influences palettes, badges and outfits; individual variation; appearance never changes membership or social outcomes. |
| **OR-1104** | **QUEUED** | M11.4 | **Limb injury simulation and equipment effects** | OR-1101, OR-1002 | Attributable incident injuries, bounded pain/healing/function rules, relevant mobility/work effects, explicit protective-item mechanics, deterministic saves. |
| **OR-1105** | **QUEUED** | M11.5 | **Autonomous care, accessibility and limb replacement** | OR-1104 | Clinic capacity and accessible transit influence autonomous care/recovery; scars, missing limbs and prosthetics persist; Steward acts through systems. |
| **OR-1106** | **QUEUED** | M11.6 | **Health-aware wardrobe, animation and cause-chain UI** | OR-1102, OR-1103, OR-1105 | Covered injury visuals, replacement attachments, meaningful gait/posture and emotion bubbles; inspector exposes incident, impairment, systems levers and measured recovery. |
| **OR-1107** | **QUEUED** | M11.7 | **Modular resident content and scale acceptance** | OR-1106, OR-1007 | Pose/coverage/content validation, save migrations, pool/LOD reuse, deterministic 30-floor/300-resident run and measured <4 ms p95 tick / 60 FPS / <180 draw calls. |

---

## 3. Preserved foundations and evidence

Closed-loop integer economy, physical utility networks, autonomous social/faction systems, eight diagnostic overlays, pooled three-tier Resident LOD, semantic zoom and weather remain delivered foundations. Historical 832/15 and overview 83 draws/~100 FPS describe earlier snapshots; archive references are in [historical backlog](./Archive/BACKLOG_HISTORICAL_ARCHIVE.md) and [2026-09-30 validation](../Docs/Validation/BACKLOG_REVIEW_2026-09-30.md).

Native 1080p Caretaker performance (~36 FPS on GT 755M) remains an unresolved measurement, not a stale blocker to silently delete. Reproduction and comparable before/after evidence belong to OR-1003-P. Passing functional tests does not certify the frame budget.

Strictly one [active handoff](../Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md). Previous detail was preserved in [the pre-hardening snapshot](../Handoffs/Archive/HANDOFF_2026-10-01_before-beta-hardening-roadmap.md). This planning update changes documentation only; Unity validation was not run.
