using System;
using System.Collections.Generic;
using OneRoof.Domain.Topology;
using UnityEngine;

namespace OneRoof.Presentation.Architecture
{
    /// <summary>
    /// Presentation catalog managing floor deck visual themes, textures, and procedural
    /// fallbacks for headless testing and runtime customization.
    /// </summary>
    public static class FloorThemeCatalog
    {
        public const string ConcreteSlab = "theme:concrete_slab";
        public const string HardwoodParquet = "theme:hardwood_parquet";
        public const string HighTechGridTile = "theme:tech_grid_tile";
        public const string RetroCheckerboard = "theme:retro_checkerboard";
        public const string ServiceUtilityGrate = "theme:utility_grate";
        public const string LuxuryCarpet = "theme:luxury_carpet";

        private static readonly Dictionary<string, FloorTheme> Themes = new Dictionary<string, FloorTheme>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        static FloorThemeCatalog()
        {
            RegisterDefaultThemes();
        }

        private static void RegisterDefaultThemes()
        {
            // 1. Concrete Slab (Default / Industrial Brutalist)
            RegisterTheme(new FloorTheme(
                ConcreteSlab,
                "Reinforced Concrete Slab",
                surfaceColor: new Color(0.38f, 0.42f, 0.48f),
                coreColor: new Color(0.20f, 0.23f, 0.28f),
                soffitColor: new Color(0.32f, 0.36f, 0.42f),
                thresholdTrimColor: new Color(0.55f, 0.60f, 0.68f),
                exteriorFasciaColor: new Color(0.28f, 0.32f, 0.38f)
            ));

            // 2. Hardwood Parquet (Residential Comfort / Warm Living)
            RegisterTheme(new FloorTheme(
                HardwoodParquet,
                "Herringbone Hardwood Parquet",
                surfaceColor: new Color(0.72f, 0.52f, 0.34f),
                coreColor: new Color(0.24f, 0.18f, 0.14f),
                soffitColor: new Color(0.85f, 0.82f, 0.76f),
                thresholdTrimColor: new Color(0.82f, 0.68f, 0.32f),
                exteriorFasciaColor: new Color(0.35f, 0.28f, 0.22f)
            ));

            // 3. High-Tech Grid Tile (Corporate Tech / Offices / Server Floor)
            RegisterTheme(new FloorTheme(
                HighTechGridTile,
                "Corporate Tech Grid Tile",
                surfaceColor: new Color(0.24f, 0.30f, 0.38f),
                coreColor: new Color(0.14f, 0.17f, 0.22f),
                soffitColor: new Color(0.45f, 0.50f, 0.58f),
                thresholdTrimColor: new Color(0.28f, 0.78f, 0.90f),
                exteriorFasciaColor: new Color(0.18f, 0.22f, 0.28f)
            ));

            // 4. Retro Checkerboard (Commercial / Diner / Retail)
            RegisterTheme(new FloorTheme(
                RetroCheckerboard,
                "Retro Diner Checkerboard",
                surfaceColor: new Color(0.82f, 0.25f, 0.22f),
                coreColor: new Color(0.18f, 0.16f, 0.18f),
                soffitColor: new Color(0.88f, 0.84f, 0.75f),
                thresholdTrimColor: new Color(0.78f, 0.82f, 0.86f),
                exteriorFasciaColor: new Color(0.25f, 0.22f, 0.24f)
            ));

            // 5. Service Utility Grate (Maintenance / Infrastructure / Wet Utility)
            RegisterTheme(new FloorTheme(
                ServiceUtilityGrate,
                "Industrial Steel Grate",
                surfaceColor: new Color(0.42f, 0.46f, 0.50f),
                coreColor: new Color(0.12f, 0.14f, 0.16f),
                soffitColor: new Color(0.22f, 0.24f, 0.28f),
                thresholdTrimColor: new Color(0.92f, 0.75f, 0.18f), // High-vis hazard trim
                exteriorFasciaColor: new Color(0.15f, 0.18f, 0.20f)
            ));

            // 6. Luxury Carpet (Penthouse / Hospitality Lounge)
            RegisterTheme(new FloorTheme(
                LuxuryCarpet,
                "Executive Velvet Carpet",
                surfaceColor: new Color(0.48f, 0.20f, 0.26f),
                coreColor: new Color(0.16f, 0.14f, 0.18f),
                soffitColor: new Color(0.90f, 0.88f, 0.84f),
                thresholdTrimColor: new Color(0.88f, 0.74f, 0.35f),
                exteriorFasciaColor: new Color(0.28f, 0.20f, 0.24f)
            ));
        }

        public static void RegisterTheme(FloorTheme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            Themes[theme.Id] = theme;
        }

        public static FloorTheme GetTheme(string themeId)
        {
            if (!string.IsNullOrEmpty(themeId) && Themes.TryGetValue(themeId, out var theme))
            {
                return theme;
            }

            return Themes[ConcreteSlab];
        }

        public static bool TryGetTheme(string themeId, out FloorTheme theme)
        {
            if (!string.IsNullOrEmpty(themeId))
            {
                return Themes.TryGetValue(themeId, out theme);
            }

            theme = null;
            return false;
        }

