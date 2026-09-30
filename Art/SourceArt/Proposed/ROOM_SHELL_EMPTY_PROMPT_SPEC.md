# Empty Room Shells (1x1, 2x2, 3x3) — Prompt Engineering & Asset Specification

**Document Version:** 1.0.0\
**Status:** PROPOSED SOURCE ART SPECIFICATION\
**Target Schema:** [`Schemas/asset-spec.schema.json`](../../Schemas/asset-spec.schema.json) (`assetType: "room_shell"`)\
**Design Reference:** [`Docs/ASSETS.md`](../../Docs/ASSETS.md), [`Docs/UX_AND_GAMEPLAY.md`](../../Docs/UX_AND_GAMEPLAY.md)

---

## 1. Executive Summary & Design Rationale

In **One Roof**, towers are vertical architectural ecosystems where structure, zoning, and systems are shaped by the Steward, while autonomous residents live, work, and express agency.

### Core Mechanic: The Empty Canvas & Outside Acquisition
- **Rooms start 100% EMPTY**: When the Steward zones and builds a residential, commercial, or utility room, it is instantiated as an unadorned structural shell (clean floor, bare walls, ceiling slab, portal cutouts).
- **Inhabitants bring/buy furniture from the outside world**: Residents and business tenants acquire furnishings, appliances, decorations, and tools through outside supply chains, catalog deliveries, and city markets.
- **Decoupled Visual Hierarchy**:
  ```text
  [Sorting Layer: -10] Room Shell Backdrop (Empty architectural cutaway)
         ↓
  [Sorting Layer:  -5] Architectural Fixtures (Doors, mullion windows, sconces)
         ↓
  [Sorting Layer:   0] Dynamic Resident Props (Furniture bought in outside world)
         ↓
  [Sorting Layer:  +5] Autonomous Residents (Spine 2D 17-bone animated figures)
  ```
- **Consequence for Art Generation**: Room backdrop images **must never bake in furniture, inhabitants, clutter, or appliances**. Any baked prop permanently blocks runtime placement and breaks resident agency.

---

## 2. Spatial Dimension Matrix (1x1, 2x2, 3x3)

One Roof operates on a **0.5m discrete grid pitch** and a **3.0m floor-to-floor height** (with a standard visual cutaway clearance of **1.42m** to 1.48m per floor slab).

Room shells are specified in modular grid unit footprints ($W \times H$ units):

| Module Size | Grid Footprint | World Dimensions ($W \times H$) | Canvas Aspect Ratio | Resolution Target | Target Use Cases | Inhabitant Capacity |
|---|---|---|---|---|---|---|
| **1x1** | $1 \times 1$ module (compact bay) | $1.5\text{ m} \times 1.5\text{ m}$ (or 1-cell micro alcove) | **1:1** (Square) | $1024 \times 1024\text{ px}$ | Micro-Apartment pod, single bathroom/washroom, utility closet, elevator vestibule, security booth | 1 Resident |
| **2x2** | $2 \times 2$ module (standard bay) | $3.0\text{ m} \times 3.0\text{ m}$ (standard 1-story double-width or loft mezzanine) | **1:1** (Square) | $1024 \times 1024\text{ px}$ (or $2048 \times 2048$) | Studio apartment, corporate office, boutique retail, clinic exam room, workshop | 1–2 Residents / 1 Business |
| **3x3** | $3 \times 3$ module (grand bay) | $4.5\text{ m} \times 4.5\text{ m}$ (or multi-story executive volume) | **1:1** (Square) | $1024 \times 1024\text{ px}$ (or $2048 \times 2048$) | Family penthouse, double-height diner, flagship store, staff commons, server vault | 3–6 Residents / Flagship Hub |

### Perspective & Alignment Constraints
- **Camera Projection**: Strictly locked **2.5D architectural cross-section cutaway** (straight-on front elevation, flat horizon, zero perspective vanishing point on horizontal floor lines).
- **Floor Baseline**: The top of the floor finish must sit exactly at the lower $10\%$ to $15\%$ margin of the frame, perfectly horizontal, parallel to the image base.
- **Slab Enclosure**: A visible cross-section of the structural ceiling and floor slab framing the top and bottom in One Roof's **midnight-navy monolithic concrete** (`#0a0f18` / `#161f30`).
- **Walls**: Left and right side walls must either show a cutaway cross-section profile or clean flat side trim with defined doorway clearance zones.

