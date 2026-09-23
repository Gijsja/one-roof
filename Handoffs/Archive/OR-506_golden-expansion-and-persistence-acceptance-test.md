# OR-506 — Golden Expansion & Persistence Acceptance Test

**Milestone:** M5.1  
**Status:** DONE  
**Dependencies:** OR-501, OR-502, OR-503, OR-504, OR-505  
**Owner:** Antigravity session  

## Outcome

Deliver the Golden Acceptance Tests for Milestone 5.1 in both EditMode and PlayMode, conclusively demonstrating the full vertical city simulation loop:
1. Standard baseline setup (5 floors, 50 residents, 1 elevator car).
2. Dynamic expansion to Floor 5 (building floor slab, 4 apartments, and extending elevator shaft with construction costs deducted from the treasury).
3. Autonomous leasing demand moving new households into Floor 5 apartments and expanding population beyond 50 residents.
4. Morning rush hour commute congestion producing elevator bottlenecks and wait times.
5. Capacity intervention adding a 2nd elevator car, reducing average elevator wait time by >40%.
6. Comprehensive persistence round-trip through `SaveEnvelope<TowerSaveData>` and `JsonSaveSerializer`, confirming bit-exact state restoration and deterministic tick progression.

## Files Owned and Created / Modified

### Tests
- `Assets/OneRoof/Tests/EditMode/Infrastructure/GoldenExpansionAcceptanceTests.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Tests/PlayMode/GoldenExpansionPlayModeTests.cs` (+ `.meta`) [NEW]
- `Assets/OneRoof/Tests/PlayMode/OneRoof.Tests.PlayMode.asmdef` [MODIFIED: added `OneRoof.Domain` reference]

### Documentation and Tracking
- `Planning/BACKLOG.md`: Marked OR-506 as `DONE`. All M5.1 tasks (`OR-501` through `OR-506`) are complete.
- `Docs/07_DECISION_LOG.md`: Appended `ADR-030`.

## Acceptance Criteria

- [x] Full simulation cycle runs deterministically across pure C# domain and application layers without Unity scene dependencies.
- [x] Construction of floor slabs and rooms correctly mutates topology and registers with the spatial index and transit graph.
- [x] Autonomous leasing demand detects vacancies on new floors and moves in households.
- [x] Elevator capacity intervention achieves >40% wait reduction over the congested single-car baseline.
- [x] Save envelope round-trip restores topology, rooms, residents, treasury, in-flight transit legs, and elevator car states with bit-exact fidelity.
- [x] PlayMode test verifies runtime integration with `TowerPlayableController`.

## Validation

| Check | Procedure | Result |
| --- | --- | --- |
| Assembly boundaries | Inspection of asmdefs and referenced namespaces | PASS |
| Clean Git tree & meta files | `git status -u` & python meta checker | PASS (all files paired with `.meta`) |
| Edit Mode golden test | `GoldenExpansionAcceptanceTests.cs` | PASS |
| Play Mode golden test | `GoldenExpansionPlayModeTests.cs` | PASS |

## Next Safe Action

Milestone 5.1 is complete. Update the walkthrough documentation and signal goal completion.
