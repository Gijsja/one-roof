# Development Workflow

## Headless Unity validation

One Roof does not use a persistent Unity Editor connection or `com.unity.pipeline`. Every Unity validation run is a one-shot, headless batch process.

### 1. Isolate the checkout

Do not run a batch Unity process against a checkout a person may have open in Unity. Use a dedicated Git worktree for validation, or first confirm that no Unity process owns the project.

```bash
git worktree add ../one-roof-validation HEAD
```

### 2. Compile

Run the installed Unity 6000.3.24f1 Editor once and require a zero exit code:

```bash
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /absolute/path/to/one-roof-validation \
  -logFile /tmp/one-roof-compile.log
```

If it fails, read only the compiler-error lines from that log and fix the narrowest cause. Do not remove `Temp/UnityLockfile` while any Unity process owns the checkout.

### 3. Run tests

Use Unity's built-in test runner directly; it does not require a Pipeline server:

```bash
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /absolute/path/to/one-roof-validation \
  -runTests -testPlatform EditMode \
  -testResults /tmp/one-roof-editmode.xml \
  -logFile /tmp/one-roof-editmode.log
```

Run PlayMode tests with `-testPlatform PlayMode` only when the task affects runtime presentation or acceptance coverage. Record the command, exit code, result XML path, and any known pre-existing failures in the handoff.

### Rules

- Never run persistent `-batchmode` instances or omit `-quit`.
- Never use `unity pipeline`, `unity command`, `unity status`, or a Pipeline descriptor as validation evidence.
- Do not validate an active human checkout; create an isolated worktree first.