---

## 3. The Empty-Shell Prompting Architecture

Generating empty rooms with modern diffusion models (Midjourney v6, SDXL, Flux.1, Imagen 3, DALL-E 3) requires counteracting the model's natural bias to furnish rooms.

### 3.1 Prompt Anatomy

Every room shell prompt must follow this 5-part construction:

```text
[CAMERA & CUTAWAY PROFILE] + [ROOM SIZE & SPATIAL VOLUME] + [EMPTY ARCHITECTURAL SHELL DESCRIPTION] + [MATERIALS & SURFACES] + [LIGHTING & PALETTE]
```

1. **Camera & Cutaway Profile**: `2.5D architectural cutaway cross-section, straight-on front elevation, orthographic view, cross-section architectural illustration, crisp vector-detailed game art style`.
2. **Room Size & Spatial Volume**:
   - For 1x1: `compact 1x1 single micro room, intimate capsule space, single structural module`.
   - For 2x2: `balanced 2x2 medium room shell, standard modular studio volume, spacious square living space`.
   - For 3x3: `expansive 3x3 large architectural chamber, high-ceiling open floor plan, multi-tier spacious hall`.
3. **Empty Architectural Shell Description**: `completely empty room, 100% unfurnished, vacant interior before move-in, bare unobstructed floorboards, clean blank perimeter walls, bare flat ceiling, open portal doorway frame on edge, no occupants`.
4. **Materials & Surfaces**: `monolithic dark navy concrete structural slab frame, [specific wall finish], [specific floor finish], crisp architectural trim`.
5. **Lighting & Palette**: `soft ambient interior daylight, warm amber interior glow (#ebc068), subtle contact shadows along baseboards, neutral clean exposure, zero cast object shadows`.

---

### 3.2 The Ironclad Negative Prompt

Models must be explicitly prevented from placing furniture, humans, or clutter. Always inject this master negative token list:

```text
--no furniture, props, chair, armchair, desk, table, coffee table, bed, mattress, couch, sofa, nightstand, lamp, floor lamp, chandelier, ceiling fan, rug, carpet roll, plant, potted plant, monstera, painting, picture frame, wall art, poster, tv, television, computer, monitor, keyboard, electronics, shelves, bookcase, cabinet, drawer, closet, wardrobe, kitchen counter, stove, oven, refrigerator, sink, microwave, appliances, boxes, cardboard, luggage, suitcase, clutter, trash, debris, clothes, shoes, human, person, man, woman, resident, worker, silhouette, avatar, dog, cat, pets, 3d isometric angle, tilted camera, oblique perspective, fisheye lens, vanishing perspective, blurry, depth of field, vignette, watermark, text, logo, signature
```

*For Stable Diffusion / WebUI / ComfyUI Negative Prompt Embeddings*:
```text
(furniture:1.5), (props:1.4), (chair:1.4), (table:1.4), (bed:1.5), (sofa:1.5), (cabinet:1.3), (appliances:1.3), (people:1.6), (person:1.6), (human:1.6), (character:1.5), (clutter:1.4), (boxes:1.3), (rug:1.3), (lighting fixture:1.2), (isometric:1.4), (oblique perspective:1.4), (tilted angle:1.3), (distorted lines:1.3), text, watermark, signature, blurry, lowres
```

---

## 4. Prompt Catalog by Room Size & Archetype

### 4.1 Size: 1x1 Micro Rooms (Compact Module, 1:1 Aspect Ratio)

#### A. Residential Micro-Apartment Pod (Warm Inhabited)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an empty compact 1x1 micro-apartment pod, straight-on front elevation view, orthographic game backdrop art. Pristine vacant interior before move-in, completely empty, zero furniture. Clean honey-oak hardwood floorboards, warm muted slate-blue smooth plaster back wall with subtle honey-oak baseboard trim, flat bare ceiling with recessed micro-conduit. Structural outer frame in monolithic midnight navy concrete (#0a0f18). Open doorframe aperture on left edge. Soft warm amber ambient domestic lighting (#ebc068), clean architectural lines, minimalist stylized game asset, crisp flat edges, 8k, high clarity --ar 1:1 --stylize 120`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $1024 \times 1024\text{ px}$.

