using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Transit
{
    public sealed class ElevatorBank
    {
        public const int MaxCarsPerBank = 4;

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

        public int MinFloor { get; private set; }

        public int MaxFloor { get; private set; }

        public void ExpandFloorRange(int minFloor, int maxFloor)
        {
            if (minFloor < MinFloor)
            {
                for (var f = minFloor; f < MinFloor; f++)
                {
                    if (!_floorQueues.ContainsKey(f))
                    {
                        _floorQueues[f] = new Queue<ElevatorPassenger>();
                    }
                }
                MinFloor = minFloor;
            }

            if (maxFloor > MaxFloor)
            {
                for (var f = MaxFloor + 1; f <= maxFloor; f++)
                {
                    if (!_floorQueues.ContainsKey(f))
                    {
                        _floorQueues[f] = new Queue<ElevatorPassenger>();
                    }
                }
                MaxFloor = maxFloor;
            }
        }

        public void RestoreDeliveredPassengers(IEnumerable<ElevatorPassenger> passengers)
        {
            _deliveredPassengers.Clear();
            if (passengers != null)
            {
                _deliveredPassengers.AddRange(passengers);
            }
        }

        public IReadOnlyList<ElevatorCar> Cars { get; }

        internal IReadOnlyList<ElevatorPassenger> DeliveredPassengers { get; }

        internal IReadOnlyDictionary<int, Queue<ElevatorPassenger>> FloorQueues => _floorQueues;

        public ElevatorBankSnapshot Snapshot()
        {
            var floorQueues = new Dictionary<int, IReadOnlyList<ElevatorPassenger>>(_floorQueues.Count);
            foreach (var kvp in _floorQueues)
            {
                floorQueues[kvp.Key] = kvp.Value.ToArray();
            }

            var cars = new ElevatorCar[_cars.Count];
            for (var i = 0; i < _cars.Count; i++)
            {
                cars[i] = _cars[i];
            }

            return new ElevatorBankSnapshot(
                MinFloor,
                MaxFloor,
                cars,
                floorQueues,
                _deliveredPassengers.ToArray(),
                AverageWaitTicks);
        }

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

        public void AddCar(ElevatorCar car)
        {
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            _cars.Add(car);
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

        public bool TryTakeDeliveredPassenger(EntityId personId, out ElevatorPassenger passenger)
        {
            for (var i = 0; i < _deliveredPassengers.Count; i++)
            {
                if (_deliveredPassengers[i].PersonId.Equals(personId))
                {
                    passenger = _deliveredPassengers[i];
                    _deliveredPassengers.RemoveAt(i);
                    return true;
                }
            }

            passenger = null;
            return false;
        }

        public bool IsPassengerInCar(EntityId personId, out ElevatorCar car)
        {
            foreach (var c in _cars)
            {
                foreach (var p in c.Passengers)
                {
                    if (p.PersonId.Equals(personId))
                    {
                        car = c;
                        return true;
                    }
                }
            }

            car = null;
            return false;
        }

        public bool IsPassengerInQueue(EntityId personId, out int floor)
        {
            foreach (var kvp in _floorQueues)
            {
                foreach (var p in kvp.Value)
                {
                    if (p.PersonId.Equals(personId))
                    {
                        floor = kvp.Key;
                        return true;
                    }
                }
            }

            floor = -1;
            return false;
        }

        public void ClearDeliveredPassengers() => _deliveredPassengers.Clear();

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

        // ── Serialization ──────────────────────────────────────────────────────

        public ElevatorBankSaveData ToSaveData()
        {
            var carSaveList = new List<ElevatorCarSaveData>(_cars.Count);
            foreach (var car in _cars)
            {
                var riderList = new List<ElevatorPassengerSaveData>(car.Passengers.Count);
                foreach (var rider in car.Passengers)
                {
                    riderList.Add(new ElevatorPassengerSaveData
                    {
                        personId = rider.PersonId.Value,
                        originFloor = rider.OriginFloor,
                        destinationFloor = rider.DestinationFloor,
                        waitTicks = rider.WaitTicks,
                        rideTicks = rider.RideTicks
                    });
                }

                carSaveList.Add(new ElevatorCarSaveData
                {
                    id = car.Id.Value,
                    currentFloor = car.CurrentFloor,
                    capacity = car.Capacity,
                    phase = (int)car.Phase,
                    direction = (int)car.Direction,
                    timerTicksRemaining = car.TimerTicksRemaining,
                    passengers = riderList.ToArray()
                });
            }

            var queuedPassengerList = new List<ElevatorPassengerSaveData>();
            foreach (var queue in _floorQueues.Values)
            {
                foreach (var p in queue)
                {
                    queuedPassengerList.Add(new ElevatorPassengerSaveData
                    {
                        personId = p.PersonId.Value,
                        originFloor = p.OriginFloor,
                        destinationFloor = p.DestinationFloor,
                        waitTicks = p.WaitTicks,
                        rideTicks = p.RideTicks
                    });
                }
            }

            var deliveredPassengerList = new List<ElevatorPassengerSaveData>(_deliveredPassengers.Count);
            foreach (var p in _deliveredPassengers)
            {
                deliveredPassengerList.Add(new ElevatorPassengerSaveData
                {
                    personId = p.PersonId.Value,
                    originFloor = p.OriginFloor,
                    destinationFloor = p.DestinationFloor,
                    waitTicks = p.WaitTicks,
                    rideTicks = p.RideTicks
                });
            }

            return new ElevatorBankSaveData
            {
                minFloor = MinFloor,
                maxFloor = MaxFloor,
                cars = carSaveList.ToArray(),
                queuedPassengers = queuedPassengerList.ToArray(),
                deliveredPassengers = deliveredPassengerList.ToArray()
            };
        }

        public static ElevatorBank FromSaveData(ElevatorBankSaveData data)
        {
            var cars = new List<ElevatorCar>();
            if (data?.cars != null)
            {
                foreach (var c in data.cars)
                {
                    var car = new ElevatorCar(new EntityId(c.id), c.currentFloor, c.capacity);
                    var riders = new List<ElevatorPassenger>();
                    if (c.passengers != null)
                    {
                        foreach (var riderData in c.passengers)
                        {
                            riders.Add(new ElevatorPassenger(new EntityId(riderData.personId), riderData.originFloor, riderData.destinationFloor)
                            {
                                WaitTicks = riderData.waitTicks,
                                RideTicks = riderData.rideTicks
                            });
                        }
                    }

                    car.RestoreState(c.currentFloor, (ElevatorCarPhase)c.phase, (ElevatorDirection)c.direction, c.timerTicksRemaining, riders);
                    cars.Add(car);
                }
            }

            var minFloor = data?.minFloor ?? 0;
            var maxFloor = data?.maxFloor ?? 4;
            var elevatorBank = new ElevatorBank(minFloor, maxFloor, cars);

            if (data?.queuedPassengers != null)
            {
                foreach (var qp in data.queuedPassengers)
                {
                    elevatorBank.EnqueuePassenger(new ElevatorPassenger(new EntityId(qp.personId), qp.originFloor, qp.destinationFloor)
                    {
                        WaitTicks = qp.waitTicks,
                        RideTicks = qp.rideTicks
                    });
                }
            }

            if (data?.deliveredPassengers != null)
            {
                var delivered = new List<ElevatorPassenger>(data.deliveredPassengers.Length);
                foreach (var dp in data.deliveredPassengers)
                {
                    delivered.Add(new ElevatorPassenger(new EntityId(dp.personId), dp.originFloor, dp.destinationFloor)
                    {
                        WaitTicks = dp.waitTicks,
                        RideTicks = dp.rideTicks
                    });
                }
                elevatorBank.RestoreDeliveredPassengers(delivered);
            }

            return elevatorBank;
        }
    }
}
