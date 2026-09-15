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

## Presentation

- Pool visible NPC views and active effects.
- Stream or activate floor presentation by camera range.
- Bind views through entity IDs and read-only projections.
- Keep camera angle and art perspective constrained.
- UI dispatches application commands; it does not mutate state directly.

## Scenes

- `Bootstrap`: composition, catalogs, save load, settings.
- `FrontEnd`: profiles, campaign selection, settings.
- `Tower`: world presentation and interaction.
- `Testbed_Transit`: isolated five-floor proof.
- `AssetLab`: Editor-only content preview and validation.

## Content

- ScriptableObjects hold authored definitions and tuning, never mutable campaign state.
- Addressables group room themes, character layers, audio, and event presentation.
- Content uses immutable string asset IDs; runtime entities use integer IDs.
- A build-time validator detects missing IDs, duplicate IDs, invalid anchors, and broken references.

## Save strategy

- One versioned root save envelope.
- Store simulation state and player decisions, not view state.
- Every schema change supplies a forward migration and fixture test.
- Autosaves write to a temporary target and replace only after successful serialization.

## Performance budgets for first playable

- 60 FPS presentation target on the agreed reference PC.
- 50 persistent residents; 40 visible views maximum.
- Simulation tick p95 below 4 ms in release-like profiling.
- No managed allocation during steady-state simulation ticks.
- Save/load below 1 second for first-playable state.

