using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Transit
{
    public sealed class TransitNode : IEquatable<TransitNode>
    {
        public TransitNode(EntityId id, TransitNodeType type, CellCoordinate location, EntityId? roomId = null)
        {
            Id = id;
            Type = type;
            Location = location;
            RoomId = roomId;
        }

        public EntityId Id { get; }

        public TransitNodeType Type { get; }

        public CellCoordinate Location { get; }

        public EntityId? RoomId { get; }

        public int Floor => Location.Floor;

        public bool Equals(TransitNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is TransitNode other && Equals(other));

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"Node {Id} ({Type}) at {Location}";
    }
}
