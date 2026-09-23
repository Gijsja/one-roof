# Oxygen Not Included-Style Utility Network Layer

**Status:** Implemented  
**Date:** 2026-09-23

## Scope

- Overhauled `UtilitiesNetworkLayerPresenter.cs` to render an *Oxygen Not Included*-style connected schematic network layer using `AllIn1SpriteShader` (`GLOW_ON`, `TEXTURESCROLL_ON`) with graceful URP Unlit fallback.
- Added full network graph geometry:
  - **Ground Terminals**: Substation source, Water Pump source, and Waste Collector sink terminals on Floor 0.
  - **Vertical Risers**: Backbone trunk lines running vertically through riser columns (`utility:electrical_riser`, `utility:water_riser`, `utility:waste_chute`).
  - **Floor Transformers & Boosters**: Step-down nodes and booster pump junctions.
  - **Horizontal Raceways**: Ceiling raceways for Power (`FloorY + 1.42f`) and subfloor raceways for Water (`FloorY + 0.22f`) and Waste (`FloorY + 0.12f`) to prevent line overlap.
  - **Room Drops & Terminal Ports**: Per user selection (Option A), drops connect horizontal raceways to each consumer room (residential, office, diner, clinic) with circular terminal port markers.
- Added procedural dashed flow pulse texture and port node texture with forward/reverse animated flow scrolling (`TEXTURESCROLL_ON`).
- Added active vs. disrupted status visualization (bright glowing flow when healthy; warning amber/red/dimmed when brownout or low pressure).
- Implemented object pooling for `LineRenderer` segments and node Quads to guarantee zero per-frame garbage generation.
- Wired `TowerPlayableController.cs` to pass `TowerTopologyProjection` to the network layer on mode change, simulation ticks, and presenter geometry syncs.
- Created `UtilitiesNetworkLayerPresenterTests.cs` covering defaults, visibility, network switching, graph building, fallbacks, and object pooling.

## Validation

Validation used isolated sibling worktree `../one-roof-utilities-validation` at `HEAD` with the scoped diff. Unity executable: `/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity`.

| Check | Result |
| --- | --- |
| One-shot `-batchmode -nographics -quit` compile | Exit 0; `Tundra build success (21.39 seconds)`, 1751 evaluated items; 0 C# errors. |
| `-runTests -testPlatform EditMode -testFilter UtilitiesNetworkLayerPresenterTests` | Exit 0; 8/8 passed in `/tmp/one-roof-utilities-editmode.xml`. |
| `-runTests -testPlatform EditMode -testFilter OneRoof.Presentation.Tests.EditMode` | Exit 2; 173/175 passed in `/tmp/one-roof-presentation-editmode.xml`. The 2 failures (`EnsureRoomViews_AdoptsAuthoredRoomInsteadOfCreatingDuplicate`, `Initialize_RemovesDuplicateSlabAndPreservesCanonicalGeometry`) are identical to baseline pristine-HEAD failures recorded in `Planning/BACKLOG.md`. |
| Scoped `git diff --check` | Passed with exit code 0. |

## Review point

Open the `Tower` or `Tower_GroundStart` scene in the Unity Editor / Play Mode. In `Data` mode, select the `Utilities` overlay, then click between `Power`, `Water`, and `Waste` in the utilities panel to observe the glowing conduit networks, directional flowing pulses, and room terminal ports.
