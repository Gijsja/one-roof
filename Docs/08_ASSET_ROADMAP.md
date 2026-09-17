# Asset Roadmap — One Roof

## Vision and Artistic Principles

One Roof portrays a living vertical society within an enormous mixed-use skyscraper. The visual presentation is a **locked 2.5D architectural cutaway** populated by **modular 2D characters**.

### Core Visual Language

- **Architectural Shell:** Midnight-navy monolithic structure (`#0a0f18`) with cool slate surfaces and crisp pale-blue structural edges.
- **Habitation & Warmth:** Honey oak plank flooring, warm terracotta tones, and amber domestic lamps (`#ebc068`) distinguish occupied spaces.
- **System Activity:** Clear cyan (`#40d9ff`) signals operational infrastructure, elevator movement, and power flow.
- **Congestion & Risk:** Energetic orange (`#ff9f40`) and red indicators highlight wait bottlenecks, overcrowding, and equipment strain.
- **Characters:** Silhouette-led, modular figures whose clothing and carried items immediately communicate their occupation, life-stage, and current activity without visual noise.

---

## Asset Pipeline and Validation Gates

In accordance with [Docs/05_ASSET_PIPELINE.md](file:///home/geisha/Vibecode/UnityAI/one-roof/Docs/05_ASSET_PIPELINE.md), no generative or artist-authored file may enter runtime `Assets/` without passing the asset compiler gates:

```text
Request → Asset spec → Source art → Normalize → Validate → Preview → Approve → Registry → Atlas/Addressables
```

### Mandatory Validation Criteria

1. **Naming & Identity:** Immutable namespaced `ContentId` (e.g., `prop.furniture.sofa.v1`, `npc.resident.service.v1`).
2. **Geometry & Bounds:** Standardized cell sizes, power-of-two atlases, transparency trimmed to declared bounding box.
3. **Scale & Perspective:** Strict orthographic cutaway projection; residents normalized to 0.58m height, rooms to 1.42m height, doors to 0.80m height.
4. **Anchors & Interaction:** Bottom-center pivot `(0.5, 0.0)` for characters and floor props; declared interaction points (`seat`, `sleep`, `cook`, `work`, `browse`).
5. **Rig Compatibility:** NPC components must map to the shared 17-bone humanoid rig (`rig.npc.humanoid.2d.v1`) and declare their slot within the 8-layer contract.

---

## Phased Delivery Roadmap

### Phase 1 — First-Playable Architectural Dressing (Milestone 5.2)

**Goal:** Eliminate all placeholder geometric primitives from the interactive Tower cutaway, replacing them with production-quality registered architectural assets.

| Asset Group | Deliverables | Target Path | Exit Criteria |
| --- | --- | --- | --- |
| **Room Backdrops** | Sliced, 9-sliceable textures for Residential Studio, Corporate Office, Commercial Diner, and Reception Lobby | `Assets/OneRoof/Runtime/Content/Resources/Rooms/` | Seamless horizontal expansion across 1 to 4 grid cells without texture distortion. |
| **Architectural Props** | Apartment entrance doors (framed with brass knob), elevator landing doors, mullioned windows with amber light, wall sconces | `Assets/OneRoof/Runtime/Content/Resources/Architecture/` | Clear door portals aligned with discrete transit graph nodes; illuminated windows anchored cleanly in apartment cutaways. |
| **Elevator Cabin & Shaft** | Steel elevator car cabin, counterweight assemblies, guide rails, and hoist cables | `Assets/OneRoof/Runtime/Content/Resources/Elevators/` | Car cabin clearly accommodates up to 8 resident views with visible passenger silhouettes. |

---

### Phase 2 — Environment Prop Families & Themed Rooms (Milestone 5.3)

**Goal:** Provide full interior furnishings across residential, commercial, civic, and maintenance spaces that support resident routine behaviors and spatial legibility.

| Prop Family | Included Items | Interaction Points | Primary Room Theme |
| --- | --- | --- | --- |
| **Domestic Living** | 2-seat sofa, double bed, tall bookcase, compact kitchenette, coffee table, potted monstera | `seat-left/right`, `sleep`, `cook`, `browse` | Residential Studios & Apartments |
| **Commercial Diner** | Diner booth, service counter, espresso bar, stool, wall menu board, bistro awning | `seat-booth`, `serve-counter`, `eat-stool` | Restaurants & Cafes |
| **Corporate Office** | Ergonomic desk with dual monitors, wheeled office chair, 4-drawer filing cabinet, whiteboard | `desk-work`, `seat`, `file-browse` | Offices & Workspaces |
| **Civic & Lobby** | Reception curved desk, freestanding directory kiosk, coat rack, waiting bench | `visitor-greet`, `staff-back`, `rest` | Ground Lobby & Sky lobbies |
| **Building Systems** | Transformer panel, water boiler, HVAC duct terminal, fire extinguisher station | `service-panel`, `inspect-meter` | Utility, Maintenance & Shafts |

---

### Phase 3 — Spine 2D Skeletal Animation & Wardrobe Composition (Milestone 6.0)

**Goal:** Bring simulation residents to life with fluid 2D skeletal animation and modular wardrobe customization driven by the shared 17-bone Spine rig.

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

#### Core Animation Set

1. `idle-breathe`: Subtle vertical chest heave and head balance (3.0s cycle).
2. `walk-stride`: Natural 2.5D locomotion along corridor slabs (1.0s cycle).
3. `queue-wait`: Weight shift and looking toward elevator shaft (4.0s cycle).
4. `elevator-ride`: Compact standing posture with subtle elevator vibration damping.
5. `chair-sit`: Transition from stand to sit on chairs, sofas, and diner booths.
6. `meal-eat` / `desk-work`: Upper-body task interactions synchronized with room props.

#### 8-Layer Wardrobe Compositor

```text
Layer 1: hair_back       (behind head & neck)
Layer 2: body            (base skin mesh: torso, head, limbs)
Layer 3: footwear        (shoes, boots, sneakers on foot bones)
Layer 4: lower_clothing  (pants, skirts, cargo shorts on hip/leg bones)
Layer 5: upper_clothing  (shirts, blouses, sweaters, suits on spine/chest bones)
Layer 6: face            (eyes, brows, mouth expressions on head bone)
Layer 7: hair_front      (front bangs, hats, beanies on head bone)
Layer 8: accessory       (glasses, aprons, badges, tool belts, jewelry)
Layer 9: carried_prop    (bags, coffee mugs, laptops, toolboxes in hand bones)
```

- **Dynamic Household Palette:** Fabric colors on `upper_clothing` and `lower_clothing` tinted according to household affiliation while preserving skin and prop authenticity.

---

### Phase 4 — AssetLab Validation Tooling & Addressables (Milestone 6.1)

**Goal:** Establish an automated, editor-integrated inspection environment (`Assets/Scenes/AssetLab.unity`) and automated test runner to guarantee zero visual regressions as content scales.

#### Validation Tooling Capabilities

- **Interactive Seam Tester:** Validates 9-slice room backdrops at non-standard room widths (1 to 6 cells) to detect edge bleeding or pixel seams.
- **Rig Inspector:** Automatically tests new clothing sprites against all bone rotation limits to catch skin weighting errors or clipping.
- **Atlas Packer & Addressables:** Groups assets into downloadable / loadable bundles:
  - `Content-Architecture-Core`
  - `Content-Props-Residential`
  - `Content-Props-Commercial`
  - `Content-Characters-Skeletons`
  - `Content-Characters-Wardrobe`
- **Automated CI Gate:** Rejects PRs containing unindexed textures, invalid anchors, or missing `.meta` files.

---

### Phase 5 — Audio Soundscapes & Environmental Atmosphere (Milestone 6.2)

**Goal:** Deepen emotional legibility through immersive spatial acoustics and responsive atmospheric lighting.

#### Sound Design Architecture

- **Corridor Footsteps:** Surface-reactive audio (wood creak, carpet thud, ceramic click, steel resonance) modulated by resident speed.
- **Vertical Circulation Audio:** Elevator car motor whine ascending/descending, hydraulic brake hiss, arrival chime, and gate cycle clatter.
- **Room Roomtones:**
  - *Residential:* Muffled domestic quiet, faint refrigerator hum, clock ticking.
  - *Diner:* Cutlery clatter, espresso steam hisses, murmur of conversation.
  - *Office:* Keyboard clatter, paper shuffling, air conditioning hiss.
  - *Lobby:* Grand acoustic reverberation, footsteps, revolving door rotation.
- **Congestion Audio Cue:** Rising low-frequency tension drone when lobby queues exceed severe thresholds (>20 waiting).

#### Environmental Visual Effects

- **Volumetric Lamp Cones:** Soft amber interior lighting emitting from windows into darker corridor thresholds.
- **Elevator Cable Shimmer:** Fine specular highlights on moving steel cables within the open shaft cutaway.
- **Transit Particle Pulses:** Subtle cyan dust particles drifting through active ventilation shafts and open elevator cavities.

---

## Technical Performance Budgets (30-Floor Beta Boundary)

| Metric | Budget Target | Mitigation Strategy |
| --- | --- | --- |
| **Visible NPCs** | Max 60 concurrent views | `NpcVisibilityPolicy` prioritizes visible camera viewport, in-transit commuters, and congested queues. |
| **Draw Calls** | < 120 draw calls | Shared sprite atlas for environment props; GPU instancing for identical structural floor slabs and dividing walls. |
| **Texture Memory** | < 180 MB uncompressed | 512 PPU standard; ASTC/BC7 compression across all production atlases. |
| **Animation Overhead** | < 1.5 ms per frame | Off-screen simulation entities execute purely in Domain without Transform or SpriteSkin evaluation. |
| **Frame Rate Target** | Stable 60 FPS | Release profiling on agreed reference desktop baseline. |
