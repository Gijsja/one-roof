using System;
using System.Collections.Generic;
using OneRoof.Application.Transit;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
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
        private TransitPrototypeProjection _cachedTransitProjection;
        private long _cachedTick = -1;

        public TowerSimulationSession(TowerSimulation simulation = null)
        {
            _simulation = simulation ?? TowerSimulation.CreateStandardFiveFloor();
        }

        public TowerSimulation Simulation => _simulation;

        public BuildingTopologyState Topology => _simulation.Topology;

        public PopulationState Population => _simulation.Population;

        public ElevatorBank ElevatorBank => _simulation.ElevatorBank;

        public TowerEconomyState Economy => _simulation.Economy;

        public long CurrentTick => _simulation.CurrentTick;

        public int ResidentCount => _simulation.ResidentCount;

        public int FloorCount => _simulation.Topology.FloorCount;

        public void AdvanceOneTick()
        {
            _simulation.AdvanceOneTick();
            _cachedTransitProjection = null;
        }

        public void AddCapacity()
        {
            _simulation.AddElevatorCar();
            _cachedTransitProjection = null;
        }

        public CommandResult BuildFloorSlab(BuildFloorSlabCommand cmd)
        {
            var result = _simulation.BuildFloorSlab(cmd);
            _cachedTransitProjection = null;
            return result;
        }

        public CommandResult BuildRoom(BuildRoomCommand cmd)
        {
            var result = _simulation.BuildRoom(cmd);
            _cachedTransitProjection = null;
            return result;
        }

        public CommandResult AddElevatorShaft(AddElevatorShaftCommand cmd)
        {
            var result = _simulation.AddElevatorShaft(cmd);
            _cachedTransitProjection = null;
            return result;
        }

        public CommandResult BuildStairwell(BuildStairwellCommand cmd)
        {
            var result = _simulation.BuildStairwell(cmd);
            _cachedTransitProjection = null;
            return result;
        }

        public CommandResult DemolishRoom(DemolishRoomCommand cmd)
        {
            var result = _simulation.DemolishRoom(cmd);
            _cachedTransitProjection = null;
            return result;
        }

        public void Reset()
        {
            _simulation = TowerSimulation.CreateStandardFiveFloor();
            _cachedTransitProjection = null;
            _cachedTick = -1;
        }

        public ElevatorBankCongestionProjection CongestionProjection()
        {
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

            return new ElevatorBankCongestionProjection(
                _simulation.CurrentTick,
                totalQueued,
                inTransit,
                _simulation.ResidentCount - _simulation.ActiveTripCount,
                _simulation.AverageElevatorWaitTicks,
                bottleneckFloor,
                overallSeverity,
                floorProjections,
                elevProjections);
        }

        public TransitPrototypeProjection Projection() => TransitProjection();

        public TransitPrototypeProjection TransitProjection()
        {
            if (_cachedTransitProjection != null && _cachedTick == _simulation.CurrentTick)
            {
                return _cachedTransitProjection;
            }

            // Index elevator bank passenger states for rush hour and transit visualization
            var deliveredByPerson = new Dictionary<EntityId, ElevatorPassenger>(_simulation.ElevatorBank.DeliveredPassengers.Count);
            foreach (var p in _simulation.ElevatorBank.DeliveredPassengers)
            {
                deliveredByPerson[p.PersonId] = p;
            }

            var ridingByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var car in _simulation.ElevatorBank.Cars)
            {
                foreach (var p in car.Passengers)
                {
                    ridingByPerson[p.PersonId] = p;
                }
            }

            var queuedByPerson = new Dictionary<EntityId, ElevatorPassenger>();
            foreach (var queue in _simulation.ElevatorBank.FloorQueues.Values)
            {
                foreach (var p in queue)
                {
                    queuedByPerson[p.PersonId] = p;
                }
            }

            var residents = new List<TransitResidentProjection>(_simulation.ResidentCount);
            var arrivedCount = 0;

            foreach (var person in _simulation.Population.Persons)
            {
                TransitResidentStatus status;
                int targetFloor;

                if (deliveredByPerson.TryGetValue(person.Id, out var delivered))
                {
                    status = TransitResidentStatus.Arrived;
                    targetFloor = delivered.DestinationFloor;
                }
                else if (ridingByPerson.TryGetValue(person.Id, out var riding))
                {
                    status = TransitResidentStatus.Riding;
                    targetFloor = riding.DestinationFloor;
                }
                else if (queuedByPerson.TryGetValue(person.Id, out var queued))
                {
                    status = TransitResidentStatus.Queued;
                    targetFloor = queued.DestinationFloor;
                }
                else
                {
                    var spatial = _simulation.GetResidentPosition(person.Id);
                    status = spatial.Activity == ActivityKind.Commuting
                        ? TransitResidentStatus.Riding
                        : (spatial.Activity == ActivityKind.Working || spatial.Activity == ActivityKind.Eating || spatial.Activity == ActivityKind.Sleeping
                            ? TransitResidentStatus.Arrived
                            : TransitResidentStatus.Queued);
                    targetFloor = spatial.Floor;
                }

                if (status == TransitResidentStatus.Arrived)
                {
                    arrivedCount++;
                }

                residents.Add(new TransitResidentProjection(
                    person.Id.Value,
                    targetFloor,
                    status));
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

            _cachedTransitProjection = new TransitPrototypeProjection(
                _simulation.CurrentTick,
                _simulation.TotalQueuedElevatorPassengers,
                arrivedCount,
                _simulation.AverageElevatorWaitTicks,
                residents,
                elevators);

            _cachedTick = _simulation.CurrentTick;
            return _cachedTransitProjection;
        }
    }
}
