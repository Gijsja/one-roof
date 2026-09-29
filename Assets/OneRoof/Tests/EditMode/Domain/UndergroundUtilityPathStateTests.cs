using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class UndergroundUtilityPathStateTests
    {
        private static readonly Tick BuildTick = new Tick(0);

        [Test]
        public void ConnectedCoreAndCorridor_CarryBothServicesToRoomWithStableSegments()
        {
            var tower = CreateTower();
            var underground = CreateUnderground();
            var first = Project(tower, underground);
            var restored = UndergroundDigState.FromSaveData(underground.ToSaveData(), underground.FloorsToSaveData(),
                false, default, false, underground.CorridorsToSaveData(), underground.ShaftToSaveData(),
                new UndergroundCellSaveData { x = underground.AccessCore.Value.X, depth = underground.AccessCore.Value.Depth },
                underground.RoomsToSaveData());
            var second = Project(tower, restored);

            Assert.That(Status(first, 301, UndergroundUtilityKind.Power).IsFlowing, Is.True);
            Assert.That(Status(first, 301, UndergroundUtilityKind.Water).IsFlowing, Is.True);
            Assert.That(first.PowerSegments.Any(s => s.Id.StartsWith("power:source:") && s.IsFlowing), Is.True);
            var bridge = first.PowerSegments.Single(s => s.Id.StartsWith("power:source:"));
            Assert.That(bridge.To.X, Is.EqualTo(underground.AccessCore.Value.X));
            Assert.That(bridge.To.Depth, Is.EqualTo(underground.AccessCore.Value.Depth));
            Assert.That(first.PowerSegments.Any(s => s.Id == "power:cell:16:0:16:1" && s.IsFlowing), Is.True);
            Assert.That(first.PowerSegments.Any(s => s.Id == "power:room:301:port:17:1" && s.IsFlowing), Is.True);
            CollectionAssert.AreEqual(first.PowerSegments.Select(s => s.Id), second.PowerSegments.Select(s => s.Id));
            CollectionAssert.AreEqual(first.WaterSegments.Select(s => s.Id), second.WaterSegments.Select(s => s.Id));
        }

        [Test]
        public void BrokenCorridor_LeavesBuiltSegmentsButStopsFlowAtDisconnectedBranch()
        {
            var tower = CreateTower();
            var underground = CreateUnderground(includeConnectingCorridor: false);
            var snapshot = Project(tower, underground);

            Assert.That(Status(snapshot, 301, UndergroundUtilityKind.Power).Cause,
                Is.EqualTo(UndergroundUtilityCause.DisconnectedPath));
            Assert.That(Status(snapshot, 301, UndergroundUtilityKind.Water).IsFlowing, Is.False);
            Assert.That(snapshot.PowerSegments.Single(s => s.Id == "power:room:301:port:18:1").IsConnected, Is.False);
            Assert.That(snapshot.PowerSegments.Any(s => s.Id.StartsWith("power:source:") && s.IsFlowing), Is.True);
        }

        [Test]
        public void LeftBranch_FlowRunsAwayFromCoreWithoutChangingSegmentId()
        {
            var tower = CreateTower();
            var cells = new List<UndergroundCellSaveData>();
            for (var x = 12; x <= 16; x++)
            for (var depth = 0; depth <= 1; depth++)
                cells.Add(new UndergroundCellSaveData { x = x, depth = depth });
            var corridors = new[]
            {
                new UndergroundCellSaveData { x = 13, depth = 0 },
                new UndergroundCellSaveData { x = 14, depth = 0 },
                new UndergroundCellSaveData { x = 15, depth = 0 }
            };
            var core = new UndergroundCellSaveData { x = 16, depth = 0 };
            var underground = UndergroundDigState.FromSaveData(cells.ToArray(), cells.ToArray(), false,
                default, false, corridors, new[] { core }, core,
                new[] { new UndergroundRoomSaveData { id = 302, type = (int)UndergroundRoomType.Workshop,
                    x = 12, depth = 1, width = 2, height = 1 } });

            var snapshot = Project(tower, underground);
            var edge = snapshot.PowerSegments.Single(s => s.Id == "power:cell:15:0:16:0");
            Assert.That(edge.IsFlowing, Is.True);
            Assert.That(edge.From.X, Is.EqualTo(16));
            Assert.That(edge.To.X, Is.EqualTo(15));
            Assert.That(Status(snapshot, 302, UndergroundUtilityKind.Power).IsFlowing, Is.True);
        }

        [Test]
        public void MissingCoreAndMissingSource_ReportDistinctCauses()
        {
            var tower = CreateTower();
            var noCore = Project(tower, CreateUnderground(includeCore: false));
            Assert.That(Status(noCore, 301, UndergroundUtilityKind.Power).Cause,
                Is.EqualTo(UndergroundUtilityCause.NoAccessCore));
            Assert.That(noCore.PowerSegments.Any(s => s.Id.StartsWith("power:source:")), Is.False);

            var noSource = CreateTower(includePowerSource: false, includeWaterSource: false);
            var noSourcePaths = Project(noSource, CreateUnderground());
            Assert.That(Status(noSourcePaths, 301, UndergroundUtilityKind.Power).Cause,
                Is.EqualTo(UndergroundUtilityCause.NoSurfaceSource));
            Assert.That(Status(noSourcePaths, 301, UndergroundUtilityKind.Water).Cause,
                Is.EqualTo(UndergroundUtilityCause.NoSurfaceSource));
        }

        [Test]
        public void BuiltSourceWithoutGroundService_ReportsNonFlowingPath()
        {
            var tower = CreateTower(includePowerRiser: false, includeWaterRiser: false);
            var snapshot = Project(tower, CreateUnderground());

            Assert.That(Status(snapshot, 301, UndergroundUtilityKind.Power).IsConnected, Is.True);
            Assert.That(Status(snapshot, 301, UndergroundUtilityKind.Power).Cause,
                Is.EqualTo(UndergroundUtilityCause.SurfaceServiceUnavailable));
            Assert.That(Status(snapshot, 301, UndergroundUtilityKind.Water).Cause,
                Is.EqualTo(UndergroundUtilityCause.SurfaceServiceUnavailable));
            Assert.That(snapshot.PowerSegments.All(s => !s.IsFlowing), Is.True);
        }

        private static UndergroundRoomUtilityStatus Status(UndergroundUtilityPathSnapshot snapshot, int roomId,
            UndergroundUtilityKind kind) => snapshot.RoomStatuses.Single(s => s.RoomId == roomId && s.Kind == kind);

        private static UndergroundUtilityPathSnapshot Project(BuildingTopologyState tower, UndergroundDigState underground) =>
            UndergroundUtilityPathState.Project(tower, underground,
                new ElectricalGridState().Evaluate(tower), new WaterWasteNetworkState().Evaluate(tower));

        private static BuildingTopologyState CreateTower(bool includePowerSource = true, bool includeWaterSource = true,
            bool includePowerRiser = true, bool includeWaterRiser = true)
        {
            var tower = new BuildingTopologyState();
            Assert.That(tower.Execute(new BuildFloorSlabCommand(0, 0, 30), BuildTick).Accepted, Is.True);
            if (includePowerSource) Build(tower, 0, 3, ElectricalGridState.SubstationContentId, 120);
            if (includePowerRiser) Build(tower, 4, 5, ElectricalGridState.RiserContentId, 0);
            Build(tower, 6, 7, ElectricalGridState.TransformerContentId, 0);
            if (includeWaterSource) Build(tower, 8, 11, WaterWasteNetworkState.WaterPumpContentId, 120);
            if (includeWaterRiser) Build(tower, 12, 13, WaterWasteNetworkState.WaterRiserContentId, 0);
            return tower;
        }

        private static void Build(BuildingTopologyState tower, int minX, int maxX, ContentId type, int capacity)
        {
            Assert.That(tower.Execute(new BuildRoomCommand(0, minX, maxX, type, capacity), BuildTick).Accepted,
                Is.True, type.Value);
        }

        private static UndergroundDigState CreateUnderground(bool includeCore = true,
            bool includeConnectingCorridor = true)
        {
            var cells = new List<UndergroundCellSaveData>();
            for (var depth = 0; depth <= 2; depth++)
            for (var x = 16; x <= 18; x++)
                cells.Add(new UndergroundCellSaveData { x = x, depth = depth });
            var shaft = includeCore
                ? new[] { new UndergroundCellSaveData { x = 16, depth = 0 }, new UndergroundCellSaveData { x = 16, depth = 1 } }
                : null;
            var corridors = new List<UndergroundCellSaveData>();
            corridors.Add(new UndergroundCellSaveData { x = includeConnectingCorridor ? 17 : 18, depth = 1 });
            corridors.Add(new UndergroundCellSaveData { x = 17, depth = 0 });
            var rooms = new[] { new UndergroundRoomSaveData
                { id = 301, type = (int)UndergroundRoomType.Workshop, x = 17, depth = 2, width = 2, height = 1 } };
            return UndergroundDigState.FromSaveData(cells.ToArray(), cells.ToArray(), false, default, false,
                corridors.ToArray(), shaft,
                includeCore ? new UndergroundCellSaveData { x = 16, depth = 0 } : null, rooms);
        }
    }
}
