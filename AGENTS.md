# One Roof — Agent Guide (v0.10+)

## Project & North Star

One Roof is a deterministic vertical-city simulation: the Steward shapes systems (architecture, zoning, infrastructure, leases, decrees), while autonomous residents respond.

- **Baseline (v0.10+ Prototype Complete)**: 832 EditMode tests passing, 15 PlayMode tests passing; 30-floor / 300-resident reference scale verified in `Tower_GoldStandard30.unity` at ~100 FPS (10.0 ms frame time) and 83 draw calls overview (171 draw calls / 86 batches standard batching limit); 3-tier resident LOD & semantic zoom facade crossfades; inspector-authored presentation settings & zero-allocation view pooling; Sims-style resident social sandbox (traits, thoughts, 12-tier relationship spectrum); closed-loop unit economics; 8 data overlays; 4 factions; 12-month weather cycle; and secret undercity expansion with physical utility paths.
- **North Star (Beta Boundary / City Status)**: Sustaining City Status for 30 consecutive in-game days under strict performance budgets (<4.0 ms tick, 60 FPS presentation), 6 adaptive crisis chains, and campaign board pacing.

---

## Canonical Vocabulary

- **Tower**: An enormous mixed-use vertical building the Steward constructs floor by floor. (*Avoid*: building, city, level)
- **Steward**: The player role: developer, mayor, and systems designer shaping structure, services, leases, policy, and capacity. (*Avoid*: player, mayor, user)
- **Resident**: An autonomous person living and working in the Tower, governed by needs, satisfaction, grievances, and strain. (*Avoid*: NPC, agent, citizen, customer)
- **Cause-chain**: The explanation contract: world symptom → overlay → inspector cause → systems-level response → measured feedback. (*Avoid*: alert chain, diagnostic flow)

---

## Agent Context Index: Read on Demand

Consult [`Planning/BACKLOG.md`](./Planning/BACKLOG.md) and [`Handoffs/Active/`](./Handoffs/Active/) for active tasks, then consult **only the single canonical doc** relevant to your task:
- **Architecture, Data Contracts & Economy**: [`Docs/ARCHITECTURE.md`](./Docs/ARCHITECTURE.md)
- **UX, Overlays & Cause-Chain Inspector**: [`Docs/UX_AND_GAMEPLAY.md`](./Docs/UX_AND_GAMEPLAY.md)
- **Workflow, Testing & Validation**: [`Docs/WORKFLOW_AND_TESTING.md`](./Docs/WORKFLOW_AND_TESTING.md)
- **Rig, Wardrobe & Content Assets**: [`Docs/ASSETS.md`](./Docs/ASSETS.md)
- **Active Architecture Decisions**: [`Docs/DECISIONS.md`](./Docs/DECISIONS.md) (ADR-070+)

---

## Core Boundaries & Invariants

1. **Systems over individuals**: Players influence systems (zoning, capacity, leases, decrees), never individual residents directly.
2. **Cause-chain explanation**: Every failure connects: `Symptom → Overlay → Inspector Cause → Systems Response → Measured Feedback`.
3. **Pure-C# Domain purity**: `OneRoof.Domain` and `OneRoof.Application` strictly maintain `noEngineReferences: true` (0 `UnityEngine` references). Never introduce `UnityEngine` dependencies into domain code.
4. **Clean Layering**: Domain ➔ Application (projections & session caching) ➔ Presentation (`MonoBehaviour`s & VFX).
5. **Projections only**: Presentation and UI consume immutable projections bound by stable entity IDs. UI dispatches validated commands via `TowerSimulation.CanExecute()`, never directly mutating simulation state.
6. **No per-agent Update loops**: Simulation runs on a fixed discrete tick (1440 ticks/day). Views are pooled and presentation-interpolated.
7. **Aggregate persistence**: All serialization lives in domain sub-aggregates (`ToSaveData()` / `FromSaveData()`). Zero scene references in saves.
8. **Strict Handoff Hygiene**: Maintain **strictly ONE active handoff** in `Handoffs/Active/`. Keep it concise, authoritative, and focused on current task status.
9. **Sub-Agent Roadmap & Handoff Protocol**: When packaging architectural proposals or feature tracks for sub-agents:
   - Create an execution plan in `Planning/<FEATURE>_ARCHITECTURE.md` with package IDs, prerequisites, owned files, and copyable prompts.
   - Add the phased sequence to `Planning/ROADMAP.md`.
   - Register work packages in `Planning/BACKLOG.md` with dispatch statuses (`READY`, `QUEUED`, `BLOCKED`).
   - Synchronize `Handoffs/Active/` without creating duplicate active handoff files.

---

## Workspace Agent Directives

### Architecture & Boundaries
- **Domain Purity**: `OneRoof.Domain` has `noEngineReferences: true`. Never introduce `UnityEngine` dependencies into domain code.
- **Clean Layering**: Domain ➔ Application (projections & session caching) ➔ Presentation (`MonoBehaviour`s & VFX).

