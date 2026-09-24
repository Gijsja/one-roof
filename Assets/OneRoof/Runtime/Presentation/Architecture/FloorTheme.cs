using System;
using UnityEngine;

namespace OneRoof.Presentation.Architecture
{
    /// <summary>
    /// Visual specification for a physical floor deck, defining walking surface,
    /// structural core, ceiling soffit, and trim appearances.
    /// </summary>
    [Serializable]
    public sealed class FloorTheme
    {
        public const float DefaultTreadThickness = 0.05f;
        public const float DefaultCoreThickness = 0.17f;
        public const float DefaultSoffitThickness = 0.05f;
        public const float StandardDeckThickness = DefaultTreadThickness + DefaultCoreThickness + DefaultSoffitThickness; // 0.27f

        public string Id { get; }
        public string DisplayName { get; }
        public Color SurfaceColor { get; }
        public Color CoreColor { get; }
        public Color SoffitColor { get; }
        public Color ThresholdTrimColor { get; }
        public Color ExteriorFasciaColor { get; }
        public string SurfaceSpriteKey { get; }
        public float TreadThickness { get; }
        public float CoreThickness { get; }
        public float SoffitThickness { get; }

        public float TotalThickness => TreadThickness + CoreThickness + SoffitThickness;

        public FloorTheme(
            string id,
            string displayName,
            Color surfaceColor,
            Color coreColor,
            Color soffitColor,
            Color thresholdTrimColor,
            Color exteriorFasciaColor,
            string surfaceSpriteKey = null,
            float treadThickness = DefaultTreadThickness,
            float coreThickness = DefaultCoreThickness,
            float soffitThickness = DefaultSoffitThickness)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            SurfaceColor = surfaceColor;
            CoreColor = coreColor;
            SoffitColor = soffitColor;
            ThresholdTrimColor = thresholdTrimColor;
            ExteriorFasciaColor = exteriorFasciaColor;
            SurfaceSpriteKey = surfaceSpriteKey ?? id;
            TreadThickness = Mathf.Max(0.01f, treadThickness);
            CoreThickness = Mathf.Max(0.01f, coreThickness);
            SoffitThickness = Mathf.Max(0.01f, soffitThickness);
        }

        public override string ToString() => $"FloorTheme({Id}, {DisplayName})";
    }
}
