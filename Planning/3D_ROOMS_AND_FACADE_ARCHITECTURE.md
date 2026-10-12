# 3D Rooms & Hybrid Facade Architecture — Execution Plan & Sub-Agent Handoff (OR-1200)

**Date**: 2026-10-12  
**Status**: Planned / Ready for Sub-Agent Dispatch  
**Ownership**: Presentation & Graphics Layer (`OneRoof.Presentation`, `OneRoof.Content`)  
**Domain Boundary**: Strictly 0 `UnityEngine` dependencies in `OneRoof.Domain` and `OneRoof.Application`.  
**Performance Budget**: Hard limit of <180 draw calls, <100 batches, ≥60 FPS (16.6 ms frame budget), and 0 steady-state heap allocations per tick at reference scale (30 floors / 300 residents).

---

## 1. System Vision & Core Thesis

Currently in **One Roof**, rooms, backdrops, fixtures, and residents are rendered as flat 2D cutaway cross-sections. While elements possess nominal local $Z$-offsets (`Backdrop` at $Z=0.7$, `Props` at $Z=0.38$, `Residents` at $Z=0.0$), the camera is strictly **orthographic** (`orthographic = true`) and looks down the $(0, 0, 1)$ axis. Under this projection, differing $Z$-depths affect only draw order; they produce **zero perspective, foreshortening, or parallax**.

By transitioning rooms into **3D stage boxes ("diorama shoeboxes")** and distributing 2D sprites across discrete depth planes on the 3D floor and walls, we achieve a tactile **dollhouse / paper-theater aesthetic** (reminiscent of *Fallout Shelter* and architectural scale cutaways):
1. **Dynamic Multi-Plane Parallax**: Panning and zooming across the Tower naturally reveals room ceilings above the horizon, floorboards below the horizon, and party walls to the sides.
2. **True Spatial Depth for Residents**: Autonomous residents no longer slide along a flat 1D line; they step *into* rooms toward desks, beds, and dining tables, and step forward into the corridor lane.
3. **Contact Shadows & Baked Corner AO**: Elliptical contact shadow decals on the floorboards and baked vertex AO at wall junctions physically ground flat 2D sprites in 3D space.
4. **Hybrid Facade Strategy (Real Fake Interiors Synthesis)**: At macro zoom (overview), exterior window panes leverage baked parallax raymarching (the *Real Fake Interiors* pattern) to render hundreds of 3D interior windows at 0 extra polygons; at micro zoom (inspection cutaway), procedural 3D diorama stage boxes host living, interactive residents and furnishings.

---

## 2. Spatial Geometry & The 3D Stage Box Model

Rather than a single flat 9-sliced quad backdrop, each cutaway room is structured as an open-front **5-sided 3D Stage Box**:

```text
               Ceiling Plane (Y = Soffit, Z: 0 → D)
                         ┌─────────────────┐
    Left Party Wall      │  REAR BACKWALL  │     Right Party Wall
    (X = Left, Z: 0 → D) │   (Z = Depth)   │     (X = Right, Z: 0 → D)
                         └─────────────────┘
                Floor Plane (Y = Baseline, Z: 0 → D)
                         ▲
                         │
                 Front Cutaway (Z = 0.0)
```

### 2.1 Dimensional Metrics
- **Height ($H$)**: $1.42\text{m}$ (standard room clearance).
- **Width ($W$)**: $W = \text{CellCount} \times 0.5\text{m}$ (spanning $0.5\text{m}$ to $8.0\text{m}$).
- **Depth ($D$)**: $0.90\text{m} - 1.20\text{m}$ from front cutaway plane ($Z=0.0$) to rear back wall ($Z=D$).
- **Floor Baseline ($Y_0$)**: $-0.74\text{m}$ relative to room origin.
- **Ceiling Soffit ($Y_1$)**: $+0.68\text{m}$ relative to room origin.

### 2.2 Vertex Ambient Occlusion (AO)
To avoid dynamic lighting overhead, the room box mesh bakes corner contact shadows directly into vertex colors (`COLOR`):
- Junctions between floor-and-wall and ceiling-and-wall receive darkened vertex color multipliers (RGB `0.55, 0.55, 0.60`).
- Open front edges remain unshaded (RGB `1.0, 1.0, 1.0`).

