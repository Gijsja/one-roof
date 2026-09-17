using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation catalog bridging EmoteContentRegistry with sliced sprite assets.
    /// Loads runtime emote sprites from Resources with procedural fallbacks for headless test runners.
    /// </summary>
    public static class EmoteSpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new Dictionary<string, Sprite>();

        public static Sprite GetSprite(NpcEmoteKind kind, int frameIndex = 0)
        {
            if (kind == NpcEmoteKind.None)
            {
                return null;
            }

            var record = EmoteContentRegistry.GetByKind(kind);
            if (record == null)
            {
                return GetOrCreateFallbackSprite(kind.ToString(), frameIndex);
            }

            frameIndex = Mathf.Clamp(frameIndex, 0, record.FrameCount - 1);
            var cacheKey = $"{record.ProposedKey}_{frameIndex}";

            if (SpriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            // 1. Attempt loading from Resources (individual frame e.g. "Emotes/emote_ellipsis_wait_0")
            var resourceName = $"{record.ResourcePath}_{frameIndex}";
            var loaded = Resources.Load<Sprite>(resourceName);
            if (loaded != null)
            {
                SpriteCache[cacheKey] = loaded;
                return loaded;
            }

            // 2. Fallback procedural sprite for headless tests
            var fallback = GetOrCreateFallbackSprite(record.ProposedKey, frameIndex);
            SpriteCache[cacheKey] = fallback;
            return fallback;
        }

        public static EmoteContentRecord GetRecord(NpcEmoteKind kind)
        {
            return EmoteContentRegistry.GetByKind(kind);
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }

        private static Sprite GetOrCreateFallbackSprite(string key, int frameIndex)
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"ProceduralEmoteFallback_{key}_{frameIndex}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[size * size];
            var hash = Mathf.Abs((key + frameIndex).GetHashCode());
            var bubbleColor = Color.white;
            var innerColor = Color.HSVToRGB((hash % 360) / 360f, 0.8f, 0.9f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = (x == 1 || x == size - 2 || y == 2 || y == size - 2);
                    var isInside = (x > 1 && x < size - 2 && y > 2 && y < size - 2);
                    if (isBorder)
                    {
                        colors[y * size + x] = bubbleColor;
                    }
                    else if (isInside)
                    {
                        colors[y * size + x] = innerColor;
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), 32);
        }
    }
}
