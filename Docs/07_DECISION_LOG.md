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

