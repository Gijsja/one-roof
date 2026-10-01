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
    public sealed class LeasingDemandSystemTests
    {
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

        [TestCase("commercial:office", 8, TestName = "OfficeZoning_LeasingDemand_EmploysNewResidentsAtOffice")]
        [TestCase("service:clinic", 12, TestName = "ServiceZoning_LeasingDemand_EmploysNewResidentsAtClinic")]
        public void WorkplaceZoning_LeasingDemand_EmploysResidentsAtAuthoredWorkplace(string contentId, int capacity)
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            var workplaceResult = sim.BuildRoom(new BuildRoomCommand(5, 5, 12, new ContentId(contentId), capacity));
            var apartmentResult = sim.BuildRoom(new BuildRoomCommand(5, -10, -5, new ContentId("residential:apartment"), 4));

            Assert.That(workplaceResult.Accepted, Is.True);
            Assert.That(apartmentResult.Accepted, Is.True);
            var workplace = sim.Topology.GetRoomsOnFloor(5).Single(room => room.ContentType.Value == contentId);

            for (var tick = 0; tick < 15; tick++) sim.AdvanceOneTick();

            Assert.That(sim.Population.Persons.Any(person => person.WorkplaceRoomId.Equals(workplace.Id)), Is.True,
                $"Expected demand-driven leasing to match a resident to the new {contentId} workplace.");
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

    }
}