        public static string MapContentTypeToTheme(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return ConcreteSlab;
            var lower = contentType.ToLowerInvariant();

            if (lower.Contains("residential") || lower.Contains("apartment") || lower.Contains("condo"))
                return HardwoodParquet;

            if (lower.Contains("office") || lower.Contains("commercial:office") || lower.Contains("security") || lower.Contains("clinic"))
                return HighTechGridTile;

            if (lower.Contains("diner") || lower.Contains("restaurant") || lower.Contains("retail") || lower.Contains("boutique"))
                return RetroCheckerboard;

            if (lower.Contains("utility") || lower.Contains("maintenance") || lower.Contains("transformer") || lower.Contains("pump") || lower.Contains("waste"))
                return ServiceUtilityGrate;

            if (lower.Contains("penthouse") || lower.Contains("lounge") || lower.Contains("hotel"))
                return LuxuryCarpet;

            return ConcreteSlab;
        }

        public static string ResolveDominantTheme(IEnumerable<Room> rooms)
        {
            if (rooms == null) return ConcreteSlab;

            var votes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var totalRooms = 0;

            foreach (var room in rooms)
            {
                if (room == null) continue;
                var contentType = room.ContentType.Value ?? "";
                if (contentType.Contains("elevator_shaft") || contentType.Contains("stairwell"))
                    continue;

                var theme = MapContentTypeToTheme(contentType);
                votes.TryGetValue(theme, out var count);
                votes[theme] = count + 1;
                totalRooms++;
            }

            if (totalRooms == 0) return ConcreteSlab;

            string bestTheme = ConcreteSlab;
            var maxCount = -1;
            foreach (var kvp in votes)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    bestTheme = kvp.Key;
                }
            }

            return bestTheme;
        }

        public static Sprite GetOrLoadSurfaceSprite(FloorTheme theme)
        {
            if (theme == null) return null;
            var key = theme.SurfaceSpriteKey ?? theme.Id;

            if (SpriteCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var resourcePath = "Floors/" + key;
            var loaded = Resources.Load<Sprite>(resourcePath);
            if (loaded != null)
            {
                SpriteCache[key] = loaded;
                return loaded;
            }

            var fallback = CreateProceduralSurfaceSprite(theme);
            SpriteCache[key] = fallback;
            return fallback;
        }

        public static Sprite CreateProceduralSurfaceSprite(FloorTheme theme)
        {
            var tex = CreateProceduralTexture(theme);
            var rect = new Rect(0, 0, tex.width, tex.height);
            var border = new Vector4(4, 4, 4, 4);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 320f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "fallback_floordeck_" + theme.Id;
            return sprite;
        }

        private static Texture2D CreateProceduralTexture(FloorTheme theme)
        {
            const int width = 32;
            const int height = 16;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[width * height];
            var baseColor = theme.SurfaceColor;

            var id = theme.Id;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    var c = baseColor;

                    if (id.Equals(HardwoodParquet, StringComparison.OrdinalIgnoreCase))
                    {
                        // Alternating parquet planks
                        var plank = (x / 8) + (y / 4);
                        var shade = (plank % 2 == 0) ? 1.05f : 0.92f;
                        if (x % 8 == 0 || y % 4 == 0) shade *= 0.85f; // Grout line
                        c = new Color(baseColor.r * shade, baseColor.g * shade, baseColor.b * shade, 1f);
                    }
                    else if (id.Equals(RetroCheckerboard, StringComparison.OrdinalIgnoreCase))
                    {
                        // Checker pattern
                        var cell = (x / 4) + (y / 4);
                        if (cell % 2 == 0)
                        {
                            c = new Color(0.92f, 0.90f, 0.85f); // Cream tile
                        }
                        else
                        {
                            c = baseColor; // Tinted tile (crimson or black)
                        }
                        if (x % 4 == 0 || y % 4 == 0) c *= 0.80f; // Grout line
                    }
                    else if (id.Equals(HighTechGridTile, StringComparison.OrdinalIgnoreCase))
                    {
                        // Tech grid with accent lines
                        var isGridEdge = (x % 8 == 0) || (y % 8 == 0);
                        if (isGridEdge)
                        {
                            c = theme.ThresholdTrimColor * 0.9f;
                        }
                        else
                        {
                            var subtleNoise = 0.97f + ((x * 7 + y * 13) % 7) * 0.01f;
                            c = baseColor * subtleNoise;
                        }
                    }
                    else if (id.Equals(ServiceUtilityGrate, StringComparison.OrdinalIgnoreCase))
                    {
                        // Industrial mesh diamond or perforated grating
                        var isHole = (x % 4 == 0 && y % 4 == 0) || (x % 4 == 2 && y % 4 == 2);
                        if (isHole)
                        {
                            c = theme.CoreColor * 0.7f;
                        }
                        else
                        {
                            var highlight = (x % 2 == 0) ? 1.08f : 0.92f;
                            c = baseColor * highlight;
                        }
                    }
                    else
                    {
                        // Concrete Slab / default: fine texture variations and joints
                        var isJoint = (x == 0 || x == 16);
                        if (isJoint)
                        {
                            c = baseColor * 0.82f;
                        }
                        else
                        {
                            var noise = 0.95f + ((x * 17 + y * 29) % 11) * 0.01f;
                            c = baseColor * noise;
                        }
                    }

                    pixels[idx] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
            Themes.Clear();
            RegisterDefaultThemes();
        }
    }
}
