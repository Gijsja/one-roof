# C0 engineering checkpoint — complete demand and save continuation

2026-10-02. **Implemented checkpoint; OR-1002-C0 remains IN PROGRESS.** This work executes the first engineering item in `BETA_IMPLEMENTATION_RUN`, not the eleven-package run or City Status certification. Three workers held separate population, executed-route and observer ownership; the coordinator integrated shared saves, generator/random state, pickup targets and tick hooks. Existing planning/UI design and concurrent Caretaker changes belong to other work.

## Behavior

- Routine demand is observed before destination/route planning, including local arrival, missing destinations, route rejection, suppressed required schedule transitions, outside-food retry and move-in Home demand. Stable Resident/purpose/episode IDs deduplicate retries. A failed attempt cannot become a second successful denominator entry just by retrying.
- The bounded observer retains current and one prior pending day, saved monotonic IDs and inclusive 360-tick completion deadlines. Completion precedes timeout. Partial initial intervals stay unavailable. Explicit missing observation remains unavailable; zero observed demand is healthy only with complete coverage and no outstanding unreachable demand.
- Required unreachable demand stays visible across midnight/inactivity until actual reachability is observed. Live wait is sampled before boarding/removal and afterward, including passengers whose demand already timed out. Nearest-rank terminal wait percentiles include zero-wait/local demand; historical positive-wait polling diagnostics are separately labelled.
- Saves preserve valid zero needs with a versioned legacy fallback, original and executed rerouted paths, leg progress, pickup targets, trip-generator IDs/retry deadlines and the production deterministic random stream. Queue reconstruction no longer replaces saved dispatch targets/phases. Invalid observer/routes/progress/random checkpoints are rejected by the existing safe loading boundary.
- Domain/Application stay engine-free. No scenes, gameplay routing rules, transit tiers, gates or new crisis phases were changed by this checkpoint.

## Validation and limitations

The first available Unity EditMode run passed **1,235 / 0 failed / 3 skipped**, including early checkpoint changes, before final integration. A later engine run was stopped by temporary Caretaker projection/domain compilation mismatch while another chat edited that feature; those files were left to that chat. Final regression evidence follows when available.

The portable runner uses the project's actual NUnit assembly and production Domain sources, not a replacement assertion implementation. **37 scoped NUnit cases passed / 0 failed / 0 skipped**, covering population boundary values/migration, exact executed routes, corrupt payload rejection, pickup continuation, generator/retry/random restoration, local/suppressed/unreachable demand, pending deadlines and observer save independence. [Durable results](./C0_CHECKPOINT_2026-10-02/standalone-tests.xml).

The production probe compares causal save state each tick after save at 7,650. It canonicalizes only faction-support ordering and the stale `currentRoomId` retained while the authoritative location is Outside. Uninterrupted and restored tier1/tier5 runs matched for **1,500 subsequent ticks**, including the next daily settlement, after the pickup-target repair. This is reference continuation evidence, not all aggregate/phase/deletion recovery certification.

Native Unity presentation, player builds, six-chain acceptance and the 30-day City Status streak are NOT RUN. Observer storage is bounded; native runtime cost is not certified. Old payloads missing temporal observation do not receive invented history. Custom random-stream implementations without checkpoint support remain outside the demonstrated deterministic continuation guarantee.

## Calibration and next work

Isolated broad stair preference removed all elevator queues but only 5,989/6,993 captured completions met360 ticks. A cost<=300 stair preference combined with the prior deadline/live-wait hypothesis achieved tier5 7,805/7,827 captured completions within360, positive-wait p95=112 and live maximum=328. These fail selected60/240 limits and use captured-trip diagnostics, not complete daily acceptance. [Isolated numerical evidence](./C0_CHECKPOINT_2026-10-02/isolated-deadline-hybrid.json). The hypothesis was not copied into production; no chosen gate was relaxed.

Continue C0 with legal routing/dispatch/layout calibration under the now-observed complete cohort, using effective/ineffective capacity interventions. Foundations remain startable on explicit ports; C0 and final gates remain open. Departure/topology deletion and all six new incident owners still need their dedicated integration/recovery matrices.

## Reproduction

```bash
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj --configuration Release --artifacts-path /tmp/c0-tests -- /tmp/c0-tests.xml
/home/geisha/.local/bin/dotnet run --project Tools/DesignValidation/CrisisDesignProbe.csproj --configuration Release --artifacts-path /tmp/c0-probe-build -- /tmp/c0-probe > /tmp/c0-probe.json
```

Use the workflow's Unity EditMode command for full engine regression. The portable runner's NUnit path can be overridden with `-p:NUnitFrameworkPath=...` if the PackageCache revision changes.
