# Asset and NPC Assembly Pipeline

## Rule

Source art is not game content until the asset compiler validates and registers it. AI-generated and artist-authored files use the same gate.

## Flow

```text
Request → Asset spec → Source art → Normalize → Validate → Preview → Approve → Registry → Atlas/Addressables
```

## NPC layer contract

Order: body, face, hair, lower clothing, upper clothing, footwear, accessory, carried prop.

Each component declares content ID, compatible rig, compatible body types, sorting layer, anchor set, palette behavior, source provenance, version, and approval state.

Use a shared 2D skeletal rig for locomotion and common interactions. Bespoke sprite sequences are exceptions and must declare which actions they replace.

## Environment contract

Furniture declares footprint, anchor, height class, collision mask, interaction points, perspective profile, theme, and variants. Visual variants must preserve gameplay semantics.

## Validation gates

- Naming and immutable ID.
- Dimensions, transparency, and pixels within bounds.
- Perspective and scale profile.
- Anchor and interaction-point validity.
- Rig/body compatibility for NPC layers.
- Sorting and palette masks.
- License/source provenance.
- Preview across representative animations or neighboring tiles.

## Registry rule

Agents may propose human-readable names, but tooling assigns final IDs. Never rename a released content ID; deprecate it and migrate references.

