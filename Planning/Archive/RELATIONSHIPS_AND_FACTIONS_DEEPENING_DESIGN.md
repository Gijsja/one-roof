# Relationships and factions: deepening design

## Purpose

Make the tower feel like a society that forms through shared space, repeated hardship, mutual aid, and contested priorities. Relationships should help explain how local experience becomes collective pressure. Factions should turn that pressure into legible, geographically grounded action. The Steward changes the conditions that shape these outcomes; they do not command individual residents.

This design deepens the existing M9 system. It does not introduce a second social simulation or a new faction roster.

## Current foundation

`FactionState` already stores a bounded resident relationship graph, affinity, contact cause and tick, per-resident support for four factions, sustained membership, top grievance, and regional floor information. Evaluation is daily and ID-ordered; each resident has at most six edges. Social state is saved, and application projections feed resident/faction inspectors and the faction-tension overlay. Civil actions and the decision record already provide downstream consequences.

The present relationship signal is intentionally small, but needs clearer semantics: presence refreshes ties, faction opposition can lower affinity without a witnessed conflict, and crowded buckets can favor low IDs. Faction support is driven primarily by direct conditions. The design below addresses these gaps before adding more state.

## Desired player-facing loop

**Condition → personal experience → trusted ties → local organizing → faction response → Steward intervention → observed recovery.**

Every step must have an inspectable cause. A resident can support more than one faction, and support is distinct from formal membership. Personal affinity is separate from political alignment: two residents with opposing faction tendencies may remain friends; a damaged relationship needs a repeated, modeled conflict to explain it.

## Design pillars

1. **Ties grow from meaningful repeated contact.** Shared space creates an opportunity to form a tie; it does not automatically create friendship or enmity.
2. **People share issues, not generic ideology.** A resident can learn about a specific rent, service, noise, or workplace grievance through a trusted tie, with weaker influence than direct experience.
3. **Factions are coalitions with different stakes.** Their existing identities and material drivers remain the foundation. Shared hardship changes support, while lived experience and work context determine which issues matter.
4. **Organization is local and conditional.** High grievance alone is not enough for action. A faction needs sustained support and enough locally connected members to act in the affected area.
5. **Relief has a lag, but it works.** Fixing the cause first reduces direct exposure; support and pressure then ease over subsequent settlements. Past response quality can recover trust, while broken promises can damage it.
6. **The Steward reads evidence, not hidden intent.** UI wording distinguishes observed direct exposure from influence through another resident and labels uncertainty.

## Layer 1: resident relationships

Retain the six-edge cap and daily evaluation. Replace presence-as-affinity with three explicit contact outcomes:

- **Encounter:** Residents share a sampled home, work, or transit context. Refresh last-contact and context only; do not move affinity by itself.
- **Shared support:** Both residents have the same named, active grievance or one offers a modeled form of help. Nudge affinity upward by a small bounded amount and record the reason.
- **Repeated conflict:** Their modeled interests conflict in a shared situation (for example, noise burden against a noise-producing business), or their specific grievances clash. Nudge affinity downward only after repeated conflict, with the current conflict named.

Faction identity alone never counts as a conflict. A single encounter or isolated event cannot swing a tie. Affinity remains in [-1, 1]. When meaningful contact stops, affinity slowly returns toward neutral; the edge expires after the existing inactivity window unless it is refreshed. Recent cause and last meaningful contact are persisted. Do not add a full diary or unrestricted social-event stream.

Candidate generation must not privilege residents because their IDs happen to be lower. Within the existing bounded home/work/transit buckets, use a stable seeded rotation or deterministic hash sampling over resident IDs, then stable ordering for updates. Preserve the per-resident degree cap and canonical edge ordering. Contact sampling should be reproducible across save/load and independent of dictionary iteration order.

## Layer 2: issue-specific social influence

