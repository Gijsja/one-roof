using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Infrastructure;
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

            state.AdvanceDaily(layout, population, treasury, outside, scrutiny, 1440, () => PoweredRooms(layout));

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
                state.AdvanceDaily(layout, population, treasury, outside, scrutiny, day * 1440L, () => PoweredRooms(layout));

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

        [Test]
        public void MissingUtilitiesStopCommunicationsAndContractProduction()
        {
            var layout = CreateLayout();
            var state = new UndergroundOperationsState();
            state.AdvanceDaily(layout, FiftyResidentFixture.Create(), new TowerEconomyState(),
                new OutsideMarketState(), new ScrutinyState(), 1440);
            Assert.That(state.Intel, Is.Zero);
            Assert.That(state.LastContractIncome, Is.Zero);
            var powered = new UndergroundOperationsState();
            powered.AdvanceDaily(layout, FiftyResidentFixture.Create(), new TowerEconomyState(),
                new OutsideMarketState(), new ScrutinyState(), 1440, () => PoweredRooms(layout));
            Assert.That(state.LastDailyCost, Is.EqualTo(powered.LastDailyCost));
        }

        [Test]
        public void LegacyGeneratorAggregateCannotInventPhysicalFeed()
        {
            var restored = UndergroundOperationsState.FromSaveData(new UndergroundOperationsSaveData
            { backupPowerCapacity = int.MaxValue });
            Assert.That(restored.BackupPowerCapacity, Is.Zero);
            Assert.That(restored.ConnectedBackupCapacity(CreateLayout(true), 1440), Is.Zero);
        }

        [Test]
        public void SavedGeneratorCapacityIsBoundedAndAggregateDerivedFromRoomOutputs()
        {
            var restored = UndergroundOperationsState.FromSaveData(new UndergroundOperationsSaveData
            { backupPowerCapacity = -1, generatorRoomIds = new[] { 305, 306, -1 },
                generatorCapacities = new[] { int.MaxValue, -1, 80 } });
            Assert.That(restored.BackupPowerCapacity, Is.EqualTo(100));
            Assert.That(restored.GeneratorOutput.Count, Is.EqualTo(1));
            Assert.That(restored.ConnectedBackupCapacity(CreateLayout(true), 1440), Is.EqualTo(100));
        }

        [Test]
        public void GeneratorWithoutFuelProducesNoBackupPower()
        {
            var layout = ReplaceRoomType(CreateLayout(true), 302, UndergroundRoomType.AccessHub);
            var state = new UndergroundOperationsState();
            state.AdvanceDaily(layout, FiftyResidentFixture.Create(), new TowerEconomyState(),
                new OutsideMarketState(), new ScrutinyState(), 1440, () => PoweredRooms(layout));
            Assert.That(state.BackupPowerCapacity, Is.Zero);
            Assert.That(state.GeneratorOutput.Count, Is.Zero);
        }

        [Test]
        public void InfirmaryRequiresWaterAsWellAsPower()
        {
            var layout = ReplaceRoomType(CreateLayout(), 303, UndergroundRoomType.Infirmary);
            var state = new UndergroundOperationsState();
            var population = FiftyResidentFixture.Create();
            var economy = new TowerEconomyState();
            var outside = new OutsideMarketState();
            var scrutiny = new ScrutinyState();
            state.AdvanceDaily(layout, population, economy, outside, scrutiny, 1440, () => PoweredRooms(layout));
            Assert.That(state.CareBoost, Is.Zero);
            state.AdvanceDaily(layout, population, economy, outside, scrutiny, 2880, () => PoweredRooms(layout, water: true));
            Assert.That(state.CareBoost, Is.GreaterThan(0));
        }

        [Test]
        public void GeneratorBootstrapsBeforeProductionAndOutputSurvivesSave()
        {
            var layout = CreateLayout(includeGenerator: true);
            var state = new UndergroundOperationsState();
            var projectionOutput = 0;
            state.AdvanceDaily(layout, FiftyResidentFixture.Create(), new TowerEconomyState(),
                new OutsideMarketState(), new ScrutinyState(), 1440, () =>
                {
                    projectionOutput = state.ConnectedBackupCapacity(layout, 1440);
                    return PoweredRooms(layout);
                });
            Assert.That(projectionOutput, Is.EqualTo(80));
            Assert.That(state.LastContractIncome, Is.GreaterThan(0));
            var restored = UndergroundOperationsState.FromSaveData(state.ToSaveData());
            Assert.That(restored.GeneratorOutput[305], Is.EqualTo(80));
            Assert.That(restored.ConnectedBackupCapacity(layout, 1440), Is.EqualTo(80));
            Assert.That(restored.ConnectedBackupCapacity(CreateLayout(), 1440), Is.Zero);
            var disruptedSave = state.ToSaveData();
            disruptedSave.disruptedRoomId = 305;
            disruptedSave.disruptionEndsAtTick = 1500;
            var disrupted = UndergroundOperationsState.FromSaveData(disruptedSave);
            Assert.That(disrupted.ConnectedBackupCapacity(layout, 1440), Is.Zero);
            Assert.That(disrupted.ConnectedBackupCapacity(layout, 1500), Is.EqualTo(80));
        }

        private static UndergroundDigState ReplaceRoomType(UndergroundDigState layout, int id, UndergroundRoomType type)
        {
            var rooms = layout.RoomsToSaveData();
            foreach (var room in rooms) if (room.id == id) room.type = (int)type;
            return UndergroundDigState.FromSaveData(layout.ToSaveData(), layout.FloorsToSaveData(), false, default, false,
                layout.CorridorsToSaveData(), layout.ShaftToSaveData(),
                new UndergroundCellSaveData { x = layout.AccessCore.Value.X, depth = layout.AccessCore.Value.Depth }, rooms);
        }

        private static UndergroundUtilityPathSnapshot PoweredRooms(UndergroundDigState layout, bool water = false)
        {
            var statuses = new List<UndergroundRoomUtilityStatus>();
            foreach (var room in layout.Rooms)
            {
                statuses.Add(new UndergroundRoomUtilityStatus(room.Id, UndergroundUtilityKind.Power, true, true, UndergroundUtilityCause.None));
                if (water) statuses.Add(new UndergroundRoomUtilityStatus(room.Id, UndergroundUtilityKind.Water, true, true, UndergroundUtilityCause.None));
            }
            return new UndergroundUtilityPathSnapshot(new UndergroundUtilitySegment[0], new UndergroundUtilitySegment[0], statuses);
        }

        private static UndergroundDigState CreateLayout(bool includeGenerator = false)
        {
            var tiles = new List<UndergroundCellSaveData>();
            for (var x = 16; x <= 27; x++)
                for (var depth = 0; depth <= 1; depth++)
                    tiles.Add(new UndergroundCellSaveData { x = x, depth = depth });
            var corridors = new List<UndergroundCellSaveData>();
            for (var x = 17; x <= 27; x++) corridors.Add(new UndergroundCellSaveData { x = x, depth = 0 });
            var rooms = new List<UndergroundRoomSaveData>
            {
                new UndergroundRoomSaveData { id = 301, type = (int)UndergroundRoomType.AccessHub, x = 18, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 302, type = (int)UndergroundRoomType.SupplyDepot, x = 20, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 303, type = (int)UndergroundRoomType.Communications, x = 22, depth = 1, width = 2, height = 1 },
                new UndergroundRoomSaveData { id = 304, type = (int)UndergroundRoomType.OperationsCenter, x = 24, depth = 1, width = 2, height = 1 }
            };
            if (includeGenerator) rooms.Add(new UndergroundRoomSaveData { id = 305, type = (int)UndergroundRoomType.Generator,
                x = 26, depth = 1, width = 2, height = 1 });
            return UndergroundDigState.FromSaveData(tiles.ToArray(), tiles.ToArray(), false, default, false,
                corridors.ToArray(), new[] { new UndergroundCellSaveData { x = 16, depth = 0 } },
                new UndergroundCellSaveData { x = 16, depth = 0 }, rooms.ToArray());
        }
    }
}
