using System;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using EntityId = OneRoof.Domain.Identity.EntityId;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class VisualEffectsPresenterTests
    {
        [Test]
        public void Agitation_CreatesAndClearsVisibleAuraWithoutDomainMutation()
        {
            var go = new GameObject("Effects");
            try
            {
                var effects = go.AddComponent<VisualEffectsPresenter>();
                effects.SetAgitation(0.8f);
                effects.Advance(0.1f);
                Assert.That(effects.Severity, Is.EqualTo(0.8f).Within(0.001f));
                Assert.That(go.transform.Find("CongestionAgitationAura"), Is.Not.Null);

                effects.SetAgitation(0f);
                Assert.That(effects.Severity, Is.EqualTo(0f));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Apply_RecoversWhenBlockLostButRenderersCached()
        {
            var go = new GameObject("EffectsStale");
            try
            {
                var effects = go.AddComponent<VisualEffectsPresenter>();
                effects.SetAgitation(0.8f);
                var blockField = typeof(VisualEffectsPresenter).GetField("_block",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                blockField.SetValue(effects, null);

                Assert.DoesNotThrow(() => effects.Advance(0.016f));
                Assert.That(blockField.GetValue(effects), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void Construction_UsesShaderFadeForChildrenCreatedAfterBegin_ThenRestoresMaterial()
        {
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
            Assert.That(shader, Is.Not.Null, "The installed All In 1 shader must be available to city presentation.");
            var root = new GameObject("Construction");
            var source = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var effects = root.AddComponent<VisualEffectsPresenter>();
                effects.BeginConstruction(1f);
                var child = GameObject.CreatePrimitive(PrimitiveType.Quad);
                child.transform.SetParent(root.transform, false);
                var renderer = child.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = source;

                effects.Advance(0.25f);
                Assert.That(renderer.sharedMaterial.shader, Is.EqualTo(shader));
                Assert.That(renderer.sharedMaterial.IsKeywordEnabled("FADE_ON"), Is.True);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_FadeAmount"), Is.EqualTo(0.75f).Within(0.001f));

                effects.Advance(0.75f);
                Assert.That(renderer.sharedMaterial, Is.SameAs(source));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void RoomPresenter_UsesDomainInteractionPointForDockCoordinate()
        {
            var roomId = new EntityId(9);
            var room = new Room(roomId, new ContentId("residential:studio"), new CellBounds(2, 4, 7),
                Array.Empty<EntityId>(), 2,
                new[] { new InteractionPoint(new EntityId(10), roomId, InteractionPointKind.Sleep, new ContentId("prop:bed"), 2) });
            var presenter = new RoomPresenter();

            var docked = presenter.TryGetInteractionDock(room, InteractionPointKind.Sleep, 0, out var position);

            Assert.That(docked, Is.True);
            Assert.That(position.x, Is.EqualTo(-2.4f + 6.5f * 0.5f).Within(0.001f));
            Assert.That(position.y, Is.EqualTo(TowerStructurePresenter.FloorY(2) - 0.58f).Within(0.001f));
        }
    }
}
