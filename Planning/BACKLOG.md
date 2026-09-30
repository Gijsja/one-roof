# One Roof — Active Backlog

Status values: `READY`, `IN PROGRESS`, `BLOCKED`, `DONE`. One agent owns one task at a time. A `READY` task is eligible for execution only after its prerequisites are complete.
Current 2026-09-30 pass: **920 EditMode passed, zero failed, one explicit benchmark skipped**. See [validation report](../Docs/Validation/BACKLOG_REVIEW_2026-09-30.md) for current evidence and open gaps. **15/15 graphical PlayMode passed**; the explicit scale benchmark passed at **1.073 ms p95**, and overview measured **12.235 ms frame / 84 draw calls**. Historical totals below describe the delivered baseline.

For all completed foundation tasks and historical validation logs (Milestones M0 through M10 scale foundation, OR-001 through OR-906A, LOD, and Pooling), see [`Planning/Archive/BACKLOG_HISTORICAL_ARCHIVE.md`](./Archive/BACKLOG_HISTORICAL_ARCHIVE.md).

---

## 1. Active & Upcoming Tasks (Beta Boundary / City Status)

| ID | Status | Milestone | Task | Depends on | Acceptance |
|---|---|---|---|---|---|
| **OR-204** | **DONE** | Transit | **Elevator shaft widening & tier upgrades** | OR-705 | Dynamic 2-cell shaft widening, tier profiles (Tier 1–5), ETA-based car dispatch, commands (`UpgradeElevatorTierCommand`, `WidenElevatorShaftCommand`), save round-trip. |
| **OR-906B** | **DONE** | M9.5 | **Issue-specific social influence** | OR-906A, OR-602 | Strong recent ties share named grievances using prior-settlement state; capped influence inspectable separately from direct exposure; reverses with relief. |
| **OR-906C** | **DONE** | M9.5 | **Local faction organizing** | OR-906B, OR-903 | Require sustained grievance pressure and connected local membership for civil action readiness; recover on measured cause improvement. |
| **OR-1001 Slice C** | **DONE** | M10.1 | **Undercity utility integration & reverse feed** | OR-801, OR-802, UNDERCITY-001 | Generator reverse-feed to surface electrical grid and utility-dependent subterranean production. |
| **OR-1002** | **READY** | M10.2 | **Adaptive crisis pressure & personal consequences** | OR-903, OR-803, OR-604, ECON-004 | 6 condition-driven chains (Power Cascade, Water Contamination, Transit Collapse, Rent Boycott, Labor Strike, Building Crackdown) with warning phases, propagation, management levers, and saved resident consequences. |
| **OR-1003** | **READY** | M10.3 | **Beta boundary golden acceptance test (City Status)** | OR-1001, OR-1002, OR-1005, OR-1006, OR-1007 | 30 floors and 300 residents run deterministically, sustaining all published City Status gates for 30 consecutive days under <4.0 ms tick and 60 FPS presentation budgets. |
| **OR-1005** | **READY** | M10.3 | **Campaign board, pacing, and end states** | OR-1002, OR-904 | Eligibility, daily trends, threatened gates, and 30-day streak are inspectable; failed streak gives reasons; sandbox continuation. |
| **OR-1006** | **IN PROGRESS** | M10.3 | **Spatial overlay and accessibility beta pass** | OR-903, OR-905 | Text selection markers delivered. Remaining: spatial channels on all 8 overlays, UI scale, remappable controls, pause/speed and W/D shortcut conflicts; see validation report. |
| **OR-1007** | **READY** | M10.3 | **Save, recovery, and content QA** | OR-1002, OR-1005 | Crisis and campaign state survive save/load; migrations, corrupt saves, partial tower demolition, and content review pass. |

---

## 2. Planned Resident Expansion (M11, After City Status)

