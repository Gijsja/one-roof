using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    public sealed class Room : IEquatable<Room>
    {
        public Room(EntityId id, ContentId contentType, CellBounds bounds, IReadOnlyList<EntityId> portalIds, int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");
            }

            Id = id;
            ContentType = contentType;
            Bounds = bounds;
            PortalIds = new ReadOnlyCollection<EntityId>(new List<EntityId>(portalIds ?? Array.Empty<EntityId>()));
            Capacity = capacity;
        }

        public EntityId Id { get; }

        public ContentId ContentType { get; }

        public CellBounds Bounds { get; }

        public int Floor => Bounds.Floor;

        public IReadOnlyList<EntityId> PortalIds { get; }

        public int Capacity { get; }

        public bool Contains(CellCoordinate coordinate) => Bounds.Contains(coordinate);

        public bool Overlaps(Room other) => Bounds.Overlaps(other.Bounds);

        public bool Equals(Room other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is Room other && Equals(other));

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"Room {Id} ({ContentType}) at {Bounds} (Cap: {Capacity})";
    }
}
