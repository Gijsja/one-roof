# M9 — Factions and Management: execution brief

Status: IMPLEMENTED. OR-901–905 and the seeded integrated proof pass in an isolated Unity worktree (547/547 EditMode, 8/8 PlayMode). Reference-hardware tick profiling and final spatial visual QA remain Beta exit checks under OR-1003/OR-1006. The sections below remain the implementation contract for follow-up work.

## Product outcome and boundaries

The Steward can see how shared living and working conditions organize residents, compare the cost and likely beneficiaries of a decree, enact it, and observe both economic and social consequences. A severe, sustained grievance can produce a legible civil action with a systems-level remedy. Residents remain autonomous; no command recruits, expels, silences, or directs an individual. Use an optimistic but candid tone about hardship.

Keep Domain pure C# and saveable, with presentation reading immutable projections by stable IDs. Reuse `PolicyDecreeState`, `SetPolicyDecreeCommand`, `TowerSimulation.CanExecute`/`ExecuteCommand`, daily settlement, `ScrutinyState`, existing wellbeing/arrears data, mode shell, and inspector patterns. No second policy store, view state in saves, or per-frame all-pairs resident search. Four policy fields exist today: rent multiplier 0.7/1.0/1.3, commercial tax 0/10/20%, transit subsidy off/on ($40/day), quiet hours off/on. Express elevator lanes and green public spaces need separate mechanics and are not implied by this slice. The four faction display names below are canonical for M9; stable content IDs must survive copy changes.

## OR-901 — Social graph and faction state

1. Define a bounded relationship edge between two resident IDs, with affinity, last meaningful contact tick, and cause. Generate or refresh edges from shared workplace, nearby home, and actual co-presence in transit. Use a fixed per-resident edge cap and stable tie-breaking by ID; avoid quadratic scans each tick. Rivalry may arise from repeated conflicting conditions, never from an unexplained roll. Remove or expire edges when a resident leaves and rebuild derived indexes after load.
2. Define four faction records with stable IDs, member/support IDs, approval or pressure, top grievances, and influence by floor or region. Tenant Union responds to rent, arrears, commute, and habitability; Corporate Coalition to office continuity, power, transit, and tax; Merchant Guild to margin, foot traffic, noise tradeoffs, and tax; Civic & Eco Council to waste, utility reliability, noise, and shared living conditions. Use only signals that already exist; show an unavailable driver as unavailable until its source exists. A resident's affiliation follows household/work context and sustained shared grievances. Membership and support are distinguishable, can change, and remain attributable to named drivers. No forced universal membership.
3. Evaluate social changes on a bounded cadence (prefer daily settlement for faction aggregates); process residents/edges in stable ID order. Persist authoritative affiliation, edges, and pressure with versioned defaults for older saves. Project faction summary, member counts, top contributors, region influence, trend, and representative stable resident IDs. Inspector drill-down must show why a resident supports a faction and what changed that support.

**Acceptance:** fixed 50-resident fixture creates at least two distinct factions with different responses to the same condition; grievance relief reverses pressure over time; same seed and commands match across two runs and a mid-run save/load; 300-resident fixture respects the edge cap and tick budget. Record chosen constants and calibration fixture in the handoff, and add an ADR if the state ownership or cadence changes.

## OR-902 — Manage mode and decree effects

1. Add a Manage panel reachable by hotkey `4` with four controls mapped exactly to `PolicyDecreeState`. A draft is separate from enacted state. Show current setting, proposed setting, next daily cash effect or a clearly labeled estimate, which households/businesses benefit or bear cost, expected faction direction with its drivers, and any scrutiny implication. Unknown or delayed effects are labeled, not presented as exact forecasts. Empty/no-op submission cannot manufacture events or repeated scrutiny.
2. Confirm through `SetPolicyDecreeCommand`; call `CanExecute` for validity and show machine-readable rejection reasons in plain language. The domain command remains the only mutation path. Any new faction approval effect must be evaluated from effective policy and observed conditions on a specified cadence, with a bounded response and recovery; it must not stack indefinitely on repeated panel clicks. Existing daily economic and wellbeing effects remain the authority for money and satisfaction.
3. After enactment, show a dated receipt with changed settings and immediate effects; after settlement, show actual treasury, household/business, faction, satisfaction/strain, and scrutiny deltas where measurable. Link from a faction response to its inspector cause and a valid systems-level lever. Preserve keyboard navigation, focus, and non-colour status text.

