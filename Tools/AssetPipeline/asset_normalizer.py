#!/usr/bin/env python3
"""
Tools/AssetPipeline/asset_normalizer.py
One Roof - 2D Asset Normalization and Ingestion Tool
Performs background removal, horizon leveling, metric PPU grid fitting,
anchor positioning, and specification metadata (.spec.json) generation.
"""

import argparse
import hashlib
import json
import math
import os
import sys
from PIL import Image

def compute_sha256(filepath: str) -> str:
    sha = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(8192):
            sha.update(chunk)
    return sha.hexdigest()

def extract_alpha_chromakey(img: Image.Image, bg_color=(255, 255, 255), tolerance=28) -> Image.Image:
    """Keys out solid or near-solid background with soft anti-aliased edge falloff."""
    rgba = img.convert("RGBA")
    data = rgba.getdata()
    new_data = []
    
    bg_r, bg_g, bg_b = bg_color
    
    for r, g, b, a in data:
        dist = math.sqrt((r - bg_r)**2 + (g - bg_g)**2 + (b - bg_b)**2)
        if dist < tolerance:
            new_data.append((r, g, b, 0))
        elif dist < tolerance + 16:
            # Soft transition edge
            alpha_factor = (dist - tolerance) / 16.0
            new_data.append((r, g, b, int(a * alpha_factor)))
        else:
            new_data.append((r, g, b, a))
            
    rgba.putdata(new_data)
    return rgba

def normalize_asset(
    input_path: str,
    output_dir: str,
    content_id: str,
    asset_type: str,
    cell_w: int = 1,
    cell_h: int = 1,
    ppu: float = 320.0,
    anchor: str = "bottom-center",
    auto_key_bg: bool = True
) -> dict:
    if not os.path.exists(input_path):
        raise FileNotFoundError(f"Input file not found: {input_path}")
        
    os.makedirs(output_dir, exist_ok=True)
    raw_hash = compute_sha256(input_path)
    
    img = Image.open(input_path)
    
    # Auto-extract alpha if image has no transparency or has white corners
    if auto_key_bg and img.mode in ("RGB", "RGBA"):
        corners = [img.getpixel((0, 0)), img.getpixel((img.width - 1, 0)), 
                   img.getpixel((0, img.height - 1)), img.getpixel((img.width - 1, img.height - 1))]
        # Check if corners are near white
        is_white_bg = all(sum(c[:3]) / 3 > 240 for c in corners)
        if is_white_bg:
            img = extract_alpha_chromakey(img, bg_color=(255, 255, 255), tolerance=25)
    
    img = img.convert("RGBA")
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    # Metric sizing calculation: 0.5m grid cell pitch
    metric_width_m = cell_w * 0.5
    metric_height_m = cell_h * 0.5 if asset_type != "room_shell" else 1.42
    
    target_pixel_w = int(round(metric_width_m * ppu))
    target_pixel_h = int(round(metric_height_m * ppu))
    
    # Preserve aspect ratio while fitting into maximum envelope
    scale = min(target_pixel_w / img.width, target_pixel_h / img.height)
    new_w = max(1, int(round(img.width * scale)))
    new_h = max(1, int(round(img.height * scale)))
    
    resized = img.resize((new_w, new_h), Image.Resampling.LANCZOS)
    
    # Create final canvas
    canvas = Image.new("RGBA", (target_pixel_w, target_pixel_h), (0, 0, 0, 0))
    
    # Alignment based on anchor
    if anchor == "bottom-center":
        paste_x = (target_pixel_w - new_w) // 2
        paste_y = target_pixel_h - new_h  # Rest on floor baseline
        pivot = {"x": 0.5, "y": 0.0}
    else:  # Center
        paste_x = (target_pixel_w - new_w) // 2
        paste_y = (target_pixel_h - new_h) // 2
        pivot = {"x": 0.5, "y": 0.5}
        
    canvas.paste(resized, (paste_x, paste_y), resized)
    
    # Save normalized asset PNG
    out_filename = f"{content_id}.png"
    out_png_path = os.path.join(output_dir, out_filename)
    canvas.save(out_png_path, "PNG")
    norm_hash = compute_sha256(out_png_path)
    
    # Construct spec declaration conforming to Schemas/asset-spec.schema.json v2
    spec = {
        "schemaVersion": 2,
        "status": "normalized-candidate",
        "contentId": content_id,
        "assetType": asset_type,
        "source": {
            "kind": "generator",
            "provenance": "Tools/AssetPipeline/asset_normalizer.py",
            "rawSourceFile": os.path.basename(input_path),
            "rawSourceSha256": raw_hash
        },
        "metrics": {
            "cellWidth": cell_w,
            "cellHeight": cell_h,
            "pixelsPerUnit": ppu,
            "pivot": pivot,
            "pixelBounds": {"width": target_pixel_w, "height": target_pixel_h}
        },
        "behavior": {
            "heightClass": "medium" if cell_h >= 2 else "low",
            "collisionMask": "solid" if asset_type == "prop" else "none",
            "interactionPoints": [],
            "compatibleThemes": ["residential", "office", "commercial"]
        },
        "rendering": {
            "sortingLayer": "Props" if asset_type == "prop" else ("Backdrops" if asset_type == "room_shell" else "Fixtures")
        }
    }
    
    if asset_type == "room_shell":
        spec["rendering"]["nineSliceBorder"] = {"left": 32, "bottom": 32, "right": 32, "top": 32}
        
    out_spec_path = os.path.join(output_dir, f"{content_id}.spec.json")
    with open(out_spec_path, "w") as f:
        json.dump(spec, f, indent=2)
        
    print(f"Normalized '{content_id}' -> {out_png_path} ({target_pixel_w}x{target_pixel_h} px @ {ppu} PPU)")
    return spec

