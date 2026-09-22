using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Furnishings
{
    /// <summary>
    /// Presentation catalog bridging PropContentRegistry with Unity prop sprite assets.
    /// Includes procedural fallback sprites for headless test runners.
    /// </summary>
    public static class PropCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetPropSprite(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;

            if (SpriteCache.TryGetValue(contentId, out var cached) && cached != null)
            {
                return cached;
            }

            var record = PropContentRegistry.GetById(contentId);
            if (record != null)
            {
                var loaded = Resources.Load<Sprite>(record.ResourcePath);
                if (loaded != null)
                {
                    SpriteCache[contentId] = loaded;
                    return loaded;
                }
            }

            var fallback = CreateFallbackSprite(contentId);
            SpriteCache[contentId] = fallback;
            return fallback;
        }

        private static Sprite CreateFallbackSprite(string contentId)
        {
            var color = ColorForProp(contentId);
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var rect = new Rect(0, 0, 32, 32);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.0f), 320f);
            sprite.name = "fallback_" + contentId;
            return sprite;
        }

        private static Color ColorForProp(string contentId)
        {
            if (contentId.Contains("bed")) return new Color(0.35f, 0.45f, 0.65f);
            if (contentId.Contains("sofa")) return new Color(0.48f, 0.38f, 0.30f);
            if (contentId.Contains("desk")) return new Color(0.38f, 0.44f, 0.52f);
            if (contentId.Contains("chair")) return new Color(0.28f, 0.32f, 0.38f);
            if (contentId.Contains("booth")) return new Color(0.68f, 0.35f, 0.22f);
            if (contentId.Contains("counter")) return new Color(0.58f, 0.40f, 0.28f);
            if (contentId.Contains("planter")) return new Color(0.25f, 0.55f, 0.35f);
            if (contentId.Contains("kitchenette")) return new Color(0.45f, 0.48f, 0.52f);
            if (contentId.Contains("retail") || contentId.Contains("checkout") || contentId.Contains("rack") || contentId.Contains("shelf")) return new Color(0.55f, 0.42f, 0.28f);
            if (contentId.Contains("exam") || contentId.Contains("clinic") || contentId.Contains("pharma") || contentId.Contains("screen")) return new Color(0.55f, 0.65f, 0.62f);
            if (contentId.Contains("bench") || contentId.Contains("maint") || contentId.Contains("toolcabinet") || contentId.Contains("partsshelf")) return new Color(0.42f, 0.32f, 0.22f);
            if (contentId.Contains("security") || contentId.Contains("monitor") || contentId.Contains("locker")) return new Color(0.25f, 0.32f, 0.48f);
            if (contentId.Contains("utility") || contentId.Contains("substation") || contentId.Contains("pump") || contentId.Contains("hopper") || contentId.Contains("pipechase")) return new Color(0.35f, 0.42f, 0.48f);
            return new Color(0.35f, 0.40f, 0.45f);
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }
    }
}
