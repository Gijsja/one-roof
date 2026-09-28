# OR-906A social ties — active handoff

## Scope and result

Anthropology and RimWorld design reviewers examined M9.5 before implementation. Both identified automatic friendship/rivalry from co-presence or faction alignment as the first credibility failure. This change makes sampled encounters neutral, strengthens a tie only after two daily contacts with a common named active grievance, returns affinity gently toward neutral, and removes low-ID preference from crowded contact buckets. The resident inspector now shows counterpart ID, affinity band and trend, recorded cause, and last meaningful tick. Social save data advances to version 2 with neutral defaults for new fields in older saves. No UnityEngine reference enters Domain.

OR-906A remains **in progress**. No existing domain event identifies an attributable resident-to-resident conflict, so the implementation does not invent one from faction alignment or another indirect signal. Inspector text names counterparts but does not yet provide clickable counterpart navigation. OR-906B and OR-906C have not started because the roadmap requires an OR-906A phase gate first.

## Validation

- Isolated managed worktree: `/home/geisha/.codex/worktrees/or-906a-validation/one-roof`, created from HEAD. Only the five scoped C# source/test files were applied there; unrelated shared-tree presentation and concept-art changes were not copied. Unity also changed `ProjectSettings/ProjectSettings.asset` inside that validation worktree; it was not copied back.
- Compile: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -quit -projectPath /home/geisha/.codex/worktrees/or-906a-validation/one-roof -logFile /tmp/or-906a-final-compile-3.log` → exit 0.
- Focused EditMode: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /home/geisha/.codex/worktrees/or-906a-validation/one-roof -runTests -testPlatform EditMode -testFilter FactionStateTests -testResults /tmp/or-906a-focused-final.xml -logFile /tmp/or-906a-focused-final.log` → exit 0, 8/8 passed.
- Full EditMode: same command without `-testFilter`, results `/tmp/or-906a-editmode-final2.xml`, log `/tmp/or-906a-editmode-final2.log` → exit 0, 717 passed, 0 failed, 1 skipped (the existing 300-resident underground p95 performance test).
- `git diff --check` → exit 0.

## Next safe action

Find or introduce a concrete, saved resident-to-resident conflict event tied to a named contested resource or interaction, then test repeated conflict, recovery, migration, and counterpart navigation. Recheck the 50-resident formation/recovery calibration and 300-resident performance on reference hardware before marking OR-906A done. Only then start OR-906B: transmit named grievance awareness from prior-day state with a small cap, keeping direct conditions dominant. For OR-906C, use issue-specific affected regions rather than home floor for workplace actions; test pressure and connected local capacity separately. Both reviewers flagged these as important causal-accuracy risks.
