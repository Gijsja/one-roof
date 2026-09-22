using NUnit.Framework;
using OneRoof.Content;

namespace OneRoof.Content.Tests.EditMode
{
    public sealed class NpcSkeletalRigTests
    {
        [Test]
        public void RigDefinition_HasCanonicalIdAndProportions()
        {
            Assert.That(NpcRigDefinition.RigId, Is.EqualTo("rig.npc.humanoid.2d.v1"));
            Assert.That(NpcRigDefinition.NominalWorldHeight, Is.EqualTo(0.58f));
            Assert.That(NpcRigDefinition.NominalWorldWidth, Is.EqualTo(0.28f));
            Assert.That(NpcRigDefinition.PivotX, Is.EqualTo(0.5f));
            Assert.That(NpcRigDefinition.PivotY, Is.EqualTo(0.0f));
        }

        [Test]
        public void RigDefinition_JointHierarchyAscendsMonotonically()
        {
            Assert.That(NpcRigDefinition.KneeHeight, Is.GreaterThan(0f));
            Assert.That(NpcRigDefinition.HipHeight, Is.GreaterThan(NpcRigDefinition.KneeHeight));
            Assert.That(NpcRigDefinition.SpineHeight, Is.GreaterThan(NpcRigDefinition.HipHeight));
            Assert.That(NpcRigDefinition.NeckHeight, Is.GreaterThan(NpcRigDefinition.SpineHeight));
            Assert.That(NpcRigDefinition.HeadHeight, Is.GreaterThan(NpcRigDefinition.NeckHeight));
            Assert.That(NpcRigDefinition.NominalWorldHeight, Is.GreaterThan(NpcRigDefinition.HeadHeight));
        }

        [Test]
        public void RigDefinition_SlotParentBonesFollowSpineSpec()
        {
            // Mirrors the slot→bone map in resident-spine-setup-v1.json: garments deform with
            // the joint they belong to. Footwear is the deliberate exception: the pair sprite
            // straddles both feet, so it rides the pelvis instead of one foot bone.
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.Face), Is.EqualTo(NpcRigDefinition.BoneHead));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.Hair), Is.EqualTo(NpcRigDefinition.BoneHead));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.Accessory), Is.EqualTo(NpcRigDefinition.BoneHead));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.UpperClothing), Is.EqualTo(NpcRigDefinition.BoneSpine));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.Body), Is.EqualTo(NpcRigDefinition.BoneSpine));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.LowerClothing), Is.EqualTo(NpcRigDefinition.BoneHip));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.Footwear), Is.EqualTo(NpcRigDefinition.BoneHip));
            Assert.That(NpcRigDefinition.GetParentBone(NpcLayerKind.CarriedProp), Is.EqualTo(NpcRigDefinition.BoneHandL));
        }

        [Test]
        public void RigDefinition_LayerRenderingOrderCoversAllLayers()
        {
            var layers = NpcRigDefinition.LayerRenderingOrder;
            Assert.That(layers.Count, Is.EqualTo(8));
            Assert.That(layers[0], Is.EqualTo(NpcLayerKind.Body));
            Assert.That(layers[layers.Count - 1], Is.EqualTo(NpcLayerKind.CarriedProp));
        }

        [Test]
        public void ContentRegistry_ContainsSixApprovedArchetypes()
        {
            Assert.That(NpcContentRegistry.Count, Is.EqualTo(6));
            Assert.That(NpcContentRegistry.AllRecords.Count, Is.EqualTo(6));
        }

        [Test]
        public void ContentRegistry_AllRecordsConformToContentIdContract()
        {
            foreach (var record in NpcContentRegistry.AllRecords)
            {
                Assert.That(record.ContentId, Does.StartWith("npc.resident."));
                Assert.That(record.ContentId, Does.EndWith(".v1"));
                Assert.That(record.WorldHeight, Is.EqualTo(0.58f));
                Assert.That(record.WorldWidth, Is.EqualTo(0.28f));
                Assert.That(record.InteractionPoints.Count, Is.GreaterThan(0));
                Assert.That(record.LayerDescriptions.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void ContentRegistry_LookupById_ReturnsExpectedRecord()
        {
            var barista = NpcContentRegistry.GetById("npc.resident.service.v1");
            Assert.That(barista, Is.Not.Null);
            Assert.That(barista.ProposedKey, Is.EqualTo("resident-01-barista"));
            Assert.That(barista.PrimaryRole, Is.EqualTo("Service"));

            var exec = NpcContentRegistry.GetById("npc.resident.corporate.v1");
            Assert.That(exec, Is.Not.Null);
            Assert.That(exec.ProposedKey, Is.EqualTo("resident-02-executive"));

            Assert.That(NpcContentRegistry.GetById("nonexistent.id"), Is.Null);
        }

        [Test]
        public void ContentRegistry_LookupByIndex_WrapsCyclically()
        {
            for (var i = 0; i < 50; i++)
            {
                var record = NpcContentRegistry.GetByIndex(i);
                Assert.That(record, Is.Not.Null);
                Assert.That(record, Is.SameAs(NpcContentRegistry.AllRecords[i % 6]));
            }
        }

        [Test]
        public void WardrobeLoadout_ResolvesAllEightCompatibleLayers()
        {
            var loadout = NpcWardrobeLoadout.FromRecord(NpcContentRegistry.GetByIndex(0));

            Assert.That(loadout.RigId, Is.EqualTo(NpcRigDefinition.RigId));
            Assert.That(loadout.Layers.Count, Is.EqualTo(8));
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                Assert.That(loadout.GetLayerId(layer), Does.StartWith("npc.wardrobe."));
                Assert.That(loadout.GetLayerId(layer), Does.EndWith(".v1"));
            }
        }
    }
}
