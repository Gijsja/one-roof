# Economy — Closed-Loop Unit Economics

Canonical: money, settlement, and policy levers. Companion to `Docs/02_ARCHITECTURE.md`
(Domain purity, snapshot rule), `Docs/03_DATA_CONTRACTS.md` (records), and
`Docs/09_CAUSE_CHAIN_INSPECTOR.md` (symptom → overlay → cause → response).

## 1. Problem statement

Baseline issues identified before the ECON implementation (the remaining acceptance gaps are tracked in `Planning/BACKLOG.md` and the active handoff):

1. **Time-base incoherence.** `DailySchedule.TicksPerDay = 1440`, but rent and business
   cycles settle every 50 ticks (~29 paychecks per day).
2. **Two currencies.** Treasury is `long` cash; `HouseholdRecord.Budget` is a `0–1` float.
   Wages (`AdjustBudget(+0.01)`) and rent (treasury `+25/resident`) never meet.
3. **Free rent.** `ProcessRentCycle` mints treasury revenue without deducting households.
4. **Isolated businesses.** `BusinessRecord` cash never pays rent or tax to the treasury;
   revenue is a fixed `40/employee`, so hiring more staff magically prints revenue.
5. **Flat commercial rent.** `200/room/cycle` regardless of size, archetype, or solvency.
6. **No sinks beyond construction.** No upkeep, no policy costs.

The shared worktree now contains the household, business, policy, and treasury-flow implementation described below. It remains in progress: the 30-day conservation proof and arrears consequence chain are not complete, and the runtime settlement order still differs from the target order in §5.

## 2. Principles

- **One currency.** All ledgers are `long` cash units. Normalized `0–1` budget tiers survive
  only as derived inspector projections, never as stored state.
- **Closed loop with explicit sources/sinks.** Cash is conserved across
  `treasury + Σhouseholds + Σbusinesses` except: construction/upkeep/operating (sinks),
  new-household starting cash and office contract revenue (sources), walk-in customer
  spend (internal household → business transfer).
- **Systems-only control (ADR-049).** The Steward sets zone-level rent, tax, and subsidy
  policy; never orders an individual to pay, work, or vacate. Collection is automatic;
  consequences flow through satisfaction, grievances, strain, and lease default.
- **Explainable (ADR-055, ADR-061-contract).** Every adverse balance exposes contributors
  through immutable projections to Overlay 4/6 and the inspector cards. No recompute in
  presentation.
- **Deterministic, allocation-free, aggregation-safe.** Integer math, tick-aligned
  settlement, O(residents + rooms) per day, no per-trip scans in the hot path.

## 3. Time base

- **Settlement period = 1 day = 1440 ticks** (`DailySchedule.TicksPerDay`).
  `TowerSimulation.AdvanceOneTick` settles economy when `tick % 1440 == 0`.
- Business reconciliation (`Businesses.Advance`, staffing) stays on the existing 10-tick
  cadence; only **cash settlement** moves to daily.
- EditMode speed: tests use a `SettlementPeriod` hook (default 1440, tests override to 50).
  Existing `TowerEconomyAndLeasingTests` expectations are rebased to per-day rates.

## 4. Ledgers

| Ledger | Stored state | Notes |
| --- | --- | --- |
| Treasury (`TowerEconomyState`) | `cashBalance`, `totalRevenue`, `totalExpenses` (existing) + per-day `TreasuryFlowProjection` | Construction pricing unchanged (`CostPer*Cell`, `ElevatorCarCost`) |
| Household | **new** `cashBalance: long` per `HouseholdRecord`; `arrearsDays: int` | `Budget` float retained one release as derived `Clamp(cash / RentReserveTarget)` for save-compat, then removed |
| Business (`BusinessRecord`) | `cashBalance`, `lastCustomerRevenue`, `lastWages` (existing) + **new** `lastRentPaid`, `lastTaxPaid`, `arrearsDays` | Insolvency threshold stays `-100` |