---

## 3. Sprite Depth Zoning & Billboarding

Sprites are arranged along the $Z$-axis into **three functional depth tiers**:

```text
Front Cutaway (Z = 0.0)
   │
   ├── Tier 0: Foreground Access Lane (Z = 0.15m ± 0.05m)
   │     • Walking residents in corridor transit, entrance doors, threshold trim.
   │
   ├── Tier 1: Midground Activity Island (Z = 0.50m ± 0.10m)
   │     • Dining booths, coffee tables, beds, couches, game tables, floor rugs.
   │
   ├── Tier 2: Background Wall Layer (Z = 0.85m ± 0.05m)
   │     • Desks, kitchenettes, bookcases, storage lockers, vanity dressers.
   │     • Flush mounts (Z = D - 0.01m): wall sconces, posters, rear windows.
   │
Rear Back Wall (Z = 1.0m)
```

### 3.1 Sprite Transform Profiles
1. **Upright Billboards (Props, Furnishings, Residents)**:
   - Upright orientation (`rotation = Quaternion.identity`, facing $-Z$).
   - Pivot anchored at bottom-center $(0.5, 0.0)$ on the floor plane ($Y = Y_0$).
   - Telephoto perspective naturally scales distant props down by $\frac{1}{Z}$.
2. **Floor-Planar Decals (Rugs, Floor Mats, Spills, Cables)**:
   - Rotated $90^\circ$ on $X$ (`Euler(90, 0, 0)`), resting at $Y = Y_0 + 0.005\text{m}$.
   - Recedes into depth along the 3D floor plane.
3. **Contact Shadow Decals**:
   - Soft radial alpha ellipse quad positioned at $(X, Y_0 + 0.002\text{m}, Z)$ under every upright prop and resident.
   - Tinted `Color(0.02, 0.04, 0.08, 0.45)` to eliminate the "floating sprite" effect.

---

## 4. Camera & Perspective Projection

- **Frustum Model**: Telephoto-perspective camera with narrow vertical Field of View ($24^\circ - 30^\circ$, default $28^\circ$).
- **Distance Matching**: The camera distance $Z_{\text{cam}}$ is tied to the current orthographic framing:
  $$Z_{\text{cam}} = -\frac{\text{OrthoSize}}{\tan(\text{FOV} / 2)}$$
- **Semantic Zoom Pacing**:
  - **Inspection Zoom ($\text{OrthoSize} < 6.5$)**: Full $28^\circ$ perspective with maximum parallax, 3D stage boxes, and contact shadows.
  - **Mid-Tower ($\text{OrthoSize} \in [6.5, 12.0]$)**: Mild $18^\circ$ perspective, simplified boxes.
  - **Overview ($\text{OrthoSize} > 12.0$)**: Low-FOV quasi-orthographic camera crossfading into the ADR-088 exterior envelope.

---

## 5. Dual-Tier Hybrid Pipeline: Real Fake Interiors Synthesis

An evaluation of **Real Fake Interiors** (Amplify Creations, package 374398) establishes the boundary between shader impostors and dynamic geometry:
- **Macro Exterior Facade (ADR-088 Zoom-Out)**: Use baked parallax raymarching (Real Fake Interiors technique) on exterior curtain wall window quads. Renders hundreds of furnished 3D interior windows across 30 floors at **0 extra polygons and 0 extra draw calls** via the SRP Batcher.
- **Micro Interior Cutaway (Zoom-In)**: Use our procedural 3D Stage Box architecture. Autonomous residents, Spine rigs, and interactive slots require physical 3D space and cannot exist inside static raymarched textures.

---

## 6. Work Package Breakdown (OR-1200 Series)

