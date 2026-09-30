# Development Workflow & Testing Strategy — One Roof (v0.10+)

This document is the authoritative engineering guide for test architecture, headless batchmode execution, live Editor pipeline probing, and repository hygiene.

---

## 1. Test Strategy & Pyramid

### 1.1 Pure Domain Tests (No Engine References)
- Located in `Assets/OneRoof/Tests/EditMode/Domain/`.
- Pure C# testing without loading Unity engine systems.
- Validates deterministic schedules, needs arbitration, routing decisions, closed-loop economy ledgers, faction alignments, social traits/relationships, save migration logic, and command validation (`CanExecute`).

### 1.2 EditMode Integration Tests
- Located in `Assets/OneRoof/Tests/EditMode/Application/`, `Presentation/`, `Infrastructure/`, `UI/`.
- Validates ScriptableObject registries, Addressables packaging, projection builders, presenter binding, and serialization round-trips.

### 1.3 PlayMode Acceptance Tests
- Located in `Assets/OneRoof/Tests/PlayMode/`.
- Exercises bootstrap composition, scene loading, pooled view binding, camera navigation, input handling, and golden multi-floor loops.

### 1.4 Required Fixtures
- **Tiny Two-Floor Graph**: Micro-transit fixture.
- **Five-Floor First-Playable**: `StandardFiveFloor` baseline loop.
- **Fifty-Resident Fixture**: Deterministic morning commute stress test.
- **30-Floor / 300-Resident Gold Standard Playground**: `Tower_GoldStandard30.unity` reference scale verification.

---

## 2. Headless Unity Batchmode Execution

Validation runs headlessly via batchmode. When no interactive Editor is locking the workspace (`Temp/UnityLockfile` is absent), execute commands directly in the primary project directory. When an Editor session is actively running or for destructive experiments, use an isolated worktree.

### 2.1 Execution Modes
- **Direct Workspace (Default)**: If `Temp/UnityLockfile` is absent, execute directly against the workspace (`-projectPath /home/geisha/Vibecode/UnityAI/one-roof` or `.`). Fast, no Git worktree overhead, no sandbox bypass required.
- **Isolated Worktree (Concurrent / Destructive)**: If an interactive Editor session has the workspace open, create a detached worktree:
  ```bash
  git worktree add ../one-roof-<task>-validation HEAD
  ```
  Apply scoped changes, validate, and remove the worktree when finished (`git worktree remove ../one-roof-<task>-validation`).

### 2.2 Fast Compilation Check
Verify C# compilation without running tests (exit code `0` required):
```bash
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /home/geisha/Vibecode/UnityAI/one-roof \
  -logFile /tmp/one-roof-compile.log
```
On failure, grep `/tmp/one-roof-compile.log` for `error CS` lines to pinpoint compiler issues.

### 2.3 Running Automated Tests

> [!IMPORTANT]
> **Never pass `-quit` with `-runTests`**. In Unity 6000.3 LTS, `-quit` causes the Editor to exit immediately upon initialization before the test runner executes or emits results XML. The test runner exits automatically with its own code.

#### EditMode Tests (832 Passing)
```bash
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/geisha/Vibecode/UnityAI/one-roof \
  -runTests -testPlatform EditMode \
  -testResults /tmp/one-roof-editmode.xml \
  -logFile /tmp/one-roof-editmode.log
```

#### PlayMode Tests (15 Passing)
```bash
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode \
  -projectPath /home/geisha/Vibecode/UnityAI/one-roof \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/one-roof-playmode.xml \
  -logFile /tmp/one-roof-playmode.log
```

**Exit Codes**: `0` all passed, `2` test failure(s), `3` execution or compilation failure.
- Target specific tests using `-testFilter <SubstringPattern>`. Note that comma-separated lists do not OR-match; run separate invocations per filter.
- Confirm the test report XML exists and has `total > 0`.

### 2.4 Attributing Failures
- Run with `-testFilter <FailedTestName>`.
- If ambiguous, verify against pristine `HEAD`.
- Always document pre-existing baseline failures explicitly rather than masking them.

---

## 3. Live Pipeline & Editor Probing (`pipeline_client.py`)

When an Editor session runs with `com.unity.pipeline` active (port 7800 or 7801, defined in `Library/Pipeline/.unity-pipeline-port`):

- **Performance Telemetry**:
  ```bash
  python3 pipeline_client.py cmd get_performance_stats
  ```
  Returns live draw calls, batches, SetPass calls, and CPU frame times. Used to certify presentation budgets at scale (e.g. `Tower_GoldStandard30` at 83 overview draw calls and ~10.0 ms frame time).

- **Console Status**:
  ```bash
  python3 pipeline_client.py cmd console_status
  ```
  Verifies zero console errors or unhandled exceptions during live simulation ticks.

- **Dynamic Expression Evaluation**:
  ```bash
  python3 pipeline_client.py eval "TowerPlayableController.Instance.CurrentTick"
  ```
  Evaluates C# expressions in the running scene without halting simulation.

---

## 4. Pre-Commit Hygiene & Git Rules

- **Paired `.meta` Files**: Ensure every new asset or directory has a valid `.meta` file; zero orphaned `.meta` files.
- **Git Sandbox Rules**: File modifications inside the workspace work in standard sandbox mode. Git commands altering index/history (`git add`, `git commit`, `git checkout`) require sandbox bypass (`BypassSandbox: true`).
- **C# Syntax & Boundary Integrity**: Confirm zero engine references in `OneRoof.Domain` and `OneRoof.Application`.
