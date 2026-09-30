# One Roof — Active Backlog

Status values: `READY`, `IN PROGRESS`, `BLOCKED`, `DONE`. One agent owns one task at a time.
For all completed foundation tasks and historical validation logs (Milestones M0 through M10 scale foundation, OR-001 through OR-906A, LOD, and Pooling), see [`Planning/Archive/BACKLOG_HISTORICAL_ARCHIVE.md`](./Archive/BACKLOG_HISTORICAL_ARCHIVE.md).

---

## 1. Active & Upcoming Tasks (Beta Boundary / City Status)

| ID | Status | Milestone | Task | Depends on | Acceptance |
|---|---|---|---|---|---|
| **OR-204** | **IN PROGRESS** | Transit | **Elevator shaft widening & tier upgrades** | OR-705 | Dynamic 2-cell shaft widening, tier profiles (Tier 1–5), ETA-based car dispatch, commands (`UpgradeElevatorTierCommand`, `WidenElevatorShaftCommand`), save round-trip. |
| **OR-906B** | **READY** | M9.5 | **Issue-specific social influence** | OR-906A, OR-602 | Strong recent ties share named grievances using prior-settlement state; capped influence inspectable separately from direct exposure; reverses with relief. |
| **OR-906C** | **READY** | M9.5 | **Local faction organizing** | OR-906B, OR-903 | Require sustained grievance pressure and connected local membership for civil action readiness; recover on measured cause improvement. |
| **OR-1001 Slice C** | **READY** | M10.1 | **Undercity utility integration & reverse feed** | OR-801, OR-802, UNDERCITY-001 | Generator reverse-feed to surface electrical grid and utility-dependent subterranean production. |
| **OR-1002** | **READY** | M10.2 | **Adaptive crisis pressure & personal consequences** | OR-903, OR-803, OR-604, ECON-004 | 6 condition-driven chains (Power Cascade, Water Contamination, Transit Collapse, Rent Boycott, Labor Strike, Building Crackdown) with warning phases, propagation, management levers, and saved resident consequences. |
| **OR-1003** | **READY** | M10.3 | **Beta boundary golden acceptance test (City Status)** | OR-1001, OR-1002, OR-1005, OR-1006, OR-1007 | 30 floors and 300 residents run deterministically, sustaining all published City Status gates for 30 consecutive days under <4.0 ms tick and 60 FPS presentation budgets. |
| **OR-1005** | **READY** | M10.3 | **Campaign board, pacing, and end states** | OR-1002, OR-904 | Eligibility, daily trends, threatened gates, and 30-day streak are inspectable; failed streak gives reasons; sandbox continuation. |
| **OR-1006** | **READY** | M10.3 | **Spatial overlay and accessibility beta pass** | OR-903, OR-905 | All 8 overlays use contracted channels with non-colour equivalents; UI scale, remappable controls, pause/speed pass review. |
| **OR-1007** | **READY** | M10.3 | **Save, recovery, and content QA** | OR-1002, OR-1005 | Crisis and campaign state survive save/load; migrations, corrupt saves, partial tower demolition, and content review pass. |

---

## 2. Current Delivered Baseline (v0.10+)

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
