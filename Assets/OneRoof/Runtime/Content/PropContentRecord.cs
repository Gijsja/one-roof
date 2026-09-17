using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Authored immutable content definition for validated environment props and furniture.
    /// Declares identity, footprint dimensions, collision, and interaction anchors.
    /// </summary>
    public sealed class PropContentRecord
    {
        public PropContentRecord(
            string contentId,
            string displayName,
            string resourcePath,
            int cellWidth,
            int cellHeight,
            string heightClass,
            string collisionMask,
            IReadOnlyList<string> interactionPoints,
            IReadOnlyList<string> compatibleThemes)
        {
            ContentId = contentId ?? throw new ArgumentNullException(nameof(contentId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ResourcePath = resourcePath ?? throw new ArgumentNullException(nameof(resourcePath));
            CellWidth = Math.Max(1, cellWidth);
            CellHeight = Math.Max(1, cellHeight);
            HeightClass = heightClass ?? "medium";
            CollisionMask = collisionMask ?? "solid";
            InteractionPoints = new ReadOnlyCollection<string>(new List<string>(interactionPoints ?? Array.Empty<string>()));
            CompatibleThemes = new ReadOnlyCollection<string>(new List<string>(compatibleThemes ?? Array.Empty<string>()));
        }

        public string ContentId { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public int CellWidth { get; }
        public int CellHeight { get; }
        public string HeightClass { get; }
        public string CollisionMask { get; }
        public IReadOnlyList<string> InteractionPoints { get; }
        public IReadOnlyList<string> CompatibleThemes { get; }

        public bool SupportsTheme(string theme)
        {
            if (string.IsNullOrEmpty(theme)) return false;
            for (var i = 0; i < CompatibleThemes.Count; i++)
            {
                if (theme.IndexOf(CompatibleThemes[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString() => $"PropContentRecord [{ContentId}] '{DisplayName}' ({CellWidth}x{CellHeight})";
    }
}
