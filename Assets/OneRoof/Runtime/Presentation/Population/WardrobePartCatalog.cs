using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation catalog bridging <see cref="NpcWardrobeVariant"/> part paths with sliced
    /// transparent wardrobe sprites in Resources. Falls back to null (palette swatches) when a
    /// part is unassigned or unimported, keeping headless tests and the procedural rig green.
    /// Slot anchors assume bottom-center pivots at 512 PPU (see resident-wardrobe-parts spec).
    /// </summary>
    public static class WardrobePartCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>();

        /// <summary>Target on-screen size in meters per wardrobe layer for auto-fit.</summary>
        public static Vector2 TargetSize(NpcLayerKind layer)
        {
            switch (layer)
            {
                case NpcLayerKind.Accessory: return new Vector2(0.15f, 0.11f);
                case NpcLayerKind.Face: return new Vector2(0.12f, 0.13f);
                case NpcLayerKind.UpperClothing: return new Vector2(0.20f, 0.20f);
                case NpcLayerKind.LowerClothing: return new Vector2(0.17f, 0.26f);
                case NpcLayerKind.Footwear: return new Vector2(0.20f, 0.09f);
                case NpcLayerKind.CarriedProp: return new Vector2(0.07f, 0.13f);
                default: return new Vector2(0.20f, 0.20f);
            }
        }

        /// <summary>
        /// Bone-local anchor (local to the <see cref="OneRoof.Content.NpcRigDefinition.GetParentBone"/>
        /// parent bone) per layer. All parts use bottom-center pivots at 512 PPU; the anchor is the
        /// world placement the garment needs minus the parent bone's bind height:
        /// footwear pairs sit on the ground (0.0m) under the hip (0.28m), full-length trousers hang
        /// to the ankle while shorts sit at the thigh, shirts span chest-to-hip under the spine
        /// (0.38m), hats ride the crown above the head (0.52m), and the prop hangs off the hand bone.
        /// Photo parts and palette-swatch fallbacks share this table so swapping paths never pops.
        /// </summary>
        public static Vector3 SlotAnchor(NpcLayerKind layer, Sprite part)
        {
            switch (layer)
            {
                case NpcLayerKind.Accessory:
                case NpcLayerKind.Hair:
                    return new Vector3(0f, 0.045f, -0.02f);
                case NpcLayerKind.Face: return new Vector3(0f, -0.005f, -0.02f);
                case NpcLayerKind.Body:
                case NpcLayerKind.UpperClothing: return new Vector3(0f, -0.10f, -0.03f);
                case NpcLayerKind.LowerClothing:
                    // Full-length trousers hang to the ankle; shorts sit at the thigh.
                    var tall = part != null && part.bounds.size.y > 0.30f;
                    return tall ? new Vector3(0f, -0.25f, -0.03f) : new Vector3(0f, -0.13f, -0.03f);
                case NpcLayerKind.Footwear: return new Vector3(0f, -0.28f, -0.04f);
                case NpcLayerKind.CarriedProp: return new Vector3(0.01f, -0.03f, -0.05f);
                default: return new Vector3(0f, -0.10f, -0.03f);
            }
        }

        public static Sprite GetPart(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (SpriteCache.TryGetValue(resourcePath, out var cached) && cached != null)
            {
                return cached;
            }
            var loaded = Resources.Load<Sprite>(resourcePath);
            if (loaded != null)
            {
                SpriteCache[resourcePath] = loaded;
            }
            return loaded;
        }

        public static Sprite GetPart(NpcWardrobeVariant variant, NpcLayerKind layer)
        {
            if (variant == null) return null;
            return GetPart(variant.GetPartPath(layer));
        }

        /// <summary>Uniformly scales a slot so the part fits its target size preserving aspect.</summary>
        public static void FitSlot(Transform slot, Sprite part, NpcLayerKind layer)
        {
            if (slot == null || part == null) return;
            var bounds = part.bounds.size;
            if (bounds.x <= 0f || bounds.y <= 0f) return;
            var target = TargetSize(layer);
            var s = Mathf.Min(target.x / bounds.x, target.y / bounds.y);
            slot.localPosition = SlotAnchor(layer, part);
            slot.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>
        /// Fallback layout for palette-swatch slots (1x1m white sprite): same anchor table and
        /// same final on-screen size as the photo path, so a missing slice never pops.
        /// </summary>
        public static void FitSwatchSlot(Transform slot, NpcLayerKind layer)
        {
            if (slot == null) return;
            var target = TargetSize(layer);
            slot.localPosition = SlotAnchor(layer, null);
            slot.localScale = new Vector3(target.x, target.y, 1f);
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }
    }
}
