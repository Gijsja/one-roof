using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerSimulationTests
    {
        [Test]
        public void CreateStandardFiveFloor_InitializesWith50ResidentsAndOneCar()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.ResidentCount, Is.EqualTo(50));
            Assert.That(sim.Topology.FloorCount, Is.EqualTo(5));
            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));
            Assert.That(sim.CurrentTick, Is.EqualTo(0));
            Assert.That(sim.ActiveTripCount, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceOneTick_AdvancesClock()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            sim.AdvanceOneTick();

            Assert.That(sim.CurrentTick, Is.EqualTo(1));
        }

        [Test]
        public void MorningRushHour_ResidentsFormElevatorQueuesAndBoardCars()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // Run until tick 35 (morning commute period from DailySchedule)
            for (var tick = 0; tick < 35; tick++)
            {
                sim.AdvanceOneTick();
            }

            // Morning commute should generate trips for workers
            Assert.That(sim.ActiveTripCount + sim.ElevatorBank.DeliveredCount, Is.GreaterThan(0));

            // Queues or riders should appear in the elevator bank
            var totalTransitTraffic = sim.TotalQueuedElevatorPassengers + sim.ElevatorBank.Cars[0].Passengers.Count + sim.ElevatorBank.DeliveredCount;
            Assert.That(totalTransitTraffic, Is.GreaterThan(0));
        }

        [Test]
        public void AddingElevatorCar_IncreasesCarCountAndServicingCapacity()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));

            sim.AddElevatorCar(capacity: 10, startingFloor: 2);

            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(2));
        }

        [Test]
        public void TransitExecutionSystem_CompletesLocalTripImmediately()
        {
            var clock = new SimulationClock(new Tick(10));
            var topology = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();
            var car = new ElevatorCar(new EntityId(501), 0, 10);
            var bank = new ElevatorBank(0, 4, new[] { car });
            var transit = new TransitExecutionSystem();

            var room0 = topology.GetRoomsOnFloor(0)[0];
            var trip = new TripRecord(
                new EntityId(900),
                new EntityId(1),
                room0.Id,
                room0.Id,
                TripPurpose.Work,
                new Tick(10),
                null);

            transit.SubmitTrip(trip, topology, new Tick(10), population);

            Assert.That(trip.State, Is.EqualTo(TripState.Completed));
            Assert.That(transit.ActiveTripCount, Is.EqualTo(0));
        }

        [Test]
        public void TransitExecutionSystem_AdvancesWalkLegAndElevatorLeg()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // Advance through morning rush hour to let residents complete full commutes
            for (var tick = 0; tick < 70; tick++)
            {
                sim.AdvanceOneTick();
            }

            // Elevator bank should have delivered passengers
            Assert.That(sim.ElevatorBank.DeliveredCount, Is.GreaterThan(0));
        }

        [Test]
        public void AllResidentsInitiallyLiveInTheirHomeRooms()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // At tick 0 before commute starts, all residents must be InRoom in their home apartment
            for (var p = 0; p < sim.ResidentCount; p++)
            {
                var person = sim.Population.Persons[p];
                var spatial = sim.GetResidentPosition(person.Id);

                Assert.That(spatial.Phase, Is.EqualTo(ResidentMovementPhase.InRoom));
                Assert.That(spatial.RoomId, Is.EqualTo(person.HomeRoomId));
                Assert.That(spatial.Floor, Is.GreaterThan(0), "Home apartments in FiveFloor fixture are on floors 1-4.");
                Assert.That(spatial.X, Is.Not.Zero);
            }
        }

        [Test]
        public void ActiveCommuteTransitionsThroughWalkingQueuedAndRidingPhases()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            var observedWalking = false;
            var observedQueued = false;
            var observedRiding = false;

            for (var tick = 0; tick < 60; tick++)
            {
                sim.AdvanceOneTick();

                for (var p = 0; p < sim.ResidentCount; p++)
                {
                    var person = sim.Population.Persons[p];
                    var spatial = sim.GetResidentPosition(person.Id);

                    if (spatial.Phase == ResidentMovementPhase.Walking) observedWalking = true;
                    if (spatial.Phase == ResidentMovementPhase.Queued) observedQueued = true;
                    if (spatial.Phase == ResidentMovementPhase.Riding) observedRiding = true;
                }
            }

            Assert.That(observedWalking, Is.True, "Should observe residents walking along corridors.");
            Assert.That(observedQueued, Is.True, "Should observe residents queued at elevator.");
            Assert.That(observedRiding, Is.True, "Should observe residents riding elevator car.");
        }

        [Test]
        public void InitialState_ElevatorIsIdleAndNoPassengersQueued()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            Assert.That(sim.TotalQueuedElevatorPassengers, Is.EqualTo(0));
            Assert.That(sim.ElevatorBank.Cars[0].Phase, Is.EqualTo(ElevatorCarPhase.Idle));
            Assert.That(sim.ElevatorBank.Cars[0].Passengers.Count, Is.EqualTo(0));
            Assert.That(sim.ElevatorBank.Cars[0].HasRequests, Is.False);
        }

        [Test]
        public void BuildingRoom_DynamicallySynchronizesPlannerAndProducesElevatorRoute()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(50000));

            // Build a second diner on Floor 0 at unoccupied cells [4..13]
            var buildCmd = new OneRoof.Domain.Commands.BuildRoomCommand(
                0,
                4,
                13,
                new ContentId("commercial:diner"),
                capacity: 10);

            var result = sim.BuildRoom(buildCmd);
            Assert.That(result.Accepted, Is.True);

            // Locate newly allocated room
            Room newDiner = null;
            foreach (var r in sim.Topology.GetRoomsOnFloor(0))
            {
                if (r.Bounds.MinX == 4 && r.Bounds.MaxX == 13)
                {
                    newDiner = r;
                    break;
                }
            }
            Assert.That(newDiner, Is.Not.Null);

            // Plan route from a residential apartment on Floor 3 to the new Diner on Floor 0
            var floor3Rooms = sim.Topology.GetRoomsOnFloor(3);
            var aptFloor3 = floor3Rooms[1]; // apartment on floor 3

            var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(aptFloor3.Id);
            var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(newDiner.Id);

            Assert.That(originNode, Is.Not.Null);
            Assert.That(destNode, Is.Not.Null);

            var route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Legs.Count, Is.EqualTo(3));
            Assert.That(route.Legs[0].Mode, Is.EqualTo(TransitMode.Walk));
            Assert.That(route.Legs[1].Mode, Is.EqualTo(TransitMode.Elevator));
            Assert.That(route.Legs[2].Mode, Is.EqualTo(TransitMode.Walk));
        }

        [Test]
        public void UnroutableTrip_IsCancelledWithoutTeleportingResident()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var person = sim.Population.Persons[0];
            var initialLocation = person.CurrentRoomId;

            // Submit trip with null route to unreachable room ID 9999
            var unroutableTrip = new TripRecord(
                new EntityId(8888),
                person.Id,
                initialLocation,
                new EntityId(9999),
                TripPurpose.Work,
                sim.Clock.CurrentTick,
                plannedRoute: null);

            sim.Transit.SubmitTrip(unroutableTrip, sim.Topology, sim.Clock.CurrentTick, sim.Population);

            Assert.That(unroutableTrip.State, Is.EqualTo(TripState.Cancelled));
            Assert.That(person.CurrentRoomId, Is.EqualTo(initialLocation), "Resident must not teleport to destination on routing failure.");
            Assert.That(sim.Transit.ActiveTripCount, Is.EqualTo(0));
        }
    }
}
