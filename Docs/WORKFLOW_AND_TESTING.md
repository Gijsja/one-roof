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
- **Direct Workspace (Default)**: If `Temp/UnityLockfile` is absent, execute directly against the workspace (`-projectPath /home/geisha/Vibecode/UnityAI/one-roof` or `.`). Fast, no Git worktree overhead. Note that invoking Unity binaries or dotnet building Unity-generated csproj files requires `BypassSandbox: true` due to external assembly paths in `/home/geisha/Unity/Hub/Editor/...`.
- **Isolated Worktree (Concurrent / Destructive)**: If an interactive Editor session has the workspace open, create a detached worktree:
  ```bash
  git worktree add ../one-roof-<task>-validation HEAD
  ```
  Apply scoped changes, validate, and remove the worktree when finished (`git worktree remove ../one-roof-<task>-validation`).

### 2.2 Fast Compilation Check
Verify C# compilation without running tests (exit code `0` required). Requires `BypassSandbox: true`.

**Preferred (Concurrent with live Editor / No lockfile clash)**:
```bash
/home/geisha/.dotnet/dotnet build OneRoof.Presentation.csproj
/home/geisha/.dotnet/dotnet build OneRoof.Presentation.Tests.EditMode.csproj
```

**Alternative (Headless Unity Batchmode - Requires Editor closed / no `Temp/UnityLockfile`)**:
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

#### EditMode Tests (1,265 Passing)

**Recommended Automated Runner (Prints Failure Traces to Console on Error)**:
```bash
Tools/run_editmode_tests.sh [-testFilter <Filter>]
```

**Direct Headless Invocations**:
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

## 3. Live Pipeline & Editor Probing (`unity-cli` via `com.unity.pipeline`)

When an Editor session runs with `com.unity.pipeline` active (port 7800 or 7801, defined in `Library/Pipeline/.unity-pipeline-port`), agents can drive, probe, and run tests directly against the running Editor via the `unity` CLI:

- **Editor Connection Status**:
  ```bash
  unity status
  ```
  Confirms the connected live Editor and port (e.g. port 7800).

- **Console Status**:
  ```bash
  unity command console_status
  ```
  Verifies zero console errors or unhandled exceptions during live simulation ticks.

- **Run Tests in Live Editor (Instant execution, no restart needed)**:
  ```bash
  unity command run_tests --mode EditMode --filter <TestPattern>
  ```
  Executes EditMode or PlayMode suites directly in the running Editor without closing the project or colliding with `Temp/UnityLockfile`.

- **Trigger Asset Refresh & Recompile**:
  ```bash
  unity command eval "UnityEditor.AssetDatabase.Refresh();"
  unity command recompile
  ```
  Forces the live Editor to ingest newly created C# scripts or assets and reload domain assemblies.

- **Dynamic Expression Evaluation**:
  ```bash
  unity command eval "OneRoof.Presentation.Tower.TowerPlayableController.FloorY(0)"
  ```
  Evaluates C# expressions in the running scene without halting simulation.

---

## 4. Pre-Commit Hygiene & Git Rules

- **Paired `.meta` Files**: Ensure every new asset or directory has a valid `.meta` file; zero orphaned `.meta` files.
- **Sandbox & Tool Execution Boundaries**: Workspace files are writable in the standard sandbox. However, `.git/` is mounted read-only, and Unity assemblies/binaries reside on external paths (`/home/geisha/Unity/Hub/Editor/...` and `/home/geisha/.vscode/extensions/...`). Therefore, sandbox bypass (`BypassSandbox: true`) is required for: (1) Git commands mutating index or history (`git add`, `git commit`, `git checkout`, `git stash`), (2) Headless Unity Editor invocations, and (3) `dotnet` compilation/runs referencing Unity-generated project files (`OneRoof.*.csproj`).
- **Tool Parameter Hygiene**: Never provide `ArtifactMetadata` to `write_to_file` when writing files in the workspace (only valid for conversational artifacts in `<appDataDir>/brain/`). Use `replace_file_content` for editing existing repository source code.
- **C# Syntax & Boundary Integrity**: Confirm zero engine references in `OneRoof.Domain` and `OneRoof.Application`.
