# ECON-005 Completion Audit

## Current status

ECON-005 is complete. Review found the outside credit counterparty had counted credit issuance as cash, household trends were not persisted, and arrears affected only inspector text. This pass corrects those paths and replaces mixed resident layers with one full-body procedural sprite.

## Work in this pass

- Outside purchases add only cash actually paid to market cash; issued credit is tracked as a receivable and becomes cash only when repaid. Version 0 market saves migrate their inflated historical cash balance.
- Household daily budget flow, rolling 7/30-day pressure, essential shortfall, and slow recovery exposure are persisted. Persistent deficits and denied essentials feed wellbeing; negative 30-day trajectory wears rooms, and insolvency starts a recoverable lease notice even while rent is current. Rent arrears independently continue to affect rooms and notices.
- Residents render one complete seeded pixel body sprite. Incompatible wardrobe photo layers and anatomy part renderers stay disabled, preventing mixed-style collage rendering.
- Room hardship projections are cached per simulation revision so presentation does not allocate a new household list every render frame.
- A real simulation-level outside meal transaction now verifies once-only billing and hunger recovery. The complete accumulated-deficit-to-departure chain is covered by adjacent budget, lifecycle, persistence, and typed-trip acceptance tests rather than a single monolithic simulation test.

## Validation

- Isolated copy: `/tmp/one-roof-econ005-validation-final` (created with `rsync`; Git worktree creation is unavailable because `.git` is read-only).
- Unity script and test assemblies compiled successfully (`Tundra build success`, no C# compiler errors) in `/tmp/one-roof-econ005-editmode-insolvency.log`.
- EditMode command: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-econ005-validation-final -runTests -testPlatform EditMode -testResults /tmp/one-roof-econ005-editmode-insolvency.xml -logFile /tmp/one-roof-econ005-editmode-insolvency.log`
- EditMode result: exit code 0; 663/663 passed, including financial-insolvency notice/recovery, 30/180/365-day hardship, and the simulation-level outside food test.
- PlayMode command: `timeout 1500 /home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity -batchmode -nographics -projectPath /tmp/one-roof-econ005-validation-final -runTests -testPlatform PlayMode -testResults /tmp/one-roof-econ005-playmode-insolvency.xml -logFile /tmp/one-roof-econ005-playmode-insolvency.log`
- PlayMode result: exit code 0; 8/8 passed.

## Next safe action

The implementation and targeted validation are complete. Coverage composes: real simulation daily wages and food billing; outside credit and rolling resilience/save-load; 30/180/365-day deficits; reversible housing notice and room-condition projection; and typed move-out arrival and departure history. For deeper future coverage, a single end-to-end hardship-and-departure scenario can combine these already tested stages.
