#!/usr/bin/env python3
"""
Tools/AssetPipeline/generate_prompts.py
One Roof - 2D Game Asset Prompt Synthesis Engine
Integrates Google (Imagen 3 / Gemini) and OpenAI (DALL-E 3) generative pipelines
with Antigravity agent workflows and Codex prompt specifications.
"""

import argparse
import hashlib
import json
import os
import sys

# Core One Roof Visual Design Tokens
PALETTE = {
    "structure_dark": "#0a0f18",       # Monolithic midnight navy concrete
    "structure_slate": "#161f30",      # Cool slate structural slabs & beams
    "trim_edge": "#3a4860",            # Crisp architectural perimeter edge
    "interior_oak": "#d4a373",         # Warm honey oak flooring & cabinetry
    "interior_amber": "#ebc068",       # Warm domestic ambient glow
    "accent_terracotta": "#c86446",    # Residential accent wall / diner booth
    "system_power": "#ffd700",         # Electric gold power conduit
    "system_water": "#40d9ff",         # Cyan water conduit & CRT phosphor glow
    "alert_warning": "#ff4040",        # Warning crimson / security lock
    "alert_congestion": "#ff9f40",     # Transit delay orange
}

# Master Negative Prompts
NEGATIVE_PROMPTS = {
    "empty_room": (
        "door, doorway, door frame, portal, entrance, archway, open door, aperture, door handle, "
        "window, window frame, glass panes, outside scenery through window, "
        "furniture, chair, armchair, desk, table, coffee table, bed, mattress, couch, sofa, "
        "nightstand, lamp, floor lamp, chandelier, ceiling fan, rug, carpet roll, plant, potted plant, "
        "monstera, painting, picture frame, wall art, poster, tv, television, computer, monitor, keyboard, "
        "electronics, shelves, bookcase, cabinet, drawer, closet, wardrobe, kitchen counter, stove, oven, "
        "refrigerator, sink, microwave, appliances, boxes, cardboard, luggage, suitcase, clutter, trash, debris, "
        "clothes, shoes, human, person, man, woman, resident, worker, silhouette, avatar, dog, cat, pets, "
        "3d isometric angle, tilted camera, oblique perspective, fisheye lens, vanishing perspective, blurry, "
        "depth of field, vignette, watermark, text, logo, signature"
    ),
    "isolated_prop": (
        "room, wall, floor background, ceiling, window, door, interior scene, architecture, room cutaway, "
        "human, person, hands holding, character, resident, man, woman, animal, shadow on wall, "
        "cluttered background, text, labels, watermark, logo, blurry, isometric grid lines, 3d render floor"
    ),
    "wardrobe_attachment": (
        "full human face, realistic skin texture, eyes, hair on torso, background, scene, scenery, furniture, "
        "shadow, floor, perspective angle, side view, back view, realistic wrinkles, noisy patterns, "
        "blurry, logo, brand mark, text"
    ),
    "emote": (
        "complex background, realistic photo, blurry lines, noisy gradient, 3d shading, perspective, "
        "text, watermark, messy pixels"
    )
}

def build_room_prompt(archetype: str, module_size: str, wall_finish: str, floor_finish: str) -> dict:
    size_descriptions = {
        "1x1": "compact 1x1 single micro room module (1.5m x 1.5m cutaway bay)",
        "2x2": "balanced 2x2 standard modular room shell (3.0m x 3.0m cutaway volume)",
        "3x3": "expansive 3x3 grand architectural chamber (4.5m x 4.5m spacious cutaway hall)"
    }
    size_desc = size_descriptions.get(module_size, size_descriptions["2x2"])
    
    positive = (
        f"2.5D architectural cutaway cross-section of an empty {size_desc}, straight-on front elevation view, "
        f"orthographic game backdrop art. Pristine completely vacant interior before tenant move-in, 100% unfurnished, "
        f"zero furniture, zero occupants. Flat bare {floor_finish} with horizontal baseline parallel to bottom edge. "
        f"Solid flat uninterrupted back perimeter wall with {wall_finish}. Solid clean flat side walls, completely closed perimeter, "
        f"zero baked doors, zero doorway openings, zero windows. Flat bare ceiling slab with recessed utility chase. "
        f"Framed top and bottom in monolithic midnight navy concrete ({PALETTE['structure_dark']} / {PALETTE['structure_slate']}). "
        f"Soft warm amber ambient interior lighting ({PALETTE['interior_amber']}), "
        f"crisp clean architectural vector linework, Frank Miller graphic novel ink clarity, sharp flat edges, 8k clarity."
    )
    
    return {
        "assetType": "room_shell",
        "moduleSize": module_size,
        "archetype": archetype,
        "prompt": positive,
        "negativePrompt": NEGATIVE_PROMPTS["empty_room"],
        "recommendedPpu": 360,
        "recommendedBorder": {"left": 32, "bottom": 32, "right": 32, "top": 32},
        "targetResolution": (1024, 1024)
    }

