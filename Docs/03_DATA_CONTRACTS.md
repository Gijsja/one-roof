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
| Person | ID, household, home, workplace, schedule, needs, focus, satisfaction, grievances, strain, personality facets, specialist role/training progress, relationships, affiliations, current activity |
| Household | ID, members, home, budget, preferences, satisfaction |
| Room | ID, content type, floor, bounds, portals, capacity, owner/tenant, service state |
| Business | ID, archetype, leased rooms, employees, finances, demand, reputation |
| Faction | ID, members/support, goals, grievances, influence by region, relations |
| Trip | person, origin and destination location endpoints (tower room or Outside), purpose, departure tick, route state, wait time |
| Event | ID, type, phase, affected entities/regions, causes, player responses |

## Wellbeing and explanation projections

- Needs are the five short-term values: Hunger, Energy, Social, Hygiene, and Purpose. Focus is a derived, inspectable consequence rather than an independently authored personality value.
- Satisfaction projections expose deterministic contributors: commute quality, crowding, noise, rent burden, service access, and recent events. Low satisfaction may create time-bounded grievances.
- Strain is a long-term, cumulative projection. Its drivers and personality-facet multipliers must be available to the inspector; personality is a small high-impact set, not an unbounded trait matrix.
- Scrutiny is tower-level state with a current value, trend, deterministic contributing factors, and externally visible consequences.
- Specialist roles are person-owned, saveable domain state. Training capacity comes from rooms and residents acquire roles autonomously; no command assigns a role to an individual.
- Presentation receives explanation-ready, immutable projections. It must not recompute wellbeing or query mutable domain state when an inspector opens.

## Command rules

- Commands express player or system intent and are validated before mutation.
- A rejected command returns machine-readable reasons for UI presentation.
- Successful commands emit domain events.
- UI never bypasses commands to edit simulation records.

## Snapshot rules

- Presentation receives immutable snapshots or projections.
- Snapshots expose only data required by the consumer.
- Collections have deterministic ordering where UI or tests depend on it.
- `Outside` is a valid world endpoint in resident location and trip contracts. It is distinct
  from a room ID and may not be encoded as an invalid/sentinel `EntityId`. Its tower connection
  is the lobby entrance, allowing move-ins and external commutes to share the same route seam.
- Save DTOs persist `currentLocationKind`, `workplaceLocationKind`, and typed origin/destination
  kinds for active trips alongside room IDs. Missing kind fields in older saves default to
  `Room`; the kind field, never an invalid room ID, selects Outside on load.
- Background computation must not retain mutable Unity objects.

## Time, day cycle, and randomness

- All gameplay time comes from the simulation clock (`Tick`).
- Day cycle: `DailySchedule.TicksPerDay = 1440` (tick 0 = midnight). `DayClock.FromTick` maps simulation ticks into `DayPhase` (day index, 24-hour HH:MM time, night phase) for deterministic presentation and daily settlement.
- Random outcomes use injected, seedable xorshift64 streams.
- Save files persist seed and stream position where outcomes would otherwise change after load.

## Economic contracts (Unit Economics)

- Money is strictly integer `long` cash units in a closed loop (`Docs/12_ECONOMY.md`).
- Treasury (`TowerEconomyState`), Households (`HouseholdRecord.CashBalance`), and Businesses (`BusinessRecord.CashBalance`) conserve cash.
- Settlement executes daily at `tick % 1440 == 0`.
- Normalized `0–1` budget tiers exist solely as derived inspector projections, never stored state.
