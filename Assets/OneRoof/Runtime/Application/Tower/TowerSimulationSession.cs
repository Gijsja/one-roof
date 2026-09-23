using System;
using System.Collections.Generic;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Transit;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Tower
{
    /// <summary>
    /// Application service boundary providing projections and actions for the unified TowerSimulation.
    /// Exposes read-only projections for presentation overlays, mode shell, and inspectors.
    /// </summary>
    public sealed class TowerSimulationSession
    {
        private TowerSimulation _simulation;
        private TowerProjection _cachedTransitProjection;
        private ElevatorBankCongestionProjection _cachedCongestionProjection;
        private TowerTopologyProjection _cachedTopologyProjection;
        private long _topologyVersion;
        private long _cachedTopologyVersion = -1;
        private long _cachedTick = -1;
        private long _cachedCongestionTick = -1;
        // Version proving projections fresh: only this module mutates the
        // simulation, so (tick, version) fully keys the caches.
        private long _version;
        private long _cachedVersion = -1;
        private long _cachedCongestionVersion = -1;

        public TowerSimulationSession() : this(TowerSimulation.CreateStandardFiveFloor())
        {
        }

        private TowerSimulationSession(TowerSimulation simulation)
        {
            _simulation = simulation;
        }

        /// <summary>Five-floor fixture with a configurable treasury for build validation.</summary>
        public static TowerSimulationSession CreateStandardFiveFloor(long startingTreasury)
        {
            return new TowerSimulationSession(TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(startingTreasury)));
        }

        /// <summary>Ground-floor from-scratch start: one slab, lobby shell, no residents.</summary>
        public static TowerSimulationSession CreateGroundFloorStart(long startingTreasury = TowerEconomyState.DefaultStartingTreasury)
        {
            return new TowerSimulationSession(TowerSimulation.CreateGroundFloorStart(startingTreasury));
        }

        public long CurrentTick => _simulation.CurrentTick;

        /// <summary>Pure calendar view over the tick clock for the day/night presentation clock.</summary>
        public DayPhase DayPhase => _simulation.DayPhase;

        public int ResidentCount => _simulation.ResidentCount;

        public int FloorCount => _simulation.Topology.FloorCount;

        public TowerTopologyProjection TopologyProjection()
        {
            if (_cachedTopologyProjection != null && _cachedTopologyVersion == _topologyVersion)
                return _cachedTopologyProjection;
            _cachedTopologyProjection = new TowerTopologyProjection(_simulation.Topology);
            _cachedTopologyVersion = _topologyVersion;
            return _cachedTopologyProjection;
        }

        public int ActiveTripCount => _simulation.ActiveTripCount;

        public int ElevatorCarCount => _simulation.ElevatorBank.Cars.Count;

        public int DeliveredPassengerCount => _simulation.ElevatorBank.DeliveredCount;

        public long TreasuryBalance => _simulation.Economy.CashBalance;

        public long TotalRevenue => _simulation.Economy.TotalRevenue;

        internal ElevatorBankSnapshot ElevatorSnapshot() => _simulation.ElevatorBank.Snapshot();

        public int ElevatorMinFloor => _simulation.ElevatorBank.MinFloor;

        public int ElevatorMaxFloor => _simulation.ElevatorBank.MaxFloor;

        public int RoomCount => _simulation.Topology.Rooms.Count;

        public IReadOnlyList<Room> GetRoomsOnFloor(int floor) => TopologyProjection().GetRoomsOnFloor(floor);

        public bool TryGetRoom(EntityId id, out Room room) => TopologyProjection().TryGetRoom(id, out room);

        public bool TryGetFloorSlab(int floor, out CellBounds slab) => TopologyProjection().TryGetFloorSlab(floor, out slab);

        internal TowerSimulation Simulation => _simulation;

        internal BuildingTopologyState Topology => _simulation.Topology;

        internal PopulationState Population => _simulation.Population;

        internal ElevatorBank ElevatorBank => _simulation.ElevatorBank;

        internal OneRoof.Domain.Scrutiny.ScrutinyState Scrutiny => _simulation.Scrutiny;

        internal IReadOnlyList<BusinessRecord> Businesses => _simulation.Businesses.Businesses;

        public bool TryGetResidentInspection(EntityId id, out ResidentInspectionSnapshot snapshot)
        {
            if (_simulation.Population.TryGetPerson(id, out var person))
            {
                snapshot = new ResidentInspectionSnapshot(person);
                return true;
            }
            snapshot = null;
            return false;
        }

        public int CountRoomOccupants(EntityId roomId)
        {
            var count = 0;
            foreach (var person in _simulation.Population.Persons)
                if (!person.CurrentLocation.IsOutside && person.CurrentRoomId.Equals(roomId)) count++;
            return count;
        }

        internal IReadOnlyList<PersonRecord> Persons => _simulation.Population.Persons;

        internal HouseholdRecord GetHousehold(EntityId id) => _simulation.Population.GetHousehold(id);

        /// <summary>Application-facing immutable power network projection for future utility UI and inspectors.</summary>
        public ElectricalGridSnapshot ElectricalGridProjection() => _simulation.ElectricalGridSnapshot();

        /// <summary>Application-facing immutable water and waste network projection for utility UI and inspectors.</summary>
        public WaterWasteNetworkSnapshot WaterWasteNetworkProjection() => _simulation.WaterWasteNetworkSnapshot();

        /// <summary>Application-facing immutable operational condition projection for utility equipment.</summary>
        public UtilityOperationsSnapshot UtilityOperationsProjection() => _simulation.UtilityOperationsSnapshot();

        public void AdvanceOneTick()
        {
            _simulation.AdvanceOneTick();
            BumpVersion();
        }

        public CommandResult AddCapacity()
        {
            var result = _simulation.AddElevatorCar();
            if (result.Accepted)
            {
                BumpVersion();
            }
            return result;
        }

        public CommandResult CanExecute(ICommand command) => _simulation.CanExecute(command);

        public CommandResult ExecuteCommand(ICommand command)
        {
            var result = _simulation.ExecuteCommand(command);
            if (result.Accepted)
            {
                BumpVersion(topologyChanged: true);
            }
            return result;
        }

        public void Reset()
        {
            _simulation = TowerSimulation.CreateStandardFiveFloor();
            BumpVersion(topologyChanged: true);
        }

        /// <summary>Resets to the ground-floor from-scratch start instead of the five-floor fixture.</summary>
        public void ResetToGroundFloorStart(long startingTreasury = TowerEconomyState.DefaultStartingTreasury)
        {
            _simulation = TowerSimulation.CreateGroundFloorStart(startingTreasury);
            BumpVersion(topologyChanged: true);
        }

        /// <summary>
        /// Seeds the elevator bank with morning-rush passengers for the standard five-floor scenario.
        /// Only seeds if the bank is empty so it is idempotent on reset or reload.
        /// </summary>
        public void SeedMorningRush()
        {
            _simulation.SeedMorningRush();
            BumpVersion();
        }


        public ElevatorBankCongestionProjection CongestionProjection()
        {
            if (_cachedCongestionProjection != null &&
                _cachedCongestionTick == _simulation.CurrentTick &&
                _cachedCongestionVersion == _version)
            {
                return _cachedCongestionProjection;
            }

            var floorCount = _simulation.Topology.FloorCount;
            var floorProjections = new List<FloorCongestionProjection>(floorCount);
            var maxQueue = -1;
            var bottleneckFloor = 0;
            var totalQueued = 0;

            for (var floor = 0; floor < floorCount; floor++)
            {
                var (maxWait, avgWait) = _simulation.GetFloorWaitMetrics(floor);
                var queue = _simulation.GetQueueLength(floor);
                totalQueued += queue;
                if (queue > maxQueue)
                {
                    maxQueue = queue;
                    bottleneckFloor = floor;
                }
                var severity = CongestionEvaluator.Evaluate(queue, maxWait);
                floorProjections.Add(new FloorCongestionProjection(
                    floor,
                    queue,
                    maxWait,
                    avgWait,
                    severity));
            }

            var elevProjections = new List<ElevatorProjection>(_simulation.ElevatorBank.Cars.Count);
            var inTransit = 0;
            foreach (var car in _simulation.ElevatorBank.Cars)
            {
                inTransit += car.Passengers.Count;
                var passengerIds = new List<int>(car.Passengers.Count);
                for (var i = 0; i < car.Passengers.Count; i++)
                {
                    passengerIds.Add(car.Passengers[i].PersonId.Value);
                }

                elevProjections.Add(new ElevatorProjection(
                    car.Id.Value,
                    car.CurrentFloor,
                    car.Passengers.Count,
                    car.Capacity,
                    passengerIds));
            }

            var (bMaxWait, _) = _simulation.GetFloorWaitMetrics(bottleneckFloor);
            var overallSeverity = CongestionEvaluator.Evaluate(maxQueue > 0 ? maxQueue : 0, bMaxWait);

            _cachedCongestionProjection = new ElevatorBankCongestionProjection(
                _simulation.CurrentTick,
                totalQueued,
                inTransit,
                _simulation.ResidentCount - _simulation.ActiveTripCount,
                _simulation.AverageElevatorWaitTicks,
                bottleneckFloor,
                overallSeverity,
                floorProjections,
                elevProjections);
            _cachedCongestionTick = _simulation.CurrentTick;
            _cachedCongestionVersion = _version;
            return _cachedCongestionProjection;
        }

        public TowerProjection Projection() => TransitProjection();

        public TowerProjection TransitProjection()
        {
            if (_cachedTransitProjection != null &&
                _cachedTick == _simulation.CurrentTick &&
                _cachedVersion == _version)
            {
                return _cachedTransitProjection;
            }

            // Index elevator bank passenger states for rush hour and transit visualization
            var snapshot = _simulation.ElevatorBank.Snapshot();

            var ridingByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var car in snapshot.Cars)
            {
                foreach (var p in car.Passengers)
                {
                    ridingByPerson[p.PersonId] = p;
                }
            }

            var queuedByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var p in snapshot.QueuedPassengers)
            {
                queuedByPerson[p.PersonId] = p;
            }

            var roomOccupantCounts = new Dictionary<EntityId, int>();
            var residents = new List<TransitResidentProjection>(_simulation.ResidentCount);
            var arrivedCount = 0;

            foreach (var person in _simulation.Population.Persons)
            {
                var spatial = _simulation.GetResidentPosition(person.Id);
                TransitResidentStatus status;
                var floor = spatial.Floor;
                var cellX = spatial.X;
                var roomId = spatial.RoomId;
                var activity = spatial.Activity;
                int targetFloor = floor;
                int slotInRoom = 0;
                int waitTicks = 0;

                switch (spatial.Phase)
                {
                    case ResidentMovementPhase.Outside:
                        status = TransitResidentStatus.Outside;
                        break;
                    case ResidentMovementPhase.InRoom:
                        status = TransitResidentStatus.InRoom;
                        arrivedCount++;
                        if (roomId.HasValue)
                        {
                            if (!roomOccupantCounts.TryGetValue(roomId.Value, out var count))
                            {
                                count = 0;
                            }
                            slotInRoom = count;
                            roomOccupantCounts[roomId.Value] = count + 1;
                        }
                        break;

                    case ResidentMovementPhase.Walking:
                        status = TransitResidentStatus.Walking;
                        break;

                    case ResidentMovementPhase.Queued:
                        status = TransitResidentStatus.Queued;
                        if (queuedByPerson.TryGetValue(person.Id, out var qp))
                        {
                            targetFloor = qp.DestinationFloor;
                            waitTicks = (int)qp.WaitTicks;
                        }
                        break;

                    case ResidentMovementPhase.Riding:
                        status = TransitResidentStatus.Riding;
                        if (ridingByPerson.TryGetValue(person.Id, out var rp))
                        {
                            targetFloor = rp.DestinationFloor;
                            waitTicks = (int)rp.WaitTicks;
                        }
                        break;

                    default:
                        status = TransitResidentStatus.InRoom;
                        break;
                }

                residents.Add(new TransitResidentProjection(
                    person.Id.Value,
                    targetFloor,
                    status,
                    floor,
                    cellX,
                    roomId?.Value,
                    activity,
                    slotInRoom,
                    waitTicks));
            }

            var elevators = new List<ElevatorProjection>(_simulation.ElevatorBank.Cars.Count);
            foreach (var car in _simulation.ElevatorBank.Cars)
            {
                var passengerIds = new List<int>(car.Passengers.Count);
                for (var i = 0; i < car.Passengers.Count; i++)
                {
                    passengerIds.Add(car.Passengers[i].PersonId.Value);
                }

                elevators.Add(new ElevatorProjection(
                    car.Id.Value,
                    car.CurrentFloor,
                    car.Passengers.Count,
                    car.Capacity,
                    passengerIds));
            }

            _cachedTransitProjection = new TowerProjection(
                _simulation.CurrentTick,
                _simulation.TotalQueuedElevatorPassengers,
                arrivedCount,
                _simulation.AverageElevatorWaitTicks,
                residents,
                elevators);

            _cachedTick = _simulation.CurrentTick;
            _cachedVersion = _version;
            return _cachedTransitProjection;
        }

        private void BumpVersion(bool topologyChanged = false)
        {
            _version++;
            if (topologyChanged) _topologyVersion++;
            InvalidateProjectionCaches();
        }

        private void InvalidateProjectionCaches()
        {
            _cachedTransitProjection = null;
            _cachedCongestionProjection = null;
            _cachedTick = -1;
            _cachedCongestionTick = -1;
            _cachedVersion = -1;
            _cachedCongestionVersion = -1;
        }
    }
}
