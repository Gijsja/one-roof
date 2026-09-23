# OR-103 — Versioned save envelope

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-15  
**Updated:** 2026-09-15

## Objective

Implement a versioned root save envelope, serialization, migration pipeline, and atomic file persistence for the simulation.

## Acceptance criteria

- [x] Versioned save envelope carries schema version, simulation tick, random stream checkpoint, and timestamp.
- [x] Round-trip serialization and deserialization preserves state and metadata without data loss.
- [x] Malformed or corrupted save payloads are rejected cleanly with a machine-readable `CorruptData` error reason.
- [x] Older schema versions migrate forward step-by-step through registered migrators.
- [x] Future schema versions are rejected cleanly with an `UnsupportedVersion` error reason.
- [x] File persistence uses an atomic write mechanism (`.tmp` write followed by atomic replace) to prevent corruption.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/Domain/Persistence/`
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/`
- `Assets/OneRoof/Tests/EditMode/Domain/SaveEnvelopeTests.cs`
- `Assets/OneRoof/Tests/EditMode/Infrastructure/JsonSaveSerializerTests.cs`
- `Assets/OneRoof/Tests/EditMode/Infrastructure/AtomicFileSaveStoreTests.cs`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`

Do not modify:

- Presentation or UI assemblies.
- Scenes or ProjectSettings files.

## State at handoff

The complete save envelope system is implemented and covered by unit tests:
- `SaveEnvelope<TState>` and `SaveEnvelopeMetadata` provide typed root envelopes.
- `JsonSaveSerializer` serializes envelopes, validates incoming JSON, supports forward migration chains via `ISaveMigrator`, and rejects corrupt payloads.
- `AtomicFileSaveStore` ensures atomic disk saves and cleans up temporary files.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Persistence/SaveEnvelopeMetadata.cs` — Value object for envelope version, tick, random state, and timestamp.
- `Assets/OneRoof/Runtime/Domain/Persistence/SaveEnvelope.cs` — Generic root envelope container.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/LoadErrorReason.cs` — Machine-readable load error enum.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/SaveOperationResults.cs` — `SaveResult` and `LoadResult<T>` types.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/ISaveMigrator.cs` — Interface for step-by-step schema migrations.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/ISaveSerializer.cs` — Interface for envelope serialization.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/JsonSaveSerializer.cs` — Unity JsonUtility envelope serializer with migration chain support.
- `Assets/OneRoof/Runtime/Infrastructure/Persistence/AtomicFileSaveStore.cs` — Atomic file write and load store.
- `Assets/OneRoof/Tests/EditMode/Domain/SaveEnvelopeTests.cs` — Unit tests for metadata and envelope validation.
- `Assets/OneRoof/Tests/EditMode/Infrastructure/JsonSaveSerializerTests.cs` — Round-trip, corruption, version rejection, and migration tests.
- `Assets/OneRoof/Tests/EditMode/Infrastructure/AtomicFileSaveStoreTests.cs` — Atomic file persistence and cleanup tests.
- `Docs/07_DECISION_LOG.md` — Appended ADR-015.
- `Planning/BACKLOG.md` — Marked OR-103 DONE.

## Decisions

- ADR-015: Use a typed JSON envelope with embedded payload string, forward migrator pipeline, and atomic file replace.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| SaveEnvelope validation | Pure Domain test `SaveEnvelopeTests` | PASS |
| Serialization & Corrupt-save | Edit Mode test `JsonSaveSerializerTests` | PASS |
| Atomic persistence | Edit Mode test `AtomicFileSaveStoreTests` | PASS |
| Git hygiene | `git diff --check` | PASS |

## Known risks or failures

None known. All Milestone 1 tasks (OR-101, OR-102, OR-103) are now DONE.

## Next safe action

Begin **OR-201** (Implement hierarchical transit graph) to connect room portals, floor-local paths, and vertical transit graphs.

## References

- Backlog: `OR-103`
- Decisions: `ADR-015`
