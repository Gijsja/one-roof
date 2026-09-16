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

            // Build a new floor 5 and an apartment
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            var buildResult = sim.BuildRoom(new BuildRoomCommand(
                floor: 5,
                minX: 0,
                maxX: 5,
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
    }
}
