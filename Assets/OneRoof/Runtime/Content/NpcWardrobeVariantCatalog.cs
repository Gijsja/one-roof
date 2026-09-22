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
        /// <summary>When true the lower part is short (shorts) and leg anatomy stays skin-toned.</summary>
        public bool BareLegs { get; }
        public IReadOnlyDictionary<NpcLayerKind, string> LayerColors { get; }
        /// <summary>Transparent sliced part sprites per layer as Resources paths ("" = none).</summary>
        public IReadOnlyDictionary<NpcLayerKind, string> PartPaths { get; }

        public NpcWardrobeVariant(
            string key,
            string displayName,
            string profession,
            string sourceView,
            float bodyScale,
            IDictionary<NpcLayerKind, string> layerColors,
            IDictionary<NpcLayerKind, string> partPaths = null,
            bool bareLegs = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Profession = profession ?? "Resident";
            SourceView = sourceView ?? "front";
            BodyScale = bodyScale > 0f ? bodyScale : 1f;
            BareLegs = bareLegs;
            LayerColors = new ReadOnlyDictionary<NpcLayerKind, string>(
                new Dictionary<NpcLayerKind, string>(layerColors ?? new Dictionary<NpcLayerKind, string>()));
            var parts = new Dictionary<NpcLayerKind, string>();
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                parts[layer] = partPaths != null && partPaths.TryGetValue(layer, out var path) ? path ?? "" : "";
            }
            PartPaths = new ReadOnlyDictionary<NpcLayerKind, string>(parts);
        }

        public string GetLayerColor(NpcLayerKind layer)
        {
            return LayerColors.TryGetValue(layer, out var hex) && !string.IsNullOrWhiteSpace(hex)
                ? hex
                : "#B8B8B8";
        }

        public string GetPartPath(NpcLayerKind layer)
        {
            return PartPaths.TryGetValue(layer, out var path) ? path ?? "" : "";
        }
    }

    public static class NpcWardrobeVariantCatalog
    {
        private const string PartPrefix = "Residents/Wardrobe/Front/wardrobe_front_";

        private static readonly List<NpcWardrobeVariant> Variants = new List<NpcWardrobeVariant>
        {
            // Skin tones cycle across 5 reference complexions; scale jitters +/-5%.
            // Part sprites are transparent slices of frontview.png (see
            // Art/SourceArt/Proposed/resident-wardrobe-parts-v1.spec.json).
            Create("npc.wardrobe.variant.firefighter.v1", "Firefighter Turnout", "Firefighter", "front",
                1.03f, "#8A4A32", "#2A2A2E", "#C8321E", "#B02020", "#2E2E34", "#E8C33C",
                accessory: "headgear_00_firehelmet", face: "head_03", upper: "upper_03_firefighter",
                lower: "lower_03_red_cargo", footwear: "footwear_03_pair"),
            Create("npc.wardrobe.variant.construction.v1", "Construction Hi-Vis", "Construction", "side",
                1.01f, "#6B4230", "#1E1E22", "#E86A1C", "#2A3440", "#1A1A1E", "#E8E8E8",
                accessory: "headgear_01_hardhat", face: "head_00", upper: "upper_00_hivis",
                lower: "lower_00_navy_cargo", footwear: "footwear_00_pair"),
            Create("npc.wardrobe.variant.chef.v1", "Chef Whites", "Chef", "front",
                0.99f, "#C68863", "#1A1A1A", "#F2F2F0", "#F2F2F0", "#1C1C20", "#F5F5F5",
                accessory: "headgear_02_toque", face: "head_05", upper: "upper_02_chef",
                lower: "lower_02_white", footwear: "footwear_02_pair"),
            Create("npc.wardrobe.variant.medic.v1", "Medic Service Red", "Medic", "side",
                0.98f, "#A06A4A", "#141416", "#C02028", "#A01820", "#222228", "#D8D8D8",
                accessory: "headgear_03_ballcap_red", face: "head_04", upper: "upper_05_medic_red",
                lower: "lower_03_red_cargo", footwear: "footwear_04_pair"),
            Create("npc.wardrobe.variant.security.v1", "Security Tactical", "Security", "back",
                1.04f, "#77503A", "#0E0E10", "#23262E", "#1B1E26", "#101114", "#3A3A40",
                accessory: "headgear_04_policecap", face: "head_02", upper: "upper_04_tactical",
                lower: "lower_06_black_tac_a", footwear: "footwear_04_pair"),
            Create("npc.wardrobe.variant.pilot.v1", "Pilot Whites", "Pilot", "front",
                1.00f, "#8A5A3C", "#26221E", "#EDEEF2", "#E8E9EE", "#1A1A20", "#2A3A5C",
                accessory: "headgear_05_bellhop", face: "head_07", upper: "upper_07_pilot",
                lower: "lower_04_black", footwear: "footwear_03_pair"),
            Create("npc.wardrobe.variant.corporate.v1", "Corporate Suit", "Corporate", "side",
                1.02f, "#C89878", "#2E2A26", "#33363E", "#2E3138", "#141518", "#2E4A7A",
                accessory: "", face: "head_06", upper: "upper_06_suit",
                lower: "lower_04_black", footwear: "footwear_04_pair"),
            Create("npc.wardrobe.variant.police.v1", "Police Navy", "Police", "front",
                1.00f, "#7A4E34", "#101418", "#2A3E5C", "#232E48", "#101418", "#C8A828",
                accessory: "headgear_04_policecap", face: "head_01", upper: "upper_08_police",
                lower: "lower_00_navy_cargo", footwear: "footwear_01_pair"),
            Create("npc.wardrobe.variant.service.v1", "Service Whites", "Service", "front",
                0.97f, "#B87A56", "#3A2E24", "#F0EDE8", "#3A3A42", "#1E1E22", "#1A1A1A",
                accessory: "", face: "head_10_alt", upper: "upper_09_service_bowtie",
                lower: "lower_01_white_stripe", footwear: "footwear_01_pair"),
            Create("npc.wardrobe.variant.maintenance.v1", "Maintenance Purple", "Maintenance", "side",
                0.96f, "#6E4530", "#1C1C22", "#6A3AA0", "#3A3F52", "#2A2A32", "#D8B83C",
                accessory: "headgear_06_baseball_navy", face: "head_11_alt", upper: "upper_11_purple",
                lower: "lower_07_black_tac_b", footwear: "footwear_06_pair"),
            Create("npc.wardrobe.variant.astronaut.v1", "Astronaut Suit", "Astronaut", "side",
                1.05f, "#96704E", "#20242A", "#D8DCE2", "#C8CCD4", "#E8E8EC", "#3A6AC8",
                accessory: "headgear_07_astrohelmet", face: "head_09_alt", upper: "upper_12_astronaut",
                lower: "lower_05_white_space", footwear: "footwear_05_pair"),
            Create("npc.wardrobe.variant.casual.v1", "Casual Off-Duty", "Casual", "front",
                0.95f, "#84603F", "#241E18", "#4A5568", "#C02828", "#2A2E36", "#808080",
                accessory: "", face: "head_08", upper: "upper_01_service_red",
                lower: "lower_08_red_shorts", footwear: "footwear_05_pair", bareLegs: true),
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
            string accessoryHex,
            string accessory = "",
            string face = "",
            string upper = "",
            string lower = "",
            string footwear = "",
            bool bareLegs = false)
        {
            var parts = new Dictionary<NpcLayerKind, string>
            {
                { NpcLayerKind.Accessory, ToPartPath(accessory) },
                { NpcLayerKind.Face, ToPartPath(face) },
                { NpcLayerKind.UpperClothing, ToPartPath(upper) },
                { NpcLayerKind.LowerClothing, ToPartPath(lower) },
                { NpcLayerKind.Footwear, ToPartPath(footwear) },
            };
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
                },
                parts,
                bareLegs);
        }

        private static string ToPartPath(string part)
        {
            return string.IsNullOrWhiteSpace(part) ? "" : PartPrefix + part;
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
