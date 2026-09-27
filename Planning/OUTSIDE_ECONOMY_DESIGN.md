# Outside Economy and Household Life-Cycle Model

**Status:** ECON-005 implemented and validated, including the work/meal trip loop, period-boundary accounting, lifecycle pressure, and authored resident sprites; see `Handoffs/Archive/HANDOFF_2026-09-26_outside-economy-integration.md`.
**Scope:** A compact daily household budget, outside jobs and services, lifecycle costs,
hardship and recovery, and links to existing housing, needs, wellbeing, business, and
faction mechanics. Outside employers and service providers are represented by one explicit
aggregate market counterparty; individual street businesses and their tenants are outside this
slice.

## Design intent

Residents should be able to live and work beyond the tower, but the tower should offer
better earning opportunities and cheaper, more reliable access to essential services.
Failing to provide housing, food, work, or care should gradually reduce a household's
financial resilience and quality of life. The model should make that chain visible and
recoverable: the Steward changes systems and capacity; residents make their own choices.

The model uses one shared household wallet and daily settlement. It tracks a small number
of economic pressures rather than simulating every purchase. Money units remain integer
`long` values and all outside flows have an explicit counterparty.

## Household budget and life cycle

For each household and day:

```text
net = earned income + transfers - rent - essentials - upkeep - lifecycle costs
cash_end = cash_start + net
```

Use `HouseholdRecord.CashBalance` as liquid cash. Extend the daily ledger to distinguish
income, rent, essential service spending, optional quality spending, and lifecycle costs.
Do not give each resident a separate wallet in this slice. Members share household cash.
Population records currently have no age or life-stage data, so do not invent age bands in
ECON-005. Here, “life cycle” means a household's cumulative path over time: shocks draw down
savings, persistent deficits create hardship, and sustained recovery rebuilds the buffer.
Track daily categories and rolling 7-day/30-day trends:

| Pressure | What it represents | Design behavior |
| --- | --- | --- |
| Essentials | Basic food and hygiene | Keeps Hunger/Hygiene from persistently collapsing; free emergency minimum is possible but poor quality |
| Housing | Residential rent and lease | Existing daily rent transfer and `ArrearsDays` remain authoritative |
| Work capacity | Wages, skill, attendance, commute | Outside employment pays less; purpose/energy/hunger affect absence and future earnings |
| Lifecycle and shocks | Job loss, illness, care, education, other life events | Deterministic bounded event cost; defer age-specific billing until population data supports it |
| Resilience | Liquid cash buffer | Savings absorb missed shifts and one-off costs; no parallel debt score |

If lifecycle events are added, use the existing seeded event stream and bound their cost.
Do not add repeated surprise bills or an ungrounded age multiplier. Calibrate event costs
against household size and wages so a normally employed household can remain solvent.

Quality has tiers. An essential baseline can still satisfy needs at low cost or through
public provision; better food, private care, transit convenience, and other quality services
cost more and improve recovery, wellbeing, or productivity. This means the tower can prevent
destitution without making high-quality provision free. Outside service purchases use the
same need restoration as comparable tower services, but have less choice, lower quality, or
higher total household burden. Prices should be calibrated from the ledger, not duplicated
inside the needs system.

## Income, services, and ledger boundary

Use an aggregate `OutsideMarketState` as the counterparty for outside wages, food, care, and
other priced services. It is not a simulated business, room, or NPC population. Each
transaction moves equal and opposite integer cash:

| Event | Household | Counterparty | Initial proposal |
| --- | ---: | ---: | ---: |
| Outside worker-day | +18 | -18 | Lower than existing tower wage rates of 30–45/day |
| Essential outside meal | available cash paid; unpaid remainder explicitly credited | merchant receives face value | Price 8; outside-market access costs more than the current tower diner ticket of 6 |
| Quality/care option | paid from cash | vendor receives payment | Explicitly priced and optional; tune per member/day |

The city-wide accounting invariant is:

```text
Δ treasury + Δ businesses + ΣΔ households + Δ outside market = documented sources/sinks
```

