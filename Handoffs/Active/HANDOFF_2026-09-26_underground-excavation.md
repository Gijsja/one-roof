# DIG-001 — Square underground excavation

## Implemented

- Extended the earth cutaway below the building with varied soil colors, three strata, and offset deposits.
- Added domain-owned excavated earth cells in an independent 16×6 board rendered as a visible 1 m square grid beneath the building.
- Added validated 1×1, 2×2, and 3×3 square dig and lair-floor brushes covering 1 m, 2 m, and 3 m. Build mode previews the selected brush; click-drag carves earth and lays floor only into excavated cells, without requiring a tower floor slab.
- Excavated cells render as dark open-space tiles and persist in the root save payload. Older saves with no underground cell array remain valid.
- Added architecture, data-contract, UX, backlog, and decision-log documentation.

## Validation

- `git diff --check` passed.
- Connected Unity 6000.3.24f1 recompile completed successfully with no compile errors. Full EditMode suite passed: 673/673, 0 failed, 0 skipped (11.03 s). `git diff --check` passed.

## Risks and next safe action

- Inspect `Tower_GroundStart` in Unity and verify brush hover, 1×1/2×2/3×3 dig and floor actions, bounds, drag paths, overlap behavior, and save/load visually. The user reported that the earth was not buildable under tower slab rules; underground now owns a fixed independent grid and separate floor-building command.
- This slice builds lair floor tiles but does not yet define rooms, equipment, or lair systems.
