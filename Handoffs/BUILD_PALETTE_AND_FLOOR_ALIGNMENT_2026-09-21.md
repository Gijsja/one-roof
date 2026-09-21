# Build palette and floor alignment repair

**Status:** DONE  
**Updated:** 2026-09-21

## Outcome

- Made Build a two-step flow: open the complete six-row palette, select a tool, then place with an unobstructed tower viewport. Right-click or Build reopens the palette. This prevents selection clicks from reaching the grid without blocking expansion clicks behind a persistent palette.
- Unified pooled-NPC floor projection coordinates with the canonical `TowerStructurePresenter` coordinates used by slabs, rooms, and elevators.
- Reinitialised scene-authored resident rigs during adoption so obsolete composite placeholder sprites are disabled and only the current modular wardrobe presentation remains visible.
- Tuned Scrutiny accumulation so ordinary first-playable simulation no longer hard-locks every construction command after a few seconds; sustained harmful expansion can still create pressure.

## Validation

| Check | Result | Evidence |
| --- | --- | --- |
| Isolated Unity compile | PASS | `/tmp/one-roof-build-placement-repair-compile.log`, exit code 0; no C# compiler errors |
| Static diff check | PASS | `git diff --check` |
| EditMode batch invocation | Limitation | `/tmp/one-roof-build-palette-editmode.log` exited 0 but Unity 6000.3.24f1 honored `-quit` before creating `/tmp/one-roof-build-palette-editmode.xml`, matching the documented project limitation |

## Added regression coverage

- Full palette hit region while selecting, plus a released grid viewport after selection.
- NPC floor position equals the shared slab/elevator floor coordinate at multiple heights.
- Existing scene residents disable their legacy composite sprite when adopted.

## Next safe action

Run a short desktop playtest in `Tower`: select each Build palette section, expand with a floor slab, construct a room plus electrical and water infrastructure, place stairs, and confirm no pointer clicks leak through the palette.
