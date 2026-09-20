# Development Workflow

## Unity Pipeline recovery

Use this runbook when `unity status` or `unity pipeline list` reports an unreachable Pipeline server.

### 1. Diagnose before changing files

Run:

```bash
unity pipeline list --format json
```

- If `data.summary.instancesInSafeMode` is greater than zero, read only the compiler-error lines from `Logs/Editor.log` (or the launched Editor log), fix those C# errors, and restart Unity.
- If Safe Mode is not detected, confirm whether a real Editor process owns the project before touching any lock file:

```bash
unity editors running --format json
ps -ef | rg '[U]nity|[u]nityhub'
lsof -nP +D /absolute/path/to/project
```

Do not remove locks while an Editor process is live or may have unsaved work.

### 2. Recover a stale project lock

If Unity says the project is already open but no process owns it, inspect `Temp/UnityLockfile`. It is a zero-byte session lock and can remain after a crashed or externally terminated Editor.

Preserve it outside the project rather than deleting it, then start Unity again:

```bash
mv Temp/UnityLockfile /tmp/one-roof-stale-locks/UnityLockfile
```

`Library/EditorInstance.json` and database lock files should be treated the same way only after process ownership is ruled out. They are session artefacts, not source files. Never place their contents in a handoff: Pipeline descriptors can contain authentication tokens.

### 3. Start and verify a headless Editor

For this host, use the installed Unity 6000.3.24f1 executable and keep it in a managed terminal session. Detached `nohup` children can be reaped by the task environment.

```bash
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/geisha/Vibecode/UnityAI/one-roof \
  -logFile /tmp/one-roof-unity-pipeline.log
```

After the initial import and compilation, confirm both the descriptor and service without printing the descriptor contents:

```bash
test -f Library/Pipeline/.unity-pipeline-port
unity pipeline list --format json
unity command editor_status --project-path /home/geisha/Vibecode/UnityAI/one-roof --format json
```

The expected result is one reachable server. A headless batch Editor is not necessarily shown by `unity status`; use `unity pipeline list` or `unity command` as the readiness check.

### Interpret Pipeline state correctly

`unity pipeline list` reports three independent facts. Do not collapse them into a single “Unity is ready” conclusion:

| Signal | Meaning | Next action |
| --- | --- | --- |
| `isRunning: true`, `isReachable: false` | The Editor process or project lock is visible, but CLI discovery cannot authenticate to a live endpoint yet. | Wait for `Library/Pipeline/.unity-pipeline-port` to appear, then retry. Do not start tests. |
| `safeMode.detected: false` | Safe Mode is not causing the failure. It does **not** prove that the Pipeline server is ready. | Continue the descriptor/readiness check. |
| `isReachable: true` and `editor_status.status: ready` | The authenticated Pipeline endpoint is available and settled. | Enable auto-tick, recompile, then test. |

On this project, initial import and domain reload can take roughly 45 seconds. A server listener may bind before the descriptor is recreated, so a 9-second retry can correctly remain unreachable. Prefer waiting on the descriptor over repeatedly polling the endpoint. If import has finished but the descriptor is still absent, restart the clean headless Editor once.

If the listener starts but `Library/Pipeline/.unity-pipeline-port` is absent after an initial domain reload, gracefully restart that same headless Editor once. The clean second launch recreates the descriptor used by CLI discovery.

### 4. Resume the validation loop

```bash
unity command set_autotick --enable true --project-path /home/geisha/Vibecode/UnityAI/one-roof
unity command recompile --project-path /home/geisha/Vibecode/UnityAI/one-roof
unity command recompile_status --project-path /home/geisha/Vibecode/UnityAI/one-roof
unity command run_tests --mode editor --async_tests true --project-path /home/geisha/Vibecode/UnityAI/one-roof
unity command test_status --project-path /home/geisha/Vibecode/UnityAI/one-roof
```

Only report a validation result after `recompile_status` is `completed` or `up_to_date` and `test_status` is `completed`.

## Recorded recovery

On 2026-09-19, a stale `Temp/UnityLockfile` prevented this project from starting after a prior failed compile. Once the stale lock was preserved and the cleanly compiled Editor was restarted, the Pipeline descriptor returned, the server was reachable on port 7801, and all 318 Edit Mode tests passed.
