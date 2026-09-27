using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class UndergroundConstructionTests
    {
        [Test]
        public void OldIndependentBoard_MigratesToCenteredFootprint()
        {
            var earth = UndergroundDigState.FromSaveData(
                new[] { new UndergroundCellSaveData { x = 0, depth = 0 }, new UndergroundCellSaveData { x = 15, depth = 5 } },
                new[] { new UndergroundCellSaveData { x = 0, depth = 0 } }, legacy16Grid: true);
            Assert.That(earth.IsExcavated(8, 3), Is.True);
            Assert.That(earth.IsExcavated(23, 8), Is.True);
            Assert.That(earth.IsFloored(8, 3), Is.True);
            Assert.That(earth.IsExcavated(0, 0), Is.False);
        }

        [Test]
        public void RoomNeedsConnectedFloorAndEntrance_ThenSurvivesSave()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();
            var zone = new ZoneUndergroundRoomCommand(UndergroundRoomType.Workshop, 18, 1, 2, 1);
            Assert.That(sim.CanExecute(zone).Accepted, Is.False);
            for (var depth = 0; depth <= 1; depth++)
            for (var x = 16; x <= 19; x++)
            {
                Assert.That(sim.ExecuteCommand(new DigUndergroundCommand(x, depth, 1)).Accepted, Is.True);
                Assert.That(sim.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth, 1)).Accepted, Is.True);
            }
            Assert.That(sim.CanExecute(zone).Accepted, Is.False);
            Assert.That(sim.ExecuteCommand(new BuildUndergroundCoreCommand(16, 1)).Accepted, Is.True);
            Assert.That(sim.ExecuteCommand(new BuildUndergroundCorridorCommand(17, 1, 1)).Accepted, Is.True);
            Assert.That(sim.ExecuteCommand(zone).Accepted, Is.True);
            Assert.That(sim.Underground.Rooms.Count, Is.EqualTo(1));
            Assert.That(sim.Underground.IsRoomReachable(sim.Underground.Rooms[0].Id), Is.True);
            Assert.That(sim.CanExecute(zone).Accepted, Is.False, "Overlapping room must be rejected.");

            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            Assert.That(restored.Underground.Rooms.Count, Is.EqualTo(1));
            Assert.That(restored.Underground.IsRoomReachable(restored.Underground.Rooms[0].Id), Is.True);
            Assert.That(restored.Underground.Rooms[0].Type, Is.EqualTo(UndergroundRoomType.Workshop));
        }

        [Test]
        public void CoreRequiresCenterAndContinuousFloor()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();
            Assert.That(sim.CanExecute(new BuildUndergroundCoreCommand(15, 0)).Accepted, Is.False);
            Assert.That(sim.CanExecute(new BuildUndergroundCoreCommand(16, 0)).Accepted, Is.False);
            sim.ExecuteCommand(new DigUndergroundCommand(16, 0, 1));
            sim.ExecuteCommand(new BuildUndergroundFloorCommand(16, 0, 1));
            Assert.That(sim.CanExecute(new BuildUndergroundCoreCommand(16, 1)).Accepted, Is.False);
            Assert.That(sim.ExecuteCommand(new BuildUndergroundCoreCommand(16, 0)).Accepted, Is.True);
        }

        [Test]
        public void ThreeByTwoRoom_ConnectsToCorridorAlongBottomEdge()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();
            for (var depth = 0; depth <= 2; depth++)
            for (var x = 16; x <= 19; x++)
            {
                sim.ExecuteCommand(new DigUndergroundCommand(x, depth, 1));
                sim.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth, 1));
            }
            Assert.That(sim.ExecuteCommand(new BuildUndergroundCoreCommand(16, 2)).Accepted, Is.True);
            Assert.That(sim.ExecuteCommand(new BuildUndergroundCorridorCommand(17, 2, 3)).Accepted, Is.True);
            var room = new ZoneUndergroundRoomCommand(UndergroundRoomType.ResearchLab, 17, 0, 3, 2);
            Assert.That(sim.CanExecute(room).Accepted, Is.True);
            Assert.That(sim.ExecuteCommand(room).Accepted, Is.True);
            Assert.That(sim.Underground.Rooms[0].IsReachable, Is.True);
        }

        [Test]
        public void AccessCore_RejectsUnaffordableBuild()
        {
            var sim = TowerSimulation.CreateGroundFloorStart(startingTreasury: 0);
            sim.ExecuteCommand(new DigUndergroundCommand(16, 0, 1));
            sim.ExecuteCommand(new BuildUndergroundFloorCommand(16, 0, 1));
            var before = sim.Economy.CashBalance;
            Assert.That(sim.CanExecute(new BuildUndergroundCoreCommand(16, 0)).Accepted, Is.False);
            Assert.That(sim.Economy.CashBalance, Is.EqualTo(before));
        }
    }
}
