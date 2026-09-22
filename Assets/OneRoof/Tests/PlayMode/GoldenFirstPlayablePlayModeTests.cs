using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Tower;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class GoldenFirstPlayablePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenFirstPlayable_CapacityIntervention_DeliversAllResidents()
        {
            var session = new TowerSimulationSession();

            // Initial state at tick 0: 50 persistent residents, 1 elevator
            var initial = session.Projection();
            Assert.That(initial.Residents.Count, Is.EqualTo(50));
            Assert.That(initial.Elevators.Count, Is.EqualTo(1));

            // Step 25 ticks into morning commute
            for (var i = 0; i < 25; i++)
            {
                session.AdvanceOneTick();
                yield return null;
            }

            var midway1 = session.Projection();
            Assert.That(midway1.Tick, Is.EqualTo(25));

            // Player intervention: Add elevator capacity
            session.AddCapacity();
            var postIntervention = session.Projection();
            Assert.That(postIntervention.Elevators.Count, Is.EqualTo(2));

            // Run through commute until delivered or max 120 ticks
            for (var i = 0; i < 120; i++)
            {
                session.AdvanceOneTick();
                yield return null;
            }

            var finalProjection = session.Projection();
            Assert.That(session.DeliveredPassengerCount, Is.GreaterThan(0),
                "Residents must be delivered by the elevator after capacity intervention.");
            Assert.That(finalProjection.Elevators.Count, Is.EqualTo(2));
        }
    }
}
