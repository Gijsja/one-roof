# Resident Systems — Agent Prompt Pack

Date: 2026-09-30. Ready-to-copy assignments; no agents have been dispatched by this document. Send the shared prompt followed by one package prompt. The [execution plan](../RESIDENT_SYSTEM_EXECUTION_PLAN.md) owns sequence, gates and validation history. The single [active handoff](../../Handoffs/Active/HANDOFF_2026-09-30_v0.10-documentation-and-beta-readiness.md) owns current status.

## Shared prompt — include with every assignment

```text
You are working on One Roof at /home/geisha/Vibecode/UnityAI/one-roof. Execute only the assigned resident-system work package after its prerequisites are verified. Read AGENTS.md, Planning/BACKLOG.md, the single active handoff and Planning/RESIDENT_SYSTEM_EXECUTION_PLAN.md. Then read the one canonical doc appropriate to this package and applicable Unity skills. Verify actual code and tests rather than relying on historical summaries.

The intended system is modular resident identity/bodywear, faction/occupation preferences, attributable limb health, inspectable animation and readable mechanics-derived thought/action/emotion cues. Extend existing aggregates, immutable projections, shared rig, catalogs and bounded pools. Domain/Application must have no UnityEngine references. Residents act autonomously; the Steward changes systems through validated commands. This is not a combat or direct resident-control task.

Predefined full-body resident artwork and old cutouts are archived at Art/Archive/ResidentRepresentations/2026-09-30, outside Assets. Do not restore or implicitly load them. Procedural close rigs and generated neutral LOD proxies are the interim representation. Modular attachments require an explicit validated rig/content contract; copying a PNG into a folder must not activate it. Keep emotion/environment art intact.

Record starting git status and preserve unrelated dirty files. Confirm exclusive ownership before changing shared hierarchy/presenter, population, save or projection files. The coordinator manages shared contracts and the sole active handoff. Do not create another active handoff, change another package's status, reset the working tree, commit unrelated work or message another chat without authorization. Agent prompt files are not a reason to launch additional agents.

Use meaningful behavior tests, compile/import checks and visual validation appropriate to the change. Never claim runtime/visual validation from compilation alone. Current live Editor availability is unknown: recheck it. Follow Docs/WORKFLOW_AND_TESTING.md when running tests; never pass -quit with -runTests. Include paired .meta files for new assets. Do not install packages or regenerate scenes just to make validation easier.

Before editing, state the package's prerequisites, owned files, integration API and intended evidence. Then implement the authorized slice, resolve failures introduced by it, review the diff and return: package ID; behavior delivered; changed files; public/serialized contract changes; exact test/render results; remaining limitations; next eligible package. If a prerequisite is not met, report concrete evidence and useful independent checks without building a speculative parallel system.
```

## Q0 — OR-1100: Validate the current resident baseline and enforce the archive

```text
Validate the resident bubble, artwork archive and animation inspection work already in the working tree. This follow-up is eligible now and does not depend on City Status. Use Docs/WORKFLOW_AND_TESTING.md as the canonical validation guide; consult the asset contract only for the archive/preview checks.

Own narrowly scoped resident validation tests and an AssetLab/import/build guard. Reserve ResidentAnimationPreviewWindow.cs and AssetLabValidator.cs with the coordinator; runtime fixes require an identified reproduction and explicit file ownership. Reconnect to the correct Unity project, capture current Console/test baseline, import, run the relevant resident tests and PlayMode checks, and verify the isolated preview opens/scrubs without modifying or leaking into the active Tower scene.

Verify archive checksums, paired metadata, absence of image GUID references and runtime bindings. Add an executable guard that rejects unapproved predefined resident body/wardrobe representations in active runtime registration. Test deliberate reintroduction through Resources, Addressables and serialized references using temporary fixtures; do not restore the archive permanently. The future approved modular attachment path must remain possible through explicit validated registration.

Check fitted/upright text, emotion fidelity, crowd overlap/zoom/inspection, pose replay, facing, pooled reuse, picking and all LOD tiers. Capture representative visuals from the current state. Historical broad-suite failures are not automatically current or attributable to this work: reproduce and report them separately. Return current evidence and remaining blockers. Mark the baseline verified only when engine and visual checks have actually run; a managed compile is a fallback, not the gate.
```

