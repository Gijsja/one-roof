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
        public void Parts_PhotoAndSwatchPathsShareAnchors()
        {
            // Regression: the old swatch fallback used a second anchor table, so residents
            // missing a slice popped by ~10cm (footwear) vs sliced neighbors. Both paths now
            // share SlotAnchor, and the swatch lands at the same final size as auto-fit.
            var go = new GameObject("AnchorParity");
            try
            {
                var photoSlot = new GameObject("Photo").transform;
                photoSlot.SetParent(go.transform, false);
                var swatchSlot = new GameObject("Swatch").transform;
                swatchSlot.SetParent(go.transform, false);

                foreach (var layer in new[] { NpcLayerKind.UpperClothing, NpcLayerKind.Footwear })
                {
                    var variant = NpcWardrobeVariantCatalog.AllVariants[0];
                    var part = WardrobePartCatalog.GetPart(variant, layer);
                    if (part == null) continue;
                    WardrobePartCatalog.FitSlot(photoSlot, part, layer);
                    WardrobePartCatalog.FitSwatchSlot(swatchSlot, layer);
                    Assert.That(swatchSlot.localPosition, Is.EqualTo(WardrobePartCatalog.SlotAnchor(layer, part)).Within(0.0001f),
                        $"{layer} swatch must share the photo anchor.");
                }

                // Trousers are the honest exception: full-length (tall) hangs to the ankle while
                // the generic swatch is cut short, so anchors differ by garment length on purpose.
                var lowerPart = WardrobePartCatalog.GetPart(NpcWardrobeVariantCatalog.AllVariants[0], NpcLayerKind.LowerClothing);
                if (lowerPart != null)
                {
                    var tallAnchor = WardrobePartCatalog.SlotAnchor(NpcLayerKind.LowerClothing, lowerPart);
                    var swatchAnchor = WardrobePartCatalog.SlotAnchor(NpcLayerKind.LowerClothing, null);
                    Assert.That(tallAnchor.y, Is.LessThan(swatchAnchor.y),
                        "Full-length trousers must hang lower than the short swatch cut.");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
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
