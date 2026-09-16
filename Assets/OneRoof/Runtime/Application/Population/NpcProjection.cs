using OneRoof.Domain.Population;

namespace OneRoof.Application.Population
{
    /// <summary>
    /// Read-only snapshot projection of an NPC resident exposed to the presentation layer.
    /// Does not expose mutable Domain records or internal simulation state.
    /// </summary>
    public readonly struct NpcProjection
    {
        public NpcProjection(
            int personId,
            int householdId,
            int floor,
            int roomId,
            ActivityKind currentActivity,
            bool isInTransit,
            int? destinationFloor,
            int? destinationRoomId,
            int waitTicks,
            float horizontalPosition)
        {
            PersonId = personId;
            HouseholdId = householdId;
            Floor = floor;
            RoomId = roomId;
            CurrentActivity = currentActivity;
            IsInTransit = isInTransit;
            DestinationFloor = destinationFloor;
            DestinationRoomId = destinationRoomId;
            WaitTicks = waitTicks;
            HorizontalPosition = horizontalPosition;
        }

        public int PersonId { get; }

        public int HouseholdId { get; }

        public int Floor { get; }

        public int RoomId { get; }

        public ActivityKind CurrentActivity { get; }

        public bool IsInTransit { get; }

        public int? DestinationFloor { get; }

        public int? DestinationRoomId { get; }

        public int WaitTicks { get; }

        public float HorizontalPosition { get; }

        public override string ToString() =>
            $"NpcProjection [Person {PersonId}, Floor {Floor}, Room {RoomId}, Activity {CurrentActivity}, Transit {IsInTransit}, Wait {WaitTicks}]";
    }
}