Only existing documented move-in cash and external contracts remain money sources. A
service credit is a household liability matched by a vendor receivable; it is not free cash.
For the first implementation, cap essential-service credit to a small household balance.
When that cap is reached, preserve emergency basic food access through a documented public
relief transfer funded by treasury/subsidy, or allow needs to decline slowly. Never silently
charge past a hidden limit or permanently freeze residents in an eating state. Tune the
essential basket against actual need decay: 18 income minus 12 rent minus one meal at 8
leaves a 2-unit daily deficit. Existing move-in savings absorb a shortfall; sustained outside
employment without tower services drains the buffer. Persistent negative 30-day flow first
wears the room; when net financial position is also non-positive it moves the lease into a
recoverable notice, even if rent itself is still current. Rent arrears remain a separate,
faster-to-explain default cause. Tower jobs and service access build a stronger reserve.
These are starting figures, not evidence of long-run balance.

Pay outside wages in the existing daily settlement for residents assigned
`WorkplaceLocation.Outside`. A future shift/absence model may prorate pay; until then, pay
once per employed worker-day. Do not pay again on trip arrival. Outside meal spending occurs
once on completed Food-trip arrival, with an idempotent trip transaction key. The daily budget
window closes at the settlement boundary before its counters reset, so trip costs and unmet-
essential exposure roll into the same period as wages and rent. Critical hunger may interrupt
a committed outside shift; residents resume it after eating and return when the minimum work
episode ends. Rejected purchases use a persisted retry delay so residents do not cycle through
rejected trips every tick.

## Need satisfaction and tower services

Separate **need restoration** from **who paid whom**. A Food trip, whether served by a tower
diner or the outside endpoint, restores hunger consistently. The visit can have essential
and quality tiers, but it resolves to the same stable activity state and daily budget rules.
Avoid making the household ledger decide whether the resident is hungry; needs remain in
`ResidentNeedsSystem` and purchases record economic flows only.

If an inside service room is staffed, its existing business capacity and staffing rules
remain in force. Do not let residents become staff simply because they visit. A diner,
security post, clinic, or office has one or more actual jobs; residents are assigned to those
positions under current capacity rules. Outside residents meet needs at outside market
counterparties without creating fake tower staff or increasing an in-tower business's
customer/staff count.

## Long-term hardship and recovery

Track household cash, rolling 7-day/30-day net flow, essential shortfall, reserve, and
housing arrears separately. `HouseholdRecord.ArrearsDays` currently increments when total
cash is negative; before tying room damage or move-out to it, record rent due/paid and
rent-specific arrears. Food credit can make cash negative while rent is current; that is cash
stress, not rent default. A rolling underprovision exposure may represent long-term effects:
raise it gradually under persistent shortfall and clear it more slowly during recovery.

| State | Entry guide | Existing game connection | Recovery |
| --- | --- | --- | --- |
| Stable | Cash buffer covers about 30 days of essentials | Normal room and wellbeing | Save or use policy as desired |
| Tight | Buffer below 14 days or negative rolling net for 7 days | Inspector explains income/spending drivers; mild wellbeing pressure | Better wages, lower rent, public essentials, or improved access |
| Strained | Essential shortfall persists or rent arrears reach 7 days | Deferred upkeep/quality only after sustained housing shortfall; wellbeing and grievance pressure | Positive net rebuilds reserve; paid rent clears rent arrears |
| Lease risk | Existing >30-day rent arrears trigger | Preserve current grievance/strain behavior and Tenant Union response | Recovery before departure cancels default |
| Departure pending | 45 consecutive days of rent default, with seven-day notice | Resident lifecycle sends household to typed `Outside`; lease releases after last arrival | Cancel after sustained solvent/rent-current recovery, not a one-day fluctuation |

