# Room cutaway restoration — 2026-10-02

Reported symptom: room interiors disappeared after hallway delivery; Residents looked like they were standing at hallway doors with “At home” bubbles.

The hallway frontage wall strips covered the rooms. Render inspection also found that the shared transparent world material uses no depth writes: floor backing at sorting order 0 drew over wallpaper (-10), interior fixtures and furniture (-2). Removing the wall alone left the rooms dark. Floor backing now sorts at -20, including repair of adopted scene views. Before/after graphical captures directly show the wallpaper and fixtures restored.

Hallway doors and stable numbers remain; opaque wall strips and window trim are omitted in cutaway. Indoor interaction docks and fallback positions use interior depth. Resident rigs use sorting groups (indoor -1, circulation +1), preserving internal limb ordering; sprite LOD uses the corresponding order. Macro presentation keeps its existing depth positions. At facade alpha >= .5, indoor Residents are excluded from rig selection and rendering, so their bubbles cannot remain floating over concealed rooms. Returning to cutaway restores views.

Validation: Unity 6000.3.24f1 compiled runtime and test assemblies. Final presentation EditMode run: **471 passed, 0 failed, 1 graphical-only skip**. Final hallway graphical PlayMode run: **3 passed, 0 failed**, covering open cutaway geometry, numbered entrances, zoom, facade concealment, elevator cycling/expansion and Resident arrival. Added regression checks cover indoor depth/sorting, concealment/restoration and repair of stale floor backing order. `git diff --check` passed. A first sorting-group implementation failed 21 tests because Unity missing-component objects do not work with C# null coalescing; explicit Unity null checks fixed this and the complete presentation rerun passed.

- [EditMode results](RoomCutaway_2026-10-02/editmode.xml)
- [PlayMode results](RoomCutaway_2026-10-02/playmode.xml)
- [Open cutaway overview](RoomCutaway_2026-10-02/hallway-numbered-frontages.png)
- [Close room reveal](RoomCutaway_2026-10-02/hallway-residential-interior-close.png)

Captures were inspected. The fixture is paused at startup; residential furnishing inventories depend on the simulation housing projection, and these captures do not certify every furnishing/activity combination. No simulation/save changes or native performance certification. Existing unrelated working tree changes preserved. No commit requested.
