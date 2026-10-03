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
        private ElevatorTierProfile _tierProfile;

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
            _tierProfile = ElevatorTierProfile.ForTier(1);

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

        public int Tier => _tierProfile.Tier;

        /// <summary>Applies a deterministic bank-wide tier; active trips finish using their current safe phase.</summary>
        public void SetTier(int tier)
        {
            var profile = ElevatorTierProfile.ForTier(tier);
            _tierProfile = profile;
            foreach (var car in _cars) car.ApplyTier(profile);
            RebalanceDispatches();
        }

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

            // Preserve custom baseline cars, but additions to an upgraded bank
            // must receive the same transport profile as its existing cars.
            if (Tier > 1) car.ApplyTier(_tierProfile);
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

        public long GetPassengerWaitTicks(EntityId personId)
        {
            foreach (var queue in _floorQueues.Values)
                foreach (var passenger in queue)
                    if (passenger.PersonId == personId) return passenger.WaitTicks;
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

        public bool TryRemoveQueuedPassenger(EntityId personId, out ElevatorPassenger passenger)
        {
            foreach (var kvp in _floorQueues)
            {
                var queue = kvp.Value;
                if (queue.Count == 0) continue;
                ElevatorPassenger found = null;
                var list = new List<ElevatorPassenger>(queue.Count);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    if (found == null && p.PersonId.Equals(personId))
                    {
                        found = p;
                    }
                    else
                    {
                        list.Add(p);
                    }
                }
                foreach (var p in list) queue.Enqueue(p);
                if (found != null)
                {
                    passenger = found;
                    return true;
                }
            }

            passenger = null;
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

        public Action<long> ObservePreboardingWait { get; set; }

        public void Advance(Tick tick, float lobbyCapacityMultiplier = 1f)
        {
            if (lobbyCapacityMultiplier <= 0f || lobbyCapacityMultiplier > 1f)
                throw new ArgumentOutOfRangeException(nameof(lobbyCapacityMultiplier));
            // 1. Advance wait timers for all waiting passengers
            foreach (var queue in _floorQueues.Values)
            {
                foreach (var passenger in queue)
                {
                    passenger.WaitTicks++;
                    ObservePreboardingWait?.Invoke(passenger.WaitTicks);
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
                        var travelDir = car.Direction;
                        if (travelDir == ElevatorDirection.None && queue.Count > 0)
                        {
                            var first = queue.Peek();
                            travelDir = first.DestinationFloor > car.CurrentFloor ? ElevatorDirection.Up
                                : first.DestinationFloor < car.CurrentFloor ? ElevatorDirection.Down
                                : ElevatorDirection.None;
                        }

                        var reserved = 0;
                        if (car.CurrentFloor == 0 && travelDir == ElevatorDirection.Up)
                        {
                            for (var f = 1; f <= 4; f++)
                            {
                                if (car.HasTargetFloor(f) && _floorQueues.TryGetValue(f, out var q))
                                {
                                    reserved += q.Count;
                                }
                            }
                        }
                        else if (car.CurrentFloor == 29 && travelDir == ElevatorDirection.Down)
                        {
                            for (var f = 28; f >= 25; f--)
                            {
                                if (car.HasTargetFloor(f) && _floorQueues.TryGetValue(f, out var q))
                                {
                                    reserved += q.Count;
                                }
                            }
                        }
                        reserved = Math.Min(car.Capacity / 2, reserved);

                        var effectiveCap = Math.Max(1, car.Capacity - reserved);
                        var boardingLimit = car.CurrentFloor == 0
                            ? Math.Max(1, Math.Min(effectiveCap, (int)Math.Floor(car.Capacity * lobbyCapacityMultiplier)))
                            : effectiveCap;
                        BoardPassengersDirectional(car, queue, boardingLimit);
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

        private void BoardPassengersDirectional(ElevatorCar car, Queue<ElevatorPassenger> queue, int boardingLimit)
        {
            if (queue == null || queue.Count == 0 || !car.CanBoard || car.Passengers.Count >= boardingLimit)
                return;

            var effectiveDirection = car.Direction;
            if (effectiveDirection == ElevatorDirection.None)
            {
                var first = queue.Peek();
                effectiveDirection = first.DestinationFloor > car.CurrentFloor ? ElevatorDirection.Up
                    : first.DestinationFloor < car.CurrentFloor ? ElevatorDirection.Down
                    : ElevatorDirection.None;
            }

            var unboarded = new List<ElevatorPassenger>(queue.Count);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var passengerDirection = p.DestinationFloor > car.CurrentFloor ? ElevatorDirection.Up
                    : p.DestinationFloor < car.CurrentFloor ? ElevatorDirection.Down
                    : ElevatorDirection.None;

                if (car.CanBoard && car.Passengers.Count < boardingLimit &&
                    (effectiveDirection == ElevatorDirection.None || passengerDirection == effectiveDirection))
                {
                    car.Board(p);
                    if (effectiveDirection == ElevatorDirection.None)
                    {
                        effectiveDirection = passengerDirection;
                    }
                }
                else
                {
                    unboarded.Add(p);
                }
            }

            // If car still has capacity and has no more targets in its current direction,
            // it can accept remaining passengers (reversal)
            if (car.CanBoard && car.Passengers.Count < boardingLimit && !car.HasTargetsInDirection(effectiveDirection))
            {
                var stillUnboarded = new List<ElevatorPassenger>(unboarded.Count);
                for (var i = 0; i < unboarded.Count; i++)
                {
                    if (car.CanBoard && car.Passengers.Count < boardingLimit)
                    {
                        car.Board(unboarded[i]);
                    }
                    else
                    {
                        stillUnboarded.Add(unboarded[i]);
                    }
                }
                unboarded = stillUnboarded;
            }

            for (var i = 0; i < unboarded.Count; i++)
            {
                queue.Enqueue(unboarded[i]);
            }
        }


        private ElevatorDirection GetFloorQueueDirection(int floor)
        {
            if (!_floorQueues.TryGetValue(floor, out var queue) || queue.Count == 0)
                return ElevatorDirection.None;

            var first = queue.Peek();
            return first.DestinationFloor > floor ? ElevatorDirection.Up
                : first.DestinationFloor < floor ? ElevatorDirection.Down
                : ElevatorDirection.None;
        }

        private bool CanTargetFloorNow(ElevatorCar car, int floor, ElevatorDirection queueDir)
        {
            if (car.CurrentFloor == floor &&
                (car.Phase == ElevatorCarPhase.DoorsClosing ||
                 car.Phase == ElevatorCarPhase.Moving ||
                 car.Phase == ElevatorCarPhase.OpenLoading))
            {
                return false;
            }

            if (car.Direction == ElevatorDirection.None)
                return true;

            if (car.Direction == ElevatorDirection.Up)
            {
                if (queueDir == ElevatorDirection.Up)
                    return floor >= car.CurrentFloor;

                if (queueDir == ElevatorDirection.Down)
                {
                    var highestTarget = car.CurrentFloor;
                    foreach (var p in car.Passengers)
                        if (p.DestinationFloor > highestTarget) highestTarget = p.DestinationFloor;
                    foreach (var t in car.TargetFloors)
                        if (t > highestTarget) highestTarget = t;
                    return floor >= highestTarget;
                }

                return true;
            }

            if (car.Direction == ElevatorDirection.Down)
            {
                if (queueDir == ElevatorDirection.Down)
                    return floor <= car.CurrentFloor;

                if (queueDir == ElevatorDirection.Up)
                {
                    var lowestTarget = car.CurrentFloor;
                    foreach (var p in car.Passengers)
                        if (p.DestinationFloor < lowestTarget) lowestTarget = p.DestinationFloor;
                    foreach (var t in car.TargetFloors)
                        if (t < lowestTarget) lowestTarget = t;
                    return floor <= lowestTarget;
                }

                return true;
            }

            return true;
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

            floorsWithQueues.Sort((a, b) =>
            {
                var waitOrder = b.maxWait.CompareTo(a.maxWait);
                return waitOrder != 0 ? waitOrder : a.floor.CompareTo(b.floor);
            });

            var committedCapacityUp = new Dictionary<ElevatorCar, int>(_cars.Count);
            var committedCapacityDown = new Dictionary<ElevatorCar, int>(_cars.Count);
            foreach (var car in _cars)
            {
                var upCount = 0;
                var downCount = 0;
                foreach (var p in car.Passengers)
                {
                    if (p.DestinationFloor > car.CurrentFloor) upCount++;
                    else if (p.DestinationFloor < car.CurrentFloor) downCount++;
                    else
                    {
                        if (car.Direction == ElevatorDirection.Up) upCount++;
                        else if (car.Direction == ElevatorDirection.Down) downCount++;
                    }
                }
                committedCapacityUp[car] = upCount;
                committedCapacityDown[car] = downCount;
            }

            foreach (var item in floorsWithQueues)
            {
                var floor = item.floor;
                var queueCount = _floorQueues[floor].Count;
                var queueDir = GetFloorQueueDirection(floor);
                var assignedCapacity = 0;
                var assignedCars = new List<ElevatorCar>();
                var committedCapacity = queueDir == ElevatorDirection.Down ? committedCapacityDown : committedCapacityUp;

                var maxCarsPerFloor = Math.Max(1, _cars.Count - 1);
                while (assignedCapacity < queueCount && assignedCars.Count < maxCarsPerFloor)
                {
                    ElevatorCar bestCar = null;
                    long minCost = long.MaxValue;

                    foreach (var car in _cars)
                    {
                        if (assignedCars.Contains(car)) continue;
                        if (committedCapacity[car] >= car.Capacity) continue;
                        if (!CanTargetFloorNow(car, floor, queueDir)) continue;

                        long cost = _tierProfile.EtaDispatch
                            ? EstimatePickupCost(car, floor, queueDir)
                            : (long)Math.Abs(car.CurrentFloor - floor) * 5L + car.RequestCount * 10L;

                        if (car.HasTargetFloor(floor))
                        {
                            cost = Math.Max(0L, cost - 10L);
                        }

                        if (cost < minCost || (cost == minCost && (bestCar == null || car.Id.CompareTo(bestCar.Id) < 0)))
                        {
                            minCost = cost;
                            bestCar = car;
                        }
                    }

                    if (bestCar == null) break;

                    bestCar.RequestFloor(floor);
                    assignedCars.Add(bestCar);
                    var canTake = Math.Min(queueCount - assignedCapacity, bestCar.Capacity - committedCapacity[bestCar]);
                    assignedCapacity += canTake;
                    committedCapacity[bestCar] += canTake;
                }

                foreach (var other in _cars)
                {
                    if (!assignedCars.Contains(other))
                    {
                        other.RemoveTargetFloor(floor);
                    }
                }
            }
        }

        private long EstimatePickupCost(ElevatorCar car, int floor, ElevatorDirection queueDir)
        {
            var timing = car.Timing;
            var moving = car.Phase == ElevatorCarPhase.Moving ? car.TimerTicksRemaining : 0;
            var doorWork = (long)car.RequestCount * (timing.DoorCycleTicks * 2L + timing.DwellTicks);
            var load = (long)car.Passengers.Count * (timing.DoorCycleTicks + timing.DwellTicks) /
                       Math.Max(1, car.Capacity);
            var waitCredit = Math.Min(_tierProfile.WaitCreditCapTicks, GetFloorWaitMetrics(floor).MaxWaitTicks / 4);
            var committedStops = doorWork + load;

            long effectiveDist;
            long turnaroundPenalty = 0;

            if (car.Direction == ElevatorDirection.None)
            {
                effectiveDist = Math.Abs(car.CurrentFloor - floor);
            }
            else if (car.Direction == ElevatorDirection.Up)
            {
                if (queueDir == ElevatorDirection.Up && floor >= car.CurrentFloor)
                {
                    effectiveDist = floor - car.CurrentFloor;
                }
                else
                {
                    var highest = car.CurrentFloor;
                    foreach (var t in car.TargetFloors)
                        if (t > highest) highest = t;
                    if (floor > highest) highest = floor;

                    effectiveDist = (highest - car.CurrentFloor) + Math.Abs(highest - floor);
                    turnaroundPenalty = timing.FloorTravelTicks * 20L;
                }
            }
            else // ElevatorDirection.Down
            {
                if (queueDir == ElevatorDirection.Down && floor <= car.CurrentFloor)
                {
                    effectiveDist = car.CurrentFloor - floor;
                }
                else
                {
                    var lowest = car.CurrentFloor;
                    foreach (var t in car.TargetFloors)
                        if (t < lowest) lowest = t;
                    if (floor < lowest) lowest = floor;

                    effectiveDist = (car.CurrentFloor - lowest) + Math.Abs(floor - lowest);
                    turnaroundPenalty = timing.FloorTravelTicks * 20L;
                }
            }

            var predictedTicks = moving + effectiveDist * timing.FloorTravelTicks + committedStops + turnaroundPenalty;
            return Math.Max(0L, predictedTicks - waitCredit);
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
                    targetFloors = new List<int>(car.TargetFloors).ToArray(),
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
                tier = Tier,
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
                    if (c.targetFloors != null) car.RestoreTargets(c.targetFloors);
                    cars.Add(car);
                }
            }

            var minFloor = data?.minFloor ?? 0;
            var maxFloor = data?.maxFloor ?? 4;
            var elevatorBank = new ElevatorBank(minFloor, maxFloor, cars);
            elevatorBank.SetTier(data != null && data.tier >= 1 && data.tier <= 5 ? data.tier : 1);
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

            // Queue reconstruction rebalances dispatch; restore the saved execution only after it.
            if (data?.cars != null)
                for (var i = 0; i < data.cars.Length; i++)
                {
                    var saved = data.cars[i];
                    if (saved.targetFloors == null) continue; // Legacy reconstruction remains supported.
                    var car = cars[i];
                    var riders = new List<ElevatorPassenger>(car.Passengers);
                    car.RestoreState(saved.currentFloor, (ElevatorCarPhase)saved.phase,
                        (ElevatorDirection)saved.direction, saved.timerTicksRemaining, riders);
                    car.RestoreTargets(saved.targetFloors);
                }
            return elevatorBank;
        }
    }
}
