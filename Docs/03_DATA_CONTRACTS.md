# Data Contracts

These are conceptual contracts. Concrete C# types may evolve through recorded decisions, but their ownership must remain stable.

## Identity

```text
EntityId      integer, unique within one save
ContentId     namespaced string, immutable across releases
Tick          integer simulation time
SchemaVersion integer save/content version
```

## Core records

| Record | Required fields |
| --- | --- |
| Person | ID, household, home, workplace, schedule, needs, traits, relationships, affiliations, current activity |
| Household | ID, members, home, budget, preferences, satisfaction |
| Room | ID, content type, floor, bounds, portals, capacity, owner/tenant, service state |
| Business | ID, archetype, leased rooms, employees, finances, demand, reputation |
| Faction | ID, members/support, goals, grievances, influence by region, relations |
| Trip | person, origin, destination, purpose, departure tick, route state, wait time |
| Event | ID, type, phase, affected entities/regions, causes, player responses |

## Command rules

- Commands express player or system intent and are validated before mutation.
- A rejected command returns machine-readable reasons for UI presentation.
- Successful commands emit domain events.
- UI never bypasses commands to edit simulation records.

## Snapshot rules

- Presentation receives immutable snapshots or projections.
- Snapshots expose only data required by the consumer.
- Collections have deterministic ordering where UI or tests depend on it.
- Background computation must not retain mutable Unity objects.

## Time and randomness

- All gameplay time comes from the simulation clock.
- Random outcomes use injected, seedable streams.
- Save files persist seed and stream position where outcomes would otherwise change after load.