The exact shortfall state thresholds are tuning defaults, not extra currencies. Do not make
financial hardship directly lower wages in the same daily step that it lowers service
quality; that can create an unrecoverable spiral. Instead, route consequences through
existing needs and wellbeing gradually: underprovision reduces wellbeing, hunger/energy can
reduce work attendance if attendance becomes modeled, and grievance changes faction affinity.
Provide a public essential floor or subsidy policy so at least one systems-level response
can interrupt a downward spiral. Rent decrees, transit subsidy, business placement/staffing,
job training, and service capacity are candidate existing levers.

Room appearance is a projection, not another mutable debt system. Deferred upkeep may alter
room presentation and explain the cause in the inspector. A departed tenant's arrears do
not permanently damage a re-leased room. Room services should reflect actual operational
availability and capacity, not merely the tenant's cash balance.

## Connection to current systems

- `HouseholdRecord`: cash, daily ledger totals, rent-specific arrears, and explainable rolling
  net/buffer/essential-shortfall projections. Keep one wallet per household.
- `TowerSimulation` daily settlement: outside payroll and market settlement in the existing
  deterministic sequence; stable ID ordering and save/load of all state.
- `ResidentNeedsSystem` and `ScheduleTripGenerator`: independent need restoration, completion
  of outside Food trips, no terminal eating/work activities, and one purchase per trip.
- `BusinessState`: existing tower business staffing, payroll, capacity, and service transfers;
  outside transactions never masquerade as tower business revenue.
- `ResidentWellbeingSystem` and inspector snapshots: expose cash stress and its contributors;
  preserve arrears grievance after 30 days and its existing recovery behavior.
- `FactionState`: prolonged arrears already contribute to Tenant Union affinity; use that
  pressure only after the hardship is visible and explainable.
- Household lifecycle/leasing: staged, recoverable departure, typed Outside trip, release
  room only after all household members leave, preserve history and stable identity.
- Save data and immutable projections: persist state in domain aggregates; keep Unity views
  out of saves and present income, essentials, rent, net trend, reserve, arrears, room
  condition, and the cause/action chain in projections.

The existing economy models the tower's treasury, household cash, business accounts, rent,
and policy. ECON-005 should extend those ledgers; it should not create a competing economic
simulation or an unrelated needs system. The current economy docs specify 200 move-in cash,
residential rent of 12/person/day, tower diner tickets of 6, and tower wages of 30–45/day.
Treat outside wages of 18/day and an essential outside meal of 8 as initial tuning values only. Set essential-basket and
quality-service prices after running household archetypes through 30-, 180-, and 365-day
scenarios. Defer age-based care prices until life-stage data exists.

## Acceptance and balancing review

- Outside workers are paid once per day; outside meals and care debit once on completion;
  the full ledger conserves cash across tower and outside counterparties.
- Essential needs can be met without a staffed interior diner, and visits resolve without
  trapping residents in eating or work activities.
- Tower business job capacity and payroll remain distinct from resident visits and outside
  services.
- Validate the single-worker household for 30, 180, and 365 days, a two-earner household for
  30 days, and a temporary one-week income shock. EditMode tests confirm reserve drawdown,
  arrears, the 45-day notice plus seven-day move-out window, rent recovery, and bounded
  departure history.
- At the initial tuning values, a one-worker/one-member household with 200 starting cash loses
  2/day while paying rent and buying outside food. It remains rent-current for the first 30 days;
  sustained pressure later creates rent arrears, room deterioration, and departure. A temporary
  shock draws savings without creating immediate lease default. These tests cover budget flows;
  live need values and faction deltas remain outputs of their existing systems and are not
  claimed as outcomes from the budget scenarios.
- A brief shock can be recovered from; chronic deficit worsens room/service quality and
  wellbeing, can create lease risk and eventual departure, but policy, work, or public
  essentials can reverse the trajectory.
- A household that recovers before departure cancels the move-out; a completed departure
  occurs once, releases the room after all members arrive Outside, and retains history.
- Save/load preserves daily flows, cash/credit, rolling hardship, pending
  departure, and in-progress trip settlement exactly.

These scenario runs are part of ECON-005 balancing and validation, not a license to add a
high-frequency per-resident economic loop. Daily aggregate work should remain O(residents +
rooms) and deterministic.
