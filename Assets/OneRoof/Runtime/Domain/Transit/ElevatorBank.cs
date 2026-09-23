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
        // The current shaft art has room for three legible cars. Keep this at the
        // domain boundary so commands, saves, predictions, and presentation agree.
        public const int MaxCarsPerBank = 3;
        public const int MinSupportedFloor = -10;
        public const int MaxSupportedFloor = 100;

        private readonly List<ElevatorCar> _cars;
        private readonly Dictionary<int, Queue<ElevatorPassenger>> _floorQueues;
        private readonly List<ElevatorPassenger> _deliveredPassengers;
        private int _cumulativeDeliveredCount;
        private long _cumulativeWaitTicks;

        public ElevatorBank(int minFloor, int maxFloor, IEnumerable<ElevatorCar> cars)
        {
            ValidateFloorRange(minFloor, maxFloor);

            MinFloor = minFloor;
            MaxFloor = maxFloor;
            _cars = new List<ElevatorCar>(cars ?? Array.Empty<ElevatorCar>());
            if (_cars.Count > MaxCarsPerBank)
            {
                throw new ArgumentException($"An elevator bank cannot contain more than {MaxCarsPerBank} cars.", nameof(cars));
            }
            _floorQueues = new Dictionary<int, Queue<ElevatorPassenger>>();
            _deliveredPassengers = new List<ElevatorPassenger>();

            for (var floor = minFloor; ; floor++)
            {
                _floorQueues[floor] = new Queue<ElevatorPassenger>();
                if (floor == maxFloor) break;
            }

            Cars = new ReadOnlyCollection<ElevatorCar>(_cars);
            DeliveredPassengers = new ReadOnlyCollection<ElevatorPassenger>(_deliveredPassengers);
        }

        public int MinFloor { get; private set; }

        public int MaxFloor { get; private set; }

        public void ExpandFloorRange(int minFloor, int maxFloor)
        {
            ValidateFloorRange(minFloor, maxFloor);
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
                for (var f = MaxFloor + 1; ; f++)
                {
                    if (!_floorQueues.ContainsKey(f))
                    {
                        _floorQueues[f] = new Queue<ElevatorPassenger>();
                    }
                    if (f == maxFloor) break;
                }
                MaxFloor = maxFloor;
            }
        }

        private static void ValidateFloorRange(int minFloor, int maxFloor)
        {
            if (minFloor > maxFloor)
                throw new ArgumentException($"minFloor ({minFloor}) cannot exceed maxFloor ({maxFloor}).");
            if (minFloor < MinSupportedFloor || maxFloor > MaxSupportedFloor)
                throw new ArgumentOutOfRangeException(nameof(maxFloor),
                    $"Elevator floor range must stay within [{MinSupportedFloor}, {MaxSupportedFloor}].");
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

        public int DeliveredCount => _cumulativeDeliveredCount + _deliveredPassengers.Count;

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

            if (_cars.Count >= MaxCarsPerBank)
            {
                throw new InvalidOperationException($"An elevator bank cannot contain more than {MaxCarsPerBank} cars.");
            }

            _cars.Add(car);
            RebalanceDispatches();
        }

        public float AverageWaitTicks
        {
            get
            {
                var totalDelivered = _cumulativeDeliveredCount + _deliveredPassengers.Count;
                if (totalDelivered == 0) return 0f;
                long totalWait = _cumulativeWaitTicks;
                foreach (var p in _deliveredPassengers)
                {
                    totalWait += p.WaitTicks;
                }

                return (float)totalWait / totalDelivered;
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
                    _cumulativeDeliveredCount++;
                    _cumulativeWaitTicks += passenger.WaitTicks;
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
            RebalanceDispatches();
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

                    if (car.Passengers.Count >= car.Capacity)
                    {
                        car.ClearPickupTargets();
                    }
                }
            }

            // 4. Rebalance unserviced floor calls to best available cars
            RebalanceDispatches();
        }

        private void RebalanceDispatches()
        {
            var floorsWithQueues = new List<(int floor, long maxWait)>();
            foreach (var kvp in _floorQueues)
            {
                if (kvp.Value.Count > 0)
                {
                    var (maxWait, _) = GetFloorWaitMetrics(kvp.Key);
                    floorsWithQueues.Add((kvp.Key, maxWait));
                }
                else
                {
                    foreach (var car in _cars)
                    {
                        car.RemoveTargetFloor(kvp.Key);
                    }
                }
            }

            floorsWithQueues.Sort((a, b) => b.maxWait.CompareTo(a.maxWait));

            foreach (var item in floorsWithQueues)
            {
                var floor = item.floor;
                var bestCar = FindBestCarForFloor(floor);
                if (bestCar != null)
                {
                    bestCar.RequestFloor(floor);

                    foreach (var other in _cars)
                    {
                        if (other != bestCar)
                        {
                            other.RemoveTargetFloor(floor);
                        }
                    }
                }
            }
        }

        private ElevatorCar FindBestCarForFloor(int floor)
        {
            ElevatorCar bestCar = null;
            var minCost = int.MaxValue;

            foreach (var car in _cars)
            {
                if (car.Passengers.Count >= car.Capacity)
                {
                    continue;
                }
                var dist = Math.Abs(car.CurrentFloor - floor);
                int cost;

                if (car.Phase == ElevatorCarPhase.Idle)
                {
                    cost = dist * 4 + car.RequestCount * 10;
                }
                else
                {
                    var isAtFloor = (car.CurrentFloor == floor);
                    var onTheWay = isAtFloor ||
                                  (car.Direction == ElevatorDirection.Up && floor > car.CurrentFloor) ||
                                  (car.Direction == ElevatorDirection.Down && floor < car.CurrentFloor);

                    if (onTheWay && car.Passengers.Count < car.Capacity)
                    {
                        cost = dist * 4 + car.RequestCount * 3;
                    }
                    else
                    {
                        cost = dist * 4 + 30 + car.RequestCount * 10 + car.Passengers.Count * 5;
                    }
                }

                if (car.HasTargetFloor(floor))
                {
                    cost -= 5;
                }

                if (cost < minCost)
                {
                    minCost = cost;
                    bestCar = car;
                }
            }

            return bestCar;
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
                deliveredPassengers = deliveredPassengerList.ToArray(),
                cumulativeDeliveredCount = _cumulativeDeliveredCount,
                cumulativeWaitTicks = _cumulativeWaitTicks
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
            elevatorBank._cumulativeDeliveredCount = data?.cumulativeDeliveredCount ?? 0;
            elevatorBank._cumulativeWaitTicks = data?.cumulativeWaitTicks ?? 0;

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