#### B. Commercial / Utility Server Alcove (Cool Systemic)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an empty compact 1x1 utility maintenance alcove, straight-on front elevation view, orthographic cross-section game art. Vacant industrial compartment, 100% empty, zero equipment, no machinery, no racks. Bare dark charcoal concrete screed floor, cool slate grey acoustic wall panels with thin cyan trim conduits, dark industrial ceiling slab with bare cable tray. Enclosed by midnight navy heavy structural beams (#0a0f18). Cool diffuse cyan-tinted ambient lighting (#40d9ff), clean and geometric, crisp game interior shell --ar 1:1 --stylize 100`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $1024 \times 1024\text{ px}$.

---

### 4.2 Size: 2x2 Standard Rooms (Studio & Office Bay, 1:1 Aspect Ratio)

#### A. Residential Studio Apartment (Warm Domestic)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an empty 2x2 residential studio apartment shell, straight-on orthographic cross-section, 2D game asset background. Completely empty and unfurnished, ready for tenant move-in, zero furniture, bare expansive honey-oak plank flooring running horizontally. Neutral warm beige-grey painted drywall with delicate botanical wallpaper accent band, clean white wood baseboards and door casings. Bare ceiling with subtle track lighting rail. Structural outer frame of heavy monolithic midnight navy concrete. Left door opening, right exterior wall cutaway with clean mullion window frame showing soft exterior sky gradient. Warm domestic amber ambient fill light (#ebc068), pristine and uncluttered, stylized architectural vector illustration --ar 1:1 --stylize 150`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $1024 \times 1024\text{ px}$ (or $2048 \times 2048\text{ px}$).

#### B. Commercial Corporate Office (Cool Professional)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an empty 2x2 corporate office room shell, straight-on orthographic front elevation, architectural cutaway illustration. Fully vacant and unleased commercial space, zero furniture, no desks, no chairs, bare muted grey modular carpet tile flooring with subtle grid texture. Cool slate-blue acoustic felt wall panels, minimalist black metal trim and baseboard, recessed ceiling grid with flat flush LED diffuser panels. Heavy dark navy monolithic structural columns on perimeter. Crisp straight horizontal floor line, bright balanced daylight lighting, pristine high-end game interior backdrop --ar 1:1 --stylize 120`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $1024 \times 1024\text{ px}$ (or $2048 \times 2048\text{ px}$).

#### C. Commercial Diner / Cafe Shell (Warm Social)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an empty 2x2 urban diner or boutique cafe shell, straight-on orthographic elevation view, game environment cutaway. Freshly renovated completely empty commercial space, zero furniture, no counters, no booths, no stools. Warm terracotta geometric quarry tile floor, warm cream painted upper walls, restrained lower terracotta exposed brick wainscoting with dark timber cap rail. Bare industrial ceiling with dark structural ribs. Thick midnight-navy concrete outer cutaway frame. Left service doorway portal. Warm golden amber ambient cafe lighting, clean architectural game asset, high readability --ar 1:1 --stylize 140`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $1024 \times 1024\text{ px}$ (or $2048 \times 2048\text{ px}$).

---

### 4.3 Size: 3x3 Grand Rooms (Penthouses, Atriums & Flagships, 1:1 Aspect Ratio)

#### A. Luxury High-Ceiling Penthouse Loft (Warm Premium)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of an expansive empty 3x3 luxury penthouse loft, straight-on orthographic cross-section, high-detail 2D game interior background. Spectacular cavernous multi-story open volume, completely vacant, zero furniture, pristine wide-plank golden honey oak hardwood floor spanning the entire base. Double-height warm taupe textured architectural walls, slim architectural bronze reveal trim, ceiling with exposed decorative cedar beams and flush brass electrical tracks. Massive exterior window cutaway on right with crisp black steel mullions revealing distant pale sky. Monolithic midnight navy structural core framework framing the room cutaway (#0a0f18). Generous golden hour domestic ambient light, ultra-clean game art, architectural drawing style, no clutter --ar 1:1 --stylize 160`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $2048 \times 2048\text{ px}$ (or $1024 \times 1024\text{ px}$).

