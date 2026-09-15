# Transit Foundation Bug Report — 2026-09-15

## Review scope

- Reviewed commits: `edbfb60` (`ci: add Unity validation workflow`) and `5477011` (`feat(milestone-1,2): complete Milestones 1 and 2`).
- Follow-up fix committed during review: `c0ae583` (`fix(transit): restore schema comparisons and tick timing`).
- Sources reviewed: Domain transit, topology, persistence, Application projections, Presentation prototype, build configuration, and related Edit Mode tests.

## Validation status

- `git diff --check`: clean for the committed review fix.
- Local targeted Edit Mode test: **NOT RUN**. Unity did not produce a test result file on this host after failing to initialize its display/licensing services.
- No Play Mode or player build was run.

## Fixed in `c0ae583`

| Priority | Issue | Resolution |
| --- | --- | --- |
| P0 | `JsonSaveSerializer` used `<` and `>` on `SchemaVersion`, but those operators were absent, preventing compilation. | Added comparison operators to `SchemaVersion`. |
| P1 | A car starting from `Idle` did not consume the first configured travel tick, contradicting `TimingFollowsConfiguredTicksPrecisely`. | The transition to `Moving` now advances movement in that same tick. |

## Outstanding defects

| Priority | Area | Defect | Impact | Primary location |
| --- | --- | --- | --- | --- |
| P1 | Transit | Passenger destination floors are not validated against the bank range. | A car can travel beyond the tower instead of rejecting an impossible trip. | `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs:86` |
| P1 | Diagnostics | Floor wait metrics are hard-coded to zero. | The elevator overlay cannot explain how long a floor has been waiting, despite its required explanation chain. | `Assets/OneRoof/Runtime/Application/Transit/TransitCongestionService.cs:39` |
| P1 | Persistence | Save replacement deletes the existing save before moving the temporary file. | A crash or failed move in the gap loses the only save. | `Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs:37` |
| P1 | Persistence | Invalid metadata is constructed outside the deserialization error boundary. | Valid JSON with corrupt timestamp, tick, or random position throws rather than returning `CorruptData`. | `Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs:128` |
| P1 | Routing | Stairwell doors never receive vertical graph edges. | Trips needing stairs cannot be planned, although stairs are first-playable scope. | `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs:130` |
| P1 | Presentation | Riders are rendered in a car chosen by resident-ID parity rather than their real passenger manifest. | After adding a second car, visible NPCs frequently occupy the wrong elevator, breaking simulation/view correspondence. | `Assets/OneRoof/Runtime/Presentation/Transit/TransitPrototypeController.cs:124` |
| P1 | Build | `Testbed_Transit` is not enabled in Build Settings. | A built player launches `SampleScene`, not the transit proof. | `ProjectSettings/EditorBuildSettings.asset:7` |
| P2 | Routing | Equal-cost Dijkstra candidates are selected by `HashSet` enumeration order. | Equivalent routes are not guaranteed to be deterministic across runtimes. | `Assets/OneRoof/Runtime/Domain/Transit/TransitRoutePlanner.cs:47` |
| P2 | Topology | Portals are not verified to reference an existing room or lie inside that room. | Invalid content produces phantom graph nodes and routes. | `Assets/OneRoof/Runtime/Domain/Topology/FloorTopology.cs:53` |
| P2 | Routing | Graph construction admits edges whose destination node does not exist. | Route planning can throw `KeyNotFoundException` on malformed content. | `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs:52` |
| P2 | Persistence | Migration chaining chooses the first matching source migration. | Registration order can select a dead-end chain despite another valid route to the target version. | `Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs:150` |
| P2 | Presentation | Every render frame constructs fresh snapshot/projection lists; `OnGUI` requests another projection. | Steady-state managed allocations create avoidable garbage collection pressure. | `Assets/OneRoof/Runtime/Application/Transit/TransitPrototypeSession.cs:86` |
| P2 | Presentation | Runtime-created world material has no lifecycle cleanup. | Reopening or recreating the prototype leaks native material resources. | `Assets/OneRoof/Runtime/Presentation/Transit/TransitPrototypeController.cs:171` |
| P2 | Tests | Scene composition test opens `Testbed_Transit` as the only scene and does not restore prior state. | Edit Mode test order changes global editor state and can affect later tests. | `Assets/OneRoof/Tests/EditMode/Presentation/TransitPrototypeSceneCompositionTests.cs:13` |

## Recommended repair order

1. Correct P1 save safety and corrupt-save handling; add interruption/failure and malformed-metadata tests.
2. Complete transit truth: validate passenger destinations, project real passenger-to-car membership, and add stair edges.
3. Repair congestion projections so per-floor wait values are real and add a regression test that proves nonzero values during the fixed scenario.
4. Add `Testbed_Transit` to Build Settings and prove the five-floor loop in Play Mode.
5. Harden topology/graph validation and deterministic route tie-breaking.
6. Remove per-frame projection allocations, release generated material, and isolate Editor scene tests.

## Working-tree note

At report time, `Assets/OneRoof/Runtime/Presentation/Transit/TransitPrototypeController.cs` has an unrelated unstaged collider-lifecycle edit. It was intentionally excluded from `c0ae583` and from the findings above.
