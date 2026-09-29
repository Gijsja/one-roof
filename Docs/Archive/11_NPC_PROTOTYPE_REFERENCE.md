# NPC Prototype Reference

## Body and limb matching reference

The supplied `vector-set-men-professions` source sheets inform the modular
prototype's silhouette and assembly: a compact head, narrow torso, independent
upper limbs, hands, two trouser-leg columns, and separate shoes. Runtime body
parts remain original, simple rig-compatible geometry; the supplied composite
art is not embedded or flattened into the game.

This exercises the same individual-joint hierarchy expected by the eventual
approved wardrobe content while preserving the current six resident portraits
as catalog and fallback assets.

## Provenance and attribution

Reference supplied by the user:

- `vector-set-men-professions.zip` (`44111.eps`)
- `vector-set-men-professions(1).zip` (`17787.eps`)
- `vector-set-men-professions(2).zip` (`17789.eps`)

The accompanying free-use license identifies the creator as **rawpixel.com /
Freepik**, permits commercial modification and use with attribution, and
prohibits redistribution as a source archive. If these source elements are
imported into a released build rather than used solely as reference, add the
required visible attribution: `Designed by rawpixel.com / Freepik`.

## Prototype boundary

- The shared `rig.npc.humanoid.2d.v1` remains authoritative.
- Body parts stay presentation-only; no visual state enters the domain or saves.
- All limb sprites use point filtering and remain anchored at existing joints.
- Future authored clothing replaces the current procedural wardrobe swatches;
  it must pass the asset-pipeline compatibility and provenance gates.
