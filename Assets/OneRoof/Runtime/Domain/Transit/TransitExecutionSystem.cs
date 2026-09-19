using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Transit
{
    public enum ResidentMovementPhase
    {
        InRoom,
        Walking,
        Queued,
        Riding
    }

    public readonly struct ResidentSpatialPosition
    {
        public ResidentSpatialPosition(
            EntityId residentId,
            int floor,
            float x,
            ActivityKind activity,
            EntityId? roomId,
            ResidentMovementPhase phase = ResidentMovementPhase.InRoom)
        {
            ResidentId = residentId;
            Floor = floor;
            X = x;
            Activity = activity;
            RoomId = roomId;
            Phase = phase;
        }

        public EntityId ResidentId { get; }

        public int Floor { get; }

        public float X { get; }

        public ActivityKind Activity { get; }

        public EntityId? RoomId { get; }

        public ResidentMovementPhase Phase { get; }
    }

    /// <summary>
    /// Pure C# domain transit execution system that processes in-flight resident trips leg-by-leg.
    /// Manages discrete walking ticks, elevator queueing/riding via ElevatorBank, and emits spatial positions.
    /// </summary>
    public sealed class TransitExecutionSystem
    {
        public sealed class ActiveTripExecution
        {
            public ActiveTripExecution(TripRecord trip)
            {
                Trip = trip ?? throw new ArgumentNullException(nameof(trip));
                CurrentLegIndex = 0;
                LegRemainingTicks = 0;
                IsQueuedInElevator = false;
                IsRidingElevator = false;
                CurrentFloor = 0;
                CurrentX = 0f;
            }

            public TripRecord Trip { get; }

            public int CurrentLegIndex { get; set; }

            public int LegRemainingTicks { get; set; }

            public bool IsQueuedInElevator { get; set; }

            public bool IsRidingElevator { get; set; }

            public int CurrentFloor { get; set; }

            public float CurrentX { get; set; }
        }

        private readonly List<ActiveTripExecution> _activeTrips = new List<ActiveTripExecution>();
        private readonly Dictionary<EntityId, ActiveTripExecution> _activeTripsByPerson = new Dictionary<EntityId, ActiveTripExecution>();

        public IReadOnlyList<ActiveTripExecution> ActiveTrips => _activeTrips;

        public int ActiveTripCount => _activeTrips.Count;

        public bool IsPersonTravelling(EntityId personId) => _activeTripsByPerson.ContainsKey(personId);

        public void RestoreTripExecution(
            TripRecord trip,
            int legIndex,
            int legRemaining,
            bool isQueued,
            bool isRiding,
            int floor,
            float x)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            var execution = new ActiveTripExecution(trip)
            {
                CurrentLegIndex = legIndex,
                LegRemainingTicks = legRemaining,
                IsQueuedInElevator = isQueued,
                IsRidingElevator = isRiding,
                CurrentFloor = floor,
                CurrentX = x
            };
            _activeTrips.Add(execution);
            _activeTripsByPerson[trip.PersonId] = execution;
        }

        public void ClearActiveTrips()
        {
            _activeTrips.Clear();
            _activeTripsByPerson.Clear();
        }

        // ── Serialization ──────────────────────────────────────────────────────

        public ActiveTripSaveData[] ToSaveData()
        {
            var tripList = new List<ActiveTripSaveData>(_activeTrips.Count);
            foreach (var exec in _activeTrips)
            {
                tripList.Add(new ActiveTripSaveData
                {
                    tripId = exec.Trip.Id.Value,
                    personId = exec.Trip.PersonId.Value,
                    originRoomId = exec.Trip.OriginRoomId.Value,
                    destinationRoomId = exec.Trip.DestinationRoomId.Value,
                    purpose = (int)exec.Trip.Purpose,
                    departureTick = exec.Trip.DepartureTick.Value,
                    state = (int)exec.Trip.State,
                    waitTicks = exec.Trip.WaitTicks,
                    currentLegIndex = exec.CurrentLegIndex,
                    legRemainingTicks = exec.LegRemainingTicks,
                    isQueuedInElevator = exec.IsQueuedInElevator,
                    isRidingElevator = exec.IsRidingElevator,
                    currentFloor = exec.CurrentFloor,
                    currentX = exec.CurrentX
                });
            }

            return tripList.ToArray();
        }

        public void RestoreFromSaveData(ActiveTripSaveData[] data, BuildingTopologyState topology, TransitRoutePlanner planner)
        {
            ClearActiveTrips();
            if (data == null) return;

            foreach (var t in data)
            {
                var originNode = topology?.TransitGraph?.GetPortalNodeForRoom(new EntityId(t.originRoomId));
                var destNode = topology?.TransitGraph?.GetPortalNodeForRoom(new EntityId(t.destinationRoomId));
                TransitRoute route = null;
                if (originNode != null && destNode != null && t.originRoomId != t.destinationRoomId && planner != null)
                {
                    route = planner.FindRoute(originNode.Id, destNode.Id);
                }

                var trip = new TripRecord(
                    new EntityId(t.tripId),
                    new EntityId(t.personId),
                    new EntityId(t.originRoomId),
                    new EntityId(t.destinationRoomId),
                    (TripPurpose)t.purpose,
                    new Tick(t.departureTick),
                    route);

                if (t.state == (int)TripState.InProgress)
                {
                    trip.Begin();
                }

                trip.AddWaitTicks(t.waitTicks);

                RestoreTripExecution(
                    trip,
                    t.currentLegIndex,
                    t.legRemainingTicks,
                    t.isQueuedInElevator,
                    t.isRidingElevator,
                    t.currentFloor,
                    t.currentX);
            }
        }

        public void SubmitTrip(TripRecord trip, BuildingTopologyState topology, Tick currentTick, PopulationState population)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            if (topology == null) throw new ArgumentNullException(nameof(topology));

            var person = population?.TryGetPerson(trip.PersonId, out var p) == true ? p : null;
            if (_activeTripsByPerson.ContainsKey(trip.PersonId))
            {
                return;
            }

            // Local trips complete immediately
            if (trip.IsLocal)
            {
                if (trip.State == TripState.Planned)
                {
                    trip.Begin();
                }

                if (trip.State == TripState.InProgress)
                {
                    trip.Complete(currentTick);
                }

                if (person != null)
                {
                    person.UpdateLocation(trip.DestinationRoomId);
                    person.UpdateActivity(PurposeToActivity(trip.Purpose));
                }

                return;
            }

            // If trip has no planned route (unreachable), cancel it without teleporting
            if (trip.PlannedRoute == null)
            {
                if (trip.State == TripState.Planned)
                {
                    trip.Cancel();
                }
                return;
            }

            // 0-leg routes complete immediately
            if (trip.PlannedRoute.Legs.Count == 0)
            {
                if (trip.State == TripState.Planned)
                {
                    trip.Begin();
                }

                if (trip.State == TripState.InProgress)
                {
                    trip.Complete(currentTick);
                }

                if (person != null)
                {
                    person.UpdateLocation(trip.DestinationRoomId);
                    person.UpdateActivity(PurposeToActivity(trip.Purpose));
                }

                return;
            }

            if (trip.State == TripState.Planned)
            {
                trip.Begin();
            }

            var execution = new ActiveTripExecution(trip);
            var firstLeg = trip.PlannedRoute.Legs[0];
            execution.LegRemainingTicks = Math.Max(1, firstLeg.Cost);

            if (topology.TransitGraph.TryGetNode(firstLeg.FromNodeId, out var fromNode))
            {
                execution.CurrentFloor = fromNode.Floor;
                execution.CurrentX = fromNode.Location.X;
            }

            if (person != null)
            {
                person.UpdateActivity(ActivityKind.Commuting);
            }

            _activeTrips.Add(execution);
            _activeTripsByPerson[trip.PersonId] = execution;
        }

        public void Advance(
            Tick tick,
            BuildingTopologyState topology,
            ElevatorBank elevatorBank,
            PopulationState population)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (elevatorBank == null) throw new ArgumentNullException(nameof(elevatorBank));

            // 1. Advance elevator bank state machine
            elevatorBank.Advance(tick);

            // 2. Process all active resident trip legs
            for (var i = _activeTrips.Count - 1; i >= 0; i--)
            {
                var execution = _activeTrips[i];
                var trip = execution.Trip;
                var person = population?.TryGetPerson(trip.PersonId, out var p) == true ? p : null;

                if (trip.State == TripState.Cancelled)
                {
                    _activeTrips.RemoveAt(i);
                    _activeTripsByPerson.Remove(trip.PersonId);
                    continue;
                }

                if (execution.CurrentLegIndex >= trip.PlannedRoute.Legs.Count)
                {
                    CompleteTrip(i, execution, tick, person);
                    continue;
                }

                var leg = trip.PlannedRoute.Legs[execution.CurrentLegIndex];
                topology.TransitGraph.TryGetNode(leg.FromNodeId, out var fromNode);
                topology.TransitGraph.TryGetNode(leg.ToNodeId, out var toNode);

                if (leg.Mode == TransitMode.Walk)
                {
                    if (fromNode != null && toNode != null)
                    {
                        var totalDistance = Math.Max(1, Math.Abs(toNode.Location.X - fromNode.Location.X));
                        var progress = 1f - ((float)execution.LegRemainingTicks / totalDistance);
                        execution.CurrentFloor = fromNode.Floor;
                        execution.CurrentX = fromNode.Location.X + (toNode.Location.X - fromNode.Location.X) * Math.Clamp(progress, 0f, 1f);
                    }

                    if (execution.LegRemainingTicks > 0)
                    {
                        execution.LegRemainingTicks--;
                    }

                    if (execution.LegRemainingTicks <= 0)
                    {
                        AdvanceLeg(i, execution, tick, person);
                    }
                }
                else if (leg.Mode == TransitMode.Elevator)
                {
                    if (!execution.IsQueuedInElevator && !execution.IsRidingElevator)
                    {
                        var originFloor = fromNode?.Floor ?? 0;
                        var destFloor = toNode?.Floor ?? 0;

                        var passenger = new ElevatorPassenger(trip.PersonId, originFloor, destFloor);
                        elevatorBank.EnqueuePassenger(passenger);
                        execution.IsQueuedInElevator = true;
                        execution.CurrentFloor = originFloor;
                        execution.CurrentX = fromNode?.Location.X ?? 0f;
                    }
                    else if (execution.IsQueuedInElevator)
                    {
                        // Check if car picked up this passenger
                        if (elevatorBank.IsPassengerInCar(trip.PersonId, out var car))
                        {
                            execution.IsQueuedInElevator = false;
                            execution.IsRidingElevator = true;
                            execution.CurrentFloor = car.CurrentFloor;
                        }
                        else if (elevatorBank.TryTakeDeliveredPassenger(trip.PersonId, out var delivered))
                        {
                            trip.AddWaitTicks((int)delivered.WaitTicks);
                            execution.IsQueuedInElevator = false;
                            execution.IsRidingElevator = false;
                            AdvanceLeg(i, execution, tick, person);
                        }
                    }
                    else if (execution.IsRidingElevator)
                    {
                        if (elevatorBank.IsPassengerInCar(trip.PersonId, out var car))
                        {
                            execution.CurrentFloor = car.CurrentFloor;
                        }
                        else if (elevatorBank.TryTakeDeliveredPassenger(trip.PersonId, out var delivered))
                        {
                            trip.AddWaitTicks((int)delivered.WaitTicks);
                            execution.IsRidingElevator = false;
                            AdvanceLeg(i, execution, tick, person);
                        }
                    }
                }
            }
        }

        private void AdvanceLeg(int listIndex, ActiveTripExecution execution, Tick tick, PersonRecord person)
        {
            execution.CurrentLegIndex++;
            if (execution.CurrentLegIndex >= execution.Trip.PlannedRoute.Legs.Count)
            {
                CompleteTrip(listIndex, execution, tick, person);
            }
            else
            {
                var nextLeg = execution.Trip.PlannedRoute.Legs[execution.CurrentLegIndex];
                execution.LegRemainingTicks = Math.Max(1, nextLeg.Cost);
                execution.IsQueuedInElevator = false;
                execution.IsRidingElevator = false;
            }
        }

        private void CompleteTrip(int listIndex, ActiveTripExecution execution, Tick tick, PersonRecord person)
        {
            var trip = execution.Trip;
            if (trip.State == TripState.InProgress)
            {
                trip.Complete(tick);
            }

            if (person != null)
            {
                person.UpdateLocation(trip.DestinationRoomId);
                person.UpdateActivity(PurposeToActivity(trip.Purpose));
            }

            _activeTrips.RemoveAt(listIndex);
            _activeTripsByPerson.Remove(trip.PersonId);
        }

        public ResidentSpatialPosition GetResidentPosition(
            EntityId personId,
            PopulationState population,
            BuildingTopologyState topology)
        {
            if (_activeTripsByPerson.TryGetValue(personId, out var execution))
            {
                var phase = execution.IsQueuedInElevator
                    ? ResidentMovementPhase.Queued
                    : (execution.IsRidingElevator
                        ? ResidentMovementPhase.Riding
                        : ResidentMovementPhase.Walking);

                return new ResidentSpatialPosition(
                    personId,
                    execution.CurrentFloor,
                    execution.CurrentX,
                    ActivityKind.Commuting,
                    execution.Trip.DestinationRoomId,
                    phase);
            }

            if (population != null && population.TryGetPerson(personId, out var person))
            {
                var roomId = person.CurrentRoomId.Value > 0 ? person.CurrentRoomId : person.HomeRoomId;
                if (topology != null && topology.TryGetRoom(roomId, out var room))
                {
                    var centerX = room.Bounds.MinX + room.Bounds.Width * 0.5f;
                    return new ResidentSpatialPosition(
                        personId,
                        room.Floor,
                        centerX,
                        person.CurrentActivity,
                        roomId,
                        ResidentMovementPhase.InRoom);
                }
            }

            return new ResidentSpatialPosition(personId, 0, 0f, ActivityKind.Idle, null, ResidentMovementPhase.InRoom);
        }

        public static ActivityKind PurposeToActivity(TripPurpose purpose)
        {
            return purpose switch
            {
                TripPurpose.Work => ActivityKind.Working,
                TripPurpose.Food => ActivityKind.Eating,
                TripPurpose.Home => ActivityKind.Sleeping,
                TripPurpose.Leisure => ActivityKind.Leisure,
                TripPurpose.Hygiene => ActivityKind.Idle,
                _ => ActivityKind.Idle
            };
        }
    }
}
