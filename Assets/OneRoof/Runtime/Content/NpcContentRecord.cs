using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Immutable authored content definition for a validated NPC resident archetype.
    /// Complies with Docs/03_DATA_CONTRACTS.md (ContentId) and Docs/05_ASSET_PIPELINE.md.
    /// </summary>
    public sealed class NpcContentRecord
    {
        public string ContentId { get; }
        public string ProposedKey { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public string PrimaryRole { get; }
        public string BodyType { get; }
        public float WorldWidth { get; }
        public float WorldHeight { get; }
        public IReadOnlyList<string> InteractionPoints { get; }
        public IReadOnlyDictionary<NpcLayerKind, string> LayerDescriptions { get; }

        public NpcContentRecord(
            string contentId,
            string proposedKey,
            string displayName,
            string resourcePath,
            string primaryRole,
            string bodyType,
            float worldWidth,
            float worldHeight,
            IEnumerable<string> interactionPoints,
            IDictionary<NpcLayerKind, string> layerDescriptions)
        {
            ContentId = contentId ?? throw new ArgumentNullException(nameof(contentId));
            ProposedKey = proposedKey ?? throw new ArgumentNullException(nameof(proposedKey));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ResourcePath = resourcePath ?? throw new ArgumentNullException(nameof(resourcePath));
            PrimaryRole = primaryRole ?? "Resident";
            BodyType = bodyType ?? "adult-standard";
            WorldWidth = worldWidth > 0f ? worldWidth : NpcRigDefinition.NominalWorldWidth;
            WorldHeight = worldHeight > 0f ? worldHeight : NpcRigDefinition.NominalWorldHeight;
            InteractionPoints = new ReadOnlyCollection<string>(new List<string>(interactionPoints ?? Array.Empty<string>()));
            LayerDescriptions = new ReadOnlyDictionary<NpcLayerKind, string>(new Dictionary<NpcLayerKind, string>(layerDescriptions ?? new Dictionary<NpcLayerKind, string>()));
        }
    }
}
