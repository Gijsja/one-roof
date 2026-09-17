using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class ElevatorBankSnapshotTests
    {
        [Test]
        public void Snapshot_CapturesElevatorBankState_Accurately()
        {
            var bank = new ElevatorBank(minFloor: 0, maxFloor: 5);
            var car = new ElevatorCar(carId: 1, capacity: 4, speedFloorsPerTick: 1f);
            bank.AddCar(car);

            var p1 = new ElevatorPassenger(new EntityId(101), 1, 4);
            var p2 = new ElevatorPassenger(new EntityId(102), 2, 5);
            bank.RequestRide(p1, requestTick: 10);
            bank.RequestRide(p2, requestTick: 10);

            var pDelivered = new ElevatorPassenger(new EntityId(103), 0, 3) { WaitTicks = 5 };
            bank.RestoreDeliveredPassengers(new[] { pDelivered });

            var snapshot = bank.Snapshot();

            Assert.That(snapshot.MinFloor, Is.EqualTo(0));
            Assert.That(snapshot.MaxFloor, Is.EqualTo(5));
            Assert.That(snapshot.Cars.Count, Is.EqualTo(1));
            Assert.That(snapshot.DeliveredCount, Is.EqualTo(1));
            Assert.That(snapshot.QueuedCount, Is.EqualTo(2));
            Assert.That(snapshot.GetQueueLength(1), Is.EqualTo(1));
            Assert.That(snapshot.GetQueueLength(2), Is.EqualTo(1));
            Assert.That(snapshot.GetQueueLength(3), Is.EqualTo(0));
            Assert.That(snapshot.AverageWaitTicks, Is.EqualTo(5f));
        }

        [Test]
        public void Snapshot_IsIndependentOfSubsequentMutations()
        {
            var bank = new ElevatorBank(minFloor: 0, maxFloor: 3);
            var p1 = new ElevatorPassenger(new EntityId(1), 0, 2);
            bank.RequestRide(p1, requestTick: 1);

            var snapshot1 = bank.Snapshot();
            Assert.That(snapshot1.QueuedCount, Is.EqualTo(1));

            // Mutate bank after taking snapshot
            var p2 = new ElevatorPassenger(new EntityId(2), 0, 3);
            bank.RequestRide(p2, requestTick: 2);

            var snapshot2 = bank.Snapshot();
            Assert.That(snapshot1.QueuedCount, Is.EqualTo(1), "Previous snapshot must not be affected by bank queue mutations.");
            Assert.That(snapshot2.QueuedCount, Is.EqualTo(2));
            Assert.That(snapshot1.GetQueueLength(0), Is.EqualTo(1));
            Assert.That(snapshot2.GetQueueLength(0), Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_PassengerLookups_LocateQueuedRidingAndDeliveredCorrectly()
        {
            var bank = new ElevatorBank(minFloor: 0, maxFloor: 4);
            var car = new ElevatorCar(carId: 1, capacity: 4, speedFloorsPerTick: 1f);
            var ridingPassenger = new ElevatorPassenger(new EntityId(20), 0, 3);
            car.AddPassenger(ridingPassenger);
            bank.AddCar(car);

            var queuedPassenger = new ElevatorPassenger(new EntityId(10), 1, 4);
            bank.RequestRide(queuedPassenger, requestTick: 1);

            var deliveredPassenger = new ElevatorPassenger(new EntityId(30), 0, 2);
            bank.RestoreDeliveredPassengers(new[] { deliveredPassenger });

            var snapshot = bank.Snapshot();

            Assert.That(snapshot.TryGetQueuedPassenger(new EntityId(10), out var foundQueued), Is.True);
            Assert.That(foundQueued.PersonId, Is.EqualTo(new EntityId(10)));
            Assert.That(snapshot.TryGetQueuedPassenger(new EntityId(999), out _), Is.False);

            Assert.That(snapshot.TryGetRidingPassenger(new EntityId(20), out var foundRiding), Is.True);
            Assert.That(foundRiding.PersonId, Is.EqualTo(new EntityId(20)));
            Assert.That(snapshot.TryGetRidingPassenger(new EntityId(999), out _), Is.False);

            Assert.That(snapshot.TryGetDeliveredPassenger(new EntityId(30), out var foundDelivered), Is.True);
            Assert.That(foundDelivered.PersonId, Is.EqualTo(new EntityId(30)));
            Assert.That(snapshot.TryGetDeliveredPassenger(new EntityId(999), out _), Is.False);
        }
    }
}