## C1 — OR-1101: Establish stable body, appearance, health and save contracts

```text
Implement C1 only after OR-1003 and Q0 are verified. Use Docs/ARCHITECTURE.md. Own the agreed population/body/appearance aggregate extensions, save DTO additions, immutable projections and migration tests. Coordinate exclusive access to PersonRecord.cs, PopulationState.cs and shared persistence/projection files before edits.

Separate stable identity, worn content IDs, faction appearance influence, limb health and transient expression. Define stable head/torso/left-right arm/leg identities, authoritative left/right independent of facing, functional capability groups and states needed for future absent/replaced parts. Reuse EntityId/content conventions. Document aggregate ownership and publish the minimal contract A1/A2/V1/H1 will consume; suggested names in planning are not an API that must be copied blindly.

Implement only the foundational data/persistence behavior, without hazards, treatment, prosthetic bonuses, outfit rendering or new faction membership rules. Old saves load healthy residents with deterministic valid appearance defaults and no fabricated injury history. New saves round-trip independently of views; unknown content gets a visual fallback without erasing authoritative health or ownership. Test migrations, stable IDs, invalid data, same-seed/load determinism and zero engine references. Freeze and hand off the agreed part/capability/projection interfaces before downstream parallel work begins.
```

## A1 — OR-1102 package 1: Make animation inputs and pose definitions explicit

```text
Implement A1 after C1. Use Docs/ASSETS.md. Own dedicated presentation animation input/pose/transition code and its tests; reserve NpcSkeletalHierarchy.cs, TowerResidentPresenter.cs and ResidentAnimationPreviewWindow.cs integration edits with the coordinator.

Extract a small explicit animation input contract from existing activity, transit, actual movement, facing, interaction, emotion and projected limb-function state. Define precedence and expose a meaningful selection reason; rendering must not choose gameplay actions or infer social/medical state. Replace unexplained pose constants with inspectable timing/amplitude/constraint definitions, reusing existing content/settings conventions rather than installing another framework.

Keep time, resident phase, pause/speed behavior and transition policy explicit. Support existing Idle/Walk/QueueWait/Ride/Sit/Sleep first; Work/Gesture are additions, not currently shipped clips. Preserve scale/facing, upright bubble compensation and the central presentation loop. Extend the preview to inspect inputs, definitions and selected output with deterministic scrub/replay. Test conflicting inputs, transition continuity, clip switches, zero/missing speed, pause, left/right facing and same-input/time replay after other poses. Return the input/pose contract needed by A2 and U1, with compile and rendered evidence.
```

## A2 — OR-1102 package 2: Define anatomy mapping and interaction contacts

```text
Implement A2 after A1. Use Docs/ASSETS.md. Own the body-part-to-bone/attachment/contact mapping and pose-contact tests; agree any shared rig, room furnishing or presenter integration files with the coordinator first.

Map C1 body parts to the actual shared bone chains, garment/prosthetic slots and mobility/manipulation capabilities. Mirroring changes presentation only; an authoritative left-arm injury must never become a right-arm injury when facing changes. Keep missing/replaced-part data distinct from temporarily hidden renderers.

Define feet/floor, seat/hip, bed/body and hand/tool contacts using existing InteractionPoint and RoomFurnishingPresenter contracts. Validate proportions, both facings, standing/walking/sitting/sleeping and permitted transitions. Furniture/targets that move or disappear must yield a documented fallback, never stale anchors or simulation mutation. Preview contact markers and report position errors rather than relying only on how a screenshot looks. Do not implement treatment/hazards or produce a large wardrobe. Return the attachment/contact table that V1 and U1 can implement against.
```

## V1 — OR-1102 package 3: Build the modular resident and bodywear compositor

