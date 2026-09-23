# Handoff — Outside boundary and smooth resident walking

## What changed
- Defined `Outside` as a first-class world endpoint connected to the tower through the ground-floor lobby. The contract explicitly avoids sentinel room IDs and reserves street-horizon/street-view presentation for expansion.
- Recorded ADR-071 and added OR-1004 for the full Domain, route, save/load, move-in, and external-job implementation.
- Walking resident views now move toward fixed-tick position projections at a frame-based speed in Play Mode; non-Play/editor presentation remains exact for deterministic inspection.

## Validation
- Not run (Unity compile/tests not requested; presentation interpolation change is limited to a guarded Play Mode branch).

## Remaining work
- OR-1004 remains READY. The current trip and save contracts require valid room IDs, so actual Outside arrivals and external work commutes need a typed location endpoint plus graph and save migration work before behavior can be enabled.
- Tune `WalkPresentationSpeed` in Play Mode once movement is observed at target tick/frame rates.
