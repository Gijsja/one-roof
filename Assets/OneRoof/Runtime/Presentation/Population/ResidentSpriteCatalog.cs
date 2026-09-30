using System.Collections.Generic;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Generates neutral silhouettes for picking and distant LOD only. The visible close
    /// resident is the articulated procedural rig. Never loads predefined body artwork.
    /// </summary>
    public static class ResidentSpriteCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetResidentSprite(int residentIndex)
        {
            return GetResidentSprite(NpcContentRegistry.GetByIndex(residentIndex)?.ContentId);
        }

        public static Sprite GetResidentSprite(string contentId)
        {
            var key = string.IsNullOrEmpty(contentId) ? "npc.resident.proxy" : contentId;
            if (SpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;
            const int width = 32, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "ProceduralResidentProxy_" + key, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = Mathf.Abs(x - 15.5f);
                var head = y >= 49 && y <= 61 && dx <= 6f;
                var torso = y >= 25 && y < 49 && dx <= 6f;
                var arms = y >= 25 && y < 47 && dx >= 7f && dx <= 10f;
                var legs = y >= 2 && y < 25 && dx >= 2f && dx <= 6f;
                pixels[y * width + x] = head || torso || arms || legs ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var record = NpcContentRegistry.GetById(key);
            var worldHeight = record != null ? record.WorldHeight : NpcRigDefinition.NominalWorldHeight;
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), height / worldHeight);
            sprite.name = key;
            SpriteCache[key] = sprite;
            return sprite;
        }

        public static NpcContentRecord GetRecord(int residentIndex) => NpcContentRegistry.GetByIndex(residentIndex);

        public static void ClearCache()
        {
            foreach (var sprite in SpriteCache.Values)
            {
                if (sprite == null) continue;
                if (UnityEngine.Application.isPlaying)
                {
                    Object.Destroy(sprite.texture);
                    Object.Destroy(sprite);
                }
                else
                {
                    Object.DestroyImmediate(sprite.texture);
                    Object.DestroyImmediate(sprite);
                }
            }
            SpriteCache.Clear();
        }
    }
}
