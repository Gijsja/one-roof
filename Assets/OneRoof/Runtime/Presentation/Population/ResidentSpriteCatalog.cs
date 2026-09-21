using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation catalog bridging NpcContentRegistry with Unity sprite assets.
    /// Composite placeholder sprites were removed; the Spine modular rig in
    /// NpcSkeletalHierarchy is canonical. Until layered wardrobe part sprites land,
    /// records carry an empty ResourcePath and this catalog resolves the procedural
    /// bottom-center-pivot fallback so headless tests and the disabled legacy
    /// MainRenderer slot stay non-null.
    /// </summary>
    public static class ResidentSpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>();

        public static Sprite GetResidentSprite(int residentIndex)
        {
            var record = NpcContentRegistry.GetByIndex(residentIndex);
            if (record == null)
            {
                return GetOrCreateFallbackSprite("npc.resident.fallback", 0);
            }

            return GetResidentSprite(record.ContentId);
        }

        public static Sprite GetResidentSprite(string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
            {
                return GetOrCreateFallbackSprite("npc.resident.fallback", 0);
            }

            if (SpriteCache.TryGetValue(contentId, out var cached) && cached != null)
            {
                return cached;
            }

            var record = NpcContentRegistry.GetById(contentId);
            if (record == null)
            {
                return GetOrCreateFallbackSprite(contentId, 0);
            }

            // 1. Attempt loading from Unity Resources (skipped when the record
            // carries no composite path; Spine modular rig is canonical).
            if (!string.IsNullOrEmpty(record.ResourcePath))
            {
                var loaded = Resources.Load<Sprite>(record.ResourcePath);
                if (loaded != null)
                {
                    SpriteCache[contentId] = loaded;
                    return loaded;
                }
            }

            // 2. Procedural fallback for headless tests or unimported editor passes
            var fallback = GetOrCreateFallbackSprite(contentId, record.ContentId.GetHashCode());
            SpriteCache[contentId] = fallback;
            return fallback;
        }

        public static NpcContentRecord GetRecord(int residentIndex)
        {
            return NpcContentRegistry.GetByIndex(residentIndex);
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }

        private static Sprite GetOrCreateFallbackSprite(string key, int seed)
        {
            const int width = 64;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"ProceduralResidentFallback_{key}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var hue = (Mathf.Abs(seed) % 360) / 360f;
            var primaryColor = Color.HSVToRGB(hue, 0.7f, 0.85f);
            var skinColor = new Color(0.95f, 0.82f, 0.72f);
            var darkColor = new Color(0.18f, 0.22f, 0.28f);

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var normY = y / (float)height;
                for (var x = 0; x < width; x++)
                {
                    var normX = (x - width * 0.5f) / (width * 0.5f);
                    var absX = Mathf.Abs(normX);

                    Color c = Color.clear;
                    // Silhouette head (normY ~ 0.75..0.95)
                    if (normY >= 0.75f && normY <= 0.95f && absX <= 0.45f)
                    {
                        c = normY > 0.88f ? darkColor : skinColor;
                    }
                    // Torso (normY ~ 0.40..0.75)
                    else if (normY >= 0.40f && normY < 0.75f && absX <= 0.65f)
                    {
                        c = primaryColor;
                    }
                    // Legs / footwear (normY ~ 0.05..0.40)
                    else if (normY >= 0.05f && normY < 0.40f && absX <= 0.50f)
                    {
                        c = normY < 0.15f ? darkColor : primaryColor * 0.8f;
                    }

                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Pivot at bottom-center (0.5, 0.0) matching 512 PPU
            var rect = new Rect(0, 0, width, height);
            var pivot = new Vector2(0.5f, 0.0f);
            return Sprite.Create(texture, rect, pivot, 128f);
        }
    }
}
