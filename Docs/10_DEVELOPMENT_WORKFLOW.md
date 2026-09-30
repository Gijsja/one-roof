# Development Workflow

## Headless Unity validation

Every Unity validation run is a one-shot, headless batch process against an isolated Git worktree. Never validate a checkout a person (or another session) has open in Unity.

### 1. Isolate the checkout

Create a worktree at the base commit **outside `/tmp`** (tmp is wiped on restart; a sibling directory survives):

```bash
git worktree add ../one-roof-<task>-validation HEAD
```

Apply only the change under validation (scoped diff or patch), then confirm the worktree contains nothing else:

```bash
git -C ../one-roof-<task>-validation status --short
```

Use a distinct worktree name per task and never write into another session's worktree. Remove scratch worktrees when done (`git worktree remove`).

### 2. Compile

Require a zero exit code:

```bash
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /absolute/path/to/one-roof-<task>-validation \
  -logFile /tmp/one-roof-compile.log
```

On failure, grep the log for `error CS` lines and fix the narrowest cause. Do not remove `Temp/UnityLockfile` while any Unity process owns the checkout.

### 3. Run tests

The test runner quits on its own with a result code — **never pass `-quit` with `-runTests`**. Verified in 6000.3.24f1: `-quit` makes the Editor exit right after load (`Batchmode quit successfully invoked`, no results XML), regardless of whether it appears before or after `-runTests`. Guard with `timeout` so no stray process survives:

```bash
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /absolute/path/to/one-roof-<task>-validation \
  -runTests -testPlatform EditMode \
  -testResults /tmp/one-roof-editmode.xml \
  -logFile /tmp/one-roof-editmode.log
```

Exit codes: `0` all pass, `2` some failed, `3` run failed. Notes:

- A fresh worktree's first invocation may only import/compile and then quit; re-run the same command to execute the suite.
- Confirm the run actually executed: the results XML must exist with `total > 0`, and the log must show test activity — not just `Batchmode quit successfully invoked`.
- Run PlayMode tests with `-testPlatform PlayMode` only when the task affects runtime presentation or acceptance coverage.
- `-testFilter` takes a single substring pattern (comma-separated lists do not OR-match); run separate invocations per filter when needed.

### 4. Attribute failures

A failing suite proves nothing until failures are attributed. Check whether failing tests reference the changed code; when in doubt, re-run the same tests on pristine HEAD (e.g. via `git stash` in the worktree, then `stash pop`) and compare. Record pre-existing failures explicitly — never silently absorb them.

### 5. Hand off

Record the worktree path (or its removal), exact commands, exit codes, result XML paths, test totals, and any pre-existing failures plus their baseline evidence. See `Handoffs/Active/` for examples.

## Pipeline & Automation Tooling Workflows

The project combines headless CLI execution, Python-driven test orchestration, live Editor probing via `com.unity.pipeline`, and pre-commit hygiene audits.

### 1. Unity Pipeline CLI & Live Probing (`pipeline_client.py`)

When the Unity Editor is running (interactively or via background service), `com.unity.pipeline` binds an HTTP server to the loopback interface (`http://127.0.0.1:7800`):
- **Discovery & Auth**: Reads the JSON descriptor at `<projectPath>/Library/Pipeline/.unity-pipeline-port` to extract the active `port` and CSPRNG bearer `evalToken`. Requests must supply `Authorization: Bearer <evalToken>`.
- **Live Probing Tooling (`pipeline_client.py`)**:
  - `python3 pipeline_client.py cmd get_performance_stats`: Queries live rendering metrics (draw calls, batches, SetPass calls, CPU frame times). Used to verify presentation performance budgets (e.g. validating the 30-floor scale target in `Tower_GoldStandard30` at 171 draw calls and ~9.27 ms frame time).
  - `python3 pipeline_client.py cmd console_status`: Checks current console errors and warnings. Enforces the strict rule of 0 console exceptions during live ticks.
  - `python3 pipeline_client.py eval "<expression>"`: Evaluates arbitrary C# expressions on the fly (e.g. `TowerPlayableController.Instance.CurrentTick`, elevator queue lengths, or resident transit states) without halting playback.
- **Pipeline CLI (`unity command`)**: The official `unity` CLI can similarly drive commands (`unity command get_performance_stats`, `unity command editor_status`, `unity command eval '...'`).
- **Safety Policy**: Prefer an isolated worktree for destructive or heavy runs. Reuse a connected editor only for light read-only inspection or telemetry probes — never trigger heavy test suites on an editor another session owns.

### 2. Headless Test Runner & Subprocess Bounding

- **Timeout Governance**: Automated runs wrap Unity batchmode invocations inside Python subprocess harnesses (e.g. `subprocess.run(..., timeout=1500)`) or shell `timeout 1500` to prevent stalled headless processes.
- **Batchmode Flag Invariant**: Never pass `-quit` when running `-runTests`. In Unity 6000.3, `-quit` causes immediate termination before executing the test runner or generating result XMLs.
- **Artifact Verification**: Scripts parse the emitted NUnit XML report (`/tmp/<name>-editmode.xml`) to assert `total > 0` and `failures == 0`, and cross-reference failures against pristine baseline logs.

### 3. Pre-Commit Hygiene & Asset Pipeline Automation

Prior to committing changes or preparing milestone handoffs, automated Python scripts verify repository integrity:
- **Meta File Audit**: Recursive traversal of `Assets/` verifying every asset and directory has a valid `.meta` file, with zero orphaned `.meta` files.
- **Asset Specification & Schema Checks**: Validation of Spine 2D JSON schemas (verifying bone counts, slot attachments, and skin mappings) and SHA-256 verification of authored art slices against specifications.
- **C# Syntax & Bracket Integrity**: Automated bracket/brace matching across all touched `.cs` files to catch unbalanced syntax before compiler invocation.
