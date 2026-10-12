# Transit and daily life — baseline and Tasks1–3 checkpoint

Current status: Tasks1–3 and the two narrow integration corrections are independently reviewed and accepted. Final full Unity checkpoint: **1416 passed /0 failed /3 skipped**, runner exit0. The user chose this cutoff; Tasks4–5 and C0 remain open. Historical evidence below is labeled by snapshot.

2026-10-03 rebaseline. HEAD `357e1d9` plus preserved working-tree `CrisisDesignProbe.cs` output-format changes. Three independent source audits and coordinator cross-check; no production code edits in this rebaseline. Superpowers systematic debugging / planning workflow. C0 closure is contradicted by fresh production evidence; full daily-life implementation remains incomplete.

| Finalized cohort | Required episodes | Within360 | Failed | Terminal p95 | Live max | Result |
|---|---:|---:|---:|---:|---:|---|
| Tier1 / day5 |2505|2457 (98.08%)|48|58|557|FAIL completion/max |
| Tier5 / day5 |2647|2631 (99.40%)|16|89|448|FAIL p95/max |

Both cohorts are explicitly day5, complete and valid in the probe output, with zero unresolved unreachable episodes. Both causal saves matched1500 subsequent ticks. The output samples last finalized day after replay; this run explicitly checked the reported day==5. Future validation must capture finalized cohorts at emission. A fixed failed-count gate is insufficient; use the declared success fraction. These results do not reproduce the documented3000/3000,p9549,max206 claim.

Source inspection also finds commuting-boundary observation removed and null-arbitration local success credited without destination equality in `ScheduleTripGenerator:137–177`. These require discriminating regressions before relying on the denominator. Ordinary long stairs can still be selected after40 live queue ticks; timing, physiology and shared-midnight sleep remain unchanged. New dispatch endpoint reservations hardcode29/25–28. The previously checked-in1265-pass Unity XML predates the current dispatch changes.

Executed commands:

```bash
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisDesignProbe.csproj --configuration Release --artifacts-path /tmp/one-roof-superpowers-baseline-build -- /tmp/one-roof-superpowers-baseline
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj --configuration Release --artifacts-path /tmp/one-roof-superpowers-tests-build -- /tmp/one-roof-superpowers-baseline-tests.xml
```

Portable checkpoint runner: **39 passed /0 failed /0 skipped**. Current-source production probe exit0; its program exit does not assert gate success. At this rebaseline stage, fresh Unity, graphical/native performance and player build validation were **NOT RUN**; subsequent Unity results are recorded below. Standalone diagnostic timings are not native performance evidence.

Durable evidence: [production JSON](./TRANSIT_DAILY_LIFE_2026-10-03/baseline-production.json), [NUnit XML](./TRANSIT_DAILY_LIFE_2026-10-03/baseline-checkpoint-tests.xml), [source SHA256 manifest](./TRANSIT_DAILY_LIFE_2026-10-03/baseline-source-sha256.json). The implementation [plan](../../Docs/superpowers/plans/2026-10-03-transit-daily-life.md) was subsequently approved for agent execution; the approved implementation subsequently reached the user-requested Tasks1–3 cutoff.

## Approved implementation progress

User approved agent execution in the current checkout2026-10-03. Task1 required-intent continuity is implemented and independently reviewed: Spec PASS/Quality APPROVED. New explicit decisions remove synthetic local success, preserve crossed required Work/Home demand before skips and retain identity through retries/save/load. Portable51/51, focused12/12 and legacy54/54 pass; initialRED reproduced5 failures. Fresh Unity scoped `RoutineDemandContinuityTests`:12/12 passed,0 failed/skipped in an exact Task1 snapshot. [RED](./TRANSIT_DAILY_LIFE_2026-10-03/task1-red.xml), [portable GREEN](./TRANSIT_DAILY_LIFE_2026-10-03/task1-green.xml), [legacy](./TRANSIT_DAILY_LIFE_2026-10-03/task1-legacy.xml), [Unity](./TRANSIT_DAILY_LIFE_2026-10-03/task1-editmode.xml).

Fresh committed baselineHEAD8414567 full Unity EditMode:1265 passed/0 failed/3 skipped (1268 total), isolated `/tmp/one-roof-transit-baseline-unity`, exit0. [XML](./TRANSIT_DAILY_LIFE_2026-10-03/baseline-editmode.xml). This certifies the baseline compilation/tests, not later Task1 or movement/routine acceptance. Current final integrated Unity/performance/graphical checks remain pending. Task2 movement/dispatch is in progress; C0 stays open.

Task2 movement/dispatch patch is frozen for independent review. Portable Domain684/684 passed, no failures/skips. Matched-clock dispatch RED reproduced8 failures in9 cases against the after-Task1 snapshot, separating queue assignment from speed. Fresh scoped Unity44/44 passed, no failures/skips, runner exit0. [Dispatch RED](./TRANSIT_DAILY_LIFE_2026-10-03/task2-dispatch-isolated-red.xml), [portable GREEN](./TRANSIT_DAILY_LIFE_2026-10-03/task2-all-domain-frozen.xml), [Unity](./TRANSIT_DAILY_LIFE_2026-10-03/task2-editmode.xml). Full C0, routine/physiology integration and native performance evidence remain pending.