| Package | Title | Primary Files | Dependencies | Reviewable Outcome |
|---|---|---|---|---|
| **OR-1201** | 3D Stage Box Geometry & Corner AO | `RoomBoxPresenter.cs`, `FloorRoomBoxBatcher.cs`, `RoomPresenter.cs` | Existing room cutaways | Procedural 5-sided room mesh replacing flat backdrops; baked corner AO vertex colors; floor-level mesh batching. |
| **OR-1202** | 3-Tier Sprite Depth & Contact Shadows | `RoomFurnishingPresenter.cs`, `InteractionPoint.cs`, `OneRoofUnlit.shader` | OR-1201 | Props spawned at explicit depth tiers ($Z=0.15, 0.50, 0.85$); soft contact shadow decals; $Z$-buffer cutout sorting. |
| **OR-1203** | Telephoto Perspective Camera & Parallax | `TowerCameraController.cs`, `TowerPlayableController.cs` | OR-1201 | Perspective camera with matching $28^\circ$ FOV; pan/zoom parallax across 30 floors; zero jitter. |
| **OR-1204** | Resident 3D Locomotion & Depth Docking | `TowerResidentPresenter.cs`, `NpcView.cs` | OR-1202, OR-1203 | Residents walk along corridor Tier 0 ($Z=0.15$) and interpolate depth ($Z \to 0.50/0.85$) to dock at beds/desks. |
| **OR-1205** | Hybrid Facade Interior Mapping Spike | `BuildingExteriorPresenter.cs`, `GoldCity.shader` | OR-1203, ADR-088 | Parallax raymarched interior windows for exterior envelope; validates 83 draw calls overview budget. |

---

## 7. Sub-Agent Prompts & Handoff Specifications

Sub-agents dispatched to implement this roadmap must be provided with the shared instructions and their respective package prompt.

### 7.1 Shared Prompt (Include with Every Assignment)

```text
You are working on One Roof at /home/geisha/Vibecode/UnityAI/one-roof.
Read AGENTS.md, Planning/BACKLOG.md, Planning/ROADMAP.md, and Planning/3D_ROOMS_AND_FACADE_ARCHITECTURE.md.
Follow the execution constraints:
1. Pure C# Domain Purity: OneRoof.Domain and OneRoof.Application must maintain 0 UnityEngine references.
2. Clean Layering: Domain -> Application (projections) -> Presentation (MonoBehaviours & meshes).
3. Performance Guarantees: 0 steady-state heap allocations per tick; 30-floor / 300-resident reference scale must sustain <180 draw calls and >=60 FPS.
4. Testing: Tier 1 (.NET CLI fast build: dotnet build OneRoof.Presentation.csproj) for routine iteration.
State your prerequisites, owned files, and deliverable before making changes.
```

### 7.2 Sub-Agent Prompt: OR-1201 (3D Stage Box Geometry & Floor Batching)

```text
Implement package OR-1201: 3D Stage Box Geometry & Floor Batching.
Prerequisites: Clean baseline compilation of OneRoof.Presentation.csproj.
Owned files:
  - Assets/OneRoof/Runtime/Presentation/Architecture/RoomBoxPresenter.cs (new)
  - Assets/OneRoof/Runtime/Presentation/Tower/FloorRoomBoxBatcher.cs (new)
  - Assets/OneRoof/Runtime/Presentation/Tower/RoomPresenter.cs (integration)
  - Assets/OneRoof/Tests/EditMode/Presentation/RoomBoxPresenterTests.cs (new)

Instructions:
1. Create RoomBoxPresenter to generate a procedural 5-sided stage box mesh (Floor at Y0, Ceiling at Y1, Backwall at Z=Depth, Left Wall, Right Wall) with open front.
2. Bake ambient occlusion into vertex colors (darker at wall-floor and wall-ceiling junctions).
3. Implement FloorRoomBoxBatcher to combine all room boxes on a floor into a single dynamic procedural mesh sharing a single architectural material, ensuring SRP Batcher compatibility.
4. Integrate with RoomPresenter.cs to instantiate or update the 3D room boxes without GC allocations during room zoning.
5. Write EditMode unit tests verifying mesh bounds, vertex counts, and batching efficiency.
Verify compilation with: dotnet build OneRoof.Presentation.csproj.
```

### 7.3 Sub-Agent Prompt: OR-1202 (3-Tier Sprite Depth & Contact Shadows)

