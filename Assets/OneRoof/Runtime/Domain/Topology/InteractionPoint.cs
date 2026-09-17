using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    /// <summary>
    /// Pure C# domain record representing a discrete furniture / interaction anchor within a room.
    /// Residents can claim and occupy interaction points for sleep, work, dining, and seating routines.
    /// </summary>
    public sealed class InteractionPoint : IEquatable<InteractionPoint>
    {
        public InteractionPoint(
            EntityId id,
            EntityId roomId,
            InteractionPointKind kind,
            ContentId propContentId,
            int localCellOffset,
            int capacity = 1,
            IReadOnlyList<EntityId> occupantIds = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be greater than zero.");
            }

            Id = id;
            RoomId = roomId;
            Kind = kind;
            PropContentId = propContentId;
            LocalCellOffset = localCellOffset;
            Capacity = capacity;
            OccupantIds = new ReadOnlyCollection<EntityId>(new List<EntityId>(occupantIds ?? Array.Empty<EntityId>()));
        }

        public EntityId Id { get; }
        public EntityId RoomId { get; }
        public InteractionPointKind Kind { get; }
        public ContentId PropContentId { get; }
        public int LocalCellOffset { get; }
        public int Capacity { get; }
        public IReadOnlyList<EntityId> OccupantIds { get; }

        public bool IsAvailable => OccupantIds.Count < Capacity;
        public int OccupantCount => OccupantIds.Count;

        public bool ContainsOccupant(EntityId residentId)
        {
            for (var i = 0; i < OccupantIds.Count; i++)
            {
                if (OccupantIds[i].Equals(residentId)) return true;
            }
            return false;
        }

        public InteractionPoint WithOccupant(EntityId residentId)
        {
            if (ContainsOccupant(residentId)) return this;
            if (OccupantIds.Count >= Capacity)
            {
                throw new InvalidOperationException($"InteractionPoint {Id} is at capacity ({Capacity}).");
            }

            var updated = new List<EntityId>(OccupantIds) { residentId };
            return new InteractionPoint(Id, RoomId, Kind, PropContentId, LocalCellOffset, Capacity, updated);
        }

        public InteractionPoint WithoutOccupant(EntityId residentId)
        {
            if (!ContainsOccupant(residentId)) return this;

            var updated = new List<EntityId>(OccupantIds);
            updated.Remove(residentId);
            return new InteractionPoint(Id, RoomId, Kind, PropContentId, LocalCellOffset, Capacity, updated);
        }

        public bool Equals(InteractionPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is InteractionPoint other && Equals(other));

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"InteractionPoint {Id} ({Kind}:{PropContentId}) in Room {RoomId} at +{LocalCellOffset} (Occ: {OccupantIds.Count}/{Capacity})";
    }
}
