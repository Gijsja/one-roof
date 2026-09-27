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
| Underground cell | 1 m square grid coordinate on an independent 32×12 board; excavated and floored cells persist |
| Underground room | Stable ID, type, rectangular bounds, corridor access, capacity, required staff, upkeep, operating state |
| Underground operation | Supplies, intel, research, exposure, policy priorities, resident assignments, investigator route and disruption |

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
- Resident purposes are person-owned domain state with an absolute start and minimum end tick. A completed trip starts the destination activity on arrival. Outside employment keeps a resident at `Outside` for at least 480 ticks after arrival; on standard schedules, diner meals last at least 30 ticks and home activities at least 60 ticks. The deliberately compressed five-floor acceptance fixture uses 5-tick room episodes. A pending minimum survives save/load, and a full-day standard work block lasts exactly 480 ticks.
- Person saves carry their schedule blocks and current purpose interval. Older saves without these fields retain their legacy schedule reconstruction and acquire a new purpose interval on the next eligible tick.
- Random outcomes use injected, seedable xorshift64 streams.
- Save files persist seed and stream position where outcomes would otherwise change after load.

## Economic contracts (Unit Economics)

- Money is strictly integer `long` cash units in a closed loop (`Docs/12_ECONOMY.md`).
- Treasury (`TowerEconomyState`), Households (`HouseholdRecord.CashBalance`), Businesses (`BusinessRecord.CashBalance`), and the aggregate outside-market counterparty reconcile explicit cash flows. Documented sources/sinks (such as outside contracts and starting household cash) remain explicit.
- Settlement executes daily at `tick % 1440 == 0`.
- Normalized `0–1` budget tiers exist solely as derived inspector projections, never stored state.
- `OutsideMarketState` is one aggregate counterparty for daily Outside-assigned wages and completed outside-service purchases. Contract-funded wages, service receipts, bounded credit issuance, and credit repayment are explicit integer flows and survive save/load.
- Household cash, outside-market credit, and residential rent arrears are distinct. Essential purchases can use bounded outside credit; only unpaid rent advances rent-specific arrears. Sustained negative rolling budget plus a non-positive financial position can independently trigger recoverable housing risk.
- Completed Food trips to `Outside` charge once by stable trip ID and restore Hunger only when the purchase is accepted. Failed essential purchases grant no free meal and must use a bounded retry interval. Internal diner visits remain in the tower business ledger.
- Household housing condition is an immutable projection of rent arrears and sustained budget/underprovision exposure. Seven-day notices and bounded departure history are persisted in the domain lifecycle state; a move-out releases the home only after all members reach typed `Outside`.
- Underground excavation stores open earth, floors, corridors, shaft, access core, and room zones on a 32×12 board independent of tower slabs. Each cell is 1 m; dig and floor brushes retain 1×1, 2×2, and 3×3 footprints. Older 16×6 coordinates migrate into the centered area. Missing operation state loads with empty resources, no investigator, and default policies.
- Underground staffing selects eligible resident IDs deterministically; no player command assigns a person. Supply purchases, resident wages, upkeep, and outside contracts are explicit treasury/outside/household transfers. Disruption and exposure survive save/load.
