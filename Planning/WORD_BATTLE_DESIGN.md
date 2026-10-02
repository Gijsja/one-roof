# Learned Wit — Faction Word Battles

Implemented pilot, 2026-10-02. Supersedes the dialogue/evidence proposal. Beta dispatch gates remain unchanged; this pilot does not replace any required crisis chain.

## Experience

The Steward learns memorable rhyming replies, recognizes a leader's argument, and outwits them in public. Victories earn lasting respect and lower faction tension. Even a loss expands the Steward's repertoire.

Use short rhyming call-and-response rather than five-line limericks by default. A leader's attack and its successful reply end in a matching rhyme and reverse the argument. Full limericks can be milestone presentation later. Keep political stakes serious, with wit in the wording.

No evidence records, investigation branches, inventory objects, drug-operation simulation, public disclosure or claim verification in this version. No free-text judging. All lines and valid matches are authored.

## Battle and learning loop

- Pilot: one Merchant Guild spokesperson, three difficulty tiers, four reply families per tier, twelve learnable replies total. The shared engine supports all four existing factions; their content is later expansion.
- Begin with one learned reply in the first tier. Resident chats teach additional replies without tension rewards, limited to one completed lesson per in-game day. Canceling a lesson does not consume the allowance; the allowance persists through saves. Smaller encounter content is a future extension.
- Each battle has three exchanges drawn from distinct families in the current tier. Correct reply scores one point for the Steward; any mismatched reply or explicit concession scores one for the leader. First to two points wins. No response timer or random success roll.
- Select challenges deterministically from the encounter sequence, rotating through the tier's four families. Each family has ordinary and leader-specific challenge wording; the same learned reply answers both. Later tiers introduce new pairs, not a random rule change.
- Offer up to four learned replies from the current tier plus “I have no answer.” Include the correct learned reply whenever it is known; unknown replies are not selectable. Fill other slots with mismatched known replies, ordered deterministically without marking correctness. Rhyming distractors should require understanding the argument, not just matching the final sound.
- After each exchange, the leader reacts and feedback explains the rhetorical match or mismatch. Seeing the wording does not create multiple rewards.
- Every completed non-practice encounter unlocks one previously unknown reply in the current tier, win or lose. Prefer an unknown family actually faced; otherwise unlock the next unknown family in stable content order. Teach through an authored closing exchange and show “New reply learned.”
- If all current-tier replies are known, introduce the next unknown reply from a later tier at completion, even if the current battle was lost. Learning an advanced line does not unlock that tier's battle. Once all twelve are known, explicitly show “Repertoire complete”; do not promise unlimited new content or disguise repeated lines as new unlocks.
- First victory in a tier grants permanent faction respect and unlocks the next tier. Already-defeated tiers are practice only: no new respect or encounter rewards. A failed current tier can be retried on the next in-game day. Suspend/resume retains the same challenges and scores; restarting or reloading cannot reroll or duplicate unlocks.
- Pausing before completion grants nothing. Learned replies and completed tier victories persist for the campaign. Losing never removes learned replies, reduces earned respect or adds punitive tension; baseline simulation pressure continues normally.

## Dialogue graph

```text
START: freeze faction/spokesperson IDs, tier, encounter sequence and challenges
  OPEN: leader delivers the first rhyming challenge
    CHOOSE learned reply / concede
      MATCH: Steward +1; leader acknowledges the turn
      MISMATCH: leader +1; authored rebuttal explains the miss
      IF neither side has 2 points: deliver next challenge → CHOOSE
      IF Steward has 2: WIN
        record first tier victory and earned respect once → LEARN
      IF leader has 2: LOSE
        retain all existing progression → LEARN
  LEARN: unlock one unknown reply according to progression rules
    RESULT: score, new reply, total mastered tiers and actual tension reduction
      CLOSE: record completed encounter, retain transcript
  SUSPEND (available before completion): save current node and resume later
```

Example paired lines, with neutral political arguments rather than hidden facts:

| Argument | Leader challenge | Learned Steward reply |
|---|---|---|
| Burden sharing | “We set the course; the rest must pay.” | “Then lead by sharing costs today.” |
| Authority | “Our rank alone proves we are right.” | “A title cannot win this fight.” |
| Empty promises | “Just trust our pledge: the future's bright.” | “A pledge alone won't keep the light.” |
| Necessity | “There is no choice; accept the strain.” | “Then say who bears it, who will gain.” |

Example harder challenge for the same burden-sharing reply: “For common good, the weak must pay.” The Steward recognizes the same family beneath new wording. Localized pairs must preserve rhyme and meaning, while stable family IDs preserve matching rules. These examples are draft copy; publish only after editorial review for rhythm, natural wording and genuinely plausible distractors.

