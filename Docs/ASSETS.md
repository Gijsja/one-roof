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
- **Spine Compatibility**: Compatibility with the shared 17-bone rig, skin attachments, and slot order.

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

## 3. Character Assembly Contract (Spine 2D)

### Shared 17-Bone Skeletal Rig
locomotion and interactions use a unified Spine 2D skeletal rig with 6 core clips (Idle, Walk, Sit, Sleep, Work, Gesture).

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
