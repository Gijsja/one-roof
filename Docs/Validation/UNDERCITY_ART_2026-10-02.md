# Undercity art and construction review — 2026-10-02

The first presentation pass was rejected as looking like a child drawing. Three authorized subagents rebuilt the earth, industrial architecture and construction/status feedback; the parent integrated and reviewed actual Editor captures.

## Tower dimensions and Resident clearance

The review found square underground cells did not match Tower proportions. Presentation now uses 0.5-unit columns and 1.75-unit storeys, matching the Tower; 32 underground columns span 16 render units. A normalized subtree scales only subterranean geometry. Surface foundations and paving remain unchanged. Residents and text compensate the subtree scale, and equipment uses uniform scaling with its base on the finished floor. Two-depth rooms remain two storeys high.

Placement hit tests/ghosts, camera limits/framing, utility endpoints and investigator feet share this mapping. Domain cell coordinates, metric budget labels, costs and saves are unchanged. A new clearance fixture uses the full-size Tower Resident rig, not a reduced underground character. The graphical close capture includes a test-only corridor Resident scale reference; it does not represent an autonomous route or simulation change.

## Delivered presentation

- Fine deterministic earth variation, uneven sediment horizons, angular inclusions, fractures and chipped excavation edges replace broad decorative bands.
- Industrial wall texture reuses a cropped approved utility backdrop. Generator machinery, lockers, benches, retaining frames and recessed light washes make room functions visible. Open corridors and topology-derived entrances preserve spatial continuity.
- Compact signs distinguish connection from staffing/operation. Build hints explain dig → floor → connect → zone, shaft previews extend from the surface, and depth/footprint/rejection feedback remains tied to validated commands.
- A screenshot contrast assertion protects dark excavated cavities. A full-suite failure revealed shared primitive contamination: placement previews wrote vertex tint/alpha into Unity’s shared Quad mesh. Previews now clone and retire their own mesh; a regression checks unrelated quads remain unchanged. Speculative terrain-material and camera-sorting changes were removed once the root cause was proven.

## Validation

Final EditMode regression: **1,187 passed, 0 failed, 3 skipped**. Final graphical PlayMode regression: **20 passed, 0 failed, 1 skipped**. Screenshots and durable XML below correspond to this final implementation. The graphical construction test checks two reachable rooms, an unfinished unfloored cavity, its contrast against solid earth, a valid real-pointer 3×3 floor preview, and no construction from hover alone. It captures the actual build palette and preview through the graphical Editor. Earth/architecture EditMode fixtures also cover deterministic geometry, topology, mesh reuse and owned-detail retirement.

- [EditMode XML](Undercity_2026-10-02/dimensions-editmode.xml)
- [Graphical PlayMode XML](Undercity_2026-10-02/dimensions-playmode.xml)
- [Close view](Undercity_2026-10-02/dimensions-close.png)
- [Overview](Undercity_2026-10-02/dimensions-overview.png)
- [Build palette](Undercity_2026-10-02/dimensions-build-palette.png)
- [Build preview](Undercity_2026-10-02/dimensions-build-preview.png)

The original rejected pass remains in [its historical report](UNDERCITY_2026-10-02.md). No simulation/save contract changes, new packages, manual scene wiring, performance certification or City Status closure are claimed. Existing broad HUD density remains outside this undercity pass. Unrelated working tree asset and gameplay changes were preserved; no commit was requested.
