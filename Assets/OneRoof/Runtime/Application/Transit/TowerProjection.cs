using System.Collections.Generic;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Transit
{
    public enum TransitResidentStatus
    {
        Queued,
        Riding,
        Arrived,
        InRoom,
        Walking
    }

    public readonly struct TransitResidentProjection
    {
        public TransitResidentProjection(
            int residentId,
            int destinationFloor,
            TransitResidentStatus status,
            int floor = 0,
            float cellX = 0f,
            int? roomId = null,
            ActivityKind activity = ActivityKind.Idle,
            int slotInRoom = 0,
            int waitTicks = 0)
        {
            ResidentId = residentId;
            DestinationFloor = destinationFloor;
            Status = status;
            Floor = floor;
            CellX = cellX;
            RoomId = roomId;
            Activity = activity;
            SlotInRoom = slotInRoom;
            WaitTicks = waitTicks;
        }

        public int ResidentId { get; }

        public int DestinationFloor { get; }

        public TransitResidentStatus Status { get; }

        public int Floor { get; }

        public float CellX { get; }

        public int? RoomId { get; }

        public ActivityKind Activity { get; }

        public int SlotInRoom { get; }

        public int WaitTicks { get; }
    }

    public readonly struct ElevatorProjection
    {
        public ElevatorProjection(int elevatorId, int floor, int passengerCount, int capacity, IReadOnlyList<int> passengerIds)
        {
            ElevatorId = elevatorId;
            Floor = floor;
            PassengerCount = passengerCount;
            Capacity = capacity;
            PassengerIds = passengerIds ?? System.Array.Empty<int>();
        }

        public int ElevatorId { get; }

        public int Floor { get; }

        public int PassengerCount { get; }

        public int Capacity { get; }

        /// <summary>Entity ID values for every resident currently inside this car.</summary>
        public IReadOnlyList<int> PassengerIds { get; }
    }

    /// <summary>
    /// Read-only projection of the full tower simulation state for presentation consumption.
    /// Contains resident spatial positions, elevator states, and aggregate transit metrics.
    /// </summary>
    public sealed class TowerProjection
    {
        public TowerProjection(long tick, int queueLength, int arrivedCount, float averageWaitTicks, IReadOnlyList<TransitResidentProjection> residents, IReadOnlyList<ElevatorProjection> elevators)
        {
            Tick = tick;
            QueueLength = queueLength;
            ArrivedCount = arrivedCount;
            AverageWaitTicks = averageWaitTicks;
            Residents = residents;
            Elevators = elevators;
        }

        public long Tick { get; }

        public int QueueLength { get; }

        public int ArrivedCount { get; }

        public float AverageWaitTicks { get; }

        public IReadOnlyList<TransitResidentProjection> Residents { get; }

        public IReadOnlyList<ElevatorProjection> Elevators { get; }
    }
}
