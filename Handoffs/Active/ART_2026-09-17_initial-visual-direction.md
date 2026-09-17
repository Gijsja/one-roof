# ART-2026-09-17 — Initial visual direction

**Status:** READY FOR REVIEW  
**Owner:** Codex session  
**Started:** 2026-09-17  
**Updated:** 2026-09-17

## Objective

Prepare graphics suitable for the current five-floor One Roof playable without bypassing the source-art validation and registry process.

## Acceptance criteria

- [x] Provide a coherent visual direction for tower architecture, residents, and modular interior props.
- [x] Preserve the game's controlled 2.5D cutaway and explainable elevator-circulation focus.
- [x] Keep generated images out of runtime content until formally validated and registered.

## Scope and ownership

Expected files/directories:

- `Art/SourceArt/Proposed/`
- `Handoffs/Active/ART_2026-09-17_initial-visual-direction.md`

Do not modify:

- `Assets/`
- Runtime code, content registry, scenes, or Addressables configuration

## State at handoff

Five reviewed source-art candidates now include a normalized 1024×1024 opaque interior-backdrop sheet. Its four fixed 512×512 room treatments complement the 16-cell transparent room-prop sheet, while leaving gameplay semantics unchanged. Both have matching specs. They are source material only and do not affect the playable.

## Changes made

- `Art/SourceArt/Proposed/one-roof-cutaway-concept-v1.png` — five-floor mixed-use cutaway art-direction candidate.
- `Art/SourceArt/Proposed/one-roof-resident-lineup-concept-v1.png` — modular NPC style candidate.
- `Art/SourceArt/Proposed/one-roof-environment-props-concept-v1.png` — architectural interior-prop style candidate.
- `Art/SourceArt/Proposed/room-props-spritesheet-v1.png` — normalized 4×4 transparent room-prop source sheet.
- `Art/SourceArt/Proposed/room-props-spritesheet-v1.spec.json` — fixed cell layout and proposed environment-contract data.
- `Art/SourceArt/Proposed/room-interior-backdrops-v1.png` — 2×2 room-wall/floor source sheet for residential, office, diner, and lobby rooms.
- `Art/SourceArt/Proposed/room-interior-backdrops-v1.spec.json` — fixed cell layout plus target Unity importer and slicing data.
- `Art/SourceArt/Proposed/README.md` — usage boundary, provenance, hashes, and validation path.

## Decisions

- Retain the existing midnight-navy cutaway, amber habitation, cyan system, and orange congestion language so visual systems reinforce the UX explanation chain.
- Store generated source art outside `Assets/` until it has item-level specs, validation, immutable IDs, and registry approval.

## Validation

| Check | Command or procedure | Result |
| --- | --- | --- |
| File integrity | `file Art/SourceArt/Proposed/*.png` | PASS — PNGs readable; resident lineup has RGBA, concepts have RGB |
| Sprite-sheet contract | `identify -format 'geometry=%wx%h type=%[type] alpha=%[channels]' Art/SourceArt/Proposed/room-props-spritesheet-v1.png` | PASS — 1024×1024 TrueColorAlpha (`srgba`), giving 16 256×256 cells |
| Backdrop-sheet contract | `identify -format 'geometry=%wx%h type=%[type] channels=%[channels]' Art/SourceArt/Proposed/room-interior-backdrops-v1.png` | PASS — 1024×1024 TrueColor (`srgb`), giving four opaque 512×512 cells |
| Provenance | `sha256sum Art/SourceArt/Proposed/*.png` | PASS — hashes recorded in README |
| Manual | Inspected all three at full size for perspective, palette, text/logos/watermarks, and consistency | PASS |
| Unity compilation | Not applicable; no Unity-owned assets or code changed | NOT RUN |
| Edit/Play Mode | Not applicable; no runtime behavior changed | NOT RUN |

## Known risks or failures

- The resident lineup is a design reference, not layer-separated or rigged sprite content.
- The prop board is a single composition, not a production atlas; each prop must be redrawn/extracted to its approved source format before validation.
- The sprite sheet has a valid alpha channel and exact cell geometry, but must be previewed over the Tower scene's dark background during AssetLab validation to reject any remaining edge-matte artifacts before it is approved for runtime.

## Next safe action

Choose one first-playable prop family (recommended: apartment door, elevator door, and wall light) and produce individually bounded source assets with anchors and interaction points for AssetLab validation.

## References

- Backlog: no dedicated art task exists yet
- Decisions: ADR-006
- Evidence: `Art/SourceArt/Proposed/README.md`
