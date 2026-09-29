# UNDERCITY-001 — Secret Undercity expansion

## Implemented

- Expanded the independent excavation board to 32×12 cells and centered old 16×6 saves. A lobby access core, shaft, reachable corridors, and rectangle zoning support fourteen distinct rooms with command validation and placement feedback.
- Connected daily underground staffing, supplies, intel, research, upkeep, outside contract income, Scrutiny, tower services, and systems policies to the deterministic domain. Investigator visits use a saved noncombat corridor route and recoverable disruption.
- Restored articulated skeletal NPC visuals and street-edge arrival routes. Refined earth, cutaways, rooms, camera focus, construction tools, utilities panel, and the management inspector. Underground workers are shown by stable resident ID.
- Updated the canonical vision, architecture, data, UX, and decision documents. DIG-001's handoff is archived.

## Validation

- Isolated worktree: `/home/geisha/Vibecode/UnityAI/one-roof-undercity-20260927-validation`, Unity `6000.3.24f1`.
- Headless compile exit 0: `/tmp/one-roof-undercity-compile-2.log`.
- Full EditMode: 688 passed, 0 failed, 1 skipped opt-in benchmark: `/tmp/one-roof-undercity-editmode-release.xml`.
- Full graphics-enabled PlayMode: 9/9 passed: `/tmp/one-roof-undercity-playmode-release.xml`. The staffed-room visual capture passed again after the final timing adjustment: `/tmp/one-roof-undercity-visual-final.xml`.
- Focused inbound/outbound street-to-lobby route assertion: 1/1 passed: `/tmp/one-roof-undercity-entrance-final.xml`.
- Three 1280×720 PlayMode images are in `/tmp/one-roof-undercity-visual/`. The final room capture shows readable labels, a connected access shaft, and a staffed skeletal worker. `git diff --check` passed.
- The 300-resident presentation cap test passed: at most 40 pooled tower views plus at most 24 underground crew views, without duplicate IDs between the two visible sets.
- Standalone 300-resident Domain timing with fourteen rooms: p95 varied from 0.729 to 5.219 ms across host-load runs. Paired no-room and fourteen-room runs showed comparable intermittent spikes; no specific underground hotspot was isolated. The strict p95 <4 ms test is opt-in and was skipped by the routine suite.

## Risks and next safe action

- Review the PlayMode captures in the Editor, especially street approach, entrance crossing, underground framing, room text, and staff poses. The fast-forward visual fixture can bunch unrelated tower residents near the lobby; normal PlayMode movement remains covered by focused tests.
- Profile integrated Unity play on reference hardware before claiming the <4 ms tick and 60 FPS presentation targets. Standalone tick samples were host-load-sensitive.
- The main checkout contains concurrent FrankMiller/Noir work; leave it intact and exclude it from isolated validation of this task.
