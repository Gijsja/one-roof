# ECON-001–004 — Closed-loop economy closeout

## Result

Daily settlement now runs payroll, residential rent, business service and contract settlement, treasury expenses, then arrears evaluation. Household, business, and treasury ledgers reconcile over a 30-day deterministic fixture. Prolonged household arrears feed a lease-risk grievance, added strain, and an inspector warning. The existing policy commands, save migration, business insolvency, and economic projections remain in place.

## Validation

- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-economy-validation` at `cebafc2` plus the scoped economy patch; removed after validation. Unrelated shared-checkout presentation edits were excluded.
- Unity 6000.3.24f1 headless compile: `Unity -batchmode -nographics -quit -projectPath <worktree> -logFile /tmp/one-roof-economy-compile.log` → exit 0, no C# compiler errors.
- Focused EditMode: `timeout 1500 Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testFilter TowerTreasuryAndPolicyTests -testResults /tmp/one-roof-economy-targeted-final.xml -logFile /tmp/one-roof-economy-targeted-final.log` → exit 0, 10/10 passed.
- Full EditMode with the final inspector change: `timeout 1500 Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testResults /tmp/one-roof-economy-final-editmode.xml -logFile /tmp/one-roof-economy-final-editmode.log` → exit 0, **523/523 passed**, 0 failed/skipped.
- Scoped `git diff --check` → exit 0.

## Scope boundary

Walk-in spending uses the documented aggregate occupancy and staffed-capacity proxy, capped by actual household cash and `$5/resident/day`. Resident visit records are not available in this slice. The faction affinity response and actual move-out decision require OR-901 and later resident lifecycle work; the economy now exposes the delinquency signal and immediate wellbeing consequences.

## Next safe action

Continue the separate stabilization roadmap from its handoff. Preserve unrelated presentation files in the shared checkout.

## 2026-09-24 quality follow-up

- Arrears add their extra strain once per settlement day. Ordinary wellbeing updates remain on their existing tick cadence, with base strain and recovery scaled by the fraction of a settlement day represented by each tick.
- Rent burden uses the greater of rent-to-income and reserve pressure when income exists; before the first paycheck it uses reserve pressure. The first full run with unconditional no-income pressure failed `ScrutinyStateTests.UntouchedMorningRush_NeverSaturatesScrutiny` (524/525, exit 2); this correction restored the ordinary morning-rush contract.
- Walk-in sales draw proportionally from eligible household daily spending capacity, with stable ID ordering only for integer remainders. The inspector describes the present stress effect without promising an unimplemented move-out.
- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-economy-quality-validation` at `cebafc2` plus scoped economy changes; removed after validation. Unity 6000.3.24f1 headless compile (`-batchmode -nographics -quit`, log `/tmp/one-roof-economy-quality-compile.log`) exited 0.
- Focused `TowerTreasuryAndPolicyTests`: `timeout 1500 Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testFilter TowerTreasuryAndPolicyTests -testResults /tmp/one-roof-economy-quality-targeted.xml -logFile /tmp/one-roof-economy-quality-targeted.log` → exit 0, 12/12 passed.
- Final full EditMode: `timeout 1500 Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testResults /tmp/one-roof-economy-quality-final-editmode.xml -logFile /tmp/one-roof-economy-quality-final-editmode.log` → exit 0, **525/525 passed**, 0 failed/skipped. Scoped `git diff --check` exited 0.
- Remaining design limit: walk-in demand is still an aggregate occupancy proxy, and actual move-out/faction responses remain later planned work.

### Final strain-cadence verification

- A further regression check advanced 300 ticks with prolonged arrears and confirmed strain stayed below 0.05 instead of saturating within hours. The economy fixture also uses the injected settlement period to scale one tick's share of a day.
- Final isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-economy-strain-validation` at `cebafc2` plus scoped economy changes; removed after validation. Unity 6000.3.24f1 `-batchmode -nographics -quit` compile → exit 0, log `/tmp/one-roof-economy-strain-compile.log`.
- Full EditMode: `timeout 1500 Unity -batchmode -nographics -projectPath <worktree> -runTests -testPlatform EditMode -testResults /tmp/one-roof-economy-strain-editmode.xml -logFile /tmp/one-roof-economy-strain-editmode.log` → exit 0, **525/525 passed**, 0 failed/skipped.
