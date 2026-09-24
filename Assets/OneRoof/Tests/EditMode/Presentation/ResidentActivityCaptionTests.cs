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
        public void TowerCaption_NamesPurposeAndWalkingDestination()
        {
            var dinerWalk = new TransitResidentProjection(1, 0, TransitResidentStatus.Walking,
                purposeLabel: "Eating at diner");
            var dinerArrival = new TransitResidentProjection(1, 0, TransitResidentStatus.InRoom,
                purposeLabel: "Eating at diner");
            var reading = new TransitResidentProjection(2, 1, TransitResidentStatus.InRoom,
                purposeLabel: "Reading");
            var learning = new TransitResidentProjection(3, 1, TransitResidentStatus.InRoom,
                purposeLabel: "Learning");
            var chilling = new TransitResidentProjection(4, 1, TransitResidentStatus.InRoom,
                purposeLabel: "Chilling");
            var homeWalk = new TransitResidentProjection(5, 1, TransitResidentStatus.Walking,
                purposeLabel: "Returning home");

            Assert.That(ResidentActivityCaption.For(dinerWalk), Is.EqualTo("To diner"));
            Assert.That(ResidentActivityCaption.For(dinerArrival), Is.EqualTo("Eating at diner"));
            Assert.That(ResidentActivityCaption.For(reading), Is.EqualTo("Reading"));
            Assert.That(ResidentActivityCaption.For(learning), Is.EqualTo("Learning"));
            Assert.That(ResidentActivityCaption.For(chilling), Is.EqualTo("Chilling"));
            Assert.That(ResidentActivityCaption.For(homeWalk), Is.EqualTo("Going home"));
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
