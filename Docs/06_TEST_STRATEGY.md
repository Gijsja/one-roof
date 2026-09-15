# Test Strategy

## Test pyramid

### Pure domain tests

Cover deterministic schedules, needs, trip generation, routing decisions, business accounting, faction formation, event causes, save migration, and prediction calculations. These tests must not load Unity.

### Edit Mode integration tests

Cover content catalogs, ScriptableObject conversion, registry uniqueness, asset validation, serialization adapters, and application command handlers.

### Play Mode tests

Cover Bootstrap composition, scene loading, pooled view binding, player input, overlay rendering hooks, build placement, and the complete five-floor loop.

## Required fixtures

- Tiny two-floor transit graph.
- Five-floor first-playable tower.
- Fixed fifty-resident seed.
- Congested elevator scenario.
- Pre- and post-migration save fixtures.

## Golden acceptance test

Given the fixed first-playable seed, morning demand must create a measurable elevator bottleneck. After the player adds the approved capacity intervention, median commute and elevator wait must improve by the configured threshold without invalidating saves or schedules.

## Evidence in handoffs

Record command, environment, result, failure count, and profiler capture location. Screenshots alone do not prove simulation correctness; unit metrics alone do not prove understandable UX.

