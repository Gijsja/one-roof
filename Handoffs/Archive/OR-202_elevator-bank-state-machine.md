# OR-202 — Elevator bank state machine

**Status:** DONE  
**Owner:** Antigravity session  
**Started:** 2026-09-15  
**Updated:** 2026-09-15

## Objective

Implement a multi-phase elevator bank state machine in `OneRoof.Domain.Transit`, enforcing strict car capacity, FIFO floor queues, and discrete tick timings for travel, door cycles, and dwelling.

## Acceptance criteria

- [x] Car state machine supports explicit phases: `Idle`, `Moving`, `DoorsOpening`, `OpenLoading`, `DoorsClosing`.
- [x] Boarding: Waiting passengers from the floor queue board when doors are in `OpenLoading`.
- [x] Capacity: Car strictly caps passenger count to `Capacity`; excess passengers remain in the floor queue.
- [x] Queue: Multiple passenger requests across different floors are dispatched and delivered to destination floors.
- [x] Timing: Floor travel, door opening/closing, and dwelling follow configurable discrete tick settings.
- [x] Multi-car bank coordinates concurrent floor calls.

## Scope and ownership

Expected files/directories:

- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCarPhase.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorTimingConfig.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorPassenger.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs`
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs`
- `Assets/OneRoof/Tests/EditMode/Domain/ElevatorBankTests.cs`
- `Docs/07_DECISION_LOG.md`
- `Planning/BACKLOG.md`

Do not modify:

- Presentation or UI assemblies.
- Scenes or ProjectSettings files.

## State at handoff

The complete elevator bank state machine is implemented and verified by unit tests:
- `ElevatorCar` manages internal target requests, phase transitions, and passenger lists.
- `ElevatorBank` handles multi-car coordination, FIFO per-floor waiting queues, boarding, passenger discharge, and wait-time tracking.
- Edit Mode tests cover boarding, capacity enforcement, multi-destination delivery, exact timing ticks, and multi-car dispatch.

## Changes made

- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCarPhase.cs` — `ElevatorCarPhase` and `ElevatorDirection` enums.
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorTimingConfig.cs` — Value struct for travel, door cycle, and dwell ticks.
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorPassenger.cs` — Record for passenger origin, destination, wait, and ride times.
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorCar.cs` — Individual elevator car state machine.
- `Assets/OneRoof/Runtime/Domain/Transit/ElevatorBank.cs` — Multi-car elevator bank manager with per-floor queues.
- `Assets/OneRoof/Tests/EditMode/Domain/ElevatorBankTests.cs` — Unit tests for boarding, capacity, queues, timing, and multi-car handling.
- `Docs/07_DECISION_LOG.md` — Appended ADR-017.
- `Planning/BACKLOG.md` — Marked OR-202 DONE; unblocked OR-203 to READY.

## Decisions

- ADR-017: Implement elevator banks as explicit multi-phase state machines with discrete travel, door cycle, and dwell ticks.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| Boarding, capacity, queue, timing tests | Pure Domain test `ElevatorBankTests` | PASS |
| Git hygiene | `git diff --check` | PASS |

## Next safe action

Begin **OR-203** (Add wait-time and congestion projections) to provide read-only diagnostic metrics and overlay data to presentation.

## References

- Backlog: `OR-202`
- Decisions: `ADR-017`
