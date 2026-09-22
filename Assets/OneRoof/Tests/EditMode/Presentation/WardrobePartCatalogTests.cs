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
        public void Parts_AllVariantReferencesResolveToImportedSprites()
        {
            foreach (var variant in NpcWardrobeVariantCatalog.AllVariants)
            {
                foreach (var layer in new[] { NpcLayerKind.Face, NpcLayerKind.UpperClothing, NpcLayerKind.LowerClothing, NpcLayerKind.Footwear })
                {
                    var sprite = WardrobePartCatalog.GetPart(variant, layer);
                    Assert.That(sprite, Is.Not.Null, $"{variant.Key}/{layer} missing sliced sprite");
                    Assert.That(sprite.rect.width, Is.GreaterThan(0));
                    Assert.That(sprite.rect.height, Is.GreaterThan(0));
                    // Bottom-center pivot contract (512 PPU import).
                    Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.01f));
                    Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.0f).Within(0.01f));
                }
            }
        }

        [Test]
        public void Parts_EmptyPathResolvesNullWithoutThrowing()
        {
            Assert.That(WardrobePartCatalog.GetPart(""), Is.Null);
            Assert.That(WardrobePartCatalog.GetPart((string)null), Is.Null);
            Assert.That(WardrobePartCatalog.GetPart(null, NpcLayerKind.Accessory), Is.Null);
        }

        [Test]
        public void Parts_HierarchyAppliesPhotoPartsAndHidesCoveredAnatomy()
        {
            var go = new GameObject("PartResident");
            try
            {
                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(2); // chef: full part set incl. headgear

                var faceSlot = skeletal.WardrobeSlots[NpcLayerKind.Face];
                Assert.That(faceSlot.sprite, Is.Not.Null);
                Assert.That(faceSlot.sprite.name, Does.Contain("wardrobe_front_head_"));
                Assert.That(faceSlot.color, Is.EqualTo(Color.white));
                Assert.That(skeletal.LimbRenderers["head"].enabled, Is.False);
                Assert.That(skeletal.LimbRenderers["torso"].enabled, Is.False);

                var accessorySlot = skeletal.WardrobeSlots[NpcLayerKind.Accessory];
                Assert.That(accessorySlot.sprite, Is.Not.Null);
                Assert.That(accessorySlot.sprite.name, Does.Contain("headgear"));

                // Casual variant: no headgear, bare legs stay skin-toned.
                skeletal.Initialize(11);
                Assert.That(skeletal.WardrobeSlots[NpcLayerKind.Accessory].sprite.name,
                    Does.Not.Contain("headgear"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
