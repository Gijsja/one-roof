# Game Vision — One Roof

## One sentence

Build one enormous mixed-use tower, then live with the society that forms inside it.

## Format

- **Genre**: Vertical-city builder and emergent society simulation.
- **Presentation**: Controlled 2.5D architectural cutaway with modular 2D characters.
- **Audience**: Players of city builders and colony simulations who enjoy legible cause and effect.
- **Platform**: Premium single-player desktop game built with Unity 6000.3 LTS (URP).
- **Tone**: Optimistic but fragile. A thriving tower feels vibrant; neglect and weak systems produce visible personal hardship without becoming pure misery or cartoon villainy.

## Player Role: The Steward

The player is the building manager, called the **Steward** in the interface. They are accountable for a single inhabited building: its structure, services, leases, policy, capacity, and zone-level priorities. They inspect outcomes, make tradeoffs under pressure, and respond to crises. They cannot directly order an individual resident to perform an action. Control is exercised through systems, never personal micromanagement.

The tonal reference points are *This War of Mine* (human stakes and resource fragility), *Fallout Shelter* (clear cutaway cross-section and satisfying expansion), and *Observer* (close, evocative environmental investigation). One Roof incorporates their presentation clarity and human weight without combat or direct character control: a competent manager can improve lives, and every hard decision has a traceable cause and consequence.

## Core Loop

1. **Build space and infrastructure**: Construct vertical floor slabs, zone rooms, and run power/water risers and stair/elevator transit.
2. **Populate through demand**: Attract residents and commercial tenants through housing quality, service capacity, and economic viability.
3. **Observe daily life**: Watch autonomous residents live, work, commute, eat, and rest across a 1440-tick daily cycle.
4. **Diagnose systemic bottlenecks**: Follow the 5-point cause chain (`Symptom → Overlay → Inspector Cause → Systems Response → Measured Feedback`).
5. **Enact systems-level responses**: Expand transit capacity, retune policy decrees (rent caps, commercial tax, transit subsidies, quiet hours), rebalance utilities, or manage leasing.
6. **Watch society adapt**: Measure resident wellbeing, faction tensions, and treasury solvency.

## Product Pillars

1. **Height is geography**: Elevators are streets, stairs are bypasses, and transfer lobbies are intersections.
2. **Architecture produces society**: Contact, access, cost, and shared grievances shape relationships and factions.
3. **Every major problem is explainable**: The player can trace any failure from visual symptom through data overlays and deep inspectors to a systems-level lever.
4. **Scale without micromanagement**: Blueprints, zoning, leasing, soft priorities, and aggregated simulation let one tower grow into a city.
5. **People remain personal**: Inspectable lives, 5 core needs, personal grievances, and local relationship networks make aggregate systems emotionally legible.
6. **Systems create pressure and relief**: Internal Strain and external Scrutiny rise and fall deterministically with player decisions.
7. **Covert depth (Secret Undercity)**: Beneath Floor 0, an independent 32×12 excavation grid allows covert operations balancing research, outside contract income, supplies, staffing, and exposure under visiting outside investigators.

## Resident Wellbeing & Social Pressure

Residents are autonomous agents governed by three readable time horizons:
- **Needs → Focus** (short-term): Hunger, Energy, Social, Hygiene, and Purpose affect immediate choices and work efficiency.
- **Satisfaction → Grievances** (medium-term): Commute quality, crowding, noise, rent burden, service access, and recent events accumulate into wellbeing and complaints.
- **Strain** (long-term): Personality-filtered cumulative pressure that can lead to move-outs, mental-strain events, or civil action radicalisation.

A compact, high-impact personality facet set (4–8 facets) filters strain and reactions without an opaque trait matrix. Pairwise relationships and shared grievances shape faction affinity.

## External Pressure: Scrutiny

**Scrutiny** represents attention and pressure from the wider city. It rises with rapid expansion, inequality, unresolved crises, and aggressive policy; it falls through balanced policy, service investment, and successful responses. High Scrutiny increases external inspection-event likelihood for crisis systems. It remains abstract and overlay-readable, never an instant expansion veto.

## Prototype Baseline (v0.10) — Delivered

- **Scalable Vertical City**: 30 floors and 300 persistent residents operating at ~108 FPS and 171 draw calls in `Tower_GoldStandard30.unity`.
- **Closed-Loop Economy**: Cash conservation strictly enforced across households, businesses, tower treasury, and the Outside market.
- **Integrated Utilities**: Physical power and water/waste networks connecting surface tower risers to subterranean undercity ports.
- **4 Factions & Policy Decrees**: Tenant Union, Corporate Coalition, Merchant Guild, and Civic Eco Council responding to living conditions and decree tradeoffs.
- **8 Data Overlays + Scrutiny**: Contracted diagnostic overlays with deep cause-chain inspectors.
- **Verified Suite**: 765 automated tests (754 EditMode, 11 PlayMode, 0 failures).

## Beta Boundary (City Status) — Target

- **City Status Streak**: Deterministically sustaining all published City Status gates (wellbeing, service, fiscal, scale, and scrutiny) for 30 consecutive in-game days under <4 ms tick and 60 FPS budgets.
- **6 Adaptive Crisis Chains**: Condition-driven crisis chains with warnings, propagation, management levers, and saved resident/economic consequences.
- **Campaign Board & Pacing**: Inspectable daily trends, threatened gates, failure reasons, and post-campaign sandbox play.
- **Accessibility & Overlay Beta Pass**: Non-colour/text equivalents across all overlays, UI scaling, and keyboard accessibility.

Deferred: multiplayer, multiple towers, full exterior city street simulation, detailed crime/combat, full individual furniture placement, mobile/console platforms.