def build_prop_prompt(prop_name: str, family: str, cell_w: int, cell_h: int, interaction_anchor: str) -> dict:
    positive = (
        f"Single isolated 2D game prop sprite of a {prop_name}, straight-on 2.5D front elevation view, "
        f"orthographic camera projection, completely isolated on pure solid white background with clean silhouette. "
        f"Designed for One Roof vertical tower simulation ({family} theme, footprint {cell_w}x{cell_h} grid units). "
        f"Stylized graphic novel ink aesthetic with clean geometry, midnight slate framing ({PALETTE['structure_slate']}) "
        f"and warm domestic oak ({PALETTE['interior_oak']}) or amber ({PALETTE['interior_amber']}) accents. "
        f"Flat baseline resting perfectly on the bottom edge, zero perspective distortion, zero ground shadow, "
        f"crisp vector edges, high clarity."
    )
    
    return {
        "assetType": "prop",
        "propName": prop_name,
        "family": family,
        "cellWidth": cell_w,
        "cellHeight": cell_h,
        "interactionAnchor": interaction_anchor,
        "prompt": positive,
        "negativePrompt": NEGATIVE_PROMPTS["isolated_prop"],
        "recommendedPpu": 320,
        "pivot": {"x": 0.5, "y": 0.0},
        "targetResolution": (512, 512)
    }

def build_fixture_prompt(fixture_name: str, fixture_type: str) -> dict:
    positive = (
        f"Single isolated 2D architectural fixture sprite of a {fixture_name}, straight-on front elevation view, "
        f"orthographic projection, perfectly isolated on pure solid white background. Clean architectural cutaway "
        f"game asset for One Roof tower. Frame in monolithic slate-navy ({PALETTE['structure_dark']}), "
        f"crisp graphic novel linework, flat lighting, no drop shadows, no perspective angle tilt, "
        f"vector clarity."
    )
    
    return {
        "assetType": "fixture",
        "fixtureName": fixture_name,
        "fixtureType": fixture_type,
        "prompt": positive,
        "negativePrompt": NEGATIVE_PROMPTS["isolated_prop"],
        "recommendedPpu": 320,
        "pivot": {"x": 0.5, "y": 0.0} if "door" in fixture_type else {"x": 0.5, "y": 0.5},
        "targetResolution": (512, 512)
    }

def build_wardrobe_prompt(layer_name: str, item_desc: str, parent_bone: str) -> dict:
    positive = (
        f"Single isolated 2D modular wardrobe attachment piece: {item_desc} for the {layer_name} slot, "
        f"straight-on front elevation view, orthographic game sprite isolated on pure solid white background. "
        f"Designed for One Roof 17-bone procedural character rig (parent bone: {parent_bone}). "
        f"Flat stylized 2D game illustration, neutral base tone with clean separation for palette masking, "
        f"crisp dark ink contours, bottom-center pivot alignment at 512 PPU, zero perspective tilt."
    )
    
    return {
        "assetType": "npc_component",
        "layer": layer_name,
        "parentBone": parent_bone,
        "prompt": positive,
        "negativePrompt": NEGATIVE_PROMPTS["wardrobe_attachment"],
        "recommendedPpu": 512,
        "pivot": {"x": 0.5, "y": 0.0},
        "targetResolution": (512, 512)
    }

def build_wear_decal_prompt(decal_name: str, decal_type: str, theme: str) -> dict:
    positive = (
        f"Single isolated 2D game decal sprite of {decal_name} ({decal_type}), "
        f"straight-on front elevation view, orthographic perspective, completely isolated on pure solid white background. "
        f"Designed for One Roof housing condition degradation ({theme} theme). "
        f"Subtle Frank Miller noir grit, semi-transparent grime, water stain watermark or plaster fracture lines, "
        f"crisp graphic novel ink styling, no drop shadow, vector clarity."
    )
    return {
        "assetType": "wear_decal",
        "decalName": decal_name,
        "decalType": decal_type,
        "theme": theme,
        "prompt": positive,
        "negativePrompt": NEGATIVE_PROMPTS["isolated_prop"],
        "recommendedPpu": 360,
        "pivot": {"x": 0.5, "y": 0.5},
        "targetResolution": (256, 256)
    }

