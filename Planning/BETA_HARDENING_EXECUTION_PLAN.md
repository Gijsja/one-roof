# Beta Hardening — Agent Execution Plan

Date: 2026-10-01. Planning and assignment definitions only; no agents or runtime implementation are started by this document. [ROADMAP](./ROADMAP.md) owns direction and scope; [BACKLOG](./BACKLOG.md) owns eligibility/status. This packet owns execution contracts. Do not duplicate detailed rules in all three files.

## 1. Start from existing evidence

The six-chain gap assessment found reusable machinery, not six finished chains:

- [CivilActionState](../Assets/OneRoof/Runtime/Domain/CivilAction/CivilActionState.cs) has RentStrike, LobbyProtest and WorkSlowdown; saved phases and onset participants; global rent/lobby/output multipliers; automatic recovery after a bounded active duration. [Civil-action tests](../Assets/OneRoof/Tests/EditMode/Domain/CivilAction/CivilActionStateTests.cs) prove parts of this lifecycle. They do not establish all six named crisis chains.
- [ElectricalGridState](../Assets/OneRoof/Runtime/Domain/Infrastructure/ElectricalGridState.cs), [WaterWasteNetworkState](../Assets/OneRoof/Runtime/Domain/Infrastructure/WaterWasteNetworkState.cs) and [UtilityOperationsState](../Assets/OneRoof/Runtime/Domain/Infrastructure/UtilityOperationsState.cs) provide physical connectivity, capacity, failure/wear and repair. Water pressure failure is not contamination.
- Transit routing/congestion and elevator upgrades already have measurable interventions. Reuse their metrics and topology rather than creating a parallel transit model.
- [ScrutinyState](../Assets/OneRoof/Runtime/Domain/Scrutiny/ScrutinyState.cs) exposes ExternalEventPressure; inspected domain usages do not consume that property. [UndergroundOperationsState](../Assets/OneRoof/Runtime/Domain/Underground/UndergroundOperationsState.cs) has an exposure-driven investigator, saved route/progress and timed room disruption/expense. Reconcile these mechanisms; do not describe them as absent or already a complete crackdown.
- [TowerSimulation](../Assets/OneRoof/Runtime/Domain/TowerSimulation.cs) settles civil/Caretaker phases and serializes their aggregates; [DecisionRecordState](../Assets/OneRoof/Runtime/Domain/Decisions/DecisionRecordState.cs) records civil onset/phase history. [TowerInspectionService](../Assets/OneRoof/Runtime/Application/Inspectors/TowerInspectionService.cs) has utility/scrutiny explanations, but the six-chain explanation path is incomplete.
- Caretaker Slice A is delivered separately. Keep its save provenance, practice isolation, paused recovery and autonomous household departure. It is not a substitute for Rent Boycott or any other named chain.

These are dated source observations. Confirm current implementation and tests before treating any observation as a live blocker.

## 2. Common definition of done

Each chain receives a one-page specification and an evidence row for every requirement below. Reuse existing aggregates; common vocabulary does not mandate a generic crisis manager or a rewrite of CivilActionState.

| Contract | Required definition / evidence |
|---|---|
| Identity and ownership | Stable chain/incident IDs, aggregate owner, supported version, fixed-tick or finalized-settlement evaluation, deterministic ordering and idempotence. |
| Eligibility and warning | Actual measured conditions; exact units, thresholds, sustained duration, minimum scope/participation and clear explanation. An isolated sample or scrutiny alone cannot bypass documented eligibility. |
| Escalation and propagation | Bounded transition rules, affected stable entity/floor/network IDs, attribution and adjacency/participation rules. Specify whether scope evolves or is intentionally fixed; global effects need an explicit justification. |
| Responses and agency | Existing or narrow new validated systems commands: infrastructure/capacity, leases, service policy or decrees. Residents remain autonomous; no individual orders. |
| Recovery versus containment | Cause-driven recovery with measured improvement/hysteresis; separately name any timed expiry or containment. Timers must not imply the original cause was solved. Persistent pressure has a documented re-entry/cooldown rule. |
| Consequences | Bounded, attributable saved effects on appropriate existing budget/business/housing/wellbeing state and history. Define onset/application/expiry and once-only accounting; avoid punitive effects without a stated rule. M11 limb injury is not required for beta. |
| Explanation | Symptom → one of the eight overlays → inspector cause/scope/timing → actionable systems lever → before/after feedback. Include text/glyph equivalents, not colour-only status. |
| Persistence and topology change | Save/load at each phase, old-save defaults, invalid/missing IDs, demolition/disconnected paths, paused/recovered state where relevant, no duplicate charges/consequences. |
| Composition and budgets | Overlap order, non-stacking/caps and money conservation; bounded buffers/history; no per-Resident Update loops or unsaved RNG. Measure affected scale paths. |
| Evidence | Negative eligibility, warning withdrawal, active onset/propagation, ineffective intervention, effective intervention and aftermath; uninterrupted versus saved/resumed continuation; Application/UI wiring and representative PlayMode acceptance. |