**Acceptance:** all legal options round-trip through save/load; panel draft does not mutate simulation; command rejection and no-op behavior are tested; a rent reduction improves a burdened household while reducing expected rent receipts, and a tax increase shows merchant cost and treasury benefit under a staffed business fixture. Verify UI input and receipt in PlayMode.

## OR-903 — Acoustic and faction overlays; civil action

1. Implement `overlay:noise` from actual room/activity sources and spatial attenuation. Show acoustic contours plus labels/glyphs, sources, affected rooms, and the quiet-hours tradeoff. Do not claim physical propagation beyond the model's granularity. Link a hot region to an inspector cause.
2. Implement `overlay:faction_tension` from faction pressure and member/support location. Show regional contours plus glyphs and text, top contributing grievances, trend, and the threshold toward action. Empty areas have an explicit no-data state. Selection opens the faction or affected floor inspector.
3. Civil actions are deterministic state machines with warning, active, recovery, and cooldown. Rent strike, lobby protest, and work slowdown each require a relevant faction, sustained grievance/pressure, and a defined capacity or economic effect. Scrutiny modulates eligibility or severity but cannot create an action by itself. Record onset causes, affected IDs/regions, expiry/recovery condition, and actual downstream effects. Save and resume an active action without replaying onset effects; prevent duplicate stacking. Provide at least one policy or capacity response for each action.

**Acceptance:** fixture reaches warning and action only under documented conditions; intervention produces recovery; high scrutiny alone produces no protest; save/load at every phase preserves timing and effects; both overlays have non-colour and inspector routes. Capture a PlayMode visual check for each overlay.

## OR-904 — Decision record and consequences

Record one dated entry per effective decree change and major civil action, with stable event ID, old/new settings or action phase, reason, affected entity/region IDs, immediate cash/pressure effects, and bounded follow-up observations at later daily settlements. Use source-of-truth values from domain projections; do not maintain a parallel narrative simulation. Retain a bounded history with documented eviction order, preserving active incidents. The record links to household, business, faction, and floor inspectors, and handles a moved-out entity as a historical snapshot without dereferencing a missing live record.

**Acceptance:** a decree appears once, later observed deltas are distinguishable from predicted ones, records and links survive save/load, and a representative resident or business can be traced from decision to measured outcome. Copy names hardship and recovery plainly.

## OR-905 — Management onboarding

Use the existing five-floor congestion fixture. Guide queue symptom → elevator overlay → cause inspector → capacity preview → build response → measured wait improvement. Then introduce one rent or utility tradeoff and open Manage without requiring the player to enact a harmful decree. Progress is based on actual actions and metrics, not clicks on instruction text. The lesson is skippable, restartable, and completable by keyboard; contextual help remains after skip. Preserve the existing golden first-playable behavior.

**Acceptance:** a new session completes the full loop with measured improvement; skip and resume do not mutate economy or social state; keyboard path and text-only explanation are verified in PlayMode.

## Integrated proof and handoff requirements

Use a seeded scenario with occupied homes, a staffed business, transit pressure, utilities, and at least two factions. Run a neutral baseline, enact a rent or tax change, advance through daily settlement, inspect opposing faction reactions, induce and recover from one civil action, and compare household/business cash, satisfaction/strain, scrutiny, and faction pressure. Repeat from the same seed and across a mid-incident save/load. Record exact seed, commands, tick checkpoints, expected direction and measured values, and profiler evidence for 300 residents under the `<4 ms` tick budget. Require the relevant pure domain tests, EditMode projection/UI tests, and PlayMode interaction/visual checks. Use the isolated headless workflow in `Docs/10_DEVELOPMENT_WORKFLOW.md`; record exact commands, exits, test totals, baseline-attributed failures, and screenshots/profiler paths in the active handoff. Documentation-only preparation needs no Unity run.

Before an agent starts a slice, inspect the current active handoffs and shared worktree for concurrent edits. Scope edits to the owned slice, preserve unrelated changes, and archive the slice handoff only when its acceptance evidence is complete. M9 is complete only when OR-901–905 and the integrated proof pass.