#### B. Civic Commons / Community Hall (Balanced Inhabited)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of a grand empty 3x3 civic community hall and skylobby, straight-on orthographic elevation view, architectural cutaway game art. Huge empty public interior space, 100% unfurnished, zero benches, zero reception desks, zero signage. Large-format polished light terrazzo stone floor with brass divider strips. Tall acoustic timber slat wall cladding interspersed with soft navy feature panels, monumental midnight navy concrete load-bearing pillars at flanks. High open ceiling with structural acoustic baffles. Soft diffused daylight illumination, warm inviting public atmosphere, ultra clean lines, crisp game environment backdrop --ar 1:1 --stylize 130`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $2048 \times 2048\text{ px}$.

#### C. Subterranean Workshop / Industrial Hub (Raw Infrastructure)
- **Positive Prompt**:
  > `2.5D architectural cutaway cross-section of a massive empty 3x3 subterranean workshop and utility hub, straight-on orthographic front elevation, stylized cutaway illustration. Vast industrial vault excavated in bedrock, completely empty, zero machinery, zero workbenches, zero tools, zero storage crates. Heavy reinforced diamond-plate steel floor and polished sealed concrete slab, rugged dark basalt stone wall with heavy steel reinforcement ribs, overhead high-clearance crane rail gantry and yellow utility conduit brackets. Monolithic midnight navy and hazard-accented outer concrete frame. Industrial amber trouble-light and cool cyan ambient fill, clean geometry, dramatic game art --ar 1:1 --stylize 130`
- **Negative Prompt**: Use Master Negative (§3.2).
- **Target Size**: $2048 \times 2048\text{ px}$.

---

## 5. Outside-World Prop Docking & Alignment Specification

When residents return from the outside world carrying or ordering furniture, the game runtime places these items dynamically via `RoomFurnishingPresenter` and `PropCatalog`.

### 5.1 Docking Zones in Empty Shells

```text
+-------------------------------------------------------------+  Top Slab (Ceiling)
| [Zone C: Ceiling / Pendant Mount]                           |  Y = 1.0 (Top 15%)
|                                                             |
| [Zone B: Wall Mount (Sconces, Shelves, Art, Monitors)]      |  Y = 0.4 - 0.7 (Mid 30%)
|                                                             |
| [Zone D: Doorway]                     [Zone E: Window / View]|  Left / Right Portals
|                                                             |
|=============================================================|  Floor Baseline (Y = 0)
| [Zone A: Floor Anchor Plane] (Beds, Sofas, Desks, Counters) |
+-------------------------------------------------------------+  Bottom Slab (Subfloor)
```

1. **Zone A: Floor Anchor Plane ($Y = 0$)**:
   - Must be continuous, completely flat, and devoid of pre-rendered rugs, curbs, steps, or dips.
   - Props dock with anchor `(0.5, 0.0)` (bottom-center) resting precisely on this line.
2. **Zone B: Wall Mount Zone ($Y = 0.40 \text{ to } 0.70$ normalized)**:
   - Must remain uniform in texture and unobstructed by 3D recesses so wall-hung props (amber sconce, wall-clock, whiteboard, shelving) do not clip or look detached.
3. **Zone C: Ceiling Mount Zone ($Y = 0.85 \text{ to } 1.0$)**:
   - Clean ceiling plane for future ceiling fans, chandeliers, or sprinkler heads.
4. **Zone D & E: Ingress / Egress Clear Zones**:
   - Left $15\%$ and right $15\%$ of the room shell floor should allow free clearance for resident walking animations when entering/exiting portals.

### 5.2 Matching Outside-World Prop Footprints to Room Shells