Once ties have meaningful causes, let residents be influenced by a small amount of issue-specific information through their strongest current ties. Influence can affect support for the related faction, but is not a new source of objective facts about city conditions.

- Residents transmit only a grievance they currently experience or support, identified by a stable grievance ID (initial candidates: rent/arrears, commute, service reliability, business continuity/margin, tax, noise, shared living strain).
- Compute from the prior settlement's state, so the same-day result cannot cascade through a chain of residents.
- Weight by positive affinity and tie recency; cap the total network contribution to a small fraction of that resident's direct-condition score. Direct material conditions remain dominant.
- A recipient may gain awareness/support but does not inherit another resident's arrears, lost wages, wellbeing, or other factual state.
- Persist only authoritative bounded state; derive neighbor contributions in stable resident/edge order. Expose whether support came mainly from direct exposure or trusted ties, and identify a representative source when one exists.

Use neutral defaults for residents without usable social contacts. Do not let negative affinity create inverse faction support or automatic faction hostility.

## Layer 3: faction alignment and membership

Keep the four stable faction IDs: Tenant Union, Corporate Coalition, Merchant Guild, and Civic & Eco Council. Preserve multiple support values per resident. Each faction should report:

- Support and its trend.
- Membership status, with distinct join and leave thresholds (hysteresis) and sustained-day requirements so a resident does not flip membership on a small daily fluctuation.
- Leading grievance and its contributing drivers.
- Direct versus tie-mediated support, where measurable.
- Local concentration by floor/region and representative residents.

Membership is voluntary, can change, and must never be assigned merely to fill faction counts. Hysteresis values and support smoothing are tuning parameters: choose them with the existing 50-resident fixture, verify timely formation and recovery, and record measured behavior before locking constants.

Faction support must be attributable. If multiple drivers contribute, show the leading driver plus meaningful secondary contributors rather than pretending the maximum alone explains the outcome. An unavailable source remains explicitly unavailable.

## Layer 4: organization, legitimacy, and action

Separate three concepts that a single pressure meter currently risks conflating:

- **Grievance pressure:** prevalence and severity of unresolved named issues among supporters.
- **Organizing capacity:** sustained members and the strength of positive ties connecting them in the affected floors/regions.
- **Steward trust:** whether previous responses addressed the named cause and held over time; improve it only from observed outcomes, and reduce it for reversals or repeated unmet commitments.

Keep the existing civil action state machine and action effects. Add local capacity as an eligibility/readiness gate, not as an arbitrary multiplier. A warning should state which grievance is unresolved and what capacity is building; an action begins only when both pressure and connected local membership meet a sustained threshold. Scrutiny may still modify eligible severity but cannot create organization. Recovery depends on the cause improving and is measured over daily settlements. Action onset, affected IDs/regions, and effects remain saved exactly once.

Trust should inform how quickly a faction believes a response is durable, not suppress legitimate grievances or punish players for having crises. A practical first release may ship pressure + local capacity and defer trust until the decision record contains enough reliable response history to compute it honestly.

## Faction identities and tensions

Keep faction differences grounded in the current simulation:

| Faction | Primary stakes | Likely coalitions or tensions |
| --- | --- | --- |
| Tenant Union | Rent burden, arrears, habitability, commute | May align with Civic & Eco Council on shared services; may contest rent extraction or access priorities |
| Corporate Coalition | Office continuity, reliable utilities and transit, commercial operating conditions | May align with Merchant Guild on continuity; may contest resource allocation with residential groups |
| Merchant Guild | Margin, foot traffic, tax, operating noise/tradeoffs | May align with Corporate Coalition on continuity; may contest quiet/access tradeoffs with Civic & Eco Council |
| Civic & Eco Council | Shared utilities, waste, noise, common living conditions | May align with Tenant Union on habitability; may contest noisy commercial activity or unequal service priorities |

These are potential issue-based coalitions, not fixed enemies. Display a faction-to-faction relationship only when shared or conflicting modeled drivers support it. Do not hard-code personal animosity from faction pairings.

