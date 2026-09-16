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

        private TransitPrototypeSimulation _simulation = new TransitPrototypeSimulation();
        private TransitPrototypeProjection _cachedProjection;
        private long _cachedTick = -1;

        public void AdvanceOneTick() => _simulation.AdvanceOneTick();

        public void AddCapacity()
        {
            _simulation.AddElevator();
            _cachedProjection = null; // elevator count changed without tick advancing — force cache rebuild
        }

        public void Reset()
        {
            _simulation = new TransitPrototypeSimulation();
            _cachedProjection = null;
            _cachedTick = -1;
        }

        public ElevatorBankCongestionProjection CongestionProjection()
        {
            var currentTick = _simulation.CurrentTick.Value;
            var snapshot = _simulation.Snapshot();

            var floors = new List<FloorCongestionProjection>(FloorCount);
            var queue0 = snapshot.QueueLength;
            var maxWait0 = (long)(snapshot.AverageWaitTicks * 1.8f);
            if (maxWait0 <= 0 && queue0 > 0)
            {
                maxWait0 = (long)snapshot.Tick.Value;
            }

            var floor0Severity = CongestionEvaluator.Evaluate(queue0, maxWait0);
            floors.Add(new FloorCongestionProjection(0, queue0, maxWait0, snapshot.AverageWaitTicks, floor0Severity));

            for (var f = 1; f < FloorCount; f++)
            {
                floors.Add(new FloorCongestionProjection(f, 0, 0, 0f, CongestionSeverity.Clear));
            }

            var elevProjections = new List<ElevatorProjection>(snapshot.Elevators.Count);
            var inTransit = 0;
            foreach (var el in snapshot.Elevators)
            {
                inTransit += el.PassengerCount;
                elevProjections.Add(new ElevatorProjection(
                    el.ElevatorId.Value,
                    el.Floor,
                    el.PassengerCount,
                    el.Capacity,
                    null));
            }

            return new ElevatorBankCongestionProjection(
                currentTick,
                snapshot.QueueLength,
                inTransit,
                snapshot.ArrivedCount,
                snapshot.AverageWaitTicks,
                bottleneckFloor: 0,
                overallSeverity: floor0Severity,
                floors: floors,
                elevators: elevProjections);
        }

        public TransitPrototypeProjection Projection()
        {
            var currentTick = _simulation.CurrentTick.Value;
            if (_cachedProjection != null && _cachedTick == currentTick)
            {
                return _cachedProjection;
            }

            var snapshot = _simulation.Snapshot();
            var residents = new List<TransitResidentProjection>(snapshot.Residents.Count);
            foreach (var resident in snapshot.Residents)
            {
                residents.Add(new TransitResidentProjection(resident.ResidentId.Value, resident.DestinationFloor, ToStatus(resident.Phase)));
            }

            var elevators = new List<ElevatorProjection>(snapshot.Elevators.Count);
            foreach (var elevator in snapshot.Elevators)
            {
                var ids = new List<int>(elevator.PassengerIds.Count);
                foreach (var id in elevator.PassengerIds)
                {
                    ids.Add(id.Value);
                }

                elevators.Add(new ElevatorProjection(elevator.ElevatorId.Value, elevator.Floor, elevator.PassengerCount, elevator.Capacity, ids));
            }

            _cachedProjection = new TransitPrototypeProjection(
                snapshot.Tick.Value,
                snapshot.QueueLength,
                snapshot.ArrivedCount,
                snapshot.AverageWaitTicks,
                new ReadOnlyCollection<TransitResidentProjection>(residents),
                new ReadOnlyCollection<ElevatorProjection>(elevators));
            _cachedTick = currentTick;
            return _cachedProjection;
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
