using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Authored content record describing an individual emote balloon animation.
    /// Follows the Docs/03_DATA_CONTRACTS.md and Docs/05_ASSET_PIPELINE.md immutable schema.
    /// </summary>
    public sealed class EmoteContentRecord
    {
        public NpcEmoteKind EmoteKind { get; }
        public string ContentId { get; }
        public string ProposedKey { get; }
        public string DisplayName { get; }
        public string ResourcePath { get; }
        public string Category { get; }
        public int FramedRow { get; }
        public int UnframedRow { get; }
        public IReadOnlyList<int> Columns { get; }
        public int FrameCount { get; }
        public float FrameDuration { get; }

        public EmoteContentRecord(
            NpcEmoteKind emoteKind,
            string contentId,
            string proposedKey,
            string displayName,
            string resourcePath,
            string category,
            int framedRow,
            int unframedRow,
            IEnumerable<int> columns,
            int frameCount = 3,
            float frameDuration = 0.15f)
        {
            EmoteKind = emoteKind;
            ContentId = contentId ?? throw new ArgumentNullException(nameof(contentId));
            ProposedKey = proposedKey ?? throw new ArgumentNullException(nameof(proposedKey));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ResourcePath = resourcePath ?? throw new ArgumentNullException(nameof(resourcePath));
            Category = category ?? "General";
            FramedRow = framedRow;
            UnframedRow = unframedRow;
            Columns = new ReadOnlyCollection<int>(new List<int>(columns ?? new[] { 0, 1, 2 }));
            FrameCount = frameCount > 0 ? frameCount : 3;
            FrameDuration = frameDuration > 0f ? frameDuration : 0.15f;
        }
    }
}
