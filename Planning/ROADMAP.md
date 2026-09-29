# One Roof Roadmap — Milestone v0.10 to Beta Boundary

This document tracks active development milestones toward the **Beta Boundary (City Status)**.
For historical delivery details (Milestones M0 through M8, ARCH prompts, and early audit logs), see [`Planning/Archive/ROADMAP_HISTORICAL_ARCHIVE.md`](./Archive/ROADMAP_HISTORICAL_ARCHIVE.md).

---

## 1. Project North Star & Beta Boundary Contract

**Beta Boundary (City Status)**:
- 30 floors and 300 persistent residents running deterministically.
- Full mixed-use zoning: residential, office, retail, diner, clinic, workshop, security, and utilities.
- Physical infrastructure networks: electrical grid, plumbing & gravity waste, technician wear.
- Social dynamics: autonomous needs, satisfaction, grievances, strain, 4 faction archetypes, policy decrees.
- 8 data overlays and 6 adaptive crisis event chains.
- Sustaining **City Status for 30 consecutive in-game days** under strict performance budgets:
  - Simulation tick: **<4.0 ms p95** on reference hardware.
  - Presentation frame rate: **60 FPS** (<16.6 ms main thread time).
  - Draw calls: **<180** (currently 171 via SRP Batcher and GPU instancing).

---

## 2. Milestone Summary & Progress

| Milestone | Scope | Status | Validation / Deliverable |
|---|---|---|---|
| **M0–M4** | Project foundation, transit graph, 50-resident loop, first playable | **DONE** | Golden first-playable test passed |
| **M5.1–M5.4** | Dynamic topology, build grid, 9-slice backdrops, inspect mode cards | **DONE** | Golden expansion passed |
| **M5.3a** | Architecture refactor: ARCH-001..005 (split God Presenter, pure Domain) | **DONE** | Pure C# Domain boundary verified (0 engine refs) |
| **M6.0–M6.3** | 17-bone Spine rig, resident needs/schedules, strain, Scrutiny, specialist roles | **DONE** | Autonomous resident wellbeing operational |
| **M7.1–M7.4** | Commercial leasing, business health, window volumetric lighting, 3-car limit | **DONE** | Commercial tenant lifecycle operational |
| **M8.1–M8.2** | Physical electrical grid, plumbing/waste networks, degradation & technician repair | **DONE** | Dual-layer utilities flow operational |
| **ECON** | ECON-001..008: Closed-loop unit economics, 365-day cash conservation | **DONE** | Multi-scenario cash conservation proofs |
| **M9.1–M9.4** | 4 Factions, policy decrees panel, civil actions, decision records, onboarding | **DONE** | Civil action proof & playable onboarding passed |
| **M10.1** | Connected building & secret undercity expansion (32×12 excavation, 14 rooms) | **DONE** | Surface-to-undercity power/water paths operational |
| **M10-SCALE** | 30-Floor Gold Standard City Playground (`Tower_GoldStandard30`), stair transit fix | **DONE** | Draw calls 14,585 -> 171 (-98.8%), ~108 FPS verified |
| **WEATHER** | 12-month multi-season weather cycle, procedural particle mesh (1 draw call) | **DONE** | Rain, snow, storm, fog, day/night lighting |
| **v0.10 Baseline** | Professional documentation upgrade, context shield, handoff archival | **DONE** | 765 passing tests, single active handoff |
| **M9.5** | Social ties deepening: resident conflict events, influence, local organizing | **IN PROGRESS** | OR-906A/B/C |
| **M10.2** | Adaptive crisis pressure chains & personal consequences | **READY** | OR-1002 |
| **M10.3** | Beta boundary golden acceptance test (City Status 30-day streak) | **READY** | OR-1003, OR-1005, OR-1006, OR-1007 |

---

## 3. Active Execution Gates

### 3.1 M9.5 Social Ties Deepening (In Progress)
- **OR-906A (Conflict Events)**: Add explicit resident-to-resident conflict event tied to a named contested resource or interaction before closing the OR-906A phase gate.
- **OR-906B (Social Influence)**: Strong recent ties share named grievances using prior-settlement state; inspectable influence separated from direct exposure.
- **OR-906C (Local Faction Organizing)**: Require sustained grievance pressure and connected local membership for civil action readiness.

### 3.2 M10.1 Connected Building Follow-up
- **OR-1001 Slice C**: Generator reverse-feed and utility-dependent subterranean production.

### 3.3 M10.2 Adaptive Crisis Pressure (OR-1002)
- 6 condition-driven chains: Power Grid Cascade, Water Contamination, Transit Collapse, Rent Boycott, Labor Strike, Building Inspection Crackdown.
- Structured with warning phases, propagation, management levers, and saved resident consequences.

### 3.4 M10.3 Beta Boundary Golden Acceptance (OR-1003)
- Sustaining City Status for 30 consecutive in-game days at 30 floors / 300 residents.
- Verification of <4.0 ms tick budget on designated reference hardware.
- Campaign board tracking (OR-1005), accessibility pass (OR-1006), and save migration QA (OR-1007).

---

## 4. Performance & Validation Tooling

### Headless Batchmode Tests
Run from an isolated Git worktree:
```bash
# EditMode tests (754 tests)
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/worktree \
  -runTests -testPlatform EditMode \
  -testResults /tmp/one-roof-editmode.xml \
  -logFile /tmp/one-roof-editmode.log

# PlayMode tests (11 tests)
timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/worktree \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/one-roof-playmode.xml \
  -logFile /tmp/one-roof-playmode.log
```

### Live Pipeline Probing (Python Tools & MCP)
When an Editor session runs with `com.unity.pipeline` active on port 7800:
- **Python Probing Script (`pipeline_client.py`)**:
  - `python3 pipeline_client.py cmd get_performance_stats`: Returns live draw calls, batches, setpass calls, and frame times.
  - `python3 pipeline_client.py cmd console_status`: Returns live console logs and error counts.
  - `python3 pipeline_client.py eval "<C# expression>"`: Evaluates simulation state dynamically.
- **Pipeline MCP Tools**: Agents equipped with MCP tools can directly call `mcp__unity__editor_status` and `mcp__unity__console_status`.