def extract_room_bundle(
    input_path: str,
    output_dir: str,
    theme_id: str,
    ppu: float = 360.0
) -> dict:
    """Decomposes an empty room cutaway into a multi-layer modular assembly (wall tile, portal, trims)."""
    if not os.path.exists(input_path):
        raise FileNotFoundError(f"Input file not found: {input_path}")
        
    bundle_dir = os.path.join(output_dir, theme_id)
    os.makedirs(bundle_dir, exist_ok=True)
    img = Image.open(input_path).convert("RGBA")
    
    w, h = img.size
    cell_pitch_px = int(round(0.5 * ppu))  # 180 px for 0.5m grid cell at 360 PPU
    room_h_px = int(round(1.42 * ppu))     # 511 px for 1.42m height at 360 PPU
    
    # 1. Extract seamless center wall tile (approx middle 40% of the image)
    wall_crop_x1 = int(w * 0.35)
    wall_crop_x2 = int(w * 0.70)
    wall_crop_y1 = int(h * 0.15)
    wall_crop_y2 = int(h * 0.85)
    wall_sample = img.crop((wall_crop_x1, wall_crop_y1, wall_crop_x2, wall_crop_y2))
    wall_tile = wall_sample.resize((cell_pitch_px, room_h_px), Image.Resampling.LANCZOS)
    
    wall_id = f"room.wall.{theme_id}.tile.v1"
    wall_path = os.path.join(bundle_dir, f"{wall_id}.png")
    wall_tile.save(wall_path, "PNG")
    
    wall_spec = {
        "schemaVersion": 2,
        "contentId": wall_id,
        "assetType": "room_shell",
        "theme": theme_id,
        "metrics": {
            "cellWidth": 1,
            "cellHeight": 2,
            "pixelsPerUnit": ppu,
            "pivot": {"x": 0.5, "y": 0.5},
            "pixelBounds": {"width": cell_pitch_px, "height": room_h_px}
        },
        "rendering": {
            "sortingLayer": "Backdrops",
            "drawMode": "Tiled",
            "tilePitchMeters": 0.5
        }
    }
    with open(os.path.join(bundle_dir, f"{wall_id}.spec.json"), "w") as f:
        json.dump(wall_spec, f, indent=2)
        
    # 2. Extract portal frame (left doorway margin)
    portal_crop = img.crop((0, int(h * 0.15), int(w * 0.25), int(h * 0.88)))
    portal_target_w = int(round(0.4 * ppu))
    portal_sprite = portal_crop.resize((portal_target_w, room_h_px), Image.Resampling.LANCZOS)
    portal_id = f"fixture.portal.{theme_id}.left.v1"
    portal_path = os.path.join(bundle_dir, f"{portal_id}.png")
    portal_sprite.save(portal_path, "PNG")
    
    portal_spec = {
        "schemaVersion": 2,
        "contentId": portal_id,
        "assetType": "fixture",
        "theme": theme_id,
        "metrics": {
            "cellWidth": 1,
            "cellHeight": 2,
            "pixelsPerUnit": ppu,
            "pivot": {"x": 0.0, "y": 0.5},
            "pixelBounds": {"width": portal_target_w, "height": room_h_px}
        },
        "rendering": {
            "sortingLayer": "Fixtures"
        }
    }
    with open(os.path.join(bundle_dir, f"{portal_id}.spec.json"), "w") as f:
        json.dump(portal_spec, f, indent=2)
        
    # 3. Extract top ceiling soffit & utility chase
    soffit_crop = img.crop((0, 0, w, int(h * 0.18)))
    soffit_target_h = int(round(0.25 * ppu))
    soffit_sprite = soffit_crop.resize((cell_pitch_px * 2, soffit_target_h), Image.Resampling.LANCZOS)
    soffit_id = f"fixture.soffit.{theme_id}.v1"
    soffit_path = os.path.join(bundle_dir, f"{soffit_id}.png")
    soffit_sprite.save(soffit_path, "PNG")
    
    soffit_spec = {
        "schemaVersion": 2,
        "contentId": soffit_id,
        "assetType": "fixture",
        "theme": theme_id,
        "metrics": {
            "cellWidth": 2,
            "cellHeight": 1,
            "pixelsPerUnit": ppu,
            "pivot": {"x": 0.5, "y": 1.0},
            "pixelBounds": {"width": cell_pitch_px * 2, "height": soffit_target_h}
        },
        "rendering": {
            "sortingLayer": "Fixtures"
        }
    }
    with open(os.path.join(bundle_dir, f"{soffit_id}.spec.json"), "w") as f:
        json.dump(soffit_spec, f, indent=2)

    print(f"Extracted modular theme bundle '{theme_id}': Wall tile ({wall_id}), Portal ({portal_id}), Soffit ({soffit_id}) in {bundle_dir}")
    return {"theme": theme_id, "wall": wall_spec, "portal": portal_spec, "soffit": soffit_spec}

