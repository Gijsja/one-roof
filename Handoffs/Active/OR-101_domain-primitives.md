# OR-101 — Domain primitives

**Milestone:** M1
**Status:** READY
**Dependencies:** OR-004
**Owner:** unassigned

## Outcome

Provide a pure C# foundation for deterministic simulation: stable runtime IDs, simulation ticks, seeded randomness, command outcomes, and immutable domain events.

## Context

- `OneRoof.Domain` is a separate assembly with `noEngineReferences: true`; no type in this task may depend on `UnityEngine`, scenes, GameObjects, or MonoBehaviours.
- The canonical contracts are in `Docs/02_ARCHITECTURE.md` and `Docs/03_DATA_CONTRACTS.md`.
- IDs are stable integers within one save. Content IDs are immutable namespaced strings. All gameplay time comes from the simulation clock. Randomness must use injected, seedable streams.
- Commands express intent, validate before mutation, return machine-readable reasons when rejected, and successful commands emit domain events.
- OR-005’s remote GitHub validation remains blocked until a repository `UNITY_LICENSE` secret is configured; local Unity CLI Edit Mode validation works.

## In scope

- Implement value types or small records for `EntityId`, `ContentId`, `Tick`, and `SchemaVersion` in `Assets/OneRoof/Runtime/Domain/`.
- Implement a fixed-step simulation clock with explicit advancement and a serializable tick value.
- Implement an injected deterministic random-stream abstraction with seed and restorable position/state suitable for save data.
- Implement minimal command-result and rejection-reason contracts that do not prescribe future room, transit, or person commands.
- Implement immutable domain-event contracts that carry event identity/type, tick, and affected entity IDs without referencing presentation objects.
- Add pure Edit Mode tests for ID validation/equality, clock progression, identical seeded sequences, restored random state, command accept/reject data, and deterministic event ordering.
- Record any durable contract decision in `Docs/07_DECISION_LOG.md` and complete this handoff with exact validation evidence.

## Out of scope

- Floors, rooms, portals, people, schedules, transit routing, elevators, saves, UI, scenes, ScriptableObjects, Addressables, and presentation views.
- Implementing a concrete command handler or a complete event bus.
- Changing the user-authored `one-roof` scene or ProjectSettings files.
- Solving the GitHub `UNITY_LICENSE` secret configuration.

## Acceptance criteria

- [ ] Domain primitives compile in `OneRoof.Domain` without a UnityEngine reference.
- [ ] Equal IDs compare and hash consistently; invalid/empty values are rejected at the chosen boundary.
- [ ] A fixed clock advances deterministically and exposes its tick without frame-time dependency.
- [ ] Two random streams with the same seed and operations produce identical sequences; restored state resumes exactly.
- [ ] Rejected commands expose stable, machine-readable reason data; accepted results can carry immutable domain events.
- [ ] Pure Edit Mode tests demonstrate deterministic behavior without loading a scene.

## Required validation

- Run the focused Domain Edit Mode tests with Unity CLI:
  `unity test /home/geisha/Vibecode/UnityAI/one-roof --editor-version 6000.3.24f1 --mode EditMode --filter "OneRoof.Domain.Tests" --output /tmp/one-roof-or101-editmode.xml --timeout 180 --format json`
- If the Unity CLI retains a dead Pipeline PID after a batch run, confirm there is no process or `Temp/UnityLockfile`, then use the direct headless Editor test runner and record that fallback.
- Do not claim remote CI passed until `UNITY_LICENSE` is configured and the GitHub workflow has completed.

## Expected ownership

- `Assets/OneRoof/Runtime/Domain/Identity/`
- `Assets/OneRoof/Runtime/Domain/Time/`
- `Assets/OneRoof/Runtime/Domain/Randomness/`
- `Assets/OneRoof/Runtime/Domain/Commands/`
- `Assets/OneRoof/Runtime/Domain/Events/`
- `Assets/OneRoof/Tests/EditMode/Domain/`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`
- `Handoffs/Active/OR-101_domain-primitives.md`