```text
Implement V1 after A2. Use Docs/ASSETS.md. Own modular attachment definitions/catalog validation and composition code, plus a small representative content set and tests. Reserve shared rig/material/pool integration files with the coordinator.

Use warm flat illustrated shapes, clear silhouettes and restrained outlines. Extend the existing shared rig/wardrobe/pooling system. Garments declare slot, compatible bodies, anchors, palette channels, covered parts/layers, occlusion and any pose-dependent arm ordering. Distinguish headwear, outerwear and accessories without hardcoding individual factions or outfits into the renderer. Supply safe missing-content fallbacks.

Demonstrate several body proportions sharing garments; a coat covering shirt/trousers; headwear masking hair; stable attachment positions through all supported poses/facings; and clothing covering a synthetic injury marker while its inspector state stays readable. Synthetic visual fixtures are tests, not authoritative injury mechanics. Keep cosmetic items separate from mechanical effects. Add content only through the explicit compiler/registry gate; never restore archived full-body sheets/cutouts as runtime inputs. Validate alpha/tint/materials, coverage, view recycling, missing parts and appearance changes. Return a small coherent set before broad art production.
```

## V2 — OR-1102 package 4: Preserve appearance across LOD and pooled reuse

```text
Implement V2 after V1. Use Docs/ASSETS.md. Own TowerResidentLod.cs and appearance-LOD cache/tests; reserve pool/settings/shared presenter changes with the coordinator.

Replace the interim loss of identity at distant tiers with a bounded representation derived from the resident's actual appearance projection: proportions, dominant garment colors and selected recognizable accessories. Preserve stable picking/identity through zoom and turnover. Do not reload predefined full-body sprites or bake a complete outfit every frame.

Specify an appearance version/dirty key, bounded cache/rebuild policy and lifecycle ownership. Verify appearance updates invalidate the correct entry, retired IDs release resources, reused roots do not inherit another resident's outfit/health cues, and inspected residents preserve appropriate detail. Measure representative material/draw-call/allocation/CPU results against a captured current baseline. Test close/sprite/macro transitions, facings, repeated zoom, missing content and 300-resident turnover. OR-1102 is not complete until A1/A2/V1/V2 are integrated and validated.
```

## F1 — OR-1103: Compose faction and occupation wardrobe preferences

```text
Implement F1 after V2 and OR-906C. Use Docs/ARCHITECTURE.md for authoritative state and deterministic selection. Own appearance preference rules/projection integration and tests; coordinate shared population/content files.

Derive palette, badge and wardrobe preferences from actual faction affiliation, actual occupation, personality and stable individual variation. Existing palette variant profession labels are visual reference data, not evidence of a resident's real job or faction. Preserve the existing affiliation model; do not add multiple membership mechanics because the renderer can combine several visual influences.

Specify precedence and eligibility when occupation/workwear, faction expression, personality, availability and future protective gear conflict. Respect the agreed appearance ownership/economy contract; do not invent free item purchases or direct individual equipment controls. Repeated evaluation with unchanged state must not reshuffle appearance. Test same-seed/save determinism, membership/job changes, neutral affiliation, ties/conflicting preferences and missing/unsupported content. Appearance must not create membership, hostility, romance or mechanical protection. Return inspectable reasons for the chosen combination and representative visuals.
```

## H1 — OR-1104: Implement attributable limb injuries and functional effects

```text
Implement H1 after C1 and OR-1002. Use Docs/ARCHITECTURE.md. Own injury/function/hazard/equipment rules and tests in Domain, plus agreed projection/save integration; reserve population/shared save files before editing. The visual branch may run concurrently only against C1's frozen contract and with separate ownership.

Use real incident/hazard sources to create bounded per-part injury records with cause, tick, severity, pain, function and healing state. Define rates, stacking/caps, capability aggregation and recovery limits against existing ticks/economy/needs before implementing tuning. A leg injury changes relevant travel capability; an arm injury changes relevant manipulation/work. Keep pain/rest/strain contributions attributable and bounded; do not apply indiscriminate productivity penalties.

Protection is an explicit validated equipment rule, not inferred from clothing color or sprite names. Do not introduce random decorative wounds, faction-based injuries or combat. Prevent duplicate consequences from repeated event delivery. Test deterministic incidents, right/left identity, caps, partial/missing/replaced function, relevant-versus-unrelated task effects, save round-trip and old healthy defaults. Publish read-only causes/capabilities for care and presentation. Do not implement H2's care flow or rewrite animation/rendering.
```

