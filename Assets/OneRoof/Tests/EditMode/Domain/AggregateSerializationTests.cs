using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class AggregateSerializationTests
    {
        [Test]
        public void BuildingTopologyState_ToSaveData_And_FromSaveData_RoundTripsAccurately()
        {
            var original = BuildingTopologyState.CreateWithFixture();
            var saveData = original.ToSaveData();

            Assert.That(saveData.floorSlabs.Length, Is.EqualTo(original.FloorCount));
            Assert.That(saveData.rooms.Length, Is.EqualTo(original.Rooms.Count));
            Assert.That(saveData.portals.Length, Is.EqualTo(original.Portals.Count));

            var restored = BuildingTopologyState.FromSaveData(saveData);

            Assert.That(restored.FloorCount, Is.EqualTo(original.FloorCount));
            Assert.That(restored.Rooms.Count, Is.EqualTo(original.Rooms.Count));
            Assert.That(restored.Portals.Count, Is.EqualTo(original.Portals.Count));
            Assert.That(restored.TransitGraph.Nodes.Count, Is.EqualTo(original.TransitGraph.Nodes.Count));
        }

        [Test]
        public void TowerEconomyState_ToSaveData_And_FromSaveData_RoundTripsAccurately()
        {
            var original = new TowerEconomyState(initialTreasury: 42000, sandboxMode: true, totalRevenue: 15000, totalExpenses: 3000);
            var saveData = original.ToSaveData();

            Assert.That(saveData.cashBalance, Is.EqualTo(42000));
            Assert.That(saveData.sandboxMode, Is.True);
            Assert.That(saveData.totalRevenue, Is.EqualTo(15000));
            Assert.That(saveData.totalExpenses, Is.EqualTo(3000));

            var restored = TowerEconomyState.FromSaveData(saveData);

            Assert.That(restored.CashBalance, Is.EqualTo(original.CashBalance));
            Assert.That(restored.SandboxMode, Is.EqualTo(original.SandboxMode));
            Assert.That(restored.TotalRevenue, Is.EqualTo(original.TotalRevenue));
            Assert.That(restored.TotalExpenses, Is.EqualTo(original.TotalExpenses));
        }

        [Test]
        public void PopulationState_ToSaveData_And_FromSaveData_RoundTripsAccurately()
        {
            var rng = new DeterministicRandomStream(42);
            var person = new PersonRecord(
                new EntityId(101),
                new EntityId(201),
                new EntityId(301),
                new EntityId(302),
                DailySchedule.Standard(new PersonTrait(PersonTraitKind.EarlyBird), rng),
                new[]
                {
                    new NeedState(NeedKind.Hunger, 0.8f),
                    new NeedState(NeedKind.Rest, 0.6f),
                    new NeedState(NeedKind.Social, 0.9f)
                },
                new[] { new PersonTrait(PersonTraitKind.EarlyBird) });
            person.UpdateActivity(ActivityKind.Dining);

            var household = new HouseholdRecord(
                new EntityId(201),
                new[] { new EntityId(101) },
                new EntityId(301),
                budget: 1500f,
                satisfaction: 0.85f);

            var original = new PopulationState(new[] { person }, new[] { household });
            var saveData = original.ToSaveData();

            Assert.That(saveData.persons.Length, Is.EqualTo(1));
            Assert.That(saveData.households.Length, Is.EqualTo(1));

            var restored = PopulationState.FromSaveData(saveData, rng);

            Assert.That(restored.PersonCount, Is.EqualTo(1));
            Assert.That(restored.HouseholdCount, Is.EqualTo(1));

            var restoredPerson = restored.GetPerson(new EntityId(101));
            Assert.That(restoredPerson.HouseholdId, Is.EqualTo(new EntityId(201)));
            Assert.That(restoredPerson.CurrentActivity, Is.EqualTo(ActivityKind.Dining));

            var restoredHousehold = restored.GetHousehold(new EntityId(201));
            Assert.That(restoredHousehold.Budget, Is.EqualTo(1500f));
            Assert.That(restoredHousehold.Satisfaction, Is.EqualTo(0.85f));
        }

        [Test]
        public void ElevatorBank_ToSaveData_And_FromSaveData_RoundTripsAccurately()
        {
            var car = new ElevatorCar(new EntityId(501), currentFloor: 2, capacity: 10);
            var rider = new ElevatorPassenger(new EntityId(101), originFloor: 0, destinationFloor: 3)
            {
                WaitTicks = 5,
                RideTicks = 2
            };
            car.RestoreState(2, ElevatorCarPhase.Moving, ElevatorDirection.Up, 3, new[] { rider });

            var bank = new ElevatorBank(minFloor: 0, maxFloor: 4, new[] { car });
            bank.EnqueuePassenger(new ElevatorPassenger(new EntityId(102), originFloor: 1, destinationFloor: 4)
            {
                WaitTicks = 12
            });
            bank.RestoreDeliveredPassengers(new[]
            {
                new ElevatorPassenger(new EntityId(103), originFloor: 0, destinationFloor: 2) { WaitTicks = 8, RideTicks = 4 }
            });

            var saveData = bank.ToSaveData();
            Assert.That(saveData.cars.Length, Is.EqualTo(1));
            Assert.That(saveData.queuedPassengers.Length, Is.EqualTo(1));
            Assert.That(saveData.deliveredPassengers.Length, Is.EqualTo(1));

            var restored = ElevatorBank.FromSaveData(saveData);

            Assert.That(restored.MinFloor, Is.EqualTo(bank.MinFloor));
            Assert.That(restored.MaxFloor, Is.EqualTo(bank.MaxFloor));
            Assert.That(restored.Cars.Count, Is.EqualTo(1));
            Assert.That(restored.Cars[0].CurrentFloor, Is.EqualTo(2));
            Assert.That(restored.Cars[0].Phase, Is.EqualTo(ElevatorCarPhase.Moving));
            Assert.That(restored.Cars[0].Passengers.Count, Is.EqualTo(1));
            Assert.That(restored.TotalQueuedCount, Is.EqualTo(1));
            Assert.That(restored.DeliveredCount, Is.EqualTo(1));
        }
    }
}
