# Active handoff — Caretaker faction crisis and Kungfu eviction

Date: 2026-10-01. Status: functional OR-1002 Slice A delivered; performance gate open.

## Delivered

- Permanent lobby Caretaker contact, faction cause-chain, rehearsal and real enforcement entry. Press C; keyboard/gamepad controls are shown in the HUD.
- Sustained connected faction evidence produces warnings and escalation. Actual floor slabs and aligned stairs form the route; bounded successive fighter waves cover the full participant group, with a final floor boss.
- Pure-C# fixed-step combat, immutable projections, validated commands, save recovery paused, pause/retry/exit and exclusive input. Background management pauses without changing its prior pause state.
- Winning creates saved whole-household crisis eviction notices, existing autonomous departure, witness thoughts, scrutiny and decision history. Departing cash transfers to OutsideMarket. Practice restores prior crisis without resident consequences.
- Centralized pooled rigs/impacts reuse existing artwork. Ambient resident views are held during combat to avoid duplicate combatants/bubble overlap and restored afterwards.
- Fully articulated, jointed stick figure body with seamless ball-and-socket capsule pivots (upper arm -> lower arm -> hand; hip -> thigh -> shin -> foot; tapered gi vest; circular head).
- Articulated 2D ragdoll controller (`CombatRagdoll2D.cs`) using deterministic Verlet distance-constraint integration and floor ground collision on defeat/knockback. Zero 3D colliders (preserving Tower mouse-picking invariants).
- Replaced legacy dead `pipeline_client.py` references across `AGENTS.md` and `Docs/WORKFLOW_AND_TESTING.md` with official `unity-cli` (`unity status`, `unity command run_tests`, `unity command console_status`) and preferred `.NET CLI` toolchain.
- Three explicitly authorized subagents handled domain, presentation and independent validation. All new Unity files have paired metas. No scene asset changes or commits made by this task.

## Validation and limits

- Before concurrent work: full EditMode 938 passed, 3 skipped (one explicit budget benchmark and two graphical-only fixtures under -nographics).
- Final feature snapshot: crisis EditMode 20/20; ragdoll EditMode 5/5; resident presentation EditMode 37/37; full graphical PlayMode 16/16. Contact and combat HUD visually verified at 1920×1080; an initial nonbatch HUD run passed 1/1.
- Combat command p95 ~0.0025 ms. Explicit 1280×720 stairs view: 118 FPS / 168 draws; staged fight: 94.5 FPS / 181.6 draws (slightly above proposed 180-draw target).
- Native 1920×1080 Editor GameView + HUD: 36.1 FPS / 126 draws on GT 755M. The 60 FPS full presentation gate remains open; do not claim release performance certification.
- Last native HUD run reached gameplay assertions but failed on an asynchronous Unity AI package NoSubscription log (ModelSelectorSuperProxyActions.cs:495). Earlier functional suites remain valid; do not suppress or attribute this package error to crisis code.
- Arcade traversal uses real slabs/stairs; internal wall, door and furnishing collisions are not modeled. Accessibility/remapping polish remains open.

## Workspace and evidence

- Caretaker crisis slice (OR-1002 Slice A) delivered and validated with isolated fixtures and HUD captures.
- Progressive Outside Furniture & Empty Room Shells delivered and verified:
  - Residential studios start empty with bare wallpaper backdrop shell (0 clutter).
  - Autonomous residents progressively purchase 9 tiers of outside furniture (Bed -> Coat Rack -> Planter -> Sofa -> Coffee Table -> Study Desk -> Bookcase -> Kitchenette -> Game Table / Vanity) governed by savings and rent reserve safety.
  - Steady-state 0 GC allocation (`_activeHomeBuffer.Clear()`), cached sprite renderers, and unified URP material batching.
  - Visual verification in `Tower_GoldStandard30.unity` PlayMode: generated in-engine 1080p captures in brain artifacts comparing empty vs furnished apartments side-by-side.
  - Full EditMode test suite: 947 passed, 0 failed, 3 ignored (`/tmp/editmode.xml`). All paired `.meta` files committed.

[Design references](../../Planning/CARETAKER_ARCADE_GAMEPLAY.md) retain the C64 longplay extraction and original/developer sources. This slice does not complete the six-chain beta contract or City Status. OR-204, OR-906B/C, OR-1001 Slice C and OR-1100 remain delivered; OR-1006 accessibility remains incomplete. M11 follows City Status.

Commit boundary: the core Caretaker crisis is already in `14bc70d`. [Crisis delivery evidence and remaining limits](../../Planning/CARETAKER_ARCADE_GAMEPLAY.md#implemented-slice-and-commit-boundary--2026-10-01) record the pre-body validation; newer resident-body/ragdoll edits remain separate uncommitted work.


## Test review remediation — completed 2026-10-01

- Three authorized subagents repaired test assertions, PlayMode isolation, fixture sizing and scenario tables; independent cross-review retained meaningful Domain scenarios and restored weather unit contracts.
- Exterior tests now observe generated geometry/materials rather than invented constants; unsupported visual promises were removed or renamed to actual contracts. Exterior, placement, economy, leasing, business and outside-worker fixtures are split by responsibility.
- PlayMode fixtures own additive scenes with awaited cleanup; rain/fog image comparisons use same-frame controls. Caretaker capture/timing diagnostics remain opt-in. Golden commute acceptance now checks actual workplace arrival with an accurate name.
- Domain/Application guards reject modular `UnityEngine.*` references. Social/weather tests require their named prerequisites; migration and round-trip checks remain independent. All 14 new test scripts have paired metas.
- Production regression exposed and fixed: returning from a manual weather override to Auto now invalidates the same-tick sample cache (`TowerPlayableController.UpdateActiveWeather`).
- Final Unity validation: **1,131 EditMode passed, 0 failed, 3 skipped** (opt-in benchmark and two graphical-only fixtures); **17 graphical PlayMode passed, 0 failed, 1 skipped** (optional Caretaker diagnostic capture). XML/logs: `/tmp/one-roof-test-remediation-editmode-final.*` and `/tmp/one-roof-test-remediation-playmode.*`.
- Domain, Application, Presentation and Presentation EditMode test `.csproj` builds passed with zero warnings/errors (single-worker MSBuild where needed). `git diff --check` passed.
- Existing unrelated working-tree changes were preserved. No commit or release/performance certification was requested or made.
