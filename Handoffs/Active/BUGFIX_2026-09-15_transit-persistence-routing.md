# Handoff — BUG_REPORT_2026-09-15 Repairs

**Session date:** 2026-09-15  
**Commits addressed:** `edbfb60`, `5477011`, `c0ae583`  
**Task scope:** All 9 outstanding defects (7 P1 + 6 P2) from the bug report.

---

## Validation status

Unity compilation: **NOT RUN** — Unity did not produce a test result on this host (no display/license services). Build and test must be run by the developer before merging.

---

## Changed files

| File | Group | Change |
| --- | --- | --- |
| `Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs` | P1 Persistence | `File.Delete` + `File.Move` → `File.Replace` (atomic rename, no gap) |
| `Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs` | P1 Persistence | Metadata construction wrapped in try/catch → `CorruptData` on bad timestamp/tick/random state |
| `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs` | P1 Transit + P1 Congestion | Destination floor range validation in `EnqueuePassenger`; new `GetFloorWaitMetrics` |
| `Assets/OneRoof/Runtime/Domain/Transit/TransitPrototypeSimulation.cs` | P1 Transit | `ElevatorCarSnapshot` extended with `PassengerIds` list |
| `Assets/OneRoof/Runtime/Application/Transit/TransitPrototypeSession.cs` | P1 Transit + P2 Alloc | `ElevatorProjection` extended with `PassengerIds`; `Projection()` caches per-tick |
| `Assets/OneRoof/Runtime/Presentation/Transit/TransitPrototypeController.cs` | P1 Presentation + P2 Leak | `FindPassengerElevator` uses real manifest; `OnDestroy` destroys world material |
| `Assets/OneRoof/Runtime/Domain/Transit/HierarchicalTransitGraph.cs` | P1 Routing + P2 Edges | Stair vertical Walk edges (step 4); `VerticalStairFloorCost = 15`; constructor drops edges with unknown `ToNodeId` |
| `Assets/OneRoof/Runtime/Application/Transit/TransitCongestionService.cs` | P1 Congestion | Real `maxWaitTicks` / `averageWaitTicks` per floor from `GetFloorWaitMetrics` |
| `ProjectSettings/EditorBuildSettings.asset` | P1 Build | `Testbed_Transit.unity` added as enabled scene (GUID `8c92f3bc15f12dcc1bc38dba3af74416`) |
| `Assets/OneRoof/Runtime/Domain/Transit/TransitRoutePlanner.cs` | P2 Routing | `HashSet<EntityId>` unvisited replaced with `SortedSet<(distance, nodeId)>` for deterministic tie-breaking |
| `Assets/OneRoof/Runtime/Domain/Topology/FloorTopology.cs` | P2 Topology | Portal acceptance verifies `RoomId` references an existing room |
| `Assets/OneRoof/Tests/EditMode/Infrastructure/AtomicFileSaveStoreTests.cs` | Tests | +`SaveOverExistingFileReplacesContentWithoutGap`, +`DeleteWhenFileAbsentReturnsFalse` |
| `Assets/OneRoof/Tests/EditMode/Infrastructure/JsonSaveSerializerTests.cs` | Tests | +`CorruptNegativeTickReturnsCorruptData`, +`CorruptZeroRandomStateReturnsCorruptData` |
| `Assets/OneRoof/Tests/EditMode/Domain/ElevatorBankTests.cs` | Tests | +`EnqueuePassengerWithDestinationOutsideBankRangeThrows` |
| `Assets/OneRoof/Tests/EditMode/Domain/HierarchicalTransitGraphTests.cs` | Tests | +`StairwellPortalsReceiveVerticalWalkEdges` |
| `Assets/OneRoof/Tests/EditMode/Application/TransitCongestionProjectionTests.cs` | Tests | +`FloorWaitMetricsAreNonzeroAfterScenarioRuns` |
| `Assets/OneRoof/Tests/EditMode/Presentation/TransitPrototypeSceneCompositionTests.cs` | P2 Tests | `[SetUp]`/`[TearDown]` capture and restore active scene |

---

## Known risks

**`File.Replace` cross-filesystem:** Atomic only when `.tmp` and target are on the same filesystem. We write `.tmp` next to the target (not to `/tmp`), so same-filesystem is guaranteed in practice.

**Stair test `CellBounds` format:** `CellBounds(floor, minX, maxX)` — verified against Room source. Stair test uses `CellBounds(0, 5, 5)` for a single-column stair room, which is valid.

**`TransitRoutePlanner` Dijkstra rewrite:** `SortedSet.Remove` of an entry that was never added is a no-op (returns false). No invariant violated.

**`FloorTopology` portal room-reference check:** New strict validation. Any fixture/content with portals referencing non-existent rooms will now throw. `TwoFloorTransitFixture` and `FiveFloorTopologyFixture` use matching room IDs; they are unaffected.

**Working-tree collider edit:** The `TransitPrototypeController.cs` had an unrelated unstaged collider-lifecycle edit at report time (noted in the bug report). The `OnDestroy` added here is a new method and does not interact with any collider code.

---

## Next safe action

1. Open Unity Editor and verify no compilation errors.
2. Run all Edit Mode tests: `Window → General → Test Runner → Run All`.
3. Open `File → Build Settings` — confirm `Testbed_Transit` appears as an enabled scene.
4. Play `Testbed_Transit` → add a second elevator → verify riders appear in the correct car.
5. If tests pass, commit with message:
   `fix(transit,persistence,routing): address all P1 and P2 defects from BUG_REPORT_2026-09-15`
