using System;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TopologyPrimitivesTests
    {
        [Test]
        public void CellCoordinatesWithSameValuesAreEqualAndHashConsistently()
        {
            var coordA = new CellCoordinate(5, 2);
            var coordB = new CellCoordinate(5, 2);
            var coordDifferent = new CellCoordinate(4, 2);

            Assert.That(coordA, Is.EqualTo(coordB));
            Assert.That(coordA.GetHashCode(), Is.EqualTo(coordB.GetHashCode()));
            Assert.That(coordA == coordB, Is.True);
            Assert.That(coordA != coordDifferent, Is.True);
        }

        [Test]
        public void CellBoundsRejectsMinXGreaterThanMaxX()
        {
            Assert.Throws<ArgumentException>(() => new CellBounds(1, 10, 5));
        }

        [Test]
        public void CellBoundsCalculatesWidthAndContainsCoordinatesCorrectly()
        {
            var bounds = new CellBounds(2, 5, 10);

            Assert.That(bounds.Width, Is.EqualTo(6));
            Assert.That(bounds.Contains(new CellCoordinate(5, 2)), Is.True);
            Assert.That(bounds.Contains(new CellCoordinate(10, 2)), Is.True);
            Assert.That(bounds.Contains(new CellCoordinate(7, 2)), Is.True);
            Assert.That(bounds.Contains(new CellCoordinate(4, 2)), Is.False);
            Assert.That(bounds.Contains(new CellCoordinate(7, 1)), Is.False);
        }

        [Test]
        public void CellBoundsDetectsOverlapsOnSameFloorOnly()
        {
            var boundsA = new CellBounds(1, 0, 5);
            var boundsB = new CellBounds(1, 4, 8);
            var boundsC = new CellBounds(1, 6, 10);
            var boundsOtherFloor = new CellBounds(2, 4, 8);

            Assert.That(boundsA.Overlaps(boundsB), Is.True);
            Assert.That(boundsA.Overlaps(boundsC), Is.False);
            Assert.That(boundsB.Overlaps(boundsOtherFloor), Is.False);
        }

        [Test]
        public void RoomRejectsNegativeCapacity()
        {
            var contentType = new ContentId("residential:studio");
            var bounds = new CellBounds(1, 0, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Room(new EntityId(1), contentType, bounds, Array.Empty<EntityId>(), -1));
        }

        [Test]
        public void FloorTopologyRejectsOverlappingRooms()
        {
            var contentType = new ContentId("residential:studio");
            var roomA = new Room(new EntityId(1), contentType, new CellBounds(1, 0, 5), Array.Empty<EntityId>(), 2);
            var roomB = new Room(new EntityId(2), contentType, new CellBounds(1, 4, 8), Array.Empty<EntityId>(), 2);

            Assert.Throws<InvalidOperationException>(() =>
                new FloorTopology(1, new[] { roomA, roomB }, Array.Empty<Portal>()));
        }

        [Test]
        public void FloorTopologyRejectsRoomOnMismatchedFloor()
        {
            var contentType = new ContentId("residential:studio");
            var roomWrongFloor = new Room(new EntityId(1), contentType, new CellBounds(2, 0, 5), Array.Empty<EntityId>(), 2);

            Assert.Throws<ArgumentException>(() =>
                new FloorTopology(1, new[] { roomWrongFloor }, Array.Empty<Portal>()));
        }
    }
}