## Lasting tension relief

Use saved **earned respect**, not a direct write to daily derived faction pressure. Initial balance defaults: each distinct tier victory grants 0.10 relief (ten percentage points), capped at 0.30 per faction after three tiers. It does not decay or reset when a spokesperson changes. These are tunable gameplay values, not validated balance.

- Keep raw Resident support, grievances, membership and `DirectPressure` unchanged. Earned respect affects the faction's willingness to escalate, not individual allegiance.
- Display `effectiveTension = max(0, rawFactionPressure − earnedRespect)`, alongside raw pressure and the earned reduction in the inspector. The result screen shows actual before/after reduction, which can be smaller than the awarded respect when raw pressure is already low.
- Daily evaluation recomputes raw pressure and reapplies saved respect. A win immediately refreshes the projection; it survives the next settlement and save/load. New hardship can still increase effective tension despite permanent respect.
- For real escalation, retain `BuildOrganizedSignals()` candidate eligibility and connected-group construction, then subtract the same respect from each faction's organized group pressure. `CivilActionState` consumes that effective signal through its normal next-settlement lifecycle. Do not directly clear phases or reset warning counters.
- Existing organized groups require raw direct pressure of at least 0.55. A fully mastered faction at 0.55 falls to 0.25, below the 0.30 warning threshold; an extreme group at 1.00 remains at 0.70 and can escalate. Early mastery reduces pressure but need not change a phase immediately.
- Tenant Union, Corporate Coalition and Civic Eco Council have existing civil-action records. Merchant Guild does not: its pilot win changes real saved/displayed faction tension and the organized signal, but no new Guild strike consequence is claimed. Verify lifecycle consequences using the existing factions' fixtures before expanding their dialogue content.
- Attribution follows the cause-chain: faction symptom → tension overlay → raw pressure and learned-wit relief in inspector → Steward wins a battle → lower effective tension and later escalation feedback. Material remedies remain available.

Long-term progression therefore has two visible tracks: the permanent repertoire that makes the Steward more capable, and permanent faction respect that reduces future escalation pressure. More victories from repeated easy content cannot neutralize the Tower.

## Shared framework and delivery

- Plain-C# immutable catalog: stable content/version, reply and family IDs, tiers, authored challenge variants, explanations and learning order. The pilot uses an explicit phase graph rather than an arbitrary graph interpreter. Catalog validation checks unique IDs and complete four-family tiers; a ScriptableObject authoring adapter is future work. Domain/Application retain zero Unity references.
- `DialogueState` owns the active session, frozen speaker, scores, deterministic challenge sequence, learned reply IDs, defeated faction/tier IDs and exactly-once outcomes. `FactionState` owns saved respect; victory applies through one validated command transaction.
- Commands: start, choose reply with expected session revision, suspend/resume and close. Validate with `TowerSimulation.CanExecute()`; UI consumes immutable dialogue/faction projections. Missing speaker or incompatible content ends the active session recoverably without granting an unfinished reward; existing learning and respect remain.
- Extend aggregate persistence with empty old-save defaults. Store IDs rather than scene/asset references. Store earned respect explicitly, with tier-award provenance preventing duplicate grants. No new generic inventory or evidence subsystem.
- Resident chats reuse speech/choice/learn/close nodes without battle scores. They can teach authored lines and acknowledge experiences; they do not command Resident behavior or grant faction respect.
- Shared panel: speaker, transcript, score, selectable lines and closing learning card; searchable repertoire panel. Use the existing exclusive modal pause/input ownership and restore prior speed on close. No per-Resident Update or per-tick dialogue graph evaluation.
- Implemented: validated catalog, deterministic progression and saves, faction relief adapters, shared panel and twelve-reply Guild pilot. Open the panel in Manage or Inspect mode; inspect a Resident to start a teaching chat. Keys 1–4 choose replies, 0 concedes, and Escape suspends a battle or closes a chat/result. Notebook search and mastered-tier practice are available. Existing Caretaker gameplay and beta chain requirements are preserved.

Acceptance checks: win/loss/concession and suspending each turn; one new reply per eligible completed encounter; new wording recognized by the same reply; correct learned choice always offered; finite repertoire completion; first-tier-only rewards and practice isolation; stale/duplicate commands; deterministic save/load replay; old saves and missing speakers/content; permanent relief survives settlement; raw support remains unchanged; actual civil-action pressure uses relief; severe crises can persist; graph IDs/edges/bounded loops/endings; keyboard/modal behavior and pause restoration.

Validation evidence is recorded in the active handoff. Content remains a Merchant Guild pilot; other factions reject battle commands until their authored content is added. Balance defaults are not campaign or performance certification.
