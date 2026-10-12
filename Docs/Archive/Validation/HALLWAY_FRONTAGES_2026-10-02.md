# Hallway frontages and zoom reveal — 2026-10-02

The hallway now has a numbered entrance and a window into every above-ground non-transit room. Entrance placement uses the room's real Door portal from the immutable topology projection; fixture rooms without portals use an in-bounds fallback. Numbers derive from stable room IDs, so adding or removing rooms does not renumber existing entrances. Windows are actual holes surrounded by wall and trim, not a duplicated interior image. Diner frontages use a taller open viewing bay.

The hallway tread, room frontage walls, doors, number plates, window trim and elevator landings fade smoothly between orthographic sizes 6 (full hallway) and 3 (full room reveal). The facade contributes an additional fade. Owned transparent materials preserve the source material and disable depth writes so fading surfaces do not leave an invisible occluder. Number glyphs are geometry in the shared frontage mesh, avoiding a text renderer per room. Three shared renderers cover tread, all room frontages, and all elevator landings.

## Validation

- Runtime and EditMode test assemblies compiled with .NET and Unity 6000.3.24f1.
- Graphical PlayMode: **22 passed, 0 failed, 1 existing optional skip**. The production-controller frontage test verifies complete door coverage, a diner bay, full hallway opacity at size 8 and zero opacity at size 2. Actual rendered captures were inspected for doors/numbers, windows, open diner frontage and close-room reveal. The existing elevator cycle/floor expansion test remains passing.
- EditMode: **1,213 passed, 0 failed, 3 existing skips**. Coverage includes actual entrance positions, immutable portal dictionary copies, mesh openings, stable numbers after removal, material ownership/cleanup and fade thresholds/clamping.
- A regression run caught native MaterialPropertyBlock allocation during controller field construction. Allocation now occurs during presenter initialization/mesh creation; no native Unity object is created by those field initializers.
- No new runtime image assets, scene edits, packages, manual wiring, simulation rules or save schema changes. Existing room art and unrelated working tree edits were preserved. No native performance or City Status certification is claimed.

## Evidence

- [EditMode XML](HallwayFrontages_2026-10-02/editmode.xml)
- [Graphical PlayMode XML](HallwayFrontages_2026-10-02/playmode.xml)
- [Numbered room frontages](HallwayFrontages_2026-10-02/hallway-numbered-frontages.png)
- [Room revealed at close zoom](HallwayFrontages_2026-10-02/hallway-residential-interior-close.png)
- [Diner open bay](HallwayFrontages_2026-10-02/hallway-diner-open-bay.png)
- [Diner interior at close zoom](HallwayFrontages_2026-10-02/hallway-diner-interior-close.png)

The original landing-door delivery is preserved in [the earlier hallway report](HALLWAY_2026-10-02.md).
