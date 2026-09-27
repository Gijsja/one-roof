using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Scrutiny;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Underground;

namespace OneRoof.Tests.EditMode.Domain
{
    public sealed class UndergroundOperationsTests
    {
        [Test]
        public void ConnectedRoomsEmployResidentsAndPostExplicitOutsideTransfers()
        {
            var layout = CreateLayout();
            var state = new UndergroundOperationsState();
            var population = FiftyResidentFixture.Create();
            var treasury = new TowerEconomyState();
            var outside = new OutsideMarketState();
            var scrutiny = new ScrutinyState();

            state.AdvanceDaily(layout, population, treasury, outside, scrutiny, 1440);

            Assert.That(layout.Rooms.Count, Is.EqualTo(4));
            Assert.That(state.RoomByResident.Count, Is.EqualTo(7));
            Assert.That(state.LastContractIncome, Is.GreaterThan(0));
            Assert.That(state.Intel, Is.GreaterThan(0));
            Assert.That(outside.ExternalContractRevenue, Is.EqualTo(state.LastContractIncome));
            Assert.That(outside.TowerContractOutflow, Is.EqualTo(state.LastContractIncome));
            Assert.That(outside.PurchaseRevenue, Is.GreaterThan(0));
            Assert.That(treasury.TotalRevenue, Is.EqualTo(state.LastContractIncome));
            Assert.That(treasury.TotalExpenses, Is.GreaterThan(0));
        }

        [Test]
        public void InvestigatorUsesReachableRouteAndDisruptionSurvivesSave()
        {
            var layout = CreateLayout();
            var state = new UndergroundOperationsState();
            var population = FiftyResidentFixture.Create();
            var treasury = new TowerEconomyState();
            var outside = new OutsideMarketState();
            var scrutiny = new ScrutinyState();
            for (var day = 1; day <= 12; day++)
                state.AdvanceDaily(layout, population, treasury, outside, scrutiny, day * 1440L);

            Assert.That(state.Exposure, Is.GreaterThanOrEqualTo(.28f));
            state.AdvanceTick(layout, treasury, scrutiny, 18000);
            Assert.That(state.Phase, Is.EqualTo(InvestigatorPhase.Street));
            Assert.That(state.InvestigatorTargetRoomId, Is.GreaterThan(0));
            var restored = UndergroundOperationsState.FromSaveData(state.ToSaveData());
            Assert.That(restored.Phase, Is.EqualTo(InvestigatorPhase.Street));
            Assert.That(restored.InvestigatorTargetRoomId, Is.EqualTo(state.InvestigatorTargetRoomId));

            for (var tick = 18001; tick < 18120; tick++) restored.AdvanceTick(layout, treasury, scrutiny, tick);
            Assert.That(restored.DisruptedRoomId, Is.EqualTo(state.InvestigatorTargetRoomId));
            Assert.That(restored.IsDisrupted(restored.DisruptedRoomId, 18120), Is.True);
            var afterSave = UndergroundOperationsState.FromSaveData(restored.ToSaveData());
            Assert.That(afterSave.DisruptedRoomId, Is.EqualTo(restored.DisruptedRoomId));
        }

        private static UndergroundDigState CreateLayout()
        {
            var tiles = new List<UndergroundCellSaveData>();
            for (var x = 16; x <= 25; x++)
                for (var depth = 0; depth <= 1; depth++)
                    tiles.Add(new UndergroundCellSaveData { x = x, depth = depth });
            var corridors = new List<UndergroundCellSaveData>();
            for (var x = 17; x <= 25; x++) corridors.Add(new UndergroundCellSaveData { x = x, depth = 0 });
            var rooms = new[]
            {
                new UndergroundRoomSaveData { id = 301, type = (int)UndergroundRoomType.AccessHub, x = 18, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 302, type = (int)UndergroundRoomType.SupplyDepot, x = 20, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 303, type = (int)UndergroundRoomType.Communications, x = 22, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 304, type = (int)UndergroundRoomType.OperationsCenter, x = 24, depth = 1, width = 2, height = 1 }
            };
            return UndergroundDigState.FromSaveData(tiles.ToArray(), tiles.ToArray(), false, default, false,
                corridors.ToArray(), new[] { new UndergroundCellSaveData { x = 16, depth = 0 } },
                new UndergroundCellSaveData { x = 16, depth = 0 }, rooms);
        }
    }
}
