using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Transit
{
    public sealed class TransitEdge : IEquatable<TransitEdge>
    {
        public TransitEdge(EntityId fromNodeId, EntityId toNodeId, int cost, TransitMode mode, int durationSeconds = -1)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Transit edge cost cannot be negative.");
            }

            if (durationSeconds < -1) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            HasPhysicalDuration = durationSeconds >= 0;
            DurationSeconds = HasPhysicalDuration ? durationSeconds : checked(cost * 60);
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Cost = cost;
            Mode = mode;
        }

        public EntityId FromNodeId { get; }

        public EntityId ToNodeId { get; }

        public int Cost { get; }

        public TransitMode Mode { get; }

        /// <summary>Physical execution duration; Cost remains the routing preference.</summary>
        public int DurationSeconds { get; }
        public bool HasPhysicalDuration { get; }

        public bool Equals(TransitEdge other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FromNodeId.Equals(other.FromNodeId) && ToNodeId.Equals(other.ToNodeId) && Cost == other.Cost && Mode == other.Mode &&
                DurationSeconds == other.DurationSeconds && HasPhysicalDuration == other.HasPhysicalDuration;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is TransitEdge other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(FromNodeId, ToNodeId, Cost, Mode, DurationSeconds, HasPhysicalDuration);

        public override string ToString() => $"{FromNodeId} -> {ToNodeId} ({Mode}, cost {Cost})";
    }
}