Residents purchase items from the **Prop Family Catalog** ([`Docs/ASSETS.md §5`](../../Docs/ASSETS.md#5-environment-prop-catalog-resourcesprops)). The empty room shell accommodates them as follows:

| Prop Key | Footprint ($W \times H$ cells) | Minimum Room Size | Default Placement Anchor |
|---|---|---|---|
| `apartment-door-single` | $1 \times 1$ | 1x1, 2x2, 3x3 | Zone D (Left/West portal) |
| `residential-bookcase-tall` | $1 \times 1$ | 1x1, 2x2, 3x3 | Zone A / Wall-flush |
| `interior-monstera-planter` | $1 \times 1$ | 1x1, 2x2, 3x3 | Zone A / Corner accent |
| `office-chair-wheeled` | $1 \times 1$ | 1x1, 2x2, 3x3 | Zone A / Desk companion |
| `wall-sconce-amber` | $1 \times 1$ | 1x1, 2x2, 3x3 | Zone B (Wall-center, $Y = 0.6$) |
| `residential-sofa-2seat` | $2 \times 1$ | 2x2, 3x3 | Zone A (Living center) |
| `office-desk-monitor` | $2 \times 1$ | 2x2, 3x3 | Zone A (Work zone) |
| `residential-double-bed` | $2 \times 2$ | 2x2, 3x3 | Zone A (Sleep zone) |
| `diner-booth-2seat` | $2 \times 1$ | 2x2, 3x3 | Zone A (Dining perimeter) |
| `compact-kitchenette` | $3 \times 1$ | 2x2 (full span) or 3x3 | Zone A (Kitchen alcove) |
| `diner-service-counter` | $3 \times 1$ | 3x3 | Zone A (Commercial service line) |
| `lobby-reception-desk` | $3 \times 1$ | 3x3 | Zone A (Civic reception zone) |

---

## 6. Asset Validation, Normalization & Importer Pipeline

Before any generated room shell enters `Assets/OneRoof/Runtime/Content/Resources/Rooms/`:

### 6.1 Technical Import Gate (Unity URP)
- **Texture Type**: `Sprite (2D and UI)`
- **Sprite Mode**: `Single` (for dedicated room backdrops) or `Multiple` (if combined in an atlas sheet)
- **Pixels Per Unit (PPU)**: `360` (standard for room backdrops in `ArchitecturalFixtureCatalog`)
- **Mesh Type**: `Full Rect`
- **Filter Mode**: `Bilinear`
- **Compression**: `High Quality` (BC7 / ASTC)
- **9-Slice Borders**: `L: 32px, B: 32px, R: 32px, T: 32px` (ensures structural slab and side trim stretch seamlessly without distorting wall finishes).

### 6.2 Asset Spec Declaration (`.spec.json`)
Every generated shell must have a companion `.spec.json` conforming to `Schemas/asset-spec.schema.json`:

```json
{
  "assetType": "room_shell",
  "contentId": "room.shell.residential.studio_2x2",
  "styleProfile": "midnight-structure-warm-interior-v1",
  "theme": "residential:apartment",
  "footprint": {
    "width": 2,
    "height": 2
  },
  "anchors": [
    "floor_baseline",
    "portal_west",
    "portal_east",
    "wall_mount_upper"
  ],
  "tags": [
    "empty_canvas",
    "unfurnished",
    "tenant_furnishable",
    "modular_shell"
  ],
  "approvalState": "draft",
  "source": {
    "kind": "generator",
    "provenance": "Midjourney v6 prompt with strict negative furniture suppression",
    "promptHash": "sha256_placeholder"
  }
}
```

---

## 7. Quality Checklist & Rejection Criteria

When reviewing generated room shells in `AssetLab` / Unity preview, reject the image immediately if:

- [ ] **ANY furniture or prop is present**: Tables, chairs, beds, counters, rugs, boxes, or lamps baked into the image.
- [ ] **ANY inhabitant is visible**: Silhouettes, people, hands, or pets.
- [ ] **Tilted perspective / Oblique angle**: The floor line must be perfectly horizontal ($0.0^\circ$ pitch/roll).
- [ ] **Harsh directional shadows from missing objects**: Shadow pooling on the floor where an AI thought a desk should be.
- [ ] **Irregular floor surface**: Stepped, uneven, curved, or bumpy flooring that causes placed props to appear floating or buried.
- [ ] **Incompatible Color Language**: Failure to incorporate One Roof's core dark navy structural shell (`#0a0f18`) and designated theme palette (Warm amber `#ebc068` for residential, Cool slate/cyan `#40d9ff` for office/utility).
