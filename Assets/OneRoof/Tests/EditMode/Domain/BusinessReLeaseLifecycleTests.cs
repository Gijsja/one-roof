using System;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class BusinessReLeaseLifecycleTests
    {
        [Test]
        public void InsolventTenant_IsReplacedOnlyAfterSevenSettlements_WithExplicitCashFlows()
        {
            var room = CreateRoom();
            var population = CreateHireablePopulation();
            var topology = CreateTopology(room);
            var oldTenant = new BusinessRecord(new EntityId(50), room.Id, room.ContentType,
                cashBalance: -101, isInsolvent: true, arrearsDays: 6);
            var businesses = new BusinessState(new[] { oldTenant });
            var nextId = 100;

            businesses.Advance(topology, population, ref nextId);
            Assert.That(businesses.Businesses[0], Is.SameAs(oldTenant));
            Assert.That(businesses.TotalReLeaseCount, Is.Zero);

            oldTenant.ProcessCycle(population);
            Assert.That(oldTenant.ArrearsDays, Is.EqualTo(7));
            Assert.That(oldTenant.CashBalance, Is.EqualTo(-101));
            var cashBefore = oldTenant.CashBalance;

            businesses.Advance(topology, population, ref nextId);

            Assert.That(businesses.Businesses, Has.Count.EqualTo(1));
            var replacement = businesses.Businesses[0];
            Assert.That(replacement.Id.Value, Is.EqualTo(100));
            Assert.That(replacement.RoomId, Is.EqualTo(room.Id));
            Assert.That(replacement.CashBalance, Is.EqualTo(BusinessRecord.OpeningCapital));
            Assert.That(replacement.IsInsolvent, Is.False);
            Assert.That(nextId, Is.EqualTo(101));
            Assert.That(businesses.TotalReLeaseCount, Is.EqualTo(1));
            Assert.That(businesses.TotalReLeaseOpeningCapitalSource, Is.EqualTo(BusinessRecord.OpeningCapital));
            Assert.That(businesses.TotalReLeaseDebtWriteOffSource, Is.EqualTo(101));
            Assert.That(businesses.TotalReLeaseCashRetiredSink, Is.Zero);
            Assert.That(replacement.CashBalance - cashBefore,
                Is.EqualTo(businesses.TotalReLeaseOpeningCapitalSource + businesses.TotalReLeaseDebtWriteOffSource));

            businesses.Advance(topology, population, ref nextId);
            Assert.That(businesses.TotalReLeaseCount, Is.EqualTo(1), "Reconciliation must not recapitalize a healthy tenant.");
            businesses.ProcessBusinessCycle(population);
            Assert.That(oldTenant.CashBalance, Is.EqualTo(cashBefore),
                "Only the active replacement may accrue later operating costs.");
            Assert.That(replacement.CashBalance, Is.EqualTo(BusinessRecord.OpeningCapital - BusinessRecord.OperatingCostPerDay));
        }

        [Test]
        public void PersistentOutputShock_KeepsFailedRoomVacantWithoutCost_ThenRecoveryReleasesIt()
        {
            var room = CreateRoom();
            var population = CreateHireablePopulation();
            var oldTenant = new BusinessRecord(new EntityId(50), room.Id, room.ContentType,
                cashBalance: -101, isInsolvent: true, arrearsDays: BusinessRecord.ReLeaseAfterInsolventDays);
            var businesses = new BusinessState(new[] { oldTenant });
            var topology = CreateTopology(room);
            var nextId = 100;

            for (var day = 0; day < 30; day++)
            {
                businesses.Advance(topology, population, ref nextId, occupancyFactor: .7f);
                businesses.ProcessBusinessCycle(population);
            }

            Assert.That(businesses.Businesses[0], Is.SameAs(oldTenant));
            Assert.That(oldTenant.CashBalance, Is.EqualTo(-101));
            Assert.That(oldTenant.LastOperatingCost, Is.Zero);
            Assert.That(businesses.TotalReLeaseCount, Is.Zero);
            Assert.That(nextId, Is.EqualTo(100));

            businesses.Advance(topology, population, ref nextId, occupancyFactor: 1f);

            Assert.That(businesses.Businesses[0].Id.Value, Is.EqualTo(100));
            Assert.That(businesses.TotalReLeaseCount, Is.EqualTo(1));
        }

        [Test]
        public void InconsistentPositiveCashInsolventSave_RetiresCashAsExplicitSink()
        {
            var room = CreateRoom();
            var population = CreateHireablePopulation();
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(50), room.Id, room.ContentType,
                    cashBalance: 25, isInsolvent: true, arrearsDays: BusinessRecord.ReLeaseAfterInsolventDays)
            });
            var nextId = 100;

            businesses.Advance(CreateTopology(room), population, ref nextId);

            Assert.That(businesses.TotalReLeaseCashRetiredSink, Is.EqualTo(25));
            Assert.That(businesses.Businesses[0].CashBalance - 25,
                Is.EqualTo(businesses.TotalReLeaseOpeningCapitalSource - businesses.TotalReLeaseCashRetiredSink));
        }

        [Test]
        public void LifecycleTotals_RoundTrip_AndLegacySaveDefaultsToZero()
        {
            var room = CreateRoom();
            var population = CreateHireablePopulation();
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(50), room.Id, room.ContentType,
                    cashBalance: -120, isInsolvent: true, arrearsDays: BusinessRecord.ReLeaseAfterInsolventDays)
            });
            var nextId = 100;
            businesses.Advance(CreateTopology(room), population, ref nextId);

            var restored = BusinessState.FromSaveData(businesses.ToSaveData());
            restored.RestoreLifecycleTotals(businesses.ToLifecycleSaveData());
            Assert.That(restored.TotalReLeaseCount, Is.EqualTo(1));
            Assert.That(restored.TotalReLeaseOpeningCapitalSource, Is.EqualTo(500));
            Assert.That(restored.TotalReLeaseDebtWriteOffSource, Is.EqualTo(120));
            Assert.That(restored.Businesses[0].Id, Is.EqualTo(businesses.Businesses[0].Id));

            var legacy = BusinessState.FromSaveData(businesses.ToSaveData());
            legacy.RestoreLifecycleTotals(null);
            Assert.That(legacy.TotalReLeaseCount, Is.Zero);
            Assert.That(legacy.TotalReLeaseOpeningCapitalSource, Is.Zero);
        }

        [Test]
        public void ProductiveStaffCapacity_TracksWalkInDemandAndInsolvency()
        {
            var room = new Room(new EntityId(10), new ContentId("commercial:diner"),
                new CellBounds(0, 2, 5), Array.Empty<EntityId>(), 4);
            var healthy = new BusinessRecord(new EntityId(50), room.Id, room.ContentType, 500);
            var insolvent = new BusinessRecord(new EntityId(51), room.Id, room.ContentType,
                -101, isInsolvent: true);

            Assert.That(healthy.GetProductiveStaffCapacity(room, 1f), Is.EqualTo(2));
            Assert.That(healthy.GetProductiveStaffCapacity(room, 0.5f), Is.EqualTo(1));
            Assert.That(healthy.GetProductiveStaffCapacity(room, 0f), Is.Zero);
            Assert.That(insolvent.GetProductiveStaffCapacity(room, 1f), Is.Zero);
        }

        private static Room CreateRoom() => new Room(new EntityId(10),
            new ContentId("commercial:office"), new CellBounds(0, 2, 5),
            Array.Empty<EntityId>(), 4);

        private static PopulationState CreateHireablePopulation()
        {
            var persons = new PersonRecord[4];
            var memberIds = new EntityId[4];
            for (var i = 0; i < persons.Length; i++)
            {
                var id = new EntityId(1000 + i);
                memberIds[i] = id;
                var trait = new PersonTrait(PersonTraitKind.EarlyBird);
                persons[i] = new PersonRecord(id, new EntityId(2000), new EntityId(3000),
                    default, DailySchedule.Standard(trait, new DeterministicRandomStream((ulong)id.Value)),
                    null, new[] { trait }, worksOutside: true);
            }
            var household = new HouseholdRecord(new EntityId(2000), memberIds,
                new EntityId(3000), .5f, .8f, cashBalance: 1000);
            return new PopulationState(persons, new[] { household });
        }

        private static BuildingTopologyState CreateTopology(Room room)
        {
            var topology = new BuildingTopologyState();
            topology.RestoreFromData(new[] { new CellBounds(0, -10, 10) },
                new[] { room }, Array.Empty<Portal>());
            return topology;
        }
    }
}
