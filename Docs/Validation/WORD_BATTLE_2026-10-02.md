# Learned Wit pilot validation — 2026-10-02

Implemented the Merchant Guild word-battle pilot with three tiers, twelve authored rhyming replies, saved learning, first-to-two exchanges, daily loss retries, reward-free practice and permanent respect. Shared Resident teaching grants one completed lesson per in-game day. Evidence/inventory systems are excluded.

Three authorized implementation subagents owned domain/commands/persistence, faction respect/projections, and application/UI respectively. Independent cross-review caught practice-save classification, reward wording and modal lifecycle issues; the coordinator integrated fixes and ran validation. Existing diversified-Tower changes remain in the working tree.

## Results

Unity 6000.3.24f1 compiled runtime and test assemblies during these runs:

| Run | Passed | Failed | Skipped | Durable result |
|---|---:|---:|---:|---|
| Full EditMode regression | 1,160 | 0 | 3 | [XML](WordBattle_2026-10-02/word-battle-all-editmode.xml) |
| Full graphical PlayMode regression | 19 | 0 | 1 | [XML](WordBattle_2026-10-02/word-battle-all-playmode.xml) |
| Final focused EditMode, after result-save/UI refinement | 19 | 0 | 0 | [XML](WordBattle_2026-10-02/word-battle-final-editmode.xml) |
| Final graphical word-battle acceptance and capture | 1 | 0 | 0 | [XML](WordBattle_2026-10-02/word-battle-final-playmode.xml) |

The three EditMode skips are existing benchmark/graphical fixtures; the PlayMode skip is an existing optional Caretaker diagnostic. The first full EditMode run exposed a schema-version assertion expecting faction version 4; it now expects version 5, which persists respect provenance, and the full rerun passed. Old version-4 factions still restore with zero respect.

Behavior checks cover learned rewards on losses, retry timing, mastered-tier practice, actual civil-action thresholds, extreme hardship, weighted overlays, unchanged individual support, stale commands, deterministic challenge resume, corrupted session recovery, daily teaching limits and JSON serialization. Graphical acceptance exercises chat cancellation/learning, actual Guild victory, pause/input ownership, Caretaker modal exclusion and disabling the result panel.

Both pure assemblies retain `noEngineReferences: true`. No new engine references were introduced. Every new Unity asset has a paired `.meta`; whitespace validation passed with existing CRLF line endings respected. A dotnet restore required single-node operation, and Unity required sandbox bypass for its existing license IPC/cache. Unity test runs provide the authoritative compilation/test evidence.

## Visual review and entry points

The final 1920×1080 captures were inspected. The compact panel shows a single current challenge, legible choices, a learned-reply card and permanent respect separately from the actual current tension drop. At zero raw pressure, actual relief is zero while earned respect remains available for future pressure.

- [Active battle](WordBattle_2026-10-02/word-battle-active.png)
- [Victory and learned reply](WordBattle_2026-10-02/word-battle-result.png)
- [Resident teaching conversation](WordBattle_2026-10-02/word-battle-chat.png)

Open Manage or Inspect to challenge the Guild leader or search learned replies. Inspect a Resident to open a teaching chat. Keys 1–4 select replies; 0 concedes; Escape suspends a battle or closes a chat/result. A live Guild contributor is preferred as spokesperson; an eligible commercial/service worker excluding office staff provides a deterministic early-Tower fallback without changing membership.

The authored content remains a Guild pilot. Other factions can consume the same respect/escalation machinery but do not yet have authored battles. Merchant Guild has no existing civil-action record; its relief is saved, displayed and applied to its organized signal without inventing a Guild strike. The pilot catalog is immutable C# with validation; a Unity asset/visual graph authoring tool is future work. No new performance-budget or City Status certification is claimed.
