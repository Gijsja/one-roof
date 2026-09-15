using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Transit
{
    public sealed class ElevatorBank
    {
        private readonly List<ElevatorCar> _cars;
        private readonly Dictionary<int, Queue<ElevatorPassenger>> _floorQueues;
        private readonly List<ElevatorPassenger> _deliveredPassengers;

        public ElevatorBank(int minFloor, int maxFloor, IEnumerable<ElevatorCar> cars)
        {
            if (minFloor > maxFloor)
            {
                throw new ArgumentException($"minFloor ({minFloor}) cannot exceed maxFloor ({maxFloor}).");
            }

            MinFloor = minFloor;
            MaxFloor = maxFloor;
            _cars = new List<ElevatorCar>(cars ?? Array.Empty<ElevatorCar>());
            _floorQueues = new Dictionary<int, Queue<ElevatorPassenger>>();
            _deliveredPassengers = new List<ElevatorPassenger>();

            for (var floor = minFloor; floor <= maxFloor; floor++)
            {
                _floorQueues[floor] = new Queue<ElevatorPassenger>();
            }

            Cars = new ReadOnlyCollection<ElevatorCar>(_cars);
            DeliveredPassengers = new ReadOnlyCollection<ElevatorPassenger>(_deliveredPassengers);
        }

        public int MinFloor { get; }

        public int MaxFloor { get; }

        public IReadOnlyList<ElevatorCar> Cars { get; }

        public IReadOnlyList<ElevatorPassenger> DeliveredPassengers { get; }

        public int DeliveredCount => _deliveredPassengers.Count;

        public int TotalQueuedCount
        {
            get
            {
                var total = 0;
                foreach (var queue in _floorQueues.Values)
                {
                    total += queue.Count;
                }

                return total;
            }
        }

        public float AverageWaitTicks
        {
            get
            {
                if (_deliveredPassengers.Count == 0) return 0f;
                long totalWait = 0;
                foreach (var p in _deliveredPassengers)
                {
                    totalWait += p.WaitTicks;
                }

                return (float)totalWait / _deliveredPassengers.Count;
            }
        }

        public int GetQueueLength(int floor)
        {
            if (_floorQueues.TryGetValue(floor, out var queue))
            {
                return queue.Count;
            }

            return 0;
        }

        public (long MaxWaitTicks, float AverageWaitTicks) GetFloorWaitMetrics(int floor)
        {
            if (!_floorQueues.TryGetValue(floor, out var queue) || queue.Count == 0)
            {
                return (0L, 0f);
            }

            long max = 0;
            long total = 0;
            foreach (var passenger in queue)
            {
                if (passenger.WaitTicks > max)
                {
                    max = passenger.WaitTicks;
                }

                total += passenger.WaitTicks;
            }

            return (max, (float)total / queue.Count);
        }

        public void EnqueuePassenger(ElevatorPassenger passenger)
        {
            if (passenger == null)
            {
                throw new ArgumentNullException(nameof(passenger));
            }

            if (passenger.DestinationFloor < MinFloor || passenger.DestinationFloor > MaxFloor)
            {
                throw new ArgumentOutOfRangeException(nameof(passenger),
                    $"Destination floor {passenger.DestinationFloor} is outside bank range [{MinFloor}..{MaxFloor}].");
            }

            if (!_floorQueues.TryGetValue(passenger.OriginFloor, out var queue))
            {
                throw new ArgumentOutOfRangeException(nameof(passenger), $"Origin floor {passenger.OriginFloor} is outside bank range [{MinFloor}..{MaxFloor}].");
            }

            queue.Enqueue(passenger);
            DispatchCarToFloor(passenger.OriginFloor);
        }

        public void Advance(Tick tick)
        {
            // 1. Advance wait timers for all waiting passengers
            foreach (var queue in _floorQueues.Values)
            {
                foreach (var passenger in queue)
                {
                    passenger.WaitTicks++;
                }
            }

            // 2. Advance ride timers for passengers inside cars
            foreach (var car in _cars)
            {
                foreach (var passenger in car.Passengers)
                {
                    passenger.RideTicks++;
                }
            }

            // 3. Advance each car and process loading/unloading
            foreach (var car in _cars)
            {
                // If car is already in OpenLoading before tick, or reaches it during tick:
                var wasOpenLoading = car.Phase == ElevatorCarPhase.OpenLoading;

                car.Tick();

                var isOpenLoading = car.Phase == ElevatorCarPhase.OpenLoading;

                if (isOpenLoading)
                {
                    // A. Discharge arrived passengers
                    var discharged = car.DischargeArrivedPassengers();
                    _deliveredPassengers.AddRange(discharged);

                    // B. Board waiting passengers up to remaining capacity
                    if (_floorQueues.TryGetValue(car.CurrentFloor, out var queue))
                    {
                        while (car.CanBoard && queue.Count > 0)
                        {
                            var nextPassenger = queue.Dequeue();
                            car.Board(nextPassenger);
                        }
                    }
                }

                // If car becomes Idle and there are active queues, dispatch it
                if (car.Phase == ElevatorCarPhase.Idle && !car.HasRequests)
                {
                    CheckForUnservicedCalls(car);
                }
            }
        }

        private void DispatchCarToFloor(int floor)
        {
            // Find best car: prefer nearest idle car, then nearest moving car
            ElevatorCar bestCar = null;
            var minDistance = int.MaxValue;

            foreach (var car in _cars)
            {
                if (car.Phase == ElevatorCarPhase.Idle)
                {
                    var dist = Math.Abs(car.CurrentFloor - floor);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestCar = car;
                    }
                }
            }

            if (bestCar == null && _cars.Count > 0)
            {
                // Fallback: pick any car
                bestCar = _cars[0];
            }

            bestCar?.RequestFloor(floor);
        }

        private void CheckForUnservicedCalls(ElevatorCar car)
        {
            var nearestFloor = -1;
            var minDelta = int.MaxValue;

            foreach (var kvp in _floorQueues)
            {
                if (kvp.Value.Count > 0)
                {
                    var delta = Math.Abs(kvp.Key - car.CurrentFloor);
                    if (delta < minDelta)
                    {
                        minDelta = delta;
                        nearestFloor = kvp.Key;
                    }
                }
            }

            if (nearestFloor >= 0)
            {
                car.RequestFloor(nearestFloor);
            }
        }
    }
}
