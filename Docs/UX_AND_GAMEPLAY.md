# UX, Gameplay & Cause-Chain Contract — One Roof (v0.10+)

This document is the authoritative design and UX specification for One Roof's core loop, player role, mode shell, diagnostic data overlays, and cause-chain inspectors.

---

## 1. Product Vision & Player Role

### One Sentence
Build one enormous mixed-use tower, then live with the society that forms inside it.

- **Genre**: Vertical-city builder and emergent society simulation.
- **Presentation**: Controlled 2.5D architectural cutaway with modular 2D characters.
- **Platform**: Single-player desktop built with Unity 6000.3 LTS (Universal Render Pipeline).
- **Tone**: Optimistic but fragile. A thriving tower feels vibrant; neglect produces visible personal hardship without cartoon villainy.

### The Steward Role: Systems over Individuals
The player is the **Steward**: building developer, mayor, and systems designer. The Steward controls architecture, zoning, utilities, leases, capacities, and policy decrees. The Steward **never** directly commands an individual resident. Control is exercised through systems; autonomous residents respond.

### The Core Loop
1. **Build structure & infrastructure**: Construct floor slabs, zone rooms, run power/water conduits, and erect stairs/elevators.
2. **Populate through demand**: Attract residents and commercial tenants through housing quality, amenities, and viability.
3. **Observe daily life**: Watch autonomous residents commute, work, eat, rest, and socialize across a 1440-tick daily cycle.
4. **Diagnose bottlenecks**: Follow the 5-point cause chain (`Symptom → Overlay → Inspector Cause → Systems Response → Measured Feedback`).
5. **Enact systems-level responses**: Expand transit, retune policy decrees, rebalance utilities, or alter leasing terms.
6. **Watch society adapt**: Track resident wellbeing, faction tensions, and treasury solvency.

---

## 2. Mode Shell & Primary Verbs

The player interacts through a unified mode shell bar (`ModeShellBarController`) styled with `StewardTheme`:

| Mode | Hotkey | Responsibilities |
|---|---|---|
| **Build** | `1` | Structure, rooms, transit, utilities, services; categorized palette (`Space & Use`, `Services`, `Utilities`, `Movement & Removal`). |
| **Inspect** | `2` | Detailed drill-down into residents, households, businesses, factions, rooms, floors, and systems via deep cards. |
| **Data** | `3` | Read the tower via 8 specialized diagnostic overlays displaying flows, intensity, pressure, and risk. |
| **Manage** | `4` | Treasury, policy decrees (rent caps, commercial tax, transit subsidies, quiet hours), tenant leases, staff roles. |

### Global View Controls
- **Camera navigation (`WASD` / arrows)**: Pans the Tower view; `D` does not select Data mode (use `3`).
- **Weather preview (`F6`)**: Cycles the existing manual weather override and Auto mode without moving the camera.
- **Facade Envelope Lock (`F`)**: Toggles visible presentation between CUTAWAY interior view and OUTSIDE curtain wall envelope, with manual locks taking precedence over camera-driven semantic zoom.
- **Overview Frame (`Home`)**: Reframes the camera to view the full tower structure.

---

## 3. The Cause-Chain Explanation Contract

Every important failure or crisis in the tower must support the 5-point explanation chain:

```text
World symptom → Data overlay → Inspector cause → Systems-level response → Measured feedback
```

The inspector is an explainable cause chain, never a flat stat dump or dead end. It exposes:
1. **Symptom**: Plain-language description of current hardship (e.g. "Long elevator commute").
2. **Data & Contributors**: Immediate factors driving the symptom (wait time, crowd density, rent burden).
3. **Root Cause**: The systemic source (e.g. lack of elevator capacity on Floor 12, missing power riser).
4. **Contextual Levers**: Direct buttons navigating to the relevant build, lease, or decree action.
5. **Measured Feedback**: Observable changes in subsequent ticks/settlements confirming whether the intervention worked.

---

## 4. The 8 Diagnostic Overlays (+ Scrutiny)

| Overlay ID | Primary Visual Channel | Data Source | Corresponding Cause / Risk |
|---|---|---|---|
| `overlay:elevator_wait` | Animated flow paths & queue bars | `ElevatorBankCongestionProjection` | Shaft capacity shortage, landing bottlenecks. |
| `overlay:foot_traffic` | Directional vector paths | `HierarchicalTransitGraph` | Corridor choke-points, stairwell demand. |
| `overlay:population` | Density gradient & demographic glyphs | `PopulationState` | Overcrowding, demographic segregation. |
| `overlay:satisfaction` | Soft regional glow + pattern glyphs | `SatisfactionService` | Commute friction, need deprivation, rent burden. |
| `overlay:noise` | Room acoustic contours + decibel text | `NoiseOverlayProjector` | Active machinery, commercial noise vs residential peace. |
| `overlay:business_health` | Solvency badges (green / amber / red) | `BusinessAccountingSystem` | Foot traffic deficit, excessive commercial rent. |
| `overlay:faction_tension` | Regional tension contours + glyphs | `FactionState` | Policy grievances, ideological polarization, strike risk. |
| `overlay:utilities` | Pipe/cable flow & pressure lines | `UtilityNetworkGraph` | Overloaded transformers, broken water lines. |

