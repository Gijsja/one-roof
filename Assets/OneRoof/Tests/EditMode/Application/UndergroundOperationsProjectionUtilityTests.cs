using System;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Underground;

namespace OneRoof.Tests.EditMode.Application
{
    public sealed class UndergroundOperationsProjectionUtilityTests
    {
        [Test]
        public void RoomInspectorReportsBothRoutesWithoutChangingOperationalCause()
        {
            var cells = new[]
            {
                new UndergroundCellSaveData { x = 16, depth = 0 },
                new UndergroundCellSaveData { x = 17, depth = 0 },
                new UndergroundCellSaveData { x = 18, depth = 0 },
                new UndergroundCellSaveData { x = 19, depth = 0 }
            };
            var layout = UndergroundDigState.FromSaveData(cells, cells, corridors: new[] { cells[1] },
                shaft: new[] { cells[0] }, core: cells[0],
                rooms: new[] { new UndergroundRoomSaveData { id = 11, type = (int)UndergroundRoomType.Workshop,
                    x = 18, depth = 0, width = 2, height = 1 } });
            var paths = new UndergroundUtilityPathSnapshot(
                Array.Empty<UndergroundUtilitySegment>(), Array.Empty<UndergroundUtilitySegment>(),
                new[]
                {
                    new UndergroundRoomUtilityStatus(11, UndergroundUtilityKind.Power, true, true, UndergroundUtilityCause.None),
                    new UndergroundRoomUtilityStatus(11, UndergroundUtilityKind.Water, false, false, UndergroundUtilityCause.NoSurfaceSource)
                });

            var projection = new UndergroundOperationsProjection(new UndergroundOperationsState(), layout, 0, paths);
            var room = projection.Rooms[0];

            Assert.That(room.HasUtilityDiagnostics, Is.True);
            Assert.That(room.PowerStatus, Is.EqualTo("Flowing"));
            Assert.That(room.WaterStatus, Is.EqualTo("No surface water pump"));
            Assert.That(room.Cause, Does.Contain("Needs 2 staff"));
        }
    }
}
