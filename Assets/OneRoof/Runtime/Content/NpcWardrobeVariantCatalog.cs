using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Deterministic wardrobe-variant palettes sampled from the modular profession
    /// sheets (Art/SourceArt/Proposed/resident-wardrobe-modular-v1.spec.json:
    /// frontview, frontview1, sideview, sideview1, backview1).
    ///
    /// The raw sheets are opaque-gradient paperdoll reference, not transparent game
    /// sprites, so they cannot be sliced onto the rig directly. This catalog captures
    /// their diversity value (12 profession columns x 5 skin tones x scale jitter) as
    /// pure data driving the shared Spine 8-layer compositor in presentation.
    /// Resident content IDs stay immutable; variety ships as variants, never new IDs.
    /// </summary>
    public sealed class NpcWardrobeVariant
    {
        public string Key { get; }
        public string DisplayName { get; }
        public string Profession { get; }
        public string SourceView { get; }
        public float BodyScale { get; }
        public IReadOnlyDictionary<NpcLayerKind, string> LayerColors { get; }

        public NpcWardrobeVariant(
            string key,
            string displayName,
            string profession,
            string sourceView,
            float bodyScale,
            IDictionary<NpcLayerKind, string> layerColors)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Profession = profession ?? "Resident";
            SourceView = sourceView ?? "front";
            BodyScale = bodyScale > 0f ? bodyScale : 1f;
            LayerColors = new ReadOnlyDictionary<NpcLayerKind, string>(
                new Dictionary<NpcLayerKind, string>(layerColors ?? new Dictionary<NpcLayerKind, string>()));
        }

        public string GetLayerColor(NpcLayerKind layer)
        {
            return LayerColors.TryGetValue(layer, out var hex) && !string.IsNullOrWhiteSpace(hex)
                ? hex
                : "#B8B8B8";
        }
    }

    public static class NpcWardrobeVariantCatalog
    {
        private static readonly List<NpcWardrobeVariant> Variants = new List<NpcWardrobeVariant>
        {
            // Skin tones cycle across 5 reference complexions; scale jitters +/-5%.
            Create("npc.wardrobe.variant.firefighter.v1", "Firefighter Turnout", "Firefighter", "front",
                1.03f, "#8A4A32", "#2A2A2E", "#C8321E", "#B02020", "#2E2E34", "#E8C33C"),
            Create("npc.wardrobe.variant.construction.v1", "Construction Hi-Vis", "Construction", "side",
                1.01f, "#6B4230", "#1E1E22", "#E86A1C", "#2A3440", "#1A1A1E", "#E8E8E8"),
            Create("npc.wardrobe.variant.chef.v1", "Chef Whites", "Chef", "front",
                0.99f, "#C68863", "#1A1A1A", "#F2F2F0", "#F2F2F0", "#1C1C20", "#F5F5F5"),
            Create("npc.wardrobe.variant.medic.v1", "Medic Service Red", "Medic", "side",
                0.98f, "#A06A4A", "#141416", "#C02028", "#A01820", "#222228", "#D8D8D8"),
            Create("npc.wardrobe.variant.security.v1", "Security Tactical", "Security", "back",
                1.04f, "#77503A", "#0E0E10", "#23262E", "#1B1E26", "#101114", "#3A3A40"),
            Create("npc.wardrobe.variant.pilot.v1", "Pilot Whites", "Pilot", "front",
                1.00f, "#8A5A3C", "#26221E", "#EDEEF2", "#E8E9EE", "#1A1A20", "#2A3A5C"),
            Create("npc.wardrobe.variant.corporate.v1", "Corporate Suit", "Corporate", "side",
                1.02f, "#C89878", "#2E2A26", "#33363E", "#2E3138", "#141518", "#2E4A7A"),
            Create("npc.wardrobe.variant.police.v1", "Police Navy", "Police", "front",
                1.00f, "#7A4E34", "#101418", "#2A3E5C", "#232E48", "#101418", "#C8A828"),
            Create("npc.wardrobe.variant.service.v1", "Service Whites", "Service", "front",
                0.97f, "#B87A56", "#3A2E24", "#F0EDE8", "#3A3A42", "#1E1E22", "#1A1A1A"),
            Create("npc.wardrobe.variant.maintenance.v1", "Maintenance Purple", "Maintenance", "side",
                0.96f, "#6E4530", "#1C1C22", "#6A3AA0", "#3A3F52", "#2A2A32", "#D8B83C"),
            Create("npc.wardrobe.variant.astronaut.v1", "Astronaut Suit", "Astronaut", "side",
                1.05f, "#96704E", "#20242A", "#D8DCE2", "#C8CCD4", "#E8E8EC", "#3A6AC8"),
            Create("npc.wardrobe.variant.casual.v1", "Casual Off-Duty", "Casual", "front",
                0.95f, "#84603F", "#241E18", "#4A5568", "#C02828", "#2A2E36", "#808080"),
        };

        private static readonly Dictionary<string, NpcWardrobeVariant> ByKey =
            new Dictionary<string, NpcWardrobeVariant>(StringComparer.Ordinal);

        static NpcWardrobeVariantCatalog()
        {
            foreach (var variant in Variants)
            {
                ByKey[variant.Key] = variant;
            }
        }

        private static NpcWardrobeVariant Create(
            string key,
            string displayName,
            string profession,
            string sourceView,
            float bodyScale,
            string skinHex,
            string hairHex,
            string upperHex,
            string lowerHex,
            string footwearHex,
            string accessoryHex)
        {
            return new NpcWardrobeVariant(
                key, displayName, profession, sourceView, bodyScale,
                new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Body, skinHex },
                    { NpcLayerKind.Face, skinHex },
                    { NpcLayerKind.Hair, hairHex },
                    { NpcLayerKind.UpperClothing, upperHex },
                    { NpcLayerKind.LowerClothing, lowerHex },
                    { NpcLayerKind.Footwear, footwearHex },
                    { NpcLayerKind.Accessory, accessoryHex },
                    { NpcLayerKind.CarriedProp, accessoryHex },
                });
        }

        public static IReadOnlyList<NpcWardrobeVariant> AllVariants => Variants;

        public static int Count => Variants.Count;

        /// <summary>Deterministic cyclic variant for a resident index (handles negatives).</summary>
        public static NpcWardrobeVariant GetVariant(int residentIndex)
        {
            if (Variants.Count == 0) return null;
            var normalized = ((residentIndex % Variants.Count) + Variants.Count) % Variants.Count;
            return Variants[normalized];
        }

        public static NpcWardrobeVariant GetByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return ByKey.TryGetValue(key, out var variant) ? variant : null;
        }
    }
}
