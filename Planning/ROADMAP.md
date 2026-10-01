# One Roof — Forward Roadmap to City Status

Updated 2026-10-01. Organize work by complete, reviewable outcomes and shared hardening needs. Existing OR IDs remain traceable; grouped packages below replace the old milestone order as the execution schedule. No delivery dates or agents are assigned by this document.

Use [BACKLOG](./BACKLOG.md) for eligibility and status, [Beta hardening execution plan](./BETA_HARDENING_EXECUTION_PLAN.md) for contracts, ownership and agent assignments, and the single [active handoff](../Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md) for current evidence. Earlier milestones remain in the [historical archive](./Archive/ROADMAP_HISTORICAL_ARCHIVE.md).

## 1. Destination and non-negotiable beta boundary

City Status requires a deterministic 30-floor / 300-Resident mixed-use Tower, physical utilities, autonomous social/economic systems, all eight diagnostic overlays and six adaptive crisis chains. Sustain the published eligibility gates for **30 consecutive in-game days** with **<4.0 ms p95 simulation tick**, **60 FPS presentation** and **<180 draw calls** on the agreed reference hardware/view.

The six chains remain Power Cascade, Water Contamination, Transit Collapse, Rent Boycott, Labor Strike and Building Crackdown. Every chain must connect **symptom → overlay → inspector cause → systems response → measured feedback**, and preserve its attributable consequences across saves. Contract work must define the exact City Status metric thresholds; an unspecified gate cannot count as passing.

Domain/Application stay pure C#; fixed ticks, stable IDs, immutable projections, validated commands, aggregate persistence, bounded history and centralized pooled presentation remain required. M11 limb health and wardrobe expansion stay after City Status; they are not new beta prerequisites.

## 2. Starting evidence and what needs hardening

Dated test-remediation snapshot (2026-10-01): **1,131 EditMode passed / 0 failed / 3 skipped** and **17 graphical PlayMode passed / 0 failed / 1 optional diagnostic skipped**. These results establish regression evidence, not six-chain completion or performance certification. Counts increased partly through independent parameter cases. Artifacts and scope are recorded in the active handoff; rebaseline the current tree before implementation.

Delivered foundations include elevator widening/tier upgrades (OR-204), social influence and local organizing (OR-906B/C), connected undercity utilities (OR-1001 Slice C), civil-action phases and persistence, decision history, utility wear/repair and diagnostic overlays. Reuse these owners; do not create a second simulation framework.

| Area | Existing foundation | Closure gap |
|---|---|---|
| Rent Boycott / Labor Strike | Saved RentStrike/WorkSlowdown phases, economic effects, organizing and decision records | Define participation/propagation and attributable aftermath; distinguish timed containment from cause-driven recovery; expose and prove the full cause-chain. |
| Power Cascade / Transit Collapse | Brownouts, wear/repair, connectivity, congestion and capacity interventions | Add sustained-condition warning/escalation and bounded propagation with saved consequences and measured recovery. |
| Water Contamination | Pump pressure, water/waste connectivity and equipment wear | Supply failure is not contamination. Define contamination source, network exposure, safe containment and recovery using existing systems. |
| Building Crackdown | Scrutiny signals and saved exposure-driven undercity investigators/disruptions | Specify their relationship; connect event pressure to an explicit scoped event, validated systems responses and expiry/aftermath. No instant scrutiny veto on construction. |
| Caretaker Slice A | Playable lobby contact, organized warnings, fixed-step combat and saved household eviction consequences | Preserve as a delivered separate slice; finish input/accessibility/performance hardening. It does not replace any of the six named chains. |
| Presentation performance | Historical overview measurements were within budget | Native 1080p Caretaker measurement was ~36 FPS on GT 755M; staged draws were ~181.6 at 720p. Reproduce comparable views and close measured bottlenecks. This blocker remains real. |

## 3. Grouped delivery and parallel preparation

