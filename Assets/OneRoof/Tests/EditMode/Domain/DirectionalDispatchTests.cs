using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class DirectionalDispatchTests
    {
        [TestCase(0, 4)]
        [TestCase(0, 29)]
        [TestCase(0, 39)]
        [TestCase(-5, 4)]
        public void OneLandingPeakUsesEveryCarWithoutHoldingAnIdleReserve(int min, int max)
        {
            var cars = new[]
            {
                new ElevatorCar(new EntityId(1), min, 8, new ElevatorTimingConfig(1, 1, 1)),
                new ElevatorCar(new EntityId(2), min, 8, new ElevatorTimingConfig(1, 1, 1)),
                new ElevatorCar(new EntityId(3), min, 8, new ElevatorTimingConfig(1, 1, 1))
            };
            var bank = new ElevatorBank(min, max, cars);
            for (var i = 0; i < 24; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100 + i), min, max));
            bank.Advance(new Tick(1));
            Assert.That(bank.TotalQueuedCount, Is.Zero);
            Assert.That(cars[0].Passengers.Count + cars[1].Passengers.Count + cars[2].Passengers.Count + bank.DeliveredCount, Is.EqualTo(24));
        }

        [Test]
        public void OppositeDirectionQueueReceivesASeparatePickupCommitment()
        {
            var first = new ElevatorCar(new EntityId(1), 2, 8, new ElevatorTimingConfig(1, 1, 1));
            var second = new ElevatorCar(new EntityId(2), 2, 8, new ElevatorTimingConfig(1, 1, 1));
            var bank = new ElevatorBank(0, 4, new[] { first, second });
            for (var i = 0; i < 8; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100 + i), 2, 4));
            for (var i = 0; i < 8; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(200 + i), 2, 0));
            Assert.That(first.Phase, Is.EqualTo(ElevatorCarPhase.DoorsOpening));
            Assert.That(second.Phase, Is.EqualTo(ElevatorCarPhase.DoorsOpening));
            for (var tick = 1; tick <= 2; tick++) bank.Advance(new Tick(tick));
            Assert.That(bank.TotalQueuedCount, Is.Zero);
            Assert.That(first.Passengers.Count, Is.EqualTo(8));
            Assert.That(second.Passengers.Count, Is.EqualTo(8));
            Assert.That(first.Passengers[0].DestinationFloor, Is.Not.EqualTo(second.Passengers[0].DestinationFloor));
        }

        [TestCase(0, 4)]
        [TestCase(0, 29)]
        [TestCase(0, 39)]
        [TestCase(-5, 4)]
        public void EndpointReservationUsesCompatibleIntermediateDemandAtConfiguredTop(int min, int max)
        {
            var car = new ElevatorCar(new EntityId(1), max, 8, new ElevatorTimingConfig(1, 1, 1));
            var bank = new ElevatorBank(min, max, new[] { car });
            for (var i = 0; i < 4; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100 + i), max - 1, min));
            for (var i = 0; i < 8; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(200 + i), max, min));
            for (var tick = 1; tick <= 2; tick++) bank.Advance(new Tick(tick));
            Assert.That(car.Passengers.Count, Is.EqualTo(4));
            Assert.That(bank.GetQueueLength(max), Is.EqualTo(4));
        }

        [Test]
        public void NewCallsDoNotReassignAnAcceptedPickup()
        {
            var first = new ElevatorCar(new EntityId(1), 0, 8, new ElevatorTimingConfig(3, 1, 1));
            var second = new ElevatorCar(new EntityId(2), 10, 8, new ElevatorTimingConfig(3, 1, 1));
            var bank = new ElevatorBank(0, 10, new[] { first, second });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100), 3, 0));
            Assert.That(first.HasTargetFloor(3), Is.True);
            bank.Advance(new Tick(1));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(200), 8, 10));
            Assert.That(first.HasTargetFloor(3), Is.True);
            Assert.That(second.HasTargetFloor(3), Is.False);
        }

        [Test]
        public void FullCarEstimateIncludesSeatReleaseBeforeTheNextPickup()
        {
            var empty = new ElevatorBank(0, 10, new[] { new ElevatorCar(new EntityId(1), 0, 2) });
            var fullCar = new ElevatorCar(new EntityId(1), 0, 2);
            fullCar.RestoreState(0, ElevatorCarPhase.OpenLoading, ElevatorDirection.Up, 0, null);
            fullCar.Board(new ElevatorPassenger(new EntityId(100), 0, 10));
            fullCar.Board(new ElevatorPassenger(new EntityId(101), 0, 10));
            var full = new ElevatorBank(0, 10, new[] { fullCar });
            Assert.That(full.EstimateArrivalTicks(1, 5), Is.GreaterThan(empty.EstimateArrivalTicks(1, 5)));
            Assert.That(new ElevatorBank(0, 10, null).EstimateArrivalTicks(1, 5), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void AlreadyOpeningPickupIsEstimatedFromItsRemainingDoorWork()
        {
            var bank = new ElevatorBank(0, 11, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            bank.SetTier(3);
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100), 0, 11));
            // 5 opening + 10 loading + 5 closing + 88 riding + 5 opening = 113 seconds.
            Assert.That(bank.EstimateArrivalTicks(0, 11), Is.EqualTo(2));
        }

        [Test]
        public void FifoHeadKeepsFasterElevatorWhenTwentyThreeLaterCallersWaitBehindIt()
        {
            var topology = TransitChoiceAndClockTests.CreateTopology(29);
            var trip = TransitChoiceAndClockTests.Trip(topology, 4, 0); trip.Begin();
            var transit = new TransitExecutionSystem();
            transit.RestoreTripExecution(trip, 0, 1, true, false, 4, 0);
            var bank = new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            bank.EnqueuePassenger(new ElevatorPassenger(trip.PersonId, 4, 0) { WaitTicks = 14 });
            for (var i = 0; i < 23; i++) bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(11000 + i), 4, 0) { WaitTicks = 14 });
            Assert.That(bank.EstimateArrivalTicks(4, 0), Is.EqualTo(33), "A newcomer joins behind three batches of existing demand.");
            Assert.That(bank.EstimateArrivalTicks(4, 0, trip.PersonId), Is.EqualTo(2), "The held FIFO head needs no additional batch.");
            transit.Advance(new Tick(15), topology, bank, null);
            Assert.That(bank.IsPassengerInQueue(trip.PersonId, out _) || bank.IsPassengerInCar(trip.PersonId, out _), Is.True,
                "The FIFO head must retain elevator service, including boarding during this minute.");
            Assert.That(transit.ActiveTrips[0].Route.Legs, Has.Some.Matches<TransitEdge>(edge => edge.Mode == TransitMode.Elevator));
            transit.Advance(new Tick(16), topology, bank, null);
            Assert.That(trip.CompletionTick.Value.Value, Is.EqualTo(16));
            Assert.That(trip.WaitTicks, Is.EqualTo(15));
        }

        [Test]
        public void CompatibleBoardingKeepsFifoWithinEachDirection()
        {
            var car = new ElevatorCar(new EntityId(1), 2, 2, new ElevatorTimingConfig(1, 1, 1));
            var bank = new ElevatorBank(0, 4, new[] { car });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(101), 2, 4));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(201), 2, 0));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(102), 2, 4));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(202), 2, 0));
            for (var tick = 1; tick <= 2; tick++) bank.Advance(new Tick(tick));
            Assert.That(car.Passengers[0].PersonId.Value, Is.EqualTo(101));
            Assert.That(car.Passengers[1].PersonId.Value, Is.EqualTo(102));
            Assert.That(bank.Snapshot().GetFloorQueue(2)[0].PersonId.Value, Is.EqualTo(201));
            Assert.That(bank.Snapshot().GetFloorQueue(2)[1].PersonId.Value, Is.EqualTo(202));
        }

        [Test]
        public void OldestCallGetsServiceUnderContinuingFreshOppositeDemand()
        {
            var bank = new ElevatorBank(-5, 39, new[] { new ElevatorCar(new EntityId(1), 0, 8) });
            var old = new ElevatorPassenger(new EntityId(100), 20, -5) { WaitTicks = 60 };
            bank.EnqueuePassenger(old);
            var delivered = false;
            for (var tick = 1; tick <= 120; tick++)
            {
                bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(1000 + tick), 0, 39));
                bank.Advance(new Tick(tick));
                if (bank.TryTakeDeliveredPassenger(old.PersonId, out _)) { delivered = true; break; }
            }
            Assert.That(delivered, Is.True);
            Assert.That(old.WaitTicks, Is.LessThanOrEqualTo(80));
        }

        [Test]
        public void LegalTierOneThroughFiveCommandsImproveTheSamePeakWithoutChangingDemand()
        {
            var arrival = new int[5];
            for (var tier = 1; tier <= 5; tier++)
            {
                var simulation = TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(1000000));
                for (var upgrade = 2; upgrade <= tier; upgrade++)
                {
                    var result = simulation.ExecuteCommand(new UpgradeElevatorTierCommand(upgrade));
                    Assert.That(result.Accepted, Is.True, result.Reason);
                }
                var bank = simulation.ElevatorBank;
                bank.ExpandFloorRange(0, 29);
                for (var resident = 0; resident < 30; resident++)
                    bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(10000 + resident), 0, 29));
                for (var tick = 1; tick <= 120; tick++)
                {
                    bank.Advance(new Tick(tick));
                    if (bank.DeliveredCount == 30) { arrival[tier - 1] = tick; break; }
                }
                Assert.That(bank.DeliveredCount, Is.EqualTo(30));
                Assert.That(bank.Cars[0].Capacity, Is.EqualTo(10), "The same existing seats must be retained at every tier.");
            }
            Assert.That(arrival[2], Is.LessThan(arrival[0]));
            Assert.That(arrival[4], Is.LessThan(arrival[2]));
        }
    }
}