Requested 2026-09-30. [Execution plan](./RESIDENT_SYSTEM_EXECUTION_PLAN.md) and [copyable agent prompts](./Prompts/RESIDENT_SYSTEM_AGENT_PROMPTS.md) define package ownership, validation and dispatch gates. OR-1100 is immediate validation of existing resident work; new M11 implementation stays after City Status. These tasks are queued in dependency order; `READY` records planned work, not permission to bypass prerequisites. Scope and acceptance scenarios are authoritative in [Roadmap §4](./ROADMAP.md#4-planned-m11--modular-residents-faction-bodywear--limb-health). Existing beta gates remain unchanged.

| ID | Status | Milestone | Task | Depends on | Acceptance |
|---|---|---|---|---|---|
| **OR-1100** | **DONE** | Resident follow-up | **Verify current resident presentation and enforce artwork archive** | Existing resident working tree | Current Unity tests/render/preview/pool/LOD evidence; executable Resources/Addressables/serialized-reference archive guard; preserve unrelated changes and attribute baseline failures. |
| **OR-1101** | **READY** | M11.1 | **Resident body, appearance and health contracts** | OR-1003, OR-1100 | Stable body-part/content IDs; separate identity, worn items and health aggregates; immutable projections; old-save healthy/default-outfit migration; no engine references in Domain/Application. |
| **OR-1102** | **READY** | M11.2 | **Modular resident art, rig and bodywear** | OR-1101 | Shared pose/anchor contract, explicit animation selection/time and scrub/replay preview, approved modular attachments only, body compatibility, tint/coverage/occlusion, pooled views and LOD. |
| **OR-1103** | **READY** | M11.3 | **Faction and occupation wardrobe preferences** | OR-1102, OR-906C | Actual faction/occupation/personality state deterministically influences palettes, badges and outfits; individual variation; appearance never changes membership or social outcomes. |
| **OR-1104** | **READY** | M11.4 | **Limb injury simulation and equipment effects** | OR-1101, OR-1002 | Attributable incident injuries, bounded pain/healing/function rules, relevant mobility/work effects, explicit protective-item mechanics, deterministic saves. |
| **OR-1105** | **READY** | M11.5 | **Autonomous care, accessibility and limb replacement** | OR-1104 | Clinic capacity and accessible transit influence autonomous care/recovery; scars, missing limbs and prosthetics persist; Steward acts through systems. |
| **OR-1106** | **READY** | M11.6 | **Health-aware wardrobe, animation and cause-chain UI** | OR-1102, OR-1103, OR-1105 | Covered injury visuals, replacement attachments, meaningful gait/posture and emotion bubbles; inspector exposes incident, impairment, systems levers and measured recovery. |
| **OR-1107** | **READY** | M11.7 | **Modular resident content and scale acceptance** | OR-1106, OR-1007 | Pose/coverage/content validation, save migrations, pool/LOD reuse, deterministic 30-floor/300-resident run and measured <4 ms p95 tick / 60 FPS / <180 draw calls. |

---

## 3. Current Delivered Baseline (v0.10+)

- **Automated Test Suite**: **832 EditMode passing**, **15 PlayMode passing**, 0 failures, 1 explicit opt-in benchmark skipped.
- **Reference Scale Verified**: In `Tower_GoldStandard30.unity` (30 floors, 300 residents), overview presentation achieves **83 draw calls** (-99.4% vs unbatched), **10.0 ms CPU frame time (~100 FPS)**, and **1.7 ms average simulation tick**.
- **Domain Purity**: Zero `UnityEngine` references in `OneRoof.Domain` and `OneRoof.Application`.
- **Delivered Systems**:
  - Closed-loop integer unit economics across households, businesses, treasury, and outside market (ECON-001..008).
  - 3-tier resident LOD (skeletal -> sprite -> combined silhouette mesh) and semantic zoom facade crossfades (orthographic sizes 8–18).
  - Configurable `ResidentPresentationSettings` and zero-allocation view pooling across population turnover.
  - Emotional resident social sandbox: traits, thoughts with mood deltas, and 12-tier relationship spectrum (ADR-087 / OR-906A).
  - Physical utilities (electrical & plumbing) connecting tower risers to subterranean undercity ports.
  - Secret undercity (32×12 grid, 14 specialized room types, autonomous staffing).
  - 12-month climate cycle with single-draw-call procedural weather VFX.
  - 8 data overlays and Steward policy decrees with immutable decision records.
- **Active Handoff**: Strictly 1 active handoff at [`Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md`](../Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md).
