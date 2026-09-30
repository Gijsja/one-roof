# Backlog review — 2026-09-30

This pass preserves the pre-existing uncommitted resident, artwork archive, housing, economy and save work. Three subagents handled transit/undercity, social, and artwork/preview/accessibility. The coordinator integrated shared files and ran Unity 6000.3.24f1 validation in this workspace. The validation pass left changes uncommitted; the Steward subsequently requested a commit. No license purchase/configuration changes were requested or made.

## Changes

- **OR-204**: cars added to upgraded banks inherit their tier profile. Tests cover mid-trip upgrade/save continuation, left widening/save round-trip, and occupied-shaft rejection without treasury changes. Two original failing widening fixtures were invalid: one constructed a forbidden single-floor shaft; another widened into occupied rooms. Both retain production topology constraints.
- **OR-906B**: strong recent ties share named prior-settlement direct grievances one hop. Influence is separately projected and inspectable, bounded to 0.04 per issue / 0.12 total, and decays with relief. Saved prior exposure and influence support continuation. Positive thoughts now contribute through the existing mood coefficient before final satisfaction clamping, preserving neutral behavior.
- **OR-906C**: civil signals use the largest deterministic connected local group with sustained same-cause pressure, not total faction membership. Three settlement samples, current measured pressure, membership and recent strong ties are required. Cause improvement removes readiness. Faction save version 4 stores pressure/history; old versions begin without organization readiness. The restore API accepts the existing externally configured settlement cadence; normal default remains 1440 ticks/day.
- **OR-1001 Slice C**: staffed/fuelled generators bootstrap before consumers, persist per-room output and reverse-feed through connected undisrupted core paths. Underground production needs power; infirmary/commons/shelter additionally need water. Staff wages/upkeep remain payable during outages. Utility projection caching accounts for equipment state and ticks. Old aggregate-only generator saves regenerate physical output at the next settlement.
- **OR-1100**: archive checksum/GUID/image guard protects AssetLab validation, Addressables configuration/build and standard player builds. Ten fixture tests cover renamed images, Resources, Addressables, missing serialized references and archive corruption. All 103 archive entries/50 images match; no active archived references. Six-pose replay, actual preview open/scrub/close and rendered body/caption checks are included. Preview cameras use explicit clipping/framing; bodies and bubbles must be visible to pass.
- **OR-1006 partial**: selected modes/tools/overlays now include a text marker independent of colour. Remaining scope is listed below.

## Validation

Final current-workspace results: **920 EditMode passed, zero failed, one explicit benchmark skipped** (921 cases); **15/15 graphical PlayMode passed**; the skipped benchmark was then run explicitly and **passed**. All runs exited 0 and emitted nonempty XML.

- [Final EditMode XML](/tmp/one-roof-backlog-final-editmode.xml), [log](/tmp/one-roof-backlog-final-editmode.log)
- [Final PlayMode XML](/tmp/one-roof-backlog-final-playmode.xml), [log](/tmp/one-roof-backlog-final-playmode.log)
- [Explicit budget XML](/tmp/one-roof-backlog-final-budget.xml), [log](/tmp/one-roof-backlog-final-budget.log)
- [Verified resident render](./RESIDENT_PREVIEW_2026-09-30.png): six poses in both facings, visible palette-tinted anatomy, readable upright bubbles. Preview open/scrub/close preserves the active scene and releases the isolated rig. Pool material sharing survives identity changes and recycling.

No engine references in Domain/Application; both retain `noEngineReferences: true`. New asset metadata pairing, archive checksums and `git diff --check` passed. Exactly one active handoff remains.

Baseline: 889 EditMode cases: 884 passed, four failed, one explicit benchmark skipped. Failures were the two widening fixtures and social co-presence/positive-thought cases recorded in the prior handoff. Co-presence needed a real fixture room and consistent household IDs.

The **final graphical PlayMode** run includes authored 30-floor/300-resident facade/LOD/pool integration. Overview measured **12.235 ms average frame (~81.7 FPS), 84 draw calls and 1.959 ms average tick** across 300 ticks, on NVIDIA GeForce GT 755M. These are Editor overview measurements, not target-build or all-camera-angle certification.

The **final explicit 30-floor/300-resident/14-room benchmark** passed across 1500 measured ticks: **p50 0.608 ms, p95 1.073 ms, worst 51.259 ms**. Leasing ticks p95 1.122 ms; ordinary ticks p95 1.073 ms. Per-thread p50/p95 allocations were zero in both buckets; GC counts were 1/1/1. The p95 criterion passes; the worst sample remains reported. This does not certify a 30-day City Status streak.

The preview fix was verified by rendering: PreviewRenderUtility defaults to near=2/far=10; a camera at z=-10 clipped anatomy behind z=0 while retaining bubbles at z<0. Explicit near=0.01/far=100 and bounds-based framing correct it. A separate tint issue required sprite-aware shared materials for procedural/pooled rigs; mesh LOD materials remain separate. Image tests require body pixels and chromatic pixels within every body region, so bubbles-only or white-body captures fail.

The M9 save/recovery fixture uses an injected one-tick settlement cadence. Restore now accepts that same optional configuration, avoiding comparison against the default 1440-tick cadence. Both restored and original simulations require actual organization before activation and measured cause improvement before Recovery.

Commands (Unity binary `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity`, project `/home/geisha/Vibecode/UnityAI/one-roof`):

```bash
# Full graphical EditMode; no -quit with -runTests.
Unity -batchmode -projectPath /home/geisha/Vibecode/UnityAI/one-roof -runTests -testPlatform EditMode -testResults /tmp/one-roof-backlog-final-editmode.xml -logFile /tmp/one-roof-backlog-final-editmode.log
# Full graphical PlayMode.
Unity -batchmode -projectPath /home/geisha/Vibecode/UnityAI/one-roof -runTests -testPlatform PlayMode -testResults /tmp/one-roof-backlog-final-playmode.xml -logFile /tmp/one-roof-backlog-final-playmode.log
# Explicit complete-tick benchmark.
Unity -batchmode -nographics -projectPath /home/geisha/Vibecode/UnityAI/one-roof -runTests -testPlatform EditMode -testFilter UndergroundTickBudgetTests -testResults /tmp/one-roof-backlog-final-budget.xml -logFile /tmp/one-roof-backlog-final-budget.log
```

## Remaining work and dependencies

1. **OR-1002**: implement six condition-driven crisis chains, warning/propagation/recovery and saved resident consequences. This is the next main beta feature.
2. **OR-1006** in parallel: use contracted spatial channels for all eight overlays; add UI scaling, actual remapping/persistence and pause/speed review. Several overlays currently render lists rather than spatial vectors/badges/glow. Input polls fixed keys despite a default Input Actions asset; W conflicts with weather cycling and D with Data selection. UI scale and 1x/2x/3x controls are absent.
3. **OR-1005**, then **OR-1007**, then **OR-1003**: campaign/streak inspectability, save/recovery/content QA, and the 30-day deterministic City Status performance acceptance.
4. **OR-1101..1107** remain after OR-1003. No M11 expansion was started.

The archive guard wraps the project's Addressables build entry; a third-party direct Addressables build can bypass that wrapper. Standard player builds still enforce the guard. No target player build was produced during this pass. Current automated/render evidence does not replace manual crowded-Tower interaction or final beta accessibility review.