Independent Task2 review requested fixes: the held queue estimator counted later Residents as predecessors and a focused probe showed a slower stair diversion; inconsistent positive temporal values and undefined phases were accepted. Fix-round regressions reproduced18 failures before correction (113 total/95 passed). [RED](./TRANSIT_DAILY_LIFE_2026-10-03/task2-fix1-red.xml). Earlier684/684 and scoped44/44 results describe the pre-fix frozen patch and do not establish these newly identified cases. Task2 is not yet accepted.

Task2 fix1 corrected snapshot: focused covering183/183 and fresh scoped Unity72/72 passed, no failures/skips, native runner exit0. [Portable](./TRANSIT_DAILY_LIFE_2026-10-03/task2-fix1-frozen.xml), [Unity](./TRANSIT_DAILY_LIFE_2026-10-03/task2-fix1-editmode.xml). Independent scoped re-review remains pending; these results establish the new regressions, not integrated C0/performance closure.

Task2 was accepted after independent review and two scoped fix rounds. The original FIFO/corrupt temporal findings and subsequent old-save compatibility findings are all addressed, with no open findings. Final covering tests passed196/196 with no failures or skips: [migration RED](./TRANSIT_DAILY_LIFE_2026-10-03/task2-fix2-red.xml), [final portable results](./TRANSIT_DAILY_LIFE_2026-10-03/task2-fix2-green.xml). The latest scoped Unity72/72 run covers the fix1 snapshot; final integrated native validation remains pending. Tasks3–5 continue and C0 remains open.

Task3 frozen pre-fix results: focused162/162, full Domain751/751 and fresh scoped Unity26/26, no failures/skips, native runner exit0. [Domain XML](./TRANSIT_DAILY_LIFE_2026-10-03/task3-all-domain.xml), [Unity XML](./TRANSIT_DAILY_LIFE_2026-10-03/task3-editmode.xml). Independent review found new routine metadata corruption acceptance and a generated midnight-fragment/compressed-clock interaction; both require fixes before Task3 acceptance. Historical interim XML lost at runtime change is described as observed counts in the report, not retained evidence. Fresh current RED/GREEN is retained.


## Tasks1–3 cutoff validation

Task3 fix1 was independently accepted (Spec PASS / Quality APPROVED), with metadata validation and generated midnight timing findings addressed. Final focused portable **188 passed /0 failed /0 skipped**; [retained XML](./TRANSIT_DAILY_LIFE_2026-10-03/task3-fix1-focused.xml). Eight700-tick save replays and a3300-tick reference case are included.

Initial full Unity checkpoint run: **1412 passed /2 failed /3 skipped (1417 total)**, runner exit2. [XML](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-first-editmode.xml). Failures: GoldStandardCityTests immediate snapshot restoration changed current location; GoldenFirstPlayableTests expected an old-clock baseline wait greater than20 but measured8.92. These are integration regressions being resolved before the checkpoint commit, not passing evidence.

The user requested this cutoff and allowed current agents to finish. Tasks4–5 (reachable providers, physiology and integrated acceptance) remain pending. C0 stays open; graphical/native performance and full completion/wait gates are not certified by this checkpoint.

The synthetic first-playable congestion scenario now explicitly uses two-seat cars in both baseline and intervention. Physical timing and all existing assertions remain intact: average wait24.52→12.04 minutes (50.90% improvement). Portable Application suite reproduced the original failure74/75 and passes75/75 after calibration. [RED](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-golden-red.xml), [GREEN](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-golden-green.xml). This deliberately stressed five-floor scenario is separate from the authored300-Resident reference's unchanged3×20 capacity.

Outside restoration now preserves the saved last indoor room before restoring the Outside endpoint. Focused regression RED188/189→GREEN190/190, zero skipped; the exact90-tick GoldStandard snapshot checks every saved field, alongside existing replay tests. [RED](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-outside-red.xml), [GREEN](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-outside-green.xml). All original native snapshot assertions remain intact. Final full Unity rerun passes, as recorded below.

Independent scoped review of both cutoff corrections: **Spec PASS / Quality APPROVED**, no findings. [Review](./TRANSIT_DAILY_LIFE_2026-10-03/agent-reviews/checkpoint-integration-review.md). [Frozen checkpoint source manifest](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-source-sha256.json).

Final full Unity EditMode checkpoint: **1416 passed /0 failed /3 skipped (1419 total)**, runner exit0,263.92 seconds, isolated exact source snapshot. Both original failing tests pass. [Final XML](./TRANSIT_DAILY_LIFE_2026-10-03/checkpoint-final-editmode.xml). Command: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-transit-baseline-unity -runTests -testPlatform EditMode -testResults /tmp/transit-routines-checkpoint-final-editmode.xml -logFile /tmp/transit-routines-checkpoint-final-editmode.log`. Source manifest rechecked after the run. Domain/Application assemblies retain `noEngineReferences:true`; no engine code references were introduced. No graphical PlayMode, player build, native performance or integrated C0 acceptance was run for this cutoff.
