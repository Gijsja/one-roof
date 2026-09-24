# Resident emote replacement

The resident emote sprites in `Assets/OneRoof/Runtime/Content/Resources/Emotes/`
come from [Kenney's Emotes Pack](https://kenney.nl/assets/emotes-pack),
`PNG/Pixel/Style 1` (version 1.0, 2018). The asset page and the included
`KENNEY_EMOTES_LICENSE.txt` license the pack under CC0 1.0.

The existing 24 emote keys and Unity `.meta` files are retained. Each key's
base image and three frame images use the matching Kenney pixel icon at its
native 16 × 16 size. The wait ellipsis advances through the pack's three dot
variants. `emotes_sheet.png` is the pack's Style 1 pixel tilesheet.

Some existing semantic keys have no exact match in the pack. They use the
nearest readable symbol (for example, `skull_danger` uses a cross and
`thumbs_up` uses a happy face). The short resident caption supplies the
actual action or destination in play.