C0 chooses and documents numeric tuning with fixtures and rationale; inherit existing rates/caps where compatible. Mark new values as chosen design, not pre-existing requirements. No TBD parameter may silently become an accepting default. Ordinary bounded design choices can be resolved by the assigned owner within these constraints; record the decision rather than repeatedly asking for confirmation.

## 3. Chain specifications to close

| Package | Chain | Required concrete choices and demonstration |
|---|---|---|
| OR-1002-C1 | Rent Boycott | Map RentStrike to this named contract; define household participation, rent withholding/arrears/conservation, propagation and settlement timing. Show lease/policy relief versus unchanged pressure, scoped collections and persisted recovery/aftermath. |
| OR-1002-C2 | Labor Strike | Define WorkSlowdown's relationship to escalation into strike; affected workplaces/Residents, service/output losses, participation spread and service/commute/policy relief. Show no unrelated workplace penalties unless the contract explicitly defines them. |
| OR-1002-I1 | Power Cascade | Specify sustained overload/wear/connectivity onset and how failures propagate along actual grid dependencies; bound secondary effects. Show capacity/maintenance/connected backup intervention and restoration with saved residual consequences. |
| OR-1002-I2 | Transit Collapse | Specify sustained waits/queues/unreachable routes and affected landings/routes; separate ordinary commute pressure from crisis. Show capacity/layout intervention, relief under ongoing demand and saved disruption/aftermath. |
| OR-1002-I3 | Water Contamination | Define actual contamination source, network exposure and source-removal/containment/clearance rules. Reuse water/waste paths and existing service/wellbeing consequences. Distinguish unavailable water, low pressure and unsafe water in projections/tests. |
| OR-1002-E1 | Building Crackdown | Decide how scrutiny/event pressure and undercity exposure/investigators relate without double-triggering. Define eligibility, warning, explicit scoped inspection/disruption, costs/expiry, systems response and history. Keep expansion available except for a specifically defined event constraint with cause/expiry. |

OR-1002-U is repeated integration work for each delivered chain, then an all-six closure check. OR-1002-X proves individual chains plus overlaps, relief competition, unchanged adverse conditions, save/resume and demolition. A chain is not DONE because its enum, warning UI or unit tests exist.

## 4. Startable hardening lanes and final gates

### C0 — Contracts and fixture matrix (READY)

Inventory current owners, define all common/chain contracts and exact City Status gate metrics, publish a pass/fail/not-run matrix and narrow API boundaries. Use a small deterministic fixture per chain, then compose at reference scale. Set a bounded history/version policy and incident ordering. Exit: downstream agents have concrete inputs and no ambiguous accepted defaults.

### P — Performance and runtime stability (READY)

Reproduce native overview, close Resident view and Caretaker/HUD on recorded hardware/resolution/settings, with simulation tick p95, allocations, frame timing and draws. Investigate the current 1080p ~36-FPS result separately from the historical ~100-FPS overview. Prioritize measured bottlenecks; preserve picking, pool/LOD identity and overlay fidelity. Record before/after and reference view. Explicit Camera.Render/720p data is diagnostic, not full-HUD 1080p acceptance.

### 1005-A/B — Campaign and City Status

A can start now: inventory current metrics, define finalized-day eligibility/trends/streak and failure reasons, and implement a deterministic evaluation model. Reconcile with C0; missing chain data is explicitly unavailable, never healthy by default. B completes board/pacing/end states after real chain projections arrive. Save streak and relevant gate history; loading cannot grant a day or erase a failure. Sandbox continuation is explicit. Final 30-day certification waits for all inputs, not initial model development.

### 1006-A/U — Explanation, accessibility and input

Inventory all eight overlays and currently delivered input paths now. Finish spatial/text/glyph equivalence, scale, remappable controls, W/D conflicts, keyboard/gamepad navigation, pause/speed and exclusive modal behavior. U integrates actual chain phase, scope, driver, response and feedback as each projection arrives. Do not add a ninth overlay or render fake active chains to complete a checklist.

### 1007-A/R — Persistence and recovery

A can baseline old/corrupt/partial saves and build replay/topology-deletion fixtures before every chain exists. Add contract tests incrementally with each owner. R closes every chain phase, campaign state, overlaps, missing content/entity references and partial demolition with actual replay comparisons and idempotent consequences. Snapshot equality alone does not prove continuation determinism.

