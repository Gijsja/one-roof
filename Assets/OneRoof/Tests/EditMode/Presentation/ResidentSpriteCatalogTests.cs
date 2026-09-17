using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentSpriteCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            ResidentSpriteCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ResidentSpriteCatalog.ClearCache();
        }

        [Test]
        public void Catalog_ResolvesNonNullSpritesForFiftySimulationResidents()
        {
            for (var i = 0; i < 50; i++)
            {
                var sprite = ResidentSpriteCatalog.GetResidentSprite(i);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.width, Is.GreaterThan(0));
                Assert.That(sprite.rect.height, Is.GreaterThan(0));
                // Pivot must be anchored bottom-center (0.5, 0.0)
                Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.01f));
                Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.0f).Within(0.01f));
            }
        }

        [Test]
        public void Catalog_LookupByContentId_ReturnsMatchingSprite()
        {
            var baristaSprite = ResidentSpriteCatalog.GetResidentSprite("npc.resident.service.v1");
            Assert.That(baristaSprite, Is.Not.Null);

            var execSprite = ResidentSpriteCatalog.GetResidentSprite("npc.resident.corporate.v1");
            Assert.That(execSprite, Is.Not.Null);

            var fallback = ResidentSpriteCatalog.GetResidentSprite("npc.invalid");
            Assert.That(fallback, Is.Not.Null);
        }

        [Test]
        public void SkeletalHierarchy_BuildsBonesAndSlotRenderers()
        {
            var go = new GameObject("TestResident");
            try
            {
                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(0);

                Assert.That(skeletal.Root, Is.Not.Null);
                Assert.That(skeletal.Hip, Is.Not.Null);
                Assert.That(skeletal.Spine, Is.Not.Null);
                Assert.That(skeletal.Neck, Is.Not.Null);
                Assert.That(skeletal.Head, Is.Not.Null);

                Assert.That(skeletal.MainRenderer, Is.Not.Null);
                Assert.That(skeletal.MainRenderer.sprite, Is.Not.Null);
                Assert.That(skeletal.StatusPlateRenderer, Is.Not.Null);
                Assert.That(skeletal.StatusPlateRenderer.sprite, Is.Not.Null);

                // Verify transit status updates
                skeletal.SetTransitStatus(TransitResidentStatus.Queued);
                Assert.That(skeletal.StatusPlateRenderer.color.r, Is.GreaterThan(0.8f)); // Amber waiting

                skeletal.SetTransitStatus(TransitResidentStatus.Riding);
                Assert.That(skeletal.StatusPlateRenderer.color.b, Is.GreaterThan(0.8f)); // Cyan transit

                skeletal.SetTransitStatus(TransitResidentStatus.Arrived);
                Assert.That(skeletal.StatusPlateRenderer.color.g, Is.GreaterThan(0.5f)); // Green arrived

                // Verify procedural animation executes cleanly
                Assert.DoesNotThrow(() => skeletal.ApplyProceduralAnimation(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
