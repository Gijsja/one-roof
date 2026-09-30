# One Roof Roadmap — Milestone v0.10+ to Beta Boundary

This document tracks active development milestones toward the **Beta Boundary (City Status)** and the planned resident expansion that follows it.
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
| **Transit Scaling** | Dynamic 2-cell shaft widening & elevator tier upgrades | **DONE** | OR-204: Tiers 1–5, ETA-based car dispatch, upgrade commands, save round-trip. |
| **M9.5** | Social ties deepening: issue influence & local organizing | **DONE** | OR-906B (issue-specific social influence) & OR-906C (local faction organizing). |
| **M10.1** | Undercity utility reverse-feed & production integration | **DONE** | OR-1001 Slice C: generator reverse-feed and utility-dependent subterranean production. |
| **M10.2** | Adaptive crisis pressure chains & personal consequences | **READY** | OR-1002: 6 condition-driven crisis chains with warning phases, propagation, and aftermath. |
| **M10.3** | Beta boundary golden acceptance test (City Status 30-day streak) | **READY** | OR-1003: 30 floors / 300 residents deterministic run sustaining City Status for 30 consecutive days under <4 ms tick and 60 FPS budgets. |
| **M10.5** | Campaign board, pacing & end states | **READY** | OR-1005: streak tracking, daily trends, threatened gates, failure explanations, sandbox continuation. |
| **M10.6** | Spatial overlay & accessibility beta pass | **IN PROGRESS** | OR-1006: non-colour equivalents across all 8 overlays, UI scale, remappable controls, pause/speed review. |
| **M10.7** | Save, recovery & content QA | **READY** | OR-1007: crisis/campaign save survival, migrations, corrupt save recovery, partial demolition QA. |

---

## 4. Planned M11 — Modular Residents, Faction Bodywear & Limb Health

**Requested 2026-09-30. Status: planned; implementation has not started.** This expansion follows the City Status beta gate (OR-1003). Complete body-part and appearance contracts before producing a large wardrobe catalog. Crisis integration extends OR-1002 after its initial delivery; M11 does not silently add requirements to the existing beta acceptance gate. No delivery dates are assigned.

### Execution Packet & Immediate Follow-up

The [execution plan](./RESIDENT_SYSTEM_EXECUTION_PLAN.md) reconciles this conversation into package gates, current validation debt, exclusive file ownership and acceptance demonstrations. Use the [agent prompt pack](./Prompts/RESIDENT_SYSTEM_AGENT_PROMPTS.md) by combining its shared prompt with one assignment; prompt availability does not dispatch agents.

**OR-1100 is complete**: current bubbles/archive/animation-preview passed engine and rendered regression checks with an executable archive guard; see the [2026-09-30 validation report](../Docs/Validation/BACKLOG_REVIEW_2026-09-30.md). This is follow-up validation of existing work, independent of City Status. Future M11 feature implementation remains gated by OR-1003. OR-1102 is deliberately split into A1 animation inputs/definitions, A2 anatomy/contact mapping, V1 modular attachment composition and V2 identity-preserving LOD; art production follows the first two contracts.

### Delivery Sequence

| Slice | Task | Depends on | Reviewable outcome |
|---|---|---|---|
| **M11.1** | OR-1101: Stable body and appearance contracts | OR-1003, OR-1100 | Stable resident/body-part IDs; independently saved identity, worn content IDs and limb condition; immutable projections; older saves load healthy residents with valid default outfits. |
| **M11.2** | OR-1102: Modular visual rig and wardrobe | OR-1101 | Extend the shared 17-bone rig, wardrobe catalog and pooled views. Body types, skin, hair, headwear, upper/lower garments, outerwear, footwear and accessories share anchors, compatibility and coverage rules. |
| **M11.3** | OR-1103: Faction and occupation appearance | OR-1102, OR-906C | Deterministic palettes, badges and wardrobe preferences express actual membership, occupation and personality, with individual variation and an explicit policy for overlapping affiliations. |
| **M11.4** | OR-1104: Limb injuries and functional consequences | OR-1101, OR-1002 | Attributable hazards produce bounded per-part injuries, pain, healing and mobility/manipulation effects. Protective item effects are explicit domain rules, separate from cosmetic appearance. |
| **M11.5** | OR-1105: Autonomous care, accessibility and prosthetics | OR-1104 | Residents seek available care and adapt routines; clinic capacity and accessible transit affect recovery. Saved scars, missing limbs and replacements have explicit functional rules and accessible alternatives. |
| **M11.6** | OR-1106: Injury presentation and cause-chain UI | OR-1102, OR-1103, OR-1105 | Garment-aware bandages/scars/prosthetics, gait and posture, bounded emotion/thought bubbles, and inspectable injury causes and recovery. |
| **M11.7** | OR-1107: Content, persistence and scale acceptance | OR-1106, OR-1007 | Integrated 30-floor / 300-resident acceptance, save migration, content validation, pool reuse, LOD fidelity and measured performance. |

