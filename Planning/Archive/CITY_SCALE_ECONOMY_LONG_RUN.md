# City-scale economy long-run stress simulation — 2026-09-28

## Method

`CityScaleEconomyLongRunTests` runs six 365-day scenarios through the production
household, business, treasury, and Outside ledgers. Each has 30 floor slabs, 100
three-person households, 300 persistent residents, and 15 business rooms (four diners,
two shops, one clinic, four offices, two workshops, two security stations). Productive
capacity supports 76 tower workers; the other 224 work Outside. Households start with
200 cash, businesses with 500 each, and the treasury with 50,000. Daily settlement
includes payroll, Outside wages, rent, optional Outside meals, sales/contracts, taxes,
operating costs, and 64 utility/elevator upkeep. All 2,190 scenario-days reconcile
cash, including explicit replacement capital and debt write-offs if they occur.

This is a fixed-population **economic stress model**, not a complete City Status
campaign. It calls production settlement methods but does not advance transit, needs,
leasing, move-outs, utilities, factions, or 1,440 ticks per day. The floor/room layout
is valid ledger input, but has no portals or utility rooms. Meal frequency and business
output shock are controlled scenario inputs. Thus resident wellbeing, service access,
actual city growth, and the 4 ms tick / 60 FPS budgets remain unmeasured here.

## Results after gap fixes

| Scenario | Day 30 margin | Day 365 margin | Insolvent at day 365 | Tower-job households without tower wage at day 365 | Cash-poor households at day 365 | Unmet Outside meals |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Neutral, no Outside meals | +28,800 | +350,400 | 0/15 | 0/100 | 0/100 | 0 |
| Neutral, half of Outside workers buy daily meals | +28,800 | +350,400 | 0/15 | 0/100 | 0/100 | 0 |
| Neutral, all Outside workers buy daily meals | +28,800 | +330,015 | 0/15 | 0/100 | 24/100 | 0 |
| Sustained 30% output loss, no Outside meals | −3,314 | +27,909 | 14/15 | 71/100 | 0/100 | 0 |
| 30-day 30% output loss, then recovery | +28,800 | +317,730 | 0/15 | 0/100 | 0/100 | 0 |
| 1.3× rent, 20% tax, quiet hours | +304 | +65,921 | 10/15 | 50/100 | 0/100 | 0 |

Margin is cumulative revenue less payroll, rent, tax, and operating cost for all
business rooms. It is not a welfare measure. Under sustained shock, failed tenants
stop incurring operating costs after seven insolvency days. The few survivors can
therefore make aggregate margin positive by day 365 even while 14/15 rooms are
insolvent and 71 households have a tower job assignment with no tower wage. The
high-policy case has the same service-capacity warning: positive aggregate margin
coexists with 10 failed rooms. At the end of the temporary shock (day 60), seven
households have an assigned tower worker without a tower wage; at day 365 all have
recovered. All cases retained 300 residents by construction and showed no rent
arrears at sampled checkpoints. No case triggered re-leasing in this fixed fixture.

## Changes motivated by the first measurement

- Outside essential food now spends household cash without consuming the separate
  tower walk-in allowance. The all-meal case formerly had seven failed walk-in
  businesses and 6,706 rejected meals. All 15 survive and meals succeed after the
  change, although 24 households reach zero cash by day 365. Cash-poor residents
  are still a meaningful fragility signal.
- New residents choose funded productive jobs; existing Outside workers can fill
  openings even when apartments are full. Old overfilled or invalid tower rosters
  migrate to Outside assignments. A temporary demand shock does not erase a valid
  job assignment, allowing work to resume on recovery.
- Insolvent tenants become dormant at seven days. Replacement needs a projected
  nonnegative daily margin at current demand, policy, wages, rent, and operating
  cost. Re-leasing records outside opening capital and retired debt explicitly in
  the conserved ledger and persists cumulative lifecycle totals. Focused tests
  verify vacancy under a persistent shock and re-leasing after recovery. The
  viability gate prevents repeated capital injections into an unchanged loss.

## Next measurement

The OR-1003 full-tick City Status fixture should connect this ledger to autonomous
move-ins, re-hiring after closure, utility supply, resident wellbeing, service
coverage, faction response, save/load determinism, and performance. The fixed
model cannot establish 30 days of sustained City Status or whether the current
rates produce a healthy population during growth.

## Validation

- Six 365-day scenarios: 6 passed, 0 failed; daily cash reconciliation passed
  (`/tmp/one-roof-economy-recovery-city-final.xml`).
- Full integrated EditMode: 714 passed, 0 failed, 1 skipped, 715 total
  (`/tmp/one-roof-economy-recovery-full-rerun.xml`).