def build_theme_bundle_prompts(theme_name: str, palette_accent: str, wall_desc: str, floor_desc: str) -> list:
    """Builds a cohesive aesthetic bundle: cutaway shell, wall tile, props, and wear decals."""
    bundle = []
    bundle.append(build_room_prompt(theme_name, "2x2", f"{wall_desc} ({palette_accent})", floor_desc))
    
    # Seamless wall tile prompt
    bundle.append({
        "assetType": "wall_tile",
        "theme": theme_name,
        "prompt": (
            f"Seamless horizontally tileable texture of {wall_desc} with {palette_accent} accents, "
            f"straight-on front elevation architectural surface pattern, orthographic game backdrop art. "
            f"Clean repetitive rhythm, crisp architectural vector linework, Frank Miller graphic novel ink styling, "
            f"no furniture, no doors, no shadows, no perspective angle, 100% flat seamless surface pattern."
        ),
        "negativePrompt": NEGATIVE_PROMPTS["empty_room"],
        "recommendedPpu": 360,
        "targetResolution": (512, 512)
    })
    
    # Matching Props
    bundle.append(build_prop_prompt(f"{theme_name.capitalize()} Primary Seating", theme_name, 2, 1, "seat-left, seat-right"))
    bundle.append(build_prop_prompt(f"{theme_name.capitalize()} Storage Console", theme_name, 2, 1, "browse-front"))
    
    # Wear Decals
    bundle.append(build_wear_decal_prompt("Water Leak Stain & Peeling Plaster", "water_damage", theme_name))
    bundle.append(build_wear_decal_prompt("Baseboard Grime & Floor Debris", "floor_grime", theme_name))
    
    return bundle

def export_manifest(prompts: list, output_file: str):
    manifest = {
        "version": 2,
        "pipeline": "OneRoof-Generative-2D-Asset-Pipeline",
        "supportedEngines": ["Google-Imagen3", "OpenAI-DallE3", "Antigravity", "Codex"],
        "count": len(prompts),
        "prompts": prompts
    }
    with open(output_file, "w") as f:
        json.dump(manifest, f, indent=2)
    print(f"Exported {len(prompts)} prompt specifications to {output_file}")

def main():
    parser = argparse.ArgumentParser(description="Generate One Roof 2D asset prompts")
    parser.add_argument("--category", choices=["rooms", "props", "fixtures", "wardrobe", "bundles", "all"], default="all")
    parser.add_argument("--theme", default="tenement_noir", help="Theme identifier when generating bundles")
    parser.add_argument("--output", default="Tools/AssetPipeline/candidate_prompts.json")
    args = parser.parse_args()

    prompts = []

    if args.category in ["bundles", "all"]:
        prompts.extend(build_theme_bundle_prompts(args.theme, PALETTE["accent_terracotta"], "warm terracotta plaster and dark baseboards", "honey-oak hardwood plank"))

    if args.category in ["rooms", "all"]:
        prompts.append(build_room_prompt("residential_studio", "2x2", "warm muted terracotta plaster (#c86446)", "honey-oak hardwood plank"))
        prompts.append(build_room_prompt("commercial_office", "2x2", "cool acoustic panels with slate trim", "dark blue-grey tech carpet tile"))
        prompts.append(build_room_prompt("civic_lobby", "3x3", "deep navy architectural monolithic panels", "polished slate stone tile"))
        prompts.append(build_room_prompt("diner_retro", "2x2", "restrained brick with cream banding", "dark warm parquet floor"))
        prompts.append(build_room_prompt("micro_pod", "1x1", "minimalist slate plaster with conduit chase", "honey-oak floorboards"))

    if args.category in ["props", "all"]:
        prompts.append(build_prop_prompt("Executive Leather Sofa", "corporate", 2, 1, "seat-left, seat-right"))
        prompts.append(build_prop_prompt("Diner Booth Set", "commercial", 2, 1, "seat-left, seat-right"))
        prompts.append(build_prop_prompt("Ergonomic Dual-Monitor Desk", "corporate", 2, 1, "work-front"))
        prompts.append(build_prop_prompt("Double Residential Bed", "domestic", 2, 2, "sleep-left, sleep-right"))
        prompts.append(build_prop_prompt("Compact Kitchenette Unit", "domestic", 3, 1, "cook-front"))
        prompts.append(build_prop_prompt("Monstera Interior Planter", "decor", 1, 1, "none"))

    if args.category in ["fixtures", "all"]:
        prompts.append(build_fixture_prompt("Sliding Double Elevator Door", "door_elevator"))
        prompts.append(build_fixture_prompt("Oak Apartment Entrance Door", "door_apartment"))
        prompts.append(build_fixture_prompt("Mullioned Residential Window", "window_mullion"))
        prompts.append(build_fixture_prompt("Amber Wall Lighting Sconce", "sconce_amber"))

    if args.category in ["wardrobe", "all"]:
        prompts.append(build_wardrobe_prompt("UpperClothing", "Tailored Corporate Suit Jacket with Lapel", "spine"))
        prompts.append(build_wardrobe_prompt("LowerClothing", "Straight-Leg Dark Wool Slacks", "hip"))
        prompts.append(build_wardrobe_prompt("Footwear", "Polished Oxford Dress Shoes", "foot"))
        prompts.append(build_wardrobe_prompt("Hair", "Sleek Side-Parted Professional Haircut", "head"))

    os.makedirs(os.path.dirname(args.output), exist_ok=True)
    export_manifest(prompts, args.output)

if __name__ == "__main__":
    main()

