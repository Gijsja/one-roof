# Resident Systems — Execution Plan

Date: 2026-09-30. Planning artifact; this document dispatches no agents and starts no new runtime implementation.

## 1. Intent and scope

Replace fragile resident presentation with a modular, explainable system. Residents need tidy action/thought bubbles connected to actual social mechanics; interchangeable bodywear and faction/occupation expression; attributable limb injury and recovery; and animation that can be inspected, scrubbed, tuned and tested. RimWorld is a reference for composability and visible consequences, not a request to reproduce its implementation or add combat/direct resident orders.

The visual direction is a warm flat illustrated silhouette, readable against the architectural cutaway. It is a target for a small representative set, not authorization to mass-produce full-body character sheets. Existing predefined resident representations remain archived outside `Assets`. Procedural geometry and the generated neutral LOD proxy are the interim representation.

Canonical contracts: [asset/animation contract](../Docs/ASSETS.md#3-character-assembly--inspectable-animation-contract), [bubble/UX contract](../Docs/UX_AND_GAMEPLAY.md#8-resident-thought-action--emotion-bubbles), [roadmap scope and City Status gate](./ROADMAP.md#4-planned-m11--modular-residents-faction-bodywear--limb-health). This packet owns detailed [system and art contracts](#8-system-and-art-contract) and [acceptance scenarios](#9-acceptance-scenarios). Read architecture or testing guidance only when the assigned slice needs it.

## 2. What exists versus what is planned

| Area | Current working-tree implementation | Remaining work |
|---|---|---|
| Bubbles | Upright cards, fitted text, action pointer/thought dots, emotion cues, inspected-resident priority, overlap/count/zoom controls | Current Unity regression and crowded Tower verification; future typed emotion/health cues and stability/accessibility checks |
| Appearance | Shared 17-bone procedural rig, palette/proportion variants, bounded pooled views | Stable appearance aggregate, modular garment contract, faction/occupation preference integration, content promotion |
| Archive | 50 PNGs plus original metadata moved outside Assets; 103 manifest entries/checksums; body/cutout bindings removed; automatic AssetLab resident registration removed | Archive guard and current validation complete; preserve enforcement during future modular registration |
| Animation | Explicit evaluator, selection reason, sample time/phase; isolated editor preview source, scrubbing and bone table | Current preview/replay verified; future explicit animation input contract, tunable pose definitions, transitions/contact anchors, body-part-to-bone mapping |
| Limb health | Design only | Domain injury/function/recovery, care/accessibility, protection/prosthetics, persistence and cause-chain presentation |
| LOD | Neutral generated silhouette for intermediate/picking use; existing macro tier | Preserve identity/garment cues across tiers without restoring predefined full-body art; verify appearance cache invalidation |

Everything above is uncommitted working-tree work unless repository evidence says otherwise. Do not infer completion from previous messages, historical test totals or planned task status.

## 3. Evidence and validation debt

**Current Q0 update (2026-09-30):** OR-1100 is complete in the [backlog validation pass](../Docs/Validation/BACKLOG_REVIEW_2026-09-30.md), including executable archive guard, current engine regressions, six-pose replay, actual isolated preview lifecycle and rendered tinted bodies/bubbles. Historical evidence below remains for attribution; current results are in the linked report. C1 remains gated by OR-1003.

- Bubble-stage presentation tests: 393/393 passed before the final URP sprite-shader preference adjustment; PlayMode: 15/15 passed before the subsequent representation/archive changes. The final bubble material rendered successfully in a temporary isolated preview.
- Broader bubble-stage EditMode: 877/881 passed. Failures were `ElevatorShaftWideningTests.Widen_ShaftGapInSelectedFloors_RejectsWithoutPartialChanges`, `ResidentSocialSystemTests.CoPresence_RoomAndElevator_AdvancesSocialSimulation`, `ResidentSocialSystemTests.ThoughtMemories_DirectlyModulateResidentSatisfactionAndStrain`, and `TowerSimulationCanExecuteTests.WidenShaft_ChargesOneCellPerServedFloorAndUnlocksWideCar`. These are historical observations outside the bubble change, not a current attribution verdict.
- Archive/inspection stage: direct Roslyn compilation against installed Unity assemblies passed for Content, Presentation, Editor and updated Content/Presentation test assemblies. Fifteen pure wardrobe/rig test methods passed through a managed runner outside Unity. Archive checksums and absence of serialized image-GUID references were verified at that stage.
- Live Editor Pipeline became unavailable. The new editor window, pose replay regression, archive/picking/LOD integration and the current full Unity test suite have not been runtime-verified. Reconnect/recheck before claiming them complete.
- Published 832-test and performance baselines in older docs describe an earlier reference state; they are not validation of this working tree.

**OR-1100/Q0 is complete.** Preserve its validation and archive guard before expanding resident work after OR-1003. Do not silently fix unrelated economy, persistence, housing, transit, social or project-settings edits merely to obtain a green suite. Record ownership and reproduce failures before attribution.

## 4. Delivery order and dispatch gates

OR-1100 is immediate follow-up validation of already requested work, independent of City Status. New M11 feature implementation stays queued after OR-1003, as currently planned. This packet does not move the expansion ahead of beta or add beta requirements. Dates and estimates remain unassigned.

| Package | Parent | Dependencies | Output required before the next package |
|---|---|---|---|
| Q0 | OR-1100 | Existing working tree | Current validation report, executable archive guard, verified isolated preview, reproducible gaps |
| C1 | OR-1101 | OR-1003, Q0 | Stable body/appearance/health/save/projection contract and migration tests |
| A1 | OR-1102 | C1 | Explicit animation inputs, precedence, pose definitions, transition/replay tests and preview integration |
| A2 | OR-1102 | A1 | Body-part-to-bone map and interaction/contact contract with pose validation |
| V1 | OR-1102 | A2 | Small modular bodywear set and deterministic composition/coverage rules |
| V2 | OR-1102 | V1 | Identity-preserving LOD, bounded caches and measured representative render results |
| F1 | OR-1103 | V2, OR-906C | Deterministic wardrobe preferences from actual faction/occupation/personality state |
| H1 | OR-1104 | C1, OR-1002 | Attributable injury/function/protection rules and deterministic aggregate persistence |
| H2 | OR-1105 | H1 | Autonomous care/accessibility, bounded recovery, scars and replacement rules |
| U1 | OR-1106 | F1, H2, A2 | Health-aware wardrobe/animation, inspectable cause-chain and faithful thought/emotion cues |
| Q1 | OR-1107 | U1, OR-1007 | Current integrated test/render/save/scale evidence and final delivery report |

OR-1102 is complete only after A1, A2, V1 and V2 pass their gates. These are work packages, not four independent claims that the parent feature is done. H1/H2 and the visual branch may run in parallel after C1 freezes their shared projection/body-part interfaces. U1 integrates both branches. Do not parallelize edits to shared aggregate/save/projection files.

## 5. Contracts that must be settled before broad art production

| Contract | Owner | Required decision/evidence |
|---|---|---|
| Identity and anatomy | C1 | Stable resident/part IDs; left/right convention; head/torso/arm/leg groups; absent/replaced parts; aggregate owner and old-save defaults |
| Body-part-to-bone map | A2 | Each part resolves to the expected bone chain, attachment slots and functional capability; mirrored rendering never swaps authoritative left/right |
| Animation inputs | A1 | Activity, travel state, actual speed, facing, emotion, limb function and interaction target; explicit precedence and meaningful selection reason |
| Time and transitions | A1 | Sample time/phase, pause/speed semantics, root/body scale, blend durations and constraint limits; reproducible replay independent of prior pose |
| Contact | A2 | Feet/floor, seat/hip, bed/body and hand/tool anchors; graceful fallback when furniture or target disappears |
| Garment coverage | V1 | Slots, compatible bodies, tint channels, part coverage, occlusion order, pose-dependent arm ordering and missing-content fallback |
| Health consequences | H1/H2 | Causes, severity, pain/function caps, hazard exposure, care rates/costs/accessibility and permanent/replacement effects; document values from existing systems before choosing new tuning |
| LOD identity | V2 | Which stable identity/garment cues survive each tier; dirty/version key; bounded rebuild frequency; no per-frame outfit baking |
| Cue semantics | U1 | Read-only typed cue source or an explicit compatibility adapter for current thought strings; never infer affection, diagnosis or recovery from color/text alone |

## 6. Working rules for agents

1. Send the [shared prompt](./Prompts/RESIDENT_SYSTEM_AGENT_PROMPTS.md#shared-prompt--include-with-every-assignment) plus exactly one package prompt. Check its dependencies; assign one owner. Prompt availability is not dispatch authorization.
2. Record the starting `git status --short`. The working tree contains other active changes, including housing/economy/save/social/transit/project settings. Preserve them. New assets require paired `.meta` files; archived files retain original metadata.
3. Read `AGENTS.md`, the backlog and the single active handoff; then one canonical document appropriate to the slice. Use applicable Unity skills. Reuse existing aggregates/presenters/catalogs/pools instead of parallel managers.
4. The coordinator owns shared contract decisions and integration points (`NpcSkeletalHierarchy`, `TowerResidentPresenter`, `PersonRecord`/population aggregates, save DTOs and projections). Assign exclusive ownership before any branch edits one. A helper/test task may run independently only with agreed APIs and separate files.
5. Only the coordinator updates roadmap/backlog status and the single active handoff during parallel work. Agents return their findings and exact changes; they do not create competing active handoffs.
6. Domain/Application remain pure C#; presentation consumes immutable projections; Steward actions stay systems-level and validated. No faction-derived hostility/injury, arbitrary medical bubbles, individual equipment orders, unbounded history, per-resident Update loops or unsaved authoritative health state.
7. Scene/prefab edits use connected Unity tooling when available; preserve GUIDs and unrelated serialization. Do not restore archived full-body art to make tests or LOD look better. Modular content promotion is explicit and validated.
8. Completion reports contain the package ID, changed files, public contract changes, exact tests and renders, limitations, unresolved ownership conflicts and the next eligible package. Compilation alone is not runtime or visual acceptance.

## 7. Integrated demonstration

Use a small reusable fixture before the 300-resident reference Tower: several residents with differing proportions share compatible garments; one has faction-influenced colors plus workwear; one has a recorded leg injury; one has an arm injury; one has a replacement. Show walking, queued/riding, sitting, sleeping and room work in both facings. Show garment coverage, bandage concealment with inspector visibility, readable action/thought/emotion cues and correct contact points. Change appearance and zoom, recycle views, save/load, and remove an interaction target. Add Work/Gesture poses deliberately rather than labeling them already supported.

The final run must preserve deterministic results, old-save compatibility, bounded runtime allocations/materials and the reference targets (<4 ms p95 tick, 60 FPS, <180 draw calls under the agreed reference view). Archive/compiler guards must prove predefined representations cannot enter setup merely by being copied into a runtime folder. A regression captures the initial unreadable rotating/crowded-caption failure as well as the later modular/injury scenarios.

## 8. System and art contract

- **Independent state**: identity, wardrobe, faction influence, limb health and transient emotion remain separate. Save content IDs and aggregate state, never sprites, rig objects or scene references. Stable parts initially cover head, torso, left/right arms and legs; finer anatomy is an extension point, not first-slice scope.
- **Inspectable animation**: keep pose selection, reason, sample time/phase and evaluated bone transforms visible. Maintain isolated scrub/replay preview and pose-transition regression tests. OR-1102 should move timing, amplitudes and joint constraints into inspectable pose definitions with explicit transition rules, so tuning does not require opaque animation assets. New modular attachments cannot drive animation implicitly; archived predefined representations stay outside runtime setup until intentionally validated under OR-1102.
- **Visual composition**: warm flat illustrated residents with readable silhouettes and restrained outlines. Garments declare slot, compatible body/rig, palette channels, attachment anchors and covered parts/layers. Coats occlude underlying garments; sleeves can conceal injuries. Missing art has a validated fallback. Replacement limbs use the same attachment contract.
- **Content expansion**: body and garment variants cover every delivered pose (currently Idle, Walk, QueueWait, Ride, Sit and Sleep); Work/Gesture are deliberate future extensions. Author new items through the existing asset compiler and registry; avoid bespoke renderer branches for individual factions or outfits. Cosmetic items confer no mechanical benefit unless a validated equipment rule defines one.
- **Faction expression**: wardrobe preference does not create membership or infer hostility. Preserve the existing faction model; support combining applicable influences without introducing multiple memberships as an unplanned simulation change.
- **Health mechanics**: model injury cause, severity, pain, functional capacity, healing and replacement separately. Deterministic fixed-tick rules bound stacked effects and injury history. Integrate real hazards and crisis events; no random cosmetic wounds or faction-derived injuries. Specify exact rates, caps and care costs during OR-1104/1105 design review before implementation.
- **Steward agency**: responses are safer workplaces/infrastructure, healthcare capacity, accessibility and service policy. Residents decide care and routines autonomously; no direct individual treatment/equipment orders or combat subsystem is introduced.
- **Cause-chain**: visible limp or distress → relevant existing overlay → inspector injury and incident → systems response → measured recovery and incident reduction. Link the appropriate overlay to the cause; do not add a ninth overlay implicitly. Critical information uses text/glyphs as well as colour. Bubbles prioritize meaningful symptoms and recovery over repetitive medical status.
- **Scale**: nearby views show layers and expressions; sprite and macro LOD preserve identity through dominant outfit colours and key accessories within measured budgets. Reuse bounded views/buffers; no per-resident Update loops. Clothing combinations must not create unbounded materials or per-frame sprite baking.

## 9. Acceptance scenarios

1. Several body types wear the same compatible items; a coat covers underlying clothing; headwear masks hair correctly in every supported pose and facing.
2. Faction preferences combine with occupation workwear and personal variation; appearance changes do not alter faction membership or social outcomes.
3. A recorded incident injures one leg, changes relevant travel capacity, generates attributable distress, and improves after autonomous care; a matching arm injury affects relevant work rather than all activities indiscriminately.
4. Clothing conceals a bandage without hiding inspector information; missing/replaced limbs remain aligned while walking, sitting and sleeping, and display correctly across view reuse and zoom changes.
5. Old and new saves round-trip identity, worn items, injury causes, healing, care and replacement state; absent content resolves safely without deleting authoritative injury state.
6. The reference Tower retains deterministic behaviour, <4.0 ms p95 tick, 60 FPS presentation and <180 draw calls under the agreed reference view. Record baseline and post-change measurements rather than claiming the budgets from tests alone.
