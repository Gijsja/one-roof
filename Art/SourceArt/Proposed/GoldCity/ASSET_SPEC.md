# Gold City environment — authored mesh asset specification

Purpose: decorative city-scale playground around `Tower_GoldStandard30`; preserve the interactive cutaway and Outside lobby seam. These are view assets, not placeable rooms or simulated people.

- Source/provenance: first-party procedural geometry authored in `GoldStandardLevelBuilder`; no external downloads, AI raster images, or vendor modifications.
- Perspective: orthographic front elevation, world units; ground anchor at `TowerStructurePresenter.BaseFloorY`. Mesh coordinates are local to that anchor.
- Theme: weathered teal, blue atmospheric distance, amber brass/signs, warm/cool patchwork windows. Optimistic inhabited city with readable tower interiors.
- Footprints: background city spans ±110 m; street excludes the tower footprint (−13 to +7 m). No collision, navigation, interaction points, or save state. Sky covers the maximum overview and pan envelope.
- Assets: atmospheric sky, three skyline meshes, street/promenade, electric taxi, city tram, wind clouds, swallow; two URP materials and a reusable environment prefab.
- Identity: Unity AssetDatabase assigns `.meta` GUIDs on import. Rebuild updates meshes/materials in place to preserve identity; prefab path stays stable.
- Animation: fixed 12 traffic vehicles, 7 clouds, 7 birds. Wrap at the edges of the decorative district, never cross the tower interior. Pause follows the simulation pause control. Per-renderer property blocks carry projected day/night and weather to a shared material.
- Normalization/validation: meshes contain vertex colors and no textures; bounds recalculated by Unity; shadows/colliders disabled; GPU-compatible UInt32 indices; referenced directly by the environment prefab (no string-based runtime registry lookup).
- Preview: generated through the isolated Unity builder; graphics PlayMode captures include full tower and close street view. Review evidence and final status live in the task handoff.
- Integration: no new content IDs or gameplay furniture definitions; existing registered room furnishings remain responsible for the tower interior.
