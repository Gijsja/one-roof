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
        private readonly Dictionary<(int floor, int carId), ElevatorDirection> _pickupDirections =
            new Dictionary<(int floor, int carId), ElevatorDirection>();
        public const int SecondsPerTick = 60;
        public const int SubstepSeconds = 5;

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
            BeginAdvanceTick(lobbyCapacityMultiplier);
            for (var step = 0; step < SecondsPerTick / SubstepSeconds; step++)
                AdvanceSubstep(lobbyCapacityMultiplier, step == 0);
        }

        internal void BeginAdvanceTick(float lobbyCapacityMultiplier)
        {
            if (lobbyCapacityMultiplier <= 0f || lobbyCapacityMultiplier > 1f)
                throw new ArgumentOutOfRangeException(nameof(lobbyCapacityMultiplier));
            foreach (var queue in _floorQueues.Values)
                foreach (var passenger in queue)
                {
                    passenger.WaitTicks++;
                    ObservePreboardingWait?.Invoke(passenger.WaitTicks);
                }
            foreach (var car in _cars)
                foreach (var passenger in car.Passengers) passenger.RideTicks++;
            RebalanceDispatches();
        }

        internal void AdvanceSubstep(float lobbyCapacityMultiplier, bool advanceLegacy)
        {
            foreach (var car in _cars)
            {
                if (car.Timing.UsesSeconds)
                    car.AdvanceSeconds(SubstepSeconds, open => ProcessLanding(open, lobbyCapacityMultiplier));
                else if (advanceLegacy)
                {
                    car.Tick();
                    if (car.Phase == ElevatorCarPhase.OpenLoading) ProcessLanding(car, lobbyCapacityMultiplier);
                }
            }
            RebalanceDispatches();
        }

        private void ProcessLanding(ElevatorCar car, float lobbyCapacityMultiplier)
        {
            _deliveredPassengers.AddRange(car.DischargeArrivedPassengers());
            if (!_floorQueues.TryGetValue(car.CurrentFloor, out var queue) || queue.Count == 0) return;
            var direction = car.Direction;
            if (car.Passengers.Count == 0 && _pickupDirections.TryGetValue((car.CurrentFloor, car.Id.Value), out var committed))
            {
                direction = committed;
                car.SetServiceDirection(direction);
            }
            if (direction == ElevatorDirection.None) direction = PassengerDirection(queue.Peek());

            // At either configured endpoint, keep seats only for actual compatible calls ahead.
            var reserved = 0;
            if ((car.CurrentFloor == MinFloor && direction == ElevatorDirection.Up) ||
                (car.CurrentFloor == MaxFloor && direction == ElevatorDirection.Down))
            {
                foreach (var target in car.TargetFloors)
                    if (Ahead(car.CurrentFloor, target, direction) &&
                        _pickupDirections.TryGetValue((target, car.Id.Value), out var targetDirection) && targetDirection == direction)
                        reserved += DirectionalQueueCount(target, direction);
                reserved = Math.Min(car.Capacity / 2, reserved);
            }
            var boardingLimit = car.Capacity - reserved;
            if (car.CurrentFloor == 0)
                boardingLimit = Math.Max(1, Math.Min(boardingLimit, (int)Math.Floor(car.Capacity * lobbyCapacityMultiplier)));
            BoardPassengersDirectional(car, queue, boardingLimit, direction);
            if (car.Passengers.Count >= car.Capacity) car.ClearPickupTargets();
        }

        private static ElevatorDirection PassengerDirection(ElevatorPassenger passenger) =>
            passenger.DestinationFloor > passenger.OriginFloor ? ElevatorDirection.Up :
            passenger.DestinationFloor < passenger.OriginFloor ? ElevatorDirection.Down : ElevatorDirection.None;

        private static bool Ahead(int origin, int floor, ElevatorDirection direction) =>
            direction == ElevatorDirection.Up ? floor > origin : direction == ElevatorDirection.Down && floor < origin;

        private int DirectionalQueueCount(int floor, ElevatorDirection direction)
        {
            var count = 0;
            if (_floorQueues.TryGetValue(floor, out var queue))
                foreach (var passenger in queue) if (PassengerDirection(passenger) == direction) count++;
            return count;
        }

        private void BoardPassengersDirectional(ElevatorCar car, Queue<ElevatorPassenger> queue, int limit, ElevatorDirection direction)
        {
            // Rotate exactly the original queue length, preserving FIFO within both directions.
            var count = queue.Count;
            for (var i = 0; i < count; i++)
            {
                var passenger = queue.Dequeue();
                if (car.CanBoard && car.Passengers.Count < limit && PassengerDirection(passenger) == direction)
                    car.Board(passenger);
                else queue.Enqueue(passenger);
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
            var calls = new List<(int floor, ElevatorDirection direction, int count, long wait)>();
            foreach (var item in _floorQueues)
            {
                foreach (var direction in new[] { ElevatorDirection.Up, ElevatorDirection.Down, ElevatorDirection.None })
                {
                    var count = 0;
                    long oldest = 0;
                    foreach (var passenger in item.Value)
                        if (PassengerDirection(passenger) == direction) { count++; oldest = Math.Max(oldest, passenger.WaitTicks); }
                    if (count > 0) calls.Add((item.Key, direction, count, oldest));
                }
            }
            calls.Sort((a, b) =>
            {
                var order = b.wait.CompareTo(a.wait);
                if (order != 0) return order;
                order = a.floor.CompareTo(b.floor);
                return order != 0 ? order : a.direction.CompareTo(b.direction);
            });
            var stale = new List<(int floor, int carId)>();
            foreach (var item in _pickupDirections)
            {
                ElevatorCar car = null;
                foreach (var candidate in _cars) if (candidate.Id.Value == item.Key.carId) car = candidate;
                if (car == null || DirectionalQueueCount(item.Key.floor, item.Value) == 0 ||
                    (car.Passengers.Count >= car.Capacity && !car.HasTargetFloor(item.Key.floor))) stale.Add(item.Key);
            }
            foreach (var key in stale) _pickupDirections.Remove(key);
            foreach (var car in _cars)
                foreach (var floor in new List<int>(car.TargetFloors))
                    if (!_pickupDirections.ContainsKey((floor, car.Id.Value))) car.RemoveTargetFloor(floor);

            var committedSeats = new Dictionary<ElevatorCar, int>();
            foreach (var car in _cars) committedSeats[car] = car.Passengers.Count;
            var protectedCars = new HashSet<ElevatorCar>();
            foreach (var call in calls)
            {
                var assigned = new List<ElevatorCar>();
                var capacity = 0;
                // Keep accepted pickups stable; newer calls cannot steal the assigned car.
                foreach (var car in _cars)
                    if (_pickupDirections.TryGetValue((call.floor, car.Id.Value), out var direction) && direction == call.direction &&
                        car.HasTargetFloor(call.floor) ||
                        (_pickupDirections.TryGetValue((call.floor, car.Id.Value), out direction) && direction == call.direction &&
                         car.CurrentFloor == call.floor && car.Phase == ElevatorCarPhase.DoorsOpening))
                    {
                        var seats = Math.Max(0, car.Capacity - committedSeats[car]);
                        var take = Math.Min(call.count - capacity, seats);
                        if (take <= 0) continue;
                        assigned.Add(car); capacity += take; committedSeats[car] += take;
                    }
                while (capacity < call.count)
                {
                    ElevatorCar best = null;
                    long cost = long.MaxValue;
                    foreach (var car in _cars)
                    {
                        if (assigned.Contains(car) || protectedCars.Contains(car) || committedSeats[car] >= car.Capacity) continue;
                        if (_pickupDirections.TryGetValue((call.floor, car.Id.Value), out var direction) && direction != call.direction) continue;
                        if (!CanTargetFloorNow(car, call.floor, call.direction)) continue;
                        var predicted = _tierProfile.EtaDispatch ? EstimatePickupSeconds(car, call.floor, call.direction) :
                            (long)Math.Abs(car.CurrentFloor - call.floor) * car.Timing.FloorTravelSeconds +
                            car.RequestCount * (car.Timing.DoorCycleSeconds * 2L + car.Timing.DwellSeconds);
                        if (predicted < cost || (predicted == cost && (best == null || car.Id.CompareTo(best.Id) < 0)))
                        { best = car; cost = predicted; }
                    }
                    if (best == null) break;
                    _pickupDirections[(call.floor, best.Id.Value)] = call.direction;
                    best.RequestFloor(call.floor);
                    assigned.Add(best);
                    var take = Math.Min(call.count - capacity, best.Capacity - committedSeats[best]);
                    capacity += take; committedSeats[best] += take;
                }
                // An aged call owns its empty car until pickup, bounding starvation under fresh demand.
                if (call.wait >= 60)
                    foreach (var car in assigned)
                    {
                        protectedCars.Add(car);
                        if (car.Passengers.Count == 0 && car.CurrentFloor != call.floor)
                        {
                            car.ClearPickupTargets();
                            car.RequestFloor(call.floor);
                        }
                    }
            }
        }

        private long EstimatePickupSeconds(ElevatorCar car, int floor, ElevatorDirection queueDirection)
        {
            var timing = car.Timing;
            if (car.CurrentFloor == floor && (car.Direction == ElevatorDirection.None || car.Direction == queueDirection))
            {
                if (car.Phase == ElevatorCarPhase.OpenLoading) return 0;
                if (car.Phase == ElevatorCarPhase.DoorsOpening)
                    return car.Timing.UsesSeconds ? car.TimerSecondsRemaining : car.TimerTicksRemaining * 60L;
            }
            var distance = Math.Abs(car.CurrentFloor - floor);
            var direction = car.Direction;
            var turning = direction != ElevatorDirection.None &&
                (!Ahead(car.CurrentFloor, floor, direction) || queueDirection != direction);
            var extreme = car.CurrentFloor;
            if (turning)
            {
                foreach (var target in car.TargetFloors)
                    if (Ahead(extreme, target, direction)) extreme = target;
                if (Ahead(extreme, floor, direction)) extreme = floor;
                distance = Math.Abs(extreme - car.CurrentFloor) + Math.Abs(extreme - floor);
            }
            long seconds = (long)distance * timing.FloorTravelSeconds;
            if (car.Phase == ElevatorCarPhase.Moving && distance > 0)
                seconds += (car.Timing.UsesSeconds ? car.TimerSecondsRemaining : car.TimerTicksRemaining * 60L) - timing.FloorTravelSeconds;
            else if (car.Phase != ElevatorCarPhase.Idle)
            {
                seconds += car.Timing.UsesSeconds ? car.TimerSecondsRemaining : car.TimerTicksRemaining * 60L;
                if (car.Phase == ElevatorCarPhase.DoorsOpening) seconds += timing.DwellSeconds + timing.DoorCycleSeconds;
                else if (car.Phase == ElevatorCarPhase.OpenLoading) seconds += timing.DoorCycleSeconds;
            }
            foreach (var target in car.TargetFloors)
                if (target != floor && (turning ||
                    (Ahead(car.CurrentFloor, target, queueDirection) && !Ahead(floor, target, queueDirection))))
                    seconds += timing.DoorCycleSeconds * 2L + timing.DwellSeconds;
            return Math.Max(0, seconds) + timing.DoorCycleSeconds;
        }

        /// <summary>Full arrival estimate in the same minute clock as routines, including committed stops and queued seats.</summary>
        public int EstimateArrivalTicks(int originFloor, int destinationFloor)
        {
            var seconds = EstimateArrivalSeconds(originFloor, destinationFloor);
            return seconds >= (long)int.MaxValue * 60 ? int.MaxValue : (int)((seconds + 59) / 60);
        }

        /// <summary>Remaining arrival for a held queue position; only compatible FIFO predecessors consume its seats.</summary>
        public int EstimateArrivalTicks(int originFloor, int destinationFloor, EntityId queuedPersonId)
        {
            var seconds = EstimateArrivalSeconds(originFloor, destinationFloor, queuedPersonId);
            return seconds >= (long)int.MaxValue * 60 ? int.MaxValue : (int)((seconds + 59) / 60);
        }

        internal long EstimateArrivalSeconds(int originFloor, int destinationFloor, EntityId? queuedPersonId = null)
        {
            if (_cars.Count == 0 || originFloor < MinFloor || originFloor > MaxFloor || destinationFloor < MinFloor || destinationFloor > MaxFloor)
                return long.MaxValue;
            var direction = destinationFloor > originFloor ? ElevatorDirection.Up :
                destinationFloor < originFloor ? ElevatorDirection.Down : ElevatorDirection.None;
            var predecessors = DirectionalQueueCount(originFloor, direction);
            if (queuedPersonId.HasValue)
            {
                predecessors = 0;
                var found = false;
                foreach (var passenger in _floorQueues[originFloor])
                {
                    if (passenger.PersonId == queuedPersonId.Value && passenger.DestinationFloor == destinationFloor)
                    { found = true; break; }
                    if (PassengerDirection(passenger) == direction) predecessors++;
                }
                if (!found) return long.MaxValue;
            }
            long best = long.MaxValue;
            foreach (var car in _cars)
            {
                var pickup = EstimatePickupSeconds(car, originFloor, direction);
                var seats = car.Capacity;
                foreach (var passenger in car.Passengers)
                    if (Ahead(originFloor, passenger.DestinationFloor, direction)) seats--;
                var ahead = predecessors;
                foreach (var commitment in _pickupDirections)
                    if (commitment.Key.carId == car.Id.Value && commitment.Value == direction &&
                        Ahead(car.CurrentFloor, commitment.Key.floor, direction) && Ahead(commitment.Key.floor, originFloor, direction))
                        ahead += DirectionalQueueCount(commitment.Key.floor, direction);
                var timing = car.Timing;
                var batchCycle = Math.Max(1L, 2L * (MaxFloor - MinFloor) * timing.FloorTravelSeconds +
                    4L * timing.DoorCycleSeconds + 2L * timing.DwellSeconds);
                var batches = seats <= 0 ? 1L + ahead / Math.Max(1, car.Capacity) : ahead / Math.Max(1, seats);
                var loading = car.CurrentFloor == originFloor && car.Phase == ElevatorCarPhase.OpenLoading ?
                    (car.Timing.UsesSeconds ? car.TimerSecondsRemaining : car.TimerTicksRemaining * 60L) : timing.DwellSeconds;
                var ride = (long)Math.Abs(destinationFloor - originFloor) * timing.FloorTravelSeconds + loading + 2L * timing.DoorCycleSeconds;
                foreach (var target in car.TargetFloors)
                    if (Ahead(originFloor, target, direction) && Ahead(target, destinationFloor, direction))
                        ride += timing.DwellSeconds + 2L * timing.DoorCycleSeconds;
                best = Math.Min(best, pickup + batches * batchCycle + ride);
            }
            return best;
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
                    timingDataVersion = 1,
                    usesSeconds = car.Timing.UsesSeconds,
                    floorTravelSeconds = car.Timing.FloorTravelSeconds,
                    doorCycleSeconds = car.Timing.DoorCycleSeconds,
                    dwellSeconds = car.Timing.DwellSeconds,
                    timerSecondsRemaining = car.Timing.UsesSeconds ? car.TimerSecondsRemaining : checked(car.TimerTicksRemaining * 60),
                    phaseTimingDataVersion = 1,
                    phaseDurationSeconds = car.Phase == ElevatorCarPhase.Idle ? 0 : car.PhaseDurationSeconds,
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

            var pickups = new List<ElevatorPickupSaveData>();
            foreach (var pickup in _pickupDirections)
                pickups.Add(new ElevatorPickupSaveData { floor = pickup.Key.floor, carId = pickup.Key.carId, direction = (int)pickup.Value });
            pickups.Sort((a, b) => { var order = a.floor.CompareTo(b.floor); return order != 0 ? order : a.carId.CompareTo(b.carId); });
            return new ElevatorBankSaveData
            {
                timingDataVersion = 1,
                pickups = pickups.ToArray(),
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

        private static void ValidateCheckpoint(ElevatorBankSaveData data)
        {
            if (data == null) return;
            if (data.timingDataVersion < 0 || data.timingDataVersion > 1 || data.tier < 0 || data.tier > 5 ||
                (data.timingDataVersion >= 1 && data.tier == 0) || data.cumulativeDeliveredCount < 0 || data.cumulativeWaitTicks < 0)
                throw new ArgumentException("Invalid elevator bank temporal checkpoint.", nameof(data));
            ValidateFloorRange(data.minFloor, data.maxFloor);
            var carIds = new HashSet<int>();
            foreach (var car in data.cars ?? Array.Empty<ElevatorCarSaveData>())
            {
                var timerTicksRemaining = car == null ? 0 : RestoredMinuteTimer(data, car);
                if (car == null || car.id <= 0 || !carIds.Add(car.id) || car.capacity <= 0 ||
                    car.currentFloor < data.minFloor || car.currentFloor > data.maxFloor ||
                    car.timingDataVersion < 0 || car.timingDataVersion > 1 || car.phaseTimingDataVersion < 0 || car.phaseTimingDataVersion > 1 ||
                    !Enum.IsDefined(typeof(ElevatorCarPhase), car.phase) || !Enum.IsDefined(typeof(ElevatorDirection), car.direction) ||
                    timerTicksRemaining < 0 || car.timerSecondsRemaining < 0 || car.phaseDurationSeconds < 0)
                    throw new ArgumentException("Invalid elevator car temporal checkpoint.", nameof(data));
                var phase = (ElevatorCarPhase)car.phase;
                if (car.timingDataVersion >= 1)
                {
                    if (car.floorTravelSeconds <= 0 || car.doorCycleSeconds < 0 || car.dwellSeconds < 0 ||
                        (!car.usesSeconds && (car.floorTravelSeconds % 60 != 0 || car.doorCycleSeconds % 60 != 0 || car.dwellSeconds % 60 != 0)))
                        throw new ArgumentException("Invalid authored elevator timing units.", nameof(data));
                    if (car.usesSeconds && timerTicksRemaining != (car.timerSecondsRemaining + 59L) / 60 ||
                        (!car.usesSeconds && car.phaseTimingDataVersion >= 1 && car.timerSecondsRemaining != timerTicksRemaining * 60L))
                        throw new ArgumentException("Inconsistent elevator timer units.", nameof(data));
                }
                var seconds = car.timingDataVersion >= 1 && car.usesSeconds ? car.timerSecondsRemaining : timerTicksRemaining * 60L;
                if (phase == ElevatorCarPhase.Idle && seconds != 0 ||
                    phase == ElevatorCarPhase.Moving && ((ElevatorDirection)car.direction == ElevatorDirection.None || seconds <= 0 ||
                        ((ElevatorDirection)car.direction == ElevatorDirection.Up ? car.currentFloor >= data.maxFloor : car.currentFloor <= data.minFloor)))
                    throw new ArgumentException("Invalid elevator phase progress.", nameof(data));
                if (car.phaseTimingDataVersion >= 1 && (seconds > car.phaseDurationSeconds ||
                    (phase == ElevatorCarPhase.Idle && car.phaseDurationSeconds != 0) ||
                    (!car.usesSeconds && car.phaseDurationSeconds % 60 != 0)))
                    throw new ArgumentException("Elevator remainder exceeds its original phase duration.", nameof(data));
                // The original phase may predate a tier upgrade; never bound it by the new tier duration.
                foreach (var target in car.targetFloors ?? Array.Empty<int>())
                    if (target < data.minFloor || target > data.maxFloor) throw new ArgumentException("Invalid elevator target checkpoint.", nameof(data));
                if ((car.passengers?.Length ?? 0) > car.capacity) throw new ArgumentException("Invalid elevator capacity checkpoint.", nameof(data));
                foreach (var passenger in car.passengers ?? Array.Empty<ElevatorPassengerSaveData>()) ValidatePassengerCheckpoint(passenger, data);
            }
            foreach (var passenger in data.queuedPassengers ?? Array.Empty<ElevatorPassengerSaveData>()) ValidatePassengerCheckpoint(passenger, data);
            foreach (var passenger in data.deliveredPassengers ?? Array.Empty<ElevatorPassengerSaveData>()) ValidatePassengerCheckpoint(passenger, data);
            var keys = new HashSet<(int floor, int carId)>();
            foreach (var pickup in data.pickups ?? Array.Empty<ElevatorPickupSaveData>())
                if (pickup == null || !carIds.Contains(pickup.carId) || pickup.floor < data.minFloor || pickup.floor > data.maxFloor ||
                    !Enum.IsDefined(typeof(ElevatorDirection), pickup.direction) || !keys.Add((pickup.floor, pickup.carId)))
                    throw new ArgumentException("Invalid elevator pickup checkpoint.", nameof(data));
        }

        private static int RestoredMinuteTimer(ElevatorBankSaveData bank, ElevatorCarSaveData car)
        {
            // The previous minute-clock writer decremented a zero closing timer to -1 on becoming idle.
            // New phase-provenance checkpoints and all other negative progress retain strict rejection.
            if (bank.timingDataVersion == 1 && car.timingDataVersion == 1 && car.phaseTimingDataVersion == 0 &&
                !car.usesSeconds && car.phase == (int)ElevatorCarPhase.Idle && car.direction == (int)ElevatorDirection.None &&
                car.timerTicksRemaining == -1 && car.timerSecondsRemaining == 0 && car.phaseDurationSeconds == 0 &&
                car.floorTravelSeconds > 0 && car.floorTravelSeconds % 60 == 0 && car.doorCycleSeconds == 0 &&
                car.dwellSeconds >= 0 && car.dwellSeconds % 60 == 0 &&
                (car.passengers?.Length ?? 0) == 0 && (car.targetFloors?.Length ?? 0) == 0)
                return 0;
            return car.timerTicksRemaining;
        }

        private static void ValidatePassengerCheckpoint(ElevatorPassengerSaveData passenger, ElevatorBankSaveData bank)
        {
            if (passenger == null || passenger.personId <= 0 || passenger.originFloor < bank.minFloor || passenger.originFloor > bank.maxFloor ||
                passenger.destinationFloor < bank.minFloor || passenger.destinationFloor > bank.maxFloor || passenger.waitTicks < 0 || passenger.rideTicks < 0)
                throw new ArgumentException("Invalid elevator passenger temporal checkpoint.", nameof(bank));
        }

        public static ElevatorBank FromSaveData(ElevatorBankSaveData data)
        {
            ValidateCheckpoint(data);
            var cars = new List<ElevatorCar>();
            if (data?.cars != null)
            {
                foreach (var c in data.cars)
                {
                    if (c.timingDataVersion < 0 || c.timingDataVersion > 1) throw new ArgumentException("Unsupported elevator timing checkpoint.");
                    var legacyTravelTicks = (data.tier >= 5 ? 3 : data.tier >= 3 ? 4 : 5);
                    var timing = c.timingDataVersion == 0 ? new ElevatorTimingConfig(legacyTravelTicks, 2, 2) :
                        c.usesSeconds ? ElevatorTimingConfig.FromSeconds(c.floorTravelSeconds, c.doorCycleSeconds, c.dwellSeconds) :
                        new ElevatorTimingConfig(c.floorTravelSeconds / 60, c.doorCycleSeconds / 60, c.dwellSeconds / 60);
                    var car = new ElevatorCar(new EntityId(c.id), c.currentFloor, c.capacity, timing);
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

                    car.RestoreState(c.currentFloor, (ElevatorCarPhase)c.phase, (ElevatorDirection)c.direction, RestoredMinuteTimer(data, c), riders);
                    if (c.timingDataVersion >= 1) car.RestoreSecondsRemaining(c.timerSecondsRemaining, c.phaseTimingDataVersion >= 1 ? c.phaseDurationSeconds : (int?)null);
                    if (c.targetFloors != null) car.RestoreTargets(c.targetFloors);
                    cars.Add(car);
                }
            }

            var minFloor = data?.minFloor ?? 0;
            var maxFloor = data?.maxFloor ?? 4;
            var elevatorBank = new ElevatorBank(minFloor, maxFloor, cars);
            elevatorBank._tierProfile = ElevatorTierProfile.ForTier(data != null && data.tier >= 1 && data.tier <= 5 ? data.tier : 1);
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
                        (ElevatorDirection)saved.direction, RestoredMinuteTimer(data, saved), riders);
                    car.RestoreTargets(saved.targetFloors);
                    if (saved.timingDataVersion >= 1) car.RestoreSecondsRemaining(saved.timerSecondsRemaining, saved.phaseTimingDataVersion >= 1 ? saved.phaseDurationSeconds : (int?)null);
                }
            if (data?.timingDataVersion >= 1)
            {
                elevatorBank._pickupDirections.Clear();
                foreach (var pickup in data.pickups ?? Array.Empty<ElevatorPickupSaveData>())
                    elevatorBank._pickupDirections[(pickup.floor, pickup.carId)] = (ElevatorDirection)pickup.direction;
            }
            return elevatorBank;
        }
    }
}
