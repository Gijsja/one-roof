# Unity Architecture

## Runtime layers

```text
Bootstrap / composition root
  ├── Application use cases and commands
  │     ├── Domain simulation
  │     └── Ports: clock, save store, path service, content catalog
  ├── Infrastructure adapters
  └── Presentation: tower, NPC views, UI, audio, VFX
```

The Domain assembly is pure C#. Unity-facing assemblies consume snapshots, projections, commands, and events.

## Proposed assemblies

- `OneRoof.Domain`
- `OneRoof.Application`
- `OneRoof.Infrastructure`
- `OneRoof.Presentation`
- `OneRoof.UI`
- `OneRoof.Content`
- `OneRoof.Editor`
- Matching `.Tests.EditMode` assemblies
- `OneRoof.Tests.PlayMode`

## Simulation

- Fixed tick independent from frame rate.
- Stable integer IDs and deterministic seeded randomness.
- Plain serializable records for people, households, rooms, businesses, factions, trips, and events.
- Systems consume indexed state and produce state changes plus immutable domain events.
- Fidelity tiers: aggregate/off-screen, scheduled/persistent, and visible/story-relevant.
- No per-agent `Update()` loops.

## Movement

Use a hierarchical graph:

1. Interaction point or room portal.
2. Floor-local walk graph.
3. Vertical transit graph of elevators, stairs, and transfer nodes.
4. Destination floor-local graph.

Route intent is domain data. Animation and physical interpolation are presentation concerns.

### Outside world boundary

`Outside` is a persistent world location beyond the tower boundary, not a room and not a
resident spawn shortcut. It connects to the tower through the ground-floor lobby entrance;
resident move-ins and outbound trips cross that entrance. External destinations (including
jobs) attach to the outside network and route through the lobby. The first implementation may
use a compact street-edge node with no simulated city block, while preserving the location
identity and route boundary. A street-level view and surrounding city remain a later
presentation expansion; the tower cutaway may show the street horizon from its exterior edge.
Outside location and route state belong to the Domain and must survive save/load; street art,
camera framing, and frame interpolation remain Presentation concerns.

The first implementation uses `WorldLocation` (`Room` or `Outside`) on residents and trips.
The transit graph adds one street-edge node joined only to the ground-floor lobby portal;
in-flight routes are reconstructed from typed endpoints on load. Leasing arrivals begin at
Outside and follow the same route seam as external workers. The cutaway draws a compact
street edge at the ground slab; a larger street view remains deferred.
`OutsideCityPresenter` builds a collider-free, three-depth skyline in Presentation. It moves
only the layer roots on horizontal camera pans, shifts its anchor with ground-slab expansion,
and derives window and sky colours from the existing day/night clock. City geometry is not
simulation or save state.

## Presentation & UI

- Pool visible Spine NPC views (`TowerResidentPresenter`) and active effects; 40–60 view cap.
- Stream or activate floor presentation by camera range.
- Bind views through entity IDs and read-only immutable projections.
- `OutsideCityPresenter` creates a 3-depth parallax skyline outside the tower edge, responsive to horizontal camera panning and day/night transitions.
- `TowerAtmospherePresenter` maps the 1440-tick daily cycle into 24-hour day/night presentation phases (warm window lighting and unlit ambient tint).
- `ModeShellBarController` and `StewardTheme` form the UI shell; UI dispatches application commands and never mutates simulation state directly.

## Scenes

- `Tower`: primary interactive world presentation and full gameplay scene.
- `Tower_GroundStart`: lightweight starting topology scene for fresh tower development.
- `Testbed_Transit`: isolated five-floor vertical transit proof.
- `AssetLab`: Editor-only content preview, rig, and asset validation.
- `Bootstrap` / `FrontEnd`: composition, profiles, and campaign selection.

## Content

- ScriptableObjects hold authored definitions and tuning, never mutable campaign state.
- Addressables group room themes, character layers, audio, and event presentation.
- Content uses immutable string asset IDs; runtime entities use integer IDs.
- A build-time validator detects missing IDs, duplicate IDs, invalid anchors, and broken references.

## Save strategy

- One versioned root save envelope (`SaveEnvelope`).
- Store simulation state and player decisions, not view state.
- Sub-aggregates own `ToSaveData()` / `FromSaveData()` serialization (ARCH-004).
- Every schema change supplies a forward migration and fixture test.
- Autosaves write to a temporary target and replace only after successful serialization.

## Performance budgets

- **First playable**: 50 persistent residents, 40 visible views maximum, 60 FPS presentation target.
- **Beta boundary (North Star)**: 30 floors, 300 persistent residents, 60 pooled visible views maximum, simulation tick p95 below 4.0 ms, draw calls < 120 via instancing/atlasing, texture memory < 180 MB, 60 FPS presentation on reference hardware.
- No managed allocation during steady-state simulation ticks.
- Save/load below 1 second for first-playable state, below 2 seconds for 30-floor beta state.
