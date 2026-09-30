# Archived resident representations — 2026-09-30

Predefined full-body resident atlas and wardrobe cutouts were removed from Unity import and runtime loading at the Steward’s request. This folder is outside `Assets`; files are reference material, not deployable content. All original `.meta` files and folder metadata are preserved. `manifest.json` records original paths and SHA-256 checksums. Registry snapshots retain the old bindings for reference.

Do not copy these assets back into `Resources` or register them through AssetLab. Returning art requires an explicit approved modular attachment contract and runtime implementation; dropping PNGs into a folder must never activate them. Current rendering uses articulated procedural parts and a generated neutral LOD/picking silhouette. Emotion icons, room art and props remain active.

Restore only as an intentional feature: use manifest paths, preserve paired metadata, validate compatibility and pose coverage, update content bindings and tests, and inspect all supported LOD tiers.
