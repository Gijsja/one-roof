using System.Collections.Generic;
using UnityEngine;

namespace OneRoof.Presentation.Architecture
{
    /// <summary>
    /// Presentation catalog bridging architectural sprite assets in Resources with room visual components.
    /// Includes procedural fallbacks for headless test environments.
    /// </summary>
    public static class ArchitecturalFixtureCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public const string ResidentialBackdrop = "backdrop_residential_studio";
        public const string OfficeBackdrop = "backdrop_office_acoustic";
        public const string DinerBackdrop = "backdrop_diner_terracotta";
        public const string LobbyBackdrop = "backdrop_lobby_navy";

        public const string ApartmentDoor = "door_apartment_single";
        public const string ElevatorDoor = "door_elevator_double";
        public const string ResidentialWindow = "window_residential_mullion";
        public const string WallSconce = "sconce_amber_wall";

        public static Sprite GetRoomBackdrop(string roomTheme)
        {
            var key = MapThemeToBackdrop(roomTheme);
            return GetOrLoadSprite("Rooms/" + key, key, 360f, new Vector4(32, 32, 32, 32), new Vector2(0.5f, 0.5f));
        }

        public static Sprite GetFixture(string fixtureKey)
        {
            var pivot = (fixtureKey == ApartmentDoor || fixtureKey == ElevatorDoor)
                ? new Vector2(0.5f, 0f)
                : new Vector2(0.5f, 0.5f);

            return GetOrLoadSprite("Architecture/" + fixtureKey, fixtureKey, 320f, Vector4.zero, pivot);
        }

        public static string MapThemeToBackdrop(string roomTheme)
        {
            if (string.IsNullOrEmpty(roomTheme)) return ResidentialBackdrop;
            var lower = roomTheme.ToLowerInvariant();

            if (lower.Contains("office") || lower.Contains("commercial:office")) return OfficeBackdrop;
            if (lower.Contains("diner") || lower.Contains("restaurant")) return DinerBackdrop;
            if (lower.Contains("lobby")) return LobbyBackdrop;
            return ResidentialBackdrop;
        }

        private static Sprite GetOrLoadSprite(string resourcePath, string cacheKey, float ppu, Vector4 border, Vector2 pivot)
        {
            if (SpriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>(resourcePath);
            if (loaded != null)
            {
                SpriteCache[cacheKey] = loaded;
                return loaded;
            }

            var fallback = CreateFallbackSprite(cacheKey, ppu, border, pivot);
            SpriteCache[cacheKey] = fallback;
            return fallback;
        }

        private static Sprite CreateFallbackSprite(string key, float ppu, Vector4 border, Vector2 pivot)
        {
            var color = ColorForKey(key);
            var width = border != Vector4.zero ? 64 : 32;
            var height = border != Vector4.zero ? 64 : 32;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            var rect = new Rect(0, 0, width, height);
            var sprite = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect, border);
            sprite.name = "fallback_" + key;
            return sprite;
        }

        private static Color ColorForKey(string key)
        {
            if (key.Contains("residential")) return new Color(0.20f, 0.26f, 0.36f);
            if (key.Contains("office")) return new Color(0.16f, 0.22f, 0.28f);
            if (key.Contains("diner")) return new Color(0.28f, 0.19f, 0.14f);
            if (key.Contains("lobby")) return new Color(0.11f, 0.15f, 0.22f);
            if (key.Contains("door_apt") || key.Contains("apartment")) return new Color(0.35f, 0.25f, 0.18f);
            if (key.Contains("door_elev") || key.Contains("elevator")) return new Color(0.30f, 0.40f, 0.50f);
            if (key.Contains("window")) return new Color(0.95f, 0.85f, 0.50f);
            if (key.Contains("sconce")) return new Color(0.98f, 0.75f, 0.30f);
            return new Color(0.25f, 0.30f, 0.40f);
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
        }
    }
}
