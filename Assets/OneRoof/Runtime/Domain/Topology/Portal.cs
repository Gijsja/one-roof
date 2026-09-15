using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    public enum PortalType
    {
        Door,
        ElevatorShaftDoor,
        StairwellDoor
    }

    public sealed class Portal : IEquatable<Portal>
    {
        public Portal(EntityId id, PortalType type, CellCoordinate location, EntityId roomId, EntityId? targetPortalId = null)
        {
            Id = id;
            Type = type;
            Location = location;
            RoomId = roomId;
            TargetPortalId = targetPortalId;
        }

        public EntityId Id { get; }

        public PortalType Type { get; }

        public CellCoordinate Location { get; }

        public EntityId RoomId { get; }

        public EntityId? TargetPortalId { get; }

        public bool Equals(Portal other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is Portal other && Equals(other));

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"Portal {Id} ({Type}) at {Location} in Room {RoomId}";
    }
}
