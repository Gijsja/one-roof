using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation catalog bridging NpcContentRegistry with Unity sprite assets.
    /// Resolves complete resident sprites from the content registry or generates a
    /// coherent full-body pixel-art fallback. Incompatible wardrobe cutouts are
    /// intentionally kept out of the body render path.
    /// </summary>
    public static class ResidentSpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> AuthoredSpritesByContentId =
            new Dictionary<string, Sprite>();
        private static readonly HashSet<Sprite> OwnedAtlasSprites = new HashSet<Sprite>();
        private static readonly HashSet<Sprite> OwnedFallbackSprites = new HashSet<Sprite>();
        private static bool _authoredAtlasLoaded;

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

            // Prefer the named complete-body sprite from the authored, imported atlas.
            if (!string.IsNullOrEmpty(record.ResourcePath))
            {
                EnsureAuthoredAtlasLoaded(record.ResourcePath);
                if (AuthoredSpritesByContentId.TryGetValue(contentId, out var loaded) && loaded != null)
                {
                    SpriteCache[contentId] = loaded;
                    return loaded;
                }
            }

            // Headless tests and unimported editor passes retain a deterministic fallback.
            var fallback = GetOrCreateFallbackSprite(contentId, StableHash(contentId));
            SpriteCache[contentId] = fallback;
            return fallback;
        }

        public static NpcContentRecord GetRecord(int residentIndex)
        {
            return NpcContentRegistry.GetByIndex(residentIndex);
        }

        public static void ClearCache()
        {
            foreach (var sprite in OwnedFallbackSprites)
            {
                if (sprite == null) continue;
                if (sprite.texture != null)
                {
                    if (UnityEngine.Application.isPlaying) Object.Destroy(sprite.texture);
                    else Object.DestroyImmediate(sprite.texture);
                }
                if (UnityEngine.Application.isPlaying) Object.Destroy(sprite);
                else Object.DestroyImmediate(sprite);
            }
            foreach (var sprite in OwnedAtlasSprites)
            {
                if (sprite == null) continue;
                if (UnityEngine.Application.isPlaying) Object.Destroy(sprite);
                else Object.DestroyImmediate(sprite);
            }
            OwnedFallbackSprites.Clear();
            OwnedAtlasSprites.Clear();
            SpriteCache.Clear();
            AuthoredSpritesByContentId.Clear();
            _authoredAtlasLoaded = false;
        }

        private static void EnsureAuthoredAtlasLoaded(string resourcePath)
        {
            if (_authoredAtlasLoaded) return;
            _authoredAtlasLoaded = true;
            var atlas = Resources.Load<Texture2D>(resourcePath);
            if (atlas == null || atlas.width != 1536 || atlas.height != 512) return;

            var records = NpcContentRegistry.AllRecords;
            for (var i = 0; i < records.Count && i < 6; i++)
            {
                var record = records[i];
                // The candidate sheet recommends 512 PPU but also declares a
                // 0.58m target height for its 512px cells. Derive PPU from the
                // stated world height (about 883) and let the hierarchy fit width.
                var pixelsPerUnit = 512f / record.WorldHeight;
                var sprite = Sprite.Create(atlas, new Rect(i * 256, 0, 256, 512),
                    new Vector2(.5f, 0f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = record.ContentId;
                AuthoredSpritesByContentId[record.ContentId] = sprite;
                OwnedAtlasSprites.Add(sprite);
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }
                return (int)(hash & 0x7fffffff);
            }
        }

        private static Sprite GetOrCreateFallbackSprite(string key, int seed)
        {
            if (SpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;
            const int width = 64;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"ProceduralResidentFallback_{key}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var hue = (Mathf.Abs(seed) % 360) / 360f;
            var primaryColor = Color.HSVToRGB(hue, 0.7f, 0.85f);
            var skinColor = new Color(0.88f, 0.62f, 0.46f);
            var outline = new Color(0.16f, 0.16f, 0.22f);
            var shadow = new Color(0.68f, 0.72f, 0.82f);

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var normY = y / (float)height;
                for (var x = 0; x < width; x++)
                {
                    var normX = (x - width * 0.5f) / (width * 0.5f);
                    var absX = Mathf.Abs(normX);

                    Color c = Color.clear;
                    var head = normY >= .76f && normY <= .94f && absX <= .36f;
                    var hair = head && normY >= .88f;
                    var torso = normY >= .43f && normY < .76f && absX <= .47f;
                    var arm = normY >= .39f && normY < .72f && absX >= .43f && absX <= .78f;
                    var leg = normY >= .12f && normY < .45f &&
                              ((normX >= -.43f && normX <= -.04f) || (normX >= .04f && normX <= .43f));
                    var shoe = normY >= .06f && normY < .15f && absX <= .48f;
                    if (head) c = hair ? outline : skinColor;
                    if (torso) c = normX < -.12f ? primaryColor * .82f : primaryColor;
                    if (arm) c = skinColor;
                    if (leg) c = normX < 0f ? shadow : shadow * .86f;
                    if (shoe) c = outline;
                    if ((head && (normY > .91f || absX > .30f)) || (torso && absX > .40f) ||
                        (arm && (absX > .70f || normY < .43f)) || (leg && normY < .18f))
                        c = outline;

                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Pivot at bottom-center (0.5, 0.0) matching 512 PPU
            var rect = new Rect(0, 0, width, height);
            var pivot = new Vector2(0.5f, 0.0f);
            var sprite = Sprite.Create(texture, rect, pivot, 128f);
            OwnedFallbackSprites.Add(sprite);
            SpriteCache[key] = sprite;
            return sprite;
        }
    }
}
