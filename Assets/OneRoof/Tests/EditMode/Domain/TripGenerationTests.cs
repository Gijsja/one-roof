using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TripGenerationTests
    {
        // ── Morning commute — core acceptance criterion ────────────────────────

        [Test]
        public void MorningCommuteProducesFiftyWorkTrips()
        {
            var fixture = new TripDemandFixture(seed: 1);

            // Sweep the full day; each person must produce exactly one Work trip.
            var workTrips = fixture.CollectAllWorkTrips();

            Assert.That(workTrips.Count, Is.EqualTo(FiftyResidentFixture.TotalResidents),
                "Every resident must generate exactly one Work trip during the day cycle.");
        }

        [Test]
        public void AllWorkTripsTargetTheWorkplaceRoom()
        {
            var fixture   = new TripDemandFixture(seed: 2);
            var workTrips = fixture.CollectAllWorkTrips();

            foreach (var trip in workTrips)
            {
                var person = fixture.Population.GetPerson(trip.PersonId);
                Assert.That(trip.DestinationRoomId, Is.EqualTo(person.WorkplaceRoomId),
                    $"Trip {trip.Id} for person {trip.PersonId}: destination should be the workplace room.");
            }
        }

        // ── Route validity ────────────────────────────────────────────────────

        [Test]
        public void WorkTripsForUpperFloorResidentsHaveValidRoutes()
        {
            var fixture   = new TripDemandFixture(seed: 3);
            var workTrips = fixture.CollectAllWorkTrips();

            foreach (var trip in workTrips)
            {
                // Residents living above Floor 0 must have a route that requires vertical transit.
                var person   = fixture.Population.GetPerson(trip.PersonId);
                var homeRoom = fixture.Topology.TryGetRoom(person.HomeRoomId, out var r) ? r : null;
                Assert.That(homeRoom, Is.Not.Null);

                if (homeRoom.Floor > 0)
                {
                    Assert.That(trip.PlannedRoute, Is.Not.Null,
                        $"Trip {trip.Id}: person {trip.PersonId} lives on floor {homeRoom.Floor} and must have a route.");
                    Assert.That(trip.PlannedRoute.TotalCost, Is.GreaterThan(0),
                        $"Trip {trip.Id}: route cost should be positive for cross-floor travel.");
                    Assert.That(trip.PlannedRoute.RequiresVerticalTransit, Is.True,
                        $"Trip {trip.Id}: cross-floor trip must pass through the elevator.");
                }
            }
        }

        // ── Determinism ───────────────────────────────────────────────────────

        [Test]
        public void SameSeedProducesIdenticalTripRouteCosts()
        {
            const uint seed = 7777;

            var fixtureA = new TripDemandFixture(seed);
            var fixtureB = new TripDemandFixture(seed);

            var tripsA = fixtureA.CollectAllWorkTrips();
            var tripsB = fixtureB.CollectAllWorkTrips();

            Assert.That(tripsA.Count, Is.EqualTo(tripsB.Count));

            for (var i = 0; i < tripsA.Count; i++)
            {
                var costA = tripsA[i].PlannedRoute?.TotalCost ?? 0;
                var costB = tripsB[i].PlannedRoute?.TotalCost ?? 0;
                Assert.That(costA, Is.EqualTo(costB),
                    $"Trip index {i}: route cost differs between identical seeds ({costA} vs {costB}).");
            }
        }

        // ── Mid-block silence ─────────────────────────────────────────────────

        [Test]
        public void NoTripsGeneratedAtMidBlockTick()
        {
            var fixture = new TripDemandFixture(seed: 4);

            // Find a tick deep inside every person's Sleep block (tick 60 → 61, well before any
            // work transition which is at minimum ~300 ticks in).
            var midBlock  = new Tick(60);
            var nextTick  = new Tick(61);

            var trips = fixture.Generator.GenerateTripsForTick(midBlock, nextTick, fixture.Population);

            Assert.That(trips.Count, Is.EqualTo(0),
                "No trips should be generated at ticks where no block boundary occurs.");
        }

        // ── Trip state defaults ───────────────────────────────────────────────

        [Test]
        public void NewlyGeneratedTripsDefaultToPlannedState()
        {
            var fixture   = new TripDemandFixture(seed: 5);
            var workTrips = fixture.CollectAllWorkTrips();

            foreach (var trip in workTrips)
            {
                Assert.That(trip.State, Is.EqualTo(TripState.Planned),
                    $"Trip {trip.Id} should default to Planned state.");
                Assert.That(trip.WaitTicks, Is.EqualTo(0),
                    $"Trip {trip.Id} should start with zero wait ticks.");
            }
        }

        // ── TripRecord mutation ───────────────────────────────────────────────

        [Test]
        public void TripRecordMutationTransitionsStateCorrectly()
        {
            var fixture   = new TripDemandFixture(seed: 6);
            var workTrips = fixture.CollectAllWorkTrips();

            Assert.That(workTrips.Count, Is.GreaterThan(0), "Need at least one trip for mutation test.");
            var trip = workTrips[0];

            // Planned → InProgress
            trip.Begin();
            Assert.That(trip.State, Is.EqualTo(TripState.InProgress));

            // Accumulate wait ticks
            trip.AddWaitTicks(5);
            trip.AddWaitTicks(3);
            Assert.That(trip.WaitTicks, Is.EqualTo(8));

            // InProgress → Completed
            var arrivalTick = new Tick(500);
            trip.Complete(arrivalTick);
            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
            Assert.That(trip.CompletionTick, Is.EqualTo(arrivalTick));

            // Cannot Begin again after Completed
            Assert.That(() => trip.Begin(), Throws.InvalidOperationException);
            // Cannot Cancel after Completed
            Assert.That(() => trip.Cancel(), Throws.InvalidOperationException);
        }

        [Test]
        public void CancelTransitionsFromPlannedOrInProgress()
        {
            var fixture   = new TripDemandFixture(seed: 8);
            var workTrips = fixture.CollectAllWorkTrips();
            Assert.That(workTrips.Count, Is.GreaterThan(1));

            // Cancel from Planned
            var tripA = workTrips[0];
            tripA.Cancel();
            Assert.That(tripA.State, Is.EqualTo(TripState.Cancelled));

            // Cancel from InProgress
            var tripB = workTrips[1];
            tripB.Begin();
            tripB.Cancel();
            Assert.That(tripB.State, Is.EqualTo(TripState.Cancelled));
        }
    }
}
