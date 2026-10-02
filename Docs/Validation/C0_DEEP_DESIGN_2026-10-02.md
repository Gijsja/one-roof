# C0 deep design validation — 2026-10-02

**Result: revised design, C0 IN PROGRESS.** The initial DONE claim was withdrawn. This report distinguishes source inspection, actual production Domain execution, isolated source hypotheses and arithmetic models. No new crisis implementation, Unity Editor/test suite, player build, graphical inspection or native performance acceptance was run.

Baseline commit `4760dd60ce766abe021f44c2078450acd5d02551`; runtime source is unchanged by this task. Unrelated ProjectSettings.asset edit preserved. The three subagents produced [civil](./C0_DESIGN_REVIEW/CIVIL.md), [infrastructure](./C0_DESIGN_REVIEW/INFRASTRUCTURE.md) and [external/acceptance](./C0_DESIGN_REVIEW/ACCEPTANCE.md) reviews. The coordinator inspected actual owners, resolved recommendations and ran probes. The corrected [canonical contract](../../Planning/CRISIS_CONTRACTS.md) takes precedence over reviewer recommendations.

## Actual current Domain experiment

[CrisisDesignProbe](../../Tools/DesignValidation/CrisisDesignProbe.cs) compiles the current production Domain sources into a standalone .NET10 executable. No UnityEngine, NUnit substitute or fake new crisis manager is used. It exercises GoldStandardCityFixture.Create, TowerSimulation.AdvanceOneTick, real CanExecute/ExecuteCommand tier upgrades and actual ExportSaveData/RestoreFromSaveData. It does not validate Unity serialization JsonUtility/platform behavior or certify frame budgets.

Two matched reference scenarios start at tick450 with300 Residents/30 floors, then advance7200 ticks. Tier1 is untouched; tier5 uses every existing upgrade command2..5. Current ApplyTier already preserves authored20-capacity cars; the reviewer initially inferred capacity shrink incorrectly, then checked and retracted it. That correction is retained in the review record. Bank lifetime average and completed-positive-wait diagnostics are not daily gate metrics.

| Observation | Tier1 | Tier5 |
|---|---:|---:|
| Minimum Residents |300|300|
| Maximum total queued |78|79|
| Maximum observed landing wait |3289|2369|
| Captured successful terminal trips |5936|6669|
| Captured successes within360 ticks |4513|5722|
| Captured positive-wait samples |1657|2295|
| Captured completed-trip wait p95 |842|391|
| Remaining captured trips |213|215|
| Brownout floor-ticks |6870|6870|
| Unavailable water floor-ticks |6870|6870|
| Save byte equality at7650 |False|False|
| First differing future step |1|1|

Daily boundary treasury increases254084→270420 at tier1 and234084→250420 after tier5's20000 investment. Resident count holds300, average satisfaction remains about.69, scrutiny0. Those facts show financial/wellbeing foundations; they do not pass six-chain or30-day gates. Utility failed-floor ratio is6870/(30*7200)=3.18%, consistent with repair oscillation. It is a floor diagnostic, not the weighted occupied-room gate.

**Limitations:** polling ActiveTrips misses same-tick local/outside completions and unreachable attempts discarded before submission. It only retains observed TripRecord objects until terminal state. This means completion percentages, tails and percentiles are diagnostic, not a complete demand denominator. First interval is partial because the fixture starts450. Instrumentation/snapshot allocations make wall-clock timing unsuitable for certification. A complete all-demand/outcome port remains implementation work.

Durable numerical output: [production probe JSON](./C0_PRODUCTION_PROBE_2026-10-02.json), including sorted production source SHA256 and physical construction receipts.

## Actual physical source feasibility

The probe constructs three supported floors and adjacent width2 riser/chute rooms with14 real validated/executed commands, including actual capacity120 pumps/collection. No RestoreFromData shortcut is used for layout. It then deliberately supplies condition fixtures through the existing aggregate loader; those condition fixtures are declared, not an organically developed incident.

- Selected riser condition.20, failed collection: water unavailable, pressure0.
- Selected riser condition.60, failed collection: water available, pressure.88.
- Condition1, failed collection: water available, pressure.88.

Therefore the old mechanically failed live-contamination source is infeasible in the existing flow model. The revised damaged-operational joint can supply flowing water. Contamination loads/consequences themselves are not present or tested in production yet.

## Save and continuation failure details

Byte equality alone would be insufficient: faction array ordering changes are semantically benign. We inspected field differences rather than declaring all serialized differences behavioral failures.

At7650 a real need value0 becomes1 on load: person2009 Hunger/Energy/Hygiene, among others. PopulationState.FromSaveData uses positive-only fallback (`value>0 ? value : 1`), conflating valid depletion with missing legacy data. This is a concrete behavioral snapshot failure, independent of faction ordering. The first subsequent step also changes routed execution and economy: tier1 active trip counts212 vs155, and outside-market purchase receipts differ by144; e.g household64 cash628 vs620. Transit restore reconstructs planned routes rather than preserving actual reroutes/leg mapping. Generator next ID/cooldowns and RNG state are not in the root contract. These are source-backed risks; the exact causal contribution of each to every divergence still needs focused production regression fixtures.

These are **existing failures**, not regressions from this task: no production runtime files changed. OR-1007 must preserve valid zero needs, actual executed route/leg, generator/retry/RNG state and once-only transaction provenance before claiming deterministic continuation. A90-tick snapshot test cannot certify this7650-tick continuation.

## Arithmetic models and rejected alternatives

