# Gold Standard — 30-floor living-city playground

Open `Assets/Scenes/Tower_GoldStandard30.unity` and press Play. The scene intentionally stores only its bootstrap and an inactive Tower World; gameplay views are generated when it starts.

The tower starts at 07:30 with 30 built floors, 300 residents in 100 three-person households, three elevator cars, a continuous stairwell, electrical/water/waste risers, floor transformers, and water boosters every four floors. Ground level provides the lobby, retail, diner and utility sources. Floors 1–25 contain homes; floors 26–29 contain offices, food, retail, clinic, maintenance and security services. Treasury starts at 250,000 for experimentation. The normal economy and autonomous residents remain active; this is not a frozen population display.

The city environment adds three batched skyline layers, glazed storefronts, signs, roof equipment, streets, crossings, lamps, benches, trees, transit shelters, 12 moving vehicles, seven clouds and seven birds. The shared URP shader changes the sky, windows and facades with the projected day/night and weather state. Ambient motion pauses with the simulation. The original game scenes retain their existing environment.

| Control | Action |
| --- | --- |
| 5 / Home / F | Full-tower overview |
| 6 | Street and lower floors |
| 7 | Rooftop and upper floors |
| Mouse wheel | Zoom |
| Middle/right drag, arrows | Pan |
| Space | Pause / resume |
| W | Cycle weather (existing control) |
| R | Reset the populated 30-floor seed |
| H | Collapse HUD and hide playground guide |
| 1–4 | Existing Build / Inspect / Data / Manage modes |

Rebuild the assets and scene through **One Roof → Build Gold Standard 30-Floor Playground**, or batch execute `OneRoof.Editor.Tower.GoldStandardLevelBuilder.Build`. The builder preserves mesh/material GUIDs. Assets live in `Assets/OneRoof/Art/GoldCity`, with the environment assembled in `GoldCityEnvironment.prefab`. Source provenance and asset contracts are in `Art/SourceArt/Proposed/GoldCity/ASSET_SPEC.md`.

This is a City-scale playground and visual test level, not proof of OR-1003 completion. The 30-day City Status hold, exact save continuation, and reference-hardware tick/FPS/draw-call budgets remain separate gates. The snapshot test revealed that the existing schedule trip generator does not persist its next trip ID; the handoff records that failure rather than claiming exact replay. The normal resident view pool remains capped at 60.
