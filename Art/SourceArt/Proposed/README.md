# Proposed source art — One Roof

These images are **art-direction references**, not registered runtime content. They live outside `Assets/` deliberately: no generated image may enter the game until it passes the asset pipeline in `Docs/05_ASSET_PIPELINE.md`.

## Contents

| File | Intended use | Status |
| --- | --- | --- |
| `one-roof-cutaway-concept-v1.png` | Overall 2.5D cutaway, room readability, lighting, and vertical-circulation reference | Proposed |
| `one-roof-resident-lineup-concept-v1.png` | NPC silhouette, wardrobe, and palette reference | Proposed — not a rig-ready sprite sheet |
| `one-roof-environment-props-concept-v1.png` | Furnishing, doors, elevator, and lobby prop-language reference | Proposed — not an import-ready atlas |
| `room-props-spritesheet-v1.png` | 16-cell, transparent room-prop source sheet; 4×4 at 256 px per cell | Proposed — individually validate/crop before registry submission |
| `room-props-spritesheet-v1.spec.json` | Per-cell proposed footprint, anchor, collision, interaction, and room-theme metadata | Proposed — final IDs belong to the content tool |
| `room-interior-backdrops-v1.png` | Four opaque room wall/floor backdrops; 2×2 at 512 px per cell | Proposed — validate horizontal slicing before registry submission |
| `room-interior-backdrops-v1.spec.json` | Per-cell room mapping plus recommended Unity importer and 9-slice settings | Proposed — final IDs belong to the content tool |
| `resident-spritesheet-v1.png` | Normalized 6-cell transparent resident source sheet; 6×1 at 256×512 px per cell | Proposed — normalized from resident lineup reference |
| `resident-spritesheet-v1.spec.json` | Per-cell archetype, 8-layer contract, and Spine rig specification | Proposed — validated candidate |
| `resident-spine-setup-v1.json` | Shared 2D humanoid skeletal rig in Spine 4.2 format (bones, slots, skins, tracks) | Proposed — standard rig format |
| `resident-emotes-v1.png` | 16×16 pixel emote balloons and reaction icons; 12×13 at 192×208 px | Proposed — 3-frame animation loops with framed/unframed rows |
| `resident-emotes-v1.spec.json` | Per-emote category, trigger condition, frame triplet, and anchoring specification | Proposed — validated candidate |

## Visual direction

- **Structure:** midnight/navy architectural shell with cool slate-blue surfaces and crisp pale-blue edges.
- **Life and wayfinding:** warm honey oak and amber lamps distinguish inhabited rooms; cyan signals normal system activity; orange marks congestion or risk.
- **Readability:** maintain a locked orthographic cutaway, consistent floor height, and a continuous central elevator shaft. Room type must remain recognizable without labels or UI color alone.
- **People:** small, modular, silhouette-led figures whose clothes carry role and life-stage variety without visual noise.

## Provenance

Generated with the built-in image-generation tool on 2026-09-17. Prompts were authored for this project; no reference images were used. Source hashes:

```text
one-roof-cutaway-concept-v1.png           ef8060b11572776c1957a9dea098b42313561f56e9ac12e5740a24335d1f4a7d
one-roof-environment-props-concept-v1.png 991bf33d95af566008f48f9fb4b23ca1b2dca5351148245ea5d15af06e262a63
one-roof-resident-lineup-concept-v1.png   29823cfb263e3be042c2ffe9c9b4809a1982984daab344a1c99d757b81493c9e
room-props-spritesheet-v1.png             017c2fd543619dc4698c2b0b4a5d44cc6c9656bb7ecd2de138c2265196d2d512
room-interior-backdrops-v1.png             bd71c1dbbe4ad051a92e2081c20a3a514ff411c4a9c7460406c2f8a7e757477e
resident-spritesheet-v1.png               f62a15d1ac43cd46734fd1e3b2e210a55428c68d042a944cace7d0ebf21b32ae
resident-emotes-v1.png                    54886858c55db1d9f7bee8601ec67194af1960f2bf77a9a4de8a705ea4ab627a
```

## Before runtime use

1. Create individual source assets with declared dimensions, transparency, anchors, perspective profile, and content intent.
2. For NPC layers, split by the prescribed body → face → hair → lower clothing → upper clothing → footwear → accessory → prop order and validate against the shared rig.
3. Run the content validator, preview next to representative rooms/animations, assign immutable content IDs, and only then register/atlas/address the approved assets.
