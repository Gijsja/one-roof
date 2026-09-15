using System;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Transit
{
    public sealed class ElevatorPassenger : IEquatable<ElevatorPassenger>
    {
        public ElevatorPassenger(EntityId personId, int originFloor, int destinationFloor)
        {
            PersonId = personId;
            OriginFloor = originFloor;
            DestinationFloor = destinationFloor;
        }

        public EntityId PersonId { get; }

        public int OriginFloor { get; }

        public int DestinationFloor { get; }

        public long WaitTicks { get; set; }

        public long RideTicks { get; set; }

        public bool Equals(ElevatorPassenger other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PersonId.Equals(other.PersonId);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is ElevatorPassenger other && Equals(other));

        public override int GetHashCode() => PersonId.GetHashCode();

        public override string ToString() => $"Passenger {PersonId} ({OriginFloor} -> {DestinationFloor})";
    }
}
