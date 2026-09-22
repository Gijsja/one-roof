using System;
using System.Collections.Generic;
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
        private long _cachedTick = -1;
        private long _cachedCongestionTick = -1;
        // Cheap structural fingerprints: direct Simulation mutations bypass the
        // wrappers below, so tick alone cannot prove a projection is fresh.
        private int _cachedFloorCount = -1;
        private int _cachedResidentCount = -1;
        private int _cachedQueuedTotal = -1;
        private int _cachedCarCount = -1;

        public TowerSimulationSession(TowerSimulation simulation = null)
        {
            _simulation = simulation ?? TowerSimulation.CreateStandardFiveFloor();
        }

        public TowerSimulation Simulation => _simulation;

        public BuildingTopologyState Topology => _simulation.Topology;

        public PopulationState Population => _simulation.Population;

        public ElevatorBank ElevatorBank => _simulation.ElevatorBank;

        public TowerEconomyState Economy => _simulation.Economy;

        public OneRoof.Domain.Scrutiny.ScrutinyState Scrutiny => _simulation.Scrutiny;

        public long CurrentTick => _simulation.CurrentTick;

        /// <summary>Pure calendar view over the tick clock for the day/night presentation clock.</summary>
        public DayPhase DayPhase => _simulation.DayPhase;

        public int ResidentCount => _simulation.ResidentCount;

        public int FloorCount => _simulation.Topology.FloorCount;

        /// <summary>Application-facing immutable power network projection for future utility UI and inspectors.</summary>
        public ElectricalGridSnapshot ElectricalGridProjection() => _simulation.ElectricalGridSnapshot();

        /// <summary>Application-facing immutable water and waste network projection for utility UI and inspectors.</summary>
        public WaterWasteNetworkSnapshot WaterWasteNetworkProjection() => _simulation.WaterWasteNetworkSnapshot();

        /// <summary>Application-facing immutable operational condition projection for utility equipment.</summary>
        public UtilityOperationsSnapshot UtilityOperationsProjection() => _simulation.UtilityOperationsSnapshot();

        public void AdvanceOneTick()
        {
            _simulation.AdvanceOneTick();
            InvalidateProjectionCaches();
        }

        public CommandResult AddCapacity()
        {
            var result = _simulation.AddElevatorCar();
            if (result.Accepted)
            {
                InvalidateProjectionCaches();
            }
            return result;
        }

        public CommandResult CanExecute(ICommand command) => _simulation.CanExecute(command);

        public CommandResult ExecuteCommand(ICommand command)
        {
            var result = _simulation.ExecuteCommand(command);
            if (result.Accepted)
            {
                InvalidateProjectionCaches();
            }
            return result;
        }

        public CommandResult BuildFloorSlab(BuildFloorSlabCommand cmd)
        {
            var result = _simulation.BuildFloorSlab(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public CommandResult ExpandGroundSlab(ExpandGroundSlabCommand cmd)
        {
            var result = _simulation.ExpandGroundSlab(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public CommandResult BuildRoom(BuildRoomCommand cmd)
        {
            var result = _simulation.BuildRoom(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public CommandResult AddElevatorShaft(AddElevatorShaftCommand cmd)
        {
            var result = _simulation.AddElevatorShaft(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public CommandResult BuildStairwell(BuildStairwellCommand cmd)
        {
            var result = _simulation.BuildStairwell(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public CommandResult DemolishRoom(DemolishRoomCommand cmd)
        {
            var result = _simulation.DemolishRoom(cmd);
            InvalidateProjectionCaches();
            return result;
        }

        public void Reset()
        {
            _simulation = TowerSimulation.CreateStandardFiveFloor();
            InvalidateProjectionCaches();
        }

        /// <summary>
        /// Seeds the elevator bank with morning-rush passengers for the standard five-floor scenario.
        /// Only seeds if the bank is empty so it is idempotent on reset or reload.
        /// </summary>
        public void SeedMorningRush()
        {
            _simulation.SeedMorningRush();
            InvalidateProjectionCaches();
        }


        public ElevatorBankCongestionProjection CongestionProjection()
        {
            var floorCount = _simulation.Topology.FloorCount;
            var carCount = _simulation.ElevatorBank.Cars.Count;
            var queuedTotal = _simulation.TotalQueuedElevatorPassengers;
            if (_cachedCongestionProjection != null &&
                _cachedCongestionTick == _simulation.CurrentTick &&
                _cachedFloorCount == floorCount &&
                _cachedCarCount == carCount &&
                _cachedQueuedTotal == queuedTotal)
            {
                return _cachedCongestionProjection;
            }

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
            _cachedFloorCount = floorCount;
            _cachedCarCount = carCount;
            _cachedQueuedTotal = queuedTotal;
            return _cachedCongestionProjection;
        }

        public TowerProjection Projection() => TransitProjection();

        public TowerProjection TransitProjection()
        {
            var transitFloorCount = _simulation.Topology.FloorCount;
            var transitResidentCount = _simulation.ResidentCount;
            var transitQueuedTotal = _simulation.TotalQueuedElevatorPassengers;
            var transitCarCount = _simulation.ElevatorBank.Cars.Count;
            if (_cachedTransitProjection != null &&
                _cachedTick == _simulation.CurrentTick &&
                _cachedFloorCount == transitFloorCount &&
                _cachedResidentCount == transitResidentCount &&
                _cachedQueuedTotal == transitQueuedTotal &&
                _cachedCarCount == transitCarCount)
            {
                return _cachedTransitProjection;
            }

            // Index elevator bank passenger states for rush hour and transit visualization
            var snapshot = _simulation.ElevatorBank.Snapshot();

            var deliveredByPerson = new Dictionary<EntityId, ElevatorPassenger>(snapshot.DeliveredPassengers.Count);
            foreach (var p in snapshot.DeliveredPassengers)
            {
                deliveredByPerson[p.PersonId] = p;
            }

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
            _cachedFloorCount = transitFloorCount;
            _cachedResidentCount = transitResidentCount;
            _cachedQueuedTotal = transitQueuedTotal;
            _cachedCarCount = transitCarCount;
            return _cachedTransitProjection;
        }

        /// <summary>
        /// Public escape hatch for code that mutates <see cref="Simulation"/>
        /// directly instead of through this session's command wrappers.
        /// </summary>
        public void InvalidateProjectionCaches()
        {
            _cachedTransitProjection = null;
            _cachedCongestionProjection = null;
            _cachedTick = -1;
            _cachedCongestionTick = -1;
            _cachedFloorCount = -1;
            _cachedResidentCount = -1;
            _cachedQueuedTotal = -1;
            _cachedCarCount = -1;
        }
    }
}
