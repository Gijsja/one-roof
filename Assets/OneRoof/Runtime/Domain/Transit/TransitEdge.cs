using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Transit
{
    public sealed class TransitEdge : IEquatable<TransitEdge>
    {
        public TransitEdge(EntityId fromNodeId, EntityId toNodeId, int cost, TransitMode mode)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Transit edge cost cannot be negative.");
            }

            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Cost = cost;
            Mode = mode;
        }

        public EntityId FromNodeId { get; }

        public EntityId ToNodeId { get; }

        public int Cost { get; }

        public TransitMode Mode { get; }

        public bool Equals(TransitEdge other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FromNodeId.Equals(other.FromNodeId) && ToNodeId.Equals(other.ToNodeId) && Cost == other.Cost && Mode == other.Mode;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is TransitEdge other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(FromNodeId, ToNodeId, Cost, Mode);

        public override string ToString() => $"{FromNodeId} -> {ToNodeId} ({Mode}, cost {Cost})";
    }
}
