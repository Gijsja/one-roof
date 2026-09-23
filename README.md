# One Roof

A deterministic vertical-city simulation: the Steward shapes architecture, infrastructure, leases, and policy, while autonomous residents respond.

Built with **Unity 6000.3 LTS** (`6000.3.24f1`), Universal Render Pipeline (URP), and a pure C# Domain simulation engine.

---

## North Star

**Beta Boundary (City Status)**:
- 30 floors and 300 persistent residents running deterministically.
- Full mixed-use zoning: residential, office, retail, diner, clinic, workshop, security, and utility spaces.
- Physical infrastructure networks: power, water, gravity waste, and maintenance wear.
- Social dynamics: autonomous needs, satisfaction, grievances, strain, 4 faction archetypes, and Steward policy decrees.
- 8 data overlays and adaptive crisis event chains.
- Sustaining **City Status** for 30 in-game days under strict performance budgets (<4 ms simulation tick, 60 FPS presentation).

---

## Architectural Layers

```text
Assets/OneRoof/
├── Runtime/
│   ├── Domain/         # Pure C# simulation (0 UnityEngine references). State, topology, transit, economy, population.
│   ├── Application/    # Commands, use cases, ports, and immutable read-only projections/snapshots.
│   ├── Infrastructure/ # Persistence adapters, atomic file save store, schema version migrations.
│   ├── Presentation/   # Unity cutaway world views, pooled Spine NPC views, parallax city, atmosphere.
│   ├── UI/             # StewardTheme, ModeShellBarController, deep inspection cards, placement preview.
│   └── Content/        # Immutable authored definitions, sprite registries, wardrobe catalogs.
├── Editor/             # Scene builders, content validators, AssetLab authoring tools.
└── Tests/
    ├── EditMode/       # Fast unit and integration suites partitioned by layer assembly.
    └── PlayMode/       # Scene composition and golden acceptance test proofs.
```

### Core Invariants
1. **Systems over individuals**: Players influence systems (zoning, capacity, leases, decrees), never individual residents directly.
2. **Cause-chain explanation**: Every failure connects: `World symptom → Data overlay → Inspector cause → Systems response → Measured feedback`.
3. **Domain purity**: `OneRoof.Domain` has `noEngineReferences: true`. Presentation reads immutable projections bound by stable entity IDs.
4. **No per-agent Update loops**: Simulation executes on a fixed tick; presentation views are pooled and projection-driven.

---

## Agent Quick Start

1. **Read binding guidance**: [`AGENTS.md`](./AGENTS.md) and [`CONTEXT.md`](./CONTEXT.md).
2. **Consult canonical docs**:
   - [`Docs/01_GAME_VISION.md`](./Docs/01_GAME_VISION.md) — Product pillars and scope.
   - [`Docs/02_ARCHITECTURE.md`](./Docs/02_ARCHITECTURE.md) & [`Docs/03_DATA_CONTRACTS.md`](./Docs/03_DATA_CONTRACTS.md) — Runtime boundaries and records.
   - [`Docs/04_UX_CONTRACT.md`](./Docs/04_UX_CONTRACT.md) & [`Docs/09_CAUSE_CHAIN_INSPECTOR.md`](./Docs/09_CAUSE_CHAIN_INSPECTOR.md) — Overlays and inspectors.
   - [`Docs/12_ECONOMY.md`](./Docs/12_ECONOMY.md) — Unit economics and cash conservation.
   - [`Docs/10_DEVELOPMENT_WORKFLOW.md`](./Docs/10_DEVELOPMENT_WORKFLOW.md) — Headless test and compile commands.
3. **Check status**: Inspect [`Planning/BACKLOG.md`](./Planning/BACKLOG.md) and [`Planning/ROADMAP.md`](./Planning/ROADMAP.md).
4. **Handoffs**: Check [`Handoffs/Active/`](./Handoffs/Active/) for current work; archive completed handoffs to [`Handoffs/Archive/`](./Handoffs/Archive/).

---

## Headless Validation Commands

All Unity validation is run headlessly in batchmode from an isolated worktree:

```bash
# 1. Compile check (must exit 0)
/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /path/to/worktree \
  -logFile /tmp/one-roof-compile.log

# 2. Run EditMode tests (never pass -quit with -runTests)
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/worktree \
  -runTests -testPlatform EditMode \
  -testResults /tmp/one-roof-editmode.xml \
  -logFile /tmp/one-roof-editmode.log

# 3. Run PlayMode tests (when modifying presentation or acceptance loops)
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/worktree \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/one-roof-playmode.xml \
  -logFile /tmp/one-roof-playmode.log
```