*Presentation Rule*: Do not render every overlay as a heatmap. Use animated paths for flow, soft regional glows for influence, discrete badges for financial solvency, and network lines for physical utilities.

---

## 5. Cause-Chain Inspector Specifications

### Resident Inspector
- **Header**: Identity, household, workplace links, plus current Focus, Satisfaction, and Strain.
- **Active State**: Primary symptom, current schedule block, active thoughts with mood deltas, active grievances.
- **Cause Breakdown**: 5 core needs, commute friction, crowding, rent burden, social traits, and relationship ties.
- **Systems Levers**: Contextual shortcuts to zoning, transit additions, decree adjustments, or training capacity.

### Room & Systems Inspectors
- **Room**: Occupancy, noise generation, utility feed status, operating margins, affected residents, and zoning levers.
- **Transit**: Wait times, passenger delivery rate, elevator car positions, queue overflow, and widening/tier upgrade levers.
- **Utilities**: Voltage delivery, water pressure, connected source, equipment degradation, and repair status.
- **Scrutiny**: Current value, trend, contributing factors (speed of expansion, unresolved crises, aggressive decrees), and external inspection risk. Scrutiny never creates an instant build veto.

---

## 6. Placement Previews & Undercity Excavation

### Placement Ghost Preview
`PlacementGhostPresenter` queries domain validity via `TowerSimulation.CanExecute(ICommand)`:
- Renders animated holographic scanlines with dynamic outline glow (green for valid, amber/red for invalid).
- Displays placement footprint, monetary cost, utility reachability, and predicted consequences (`PlacementPreviewCardView`).

### Subterranean Excavation UX
- **Independent Earth Board**: 32×12 subterranean grid (1m pitch) beneath Floor 0.
- **Brushes**: 1×1, 2×2, and 3×3 excavation and lair-floor brushes.
- **Visuals**: Subdued stratified earth, recessed cavities, continuous floor tiles, and non-colour status icons for covert staffing, supplies, and visiting outside investigators.

---

## 7. Accessibility Baseline

- **Multi-Channel Encoding**: Every overlay state, alert, and economic indicator pairs color with glyphs, shapes, or text.
- **Input Controls**: Keyboard accessible speed controls (pause, 1x, 2x, 3x) and hotkeys (`1`–`4`, `F`, `Home`).
- **Text & Audio Independence**: Critical events provide plain-language text explanations independent of audio cues.


## 8. Resident Thought, Action & Emotion Bubbles

Resident bubbles are observation cues derived from immutable transit/activity projections and the latest active thought memory. They never create social outcomes or change resident decisions.

- **Action**: A rounded warm-white card with a small pointer shows the current routine or journey (sleep, work, food, leisure, destination, lift queue).
- **Thought**: A cool-white card with thought dots shows a remembered encounter. Connected, flirty, embarrassed, angry and impatient cues combine a distinct glyph, short text and a subtle tint. Flirting describes the encounter, without claiming a successful romance.
- **Priority**: Long lift waits (30 ticks) precede social memories; sleep retains its action. Arguments precede awkward encounters, then positive social memories, then other thoughts and routine actions.
- **Typography**: Two fitted lines inside a fixed compact surface. Unknown thoughts are bounded to 38 characters at a word boundary and receive neutral styling. The inspector retains the detailed cause context.
- **Crowds & zoom**: Bubbles stay upright and unmirrored during walking, facing and sleeping. A centralized screen-space pass favors the inspected resident, then emotion priority, then stable resident ID. It suppresses overlaps, viewport clipping and excess cards (default 12, 6-pixel spacing). Routine bubbles disappear beyond the close skeletal zoom range; the inspected resident remains eligible.
- **Authoring**: `ResidentPresentationSettings.MaximumBubbles` and `BubbleSpacingPixels` control density. Zero maximum hides all bubbles. Views and selection buffers are reused; there are no per-resident Update loops.

The bubble is the world symptom step of the cause-chain. Inspecting the resident reveals the underlying thought, activity, needs, grievances and available systems responses.