`Budget` derivation (projection only): `RentReserveTarget = 30 × dailyRent(household)`.
Inspector shows tier (destitute/struggling/stable/affluent), never the raw float as cause.

## 5. Daily settlement order (deterministic, room-ID then household-ID order)

1. **Payroll.** Each solvent business pays `wageRate[role]/employee` to the employee's
   household cash. Unpaid if business cash would breach `-100` → wage arrears flag.
2. **Household spend.** Rent ( §6 ) → treasury. Food/service (`5/resident`, only if a
   diner or clinic served the tower that day, else needs decay per `ResidentNeedsSystem`)
   → split across open walk-in businesses proportional to staffed capacity (internal transfer).
3. **Business settlement.** Customer/contract revenue ( §7 ) → business cash.
   Business pays rent ( §6 ) + tax ( §8 ) → treasury. Subtract operating (`35`, existing).
4. **Treasury settlement.** Add tax/rent receipts, subtract upkeep ( §6 ) and active
   subsidies ( §8 ). Record `TreasuryFlowProjection{rent, tax, upkeep, subsidy, construction,
   constructionSalvage}`.
5. **Delinquency.** Household cash `< 0` → `arrearsDays++`, else decay to 0.
   `arrearsDays > 30` → grievance + strain driver + move-out risk + Tenant-Union affinity
   hook (OR-901). Business cash `< -100` → `IsInsolvent` (existing); insolvent rooms stop
   paying rent, are flagged for re-lease after 7 days, and appear on Overlay 6.

**Current runtime ordering gap:** `TowerSimulation.AdvanceOneTick` currently calls the combined
business cycle (payroll, household walk-in spend, business revenue/rent/tax/operating cost)
before collecting household residential rent. The sequence above remains the target contract;
the implementation must be reconciled before the ECON slice is marked complete. Walk-in
spending is limited to `$5/resident/day` and apportioned by staffed share across walk-in
businesses, using occupancy-derived demand rather than recorded resident visits.

## 6. Rates (per day)

| Flow | Rate | Rationale |
| --- | --- | --- |
| Residential rent | `12/resident × rentMultiplier` | Family of 3 ≈ 36/day vs dual income ≈ 60/day ( §7 wages) |
| Commercial rent | `8/cell × rentMultiplier` (office/retail/diner); `5/cell` (clinic/workshop/security, subsidized service) | 8-cell office = 64/day; replaces flat 200/room/50-ticks |
| Household food/service | `5/resident` (transfer to walk-ins) | Closes the walk-in loop; absence degrades needs instead |
| Utility upkeep (treasury sink) | `1/utility-cell + 2/elevator-car` | Gives height a running cost; links OR-803 wear to cash |
| New household starting cash | `200` | Documented source; bounds move-in demand stimulus |
| Demolish salvage | 50% (unchanged) | Existing behavior preserved |

## 7. Business revenue model (replaces fixed 40/employee)

Two archetypes; both capped by staffed capacity so headcount alone cannot print revenue.

- **Walk-in** (diner, retail, clinic): `customers = min(staff × serveRate, demandCap)`,
  `revenue = customers × ticket`. `serveRate = 8/day`, `ticket`: diner 6, retail 8,
  clinic 10. `demandCap = 4 × roomCapacity × occupancyFactor`, where
  `occupancyFactor = residents / totalResidentialCapacity` (O(1) tower-wide ratio;
  no per-trip scan, aggregation-safe at 300).
- **Contract** (office, workshop, security): `revenue = staff × contractRate × occupancyFactor`,
  `contractRate`: office 55, workshop 40, security 38. External city clients = documented source.