### Execution & Testing Protocol (3-Tier Escalation)
- **Tier 1 (Fast Inner Loop — 95% of tasks)**: Pure .NET CLI.
  - Compile check: `dotnet build OneRoof.Presentation.csproj` (~7–11s; compiles Domain, App, Content, UI, Presentation).
  - Fast domain tests: `dotnet run --project Tools/DesignValidation/CrisisCheckpointTests.csproj` (1.5s; standalone runner).
  - Always prefer Tier 1 for routine iteration. Zero Unity startup overhead, zero lockfile conflicts, zero pipeline drops.
- **Tier 2 (Interactive Live Editor)**: `unity-cli` (`unity status` / `unity command`).
  - Use opportunistically when an Editor GUI instance is open to inspect game objects or drive play mode.
  - **No Fallback on Drop**: If `unity status` is disconnected or the Pipeline socket drops, **NEVER fallback to headless batchmode**. Stay in Tier 1.
- **Tier 3 (Full Regression Gate — On-Demand Only)**: Headless Unity Editor batchmode (`Tools/run_editmode_tests.sh`).
  - Reserved strictly for explicit user-requested release checkpoints or full PlayMode/EditMode suite verification (832+ tests).
  - Never invoke automatically as a background fallback.
- **Zero Overengineering**: Never write Python test wrapper scripts or attempt headless display (`xvfb`) harnesses.
- **Inspect Logs**: Check `~/.config/unity3d/DefaultCompany/one-roof/TestResults.xml` and `Editor.log` directly when examining editor test results.

### Performance Guarantees
- **0 Steady-State GC**: Maintain zero heap allocations per tick in simulation and presentation loops (use value structs, retained dictionary capacities, and procedural meshes).
- **Hardware-Aware**: Cap concurrency to 4 threads (`-j4`) to respect the 4-core i5-4570 CPU.

---

## Tooling & Validation Workflow

### Quickstart Tech Spec

| Key | Specification |
|---|---|
| **Unity Version** | `6000.3.24f1` (Unity 6000.3 LTS) — defined in [`ProjectSettings/ProjectVersion.txt`](./ProjectSettings/ProjectVersion.txt) |
| **Unity Binary** | `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity` |
| **.NET CLI** | `/home/geisha/.dotnet/dotnet` (or `dotnet`) (preferred fast compilation toolchain) |
| **Project Path** | `/home/geisha/Vibecode/UnityAI/one-roof` |
| **Render Pipeline** | Universal Render Pipeline (URP 17.3.0) |
| **Assemblies** | `OneRoof.Domain` & `OneRoof.Application` (`noEngineReferences: true`, 0 `UnityEngine` refs) |
| **Active Baseline** | 832 EditMode tests passing, 15 PlayMode tests passing |

### Essential Validation Commands
```bash
# Preferred fast compilation check (concurrent with live running Editor; no lockfile clash):
/home/geisha/.dotnet/dotnet build OneRoof.Presentation.csproj
/home/geisha/.dotnet/dotnet build OneRoof.Presentation.Tests.EditMode.csproj

# Headless Unity compilation check (requires Editor to be closed / no Temp/UnityLockfile):
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath . -logFile /tmp/compile.log

# EditMode tests with automated failure reporting (recommended):
Tools/run_editmode_tests.sh [-testFilter <Filter>]

# Or raw headless EditMode tests (NEVER pass -quit with -runTests!):
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults /tmp/editmode.xml -logFile /tmp/editmode.log
```

- **Validation & Pipeline Probing**: See [`Docs/WORKFLOW_AND_TESTING.md`](./Docs/WORKFLOW_AND_TESTING.md) for full details, worktree isolation, and live `unity command` telemetry/test probes (`unity-cli`).
- **Commit hygiene**: Always commit paired `.meta` files for any new assets. Keep generated directories (`Library/`, `Temp/`, `.plastic/`) untracked.
- **Sandbox & Tool Execution Boundaries**: Project workspace files are writable in the standard sandbox. However, `.git/` is mounted read-only, and Unity Editor assemblies/binaries reside on external paths (`/home/geisha/Unity/Hub/Editor/...` and `/home/geisha/.vscode/extensions/...`). Therefore, sandbox bypass (`BypassSandbox: true`) is required for: (1) Git commands mutating index or history (`git add`, `git commit`, `git checkout`, `git stash`), (2) Headless Unity Editor invocations, and (3) `dotnet` compilation/runs referencing Unity-generated project files (`OneRoof.*.csproj`).
- **Tool Parameter Hygiene**: Never provide `ArtifactMetadata` to `write_to_file` when writing files in the workspace (only valid for `<appDataDir>/brain/`). Use `replace_file_content` for editing existing repository source code.
- For documentation-only changes, state that Unity validation was not run.
