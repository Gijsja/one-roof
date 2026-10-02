using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitExecutionPersistenceTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void RoundTripPreservesReroutedLegMappingAndMovementProgress(bool queued, bool riding)
        {
            var planned = Route(101, TransitMode.Elevator, 3);
            var rerouted = Route(102, TransitMode.Walk, 15, queued || riding);
            var trip = new TripRecord(new EntityId(501), new EntityId(502),
                WorldLocation.InRoom(new EntityId(100)), WorldLocation.InRoom(new EntityId(200)),
                TripPurpose.Work, new Tick(17), planned);
            trip.Begin();
            trip.AddWaitTicks(4);
            var original = new TransitExecutionSystem();
            original.RestoreTripExecution(trip, 1, 7, queued, riding, 2, 4.25f);
            original.ActiveTrips[0].Route = rerouted;

            var save = original.ToSaveData();
            var restored = new TransitExecutionSystem();
            // New saves contain the route; restoration does not require a planner.
            restored.RestoreFromSaveData(save, null, null);
            Assert.That(restored.ActiveTripCount, Is.EqualTo(1));
            var execution = restored.ActiveTrips[0];
            Assert.That(execution.Route.OriginNodeId, Is.EqualTo(rerouted.OriginNodeId));
            Assert.That(execution.Route.Origin, Is.EqualTo(rerouted.Origin));
            Assert.That(execution.Route.Destination, Is.EqualTo(rerouted.Destination));
            Assert.That(execution.Route.Legs, Is.EqualTo(rerouted.Legs));
            Assert.That(execution.Trip.PlannedRoute.Legs, Is.EqualTo(planned.Legs));
            Assert.That(execution.CurrentLegIndex, Is.EqualTo(1));
            Assert.That(execution.LegRemainingTicks, Is.EqualTo(7));
            Assert.That(execution.IsQueuedInElevator, Is.EqualTo(queued));
            Assert.That(execution.IsRidingElevator, Is.EqualTo(riding));
            Assert.That(execution.CurrentFloor, Is.EqualTo(2));
            Assert.That(execution.CurrentX, Is.EqualTo(4.25f));
            Assert.That(execution.Trip.WaitTicks, Is.EqualTo(4));
            Assert.That(execution.Trip.State, Is.EqualTo(TripState.InProgress));
            Assert.That(save[0].routeDataVersion, Is.EqualTo(1));
        }

        [TestCase("version")]
        [TestCase("index")]
        [TestCase("edge")]
        [TestCase("chain")]
        public void InvalidRouteCheckpointIsRejected(string corruption)
        {
            var trip = new TripRecord(new EntityId(501), new EntityId(502),
                WorldLocation.InRoom(new EntityId(100)), WorldLocation.InRoom(new EntityId(200)),
                TripPurpose.Work, new Tick(17), Route(101, TransitMode.Walk, 3));
            trip.Begin();
            var original = new TransitExecutionSystem();
            original.RestoreTripExecution(trip, 0, 2, false, false, 0, 3f);
            var save = original.ToSaveData();
            if (corruption == "version") save[0].routeDataVersion = 2;
            if (corruption == "index") save[0].currentLegIndex = -1;
            if (corruption == "edge") save[0].executionRoute.legs[0].cost = -1;
            if (corruption == "chain") save[0].executionRoute.legs[1].fromNodeId = 999;
            Assert.Throws<System.ArgumentException>(() => new TransitExecutionSystem().RestoreFromSaveData(save, null, null));
        }

        [Test]
        public void LegacyTripWithoutRouteDataStillUsesTopologyPlanner()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            foreach (var origin in simulation.Topology.Rooms.Values)
            {
                foreach (var destination in simulation.Topology.Rooms.Values)
                {
                    if (origin.Id == destination.Id) continue;
                    var originNode = simulation.Topology.TransitGraph.GetNodeForLocation(WorldLocation.InRoom(origin.Id));
                    var destinationNode = simulation.Topology.TransitGraph.GetNodeForLocation(WorldLocation.InRoom(destination.Id));
                    if (originNode == null || destinationNode == null) continue;
                    var expected = simulation.Planner.FindRoute(originNode.Id, destinationNode.Id);
                    if (expected == null || expected.Legs.Count == 0) continue;
                    var restored = new TransitExecutionSystem();
                    restored.RestoreFromSaveData(new[] { new ActiveTripSaveData
                    {
                        tripId = 501, personId = 502, originRoomId = origin.Id.Value,
                        destinationRoomId = destination.Id.Value, state = (int)TripState.InProgress,
                        currentLegIndex = 0, legRemainingTicks = 3
                    } }, simulation.Topology, simulation.Planner);
                    Assert.That(restored.ActiveTripCount, Is.EqualTo(1));
                    Assert.That(restored.ActiveTrips[0].Route.Legs, Is.EqualTo(expected.Legs));
                    Assert.That(restored.ActiveTrips[0].LegRemainingTicks, Is.EqualTo(3));
                    return;
                }
            }
            Assert.Fail("Standard topology must contain a connected room pair.");
        }

        private static TransitRoute Route(int origin, TransitMode mode, int cost, bool elevatorFinalLeg = false)
        {
            return new TransitRoute(new EntityId(origin), new EntityId(203),
                new CellCoordinate(3, 0), new CellCoordinate(7, 2), new[]
                {
                    new TransitEdge(new EntityId(origin), new EntityId(202), cost, mode),
                    new TransitEdge(new EntityId(202), new EntityId(203), 9, elevatorFinalLeg ? TransitMode.Elevator : TransitMode.Walk)
                });
        }
    }
}
