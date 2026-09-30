using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class WardrobePartCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            WardrobePartCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            WardrobePartCatalog.ClearCache();
        }

        [Test]
        public void Parts_ArchivedPathsCannotActivatePredefinedRepresentations()
        {
            Assert.That(WardrobePartCatalog.GetPart("Residents/Wardrobe/Front/wardrobe_front_head_03"), Is.Null);
            foreach (var variant in NpcWardrobeVariantCatalog.AllVariants)
                foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
                    Assert.That(WardrobePartCatalog.GetPart(variant, layer), Is.Null);
        }

        [Test]
        public void Parts_EmptyPathResolvesNullWithoutThrowing()
        {
            Assert.That(WardrobePartCatalog.GetPart(""), Is.Null);
            Assert.That(WardrobePartCatalog.GetPart((string)null), Is.Null);
            Assert.That(WardrobePartCatalog.GetPart(null, NpcLayerKind.Accessory), Is.Null);
        }

        [Test]
        public void Parts_ProceduralSlotSizingKeepsDocumentedAnchors()
        {
            var go = new GameObject("ProceduralSlot");
            try
            {
                foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
                {
                    WardrobePartCatalog.FitSwatchSlot(go.transform, layer);
                    Assert.That(go.transform.localPosition, Is.EqualTo(WardrobePartCatalog.SlotAnchor(layer, null)));
                    Assert.That(go.transform.localScale.x, Is.EqualTo(WardrobePartCatalog.TargetSize(layer).x));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Parts_HierarchyShowsArticulatedBodyAndDisablesMismatchedPhotoParts()
        {
            var go = new GameObject("PartResident");
            try
            {
                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(2); // chef: full part set incl. headgear

                var faceSlot = skeletal.WardrobeSlots[NpcLayerKind.Face];
                Assert.That(faceSlot.enabled, Is.False);
                Assert.That(skeletal.MainRenderer.enabled, Is.False);
                Assert.That(skeletal.MainRenderer.sprite, Is.Not.Null);
                foreach (var limb in skeletal.LimbRenderers.Values) Assert.That(limb.enabled, Is.True);

                var accessorySlot = skeletal.WardrobeSlots[NpcLayerKind.Accessory];
                Assert.That(accessorySlot.enabled, Is.False);

                // Casual variant: no headgear, bare legs stay skin-toned.
                skeletal.Initialize(11);
                Assert.That(skeletal.WardrobeSlots[NpcLayerKind.Accessory].enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
