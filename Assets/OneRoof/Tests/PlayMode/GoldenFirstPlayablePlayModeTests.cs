using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Transit;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class GoldenFirstPlayablePlayModeTests
    {
        [UnityTest]
        public IEnumerator GoldenFirstPlayable_CapacityIntervention_DeliversAllResidents()
        {
            var session = new TransitPrototypeSession();

            // Initial projection: 50 residents queued, 1 elevator
            var initial = session.Projection();
            Assert.That(initial.QueueLength, Is.EqualTo(50));
            Assert.That(initial.Elevators.Count, Is.EqualTo(1));

            // Step 10 ticks with 1 car
            for (var i = 0; i < 10; i++)
            {
                session.AdvanceOneTick();
                yield return null;
            }

            var midway1 = session.Projection();
            Assert.That(midway1.Tick, Is.EqualTo(10));
            Assert.That(midway1.QueueLength, Is.GreaterThan(0));

            // Player intervention: Add elevator capacity
            session.AddCapacity();
            var postIntervention = session.Projection();
            Assert.That(postIntervention.Elevators.Count, Is.EqualTo(2));

            // Run until completion or max 120 ticks
            for (var i = 0; i < 120 && session.Projection().ArrivedCount < 50; i++)
            {
                session.AdvanceOneTick();
                yield return null;
            }

            var finalProjection = session.Projection();
            Assert.That(finalProjection.ArrivedCount, Is.EqualTo(50),
                "All 50 residents must arrive after capacity intervention.");
            Assert.That(finalProjection.QueueLength, Is.EqualTo(0));
            Assert.That(finalProjection.AverageWaitTicks, Is.GreaterThan(0f));
        }
    }
}
