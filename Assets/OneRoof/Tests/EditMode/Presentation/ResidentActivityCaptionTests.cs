using NUnit.Framework;
using OneRoof.Application.Population;
using OneRoof.Application.Transit;
using OneRoof.Domain.Population;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentActivityCaptionTests
    {
        [Test]
        public void PopulationCaption_ExplainsDestinationAndActivity()
        {
            var traveling = new NpcProjection(1, 1, 2, 10, NpcActivityKind.Idle,
                true, 4, 12, 0, 0f);
            var waiting = new NpcProjection(1, 1, 2, 10, NpcActivityKind.Idle,
                true, 4, 12, 15, 0f);
            var frustrated = new NpcProjection(1, 1, 2, 10, NpcActivityKind.Idle,
                true, 4, 12, 30, 0f);
            var working = new NpcProjection(1, 1, 4, 12, NpcActivityKind.Working,
                false, null, null, 0, 0f);

            Assert.That(ResidentActivityCaption.For(traveling), Is.EqualTo("To floor 4"));
            Assert.That(ResidentActivityCaption.For(waiting), Is.EqualTo("Waiting · F4"));
            Assert.That(ResidentActivityCaption.For(frustrated), Is.EqualTo("Lift's late · F4"));
            Assert.That(ResidentActivityCaption.For(working), Is.EqualTo("At work"));
        }

        [Test]
        public void TowerCaption_ExplainsElevatorJourneyAndRoutine()
        {
            var queued = new TransitResidentProjection(1, 4, TransitResidentStatus.Queued);
            var riding = new TransitResidentProjection(1, 4, TransitResidentStatus.Riding);
            var eating = new TransitResidentProjection(1, 4, TransitResidentStatus.InRoom,
                activity: ActivityKind.Eating);

            Assert.That(ResidentActivityCaption.For(queued), Is.EqualTo("Waiting · F4"));
            Assert.That(ResidentActivityCaption.For(riding), Is.EqualTo("Lift to F4"));
            Assert.That(ResidentActivityCaption.For(eating), Is.EqualTo("Eating"));
        }

        [Test]
        public void RigCaption_ClearsWhenViewIsReused()
        {
            var go = new GameObject("CaptionTestResident");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                rig.Initialize(0);
                rig.SetCaption("At work");
                Assert.That(rig.ActivityCaption.text, Is.EqualTo("At work"));
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.True);

                rig.Initialize(1);
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PooledView_BindsAndClearsCaption()
        {
            var go = new GameObject("CaptionTestView");
            try
            {
                var rig = go.AddComponent<NpcSkeletalHierarchy>();
                var view = go.AddComponent<NpcView>();
                var resident = new NpcProjection(1, 1, 2, 10, NpcActivityKind.Working,
                    false, null, null, 0, 0f);

                view.Bind(resident, Vector3.zero);
                Assert.That(rig.ActivityCaption.text, Is.EqualTo("At work"));

                view.Unbind();
                Assert.That(rig.ActivityCaption.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
