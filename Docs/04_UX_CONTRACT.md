# UX Contract

## Primary verbs & Mode Shell

The player interacts through a unified mode shell bar (`ModeShellBarController`) styled with `StewardTheme`:

| Mode | Hotkey | Responsibility |
| --- | --- | --- |
| **Build** | `1` | Structure, rooms, transit, utilities, services; categorized palette (`Space & Use`, `Services`, `Utilities`, `Movement & Removal`) |
| **Inspect** | `2` | Detailed drill-down into people, households, businesses, factions, rooms, floors, and systems via deep cards |
| **Data** | `3` | Read the tower via 8 specialized overlays displaying flows, intensity, pressure, and risk |
| **Manage** | `4` | Economy, policy decrees (rent caps, transit subsidies, quiet hours, tax rates), tenant leases, staff |

## Explanation chain

Every important problem must support:

1. A visible or audible symptom in the tower.
2. A relevant overlay that exposes pattern or flow.
3. An inspector that names contributing causes.
4. A direct route to a build, management, or policy response.
5. Feedback showing whether the response worked.

The inspector is a cause chain, not a flat stat dump: it names the symptom, exposes the relevant contributing data, and offers only contextual systems-level levers. See `Docs/09_CAUSE_CHAIN_INSPECTOR.md` for the canonical interaction contract.

## Beta overlays

| Overlay ID | Primary Visual Channel | Data Source | Corresponding Cause / Risk |
| --- | --- | --- | --- |
| `overlay:elevator_wait` | Animated flow paths & queue bars | `ElevatorBankCongestionProjection` | Shaft capacity shortage, floor bottlenecks |
| `overlay:foot_traffic` | Directional vector paths | `HierarchicalTransitGraph` | Corridor choke-points, stairwell demand |
| `overlay:population` | Density gradient & demographic glyphs | `PopulationState` | Overcrowding, demographic segregation |
| `overlay:satisfaction` | Soft regional glow + pattern glyphs | `SatisfactionService` | Commute friction, need deprivation, rent burden |
| `overlay:noise` | Acoustic wave contours | `AcousticPropagationService` | Workshop/diner noise bleeding into apartments |
| `overlay:business_health` | Solvency badges (green / amber / red) | `BusinessAccountingSystem` | Foot traffic failure, commercial rent burden |
| `overlay:faction_tension` | Regional tension contours + glyphs | `FactionState` | Policy grievances, strain, protest/strike risk |
| `overlay:utilities` | Network pipe/cable flow & pressure | `UtilityNetworkGraph` | Overloaded transformers, low water pressure |

Do not render every overlay as a heatmap. Use heatmaps for intensity, animated paths for flow, soft regions for influence, and reach contours for service coverage.

## Placement preview

A preview must show cost, invalid conditions, footprint, utility connections, and the most important predicted consequences (`PlacementGhostPresenter` / `PlacementPreviewCardView`). Predictions are estimates and must be labeled when confidence is low. Placement queries domain validity via `TowerSimulation.CanExecute(ICommand)`.

## Accessibility baseline

- All overlay meaning has a non-color channel (glyphs, text, shapes).
- Simulation speed and pause are keyboard accessible.
- UI scaling and remappable controls are planned from the first playable.
- Critical event information is available as text and audio-independent feedback.
- Wellbeing, pressure, and overlay states use a non-colour channel and expose their contributing causes.
