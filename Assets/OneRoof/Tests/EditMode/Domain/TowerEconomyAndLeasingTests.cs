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
    public sealed class TowerEconomyAndLeasingTests
    {
        [Test]
        public void TowerEconomyState_CalculatesCostsAndDeductsCash()
        {
            var economy = new TowerEconomyState(initialTreasury: 10000);

            var slabBounds = new CellBounds(1, 0, 9); // width 10
            var slabCost = economy.CalculateFloorSlabCost(slabBounds);
            Assert.That(slabCost, Is.EqualTo(1000)); // 10 * 100

            var roomBounds = new CellBounds(1, 0, 3); // width 4
            var roomCost = economy.CalculateRoomCost(new ContentId("residential:apartment"), roomBounds);
            Assert.That(roomCost, Is.EqualTo(1000)); // 4 * 250

            var shaftCost = economy.CalculateElevatorShaftCost(floorSpan: 5, shaftWidth: 2);
            Assert.That(shaftCost, Is.EqualTo(5000)); // 5 * 2 * 500

            Assert.That(economy.CanAfford(5000), Is.True);
            var deducted = economy.TryDeduct(5000);
            Assert.That(deducted, Is.True);
            Assert.That(economy.CashBalance, Is.EqualTo(5000));
            Assert.That(economy.TotalExpenses, Is.EqualTo(5000));

            // Cannot afford 6000 with 5000 balance
            Assert.That(economy.CanAfford(6000), Is.False);
            Assert.That(economy.TryDeduct(6000), Is.False);
            Assert.That(economy.CashBalance, Is.EqualTo(5000));
        }

        [Test]
        public void TowerEconomyState_SandboxMode_BypassesFundsCheck()
        {
            var economy = new TowerEconomyState(initialTreasury: 500, sandboxMode: true);

            Assert.That(economy.CanAfford(100000), Is.True);
            var deducted = economy.TryDeduct(100000);
            Assert.That(deducted, Is.True);
            Assert.That(economy.CashBalance, Is.EqualTo(500)); // balance untouched in sandbox
            Assert.That(economy.TotalExpenses, Is.EqualTo(100000));
        }

        [Test]
        public void TowerEconomyState_RentCollection_AccumulatesRevenue()
        {
            var economy = new TowerEconomyState(initialTreasury: 1000);
            var topology = BuildingTopologyState.CreateWithFixture();
            var population = FiftyResidentFixture.Create();

            var rent = economy.ProcessRentCycle(topology, population);

            Assert.That(rent, Is.GreaterThan(0));
            Assert.That(economy.CashBalance, Is.EqualTo(1000 + rent));
            Assert.That(economy.TotalRevenue, Is.EqualTo(rent));
        }

        [Test]
        public void TowerSimulation_BuildRoom_InsufficientFunds_RejectsCommand()
        {
            var economy = new TowerEconomyState(initialTreasury: 200); // Only $200
            var sim = TowerSimulation.CreateStandardFiveFloor(economy);

            // Cost for width 4 residential room is $1000
            var cmd = new BuildRoomCommand(
                floor: 1,
                minX: 14,
                maxX: 17,
                contentType: new ContentId("residential:apartment"),
                capacity: 4);

            var result = sim.BuildRoom(cmd);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("economy:insufficient_funds")));
        }

        [Test]
        public void TowerSimulation_BuildRoom_SufficientFunds_DeductsAndBuilds()
        {
            var economy = new TowerEconomyState(initialTreasury: 10000);
            var sim = TowerSimulation.CreateStandardFiveFloor(economy);

            var cmd = new BuildRoomCommand(
                floor: 1,
                minX: 14,
                maxX: 17,
                contentType: new ContentId("residential:apartment"),
                capacity: 4);

            var result = sim.BuildRoom(cmd);

            Assert.That(result.Accepted, Is.True);
            Assert.That(economy.CashBalance, Is.EqualTo(9000)); // 10000 - 1000
            Assert.That(sim.Topology.GetRoomsOnFloor(1).Count, Is.EqualTo(6));
        }

        [Test]
        public void LeasingDemandSystem_VacantApartment_SpawnsNewHouseholdAndPerson()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var initialCount = sim.ResidentCount;

            // Build a new floor 5 and an apartment (clear of the reserved shaft column).
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            var buildResult = sim.BuildRoom(new BuildRoomCommand(
                floor: 5,
                minX: 2,
                maxX: 7,
                contentType: new ContentId("residential:apartment"),
                capacity: 4));

            Assert.That(buildResult.Accepted, Is.True);

            // Run simulation ticks to trigger periodic leasing evaluation
            for (var tick = 0; tick < 15; tick++)
            {
                sim.AdvanceOneTick();
            }

            // New resident should have leased the apartment
            Assert.That(sim.ResidentCount, Is.GreaterThan(initialCount));
        }

        [Test]
        public void DemolishRoom_RefundsFiftyPercentSalvageCash_ToTreasury()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));

            // Build an office room (8 cells * 350 = 2800)
            var buildResult = sim.BuildRoom(new BuildRoomCommand(5, 5, 12, new ContentId("commercial:office"), 8));
            Assert.That(buildResult.Accepted, Is.True);

            var roomsF5 = sim.Topology.GetRoomsOnFloor(5);
            Room office = null;
            for (var i = 0; i < roomsF5.Count; i++)
            {
                if (roomsF5[i].ContentType.Value == "commercial:office")
                {
                    office = roomsF5[i];
                    break;
                }
            }
            Assert.That(office, Is.Not.Null);

            var cost = sim.Economy.CalculateRoomCost(office.ContentType, office.Bounds);
            Assert.That(cost, Is.EqualTo(8 * TowerEconomyState.CostPerCommercialCell));

            var cashBefore = sim.Economy.CashBalance;
            var demolishResult = sim.DemolishRoom(new DemolishRoomCommand(office.Id));

            Assert.That(demolishResult.Accepted, Is.True);
            Assert.That(sim.Economy.CashBalance, Is.EqualTo(cashBefore + (cost / 2)));
            Assert.That(sim.Topology.TryGetRoom(office.Id, out _), Is.False);
        }

        [Test]
        public void DemolishRoom_OccupiedApartment_RejectsEvenWhenForced()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var household = sim.Population.Households[0];
            var homeRoomId = household.HomeRoomId;

            // Attempt unforced demolish of occupied apartment
            var unforcedResult = sim.DemolishRoom(new DemolishRoomCommand(homeRoomId, force: false));
            Assert.That(unforcedResult.Accepted, Is.False);
            Assert.That(unforcedResult.Rejections[0].Code, Is.EqualTo(new ContentId("demolish:occupied")));

            // Force cannot bypass the state-integrity guard.
            var forcedResult = sim.DemolishRoom(new DemolishRoomCommand(homeRoomId, force: true));
            Assert.That(forcedResult.Accepted, Is.False);
            Assert.That(forcedResult.Rejections[0].Code, Is.EqualTo(new ContentId("demolish:occupied")));
            Assert.That(sim.Topology.TryGetRoom(homeRoomId, out _), Is.True);
            Assert.That(household.HomeRoomId, Is.EqualTo(homeRoomId));
        }

        [Test]
        public void OfficeZoning_LeasingDemand_EmploysNewResidentsAtOffice()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // Build floor 5 slab, an office room, and a vacant apartment
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            var officeResult = sim.BuildRoom(new BuildRoomCommand(5, 5, 12, new ContentId("commercial:office"), 8));
            var aptResult = sim.BuildRoom(new BuildRoomCommand(5, -10, -5, new ContentId("residential:apartment"), 4));

            Assert.That(officeResult.Accepted, Is.True);
            Assert.That(aptResult.Accepted, Is.True);

            var roomsF5 = sim.Topology.GetRoomsOnFloor(5);
            Room office = null;
            for (var i = 0; i < roomsF5.Count; i++)
            {
                if (roomsF5[i].ContentType.Value == "commercial:office")
                {
                    office = roomsF5[i];
                    break;
                }
            }
            Assert.That(office, Is.Not.Null);

            // Advance simulation to trigger leasing evaluation
            for (var tick = 0; tick < 15; tick++)
            {
                sim.AdvanceOneTick();
            }

            // Find any resident whose workplace is the new office
            var employedAtOffice = false;
            foreach (var person in sim.Population.Persons)
            {
                if (person.WorkplaceRoomId.Equals(office.Id))
                {
                    employedAtOffice = true;
                    break;
                }
            }

            Assert.That(employedAtOffice, Is.True, "Expected at least one newly leased resident to be employed at the office.");
        }

        [Test]
        public void ServiceZoning_LeasingDemand_EmploysNewResidentsAtClinic()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            var clinicResult = sim.BuildRoom(new BuildRoomCommand(5, 5, 12, new ContentId("service:clinic"), 12));
            var apartmentResult = sim.BuildRoom(new BuildRoomCommand(5, -10, -5, new ContentId("residential:apartment"), 4));

            Assert.That(clinicResult.Accepted, Is.True);
            Assert.That(apartmentResult.Accepted, Is.True);

            Room clinic = null;
            var rooms = sim.Topology.GetRoomsOnFloor(5);
            for (var i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].ContentType.Value == "service:clinic")
                {
                    clinic = rooms[i];
                    break;
                }
            }
            Assert.That(clinic, Is.Not.Null);

            for (var tick = 0; tick < 15; tick++) sim.AdvanceOneTick();

            Assert.That(sim.Population.Persons.Any(person => person.WorkplaceRoomId.Equals(clinic.Id)), Is.True,
                "Expected demand-driven leasing to match a resident to the new clinic workplace.");
        }

        [Test]
        public void LeasingDemand_DoesNotAssignWorkplaceRoomWithoutPayingBusiness()
        {
            var topology = new BuildingTopologyState();
            var home = new Room(new EntityId(101), new ContentId("residential:apartment"),
                new CellBounds(0, 0, 3), System.Array.Empty<EntityId>(), 4);
            var unpaidWorkplace = new Room(new EntityId(102), new ContentId("workplace:office"),
                new CellBounds(0, 5, 8), System.Array.Empty<EntityId>(), 4);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 10) },
                new[] { home, unpaidWorkplace }, System.Array.Empty<Portal>());
            var population = new PopulationState(null, null);
            var nextEntityId = 200;

            var arrivals = new LeasingDemandSystem().EvaluateLeasingDemand(topology, population,
                null, null, new Tick(0), ref nextEntityId);

            Assert.That(arrivals, Has.Count.EqualTo(1));
            Assert.That(arrivals[0].HomeRoomId, Is.EqualTo(home.Id));
            Assert.That(arrivals[0].WorkplaceLocation.IsOutside, Is.True,
                "A workplace room without a business ledger cannot pay a resident's wages.");
        }

        [Test]
        public void LeasingDemand_OnlyMatchesFundedProductiveSlots_ThenUsesOutsideWork()
        {
            var topology = new BuildingTopologyState();
            var rooms = new[]
            {
                new Room(new EntityId(101), new ContentId("residential:apartment"),
                    new CellBounds(0, 0, 3), System.Array.Empty<EntityId>(), 4),
                new Room(new EntityId(102), new ContentId("residential:apartment"),
                    new CellBounds(0, 4, 7), System.Array.Empty<EntityId>(), 4),
                new Room(new EntityId(103), new ContentId("commercial:diner"),
                    new CellBounds(0, 8, 11), System.Array.Empty<EntityId>(), 4)
            };
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 11) }, rooms,
                System.Array.Empty<Portal>());
            var population = new PopulationState(null, null);
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(201), rooms[2].Id, rooms[2].ContentType,
                    cashBalance: BusinessRecord.OpeningCapital)
            });
            var nextEntityId = 300;

            var first = new LeasingDemandSystem().EvaluateLeasingDemand(topology, population,
                null, null, new Tick(10), ref nextEntityId, businesses);

            Assert.That(first, Has.Count.EqualTo(2));
            Assert.That(first.All(person => person.WorkplaceRoomId.Equals(rooms[2].Id)), Is.True,
                "A four-cell diner has two productive positions at full demand.");
            Assert.That(first.All(person => !person.WorkplaceLocation.IsOutside), Is.True);

            var extraHome = new Room(new EntityId(104), new ContentId("residential:apartment"),
                new CellBounds(1, 0, 3), System.Array.Empty<EntityId>(), 4);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 11), new CellBounds(1, 0, 3) },
                new[] { rooms[0], rooms[1], rooms[2], extraHome }, System.Array.Empty<Portal>());

            var overflow = new LeasingDemandSystem().EvaluateLeasingDemand(topology, population,
                null, null, new Tick(20), ref nextEntityId, businesses);

            Assert.That(overflow, Has.Count.EqualTo(1));
            Assert.That(overflow[0].WorkplaceLocation.IsOutside, Is.True,
                "An excess roster slot must not strand a household without wages.");
            Assert.That(overflow[0].WorkplaceRoomId.IsValid, Is.False);
        }

        [Test]
        public void LeasingDemand_DoesNotMatchTenantWithoutPayrollReserve()
        {
            var topology = new BuildingTopologyState();
            var home = new Room(new EntityId(110), new ContentId("residential:apartment"),
                new CellBounds(0, 0, 3), System.Array.Empty<EntityId>(), 4);
            var office = new Room(new EntityId(111), new ContentId("commercial:office"),
                new CellBounds(0, 4, 7), System.Array.Empty<EntityId>(), 4);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 7) },
                new[] { home, office }, System.Array.Empty<Portal>());
            var population = new PopulationState(null, null);
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(112), office.Id, office.ContentType,
                    cashBalance: BusinessRecord.InsolvencyThreshold + 30)
            });
            var nextEntityId = 200;

            var arrivals = new LeasingDemandSystem().EvaluateLeasingDemand(topology, population,
                null, null, new Tick(10), ref nextEntityId, businesses);

            Assert.That(arrivals, Has.Count.EqualTo(1));
            Assert.That(arrivals[0].WorkplaceLocation.IsOutside, Is.True);
        }

        [Test]
        public void LeasingDemand_ReLeasedBusinessHiresExistingOutsideWorkersWithoutVacantHomes()
        {
            var topology = new BuildingTopologyState();
            var home = new Room(new EntityId(120), new ContentId("residential:apartment"),
                new CellBounds(0, 0, 3), System.Array.Empty<EntityId>(), 4);
            var office = new Room(new EntityId(121), new ContentId("commercial:office"),
                new CellBounds(0, 4, 7), System.Array.Empty<EntityId>(), 2);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 7) },
                new[] { office, home }, System.Array.Empty<Portal>());
            var sample = FiftyResidentFixture.Create().Persons[0];
            var workers = new[]
            {
                new PersonRecord(new EntityId(203), new EntityId(204), home.Id, default,
                    sample.Schedule, sample.Needs, sample.Traits, worksOutside: true),
                new PersonRecord(new EntityId(201), new EntityId(204), home.Id, default,
                    sample.Schedule, sample.Needs, sample.Traits, worksOutside: true),
                new PersonRecord(new EntityId(202), new EntityId(204), home.Id, default,
                    sample.Schedule, sample.Needs, sample.Traits, worksOutside: true)
            };
            var household = new HouseholdRecord(new EntityId(204), workers.Select(person => person.Id).ToArray(),
                home.Id, .5f, .8f);
            var population = new PopulationState(workers, new[] { household });
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(205), office.Id, office.ContentType,
                    cashBalance: BusinessRecord.OpeningCapital)
            });
            var leasing = new LeasingDemandSystem();

            var matched = leasing.MatchExistingOutsideWorkers(topology, population, businesses);

            Assert.That(matched, Is.EqualTo(2));
            Assert.That(workers.Single(person => person.Id.Value == 201).WorkplaceRoomId, Is.EqualTo(office.Id));
            Assert.That(workers.Single(person => person.Id.Value == 202).WorkplaceRoomId, Is.EqualTo(office.Id));
            Assert.That(workers.Single(person => person.Id.Value == 203).WorkplaceLocation.IsOutside, Is.True);
            Assert.That(leasing.MatchExistingOutsideWorkers(topology, population, businesses), Is.Zero,
                "A stable full roster should not churn on the next reconciliation.");
        }

        [Test]
        public void LeasingDemand_RehiringSkipsTenantWithoutPayrollReserve()
        {
            var topology = new BuildingTopologyState();
            var office = new Room(new EntityId(131), new ContentId("commercial:office"),
                new CellBounds(0, 0, 3), System.Array.Empty<EntityId>(), 2);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 3) },
                new[] { office }, System.Array.Empty<Portal>());
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(211), new EntityId(212), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            var population = new PopulationState(new[] { worker }, null);
            var businesses = new BusinessState(new[]
            {
                new BusinessRecord(new EntityId(213), office.Id, office.ContentType,
                    cashBalance: BusinessRecord.InsolvencyThreshold + 30)
            });

            var matched = new LeasingDemandSystem().MatchExistingOutsideWorkers(topology, population, businesses);

            Assert.That(matched, Is.Zero);
            Assert.That(worker.WorkplaceLocation.IsOutside, Is.True);
        }

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
