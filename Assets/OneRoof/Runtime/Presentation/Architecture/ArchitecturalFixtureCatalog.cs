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
        public const string RetailBackdrop = "backdrop_retail_boutique";
        public const string ClinicBackdrop = "backdrop_clinic_sterile";
        public const string MaintenanceBackdrop = "backdrop_maintenance_workshop";
        public const string SecurityBackdrop = "backdrop_security_station";
        public const string UtilityIndustrialBackdrop = "backdrop_utility_industrial";
        public const string UtilityWetBackdrop = "backdrop_utility_wet";
        public const string StairwellBackdrop = "backdrop_stairwell_concrete";

        public const string ApartmentDoor = "door_apartment_single";
        public const string ElevatorDoor = "door_elevator_double";
        public const string ServiceDoor = "door_service_single";
        public const string UtilityRollerDoor = "door_utility_roller";
        public const string ResidentialWindow = "window_residential_mullion";
        public const string StorefrontWindow = "window_storefront_glass";
        public const string ExitSign = "sign_exit_service";
        public const string StairFlight = "stair_flight_dressing";
        public const string WallSconce = "sconce_amber_wall";

        public static Sprite GetRoomBackdrop(string roomTheme)
        {
            var key = MapThemeToBackdrop(roomTheme);
            return GetOrLoadSprite("Rooms/" + key, key, 360f, new Vector4(32, 32, 32, 32), new Vector2(0.5f, 0.5f));
        }

        public static Sprite GetFixture(string fixtureKey)
        {
            var pivot = (fixtureKey == ApartmentDoor || fixtureKey == ElevatorDoor ||
                         fixtureKey == ServiceDoor || fixtureKey == UtilityRollerDoor)
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
            if (lower.Contains("retail")) return RetailBackdrop;
            if (lower.Contains("clinic")) return ClinicBackdrop;
            if (lower.Contains("maintenance")) return MaintenanceBackdrop;
            if (lower.Contains("security")) return SecurityBackdrop;
            if (lower.Contains("stairwell")) return StairwellBackdrop;
            if (lower.Contains("electrical") || lower.Contains("transformer")) return UtilityIndustrialBackdrop;
            if (lower.Contains("utility") || lower.Contains("water") || lower.Contains("waste")) return UtilityWetBackdrop;
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
            if (key.Contains("retail")) return new Color(0.32f, 0.26f, 0.18f);
            if (key.Contains("clinic")) return new Color(0.45f, 0.55f, 0.52f);
            if (key.Contains("maintenance")) return new Color(0.22f, 0.25f, 0.30f);
            if (key.Contains("security")) return new Color(0.14f, 0.18f, 0.28f);
            if (key.Contains("utility") || key.Contains("electrical") || key.Contains("water") || key.Contains("waste")) return new Color(0.20f, 0.24f, 0.28f);
            if (key.Contains("stairwell")) return new Color(0.30f, 0.33f, 0.38f);
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
