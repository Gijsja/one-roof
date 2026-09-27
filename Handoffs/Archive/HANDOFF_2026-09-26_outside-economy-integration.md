# Handoff: Outside Economy and Resident Integration

**Status:** Complete
**Date:** 2026-09-26
**Scope:** ECON-005 follow-through across outside work/food trips, budget windows, household hardship, and resident presentation.

## Delivered

- Outside workers receive a deterministic daily wage from the aggregate outside-market counterparty. Completed outside Food trips charge the household once; accepted meals restore Hunger, while exhausted essential credit records a shortfall and persists a retry delay.
- Critical Hunger can interrupt an outside work episode. The resident resumes work after eating and leaves for home when the minimum outside-work episode ends. Food trips to an interior diner remain in the tower business ledger.
- Daily household budget windows close before their counters reset. The close includes wages/rent from the period and trip/service purchases or essential shortfalls made during it. Partial-window state already persists through save/load.
- Persistent negative 30-day flow, non-positive net financial position, and unmet-essential exposure feed room condition and recoverable lease notice. Wellbeing, grievances, satisfaction, and faction pressure consume those resident condition projections; move-out remains typed, recoverable, and saved.
- The resident collage is replaced by six coherent full-body atlas sprites. Content IDs map deterministically to atlas cells; sprites match the intended 0.28×0.58m bounds. Walk and sit apply visible whole-body motion rather than animating hidden cutout limbs.

Outside service providers remain an aggregate counterparty for this slice. Individual street businesses and their commercial tenants are not simulated. Tower businesses retain their own cash and job-capacity systems.

## Validation

Isolated project copy: `/tmp/one-roof-outside-economy-validation` (created with `rsync`, excluding `.git`, `Library`, `Temp`, `Logs`, `obj`, and `Build`). Unity 6000.3.24f1 was run headless against the copy.

| Command | Exit | Result |
| --- | ---: | --- |
| `timeout 1200 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-outside-economy-validation -runTests -testPlatform EditMode -testResults /tmp/one-roof-outside-editmode-r2.xml -logFile /tmp/one-roof-outside-editmode-r2.log` | 0 | 673/673 EditMode passed |
| `timeout 1200 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-outside-economy-validation -runTests -testPlatform PlayMode -testResults /tmp/one-roof-outside-playmode.xml -logFile /tmp/one-roof-outside-playmode.log` | 0 | 8/8 PlayMode passed |
| `git diff --check` | 0 | Clean |

The initial EditMode run passed 668/673 and exposed five issues in work/meal timing, a lifecycle fixture, and a pose expectation. Those were fixed; the final EditMode rerun passed all 673 tests. A standalone `-quit` compile attempt was stopped during first-time package import; both final test runs completed successfully and included editor compilation.

## Connected Backlog Systems

- **ECON-004:** reuses household cash, settlement, and explanation projections.
- **OR-1004:** uses the typed `Outside` endpoint and lobby/street-edge transit route.
- **OR-602 / OR-901:** hardship changes wellbeing and grievance inputs used by resident satisfaction and faction pressure.
- **OR-702:** interior businesses retain their established jobs, wages, customer capacity, and business ledgers; outside employment uses the outside counterparty.
- **Resident housing lifecycle:** sustained insolvency and rent arrears can trigger recoverable notice, typed move-out, room release, and bounded departure history.

## Risks and Next Step

Prices and thresholds are initial tuning values. The next safe step is to balance outside wage/meal/rent scenarios against the 30/180/365-day hardship fixtures, then observe whether interventions through existing rent, staffing, service-capacity, and subsidy systems produce recovery without resident-level orders.
