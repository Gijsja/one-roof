using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>Immutable, eight-layer wardrobe selection for the shared resident rig.</summary>
    public sealed class NpcWardrobeLoadout
    {
        private readonly IReadOnlyDictionary<NpcLayerKind, string> _layers;

        public NpcWardrobeLoadout(string rigId, IDictionary<NpcLayerKind, string> layers)
        {
            RigId = string.IsNullOrWhiteSpace(rigId) ? throw new ArgumentException("A compatible rig ID is required.", nameof(rigId)) : rigId;
            var resolved = new Dictionary<NpcLayerKind, string>();
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                resolved[layer] = layers != null && layers.TryGetValue(layer, out var item) && !string.IsNullOrWhiteSpace(item)
                    ? item
                    : $"npc.wardrobe.{layer.ToString().ToLowerInvariant()}.default.v1";
            }
            _layers = new ReadOnlyDictionary<NpcLayerKind, string>(resolved);
        }

        public string RigId { get; }
        public IReadOnlyDictionary<NpcLayerKind, string> Layers => _layers;
        public string GetLayerId(NpcLayerKind layer) => _layers[layer];

        public static NpcWardrobeLoadout FromRecord(NpcContentRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var layers = new Dictionary<NpcLayerKind, string>();
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                var description = record.LayerDescriptions.TryGetValue(layer, out var value) ? value : "default";
                layers[layer] = $"npc.wardrobe.{record.ProposedKey}.{layer.ToString().ToLowerInvariant()}.{StableToken(description)}.v1";
            }
            return new NpcWardrobeLoadout(NpcRigDefinition.RigId, layers);
        }

        private static string StableToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "default";
            var hash = 17;
            foreach (var character in value) hash = hash * 31 + character;
            return Math.Abs(hash).ToString("x");
        }
    }
}
