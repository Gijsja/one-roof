using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Transit
{
    /// <summary>
    /// Immutable snapshot of an elevator bank's operational state.
    /// Encapsulates per-floor queue counts, car statuses, in-cabin riders, and delivered passengers
    /// so Application and Presentation layers do not access internal mutable collections directly.
    /// </summary>
    public sealed class ElevatorBankSnapshot
    {
        private readonly Dictionary<int, IReadOnlyList<ElevatorPassenger>> _floorQueues;
        private readonly Dictionary<EntityId, ElevatorPassenger> _queuedByPerson;
        private readonly Dictionary<EntityId, ElevatorPassenger> _ridingByPerson;
        private readonly Dictionary<EntityId, ElevatorPassenger> _deliveredByPerson;

        public ElevatorBankSnapshot(
            int minFloor,
            int maxFloor,
            IReadOnlyList<ElevatorCar> cars,
            IReadOnlyDictionary<int, IReadOnlyList<ElevatorPassenger>> floorQueues,
            IReadOnlyList<ElevatorPassenger> deliveredPassengers,
            float averageWaitTicks)
        {
            MinFloor = minFloor;
            MaxFloor = maxFloor;
            Cars = cars ?? Array.Empty<ElevatorCar>();
            DeliveredPassengers = deliveredPassengers ?? Array.Empty<ElevatorPassenger>();
            AverageWaitTicks = averageWaitTicks;

            var queueList = new List<ElevatorPassenger>();
            _floorQueues = new Dictionary<int, IReadOnlyList<ElevatorPassenger>>();
            _queuedByPerson = new Dictionary<EntityId, ElevatorPassenger>();

            if (floorQueues != null)
            {
                foreach (var kvp in floorQueues)
                {
                    _floorQueues[kvp.Key] = kvp.Value;
                    if (kvp.Value != null)
                    {
                        foreach (var p in kvp.Value)
                        {
                            queueList.Add(p);
                            _queuedByPerson[p.PersonId] = p;
                        }
                    }
                }
            }

            QueuedPassengers = queueList;

            _ridingByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var car in Cars)
            {
                if (car.Passengers != null)
                {
                    foreach (var p in car.Passengers)
                    {
                        _ridingByPerson[p.PersonId] = p;
                    }
                }
            }

            _deliveredByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var p in DeliveredPassengers)
            {
                _deliveredByPerson[p.PersonId] = p;
            }
        }

        public int MinFloor { get; }
        public int MaxFloor { get; }
        public IReadOnlyList<ElevatorCar> Cars { get; }
        public IReadOnlyList<ElevatorPassenger> QueuedPassengers { get; }
        public IReadOnlyList<ElevatorPassenger> DeliveredPassengers { get; }
        public float AverageWaitTicks { get; }

        public int TotalQueuedCount => QueuedPassengers.Count;
        public int QueuedCount => TotalQueuedCount;
        public int DeliveredCount => DeliveredPassengers.Count;

        public int GetQueueLength(int floor)
        {
            if (_floorQueues.TryGetValue(floor, out var list))
            {
                return list.Count;
            }
            return 0;
        }

        public IReadOnlyList<ElevatorPassenger> GetFloorQueue(int floor)
        {
            if (_floorQueues.TryGetValue(floor, out var list))
            {
                return list;
            }
            return Array.Empty<ElevatorPassenger>();
        }

        public bool TryGetQueuedPassenger(EntityId personId, out ElevatorPassenger passenger) =>
            _queuedByPerson.TryGetValue(personId, out passenger);

        public bool TryGetRidingPassenger(EntityId personId, out ElevatorPassenger passenger) =>
            _ridingByPerson.TryGetValue(personId, out passenger);

        public bool TryGetDeliveredPassenger(EntityId personId, out ElevatorPassenger passenger) =>
            _deliveredByPerson.TryGetValue(personId, out passenger);
    }
}
