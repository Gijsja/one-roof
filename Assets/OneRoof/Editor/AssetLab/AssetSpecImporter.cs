using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>
    /// Automatic Unity AssetPostprocessor that configures Sprite TextureImporters
    /// directly from paired .spec.json declarations adhering to Schemas/asset-spec.schema.json.
    /// Guarantees consistent PPU, pivot, and 9-slice borders without manual Inspector tweaking.
    /// </summary>
    public sealed class AssetSpecImporter : AssetPostprocessor
    {
#pragma warning disable CS0649
        [Serializable]
        private class SpecPivot
        {
            public float x = 0.5f;
            public float y = 0.0f;
        }

        [Serializable]
        private class SpecMetrics
        {
            public int cellWidth = 1;
            public int cellHeight = 1;
            public float pixelsPerUnit = 320f;
            public SpecPivot pivot;
        }

        [Serializable]
        private class SpecBorder
        {
            public int left;
            public int bottom;
            public int right;
            public int top;
        }

        [Serializable]
        private class SpecRendering
        {
            public string sortingLayer = "Default";
            public SpecBorder nineSliceBorder;
        }

        [Serializable]
        private class AssetSpecData
        {
            public string assetType;
            public string contentId;
            public SpecMetrics metrics;
            public SpecRendering rendering;
        }
#pragma warning restore CS0649

        private void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                !assetPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Look for matching {name}.spec.json in same folder
            var specPath = Path.ChangeExtension(assetPath, ".spec.json");
            if (!File.Exists(specPath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(specPath);
                var spec = JsonUtility.FromJson<AssetSpecData>(json);
                if (spec == null) return;

                var importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;

                if (spec.metrics != null && spec.metrics.pixelsPerUnit > 0f)
                {
                    importer.spritePixelsPerUnit = spec.metrics.pixelsPerUnit;

                    if (spec.metrics.pivot != null)
                    {
                        importer.spritePivot = new Vector2(spec.metrics.pivot.x, spec.metrics.pivot.y);
                    }
                }

                if (spec.rendering?.nineSliceBorder != null)
                {
                    var b = spec.rendering.nineSliceBorder;
                    importer.spriteBorder = new Vector4(b.left, b.bottom, b.right, b.top);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AssetSpecImporter] Failed to parse spec for {assetPath}: {ex.Message}");
            }
        }
    }
}