### 1003-G — Integrated release gate (BLOCKED until inputs pass)

After X, B, A-accessibility, R and P close: run the actual 30-floor / 300-Resident Tower for 30 consecutive eligible in-game days; publish daily gate/streak/failure data, replay evidence and all budgets on the agreed reference views. Do not weaken thresholds, suppress errors or certify native presentation from headless functional tests. Return Ready, Ready with explicit non-blocking limitations, Failed or Blocked with exact evidence.

## 5. File ownership and dispatch rules

1. Read AGENTS.md, BACKLOG and the single active handoff; choose only the canonical doc relevant to the assignment. Apply relevant Unity skills. Record commit/tree, existing changes, test/Console baseline and active Editor/lock.
2. Assign exactly one package and exclusive write paths to one owner. The coordinator owns shared integration: TowerSimulation.cs, shared save roots, command dispatch, common projection/inspection/overlay APIs and any shared CivilActionState edits. Extend the existing state owners rather than duplicating them.
3. After C0, civil-chain agents can work on separate tests/adapters, but C1/C2 must not edit CivilActionState concurrently. Likewise grid/water utility owners coordinate shared utility/save paths; UI agents wait for real API contracts. Declare dependencies at interfaces, not entire parent milestones.
4. Parallel agent work requires human authorization and disjoint ownership. Prompt availability is not dispatch. With four slots, use one coordinator and at most three workers; one worker can independently review/validate while other two implement. Do not keep all three editing shared aggregates.
5. One coordinator schedules Unity runs; no concurrent Editor/test runners against the same project. Use live unity-cli or a correctly isolated snapshot when locked. A HEAD worktree alone does not validate uncommitted work: include the exact scoped changes under review. See [WORKFLOW_AND_TESTING](../Docs/WORKFLOW_AND_TESTING.md).
6. Test prerequisite existence before asserting consequences. Use deterministic input, real production observations and named parameter cases; preserve preview/execution/migration/replay distinctions. Scene-owning PlayMode fixtures use owned worlds and awaited failure-safe cleanup; captures isolate unrelated motion.
7. New Unity assets have paired metas. Preserve unrelated edits; no implicit commits, package updates, scene rewrites, art expansion or target-setting changes. Agents do not create competing active handoffs.
8. Each owner reports a concrete blocker with missing input, current evidence and next experiment. Recheck inherited blockers at assignment start; retire resolved ones with evidence. Keep environment limits separate from product failure and intentional scope gates.
9. The coordinator integrates, cross-reviews actual outputs, runs targeted then required broad checks, and updates status/handoff. DONE requires the package evidence matrix; never infer parent completion from passing child tests.

### Reusable assignment definition

> Work on **[package ID / outcome]** from Planning/BETA_HARDENING_EXECUTION_PLAN.md. Confirm **[dependency inputs]** and record the starting tree/baseline. You own **[exclusive paths]**; the coordinator owns **[shared paths]**. Deliver **[specific behavior + relevant contract rows]**, reusing **[existing aggregates/APIs]**. Keep **[explicit scope exclusions]**. Validate with **[targeted tests, integration scenario, replay/visual/performance evidence as applicable]**; coordinate Unity execution. Report changed files, decisions/tuning, pass/fail/not-run evidence, remaining risks and next eligible package. Preserve unrelated changes and paired metas; do not update parent status or create another active handoff.

### Suggested dispatch batches after authorization

- **Start now:** coordinator C0; workers P, campaign-A and accessibility-A on exclusive files. Persistence-A may replace one lane when save evidence is the stronger blocker. This is a suggested sequence, not a current dispatch.
- **After C0:** finish C1 first as a complete experience; C2 reuses its agreed civil APIs through serialized integration. Separate power/transit investigations or persistence/UI integration can run alongside it.
- **Next:** power and transit implementation on separate owners; one independent reviewer verifies real propagation, ineffective/effective interventions and resume behavior. Water and crackdown specification work can begin once C0 supplies bounds.
- **Then:** complete water and crackdown, integrate U/B/R continuously, close X and the final 1003-G gate. Do not wait until all six are implemented to discover explanation, persistence or frame-budget failures.

## 6. Completion report and blocker template

Every package report includes: package ID; before/after behavior; owned/shared paths; chosen rules and units; exact scenarios and results; save/replay evidence; measured costs where affected; and unresolved limits. Store reviewable evidence under a durable validation/evidence path before final certification; `/tmp` logs are useful working artifacts but not permanent release proof.

For a blocker record: **package → missing input/observed failure → dated evidence → impact → next discriminating action → exit condition**. Specify whether work can continue on an independent sub-slice. No delivery date is assumed. This packet does not itself implement or validate any package.
