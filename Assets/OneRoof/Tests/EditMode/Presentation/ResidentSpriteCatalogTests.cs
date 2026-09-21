using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Content;
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
                Assert.That(skeletal.Bones.Count, Is.EqualTo(17));
                Assert.That(skeletal.WardrobeSlots.Count, Is.EqualTo(8));
                Assert.That(skeletal.LimbRenderers.Count, Is.EqualTo(10));
                Assert.That(skeletal.LimbRenderers[NpcRigDefinition.BoneArmUpperL].transform.parent,
                    Is.SameAs(skeletal.Bones[NpcRigDefinition.BoneArmUpperL]));
                Assert.That(skeletal.LimbRenderers[NpcRigDefinition.BoneLegUpperR].transform.parent,
                    Is.SameAs(skeletal.Bones[NpcRigDefinition.BoneLegUpperR]));
                Assert.That(skeletal.Wardrobe, Is.Not.Null);

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

                skeletal.SetAnimationClip(NpcAnimationClip.Sleep);
                Assert.DoesNotThrow(() => skeletal.ApplyProceduralAnimation(2f));
                skeletal.SetAnimationClip(NpcAnimationClip.Sit);
                Assert.DoesNotThrow(() => skeletal.ApplyProceduralAnimation(2.5f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SkeletalHierarchy_EmoteBubbleSetupAndAnimation()
        {
            var go = new GameObject("TestEmoteResident");
            try
            {
                var skeletal = go.AddComponent<NpcSkeletalHierarchy>();
                skeletal.Initialize(0);

                Assert.That(skeletal.EmoteAnchor, Is.Not.Null);
                Assert.That(skeletal.EmoteRenderer, Is.Not.Null);
                Assert.That(skeletal.CurrentEmote, Is.EqualTo(NpcEmoteKind.None));
                Assert.That(skeletal.EmoteRenderer.enabled, Is.False);

                // Set anger emote (congestion wait bottleneck)
                skeletal.SetEmote(NpcEmoteKind.Anger);
                Assert.That(skeletal.CurrentEmote, Is.EqualTo(NpcEmoteKind.Anger));
                Assert.That(skeletal.EmoteRenderer.enabled, Is.True);
                Assert.That(skeletal.EmoteRenderer.sprite, Is.Not.Null);
                Assert.That(skeletal.EmoteRenderer.sortingOrder, Is.EqualTo(25));

                // Animate procedural frame advance and floating bob
                Assert.DoesNotThrow(() => skeletal.ApplyProceduralAnimation(0.2f));
                Assert.DoesNotThrow(() => skeletal.ApplyProceduralAnimation(0.4f));

                // Switch to sweat drops
                skeletal.SetEmote(NpcEmoteKind.Sweat);
                Assert.That(skeletal.CurrentEmote, Is.EqualTo(NpcEmoteKind.Sweat));
                Assert.That(skeletal.EmoteRenderer.enabled, Is.True);

                // Clear emote
                skeletal.SetEmote(NpcEmoteKind.None);
                Assert.That(skeletal.CurrentEmote, Is.EqualTo(NpcEmoteKind.None));
                Assert.That(skeletal.EmoteRenderer.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
