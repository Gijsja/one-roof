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
3. **Pure-C# Domain purity**: `OneRoof.Domain` and `OneRoof.Application` strictly maintain `noEngineReferences: true` (0 `UnityEngine` references).
4. **Projections only**: Presentation and UI consume immutable projections bound by stable entity IDs. UI dispatches validated commands via `TowerSimulation.CanExecute()`, never directly mutating simulation state.
5. **No per-agent Update loops**: Simulation runs on a fixed discrete tick (1440 ticks/day). Views are pooled and presentation-interpolated.
6. **Aggregate persistence**: All serialization lives in domain sub-aggregates (`ToSaveData()` / `FromSaveData()`). Zero scene references in saves.
7. **Strict Handoff Hygiene**: Maintain **strictly ONE active handoff** in `Handoffs/Active/`. Keep it concise, authoritative, and focused on current task status.

---

## Tooling & Validation Workflow

### Quickstart Tech Spec

| Key | Specification |
|---|---|
| **Unity Version** | `6000.3.24f1` (Unity 6000.3 LTS) — defined in [`ProjectSettings/ProjectVersion.txt`](./ProjectSettings/ProjectVersion.txt) |
| **Unity Binary** | `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity` |
| **Project Path** | `/home/geisha/Vibecode/UnityAI/one-roof` |
| **Render Pipeline** | Universal Render Pipeline (URP 17.3.0) |
| **Assemblies** | `OneRoof.Domain` & `OneRoof.Application` (`noEngineReferences: true`, 0 `UnityEngine` refs) |
| **Active Baseline** | 832 EditMode tests passing, 15 PlayMode tests passing |

### Essential Validation Commands
```bash
# Fast compilation check
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath . -logFile /tmp/compile.log

# EditMode tests (NEVER pass -quit with -runTests!)
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults /tmp/editmode.xml -logFile /tmp/editmode.log
```

- **Validation & Pipeline Probing**: See [`Docs/WORKFLOW_AND_TESTING.md`](./Docs/WORKFLOW_AND_TESTING.md) for full details, worktree isolation, and `pipeline_client.py` telemetry probes.
- **Commit hygiene**: Always commit paired `.meta` files for any new assets. Keep generated directories (`Library/`, `Temp/`, `.plastic/`) untracked.
- **Sandbox & Git Operations**: Project workspace files are writable in the standard sandbox, but `.git/` is mounted read-only. Use standard shell commands (`mv`, `cp`, `mkdir`) inside sandbox. Any Git commands mutating repository index or history (`git add`, `git commit`, `git checkout`, `git stash`) require sandbox bypass (`BypassSandbox: true`).
- For documentation-only changes, state that Unity validation was not run.
