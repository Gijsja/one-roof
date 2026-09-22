using System.Collections.Generic;

namespace OneRoof.Content
{
    /// <summary>
    /// Architectural definition for the shared 2D humanoid skeletal rig (rig.npc.humanoid.2d.v1)
    /// conforming to Spine 4.2 / Unity 2D Animation specifications and Docs/05_ASSET_PIPELINE.md.
    /// </summary>
    public static class NpcRigDefinition
    {
        public const string RigId = "rig.npc.humanoid.2d.v1";

        // Nominal world scale dimensions (meters)
        public const float NominalWorldHeight = 0.58f;
        public const float NominalWorldWidth = 0.28f;
        public const float PivotX = 0.5f;
        public const float PivotY = 0.0f; // Bottom-center anchor at feet

        // Anatomical joint heights in normalized world units (meters from ground baseline)
        public const float HipHeight = 0.28f;
        public const float SpineHeight = 0.38f;
        public const float NeckHeight = 0.47f;
        public const float HeadHeight = 0.52f;
        public const float KneeHeight = 0.14f;

        // Canonical Spine bone names
        public const string BoneRoot = "root";
        public const string BoneHip = "hip";
        public const string BoneSpine = "spine";
        public const string BoneNeck = "neck";
        public const string BoneHead = "head";
        public const string BoneArmUpperL = "arm_upper_L";
        public const string BoneArmLowerL = "arm_lower_L";
        public const string BoneHandL = "hand_L";
        public const string BoneArmUpperR = "arm_upper_R";
        public const string BoneArmLowerR = "arm_lower_R";
        public const string BoneHandR = "hand_R";
        public const string BoneLegUpperL = "leg_upper_L";
        public const string BoneLegLowerL = "leg_lower_L";
        public const string BoneFootL = "foot_L";
        public const string BoneLegUpperR = "leg_upper_R";
        public const string BoneLegLowerR = "leg_lower_R";
        public const string BoneFootR = "foot_R";

        /// <summary>
        /// Back-to-front rendering layer slot order.
        /// </summary>
        public static readonly IReadOnlyList<NpcLayerKind> LayerRenderingOrder = new[]
        {
            NpcLayerKind.Body,          // Base body & rear limbs
            NpcLayerKind.Footwear,      // Shoes / boots
            NpcLayerKind.LowerClothing,  // Trousers / skirts
            NpcLayerKind.UpperClothing,  // Shirts / jackets
            NpcLayerKind.Face,          // Expressions / features
            NpcLayerKind.Hair,          // Hair & bangs
            NpcLayerKind.Accessory,     // Glasses, hats, belts, aprons
            NpcLayerKind.CarriedProp    // Held bags, mugs, tools
        };

        /// <summary>
        /// Animator-correct parent bone per wardrobe layer, mirroring the slot→bone map in
        /// Art/SourceArt/Proposed/resident-spine-setup-v1.json. Attachments must deform with
        /// the bone they visually belong to: headgear with the head (head counter-rotation),
        /// trousers with the pelvis (not the swaying chest), shirts with the chest, and the
        /// carried prop with the holding hand so arm swing carries it along.
        /// Footwear is a single pair sprite straddling both feet, so it rides the hip (pelvis):
        /// parenting a pair to one foot bone would drag both shoes through the walk cycle.
        /// </summary>
        public static string GetParentBone(NpcLayerKind layer)
        {
            switch (layer)
            {
                case NpcLayerKind.Face:
                case NpcLayerKind.Hair:
                case NpcLayerKind.Accessory:
                    return BoneHead;
                case NpcLayerKind.LowerClothing:
                case NpcLayerKind.Footwear:
                    return BoneHip;
                case NpcLayerKind.CarriedProp:
                    return BoneHandL;
                case NpcLayerKind.Body:
                case NpcLayerKind.UpperClothing:
                default:
                    return BoneSpine;
            }
        }
    }
}
