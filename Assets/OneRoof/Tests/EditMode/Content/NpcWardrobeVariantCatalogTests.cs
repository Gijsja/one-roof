using System.Collections.Generic;
using NUnit.Framework;

namespace OneRoof.Content.Tests.EditMode
{
    public sealed class NpcWardrobeVariantCatalogTests
    {
        [Test]
        public void Catalog_ExposesTwelveProfessionVariants()
        {
            Assert.That(NpcWardrobeVariantCatalog.Count, Is.EqualTo(12));
            Assert.That(NpcWardrobeVariantCatalog.AllVariants.Count, Is.EqualTo(12));
        }

        [Test]
        public void Catalog_KeysAreUniqueNamespacedAndVersioned()
        {
            var seen = new HashSet<string>();
            foreach (var variant in NpcWardrobeVariantCatalog.AllVariants)
            {
                Assert.That(variant.Key, Does.StartWith("npc.wardrobe.variant."));
                Assert.That(variant.Key, Does.EndWith(".v1"));
                Assert.That(seen.Add(variant.Key), Is.True, $"Duplicate variant key: {variant.Key}");
                Assert.That(NpcWardrobeVariantCatalog.GetByKey(variant.Key), Is.SameAs(variant));
            }
        }

        [Test]
        public void Catalog_EachVariantCoversAllEightRigLayersWithValidHex()
        {
            foreach (var variant in NpcWardrobeVariantCatalog.AllVariants)
            {
                Assert.That(variant.LayerColors.Count, Is.EqualTo(8));
                Assert.That(variant.BodyScale, Is.InRange(0.9f, 1.1f));
                Assert.That(new[] { "front", "side", "back" }, Does.Contain(variant.SourceView));
                foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
                {
                    var hex = variant.GetLayerColor(layer);
                    Assert.That(hex, Does.Match("^#[0-9A-Fa-f]{6}$"), $"{variant.Key}/{layer} must be #RRGGBB");
                }
            }
        }

        [Test]
        public void Catalog_GetVariantIsDeterministicAndCyclic()
        {
            for (var i = 0; i < 50; i++)
            {
                Assert.That(NpcWardrobeVariantCatalog.GetVariant(i),
                    Is.SameAs(NpcWardrobeVariantCatalog.AllVariants[i % 12]));
            }
            Assert.That(NpcWardrobeVariantCatalog.GetVariant(-1),
                Is.SameAs(NpcWardrobeVariantCatalog.AllVariants[11]));
            Assert.That(NpcWardrobeVariantCatalog.GetByKey(null), Is.Null);
            Assert.That(NpcWardrobeVariantCatalog.GetByKey("npc.wardrobe.variant.missing.v1"), Is.Null);
        }

        [Test]
        public void Catalog_FiftyResidentsResolveMoreVariantsThanSixArchetypes()
        {
            var seen = new HashSet<string>();
            for (var i = 0; i < 50; i++)
            {
                seen.Add(NpcWardrobeVariantCatalog.GetVariant(i).Key);
            }
            Assert.That(seen.Count, Is.EqualTo(12));
        }

        [Test]
        public void Catalog_PartPathsAreNamespacedResourcePaths()
        {
            const string prefix = "Residents/Wardrobe/Front/wardrobe_front_";
            var bareLegsCount = 0;
            foreach (var variant in NpcWardrobeVariantCatalog.AllVariants)
            {
                Assert.That(variant.PartPaths.Count, Is.EqualTo(8));
                foreach (var layer in new[] { NpcLayerKind.Face, NpcLayerKind.UpperClothing, NpcLayerKind.LowerClothing, NpcLayerKind.Footwear })
                {
                    Assert.That(variant.GetPartPath(layer), Does.StartWith(prefix), $"{variant.Key}/{layer} must reference a sliced part");
                }
                var accessory = variant.GetPartPath(NpcLayerKind.Accessory);
                Assert.That(accessory == "" || accessory.StartsWith(prefix), Is.True);
                if (variant.BareLegs) bareLegsCount++;
            }
            Assert.That(bareLegsCount, Is.EqualTo(1));
        }
    }
}