### System & Art Contract

- **Independent state**: identity, wardrobe, faction influence, limb health and transient emotion remain separate. Save content IDs and aggregate state, never sprites, rig objects or scene references. Stable parts initially cover head, torso, left/right arms and legs; finer anatomy is an extension point, not first-slice scope.
- **Inspectable animation**: keep pose selection, reason, sample time/phase and evaluated bone transforms visible. Maintain isolated scrub/replay preview and pose-transition regression tests. OR-1102 should move timing, amplitudes and joint constraints into inspectable pose definitions with explicit transition rules, so tuning does not require opaque animation assets. New modular attachments cannot drive animation implicitly; archived predefined representations stay outside runtime setup until intentionally validated under OR-1102.
- **Visual composition**: warm flat illustrated residents with readable silhouettes and restrained outlines. Garments declare slot, compatible body/rig, palette channels, attachment anchors and covered parts/layers. Coats occlude underlying garments; sleeves can conceal injuries. Missing art has a validated fallback. Replacement limbs use the same attachment contract.
- **Content expansion**: body and garment variants cover every delivered pose (currently Idle, Walk, QueueWait, Ride, Sit and Sleep); Work/Gesture are deliberate future extensions. Author new items through the existing asset compiler and registry; avoid bespoke renderer branches for individual factions or outfits. Cosmetic items confer no mechanical benefit unless a validated equipment rule defines one.
- **Faction expression**: wardrobe preference does not create membership or infer hostility. Preserve the existing faction model; support combining applicable influences without introducing multiple memberships as an unplanned simulation change.
- **Health mechanics**: model injury cause, severity, pain, functional capacity, healing and replacement separately. Deterministic fixed-tick rules bound stacked effects and injury history. Integrate real hazards and crisis events; no random cosmetic wounds or faction-derived injuries. Specify exact rates, caps and care costs during OR-1104/1105 design review before implementation.
- **Steward agency**: responses are safer workplaces/infrastructure, healthcare capacity, accessibility and service policy. Residents decide care and routines autonomously; no direct individual treatment/equipment orders or combat subsystem is introduced.
- **Cause-chain**: visible limp or distress → relevant existing overlay → inspector injury and incident → systems response → measured recovery and incident reduction. Link the appropriate overlay to the cause; do not add a ninth overlay implicitly. Critical information uses text/glyphs as well as colour. Bubbles prioritize meaningful symptoms and recovery over repetitive medical status.
- **Scale**: nearby views show layers and expressions; sprite and macro LOD preserve identity through dominant outfit colours and key accessories within measured budgets. Reuse bounded views/buffers; no per-resident Update loops. Clothing combinations must not create unbounded materials or per-frame sprite baking.

### Acceptance Scenarios

1. Several body types wear the same compatible items; a coat covers underlying clothing; headwear masks hair correctly in every supported pose and facing.
2. Faction preferences combine with occupation workwear and personal variation; appearance changes do not alter faction membership or social outcomes.
3. A recorded incident injures one leg, changes relevant travel capacity, generates attributable distress, and improves after autonomous care; a matching arm injury affects relevant work rather than all activities indiscriminately.
4. Clothing conceals a bandage without hiding inspector information; missing/replaced limbs remain aligned while walking, sitting and sleeping, and display correctly across view reuse and zoom changes.
5. Old and new saves round-trip identity, worn items, injury causes, healing, care and replacement state; absent content resolves safely without deleting authoritative injury state.
6. The reference Tower retains deterministic behaviour, <4.0 ms p95 tick, 60 FPS presentation and <180 draw calls under the agreed reference view. Record baseline and post-change measurements rather than claiming the budgets from tests alone.

The initial implementation target is OR-1101, followed by a small representative wardrobe in OR-1102. Broad faction outfit production and severe-injury/prosthetic content follow only after these contracts are validated.

---

## 5. Performance & Validation Tooling

- **Test Suite Status**: **832 EditMode passing**, **15 PlayMode passing**, 0 failures.
- **Reference Overview Performance**: In `Tower_GoldStandard30` (30 floors, 300 residents), overview presentation achieves **83 draw calls**, **86 batches**, **10.0 ms CPU frame time (~100 FPS)**, and **1.7 ms average tick**.
- **Executable Validation & Pipeline Probes**: For exact headless batchmode test commands (`Unity -batchmode ...`) and live Editor pipeline probes (`pipeline_client.py`), see [`Docs/WORKFLOW_AND_TESTING.md`](../Docs/WORKFLOW_AND_TESTING.md).
