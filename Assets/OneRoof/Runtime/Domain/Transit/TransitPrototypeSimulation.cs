using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Transit
{
    public enum ResidentTransitPhase
    {
        Queued,
        Riding,
        Arrived
    }

    public readonly struct TransitResidentSnapshot
    {
        public TransitResidentSnapshot(EntityId residentId, int destinationFloor, ResidentTransitPhase phase)
        {
            ResidentId = residentId;
            DestinationFloor = destinationFloor;
            Phase = phase;
        }

        public EntityId ResidentId { get; }

        public int DestinationFloor { get; }

        public ResidentTransitPhase Phase { get; }
    }

    public readonly struct ElevatorCarSnapshot
    {
        public ElevatorCarSnapshot(EntityId elevatorId, int floor, int passengerCount, int capacity, IReadOnlyList<EntityId> passengerIds)
        {
            ElevatorId = elevatorId;
            Floor = floor;
            PassengerCount = passengerCount;
            Capacity = capacity;
            PassengerIds = passengerIds ?? Array.Empty<EntityId>();
        }

        public EntityId ElevatorId { get; }

        public int Floor { get; }

        public int PassengerCount { get; }

        public int Capacity { get; }

        public IReadOnlyList<EntityId> PassengerIds { get; }
    }

    public sealed class TransitPrototypeSnapshot
    {
        public TransitPrototypeSnapshot(Tick tick, int queueLength, int arrivedCount, float averageWaitTicks, IReadOnlyList<TransitResidentSnapshot> residents, IReadOnlyList<ElevatorCarSnapshot> elevators)
        {
            Tick = tick;
            QueueLength = queueLength;
            ArrivedCount = arrivedCount;
            AverageWaitTicks = averageWaitTicks;
            Residents = residents;
            Elevators = elevators;
        }

        public Tick Tick { get; }

        public int QueueLength { get; }

        public int ArrivedCount { get; }

        public float AverageWaitTicks { get; }

        public IReadOnlyList<TransitResidentSnapshot> Residents { get; }

        public IReadOnlyList<ElevatorCarSnapshot> Elevators { get; }
    }

    /// <summary>
    /// Deterministic five-floor morning commute used by the first visible transit prototype.
    /// It owns simulation state; Unity views only read the snapshot returned by <see cref="Snapshot"/>.
    /// </summary>
    public sealed class TransitPrototypeSimulation
    {
        public const int FloorCount = 5;
        public const int ResidentCount = 50;
        public const int ElevatorCapacity = 8;

        private readonly List<ResidentState> _residents = new List<ResidentState>(ResidentCount);
        private readonly List<ElevatorState> _elevators = new List<ElevatorState>();
        private long _tick;
        private long _totalCompletedWait;
        private int _arrivedCount;

        public TransitPrototypeSimulation()
        {
            for (var residentIndex = 0; residentIndex < ResidentCount; residentIndex++)
            {
                _residents.Add(new ResidentState(new EntityId(residentIndex + 1), 1 + residentIndex % (FloorCount - 1)));
            }

            AddElevator();
        }

        public Tick CurrentTick => new Tick(_tick);

        public bool IsComplete => _arrivedCount == ResidentCount;

        public int ElevatorCount => _elevators.Count;

        public void AddElevator()
        {
            _elevators.Add(new ElevatorState(new EntityId(1000 + _elevators.Count + 1)));
        }

        public void AdvanceOneTick()
        {
            if (IsComplete)
            {
                return;
            }

            foreach (var resident in _residents)
            {
                if (resident.Phase == ResidentTransitPhase.Queued)
                {
                    resident.WaitTicks++;
                }
            }

            foreach (var elevator in _elevators)
            {
                AdvanceElevator(elevator);
            }

            _tick = checked(_tick + 1);
        }

        public TransitPrototypeSnapshot Snapshot()
        {
            var residents = new List<TransitResidentSnapshot>(_residents.Count);
            var queued = 0;
            foreach (var resident in _residents)
            {
                residents.Add(new TransitResidentSnapshot(resident.Id, resident.DestinationFloor, resident.Phase));
                if (resident.Phase == ResidentTransitPhase.Queued)
                {
                    queued++;
                }
            }

            var elevators = new List<ElevatorCarSnapshot>(_elevators.Count);
            foreach (var elevator in _elevators)
            {
                var passengerIds = new List<EntityId>(elevator.Passengers.Count);
                foreach (var p in elevator.Passengers)
                {
                    passengerIds.Add(p.Id);
                }

                elevators.Add(new ElevatorCarSnapshot(elevator.Id, elevator.Floor, elevator.Passengers.Count, ElevatorCapacity, passengerIds));
            }

            var averageWait = _arrivedCount == 0 ? 0f : (float)_totalCompletedWait / _arrivedCount;
            return new TransitPrototypeSnapshot(
                CurrentTick,
                queued,
                _arrivedCount,
                averageWait,
                new ReadOnlyCollection<TransitResidentSnapshot>(residents),
                new ReadOnlyCollection<ElevatorCarSnapshot>(elevators));
        }

        private void AdvanceElevator(ElevatorState elevator)
        {
            switch (elevator.Direction)
            {
                case ElevatorDirection.Idle:
                    BoardFromLobby(elevator);
                    break;
                case ElevatorDirection.Up:
                    elevator.Floor++;
                    DeliverPassengers(elevator);
                    if (elevator.Floor == FloorCount - 1)
                    {
                        elevator.Direction = ElevatorDirection.Down;
                    }
                    break;
                case ElevatorDirection.Down:
                    elevator.Floor--;
                    if (elevator.Floor == 0)
                    {
                        elevator.Direction = ElevatorDirection.Idle;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown elevator direction.");
            }
        }

        private void BoardFromLobby(ElevatorState elevator)
        {
            foreach (var resident in _residents)
            {
                if (elevator.Passengers.Count == ElevatorCapacity)
                {
                    break;
                }

                if (resident.Phase != ResidentTransitPhase.Queued)
                {
                    continue;
                }

                resident.Phase = ResidentTransitPhase.Riding;
                elevator.Passengers.Add(resident);
            }

            if (elevator.Passengers.Count > 0)
            {
                elevator.Direction = ElevatorDirection.Up;
            }
        }

        private void DeliverPassengers(ElevatorState elevator)
        {
            for (var passengerIndex = elevator.Passengers.Count - 1; passengerIndex >= 0; passengerIndex--)
            {
                var passenger = elevator.Passengers[passengerIndex];
                if (passenger.DestinationFloor != elevator.Floor)
                {
                    continue;
                }

                passenger.Phase = ResidentTransitPhase.Arrived;
                _totalCompletedWait += passenger.WaitTicks;
                _arrivedCount++;
                elevator.Passengers.RemoveAt(passengerIndex);
            }
        }

        private sealed class ResidentState
        {
            public ResidentState(EntityId id, int destinationFloor)
            {
                Id = id;
                DestinationFloor = destinationFloor;
                Phase = ResidentTransitPhase.Queued;
            }

            public EntityId Id { get; }

            public int DestinationFloor { get; }

            public ResidentTransitPhase Phase { get; set; }

            public long WaitTicks { get; set; }
        }

        private sealed class ElevatorState
        {
            public ElevatorState(EntityId id)
            {
                Id = id;
            }

            public EntityId Id { get; }

            public int Floor { get; set; }

            public ElevatorDirection Direction { get; set; }

            public List<ResidentState> Passengers { get; } = new List<ResidentState>(ElevatorCapacity);
        }

        private enum ElevatorDirection
        {
            Idle,
            Up,
            Down
        }
    }
}
