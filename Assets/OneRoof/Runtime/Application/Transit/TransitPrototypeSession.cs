using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Transit
{
    public enum TransitResidentStatus
    {
        Queued,
        Riding,
        Arrived
    }

    public readonly struct TransitResidentProjection
    {
        public TransitResidentProjection(int residentId, int destinationFloor, TransitResidentStatus status)
        {
            ResidentId = residentId;
            DestinationFloor = destinationFloor;
            Status = status;
        }

        public int ResidentId { get; }

        public int DestinationFloor { get; }

        public TransitResidentStatus Status { get; }
    }

    public readonly struct ElevatorProjection
    {
        public ElevatorProjection(int elevatorId, int floor, int passengerCount, int capacity)
        {
            ElevatorId = elevatorId;
            Floor = floor;
            PassengerCount = passengerCount;
            Capacity = capacity;
        }

        public int ElevatorId { get; }

        public int Floor { get; }

        public int PassengerCount { get; }

        public int Capacity { get; }
    }

    public sealed class TransitPrototypeProjection
    {
        public TransitPrototypeProjection(long tick, int queueLength, int arrivedCount, float averageWaitTicks, IReadOnlyList<TransitResidentProjection> residents, IReadOnlyList<ElevatorProjection> elevators)
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

    /// <summary>Application boundary for the visual transit proof; it exposes projections and capacity actions only.</summary>
    public sealed class TransitPrototypeSession
    {
        public const int FloorCount = TransitPrototypeSimulation.FloorCount;
        public const int ResidentCount = TransitPrototypeSimulation.ResidentCount;

        private readonly TransitPrototypeSimulation _simulation = new TransitPrototypeSimulation();

        public void AdvanceOneTick() => _simulation.AdvanceOneTick();

        public void AddCapacity() => _simulation.AddElevator();

        public TransitPrototypeProjection Projection()
        {
            var snapshot = _simulation.Snapshot();
            var residents = new List<TransitResidentProjection>(snapshot.Residents.Count);
            foreach (var resident in snapshot.Residents)
            {
                residents.Add(new TransitResidentProjection(resident.ResidentId.Value, resident.DestinationFloor, ToStatus(resident.Phase)));
            }

            var elevators = new List<ElevatorProjection>(snapshot.Elevators.Count);
            foreach (var elevator in snapshot.Elevators)
            {
                elevators.Add(new ElevatorProjection(elevator.ElevatorId.Value, elevator.Floor, elevator.PassengerCount, elevator.Capacity));
            }

            return new TransitPrototypeProjection(
                snapshot.Tick.Value,
                snapshot.QueueLength,
                snapshot.ArrivedCount,
                snapshot.AverageWaitTicks,
                new ReadOnlyCollection<TransitResidentProjection>(residents),
                new ReadOnlyCollection<ElevatorProjection>(elevators));
        }

        private static TransitResidentStatus ToStatus(ResidentTransitPhase phase)
        {
            switch (phase)
            {
                case ResidentTransitPhase.Queued:
                    return TransitResidentStatus.Queued;
                case ResidentTransitPhase.Riding:
                    return TransitResidentStatus.Riding;
                default:
                    return TransitResidentStatus.Arrived;
            }
        }
    }
}
