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
                default: return new Vector2(0.20f, 0.20f);
            }
        }

        /// <summary>Bottom-center anchor (local to the slot parent bone) per layer.</summary>
        public static Vector3 SlotAnchor(NpcLayerKind layer, Sprite part)
        {
            switch (layer)
            {
                case NpcLayerKind.Accessory: return new Vector3(0f, 0.16f, -0.05f);
                case NpcLayerKind.Face: return new Vector3(0f, -0.005f, -0.02f);
                case NpcLayerKind.UpperClothing: return new Vector3(0f, -0.08f, -0.03f);
                case NpcLayerKind.LowerClothing:
                    // Full-length trousers hang to the ankle; shorts sit at the hip.
                    var tall = part != null && part.bounds.size.y > 0.30f;
                    return tall ? new Vector3(0f, -0.35f, -0.03f) : new Vector3(0f, -0.09f, -0.03f);
                case NpcLayerKind.Footwear: return new Vector3(0f, -0.35f, -0.04f);
                default: return new Vector3(0f, -0.035f, -0.03f);
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

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }
    }
}