```text
Implement package OR-1202: 3-Tier Sprite Depth & Contact Shadows.
Prerequisites: OR-1201 delivered.
Owned files:
  - Assets/OneRoof/Runtime/Domain/Topology/InteractionPointKind.cs / InteractionPoint.cs (depth tier contract)
  - Assets/OneRoof/Runtime/Presentation/Furnishings/RoomFurnishingPresenter.cs (depth positioning)
  - Assets/OneRoof/Runtime/Presentation/Shaders/OneRoofUnlit.shader (ZWrite / cutout)
  - Assets/OneRoof/Tests/EditMode/Furnishings/RoomFurnishingDepthTests.cs (new)

Instructions:
1. Extend InteractionPoint (or Presentation mapping) with RoomDepthTier (AccessLane Z=0.15, Activity Z=0.50, BackWall Z=0.85).
2. Update RoomFurnishingPresenter.cs to spawn props at discrete local Z depths matching their functional kind (desks/shelves at Z=0.85, tables/beds at Z=0.50, entrance doors at Z=0.15).
3. Spawn an elliptical contact shadow decal quad on the floorboard (Y = baseline + 0.002m) under each upright prop to ground it visually.
4. Ensure OneRoofUnlit.shader supports ZWrite On and alpha cutout so props and rooms properly test against the hardware depth buffer.
Verify compilation with: dotnet build OneRoof.Presentation.csproj.
```

### 7.4 Sub-Agent Prompt: OR-1203 (Telephoto Perspective Camera & Parallax)

```text
Implement package OR-1203: Telephoto Perspective Camera & Parallax Framing.
Prerequisites: OR-1201 delivered.
Owned files:
  - Assets/OneRoof/Runtime/Presentation/Tower/TowerCameraController.cs
  - Assets/OneRoof/Tests/EditMode/Tower/TowerCameraControllerTests.cs

Instructions:
1. Update TowerCameraController.cs to support a configurable perspective camera mode (FOV 24°-30°, default 28°).
2. Maintain equivalent framing to orthographic mode: calculate camera Z position dynamically from OrthoSize and FOV: Z = -OrthoSize / tan(FOV/2).
3. Ensure WASD panning, edge drag, and wheel zooming maintain smooth bounds clamping without perspective distortion or clipping planes issues (nearClip 0.3m, farClip 150m).
4. Verify seamless integration with semantic zoom (ADR-088) transitions.
Verify compilation with: dotnet build OneRoof.Presentation.csproj.
```

### 7.5 Sub-Agent Prompt: OR-1204 (Resident 3D Locomotion & Depth Docking)

```text
Implement package OR-1204: Resident 3D Locomotion & Depth Docking.
Prerequisites: OR-1202 and OR-1203 delivered.
Owned files:
  - Assets/OneRoof/Runtime/Presentation/Tower/TowerResidentPresenter.cs
  - Assets/OneRoof/Runtime/Presentation/Population/NpcView.cs
  - Assets/OneRoof/Tests/EditMode/Tower/TowerResidentLocomotionTests.cs

Instructions:
1. Update TowerResidentPresenter.cs to interpolate resident positions in 3D: (X, BaselineY, Z).
2. While moving along hallways and corridors, residents walk in Tier 0 (Z = 0.15m), in front of room furniture.
3. Upon arriving at a room destination, residents interpolate their Z-coordinate from Tier 0 to their target dock depth (e.g. Z = 0.50m for a bed, Z = 0.85m for a desk).
4. Under the perspective camera, verify that depth scaling and Z-buffer sorting naturally occlude residents behind foreground tables.
Verify compilation with: dotnet build OneRoof.Presentation.csproj.
```

### 7.6 Sub-Agent Prompt: OR-1205 (Hybrid Facade Interior Mapping Spike)

```text
Implement package OR-1205: Hybrid Facade Interior Mapping Spike.
Prerequisites: OR-1203 delivered.
Owned files:
  - Assets/OneRoof/Runtime/Presentation/Tower/BuildingExteriorPresenter.cs
  - Assets/OneRoof/Runtime/Presentation/Shaders/GoldCity.shader (or new InteriorMapping pass)

Instructions:
1. Evaluate Real Fake Interiors (package 374398) or author a lightweight URP raymarched interior cubemap pass for exterior window fenestration.
2. In BuildingExteriorPresenter.cs, apply the interior mapping shader to facade window quads when the building envelope is visible (OrthoSize > 8.0 or 'F' key toggle).
3. Connect window illumination and room theme to domain tenant occupancy and electrical voltage.
4. Measure draw calls and batch counts in Tower_GoldStandard30 to ensure overview mode remains at <=83 draw calls.
Verify compilation with: dotnet build OneRoof.Presentation.csproj.
```
