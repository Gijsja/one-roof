using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class BusinessCycleTests
    {
        [Test]
        public void BusinessCycle_ReconcilesLeaseStaffAndPaysWagesFromOpeningCapital()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 50);

            for (var tick = 0; tick < 50; tick++) sim.AdvanceOneTick();

            var diner = sim.Businesses.Businesses.FirstOrDefault(business => business.ContentType.Value == "commercial:diner");
            Assert.That(diner, Is.Not.Null);
            Assert.That(diner.EmployeeIds.Count, Is.EqualTo(10),
                "Only demand-supported diner positions should retain tower jobs.");
            Assert.That(diner.LastCustomerRevenue, Is.GreaterThan(0));
            Assert.That(diner.LastWages, Is.GreaterThan(0),
                "Opening investment should fund the staff who serve customers.");
            Assert.That(diner.WageArrears, Is.False);
            Assert.That(diner.CashBalance, Is.EqualTo(BusinessRecord.OpeningCapital - diner.LastWages + diner.LastCustomerRevenue
                - diner.LastRentPaid - diner.LastTaxPaid - diner.LastOperatingCost));
        }

        [TestCase("commercial:office")]
        [TestCase("commercial:diner")]
        public void BusinessCycle_UnpaidEmployeeCannotGenerateRevenue(string contentType)
        {
            var person = new PersonRecord(new EntityId(301), new EntityId(302), new EntityId(303),
                new EntityId(304), DailySchedule.Standard(new PersonTrait(PersonTraitKind.EarlyBird),
                    new DeterministicRandomStream(301)), null, null);
            var household = new HouseholdRecord(new EntityId(302), new[] { person.Id },
                person.HomeRoomId, .5f, .8f, cashBalance: 100);
            var population = new PopulationState(new[] { person }, new[] { household });
            var room = new Room(person.WorkplaceRoomId, new ContentId(contentType),
                new CellBounds(0, 0, 0), System.Array.Empty<EntityId>(), 1);
            var business = new BusinessRecord(new EntityId(305), room.Id, room.ContentType,
                cashBalance: -90);
            business.ReconcileEmployees(population, room.Capacity);
            var cashBefore = household.CashBalance;

            business.ProcessCycle(population, room, null, 1f, PolicyDecreeState.Default,
                new[] { household }, walkInSpendBudget: 5);

            Assert.That(business.EmployeeIds, Has.Count.EqualTo(1));
            Assert.That(business.WageArrears, Is.True);
            Assert.That(business.LastWages, Is.Zero);
            Assert.That(business.LastCustomerRevenue, Is.Zero);
            Assert.That(business.LastContractRevenue, Is.Zero);
            Assert.That(household.CashBalance, Is.EqualTo(cashBefore));
            Assert.That(household.DailyServiceSpend, Is.Zero);
        }

        [Test]
        public void Diner_WithPaidStaffAndHealthyDemand_EarnsPositiveDailyMargin()
        {
            var householdId = new EntityId(402);
            var homeId = new EntityId(403);
            var roomId = new EntityId(404);
            var persons = Enumerable.Range(0, 26).Select(index => new PersonRecord(
                new EntityId(500 + index), householdId, homeId, roomId,
                DailySchedule.Standard(new PersonTrait(PersonTraitKind.EarlyBird),
                    new DeterministicRandomStream((ulong)(500 + index))), null, null,
                worksOutside: index >= 2)).ToArray();
            var household = new HouseholdRecord(householdId, persons.Select(person => person.Id).ToArray(),
                homeId, .5f, .8f, cashBalance: 1000);
            var population = new PopulationState(persons, new[] { household });
            var room = new Room(roomId, new ContentId("commercial:diner"), new CellBounds(0, 0, 1),
                System.Array.Empty<EntityId>(), 4);
            var business = new BusinessRecord(new EntityId(405), room.Id, room.ContentType,
                cashBalance: BusinessRecord.OpeningCapital);
            var treasury = new TowerEconomyState(initialTreasury: 0);
            business.ReconcileEmployees(population, room.Capacity);

            business.ProcessCycle(population, room, treasury, 1f, PolicyDecreeState.Default,
                new[] { household }, walkInSpendBudget: 26 * 5);

            Assert.That(business.EmployeeIds, Has.Count.EqualTo(2));
            Assert.That(business.WageArrears, Is.False);
            Assert.That(business.LastWages, Is.EqualTo(60));
            Assert.That(business.LastCustomerRevenue, Is.GreaterThan(0));
            Assert.That(business.LastCustomerRevenue - business.LastWages - business.LastRentPaid
                - business.LastTaxPaid - business.LastOperatingCost, Is.GreaterThan(0),
                "Paid staff and healthy household demand should sustain a small diner.");
            Assert.That(household.CashBalance, Is.EqualTo(1000 + business.LastWages - business.LastCustomerRevenue));
        }

        [Test]
        public void BusinessState_SaveRoundTrip_PreservesTenantFinancesAndMembership()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 50);
            for (var tick = 0; tick < 50; tick++) sim.AdvanceOneTick();

            var save = sim.ExportSaveData();
            var restored = TowerSimulation.RestoreFromSaveData(save);

            Assert.That(restored.Businesses.Businesses.Count, Is.EqualTo(sim.Businesses.Businesses.Count));
            var original = sim.Businesses.Businesses[0];
            var restoredBusiness = restored.Businesses.Businesses[0];
            Assert.That(restoredBusiness.RoomId, Is.EqualTo(original.RoomId));
            Assert.That(restoredBusiness.CashBalance, Is.EqualTo(original.CashBalance));
            Assert.That(restoredBusiness.EmployeeIds.Select(id => id.Value), Is.EqualTo(original.EmployeeIds.Select(id => id.Value)));
        }

        [Test]
        public void BusinessCycle_PrefersCompletedRelevantSpecialistsBeforeIdFallback()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var specialist = sim.Population.Persons[sim.Population.Persons.Count - 1];
            specialist.Specialization.AdvanceTowards(SpecialistRole.Service, 1f);

            var businesses = new BusinessState();
            var nextBusinessId = 9000;
            businesses.Advance(sim.Topology, sim.Population, ref nextBusinessId);

            var diner = businesses.Businesses.First(business => business.ContentType.Value == "commercial:diner");
            Assert.That(diner.EmployeeIds[0], Is.EqualTo(specialist.Id),
                "Completed Service specialists should be recruited before lower-ID generalists.");
        }

        [Test]
        public void BusinessCycle_UnstaffedTenantBecomesInsolventAfterSustainedOperatingLosses()
        {
            var population = FiftyResidentFixture.Create();
            var business = new BusinessRecord(new EntityId(9000), new EntityId(9001), new ContentId("service:clinic"));

            business.ProcessCycle(population);
            business.ProcessCycle(population);
            business.ProcessCycle(population);

            Assert.That(business.CashBalance, Is.EqualTo(-105));
            Assert.That(business.IsInsolvent, Is.True);
        }
    }
}
