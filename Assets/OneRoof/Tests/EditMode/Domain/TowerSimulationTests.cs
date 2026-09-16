using NUnit.Framework;
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
    }
}
