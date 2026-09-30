# One Roof Roadmap — Milestone v0.10+ to Beta Boundary

This document tracks active development milestones toward the **Beta Boundary (City Status)**.
For historical delivery details (Milestones M0 through M9, ARCH prompts, and early audit logs), see [`Planning/Archive/ROADMAP_HISTORICAL_ARCHIVE.md`](./Archive/ROADMAP_HISTORICAL_ARCHIVE.md).

---

## 1. Project North Star & Beta Boundary Contract

**Beta Boundary (City Status)**:
- 30 floors and 300 persistent residents running deterministically.
- Full mixed-use zoning: residential, office, retail, diner, clinic, workshop, security, and utilities.
- Physical infrastructure networks: electrical grid, plumbing & gravity waste, technician wear.
- Social dynamics: autonomous needs, satisfaction, grievances, strain, 4 faction archetypes, policy decrees, 12-tier relationship progression spectrum.
- 8 data overlays and 6 adaptive crisis event chains.
- Sustaining **City Status for 30 consecutive in-game days** under strict performance budgets:
  - Simulation tick: **<4.0 ms p95** on reference hardware.
  - Presentation frame rate: **60 FPS** (<16.6 ms main thread time; reference overview achieves ~10.0 ms / ~100 FPS).
  - Draw calls: **<180** (reference overview achieves 83 draw calls via SRP Batcher, GPU instancing, and LOD).

---

## 2. Delivered Foundation (v0.10+ Baseline)

All core architectural tiers and simulation systems are verified operational with **832 EditMode passing tests** and **15 PlayMode passing tests**:
- **Pure-C# Simulation Engine**: Zero `UnityEngine` references in Domain and Application; discrete 1440-tick daily cycle; aggregate push persistence (`ToSaveData()` / `FromSaveData()`).
- **Closed-Loop Unit Economics**: Integer currency conservation across households, commercial tenants, tower treasury, and outside markets (ECON-001..008).
- **Physical Utilities & Secret Undercity**: Ground substation, pumps, vertical risers, technician wear, and 32×12 subterranean excavation grid with 14 specialized room types connected via lobby bridge.
- **Social Fabric & Management**: 4 factions (Tenant Union, Corporate Coalition, Merchant Guild, Civic & Eco Council), policy decree panel, civil actions, decision records, onboarding lesson, and Sims-style emotional social dynamics (OR-906A / ADR-087).
- **Presentation & LOD**: 30-floor scale proven in `Tower_GoldStandard30.unity`, 3-tier resident LOD, semantic zoom facade crossfades (orthographic size 8–18), inspector-authored `ResidentPresentationSettings`, zero-allocation view pooling, single `SRPDefaultUnlit` pass, 12-month procedural weather VFX.

---

## 3. Active Beta Boundary Gates

| Milestone | Scope | Status | Deliverable & Acceptance |
|---|---|---|---|
| **Transit Scaling** | Dynamic 2-cell shaft widening & elevator tier upgrades | **IN PROGRESS** | OR-204: Tiers 1–5, ETA-based car dispatch, upgrade commands, save round-trip. |
| **M9.5** | Social ties deepening: issue influence & local organizing | **READY** | OR-906B (issue-specific social influence) & OR-906C (local faction organizing). |
| **M10.1** | Undercity utility reverse-feed & production integration | **READY** | OR-1001 Slice C: generator reverse-feed and utility-dependent subterranean production. |
| **M10.2** | Adaptive crisis pressure chains & personal consequences | **READY** | OR-1002: 6 condition-driven crisis chains with warning phases, propagation, and aftermath. |
| **M10.3** | Beta boundary golden acceptance test (City Status 30-day streak) | **READY** | OR-1003: 30 floors / 300 residents deterministic run sustaining City Status for 30 consecutive days under <4 ms tick and 60 FPS budgets. |
| **M10.5** | Campaign board, pacing & end states | **READY** | OR-1005: streak tracking, daily trends, threatened gates, failure explanations, sandbox continuation. |
| **M10.6** | Spatial overlay & accessibility beta pass | **READY** | OR-1006: non-colour equivalents across all 8 overlays, UI scale, remappable controls, pause/speed review. |
| **M10.7** | Save, recovery & content QA | **READY** | OR-1007: crisis/campaign save survival, migrations, corrupt save recovery, partial demolition QA. |

---

## 4. Performance & Validation Tooling

- **Test Suite Status**: **832 EditMode passing**, **15 PlayMode passing**, 0 failures.
- **Reference Overview Performance**: In `Tower_GoldStandard30` (30 floors, 300 residents), overview presentation achieves **83 draw calls**, **86 batches**, **10.0 ms CPU frame time (~100 FPS)**, and **1.7 ms average tick**.
- **Executable Validation & Pipeline Probes**: For exact headless batchmode test commands (`Unity -batchmode ...`) and live Editor pipeline probes (`pipeline_client.py`), see [`Docs/WORKFLOW_AND_TESTING.md`](../Docs/WORKFLOW_AND_TESTING.md).
