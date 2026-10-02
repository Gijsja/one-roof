# Asset Pipeline & Content Catalog — One Roof (v0.10+)

This document is the authoritative specification for One Roof's asset compilation pipeline, visual styling, character composition, and environment prop catalog.

---

## 1. Asset Pipeline & Governance

### Core Invariant
Source art is not game content until the asset compiler validates and registers it. AI-generated and artist-authored files use the identical pipeline gate:

```text
Request → Asset Spec → Source Art → Normalize → Validate → Preview → Approve → Registry → Addressables
```

### Validation Gates
- **Naming & Immutable ID**: Unique namespaced `ContentId` assigned by tooling; never rename released content IDs.
- **Dimensional Integrity**: Exact pixel bounds, transparent backgrounds, and anchor point alignment.
- **Perspective & Metric Profile**: 2.5D orthographic profile with 0.5m grid metric.
- **Rig Compatibility**: Compatibility with the shared 17-bone attachment hierarchy, pivots and slot order. Artwork cannot select or replace animation implicitly.

---

## 2. Visual Language & Dimensions

- **Projection**: Locked 2.5D architectural cutaway with orthographic camera.
- **Grid Metric**: Discrete 2D grid with 0.5m cell pitch and 3.0m floor height (`FloorY(f) = BaseFloorY + f * 3.0f`).
- **Standard Dimensions**:
  - Resident figures: `0.58m` height, bottom-center pivot `(0.5, 0.0)`.
  - Room backdrops: `1.42m` height, 9-sliceable across 1 to 8 cells.
  - Door portals: `0.80m` height.
- **Palette & Lighting**:
  - Structural shell: Midnight-navy monolithic core (`#0a0f18`) with slate edges.
  - Occupied rooms: Honey oak flooring, terracotta backdrops, and amber domestic lighting (`#ebc068`).
  - Active utilities: Electric gold power conduits (`#ffd700`) and cyan water conduits (`#40d9ff`).
  - Congestion / alerts: Energetic orange (`#ff9f40`) and warning crimson (`#ff4040`).

---

## 3. Character Assembly & Inspectable Animation Contract

### Shared 17-Bone Procedural Rig
Current resident locomotion is evaluated by `NpcSkeletalHierarchy.ApplyProceduralAnimation(sampleTime)` using explicit bone transforms. All 9 standard states are fully animated procedurally following Alan Becker stick figure principles (overlapping multi-joint drag, dynamic line of action, elastic whip, squash & stretch, and living breathing/weight shifts):
- **Locomotion**: `Walk` (energetic stride with forearm whip, wrist drag, foot roll, and passing knee clearance), `Run` (high-cadence athletic sprint with 90° arm pumps and glute heel kick), `Crawl` (low-profile duct/tunnel horizontal crawl with alternating 4-point gait).
- **Settled & Transit**: `Idle` (respiratory ease-in with ready stance and strict $t=0$ bind restoration), `QueueWait` (impatient hip-cocked weight shifts with restless foot taps), `Ride` (shock-absorbing elevator stance with micro-squash and balance hands).
- **Rest & Industry**: `Sit` (relaxed seated fold with gentle chest breathing), `Sleep` (restful fetal curl with deep-sleep breathing cadence), `WorkDesk` (attentive forward lean with desk-height forearms and burst typing flutter).

The central presenter selects the pose from transit state and room activity. `AnimationSelectionReason`, `LastAnimationSampleTime` and `AnimationPhaseOffset` expose the selection and evaluator inputs. Root facing/stature and bone-local transforms remain inspectable. No resident owns an Update loop.

Open **One Roof → Residents → Animation Preview** to choose a palette/proportion variant and pose, scrub explicit time, flip facing and inspect evaluated bone positions/angles. The window creates an isolated preview object; it does not alter the Tower scene. Selecting a scene resident shows its current pose, selection reason and last sample time. The source button opens the evaluator. Replay at the same pose/time must reproduce transforms after other poses.

### Predefined Resident Artwork Archive (2026-09-30)
The old full-body sheet and wardrobe cutouts are archived outside Unity import in [Art/Archive/ResidentRepresentations/2026-09-30](../Art/Archive/ResidentRepresentations/2026-09-30/README.md). The archive preserves 50 PNGs and original metadata, checksums and registry snapshots. No archived image GUID remains referenced by scenes/prefabs/assets.