## H2 — OR-1105: Implement autonomous care, accessibility and replacements

```text
Implement H2 after H1. Use Docs/ARCHITECTURE.md. Own care/recovery/accessibility/replacement domain rules and meaningful tests, with agreed clinic/transit/persistence integration.

Residents seek care and adapt schedules autonomously. Existing clinic capacity, route accessibility, needs, service availability and funding constrain recovery. Specify bounded healing/pain relief, interruption/resumption, unavailable-care behavior, scars, permanent loss and replacement capability effects. Costs and supplies follow existing conserved economy/aggregate rules; no invisible free treatment or invented replacement stock.

Preserve meaningful accessible alternatives when a resident cannot use a route. The Steward responds through healthcare capacity, infrastructure, workplace conditions and service policy—not direct treatment or limb-equipment orders. Test unavailable/full clinics, inaccessible routes, interruptions, repeat treatment, recovery caps, permanent/replacement states and save/load mid-care. Retain incident provenance and measurable recovery so U1 can explain them. Return the care/capability projection contract; avoid introducing medical micro-management or renderer changes.
```

## U1 — OR-1106: Integrate health, emotion, wardrobe and cause-chain UI

```text
Implement U1 after F1, H2 and A2. Use Docs/UX_AND_GAMEPLAY.md. Own resident inspection/cue UI and health presentation integration; reserve NpcSkeletalHierarchy.cs, ResidentActivityCaption.cs, TowerResidentPresenter.cs and shared projections before edits.

Consume actual read-only thoughts/emotions, injuries, function, care and appearance. Prefer typed cues or a clearly documented compatibility adapter for existing thought strings. A flirt is an encounter, not successful romance; a limp is a projected impairment, not a diagnosis inferred from a pose. Prioritize significant hardship/recovery and social memories while retaining readable actions. Add bounded duration/change policy so crowded cues do not chatter every frame; preserve inspected-resident priority and accessibility.

Use the anatomy/contact/coverage contract for bandages, scars, missing limbs, prosthetics, posture and gait. Clothing can hide a visual wound without hiding inspector information. Complete Symptom → appropriate existing Overlay → Inspector Cause → Systems Response → Measured Feedback. Do not invent a ninth overlay or direct resident commands. Test long text, glyph/tint independence, both facings/poses, hidden injuries, multiple causes, care progress, cue expiry, crowd density, pause/zoom and pooled reuse. Capture integrated images and verify actual click-through explanations.
```

## Q1 — OR-1107: Validate the integrated modular resident system

```text
Validate Q1 after U1 and OR-1007. Use Docs/WORKFLOW_AND_TESTING.md. Own scoped acceptance fixtures/tests and validation evidence; product fixes remain with their assigned package owners or a narrowly agreed integration owner.

Run the integrated demonstration in Planning/RESIDENT_SYSTEM_EXECUTION_PLAN.md, then the reference 30-floor/300-resident Tower. Check old/new saves, missing content, identity/wardrobe/health/care continuity, deterministic same-seed/reload outcomes, pose transitions and contacts, faction/job changes, covered injuries, replacements, inspector cause-chain, fitted accessible bubbles, zoom/picking and pool reuse. Exercise the archive guard against runtime registration and build paths.

Compile/import and run relevant EditMode/PlayMode tests. Attribute pre-existing failures with reproduction and baseline evidence; do not disable tests or silently broaden fixes. Render the supported pose/body/garment combinations and inspect alpha/tint/layering/anchors rather than only asserting field values. Measure p95 tick (<4 ms), presentation (60 FPS), draw calls (<180 in the agreed reference view), allocations/material counts and cache bounds; report hardware, scene, population, view, sampling and limits.

Return one current acceptance report with exact passes/failures, images, migrations, measured budgets and unresolved risks. Do not mark M11 complete while manual preview, engine tests, serialization or performance remain unverified. The coordinator reconciles package results and updates the sole active handoff/backlog.
```
