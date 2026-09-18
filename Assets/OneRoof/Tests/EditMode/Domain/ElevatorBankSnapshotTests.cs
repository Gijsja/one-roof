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
            var timing = new ElevatorTimingConfig(2, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 4, timing);
            var bank = new ElevatorBank(0, 5, new[] { car });

            var p1 = new ElevatorPassenger(new EntityId(101), 1, 4);
            var p2 = new ElevatorPassenger(new EntityId(102), 2, 5);
            bank.EnqueuePassenger(p1);
            bank.EnqueuePassenger(p2);

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
            var timing = new ElevatorTimingConfig(2, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 4, timing);
            var bank = new ElevatorBank(0, 3, new[] { car });

            var p1 = new ElevatorPassenger(new EntityId(1), 0, 2);
            bank.EnqueuePassenger(p1);

            var snapshot1 = bank.Snapshot();
            Assert.That(snapshot1.QueuedCount, Is.EqualTo(1));

            // Mutate bank after taking snapshot
            var p2 = new ElevatorPassenger(new EntityId(2), 0, 3);
            bank.EnqueuePassenger(p2);

            var snapshot2 = bank.Snapshot();
            Assert.That(snapshot1.QueuedCount, Is.EqualTo(1), "Previous snapshot must not be affected by bank queue mutations.");
            Assert.That(snapshot2.QueuedCount, Is.EqualTo(2));
            Assert.That(snapshot1.GetQueueLength(0), Is.EqualTo(1));
            Assert.That(snapshot2.GetQueueLength(0), Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_PassengerLookups_LocateQueuedRidingAndDeliveredCorrectly()
        {
            var timing = new ElevatorTimingConfig(2, 1, 1);
            var car = new ElevatorCar(new EntityId(1), 0, 4, timing);
            var ridingPassenger = new ElevatorPassenger(new EntityId(20), 0, 3);
            car.RestoreState(floor: 0, phase: ElevatorCarPhase.Idle, direction: ElevatorDirection.None, timerRemaining: 0, passengers: new[] { ridingPassenger });
            var bank = new ElevatorBank(0, 4, new[] { car });

            var queuedPassenger = new ElevatorPassenger(new EntityId(10), 1, 4);
            bank.EnqueuePassenger(queuedPassenger);

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
