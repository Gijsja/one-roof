using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Transit
{
    public sealed class ElevatorCar
    {
        private readonly List<ElevatorPassenger> _passengers;
        private readonly SortedSet<int> _targetFloors;

        public ElevatorCar(EntityId id, int startingFloor, int capacity, ElevatorTimingConfig timing = default)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Elevator car capacity must be positive.");
            }

            Id = id;
            CurrentFloor = startingFloor;
            Capacity = capacity;
            Timing = timing.FloorTravelTicks > 0 ? timing : ElevatorTimingConfig.Default;

            Phase = ElevatorCarPhase.Idle;
            Direction = ElevatorDirection.None;
            _passengers = new List<ElevatorPassenger>(capacity);
            _targetFloors = new SortedSet<int>();
            Passengers = new ReadOnlyCollection<ElevatorPassenger>(_passengers);
        }

        public EntityId Id { get; }

        public int CurrentFloor { get; private set; }

        public int Capacity { get; private set; }

        public ElevatorTimingConfig Timing { get; private set; }

        public ElevatorCarPhase Phase { get; private set; }

        public ElevatorDirection Direction { get; private set; }

        private int _legacyTimerTicksRemaining;
        public int TimerTicksRemaining
        {
            get => Timing.UsesSeconds ? (TimerSecondsRemaining + 59) / 60 : _legacyTimerTicksRemaining;
            private set { _legacyTimerTicksRemaining = value; }
        }
        public int TimerSecondsRemaining { get; private set; }
        private int _phaseDurationSeconds;
        public int PhaseDurationSeconds => Timing.UsesSeconds ? _phaseDurationSeconds : ConfiguredPhaseDurationSeconds(Timing, Phase);

        internal static int ConfiguredPhaseDurationSeconds(ElevatorTimingConfig timing, ElevatorCarPhase phase) =>
            phase == ElevatorCarPhase.Moving ? timing.FloorTravelSeconds :
            phase == ElevatorCarPhase.OpenLoading ? timing.DwellSeconds :
            phase == ElevatorCarPhase.DoorsOpening || phase == ElevatorCarPhase.DoorsClosing ? timing.DoorCycleSeconds : 0;

        private void StartSecondTimer(int seconds)
        {
            TimerSecondsRemaining = seconds;
            _phaseDurationSeconds = seconds;
        }

        public IReadOnlyList<ElevatorPassenger> Passengers { get; }

        public bool CanBoard => Phase == ElevatorCarPhase.OpenLoading && _passengers.Count < Capacity;

        public bool HasRequests => _targetFloors.Count > 0;

        public int RequestCount => _targetFloors.Count;

        public IReadOnlyCollection<int> TargetFloors => _targetFloors;

        public bool HasTargetFloor(int floor) => _targetFloors.Contains(floor);

        internal void ApplyTier(ElevatorTierProfile profile)
        {
            if (!Timing.UsesSeconds)
            {
                _phaseDurationSeconds = PhaseDurationSeconds;
                TimerSecondsRemaining = checked(_legacyTimerTicksRemaining * 60);
            }
            Timing = profile.Timing;
            // Upgrades never shrink a custom or previously upgraded car, or strand riders.
            Capacity = Math.Max(Math.Max(_passengers.Count, Capacity), profile.CarCapacity);
        }

        public void RequestFloor(int floor)
        {
            if (floor == CurrentFloor)
            {
                if (Phase == ElevatorCarPhase.Idle)
                {
                    Phase = ElevatorCarPhase.DoorsOpening;
                    TimerTicksRemaining = Timing.DoorCycleTicks;
                    StartSecondTimer(Timing.DoorCycleSeconds);
                }
                return;
            }

            _targetFloors.Add(floor);
        }

        public void RemoveTargetFloor(int floor)
        {
            for (var i = 0; i < _passengers.Count; i++)
            {
                if (_passengers[i].DestinationFloor == floor)
                {
                    return;
                }
            }

            _targetFloors.Remove(floor);
        }

        public void ClearPickupTargets()
        {
            var destinationFloors = new HashSet<int>();
            for (var i = 0; i < _passengers.Count; i++)
            {
                destinationFloors.Add(_passengers[i].DestinationFloor);
            }

            var floorsToRemove = new List<int>();
            foreach (var floor in _targetFloors)
            {
                if (!destinationFloors.Contains(floor))
                {
                    floorsToRemove.Add(floor);
                }
            }

            for (var i = 0; i < floorsToRemove.Count; i++)
            {
                _targetFloors.Remove(floorsToRemove[i]);
            }
        }

        public bool Board(ElevatorPassenger passenger)
        {
            if (!CanBoard || passenger == null)
            {
                return false;
            }

            _passengers.Add(passenger);
            RequestFloor(passenger.DestinationFloor);
            if (Direction == ElevatorDirection.None)
            {
                Direction = passenger.DestinationFloor > CurrentFloor ? ElevatorDirection.Up
                    : passenger.DestinationFloor < CurrentFloor ? ElevatorDirection.Down
                    : ElevatorDirection.None;
            }
            return true;
        }

        public List<ElevatorPassenger> DischargeArrivedPassengers()
        {
            var discharged = new List<ElevatorPassenger>();
            for (var i = _passengers.Count - 1; i >= 0; i--)
            {
                if (_passengers[i].DestinationFloor == CurrentFloor)
                {
                    discharged.Add(_passengers[i]);
                    _passengers.RemoveAt(i);
                }
            }

            if (_passengers.Count == 0 && !HasTargetsInDirection(Direction))
            {
                var opposite = Direction == ElevatorDirection.Up ? ElevatorDirection.Down : ElevatorDirection.Up;
                Direction = HasTargetsInDirection(opposite) ? opposite : ElevatorDirection.None;
            }

            return discharged;
        }

        public void RestoreState(
            int floor,
            ElevatorCarPhase phase,
            ElevatorDirection direction,
            int timerRemaining,
            IEnumerable<ElevatorPassenger> passengers)
        {
            CurrentFloor = floor;
            Phase = phase;
            Direction = direction;
            TimerTicksRemaining = timerRemaining;
            _passengers.Clear();
            if (passengers != null)
            {
                _passengers.AddRange(passengers);
                foreach (var p in passengers)
                {
                    _targetFloors.Add(p.DestinationFloor);
                }
            }
        }

        internal void RestoreTargets(IEnumerable<int> floors)
        {
            _targetFloors.Clear();
            foreach (var floor in floors) _targetFloors.Add(floor);
        }

        public void Tick()
        {
            if (Timing.UsesSeconds)
            {
                AdvanceSeconds(60, null);
                return;
            }
            switch (Phase)
            {
                case ElevatorCarPhase.Idle:
                    UpdateIdle();
                    break;

                case ElevatorCarPhase.Moving:
                    UpdateMoving();
                    break;

                case ElevatorCarPhase.DoorsOpening:
                    UpdateDoorsOpening();
                    break;

                case ElevatorCarPhase.OpenLoading:
                    UpdateOpenLoading();
                    break;

                case ElevatorCarPhase.DoorsClosing:
                    UpdateDoorsClosing();
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (_targetFloors.Count == 0)
            {
                Direction = ElevatorDirection.None;
                return;
            }

            // Find best direction
            var nextTarget = ChooseNextTarget();
            if (nextTarget == CurrentFloor)
            {
                _targetFloors.Remove(CurrentFloor);
                Phase = ElevatorCarPhase.DoorsOpening;
                TimerTicksRemaining = Timing.DoorCycleTicks;
            }
            else
            {
                Direction = nextTarget > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
                Phase = ElevatorCarPhase.Moving;
                TimerTicksRemaining = Timing.FloorTravelTicks;
                UpdateMoving();
            }
        }

        private void UpdateMoving()
        {
            TimerTicksRemaining--;
            if (TimerTicksRemaining > 0)
            {
                return;
            }

            CurrentFloor += Direction == ElevatorDirection.Up ? 1 : -1;

            if (_targetFloors.Contains(CurrentFloor))
            {
                _targetFloors.Remove(CurrentFloor);
                Phase = ElevatorCarPhase.DoorsOpening;
                TimerTicksRemaining = Timing.DoorCycleTicks;
                if (!HasTargetsInDirection(Direction))
                {
                    var opposite = Direction == ElevatorDirection.Up ? ElevatorDirection.Down : ElevatorDirection.Up;
                    Direction = HasTargetsInDirection(opposite) ? opposite : ElevatorDirection.None;
                }
            }
            else
            {
                // Continue moving if there are targets ahead
                if (HasTargetsInDirection(Direction))
                {
                    TimerTicksRemaining = Timing.FloorTravelTicks;
                }
                else if (_targetFloors.Count > 0)
                {
                    // Reverse direction
                    Direction = Direction == ElevatorDirection.Up ? ElevatorDirection.Down : ElevatorDirection.Up;
                    TimerTicksRemaining = Timing.FloorTravelTicks;
                }
                else
                {
                    Phase = ElevatorCarPhase.Idle;
                    Direction = ElevatorDirection.None;
                    TimerTicksRemaining = 0;
                }
            }
        }

        private void UpdateDoorsOpening()
        {
            TimerTicksRemaining--;
            if (TimerTicksRemaining <= 0)
            {
                Phase = ElevatorCarPhase.OpenLoading;
                TimerTicksRemaining = Timing.DwellTicks;
            }
        }

        private void UpdateOpenLoading()
        {
            TimerTicksRemaining--;
            if (TimerTicksRemaining <= 0)
            {
                Phase = ElevatorCarPhase.DoorsClosing;
                TimerTicksRemaining = Timing.DoorCycleTicks;
            }
        }

        private void UpdateDoorsClosing()
        {
            TimerTicksRemaining--;
            if (TimerTicksRemaining > 0)
            {
                return;
            }

            if (HasTargetsInDirection(Direction))
            {
                Phase = ElevatorCarPhase.Moving;
                TimerTicksRemaining = Timing.FloorTravelTicks;
            }
            else if (_targetFloors.Count > 0)
            {
                var target = ChooseNextTarget();
                if (target != CurrentFloor)
                {
                    Direction = target > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
                    Phase = ElevatorCarPhase.Moving;
                    TimerTicksRemaining = Timing.FloorTravelTicks;
                }
                else
                {
                    _targetFloors.Remove(CurrentFloor);
                    Phase = ElevatorCarPhase.DoorsOpening;
                    TimerTicksRemaining = Timing.DoorCycleTicks;
                }
            }
            else
            {
                Phase = ElevatorCarPhase.Idle;
                Direction = ElevatorDirection.None;
                TimerTicksRemaining = 0;
            }
        }

        private int ChooseNextTarget()
        {
            if (Direction == ElevatorDirection.Up)
            {
                foreach (var floor in _targetFloors)
                {
                    if (floor >= CurrentFloor) return floor;
                }
            }
            else if (Direction == ElevatorDirection.Down)
            {
                var floorList = new List<int>(_targetFloors);
                floorList.Reverse();
                foreach (var floor in floorList)
                {
                    if (floor <= CurrentFloor) return floor;
                }
            }

            // Fallback to nearest
            var nearest = -1;
            var minDelta = int.MaxValue;
            foreach (var floor in _targetFloors)
            {
                var delta = Math.Abs(floor - CurrentFloor);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    nearest = floor;
                }
            }

            return nearest;
        }

        internal void RestoreSecondsRemaining(int seconds, int? phaseDuration = null)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            TimerSecondsRemaining = seconds;
            _phaseDurationSeconds = phaseDuration ?? Math.Max(seconds, ConfiguredPhaseDurationSeconds(Timing, Phase));
        }

        internal void SetServiceDirection(ElevatorDirection direction)
        {
            if (_passengers.Count == 0) Direction = direction;
        }

        /// <summary>Consume an integer-second budget, retaining partial floor/phase progress exactly.</summary>
        internal void AdvanceSeconds(int seconds, Action<ElevatorCar> onOpen)
        {
            if (!Timing.UsesSeconds) throw new InvalidOperationException("Authored minute timing uses Tick().");
            var transitions = 0;
            while (seconds > 0 && transitions++ < 32)
            {
                if (Phase == ElevatorCarPhase.Idle)
                {
                    if (_targetFloors.Count == 0) { Direction = ElevatorDirection.None; return; }
                    var target = ChooseNextTarget();
                    if (target == CurrentFloor)
                    {
                        _targetFloors.Remove(target);
                        Phase = ElevatorCarPhase.DoorsOpening;
                        StartSecondTimer(Timing.DoorCycleSeconds);
                    }
                    else
                    {
                        Direction = target > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
                        Phase = ElevatorCarPhase.Moving;
                        StartSecondTimer(Timing.FloorTravelSeconds);
                    }
                }
                if (Phase == ElevatorCarPhase.OpenLoading) onOpen?.Invoke(this);
                var consumed = Math.Min(seconds, TimerSecondsRemaining);
                TimerSecondsRemaining -= consumed;
                seconds -= consumed;
                if (TimerSecondsRemaining > 0) return;
                switch (Phase)
                {
                    case ElevatorCarPhase.Moving:
                        CurrentFloor += Direction == ElevatorDirection.Up ? 1 : -1;
                        if (_targetFloors.Remove(CurrentFloor))
                        {
                            Phase = ElevatorCarPhase.DoorsOpening;
                            StartSecondTimer(Timing.DoorCycleSeconds);
                        }
                        else if (HasTargetsInDirection(Direction)) StartSecondTimer(Timing.FloorTravelSeconds);
                        else if (_targetFloors.Count > 0)
                        {
                            Direction = Direction == ElevatorDirection.Up ? ElevatorDirection.Down : ElevatorDirection.Up;
                            StartSecondTimer(Timing.FloorTravelSeconds);
                        }
                        else { Phase = ElevatorCarPhase.Idle; Direction = ElevatorDirection.None; }
                        break;
                    case ElevatorCarPhase.DoorsOpening:
                        Phase = ElevatorCarPhase.OpenLoading;
                        StartSecondTimer(Timing.DwellSeconds);
                        onOpen?.Invoke(this);
                        break;
                    case ElevatorCarPhase.OpenLoading:
                        Phase = ElevatorCarPhase.DoorsClosing;
                        StartSecondTimer(Timing.DoorCycleSeconds);
                        break;
                    case ElevatorCarPhase.DoorsClosing:
                        Phase = ElevatorCarPhase.Idle;
                        break;
                }
            }
        }

        public bool HasTargetsInDirection(ElevatorDirection dir)
        {
            if (dir == ElevatorDirection.Up)
            {
                foreach (var floor in _targetFloors)
                {
                    if (floor > CurrentFloor) return true;
                }
            }
            else if (dir == ElevatorDirection.Down)
            {
                foreach (var floor in _targetFloors)
                {
                    if (floor < CurrentFloor) return true;
                }
            }

            return false;
        }
    }
}
