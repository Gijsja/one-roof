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

### Not used

This project has no persistent Editor connection, no `com.unity.pipeline`, and no Pipeline descriptors. Unity CLI invocations other than one-shot `-batchmode` Editor runs are not validation evidence.
