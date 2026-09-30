using NUnit.Framework;
using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ElevatorBankTests
    {
        [TestCase(0, int.MaxValue)]
        [TestCase(int.MinValue, 0)]
        public void ConstructorRejectsUnsupportedFloorRanges(int minFloor, int maxFloor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ElevatorBank(minFloor, maxFloor, null));
        }

        [Test]
        public void ExpandFloorRangeRejectsUnsupportedRangeWithoutChangingBank()
        {
            var bank = new ElevatorBank(0, 4, null);
            Assert.Throws<ArgumentOutOfRangeException>(() => bank.ExpandFloorRange(0, int.MaxValue));
            Assert.That(bank.MaxFloor, Is.EqualTo(4));
        }

        [Test]
        public void WaitingPassengerBoardsWhenCarOpensDoors()
        {
            var timing = new ElevatorTimingConfig(2, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 8, timing);
            var bank = new ElevatorBank(0, 4, new[] { car });

            var passenger = new ElevatorPassenger(new EntityId(101), 0, 2);
            bank.EnqueuePassenger(passenger);

            Assert.That(bank.GetQueueLength(0), Is.EqualTo(1));

            // Tick until doors open at Floor 0:
            // Idle -> DoorsOpening (1 tick) -> OpenLoading (boards)
            bank.Advance(new Tick(1)); // DoorsOpening
            bank.Advance(new Tick(2)); // OpenLoading -> boards!

            Assert.That(bank.GetQueueLength(0), Is.Zero);
            Assert.That(car.Passengers.Count, Is.EqualTo(1));
            Assert.That(car.Passengers[0].PersonId, Is.EqualTo(new EntityId(101)));
        }

        [Test]
        public void CapacityConstraintIsStrictlyEnforced()
        {
            var timing = new ElevatorTimingConfig(2, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 4, timing);
            var bank = new ElevatorBank(0, 4, new[] { car });

            // Enqueue 7 passengers into car with capacity 4
            for (var i = 1; i <= 7; i++)
            {
                bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(i), 0, 3));
            }

            Assert.That(bank.GetQueueLength(0), Is.EqualTo(7));

            // Advance until doors open at Floor 0
            bank.Advance(new Tick(1)); // DoorsOpening
            bank.Advance(new Tick(2)); // OpenLoading -> boards up to capacity

            Assert.That(car.Passengers.Count, Is.EqualTo(4));
            Assert.That(bank.GetQueueLength(0), Is.EqualTo(3)); // 3 passengers still waiting!
        }

        [Test]
        public void PassengersDeliveredToRespectiveDestinations()
        {
            var timing = new ElevatorTimingConfig(1, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 8, timing);
            var bank = new ElevatorBank(0, 4, new[] { car });

            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(1), 0, 1));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(2), 0, 2));

            // Advance simulation until both passengers are delivered
            for (var tick = 1; tick <= 50 && bank.DeliveredCount < 2; tick++)
            {
                bank.Advance(new Tick(tick));
            }

            Assert.That(bank.DeliveredCount, Is.EqualTo(2));
            Assert.That(bank.TotalQueuedCount, Is.Zero);
            Assert.That(car.Passengers.Count, Is.Zero);
        }

        [Test]
        public void TimingFollowsConfiguredTicksPrecisely()
        {
            // 3 ticks to travel 1 floor, 2 ticks door cycle, 2 ticks dwell
            var timing = new ElevatorTimingConfig(3, 2, 2);
            var car = new ElevatorCar(new EntityId(1), 0, 8, timing);

            // Car starts idle at Floor 0, request Floor 1
            car.RequestFloor(1);
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.Idle));

            // Tick 1: Starts moving to Floor 1
            car.Tick();
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.Moving));
            Assert.That(car.Direction, Is.EqualTo(ElevatorDirection.Up));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(2)); // 3 - 1

            // Tick 2: Moving
            car.Tick();
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.Moving));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(1));

            // Tick 3: Arrives at Floor 1, starts opening doors
            car.Tick();
            Assert.That(car.CurrentFloor, Is.EqualTo(1));
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.DoorsOpening));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(2));

            // Tick 4: DoorsOpening continues
            car.Tick();
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.DoorsOpening));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(1));

            // Tick 5: Doors reach OpenLoading!
            car.Tick();
            Assert.That(car.Phase, Is.EqualTo(ElevatorCarPhase.OpenLoading));
            Assert.That(car.TimerTicksRemaining, Is.EqualTo(2));
        }

        [Test]
        public void MultiCarBankServicesConcurrentFloorCalls()
        {
            var timing = new ElevatorTimingConfig(1, 1, 1);
            var car1 = new ElevatorCar(new EntityId(1), 0, 4, timing);
            var car2 = new ElevatorCar(new EntityId(2), 4, 4, timing);
            var bank = new ElevatorBank(0, 4, new[] { car1, car2 });

            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(101), 0, 2));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(102), 4, 2));

            for (var tick = 1; tick <= 40 && bank.DeliveredCount < 2; tick++)
            {
                bank.Advance(new Tick(tick));
            }

            Assert.That(bank.DeliveredCount, Is.EqualTo(2));
        }

        [Test]
        public void EnqueuePassengerWithDestinationOutsideBankRangeThrows()
        {
            var timing = new ElevatorTimingConfig(1, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 8, timing);
            var bank = new ElevatorBank(0, 4, new[] { car });

            // Floor 5 is outside [0..4]
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(999), 0, 5)));

            // Floor -1 is also outside [0..4]
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(998), 0, -1)));
        }

        [TestCase(1, 5, 8)]
        [TestCase(2, 5, 8)]
        [TestCase(3, 4, 9)]
        [TestCase(4, 4, 9)]
        [TestCase(5, 3, 10)]
        public void SetTierAppliesBankWideTimingAndCapacity(int tier, int expectedTravelTicks, int expectedCapacity)
        {
            var first = new ElevatorCar(new EntityId(2), 0, 6);
            var second = new ElevatorCar(new EntityId(1), 0, 10);
            var bank = new ElevatorBank(0, 4, new[] { first, second });

            bank.SetTier(tier);

            Assert.That(bank.Tier, Is.EqualTo(tier));
            Assert.That(first.Timing.FloorTravelTicks, Is.EqualTo(expectedTravelTicks));
            Assert.That(second.Timing.FloorTravelTicks, Is.EqualTo(expectedTravelTicks));
            Assert.That(first.Timing.DoorCycleTicks, Is.EqualTo(2));
            Assert.That(first.Timing.DwellTicks, Is.EqualTo(2));
            Assert.That(first.Capacity, Is.EqualTo(expectedCapacity));
            Assert.That(second.Capacity, Is.EqualTo(10));
        }

        [Test]
        public void SetTierRejectsValuesOutsideOneThroughFive()
        {
            var bank = new ElevatorBank(0, 2, null);
            Assert.Throws<ArgumentOutOfRangeException>(() => bank.SetTier(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => bank.SetTier(6));
            Assert.That(bank.Tier, Is.EqualTo(1));
        }

        [Test]
        public void EqualDispatchCostsUseStableCarIdTieBreakRegardlessOfCarOrder()
        {
            var highId = new ElevatorCar(new EntityId(20), 0, 8);
            var lowId = new ElevatorCar(new EntityId(10), 0, 8);
            var bank = new ElevatorBank(0, 3, new[] { highId, lowId });
            bank.SetTier(2);

            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(100), 2, 3));

            Assert.That(lowId.HasTargetFloor(2), Is.True);
            Assert.That(highId.HasTargetFloor(2), Is.False);
        }

        [Test]
        public void EtaDispatchCanChooseFartherCarAlreadyTravelingTowardCall()
        {
            var nearerButWrongWay = new ElevatorCar(new EntityId(1), 2, 8);
            nearerButWrongWay.RestoreState(2, ElevatorCarPhase.Moving, ElevatorDirection.Down, 1, null);
            var fartherButAligned = new ElevatorCar(new EntityId(2), 7, 8);
            fartherButAligned.RestoreState(7, ElevatorCarPhase.Moving, ElevatorDirection.Down, 1, null);
            var bank = new ElevatorBank(0, 8, new[] { nearerButWrongWay, fartherButAligned });
            bank.SetTier(2);

            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(101), 4, 5));

            Assert.That(nearerButWrongWay.HasTargetFloor(4), Is.False);
            Assert.That(fartherButAligned.HasTargetFloor(4), Is.True);
        }

        [TestCase(3, 4)]
        [TestCase(5, 3)]
        public void HigherTransportTierReducesMeasuredSingleFloorTripTicks(int tier, int expectedTravelTicks)
        {
            var bank = CreateSingleTripBank(tier, out var car);
            var deliveredAt = AdvanceUntilDelivered(bank);

            Assert.That(car.Timing.FloorTravelTicks, Is.EqualTo(expectedTravelTicks));
            Assert.That(deliveredAt, Is.LessThan(AdvanceUntilDelivered(CreateSingleTripBank(1, out _))));
        }

        private static ElevatorBank CreateSingleTripBank(int tier, out ElevatorCar car)
        {
            car = new ElevatorCar(new EntityId(1), 0, 8);
            var bank = new ElevatorBank(0, 1, new[] { car });
            bank.SetTier(tier);
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(200), 0, 1));
            return bank;
        }

        private static int AdvanceUntilDelivered(ElevatorBank bank)
        {
            for (var tick = 1; tick <= 30; tick++)
            {
                bank.Advance(new Tick(tick));
                if (bank.DeliveredCount > 0) return tick;
            }
            Assert.Fail("Passenger was not delivered within 30 ticks.");
            return int.MaxValue;
        }
    }
}
