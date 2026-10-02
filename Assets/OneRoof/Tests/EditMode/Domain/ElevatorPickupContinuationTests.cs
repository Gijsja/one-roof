using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ElevatorPickupContinuationTests
    {
        [Test]
        public void QueueReconstructionPreservesPickupTargetsAndMovingTimer()
        {
            var bank = new ElevatorBank(0, 5, new[]
            {
                new ElevatorCar(new EntityId(1), 0, 4),
                new ElevatorCar(new EntityId(2), 5, 4)
            });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(101), 3, 0));
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(102), 1, 5));
            for (var tick = 1; tick <= 3; tick++) bank.Advance(new Tick(tick));
            var snapshot = bank.ToSaveData();
            var restored = ElevatorBank.FromSaveData(snapshot);
            for (var tick = 4; tick <= 100; tick++)
            {
                for (var i = 0; i < bank.Cars.Count; i++)
                {
                    Assert.That(restored.Cars[i].TargetFloors, Is.EqualTo(bank.Cars[i].TargetFloors), $"tick {tick}, car {i}");
                    Assert.That(restored.Cars[i].TimerTicksRemaining, Is.EqualTo(bank.Cars[i].TimerTicksRemaining));
                    Assert.That(restored.Cars[i].CurrentFloor, Is.EqualTo(bank.Cars[i].CurrentFloor));
                    Assert.That(restored.Cars[i].Phase, Is.EqualTo(bank.Cars[i].Phase));
                    Assert.That(restored.Cars[i].Direction, Is.EqualTo(bank.Cars[i].Direction));
                    Assert.That(restored.Cars[i].Passengers.Count, Is.EqualTo(bank.Cars[i].Passengers.Count));
                }
                Assert.That(restored.TotalQueuedCount, Is.EqualTo(bank.TotalQueuedCount));
                bank.Advance(new Tick(tick));
                restored.Advance(new Tick(tick));
            }
        }
    }
}
