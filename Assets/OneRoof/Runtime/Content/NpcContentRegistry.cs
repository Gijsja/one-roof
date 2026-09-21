using System;
using System.Collections.Generic;

namespace OneRoof.Content
{
    /// <summary>
    /// Canonical registry of validated, immutable NPC resident content items for One Roof.
    /// Composite placeholder sprites (resident_spritesheet / resident_0X_*) were removed;
    /// the shared Spine 2D modular rig (torso/head/limbs + 8 wardrobe slots in
    /// NpcSkeletalHierarchy) is the sole presentation path. ResourcePath is therefore
    /// empty until layered wardrobe part sprites land; content IDs remain immutable.
    /// </summary>
    public static class NpcContentRegistry
    {
        public const string RigId = NpcRigDefinition.RigId;

        private static readonly List<NpcContentRecord> Records = new List<NpcContentRecord>
        {
            new NpcContentRecord(
                contentId: "npc.resident.service.v1",
                proposedKey: "resident-01-barista",
                displayName: "Service Resident (Barista / Diner Staff)",
                resourcePath: "",
                primaryRole: "Service",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "reach-counter", "carry-tray" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "dark hair in casual bun" },
                    { NpcLayerKind.UpperClothing, "cream rolled-sleeve collar shirt" },
                    { NpcLayerKind.LowerClothing, "dark tailored rolled pants" },
                    { NpcLayerKind.Footwear, "white athletic sneakers" },
                    { NpcLayerKind.Accessory, "deep teal waist-tied apron" }
                }
            ),
            new NpcContentRecord(
                contentId: "npc.resident.corporate.v1",
                proposedKey: "resident-02-executive",
                displayName: "Corporate Resident (Manager / Office Worker)",
                resourcePath: "",
                primaryRole: "Corporate",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "desk-work", "briefcase-place" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "styled dark side-part" },
                    { NpcLayerKind.UpperClothing, "navy business suit jacket over shirt and v-neck" },
                    { NpcLayerKind.LowerClothing, "navy matching tailored suit trousers" },
                    { NpcLayerKind.Footwear, "brown leather oxfords" },
                    { NpcLayerKind.Accessory, "corporate ID badge on lanyard" },
                    { NpcLayerKind.CarriedProp, "laptop portfolio in hand" }
                }
            ),
            new NpcContentRecord(
                contentId: "npc.resident.creative.v1",
                proposedKey: "resident-03-creative",
                displayName: "Creative Resident (Designer / Architect)",
                resourcePath: "",
                primaryRole: "Creative",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "drafting-work", "lounge-sit" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "voluminous curly afro with orange headband" },
                    { NpcLayerKind.UpperClothing, "terracotta duster coat over white knit top" },
                    { NpcLayerKind.LowerClothing, "wide-leg dark slate trousers with leather belt" },
                    { NpcLayerKind.Footwear, "black ankle dress boots" },
                    { NpcLayerKind.CarriedProp, "designer canvas tote shoulder bag" }
                }
            ),
            new NpcContentRecord(
                contentId: "npc.resident.senior.v1",
                proposedKey: "resident-04-senior",
                displayName: "Senior Resident (Retired Scholar / Community Elder)",
                resourcePath: "",
                primaryRole: "Senior",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "reading-sit", "sip-coffee" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "short cropped silver hair and neat trimmed beard" },
                    { NpcLayerKind.UpperClothing, "navy crewneck sweater over collared oxford" },
                    { NpcLayerKind.LowerClothing, "tan casual chinos" },
                    { NpcLayerKind.Footwear, "comfortable brown leather walking shoes" },
                    { NpcLayerKind.CarriedProp, "white ceramic coffee mug" }
                }
            ),
            new NpcContentRecord(
                contentId: "npc.resident.youth.v1",
                proposedKey: "resident-05-student",
                displayName: "Youth Resident (Student / Freelancer)",
                resourcePath: "",
                primaryRole: "Youth",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "study-desk", "music-listen" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "black shoulder bob under mustard-yellow knit beanie" },
                    { NpcLayerKind.UpperClothing, "teal bomber jacket over oversized black hoodie" },
                    { NpcLayerKind.LowerClothing, "olive cargo jogger pants" },
                    { NpcLayerKind.Footwear, "black and white skate sneakers" },
                    { NpcLayerKind.Accessory, "black crossbody utility sling bag" }
                }
            ),
            new NpcContentRecord(
                contentId: "npc.resident.trades.v1",
                proposedKey: "resident-06-technician",
                displayName: "Trades Resident (Maintenance Technician / Facility Engineer)",
                resourcePath: "",
                primaryRole: "Trades",
                bodyType: "adult-standard",
                worldWidth: 0.28f,
                worldHeight: 0.58f,
                interactionPoints: new[] { "feet", "panel-repair", "elevator-inspect" },
                layerDescriptions: new Dictionary<NpcLayerKind, string>
                {
                    { NpcLayerKind.Hair, "dark hair under navy work baseball cap" },
                    { NpcLayerKind.UpperClothing, "navy mechanic jumpsuit with high-vis yellow vest" },
                    { NpcLayerKind.LowerClothing, "reinforced utility jumpsuit legs" },
                    { NpcLayerKind.Footwear, "heavy-duty steel-toe work boots" },
                    { NpcLayerKind.Accessory, "leather tool belt with wrenches and pliers, work gloves" }
                }
            )
        };

        private static readonly Dictionary<string, NpcContentRecord> ByContentId =
            new Dictionary<string, NpcContentRecord>(StringComparer.Ordinal);

        static NpcContentRegistry()
        {
            foreach (var rec in Records)
            {
                ByContentId[rec.ContentId] = rec;
            }
        }

        public static IReadOnlyList<NpcContentRecord> AllRecords => Records;

        public static int Count => Records.Count;

        public static NpcContentRecord GetByIndex(int index)
        {
            if (Records.Count == 0) return null;
            var normalized = ((index % Records.Count) + Records.Count) % Records.Count;
            return Records[normalized];
        }

        public static NpcContentRecord GetById(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;
            return ByContentId.TryGetValue(contentId, out var record) ? record : null;
        }
    }
}
