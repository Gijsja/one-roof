using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class InteractionPointTests
    {
        [Test]
        public void InteractionPoint_Constructor_InitializesProperties()
        {
            var pt = new InteractionPoint(
                new EntityId(101),
                new EntityId(5),
                InteractionPointKind.Sleep,
                new ContentId("prop:furniture.bed.v1"),
                localCellOffset: 1,
                capacity: 2);

            Assert.That(pt.Id.Value, Is.EqualTo(101));
            Assert.That(pt.RoomId.Value, Is.EqualTo(5));
            Assert.That(pt.Kind, Is.EqualTo(InteractionPointKind.Sleep));
            Assert.That(pt.PropContentId.Value, Is.EqualTo("prop:furniture.bed.v1"));
            Assert.That(pt.LocalCellOffset, Is.EqualTo(1));
            Assert.That(pt.Capacity, Is.EqualTo(2));
            Assert.That(pt.OccupantCount, Is.EqualTo(0));
            Assert.That(pt.IsAvailable, Is.True);
        }

        [Test]
        public void InteractionPoint_WithOccupant_UpdatesOccupantsImmutably()
        {
            var pt = new InteractionPoint(
                new EntityId(101),
                new EntityId(5),
                InteractionPointKind.Seat,
                new ContentId("prop:furniture.sofa.v1"),
                localCellOffset: 0,
                capacity: 2);

            var r1 = new EntityId(1);
            var r2 = new EntityId(2);

            var occupied1 = pt.WithOccupant(r1);
            Assert.That(pt.OccupantCount, Is.EqualTo(0)); // Original unmodified
            Assert.That(occupied1.OccupantCount, Is.EqualTo(1));
            Assert.That(occupied1.ContainsOccupant(r1), Is.True);
            Assert.That(occupied1.IsAvailable, Is.True); // Cap 2

            var occupied2 = occupied1.WithOccupant(r2);
            Assert.That(occupied2.OccupantCount, Is.EqualTo(2));
            Assert.That(occupied2.IsAvailable, Is.False); // At capacity

            // Attempting to exceed capacity throws
            Assert.Throws<InvalidOperationException>(() => occupied2.WithOccupant(new EntityId(3)));

            // Re-adding same occupant is idempotent
            var readded = occupied1.WithOccupant(r1);
            Assert.That(readded.OccupantCount, Is.EqualTo(1));
        }

        [Test]
        public void InteractionPoint_WithoutOccupant_ReleasesOccupant()
        {
            var r1 = new EntityId(1);
            var pt = new InteractionPoint(
                new EntityId(101),
                new EntityId(5),
                InteractionPointKind.Work,
                new ContentId("prop:furniture.desk.v1"),
                localCellOffset: 0,
                capacity: 1,
                occupantIds: new[] { r1 });

            Assert.That(pt.IsAvailable, Is.False);

            var released = pt.WithoutOccupant(r1);
            Assert.That(released.OccupantCount, Is.EqualTo(0));
            Assert.That(released.IsAvailable, Is.True);

            // Removing non-existent occupant is no-op
            var noop = released.WithoutOccupant(new EntityId(99));
            Assert.That(noop.OccupantCount, Is.EqualTo(0));
        }

        [Test]
        public void Room_TryGetAvailablePoint_FindsAvailableKind()
        {
            var bed = new InteractionPoint(new EntityId(1), new EntityId(10), InteractionPointKind.Sleep, new ContentId("prop:bed"), 0, 1);
            var sofa = new InteractionPoint(new EntityId(2), new EntityId(10), InteractionPointKind.Seat, new ContentId("prop:sofa"), 2, 1, new[] { new EntityId(50) });

            var room = new Room(
                new EntityId(10),
                new ContentId("residential:studio"),
                new CellBounds(1, 0, 3),
                Array.Empty<EntityId>(),
                capacity: 2,
                interactionPoints: new[] { bed, sofa });

            // Bed is available
            Assert.That(room.TryGetAvailablePoint(InteractionPointKind.Sleep, out var foundBed), Is.True);
            Assert.That(foundBed.Id, Is.EqualTo(bed.Id));

            // Sofa is occupied
            Assert.That(room.TryGetAvailablePoint(InteractionPointKind.Seat, out _), Is.False);

            // Cook does not exist in room
            Assert.That(room.TryGetAvailablePoint(InteractionPointKind.Cook, out _), Is.False);
        }
    }
}
