# One Roof (v0.10)

A deterministic vertical-city simulation: the Steward shapes architecture, infrastructure, leases, and policy, while autonomous residents respond.

Built with **Unity 6000.3 LTS** (`6000.3.24f1`), Universal Render Pipeline (URP), and a pure C# Domain simulation engine (`noEngineReferences: true`).

---

## Prototype Baseline (v0.10)

- **30-Floor Scale Proven**: In `Tower_GoldStandard30.unity`, performance optimizations (stair navigation routing, single `SRPDefaultUnlit` pass, GPU instancing) delivered **171 draw calls** (-98.8%), **86 batches** (-98.9%), and **~108 FPS** (9.27 ms frame time) with 0 console errors and 92 concurrent residents using stairs.
- **Closed-Loop Economy**: Cash conservation strictly enforced across households, businesses, tower treasury, and the Outside market. 365-day multi-scenario validated.
- **Physical Utilities & Undercity**: Subterranean 32×12 excavation grid, 14 specialized rooms, and continuous power/water networks connected to surface risers.
- **Factions & Decrees**: 4 faction archetypes, policy decree management panel with preview, civil actions (Rent Strike, Lobby Protest, Work Slowdown), and immutable decision log.
- **8 Data Overlays**: All 8 contracted diagnostic overlays + Scrutiny fully implemented.
- **Weather & Atmosphere**: 12-month climate cycle with 6 conditions, single-draw-call procedural weather particle mesh, volumetric window lighting cones, and rooftop architectural dressing.
- **Automated Test Suite**: **765 automated tests** (754 EditMode passing, 11 PlayMode passing, 0 failures, 1 opt-in benchmark skipped).

---

## North Star: Beta Boundary (City Status)

Sustaining **City Status for 30 consecutive in-game days** under strict performance budgets:
- Simulation tick: **<4.0 ms p95** on reference hardware.
- Presentation frame rate: **60 FPS** (<16.6 ms main thread time).
- 6 adaptive condition-driven crisis chains with personal resident consequences.
- Campaign board pacing, streak tracking, failure explanations, and post-campaign sandbox play.

---

## Architectural Layers

```text
Assets/OneRoof/
├── Runtime/
│   ├── Domain/         # Pure C# simulation (0 UnityEngine refs). Topology, transit, economy, population, utilities.
│   ├── Application/    # Pure C# ports, commands, and immutable read-only projections/snapshots.
│   ├── Infrastructure/ # Persistence adapters, atomic file save store, schema version migrations.
│   ├── Presentation/   # Unity cutaway world views, pooled Spine NPC views, parallax city, atmosphere, VFX.
│   ├── UI/             # StewardTheme, ModeShellBarController, deep inspection cards, placement preview.
│   └── Content/        # Immutable authored definitions, sprite registries, wardrobe catalogs, props.
├── Editor/             # Scene builders, content validators, AssetLab authoring tools.
└── Tests/
    ├── EditMode/       # Fast unit and integration suites partitioned by layer assembly (754 tests).
    └── PlayMode/       # Scene composition and golden acceptance test proofs (11 tests).
```

### Core Invariants
1. **Systems over individuals**: Players influence systems (zoning, capacity, leases, decrees), never individual residents directly.
2. **Cause-chain explanation**: Every failure connects: `World symptom → Data overlay → Inspector cause → Systems response → Measured feedback`.
3. **Domain purity**: `OneRoof.Domain` and `OneRoof.Application` have `noEngineReferences: true`. Presentation reads immutable projections bound by stable entity IDs.
4. **No per-agent Update loops**: Simulation executes on a fixed tick (1440 ticks/day); presentation views are pooled and projection-driven.

---

## Playable Scenes

The project includes 5 canonical scenes under `Assets/Scenes/`:
- `Tower_GoldStandard30.unity`: 30-floor / 300-resident reference scale playground running at ~108 FPS and 171 draw calls.
- `Tower_GroundStart.unity`: Dynamic ground-start playground starting at Floor 0 foundation.
- `Tower.unity`: Authored 5-floor first-playable scene.
- `Testbed_Transit.unity`: Isolated 5-floor vertical transit harness wired to `FiftyResidentFixture`.
- `AssetLab.unity`: Content and 17-bone rig preview and validation lab.

---

## Agent Quick Start

1. **Read binding guidance**: [`AGENTS.md`](./AGENTS.md).
2. **Check active task**: Inspect [`Planning/BACKLOG.md`](./Planning/BACKLOG.md) and [`Handoffs/Active/`](./Handoffs/Active/).
3. **Read on demand**: Consult only the specific canonical doc in `Docs/` relevant to your assigned task (see `AGENTS.md`).

---

## Validation & Probing Commands

### Headless Unity Tests
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

### Python Tooling & Live Editor Probing
When the Unity Editor is running on port 7800 with `com.unity.pipeline`:
```bash
# Live performance stats (draw calls, batches, frame times)
python3 pipeline_client.py cmd get_performance_stats

# Live console status and error count
python3 pipeline_client.py cmd console_status

# Evaluate expressions in the running scene
python3 pipeline_client.py eval "TowerPlayableController.Instance.CurrentTick"
```
