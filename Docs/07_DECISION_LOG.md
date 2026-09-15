# Architecture Decision Log

Append decisions; do not silently rewrite history. Use `Handoffs/DECISION_TEMPLATE.md` for full records when needed.

| ID | Status | Decision | Reason |
| --- | --- | --- | --- |
| ADR-001 | Accepted | Pure C# domain separate from Unity presentation | Scale, testability, save stability |
| ADR-002 | Accepted | Stable integer runtime IDs and immutable string content IDs | Decouple simulation from assets and scenes |
| ADR-003 | Accepted | Hierarchical floor/transit routing | Make vertical travel scalable and inspectable |
| ADR-004 | Accepted | Persistent simulation entities with pooled visible views | Support many lives without many GameObjects |
| ADR-005 | Accepted | ScriptableObjects for definitions only | Prevent mutable campaign state leaking into assets |
| ADR-006 | Accepted | First playable precedes factions and asset generation | Prove the core cause-and-effect loop first |
| ADR-007 | Proposed | Unity 6 LTS + URP + 2D Animation + Addressables | Matches 2.5D presentation and content volume; confirm exact versions after project creation |
| ADR-008 | Accepted | Ship the initial release as a premium single-player desktop game using Unity 6000.3.24f1, URP, and GitHub-hosted Git | Confirms OR-001 from the product vision, installed project metadata, and established repository workflow |
| ADR-009 | Accepted | Use Unity Addressables 4.0.1 and 2D Animation 13.0.6 | These approved Package Manager resolutions implement ADR-007 for the current Unity 6000.3.24f1 project |
| ADR-010 | Accepted | Separate runtime concerns into named assemblies with pure Domain and Application layers | Compile-time references enforce the architecture boundary before simulation systems are added |
| ADR-011 | Accepted | Run Unity validation in GitHub Actions with GameCI v4 | The workflow tests Edit Mode and Play Mode separately, then compiles a Linux player from a clean checkout |
| ADR-012 | Accepted | Validate primitive values at construction; preserve random streams as xorshift64 seed/state/position snapshots | Gives save-local contracts unambiguous invalid-value boundaries and exact, portable replay after save/load |
| ADR-013 | Accepted | Expose transit simulation to presentation through Application projections | Keeps presentation dependent on read-only data and capacity actions without leaking mutable Domain state or Domain assembly types across the boundary |
| ADR-014 | Accepted | Model building spaces with discrete 1D cell bounds per floor and typed portal thresholds | Supports non-overlapping rooms, vertical and horizontal portal graphs, and deterministic pathfinding without continuous physics |
| ADR-015 | Accepted | Use a typed JSON envelope with embedded payload string, forward migrator pipeline, and atomic file replace | Guarantees clean corruption rejection, schema version evolution without full model reflection, and safe writes via temporary files |
| ADR-016 | Accepted | Connect horizontal walk edges and vertical elevator shaft edges into a unified hierarchical transit graph | Allows deterministic multi-floor route planning with explicit walk and elevator transit legs without continuous physics |
| ADR-017 | Accepted | Implement elevator banks as explicit multi-phase state machines with discrete travel, door cycle, and dwell ticks | Enforces strict car capacity, FIFO floor queuing, and deterministic transit timing |
| ADR-018 | Accepted | Project transit congestion through read-only floor and bank projections with discrete severity tiers | Enables explainable cause diagnosis (queue length and wait time) for presentation overlays and inspectors |
