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

        public int Capacity { get; }

        public ElevatorTimingConfig Timing { get; }

        public ElevatorCarPhase Phase { get; private set; }

        public ElevatorDirection Direction { get; private set; }

        public int TimerTicksRemaining { get; private set; }

        public IReadOnlyList<ElevatorPassenger> Passengers { get; }

        public bool CanBoard => Phase == ElevatorCarPhase.OpenLoading && _passengers.Count < Capacity;

        public bool HasRequests => _targetFloors.Count > 0;

        public void RequestFloor(int floor)
        {
            if (floor == CurrentFloor && Phase == ElevatorCarPhase.Idle)
            {
                Phase = ElevatorCarPhase.DoorsOpening;
                TimerTicksRemaining = Timing.DoorCycleTicks;
                return;
            }

            _targetFloors.Add(floor);
        }

        public bool Board(ElevatorPassenger passenger)
        {
            if (!CanBoard || passenger == null)
            {
                return false;
            }

            _passengers.Add(passenger);
            RequestFloor(passenger.DestinationFloor);
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

        public void Tick()
        {
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

        private bool HasTargetsInDirection(ElevatorDirection dir)
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
