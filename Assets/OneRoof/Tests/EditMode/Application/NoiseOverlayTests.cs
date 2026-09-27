using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class NoiseOverlayTests
    {
        [Test]
        public void StandardTower_ProjectsActivityAndInspectableRoomLabels()
        {
            var overlays = new TowerDataOverlays(new TowerSimulationSession());
            var projection = overlays.Noise;

            Assert.That(projection.Rooms, Is.Not.Empty);
            Assert.That(projection.Rooms, Has.Some.Matches<NoiseRoomProjection>(room => room.SourceCount > 0 && room.Intensity > 0f));
            Assert.That(projection.Rooms, Has.Some.Matches<NoiseRoomProjection>(room => room.AffectedOccupants > 0 && room.RoomId.IsValid));
            Assert.That(projection.ModelLabel, Does.Contain("not measured"));
            Assert.That(projection.Rooms[0].AccessibilityLabel, Does.Contain("Floor"));
            Assert.That(overlays.Noise, Is.SameAs(projection));
        }
    }
}
