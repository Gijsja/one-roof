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
