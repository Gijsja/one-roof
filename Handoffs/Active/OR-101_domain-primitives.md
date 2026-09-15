# OR-101 — Domain primitives

**Milestone:** M1
**Status:** DONE
**Dependencies:** OR-004
**Owner:** Codex session

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

- [x] Domain primitives compile in `OneRoof.Domain` without a UnityEngine reference.
- [x] Equal IDs compare and hash consistently; invalid/empty values are rejected at the chosen boundary.
- [x] A fixed clock advances deterministically and exposes its tick without frame-time dependency.
- [x] Two random streams with the same seed and operations produce identical sequences; restored state resumes exactly.
- [x] Rejected commands expose stable, machine-readable reason data; accepted results can carry immutable domain events.
- [x] Pure Edit Mode tests demonstrate deterministic behavior without loading a scene.

## Required validation

- Run the focused Domain Edit Mode tests with Unity CLI:
  `unity test /home/geisha/Vibecode/UnityAI/one-roof --editor-version 6000.3.24f1 --mode EditMode --filter "OneRoof.Domain.Tests" --output /tmp/one-roof-or101-editmode.xml --timeout 180 --format json`
- If the Unity CLI retains a dead Pipeline PID after a batch run, confirm there is no process or `Temp/UnityLockfile`, then use the direct headless Editor test runner and record that fallback.
- Do not claim remote CI passed until `UNITY_LICENSE` is configured and the GitHub workflow has completed.

## Completed work

- Added validated `EntityId`, `ContentId`, `Tick`, and `SchemaVersion` primitives. Runtime IDs and schema versions are positive; content IDs use `namespace:name`; ticks are non-negative.
- Added a fixed-step `SimulationClock` and a saveable xorshift64 random stream represented by seed, internal state, and consumed-value position.
- Added immutable command result/rejection contracts and immutable domain events, with deterministic event sorting by tick then event ID.
- Added seven focused pure Domain tests, alongside the existing Domain-to-UnityEngine boundary test.
- Recorded ADR-012 and marked OR-101 complete. OR-102, OR-103, and OR-301 are now ready.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Focused Domain Edit Mode (CLI) | `unity test /home/geisha/Vibecode/UnityAI/one-roof --editor-version 6000.3.24f1 --mode EditMode --filter "OneRoof.Domain.Tests" --output /tmp/one-roof-or101-editmode.xml --timeout 180 --format json` | NOT RUN TO VERDICT — CLI exited before test execution because its sandboxed licensing initialization failed and attempted to open X. |
| Stale-process check | Checked for Unity/LicenseClient processes and `Temp/UnityLockfile` after the CLI exit. | PASS — none found. |
| Focused Domain Edit Mode (direct fallback) | `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /home/geisha/Vibecode/UnityAI/one-roof -runTests -testPlatform EditMode -testFilter OneRoof.Domain.Tests -testResults /tmp/one-roof-or101-editmode.xml -logFile /tmp/one-roof-or101-editor.log` | PASS — 8 passed, 0 failed; report: `/tmp/one-roof-or101-editmode.xml`. |
| Whitespace check | `git diff --check` | PASS |

## Known risks

- Remote CI remains unverified because OR-005 is blocked on the repository `UNITY_LICENSE` secret.
- The Unity CLI test wrapper cannot access the local licensing service inside the filesystem sandbox; use the documented direct headless fallback until that environment issue changes.
- User-authored scene and ProjectSettings changes remain unmodified.

## Next safe action

Start OR-102 to introduce the five-floor topology primitives on top of these IDs and fixed clock contracts.

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
