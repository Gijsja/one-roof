using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class GoldenFirstPlayablePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenFirstPlayable_CapacityIntervention_DeliversResidentsToTheirWorkplace()
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

            // Steward intervention: Add elevator capacity
            Assert.That(session.AddCapacity().Accepted, Is.True);
            var postIntervention = session.Projection();
            Assert.That(postIntervention.Elevators.Count, Is.EqualTo(2));

            // Observe real workplace arrivals during the compact first-playable routine.
            var workplaceArrivals = new HashSet<int>();
            var deliveredBeforeIntervention = session.DeliveredPassengerCount;
            for (var i = 0; i < 120; i++)
            {
                session.AdvanceOneTick();
                foreach (var resident in session.Projection().Residents)
                {
                    Assert.That(session.TryGetResidentInspection(new EntityId(resident.ResidentId), out var facts), Is.True);
                    if (resident.Status == TransitResidentStatus.InRoom && resident.RoomId == facts.WorkplaceRoomId)
                        workplaceArrivals.Add(resident.ResidentId);
                }
                yield return null;
            }

            var finalProjection = session.Projection();
            Assert.That(session.DeliveredPassengerCount, Is.GreaterThan(deliveredBeforeIntervention),
                "Residents must be delivered by the elevator after capacity intervention.");
            Assert.That(finalProjection.Elevators.Count, Is.EqualTo(2));
            Assert.That(finalProjection.Residents.Count, Is.EqualTo(initial.Residents.Count));
            Assert.That(workplaceArrivals.Count, Is.GreaterThan(0),
                "At least one resident must arrive at their assigned workplace, beyond exiting an elevator.");
        }
    }
}
