# ECON-006–008 — Livelihoods, City-scale margins, and recovery

## Implemented

- Business opening capital is 500 explicit outside units. Productive, funded staff
  alone create output; diner/workshop/security rates and 24/day Outside wages cover
  baseline livelihoods. Invalid `workplace:` leasing was removed (ECON-006).
- A fixed-population, 30-floor / 300-resident economy fixture runs six 365-day
  scenarios, reports margins and hardship indicators, and reconciles cash daily.
  See `Planning/CITY_SCALE_ECONOMY_LONG_RUN.md` for results and limits (ECON-007).
- Outside essential meals spend household cash without consuming the separate
  tower walk-in allowance. Arrivals take only funded productive tower positions.
  Existing Outside residents can fill positions when a later business opens or
  a tenant re-leases, even if every home is occupied. Legacy excess rosters are
  reassigned Outside. Initial tenant creation preserves existing Outside jobs.
- After seven insolvent days, a failed tenant stops operations. Replacement
  requires a nonnegative projected margin at current demand and policy. Explicit
  replacement capital, debt write-off, and retired cash counters persist through
  save/load and enter conservation proofs (ECON-008).

## Validation

- Unity 6000.3.24f1, isolated worktree
  `/home/geisha/Vibecode/UnityAI/one-roof-economy-recovery-validation`, scoped
  Domain code and tests copied from the working checkout. Initial compile exit 0
  (`/tmp/one-roof-economy-recovery-compile.log`) before the final targeted edits;
  final test runs recompiled the changed scripts without compilation errors.
- Command: `timeout 1500 Unity -batchmode -nographics -projectPath
  /home/geisha/Vibecode/UnityAI/one-roof-economy-recovery-validation -runTests
  -testPlatform EditMode -testFilter OneRoof.Domain.Tests.EditMode.CityScaleEconomyLongRunTests
  -testResults /tmp/one-roof-economy-recovery-city-final.xml -logFile
  /tmp/one-roof-economy-recovery-city-final.log`; exit 0, 6 passed / 0 failed.
- A first integrated full run found three Outside-commute regressions from
  matching Outside workers during initial tenant creation. The startup guard
  fixed them: filtered TowerSimulation suite exit 0, 20 passed / 0 failed
  (`/tmp/one-roof-economy-recovery-simulation-rerun.xml`).
- Full EditMode rerun with the guard: exit 0, 714 passed / 0 failed / 1 skipped,
  715 total (`/tmp/one-roof-economy-recovery-full-rerun.xml`,
  `/tmp/one-roof-economy-recovery-full-rerun.log`). `git diff --check` passed.
- The isolated validation worktree was removed after these runs.
- No PlayMode run: only pure Domain logic, DTOs, and EditMode fixtures changed.
  Unrelated presentation/art work in the shared checkout was untouched.

## Measured balance and remaining risk

- Neutral demand keeps 15/15 businesses solvent for a year with no unpaid tower
  assignments. Daily Outside meals leave 24/100 households at zero cash at day 365.
- A 30-day, 30% output loss recovers to 15/15 solvent businesses, whereas a
  persistent 30% loss leaves 14/15 insolvent. The gate prevents repetitive
  recapitalization under that sustained loss. Aggregate margin can turn positive
  after closures and must not be read as service recovery.
- The fixture fixes population at 300 and bypasses the full-tick systems; City
  Status, wellbeing, service coverage, growth, and the <4 ms tick / 60 FPS budgets
  need the OR-1003 acceptance fixture. Recruitment after a later tenant opening is
  covered by focused tests, but not yet a full City Status campaign.

## Next safe action

Build the OR-1003 full-tick City Status scenario and measure 30 sustained days,
including service coverage, household cash, closure/re-hiring, and tick budget.