## Player experience and feedback

### Resident inspector

Show a compact list of notable ties: counterpart, affinity band and direction of change, last meaningful contact, and named cause. Show current faction support and membership separately, including the leading direct driver and any tie-mediated influence. Clicking a counterpart opens their inspector. Do not present stale broad contact context as a recent personal event.

### Faction inspector and tension overlay

Show pressure and trend, top grievances and secondary contributors, supporter/member counts, local concentration, organizing capacity/readiness, representative residents, and whether support is direct or socially reinforced. Empty regions remain explicit no-data. Every summary should link to the residents and conditions behind it.

### Decrees and incidents

Policy drafts describe expected faction direction and its drivers as estimates. Receipts report observed support, pressure, and grievance changes after settlement. Civil action warnings and incident records connect grievance → affected region → organizing threshold → modeled effect → valid systems-level recovery lever. Never claim a tie or faction reaction caused an outcome unless the domain recorded that causal contribution.

## Implementation sequence

### Phase A — make current relationships credible

Clarify meaningful-contact semantics, remove automatic opposition-based tie loss, add affinity return toward neutral, and eliminate low-ID crowding bias. Expand resident projections/inspectors to expose existing edges accurately. No new social influence or new action thresholds yet.

### Phase B — let trusted ties carry named issues

Add stable grievance IDs and prior-day, bounded tie-mediated support. Show direct versus socially reinforced contributions in inspectors and faction projections. Validate that relief reverses support and that no-contact populations match the prior baseline.

### Phase C — local organizing and action readiness

Project regional membership connectivity/capacity and use it as an explainable sustained civil-action gate. Add faction-to-faction coalitions as derived, issue-based projections only where supported. Consider Steward trust only after response history can support a deterministic, testable definition.

Do not bundle all phases into one feature. Phase A can prove relationship quality before network spillover or action balance changes are introduced.

## Domain and data constraints

- Keep authoritative state in pure C# domain aggregates; zero UnityEngine references.
- Retain stable IDs, daily cadence, deterministic ordering, bounded storage, and the 300-resident/<4 ms budget.
- Projections are immutable and bind by IDs; views never write social state.
- Version social save data for any new authoritative fields. Older saves receive neutral defaults; validate live IDs, clamp values, enforce edge caps in stable order, and rebuild derived indexes after load.
- No per-frame graph updates, all-pairs scans, unbounded memories, individual-player orders, or plugin-owned duplicate state.
- Keep new parameters and faction content IDs stable; display-name changes must not change saved identities.

## Acceptance criteria

1. Replaying the same seed, commands, and save/load checkpoints yields byte-for-byte equivalent social state and the same projections.
2. Repeated meaningful shared support strengthens a tie; mere co-presence does not. Repeated modeled conflict can weaken it, while faction disagreement alone cannot.
3. Dense-room contact sampling is bounded, fair across resident IDs, stable across runs, and respects the six-edge cap.
4. A supported grievance can spread through a strong recent tie only as a named, capped contribution; the recipient retains distinct personal condition values.
5. A fixture demonstrates distinct faction support, voluntary membership transition, response-led recovery, and a civil action requiring both sustained pressure and local capacity.
6. Inspectors and overlays explain direct exposure versus social influence, named causes, trends, regions, representative residents, and action readiness without asserting unmodeled facts.
7. Current saves migrate with neutral defaults; save/load during a relationship shift and each action phase does not replay or duplicate effects.
8. 50-resident calibration and 300-resident profiling demonstrate expected formation/recovery behavior and preserve tick/presentation budgets.

## Out of scope

Romance simulation, full personality matrices, dialogue/deed authoring tools, rumor trees, arbitrary faction proliferation, tactical social micromanagement, and a second relationship plugin runtime. These could be reconsidered only if the core loop later needs them and the domain model can support them without hiding causal feedback.