def main():
    parser = argparse.ArgumentParser(description="Normalize 2D game assets for One Roof")
    parser.add_argument("--input", required=True, help="Input source image path")
    parser.add_argument("--output-dir", default="Art/SourceArt/Proposed/Normalized", help="Output directory")
    parser.add_argument("--id", help="Content ID (e.g. prop.furniture.sofa_lounge.v1)")
    parser.add_argument("--type", choices=["prop", "room_shell", "fixture", "npc_component"], default="prop")
    parser.add_argument("--cells-w", type=int, default=1, help="Width in 0.5m grid cells")
    parser.add_argument("--cells-h", type=int, default=1, help="Height in 0.5m grid cells")
    parser.add_argument("--ppu", type=float, default=320.0, help="Pixels Per Unit")
    parser.add_argument("--anchor", choices=["bottom-center", "center"], default="bottom-center")
    parser.add_argument("--bundle", action="store_true", help="Extract multi-layer room bundle (wall tile, portal, trims)")
    parser.add_argument("--theme", default="residential_studio", help="Theme identifier when using --bundle")
    args = parser.parse_args()

    if args.bundle:
        extract_room_bundle(args.input, args.output_dir, args.theme, args.ppu)
    else:
        if not args.id:
            print("Error: --id is required unless --bundle is specified.")
            sys.exit(1)
        normalize_asset(
            input_path=args.input,
            output_dir=args.output_dir,
            content_id=args.id,
            asset_type=args.type,
            cell_w=args.cells_w,
            cell_h=args.cells_h,
            ppu=args.ppu,
            anchor=args.anchor
        )

if __name__ == "__main__":
    main()

