# Caretaker arcade crisis — gameplay reference extraction

Date: 2026-09-30. Updated: 2026-10-01. Reference extraction retained below; the authorized OR-1002 Slice A implementation now provides lobby contact/rehearsal, faction warning, real-floor combat and saved eviction consequences. Validation details are recorded in the active handoff.

## Request and source

The Caretaker is permanently present in the lobby as a resident contact and becomes directly playable for the first crisis, fighting through the actual constructed Tower floor by floor. The requested references are Kungfu Master, Street Fighter and Oldboy, with Fisher Dave as minigame inspiration.

Primary inspected reference: [C64 Longplay - Kung Fu Master (HQ), MontyMole1976](https://www.youtube.com/watch?v=FtkWw7ZAXdA), 5:03. Inspected through browser playback and seeks across 0:30, 1:00, 1:31, 2:01, 2:31, 3:02, 3:32, 4:02 and 4:33. This is a gameplay extraction, not a video download, asset extraction or implementation. Timestamp samples are anchors, not exact transition boundaries. No original input recording, hitbox data or frame timing is available.

## What the footage establishes

| Observed feature | Evidence | Adaptation to One Roof |
|---|---|---|
| Side-on corridor combat with horizontal travel | Early and late samples show one walkable horizontal lane and a following view | Follow the Caretaker at close range through the Tower's actual corridors |
| Facing, close attacks and airborne kicks | 1:31 and 4:33 samples show airborne attack poses; early playback shows nearby opponents approaching | Movement, facing, punch, kick, duck and jump; jump-kick as a deliberate action |
| Repeated opponents and stronger duels | Repeated corridor opponents; late encounter at 4:33 shows opposing fighter and depleted enemy bar | Short approach waves followed by a distinct floor encounter with its own readable attack pattern |
| Stairs at corridor ends | Stairs visible near 1:00, 2:01 and 3:32 | Cleared route leads to an actual connected staircase in the constructed Tower |
| Five-floor progress and compact combat HUD | HUD labels 1F–5F, PLAYER, ENEMY and TIME throughout | Show actual floor ID, cleared/remaining route, Caretaker health, current major opponent health and encounter clock |
| Arcade score and remaining-character icons | Score and head icons visible across samples | Optional encounter score/retry display, separate from treasury and resident satisfaction |

The footage does not establish exact attack startup/recovery, damage, hitbox extents, invulnerability, original key bindings, all enemy rules or failure/save behavior. Define and playtest those explicitly. Do not infer implementation numbers from the longplay.

## Proposed playable loop

1. Lobby: the Caretaker is visible and selectable; resident contact behavior remains available between encounters.
2. Warning: identify the affected floors and give the opening crisis a concrete cause and objective.
3. Enter direct control: switch camera and inputs to arcade play; expose controls and pause/retry/exit.
4. Traverse and fight: walk through the actual corridor geometry, face threats, use close strikes and jump/duck responses, and avoid being surrounded.
5. Floor encounter: defeat a stronger opponent with a distinct telegraph and recovery window. Mark the floor objective resolved and expose the connected stair route.
6. Ascend: travel through the Tower's existing vertical connection to the next affected floor. Direction follows the constructed layout, rather than inventing alternating stairs.
7. Resolve or fail: show floor results and attributable crisis consequences; restore management controls and return the Caretaker to the lobby contact role.

Punch/kick/duck controls and the exact warning/ending are proposed design, not a recovered original control scheme. A representative multi-floor prototype is the first validation slice, not the full final scope: the finished encounter must support all eligible floors and variable Tower layouts.

## Combat feel and encounter composition

Keep the primary challenge in spacing, facing and timing. Give strikes explicit windup, active and recovery phases; resolve contacts once per attack and show unambiguous impact feedback. Start with separate punch and kick actions, duck, jump and jump-kick. Add complex combos only if playtesting justifies them. Street Fighter and Oldboy describe requested action feel; their specific systems were not inspected in this reference pass.

Proposed encounter roles: approaching close fighter, a ranged threat that prompts duck/jump timing, and a floor opponent with a recognizable attack/recovery cycle. These are adaptation choices; no claim is made that every role was demonstrated by the sampled footage. Spawn at real reachable entrances, with readable arrival cues and bounded active combatants. Avoid spawning inside residents, closed rooms or the Caretaker.

Use the Tower's existing visual identity, rooms and furnishing. Combatants need a defined narrative identity; do not automatically turn dissatisfied residents or factions into enemies. The direct-control exception applies to the Caretaker. Residents continue to own their routines and social decisions.

## Real-Tower integration requirements

- Derive route and ground collision from real floor/corridor geometry and stable floor IDs; never substitute a stock arena.
- Validate connectivity before starting. Disconnected floors need a visible unavailable-route reason and a systems response, not teleportation or a permanently trapped encounter.
- Handle short floors, multiple stairs, doors, room edges and elevator layouts explicitly. Route progression cannot assume the reference game's five-floor geometry.
- Define the simulation clock during combat. Recommended prototype default: freeze background simulation and construction while the arcade encounter advances on its own fixed step; restore the prior simulation speed on exit. This is a proposal pending implementation design.
- Keep arcade controls exclusive while active so strikes and movement cannot also trigger building, demolition, weather, overlays or camera shortcuts. Support remapping and text/glyph controls.
- Keep encounter state authoritative in pure C# and expose immutable projections. Use centralized combat stepping and pooled presentation, with no per-resident Update loops.
- Define saved encounter state, interrupted-session recovery, repeat-entry behavior and lobby restoration before production integration. Save IDs and logical state, never scene references.
- Resolve how the opening arcade crisis relates to OR-1002's six condition-driven chains. No existing beta gate is silently replaced by this reference extraction.

## Acceptance for implementation

- The Caretaker is always available in the lobby between sequences, with an accessible entry point for contact and play.
- Direct control traverses multiple real floors, uses actual stairs and works after layout changes before encounter start.
- Punch, kick, duck, jump and jump-kick have visible response, bounded timing and consistent collision rules; attacks cannot repeatedly damage the same target within one active window.
- Corridor pressure, a distinct floor opponent and floor clearance form a complete encounter; completion, defeat, retry and exit reliably restore the lobby and management input.
- No unreachable route, short-floor spawn, deleted-floor reference or save interruption strands the encounter.
- Residents remain autonomous; crisis outcome has an explicit cause-chain and documented effects.
- Validate deterministic combat stepping, route selection, save recovery and input separation with relevant tests, then verify camera, animation, collision and full encounter in Unity.
- Measure integrated performance at 30 floors / 300 residents against the existing beta budgets.

## Validation of this extraction

Video identity, duration, displayed HUD, combat poses and stair geometry were inspected in browser playback. Design proposals are labeled separately from observations. Documentation-only changes; Unity compilation, tests and runtime implementation validation were not run.

## Online design references — researched 2026-09-30

These sources extend the video extraction. Recommendations below are One Roof design proposals.

| Reference | Source finding | Caretaker application |
|---|---|---|
| [Kung-Fu Master original Activision manual, archived transcription](https://atariage.com/manual_html_page.php?SoftwareLabelID=268) | Five floors, two-sided enemy pressure, duck/jump responses to projectiles, floor bosses, health and timer reset on advancement | Simple attack vocabulary, recognizable threat types and a clear floor-clear milestone. This manual is for Atari 2600, not the supplied C64 version; do not assume identical controls or tuning. |
| [Sifu: developer interview with Sloclap](https://www.unrealengine.com/developer-interviews/old-boy-john-wick-sifu-the-design-of-a-pak-mei-master?lang=en) | An attack-ticket system distinguishes main and secondary opponents; pushes, knockdowns and defensive options help manage groups. Oldboy and Jackie Chan films inform the combat/environment approach | Limit simultaneous committed attacks; let knockback create space in a narrow corridor. Use actual props and doorways as spatial context. |
| [Streets of Rage 4: developer interview](https://www.nintendolife.com/news/2020/12/feature_the_making_of_streets_of_rage_4_by_the_people_who_made_it_happen) | Developers emphasize positioning and patterns, bounded enemy counts, stage retries, responsive animation and frame-step tools | Pace distinct small encounters with pauses at stairs; prototype a floor checkpoint and inspect hitboxes/attack phases in slow motion. The cited five-enemy limit is that game's tuning, not a prescribed One Roof limit. |
| [Street Fighter 6: Capcom practice manual](https://game.capcom.com/manual/SF6/en/ps5/page/6/3) | Tutorials, character guides, combo trials and configurable practice serve different learning needs | Lobby practice can teach movement and individual attacks before asking for combinations. This is an optional proposal, not an approved expansion. |

Recommended synthesis: Kung-Fu Master's floor progression, Sifu's readable group pressure and use of space, Streets of Rage 4's encounter pacing and response, and Street Fighter's focused practice. Preserve the actual constructed Tower as the traversal space.

Suggested first three encounter beats: teach facing and strike range on the first affected floor; introduce a telegraphed ranged threat and duck/jump responses on the next; then combine threats with a stronger opponent. Assign these beats to eligible connected floors rather than hard-coded floor numbers. Keep opponent identity and crisis cause as explicit unresolved narrative design.

Research scope: original-manual transcription, direct developer interviews and official manual search excerpt. Capcom's full practice page failed to open through the web fetcher; only the returned official search excerpt was available. No exact C64 damage values or Street Fighter frame data were verified. No Unity validation was run for these documentation changes.

## Implemented slice and commit boundary — 2026-10-01

The complete first playable crisis is already included in commit `14bc70d` (alongside concurrent furniture work). It provides permanent lobby contact, safe rehearsal, sustained faction warnings, actual slab/stair traversal, successive bounded fighter waves, saved whole-household eviction consequences, and management input/camera restoration. Press **C** to open contact; combat controls appear in the HUD.

Validation of that implementation, before subsequent body/ragdoll changes:

- Crisis EditMode: 20/20 passing; resident presentation EditMode: 37/37 passing; full graphical PlayMode: 16/16 passing.
- Real 1920×1080 contact and combat HUD captures inspected; an initial targeted HUD run passed 1/1.
- Staged fight at explicit 1280×720: 94.5 FPS / 181.6 draws. Native 1920×1080 Editor GameView plus HUD: 36.1 FPS / 126 draws on GT 755M. The full 60 FPS presentation gate remains open.
- A later HUD capture run reached gameplay assertions but failed on an asynchronous Unity AI package `NoSubscription` log; this is separate from crisis assertions.

Evidence remains in `/tmp/one-roof-caretaker-validation/`; frozen validation source and manifests were retained, while isolated generated caches were removed. Internal room wall, door and furnishing collisions, accessibility/remapping polish, and the remaining six-chain beta contract are outside this delivered slice.

The subsequent resident-body and ragdoll edits remain separate work. This closeout records the validated crisis boundary without including those uncommitted changes.
