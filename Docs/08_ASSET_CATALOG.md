# Asset Catalog — One Roof (v0.10)

This document provides the authoritative catalog of runtime visual, architectural, and character assets in One Roof.
For pipeline authoring rules, see [`Docs/05_ASSET_PIPELINE.md`](./05_ASSET_PIPELINE.md).
For historical asset delivery milestones, see [`Docs/Archive/08_ASSET_ROADMAP_ARCHIVE.md`](./Archive/08_ASSET_ROADMAP_ARCHIVE.md).

---

## 1. Visual Language & Dimensions

- **Projection**: Locked 2.5D architectural cutaway with orthographic camera.
- **Grid Metric**: Discrete 2D grid with 0.5m cell pitch and 3.0m floor height (`FloorY(f) = BaseFloorY + f * 3.0f`).
- **Standard Heights**:
  - Resident figures: `0.58m` height, bottom-center pivot `(0.5, 0.0)`.
  - Room backdrops: `1.42m` height, 9-sliceable across 1 to 8 cells.
  - Door portals: `0.80m` height.
- **Palette & Atmosphere**:
  - Structural shell: Midnight-navy monolithic core (`#0a0f18`) with cool slate edges.
  - Occupied rooms: Honey oak flooring, terracotta backdrops, and amber domestic lighting (`#ebc068`).
  - Active utilities: Electric gold power (`#ffd700`) and cyan water conduits (`#40d9ff`).
  - Congestion / alerts: Energetic orange (`#ff9f40`) and warning crimson (`#ff4040`).

---

## 2. Architectural Dressing & Room Backdrops

### 2.1 9-Sliceable Room Backdrops (`Resources/Rooms/`)
Managed via `RoomBackdropPresenter.cs` and `FloorThemeCatalog.cs`. Backdrops auto-expand horizontally across 1 to 8 cells without texture distortion:
- **Residential**: Studio Apartments, Family Living Units.
- **Commercial & Services**: Corporate Offices, Diners/Cafes, Retail Shops, Medical Clinics, Maintenance Workshops, Security Stations.
- **Circulation & Civic**: Reception Lobbies, Skylobbies, Stairwells (2-cell vertical span).
- **Subterranean**: 14 Secret Undercity chamber backdrops (Excavation cavity, Command Center, Hydroponics, Laboratories, etc.).

### 2.2 Structural Fixtures (`Resources/Architecture/`)
- **Doors**: Apartment wooden doors, elevator landing sliding doors, stairwell fire doors.
- **Windows**: Mullioned exterior windows with day/night emissive materials and exterior volumetric light cones.
- **Elevators**: Steel car cabin, counterweights, guide rails, and hoist cables clamped inside discrete shaft column `[-2.32 .. -1.48]`.

---

## 3. Environment Prop Catalog (`Resources/Props/`)

16 standardized props managed via `PropContentRegistry`, `PropCatalog`, and `RoomFurnishingPresenter`:

| Prop Family | Included Items | Interaction Anchors | Primary Room Theme |
|---|---|---|---|
| **Domestic Living** | 2-seat sofa, double bed, bookcase, kitchenette, coffee table, potted monstera | `seat-left/right`, `sleep`, `cook`, `browse` | Residential Apartments |
| **Commercial Diner** | Diner booth, service counter, espresso bar, stool, wall menu board | `seat-booth`, `serve-counter`, `eat-stool` | Restaurants & Diners |
| **Corporate Office** | Ergonomic desk with dual monitors, rolling office chair, filing cabinet, whiteboard | `desk-work`, `seat`, `file-browse` | Offices & Workspaces |
| **Civic & Lobby** | Reception curved desk, directory kiosk, coat rack, bench | `visitor-greet`, `staff-back`, `rest` | Ground Lobby & Skylobbies |
| **Building Systems** | Transformer panel, water pump, HVAC duct, fire extinguisher | `service-panel`, `inspect-meter` | Utility Substations & Shafts |

---

## 4. Presentation Shaders & Visual FX

All shader effects run through presentation components without domain coupling:

1. **`OneRoofUnlit.shader`**:
   - Single canonical `SRPDefaultUnlit` pass, 100% SRP Batcher compatible.
   - GPU instancing enabled on NPC materials (`Npc_DefaultSharedMaterial`, `PooledNpc_SharedMaterial`), achieving 171 draw calls at 30 floors / 300 residents.
2. **Build Placement Ghost (`PlacementGhostPresenter.cs`)**:
   - Animated holographic scanlines across placement footprints.
   - Emerald green for legal placement; red with accelerated scanlines for obstructed/invalid cells.
3. **Inspect Mode Selection & Outlines (`InspectOutlinePresenter.cs`)**:
   - Pixel-perfect outline silhouettes and soft glow when hovering or selecting rooms, elevators, and residents.
4. **Demolition Dissolve FX (`DemolitionDissolvePresenter.cs`)**:
   - Animated dissolve shader effect when bulldozing rooms or floor slabs.
5. **Congestion & Agitation Aura (`CongestionAuraPresenter.cs`)**:
   - Pulsing visual aura on elevator doors and waiting residents during commute bottlenecks.
6. **Procedural Weather & Particle Mesh (`PixelRainPresenter.cs`)**:
   - Single dynamic mesh buffer (672 quads, 2688 vertices, 1 draw call, 0 GC steady state) simulating rain drops, splash impacts, roof eave drips, drifting snowflakes, and radial fog puffs.
   - Respects building envelope occlusion.

---

## 5. Resident Skeletal Hierarchy & Wardrobe

### 5.1 Shared 17-Bone Humanoid Rig (`rig.npc.humanoid.2d.v1`)
Managed in `NpcSkeletalHierarchy.cs`:
```text
                                 [head]
                                   │
                                 [neck]
                                   │
       [arm_upper_L] ─── [shoulder] ─ [spine] ─ [shoulder] ─── [arm_upper_R]
             │                          │                           │
       [arm_lower_L]                  [hip]                   [arm_lower_R]
             │                         / \                          │
          [hand_L]         [leg_upper_L] [leg_upper_R]          [hand_R]
                                 │             │
                           [leg_lower_L] [leg_lower_R]
                                 │             │
                              [foot_L]      [foot_R]
```

### 5.2 8-Layer Wardrobe Compositor
1. `Skin/Anatomy` (base skin tones)
2. `Footwear` (shoes, boots)
3. `Lower` (pants, skirts)
4. `Upper` (shirts, jackets, uniforms)
5. `Head` (face, hairstyles)
6. `Headgear` (hats, caps, helmets)
7. `Accessory` (glasses, badges)
8. `Handheld` (briefcases, tools, mugs)

### 5.3 Resident Emotes (`Resources/Emotes/`)
24 pixel-art emote balloons with 3-frame animations across 8 categories:
- Needs: Hunger (sandwich), Energy (zzz), Hygiene (water drops), Social (speech balloon).
- Congestion: Impatience (dots), Strain (sweat), Anger (anger mark), Relief (sparkle).

---

## 6. Soundscape & Spatial Audio

Managed via `TowerAudioController.cs`:
- **Footsteps**: Surface-dependent audio (carpet, wood, concrete, steel grating).
- **Elevators**: Motor whine, hoist cable foley, bell ding, sliding door whoosh.
- **Ambience**: Dynamic room tones (office chatter, diner clatter, residential domestic hum, rain on glass).