| Wave | Packages | Reviewable outcome / exit gate |
|---|---|---|
| **W0 — Establish truthful contracts and baseline** | OR-1002-C0; begin OR-1003-P, OR-1005-A, OR-1006-A, OR-1007-A | Six chain specifications, exact City Status gates, regression/measurement baseline, exclusive integration ownership and concrete closure matrix. |
| **W1 — Finish the closest complete experiences** | OR-1002-C1 (Rent Boycott), OR-1002-C2 (Labor Strike), OR-1002-U per delivered chain | Two systems-driven chains with saved phases/consequences, actionable cause-chain UI and demonstrated response feedback. Share machinery, retain distinct contracts and tests. |
| **W2 — Close infrastructure and external-event gaps** | OR-1002-I1 (Power), I2 (Transit), I3 (Water); OR-1002-E1 (Crackdown) | Four remaining chains satisfy the same end-to-end gate. Power/transit can progress on separate files; water and crackdown specify their novel rules before broad implementation. |
| **Continuous — Make each slice durable and usable** | OR-1007-A/R; OR-1006-A; OR-1003-P; OR-1005-A/B | Persistence, explanation, accessibility, input and measured performance ship with each slice rather than being deferred to a final cleanup milestone. |
| **W3 — Prove composition and City Status** | OR-1002-X, OR-1005-B, OR-1007-R, OR-1003-G | Cross-chain interaction/replay tests, completed campaign board and recovery QA, followed by the actual 30-day / 300-Resident acceptance under all budgets. |

Waves express priority, not blanket barriers. A READY package can start when its own inputs exist; a later-wave read-only investigation or harness can proceed earlier. Shared aggregate/save/projection edits remain serialized through one integration owner. Parallel execution requires explicit authorization and non-overlapping file ownership.

### Blocker reassessment

- **Retired as blanket blockers:** waiting for all six chains before writing campaign eligibility logic, save/recovery fixtures, accessibility improvements or profiling. Each has an independently startable preparation package.
- **Still blocking closure:** incomplete six-chain behavior/explanation; unspecified City Status thresholds; current full-resolution presentation budget; unfinished input/accessibility; missing combined save/replay and 30-day acceptance evidence.
- **Corrected status:** old READY labels on OR-1003/1005/1007 and M11 meant queued despite unmet dependencies. The backlog now separates READY preparation from BLOCKED final gates and QUEUED expansion.
- **Historical evidence:** 832/15 and later 920/15 totals, 83-draw/~100-FPS overview numbers, previous live-Editor outages and individual historical failures are dated snapshots. Recheck the relevant current condition; neither assume they still block nor claim they certify this tree.

A blocker record needs the affected package, current evidence, precise missing input, next discriminating check and exit condition. Missing test evidence is validation debt; broken tooling is an environment blocker; a deliberate beta/M11 boundary is a scope gate. Do not conflate them.

## 3a. Preserve the delivered Caretaker slice

OR-1002 Slice A remains delivered. See [Caretaker extraction and acceptance](./CARETAKER_ARCADE_GAMEPLAY.md) for provenance. The permanent lobby contact supports rehearsal/enforcement from connected faction evidence; real slabs/stairs define fixed-step, bounded-wave encounters. Saved encounters resume paused. Winning records whole-household eviction, autonomous departure, witness thoughts, scrutiny and decision history; rehearsal restores prior crisis state.

Direct Caretaker control is the existing explicit exception to systems-only control; other Residents remain autonomous. Internal walls, doors and furnishing collisions are outside the delivered arcade-lane model. Existing test isolation, matched weather controls and opt-in capture diagnostics must be preserved. The remaining HUD/input/accessibility/performance work belongs to the shared hardening lanes, not a seventh required beta chain.

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

## 5. Evidence required to close the roadmap

A green unit suite alone cannot close a chain. Every chain requires the contract matrix, immutable explanation, validated systems intervention, attributable saved aftermath, deterministic continuation and observed feedback defined in the [execution plan](./BETA_HARDENING_EXECUTION_PLAN.md#2-common-definition-of-done).

OR-1003-G closes only after all six chain packages plus explanation, campaign, recovery, accessibility and performance acceptance pass. Publish the actual 30-day streak, gate values/failure reasons, replay comparison and native reference-view measurements. Report skipped or unavailable evidence explicitly; do not substitute explicit Camera.Render timing for full presentation with HUD.

Use [WORKFLOW_AND_TESTING](../Docs/WORKFLOW_AND_TESTING.md) for exact commands and `unity-cli` Editor probes. Recheck the lock/active Editor, coordinate one Unity runner, preserve unrelated changes, and retain one concise active handoff. Archived delivery detail is context, not a reason to reopen completed foundations without regression evidence.