Current resident content records have no Resources body path; wardrobe variants retain palettes/proportions but no cutout bindings. `WardrobePartCatalog` does not load imported art, and AssetLab does not automatically register the Residents folder. Dropping predefined PNGs back into Resources must not activate them. `ResidentSpriteCatalog` now generates a neutral silhouette for picking and distant LOD; visible close residents remain articulated geometry. This proxy is derived at runtime and contains no baked outfit or imported resident representation. Emotion icons and environment artwork remain active.

Returning resident art is an explicit feature under OR-1102: validate modular anchors, body/garment coverage, supported poses and LOD before binding assets. SourceArt/Proposed sheets/specs are reference candidates, not approved runtime inputs.

### 8-Layer Wardrobe Composition
Layer sorting order (back to front):
1. Body base
2. Face / Head
3. Hair
4. Lower clothing (pants, skirts)
5. Upper clothing (shirts, jackets)
6. Footwear
7. Accessories (glasses, hats)
8. Carried prop (tools, briefcases)

Each component declares content ID, compatible rig, body types, sorting layer, and palette masks.

---

## 4. Architectural Dressing & Backdrops

### 4.1 9-Sliceable Room Backdrops (`Resources/Rooms/`)
Backdrops auto-expand horizontally across 1 to 8 cells without texture distortion (`RoomBackdropPresenter.cs`, `FloorThemeCatalog.cs`):
- **Residential**: Studio Apartments, Family Living Units.
- **Commercial & Services**: Corporate Offices, Diners/Cafes, Retail Shops, Medical Clinics, Maintenance Workshops, Security Stations.
- **Circulation & Civic**: Ground Reception Lobby, Skylobbies, 2-cell vertical Stairwells.
- **Subterranean**: 14 Secret Undercity backdrops (Excavation cavity, Command Center, Hydroponics, Laboratories, etc.).

The underground cutaway uses `BuildingExteriorPresenter`: granular deterministic soil/rock, uneven sediment horizons, angular inclusions/fractures and chipped excavation rims, with shared boundaries omitted between excavated cells. Earth uses a shared vertex lattice and stays one batch. Underground presentation cells match Tower columns (0.5 render units) and storeys (1.75 render units), with 32 columns spanning 16 units. The normalized undercity subtree keeps surface dressing unchanged; inverse scale preserves Resident bodies and text. Placement, utility routes and investigator positions use the same rectangular mapping. Corridors have open walkways and recessed gradient lighting. Room walls reuse a cropped approved industrial backdrop that excludes its baked utility stripe; the Generator has engine/alternator/control/exhaust detail, and the Access Hub has lockers and seating. Entrances/hatches follow actual adjacent corridor/shaft cells. Compact signs separate connection from staffing/operating state using text. Placement previews own their tinted mesh so excavation interiors and Tower geometry retain their own colors.

Geometry refreshes on topology revisions; generated meshes and the cached artwork crop are released on retirement/teardown. Fixtures are decorative; utility projections remain authoritative. Build previews show the complete shaft footprint and metric depth, with dig → floor → connect → zone prerequisites. See [art/UX validation](Validation/UNDERCITY_ART_2026-10-02.md) for actual GameView captures and [initial construction validation](Validation/UNDERCITY_2026-10-02.md) for the earlier implementation evidence.

### 4.2 Structural Fixtures (`Resources/Architecture/`)
- **Doors**: Apartment wooden doors, elevator landing sliding doors, stairwell fire doors.
- **Windows**: Mullioned exterior windows with day/night emissive lighting and volumetric light cones.
- **Elevators**: Steel car cabin, counterweights, guide rails, and hoist cables inside discrete shaft columns.

---

## 5. Environment Prop Catalog (`Resources/Props/`)

19 standardized props managed via `PropContentRegistry`, `PropCatalog`, and `RoomFurnishingPresenter`:

| Prop Family | Included Items | Interaction Anchors | Primary Room Theme |
|---|---|---|---|
| **Domestic Living** | 2-seat sofa, double bed, bookcase, kitchenette, coffee table, potted monstera, vanity dresser | `seat-left/right`, `sleep`, `cook`, `browse`, `groom-left/right` | Residential Apartments |
| **Commercial Diner** | Diner booth, service counter, espresso bar, stool, wall menu board, recreation game table | `seat-booth`, `serve-counter`, `eat-stool`, `seat-left/right` | Restaurants & Diners |
| **Corporate Office** | Ergonomic desk with dual monitors, rolling office chair, filing cabinet, whiteboard | `desk-work`, `seat`, `file-browse` | Offices & Workspaces |
| **Civic & Lobby** | Reception curved desk, directory kiosk, coat rack, bench, lounge conversation set | `visitor-greet`, `staff-back`, `rest`, `seat-left/right` | Ground Lobby, Skylobbies & Lounges |