[CrisisContractModels.py](../../Tools/DesignValidation/CrisisContractModels.py) executes discriminating Decimal/integer calculations. Its checks passed; they prove arithmetic compatibility of assumptions, not that proposed adapters have been implemented. [Results JSON](./C0_DESIGN_MODELS_2026-10-02.json).

- Rolling rent delay collects109/12/12 versus selected incident hold109/9/9 for initial debt100/rent12. Held debt3/6/9 remains authoritative existing liability, not an extra debt. Low-cash household pays5 in both normal/boycott, so intended withholding must not be presented as realized treasury loss.
- Cap8 under selected hold has adjusted pressure>.35 then<=.35 twice; comparison with declared onset satisfies reduction.20. Raw self-created arrears cannot be used as unadjusted recovery proof.
- Current one-technician repair fails46 of500 ticks; .50-latch alternative still fails10; preventive.70→.80 fails0. Decimal idealization differs from C# float at exact thresholds; production boundaries remain future fixtures.
- Residual100/100 with clean flow first reaches<=10 at600; disconnected residue never clears. Clearance cannot blindly reduce every path simultaneously while upstream remains unsafe.
- Four120-cadence samples at120/240/360/480 span360 elapsed; three samples span240.

## Reproduction

Run from repository root; all generated build/runtime data goes to explicit /tmp paths:

```bash
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisDesignProbe.csproj --configuration Release --artifacts-path /tmp/one-roof-c0-build -- /tmp/one-roof-c0-results
python3 Tools/DesignValidation/CrisisContractModels.py /tmp/one-roof-c0-models.json
```

The standalone probe returns success if current code compiles/runs and construction commands succeed, while explicitly reporting known replay/metric failures in JSON. Exit0 must not be interpreted as all acceptance gates passing. Raw before/after/future saves are emitted to the explicit output directory for deeper comparison; they are fixture data, not user saves.

## Closure and next discriminating work

The revised contract now contains alternative selection/rationale, production scoring and APIs, realistic economic effects, correct operational source geometry, shared availability consequences, saved attribution, exact interval ownership, adversarial delete/replay/composition scenarios and honest evidence. This is substantially deeper than threshold enumeration.

C0 remains open until reference transit attainability is demonstrated or its proposed metric/mechanic is deliberately revised with evidence. Native performance and new six-chain behavior remain downstream gates. The coordinator also runs an isolated source hypothesis to test live-queue wait and deadline-aware stairs without modifying production files; its results and limitations follow below.

## Isolated transit hypothesis and rejected measurement

[TransitHypothesis.py](../../Tools/DesignValidation/TransitHypothesis.py) copies Domain sources to a fresh /tmp directory and changes only the copied TransitExecutionSystem. It queries live passenger wait and admits stair diversion only when remaining route cost fits360 minus elapsed ticks. It preserves the current no-car last-resort fallback and existing capacity20. The repository runtime files are untouched.

The first isolated experiment removed/requeued before rejecting a stair route and repeatedly added the same passenger wait. Its impossible completed-trip p95 values26001/5355 expose a measurement/accounting defect; [v1 output](./C0_TRANSIT_HYPOTHESIS_2026-10-02.json) is preserved as rejected evidence, not used to justify a mechanic. The corrected experiment plans feasibility before queue mutation and adds diverted wait once. This changes the hypothesis, so a rerun was necessary.

| Captured diagnostic | Current tier1 | Hypothesis v2 tier1 | Current tier5 | Hypothesis v2 tier5 |
|---|---:|---:|---:|---:|
| Maximum observed landing wait |3289|809|2369|319|
| Positive-wait trip p95 |842|215|391|87|
| Completed within360 / captured completed |4513/5936|6965/7275|5722/6669|7723/7728|
| Unfinished captured trips |213|206|215|204|
| Unfinished captured older than360 |Not recorded|0|Not recorded|0|

[V2 durable output](./C0_TRANSIT_HYPOTHESIS_V2_2026-10-02.json). The corrected hypothesis strongly improves observed travel: tier5 deadline completion99.94% of captured completed trips. It **still fails** chosen p95<=60 and maximum<=240, and captured successes are not a full demand cohort. No acceptance is claimed. All-demand observation, preboarding maxima, invalid demands and complete pending-cohort finalization remain missing. Need restoration/replay failures persist. This experiment narrows the problem; it does not close C0 or justify relaxing thresholds silently.

The hypothesis repeatedly computes alternate paths, so its execution cost is also a risk. A production solution must cache alternatives by topology revision/origin/destination and avoid retrying an unchanged infeasible alternative every tick. Exact cap and invalidation need a bounded300-Resident measurement. Diagnostic elapsed timings were deliberately removed from durable summaries because this instrumented .NET runner is not native Unity performance certification.

To reproduce the corrected isolated experiment with a fresh directory:

```bash
python3 Tools/DesignValidation/TransitHypothesis.py /tmp/one-roof-c0-transit-hypothesis
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisDesignProbe.csproj --configuration Release --artifacts-path /tmp/one-roof-c0-hypothesis-build -p:DomainSourceRoot=/tmp/one-roof-c0-transit-hypothesis -- /tmp/one-roof-c0-hypothesis-results 'Isolated live-wait/deadline hypothesis; not production acceptance'
```

Current-source compile/run,14 physical command receipts and arithmetic checks succeeded. Known source replay fails and proposed transit feasibility remains unproved; neither is a regression introduced by this design task. The roadmap/handoff stay IN PROGRESS and retain exclusive runtime ownership requirements.