Wages per employee per day: Service 30, Maintenance 35, Security 35, Knowledge 45.
Training (OR-605) therefore raises payroll cost *and* contract eligibility — a real tradeoff.
Example: 4-employee office at full occupancy: revenue 220, wages ~150 (mixed roles),
rent 64, operating 35 → net ≈ −29 without Knowledge staff; with 2 Knowledge (90) + 2 Service
(60): wages 150 — same; margin turns positive on contract mix and occupancy. Tuning lives in
ContentScriptableObjects, never in code constants beyond defaults.

## 8. Policy levers (pre-wires OR-902 decree panel; domain value object `PolicyDecreeState`)

| Decree | Settings | Effect |
| --- | --- | --- |
| Rent cap | `0.7 / 1.0 / 1.3` multiplier | Scales §6 rents; low → satisfaction up, treasury down, scrutiny relief; high → inverse + inequality pressure |
| Commercial tax | `0% / 10% / 20%` of gross | Treasury inflow; high rate → Merchant-Guild grievance, insolvency risk |
| Transit subsidy | off / `40/day` | Costs treasury; adds `+0.1` commute satisfaction and reduces commute grievances |
| Quiet hours | off / on | On: noise grievances down; walk-in revenue −10% (tradeoff) |

All decrees validate through `TowerSimulation.CanExecute` (ARCH-003 seam), emit domain
events, record `ScrutinyState.RecordAggressivePolicy` for extreme settings (1.3 rents,
20% tax), and persist in the save envelope.

## 9. Explanation wiring (Docs/09 contract)

- **Overlay 6 Business Health** (extends `BusinessHealthFloorProjection`, immutable):
  per-tenant `rentPaid, wageBill, margin, arrears/insolvent` + existing solvency badges.
- **Overlay 4 Satisfaction:** `rentBurden` contributor becomes `rent/income` ratio instead of
  raw budget (`ResidentWellbeingSystem.cs:21` replaced by ledger-derived value).
- **New `TreasuryFlowProjection`** (Data mode + treasury card): daily
  `{rent, tax, upkeep, subsidy, construction, constructionSalvage, net}` with deterministic ordering.
- **Resident card:** income / rent / food / net trajectory + arrears countdown.
- **Business card:** revenue / wages / rent / tax / margin + default countdown.
- Every chain ends in a lever: adjust decree, rezone, add capacity, subsidize, or demolish —
  never a dead-end statistic, never an individual order.

## 10. Persistence, determinism, performance

- `EconomySaveData` persists policy settings, settlement tick, pending and last daily flows,
  including construction salvage. `PopulationSaveData` persists the household-ledger version
  and per-household cash, arrears, income, and service spend. `BusinessSaveData` persists
  revenue, payroll, rent, tax, operating cost, arrears, wage arrears, and insolvency state.
  Pre-ledger saves derive household cash from the legacy normalized budget; missing policy and
  treasury-flow fields default to the neutral policy and zero flows (ARCH-004 pattern; cf. ADR-060).
- Integer-only settlement; entities processed in stable ID order; seeded streams untouched.
- Budget: O(residents + rooms) once per 1440 ticks; negligible vs the 4 ms tick budget.

## 11. Sliced implementation status

- **ECON-001 implementation:** household cash, rent deduction, arrears, daily settlement, and legacy-save migration are wired. The 30-day conservation fixture remains outstanding.
- **ECON-002 implementation:** per-cell rent, tax remittance, demand-capped revenue, insolvency, and seven-day re-lease status are wired; validate the business ledger before completion.
- **ECON-003 implementation:** `PolicyDecreeState`, command validation, domain events, scrutiny hooks, and save persistence are wired; the OR-902 decree panel remains separate backlog work.
- **ECON-004 implementation:** treasury and tenant projections, inspector details, resident rent burden, and Data-mode flow display are wired. Golden ledger acceptance (30-day conservation plus arrears-to-grievance/move-out consequence chain) remains outstanding.

See `Planning/BACKLOG.md` for row status and the active handoff for the next safe action. Do not mark an ECON row DONE until its acceptance evidence exists.
