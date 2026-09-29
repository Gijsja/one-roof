# Economy — Closed-Loop Unit Economics (v0.10)

This document specifies the authoritative closed-loop unit economics, cash settlement, and policy decree system in One Roof.
Companion to [`Docs/02_ARCHITECTURE.md`](./02_ARCHITECTURE.md) (Domain purity, snapshot rules), [`Docs/03_DATA_CONTRACTS.md`](./03_DATA_CONTRACTS.md) (records), and [`Docs/09_CAUSE_CHAIN_INSPECTOR.md`](./09_CAUSE_CHAIN_INSPECTOR.md) (symptom → overlay → cause → response).

---

## 1. System Overview

One Roof implements a strictly conserved, closed-loop integer economy verified across multi-year deterministic scenarios:
- **One Currency**: All ledgers are integer `long` cash units. Normalized `0–1` budget tiers exist solely as derived inspector projections, never stored state.
- **Closed Loop with Explicit Sources/Sinks**: Cash is conserved across `treasury + Σhouseholds + Σbusinesses + OutsideMarketState`.
  - *Explicit sinks*: Construction costs, equipment upkeep, demolition salvage, business operating costs.
  - *Explicit sources*: Starting household cash (200), business opening capital (500), outside employee wages (24/day), and outside contracts.
- **Systems-Only Control (ADR-049)**: The Steward influences economics via room zoning, capacity additions, leasing policies, and 4 policy decrees (Rent Cap, Commercial Tax, Transit Subsidy, Quiet Hours). The Steward never orders an individual resident or business.
- **Deterministic & Allocation-Free**: Daily settlement executes at `tick % 1440 == 0` with zero steady-state heap allocations.

---

## 2. Ledgers & Accounts

| Ledger | Entity Record | State Fields | Notes |
|---|---|---|---|
| **Treasury** | `TowerEconomyState` | `CashBalance`, `TotalRevenue`, `TotalExpenses`, `DailyGrossRent`, `DailyGrossTax`, `DailyUpkeep` | Funds building construction and subsidies; collects rent and taxes. Emits immutable `TreasuryFlowProjection`. |
| **Household** | `HouseholdRecord` | `CashBalance`, `ArrearsDays`, `RollingBudgetWindow` | Receives wages (tower or outside); pays rent and food/services. Arrears accumulate if cash < 0. |
| **Business** | `BusinessRecord` | `CashBalance`, `LastCustomerRevenue`, `LastWages`, `LastRentPaid`, `LastTaxPaid`, `ArrearsDays` | Insolvency threshold is `-100`. Insolvent tenants pause after 7 days and re-lease only if projected margin is non-negative. |
| **Outside Market** | `OutsideMarketState` | `ContractFundedWages`, `ServiceReceipts`, `BoundedCredit` | Aggregate external counterparty providing outside employment wages and essential meals. |

---

## 3. Daily Settlement Lifecycle (`tick % 1440 == 0`)

`TowerSimulation.AdvanceOneTick` executes daily settlement in deterministic room-ID then household-ID order:

1. **Payroll**: Solvent businesses pay `wageRate[role]` per active worker to their household cash. Walk-in activity is capped to staffed slots supported by occupancy demand. If business cash would breach `-100`, payroll is unpaid, marking wage arrears and halting output.
2. **Outside Wages**: Residents employed Outside receive 24 cash/day from `OutsideMarketState`.
3. **Household Spend**:
   - **Residential Rent**: Deducted from household cash and credited to tower treasury (`12/resident × rentMultiplier`).
   - **Food / Services**: Essential meal spend (5/resident) transfers to open walk-in businesses (diner, clinic). Outside meals draw from household cash without consuming tower customer allowances.
4. **Business Settlement**:
   - Commercial tenants collect customer and contract revenue.
   - Businesses pay rent (`8/cell × rentMultiplier` for office/retail; `5/cell` for diner/clinic/workshop/security) and commercial tax to treasury.
   - Operating costs (35/day) deducted.
5. **Treasury Settlement**:
   - Aggregates rent, tax, utility upkeep (`1/utility-cell + 2/elevator-car`), and active subsidies.
   - Emits immutable `TreasuryFlowProjection`.
6. **Delinquency & Insolvency**:
   - Households with cash < 0 increment `ArrearsDays`. `ArrearsDays > 30` creates grievances, strain, and potential recoverable move-out notice.
   - Businesses with cash < -100 become `IsInsolvent`. Insolvent rooms stop paying rent and appear on Overlay 6. After 7 days, operations halt until re-leased at a viable margin.

---

## 4. Business Revenue Model

 Headcount alone cannot print revenue; revenue is capped by staffed capacity and demand:
- **Walk-in Archetypes** (diner, retail, clinic):
  - `customers = min(paid active staff × serveRate, demandCap)`
  - `revenue = customers × ticket`
  - `serveRate = 8/day`; tickets: diner 10, retail 8, clinic 10.
  - `demandCap = 4 × roomCapacity × occupancyFactor` (where `occupancyFactor = residents / totalResidentialCapacity`).
- **Contract Archetypes** (office, workshop, security):
  - `revenue = staff × contractRate × occupancyFactor`
  - `contractRate`: office 55, workshop 60, security 65.
- **Wages per active employee per day**: Service 30, Maintenance 35, Security 35, Knowledge 45.

---

## 5. Policy Decrees (`PolicyDecreeState`)

Managed via the Steward Manage Mode panel (`PolicyDecreePanelView`):

| Decree | Settings | Direct Effect | Systems Tradeoff |
|---|---|---|---|
| **Rent Cap** | `0.7x / 1.0x / 1.3x` | Scales residential & commercial rent multipliers | Low rent boosts satisfaction and reduces Scrutiny but lowers treasury revenue. High rent strains households and drives radicalization. |
| **Commercial Tax** | `0% / 10% / 20%` | Percent tax on gross business revenue | Treasury inflow. High rate triggers Merchant Guild grievance and business insolvency risk. |
| **Transit Subsidy** | Off / `40/day` | Treasury expense | Adds `+0.1` commute satisfaction and suppresses transit grievances. |
| **Quiet Hours** | Off / On | Lowers noise grievance accumulation | Reduces walk-in business customer throughput by 10%. |

All decree changes validate via `TowerSimulation.CanExecute(ICommand)`, emit domain events, and adjust `ScrutinyState` when aggressive policies are enacted.
