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
        Riding,
        Outside
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
                Route = trip.PlannedRoute;
                CurrentFloor = 0;
                CurrentX = 0f;
                LastProgressTick = trip.DepartureTick.Value;
            }

            public TripRecord Trip { get; }

            public int CurrentLegIndex { get; set; }

            public int LegRemainingTicks { get; set; }
            public int LegRemainingSeconds { get; set; }
            public int LegDurationSeconds { get; set; }
            public bool LegacyLegProgress { get; set; }
            public long LastReconsiderationTick { get; set; } = long.MinValue;
            public long LastProgressTick { get; set; }

            public bool IsQueuedInElevator { get; set; }

            public bool IsRidingElevator { get; set; }

            public TransitRoute Route { get; set; }

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
            if (trip.PlannedRoute?.Legs == null)
            {
                CancelInvalidRestoredTrip(trip);
                return;
            }
            var execution = new ActiveTripExecution(trip)
            {
                CurrentLegIndex = legIndex,
                LegRemainingTicks = legRemaining,
                LegRemainingSeconds = checked(legRemaining * 60),
                LegDurationSeconds = trip.PlannedRoute.Legs.Count > legIndex ? Math.Max(60, checked(trip.PlannedRoute.Legs[legIndex].Cost * 60)) : 0,
                LegacyLegProgress = true,
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
                    routeDataVersion = 1,
                    movementDataVersion = 2,
                    checkpointTick = exec.LastProgressTick,
                    legRemainingSeconds = exec.LegRemainingSeconds,
                    legDurationSeconds = exec.LegDurationSeconds,
                    legacyLegProgress = exec.LegacyLegProgress,
                    lastReconsiderationTick = exec.LastReconsiderationTick,
                    plannedRoute = SaveRoute(exec.Trip.PlannedRoute),
                    executionRoute = SaveRoute(exec.Route),
                    tripId = exec.Trip.Id.Value,
                    personId = exec.Trip.PersonId.Value,
                    originRoomId = exec.Trip.Origin.IsOutside ? 0 : exec.Trip.Origin.RoomId.Value,
                    destinationRoomId = exec.Trip.Destination.IsOutside ? 0 : exec.Trip.Destination.RoomId.Value,
                    originLocationKind = (int)exec.Trip.Origin.Kind,
                    destinationLocationKind = (int)exec.Trip.Destination.Kind,
                    purpose = (int)exec.Trip.Purpose,
                    outsideServiceTransactionRecorded = exec.Trip.OutsideServiceTransactionRecorded,
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

        private static TransitRouteSaveData SaveRoute(TransitRoute route)
        {
            if (route == null) return null;
            var legs = new TransitEdgeSaveData[route.Legs.Count];
            for (var i = 0; i < legs.Length; i++)
            {
                var edge = route.Legs[i];
                legs[i] = new TransitEdgeSaveData
                {
                    fromNodeId = edge.FromNodeId.Value, toNodeId = edge.ToNodeId.Value,
                    cost = edge.Cost, mode = (int)edge.Mode,
                    durationDataVersion = 1, durationSeconds = edge.DurationSeconds, hasPhysicalDuration = edge.HasPhysicalDuration
                };
            }
            return new TransitRouteSaveData
            {
                originNodeId = route.OriginNodeId.Value, destinationNodeId = route.DestinationNodeId.Value,
                originX = route.Origin.X, originFloor = route.Origin.Floor,
                destinationX = route.Destination.X, destinationFloor = route.Destination.Floor, legs = legs
            };
        }

        private static TransitRoute RestoreRoute(TransitRouteSaveData route)
        {
            if (route == null) return null;
            if (route.originNodeId <= 0 || route.destinationNodeId <= 0 || route.legs == null)
                throw new ArgumentException("Invalid transit route checkpoint.", nameof(route));
            var expectedNode = route.originNodeId;
            var legs = new List<TransitEdge>();
            foreach (var edge in route.legs ?? Array.Empty<TransitEdgeSaveData>())
            {
                if (edge == null || edge.fromNodeId != expectedNode || edge.toNodeId <= 0 ||
                    edge.durationDataVersion < 0 || edge.durationDataVersion > 1 || edge.durationSeconds < 0 ||
                    edge.cost < 0 || (edge.durationDataVersion >= 1 && !edge.hasPhysicalDuration && edge.durationSeconds != edge.cost * 60L) ||
                    !Enum.IsDefined(typeof(TransitMode), edge.mode))
                    throw new ArgumentException("Invalid transit route leg checkpoint.", nameof(route));
                expectedNode = edge.toNodeId;
                legs.Add(new TransitEdge(new EntityId(edge.fromNodeId), new EntityId(edge.toNodeId),
                    edge.cost, (TransitMode)edge.mode, edge.durationDataVersion >= 1 && edge.hasPhysicalDuration ? edge.durationSeconds : -1));
            }
            if (expectedNode != route.destinationNodeId)
                throw new ArgumentException("Transit route checkpoint does not reach its destination.", nameof(route));
            return new TransitRoute(new EntityId(route.originNodeId), new EntityId(route.destinationNodeId),
                new CellCoordinate(route.originX, route.originFloor),
                new CellCoordinate(route.destinationX, route.destinationFloor), legs);
        }

        public void RestoreFromSaveData(ActiveTripSaveData[] data, BuildingTopologyState topology, TransitRoutePlanner planner, Tick? checkpointTick = null)
        {
            ClearActiveTrips();
            if (data == null) return;

            foreach (var t in data)
            {
                if (t == null || t.routeDataVersion < 0 || t.routeDataVersion > 1 ||
                    t.movementDataVersion < 0 || t.movementDataVersion > 2 || t.legRemainingSeconds < 0 || t.legDurationSeconds < 0 ||
                    !Enum.IsDefined(typeof(TripState), t.state) || !Enum.IsDefined(typeof(TripPurpose), t.purpose) ||
                    !Enum.IsDefined(typeof(WorldLocationKind), t.originLocationKind) || !Enum.IsDefined(typeof(WorldLocationKind), t.destinationLocationKind) ||
                    t.departureTick < 0 || t.waitTicks < 0 ||
                    t.currentLegIndex < 0 || t.legRemainingTicks < 0 || t.legRemainingTicks > int.MaxValue / 60 || float.IsNaN(t.currentX) || float.IsInfinity(t.currentX))
                    throw new ArgumentException("Invalid active transit checkpoint.", nameof(data));
                var origin = t.originLocationKind == (int)WorldLocationKind.Outside
                    ? WorldLocation.Outside : WorldLocation.InRoom(new EntityId(t.originRoomId));
                var destination = t.destinationLocationKind == (int)WorldLocationKind.Outside
                    ? WorldLocation.Outside : WorldLocation.InRoom(new EntityId(t.destinationRoomId));
                var originNode = topology?.TransitGraph?.GetNodeForLocation(origin);
                var destNode = topology?.TransitGraph?.GetNodeForLocation(destination);
                TransitRoute route = t.routeDataVersion >= 1 ? RestoreRoute(t.plannedRoute) : null;
                if (t.routeDataVersion == 0 && originNode != null && destNode != null && !origin.Equals(destination) && planner != null)
                {
                    route = planner.FindRoute(originNode.Id, destNode.Id);
                }

                var executionRoute = t.routeDataVersion == 1 ? RestoreRoute(t.executionRoute) : route;
                if (t.routeDataVersion == 1 && (route == null || executionRoute == null ||
                    t.currentLegIndex >= executionRoute.Legs.Count ||
                    (t.isQueuedInElevator && t.isRidingElevator) ||
                    ((t.isQueuedInElevator || t.isRidingElevator) && executionRoute.Legs[t.currentLegIndex].Mode != TransitMode.Elevator)))
                    throw new ArgumentException("Invalid active transit route progress checkpoint.", nameof(data));

                var legDurationSeconds = ValidateMovementCheckpoint(t, executionRoute, checkpointTick);
                var trip = new TripRecord(
                    new EntityId(t.tripId),
                    new EntityId(t.personId),
                    origin,
                    destination,
                    (TripPurpose)t.purpose,
                    new Tick(t.departureTick),
                    route);

                if (t.state == (int)TripState.InProgress)
                {
                    trip.Begin();
                }

                trip.AddWaitTicks(t.waitTicks);
                trip.RestoreOutsideServiceTransactionRecorded(t.outsideServiceTransactionRecorded);

                RestoreTripExecution(
                    trip,
                    t.currentLegIndex,
                    t.legRemainingTicks,
                    t.isQueuedInElevator,
                    t.isRidingElevator,
                    t.currentFloor,
                    t.currentX);
                if (t.routeDataVersion >= 1 && _activeTripsByPerson.TryGetValue(trip.PersonId, out var execution))
                {
                    execution.Route = executionRoute;
                    if (t.movementDataVersion >= 1)
                    {
                        execution.LegRemainingSeconds = t.legRemainingSeconds;
                        execution.LegDurationSeconds = legDurationSeconds;
                        execution.LegacyLegProgress = t.legacyLegProgress;
                        execution.LastReconsiderationTick = t.lastReconsiderationTick;
                        execution.LastProgressTick = t.movementDataVersion >= 2 ? t.checkpointTick :
                            checkpointTick?.Value ?? Math.Max(t.departureTick, t.lastReconsiderationTick);
                    }
                }
            }
        }

        private static int ValidateMovementCheckpoint(ActiveTripSaveData save, TransitRoute route, Tick? authoritativeTick)
        {
            if (authoritativeTick.HasValue && save.departureTick > authoritativeTick.Value.Value)
                throw new ArgumentException("Transit departure is after the checkpoint clock.", nameof(save));
            if (save.movementDataVersion >= 2 && (save.checkpointTick < save.departureTick ||
                (authoritativeTick.HasValue && save.checkpointTick > authoritativeTick.Value.Value)))
                throw new ArgumentException("Invalid transit checkpoint chronology.", nameof(save));
            if (save.movementDataVersion >= 1)
            {
                var last = save.lastReconsiderationTick;
                var upper = save.movementDataVersion >= 2 ? save.checkpointTick : authoritativeTick?.Value ?? long.MaxValue - 1;
                if (last != long.MinValue && (last < save.departureTick || last > upper))
                    throw new ArgumentException("Invalid transit reconsideration chronology.", nameof(save));
            }
            if (route == null) return save.legDurationSeconds; // Older unreachable records keep the established cancellation path.
            if (save.currentLegIndex >= route.Legs.Count || (save.isQueuedInElevator && save.isRidingElevator))
                throw new ArgumentException("Invalid active route progress.", nameof(save));
            var leg = route.Legs[save.currentLegIndex];
            if ((save.isQueuedInElevator || save.isRidingElevator) && leg.Mode != TransitMode.Elevator)
                throw new ArgumentException("Elevator state does not match the active route leg.", nameof(save));
            if (save.movementDataVersion >= 1)
            {
                var duration = save.legacyLegProgress ? Math.Max(60L, leg.Cost * 60L) : leg.DurationSeconds;
                // Version 1's legacy restoration used the physical edge duration as metadata while advancing whole minutes.
                // Preserve that minute progress, but canonicalize its duration for subsequent version-2 checkpoints.
                var previousLegacyDuration = save.movementDataVersion == 1 && save.legacyLegProgress && leg.HasPhysicalDuration &&
                    save.legDurationSeconds == Math.Max(60L, leg.DurationSeconds);
                if ((!save.legacyLegProgress && !leg.HasPhysicalDuration) || duration > int.MaxValue ||
                    (!previousLegacyDuration && save.legDurationSeconds != duration) ||
                    save.legRemainingSeconds > duration || save.legRemainingTicks != (save.legRemainingSeconds + 59L) / 60 ||
                    (save.legacyLegProgress && save.legRemainingSeconds % 60 != 0))
                    throw new ArgumentException("Inconsistent active transit temporal progress.", nameof(save));
                return (int)duration;
            }
            return save.legDurationSeconds;
        }

        public void SubmitTrip(TripRecord trip, BuildingTopologyState topology, Tick currentTick, PopulationState population,
            Action<TripRecord, PersonRecord> onTripCompleted = null)
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
                    person.UpdateLocation(trip.Destination);
                    person.UpdateActivity(PurposeToActivity(trip.Purpose));
                    ReconcileArrivalActivity(person, currentTick);
                }

                RecordOutsideArrivalCallback(trip, person, onTripCompleted);

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
                    person.UpdateLocation(trip.Destination);
                    person.UpdateActivity(PurposeToActivity(trip.Purpose));
                    ReconcileArrivalActivity(person, currentTick);
                }

                RecordOutsideArrivalCallback(trip, person, onTripCompleted);

                return;
            }

            if (trip.State == TripState.Planned)
            {
                trip.Begin();
            }

            var execution = new ActiveTripExecution(trip) { LastProgressTick = currentTick.Value };
            var firstLeg = trip.PlannedRoute.Legs[0];
            StartLeg(execution, firstLeg);

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
            PopulationState population,
            float lobbyCapacityMultiplier = 1f,
            Action<TripRecord, PersonRecord> onTripCompleted = null)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (elevatorBank == null) throw new ArgumentNullException(nameof(elevatorBank));

            elevatorBank.BeginAdvanceTick(lobbyCapacityMultiplier);
            for (var step = 0; step < ElevatorBank.SecondsPerTick / ElevatorBank.SubstepSeconds; step++)
            {
                elevatorBank.AdvanceSubstep(lobbyCapacityMultiplier, step == 0);
                for (var i = _activeTrips.Count - 1; i >= 0; i--)
                {
                    var execution = _activeTrips[i];
                    var trip = execution.Trip;
                    execution.LastProgressTick = tick.Value;
                    var person = population?.TryGetPerson(trip.PersonId, out var p) == true ? p : null;
                    if (trip.State == TripState.Cancelled)
                    {
                        if (execution.IsQueuedInElevator) elevatorBank.TryRemoveQueuedPassenger(trip.PersonId, out _);
                        _activeTrips.RemoveAt(i); _activeTripsByPerson.Remove(trip.PersonId); continue;
                    }
                    if (execution.Route?.Legs == null)
                    {
                        CancelTrip(i, execution, person, elevatorBank); continue;
                    }
                    var budget = ElevatorBank.SubstepSeconds;
                    // A bounded budget may span several short hall edges, without rounding each edge to a minute.
                    while (execution.CurrentLegIndex < execution.Route.Legs.Count)
                    {
                        var leg = execution.Route.Legs[execution.CurrentLegIndex];
                        topology.TransitGraph.TryGetNode(leg.FromNodeId, out var fromNode);
                        topology.TransitGraph.TryGetNode(leg.ToNodeId, out var toNode);
                        if (leg.Mode == TransitMode.Walk)
                        {
                            if (execution.LegacyLegProgress)
                            {
                                if (step != 0) break;
                                execution.LegRemainingTicks = Math.Max(0, execution.LegRemainingTicks - 1);
                                execution.LegRemainingSeconds = execution.LegRemainingTicks * 60;
                            }
                            else
                            {
                                var used = Math.Min(budget, execution.LegRemainingSeconds);
                                execution.LegRemainingSeconds -= used;
                                execution.LegRemainingTicks = (execution.LegRemainingSeconds + 59) / 60;
                                budget -= used;
                            }
                            if (fromNode != null && toNode != null)
                            {
                                var progress = Math.Clamp(1f - (float)execution.LegRemainingSeconds / Math.Max(1, execution.LegDurationSeconds), 0f, 1f);
                                execution.CurrentFloor = fromNode.Floor == toNode.Floor ? fromNode.Floor : progress < .5f ? fromNode.Floor : toNode.Floor;
                                execution.CurrentX = fromNode.Location.X + (toNode.Location.X - fromNode.Location.X) * progress;
                            }
                            if (execution.LegRemainingSeconds > 0) break;
                            var legacy = execution.LegacyLegProgress;
                            AdvanceLeg(i, execution, tick, person, onTripCompleted);
                            if (!IsPersonTravelling(trip.PersonId) || legacy || budget == 0) break;
                            continue;
                        }

                        var origin = fromNode?.Floor ?? execution.CurrentFloor;
                        var destination = toNode?.Floor ?? origin;
                        var usable = elevatorBank.Cars.Count > 0 && origin >= elevatorBank.MinFloor && origin <= elevatorBank.MaxFloor &&
                            destination >= elevatorBank.MinFloor && destination <= elevatorBank.MaxFloor;
                        if (!usable)
                        {
                            var alternative = FindStairAlternative(execution, topology.TransitGraph, long.MaxValue, false);
                            if (alternative == null) { CancelTrip(i, execution, person, elevatorBank); break; }
                            if (execution.IsQueuedInElevator)
                            {
                                if (!elevatorBank.TryRemoveQueuedPassenger(trip.PersonId, out var stranded)) break;
                                trip.AddWaitTicks((int)stranded.WaitTicks);
                            }
                            AcceptAlternative(execution, alternative);
                            continue;
                        }
                        if (!execution.IsQueuedInElevator && !execution.IsRidingElevator)
                        {
                            var alternative = ChooseStairs(execution, topology.TransitGraph, elevatorBank, origin, destination, tick, false);
                            if (alternative != null) { AcceptAlternative(execution, alternative); continue; }
                            elevatorBank.EnqueuePassenger(new ElevatorPassenger(trip.PersonId, origin, destination));
                            execution.IsQueuedInElevator = true;
                            execution.CurrentFloor = origin;
                            execution.CurrentX = fromNode?.Location.X ?? 0f;
                        }
                        else if (elevatorBank.IsPassengerInCar(trip.PersonId, out var car))
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
                            AdvanceLeg(i, execution, tick, person, onTripCompleted);
                            if (IsPersonTravelling(trip.PersonId) && budget > 0) continue;
                        }
                        else if (execution.IsQueuedInElevator)
                        {
                            var liveWait = elevatorBank.GetPassengerWaitTicks(trip.PersonId);
                            if (liveWait >= 15 && (execution.LastReconsiderationTick == long.MinValue || tick.Value - execution.LastReconsiderationTick >= 5))
                            {
                                execution.LastReconsiderationTick = tick.Value;
                                var alternative = ChooseStairs(execution, topology.TransitGraph, elevatorBank, origin, destination, tick, true);
                                // Planning is read-only. Route mutation follows successful queue removal.
                                if (alternative != null && elevatorBank.TryRemoveQueuedPassenger(trip.PersonId, out var waiting))
                                {
                                    trip.AddWaitTicks((int)waiting.WaitTicks);
                                    AcceptAlternative(execution, alternative);
                                    continue;
                                }
                            }
                        }
                        break;
                    }
                    if (IsPersonTravelling(trip.PersonId) && execution.CurrentLegIndex >= execution.Route.Legs.Count)
                        CompleteTrip(i, execution, tick, person, onTripCompleted);
                }
            }
        }

        private static void StartLeg(ActiveTripExecution execution, TransitEdge leg)
        {
            execution.LegacyLegProgress = !leg.HasPhysicalDuration;
            execution.LegDurationSeconds = leg.HasPhysicalDuration ? leg.DurationSeconds : Math.Max(60, leg.DurationSeconds);
            execution.LegRemainingSeconds = execution.LegDurationSeconds;
            execution.LegRemainingTicks = (execution.LegRemainingSeconds + 59) / 60;
        }

        private static TransitRoute FindStairAlternative(ActiveTripExecution execution, HierarchicalTransitGraph graph, long deadline, bool ordinary)
        {
            if (execution?.Route == null || graph == null || execution.CurrentLegIndex >= execution.Route.Legs.Count) return null;
            var route = new TransitRoutePlanner(graph.NonElevatorGraph).FindRoute(
                execution.Route.Legs[execution.CurrentLegIndex].FromNodeId, execution.Route.DestinationNodeId);
            if (route == null) return null;
            long seconds = 0;
            var up = 0; var down = 0;
            foreach (var edge in route.Legs)
            {
                seconds += edge.DurationSeconds;
                if (graph.TryGetNode(edge.FromNodeId, out var from) && graph.TryGetNode(edge.ToNodeId, out var to))
                { up += Math.Max(0, to.Floor - from.Floor); down += Math.Max(0, from.Floor - to.Floor); }
            }
            if (ordinary && (up > 2 || down > 4)) return null;
            return (seconds + 59) / 60 <= deadline ? route : null;
        }

        private static TransitRoute ChooseStairs(ActiveTripExecution execution, HierarchicalTransitGraph graph,
            ElevatorBank bank, int origin, int destination, Tick tick, bool reconsidering)
        {
            if ((destination > origin && destination - origin > 2) || (origin > destination && origin - destination > 4)) return null;
            var remaining = 360 - (tick.Value - execution.Trip.DepartureTick.Value);
            var stairs = FindStairAlternative(execution, graph, remaining, true);
            if (stairs == null) return null;
            long stairCostSeconds = 0;
            foreach (var edge in stairs.Legs)
            {
                stairCostSeconds += edge.DurationSeconds;
                if (graph.TryGetNode(edge.FromNodeId, out var from) && graph.TryGetNode(edge.ToNodeId, out var to))
                    stairCostSeconds += Math.Max(0, to.Floor - from.Floor) * 30L + Math.Max(0, from.Floor - to.Floor) * 15L;
            }
            long elevatorSeconds = bank.EstimateArrivalSeconds(origin, destination, reconsidering ? execution.Trip.PersonId : (EntityId?)null);
            for (var i = execution.CurrentLegIndex + 1; i < execution.Route.Legs.Count; i++)
                elevatorSeconds += execution.Route.Legs[i].DurationSeconds;
            var improvement = elevatorSeconds - stairCostSeconds;
            return improvement >= (reconsidering ? 180 : 1) ? stairs : null;
        }

        private static void AcceptAlternative(ActiveTripExecution execution, TransitRoute route)
        {
            execution.Route = route;
            execution.CurrentLegIndex = 0;
            if (route.Legs.Count > 0) StartLeg(execution, route.Legs[0]);
            else { execution.LegRemainingTicks = 0; execution.LegRemainingSeconds = 0; }
            execution.IsQueuedInElevator = false;
            execution.IsRidingElevator = false;
        }

        private static void CancelInvalidRestoredTrip(TripRecord trip)
        {
            if (trip.State != TripState.Completed && trip.State != TripState.Cancelled)
                trip.Cancel();
        }

        private void AdvanceLeg(int listIndex, ActiveTripExecution execution, Tick tick, PersonRecord person,
            Action<TripRecord, PersonRecord> onTripCompleted)
        {
            execution.CurrentLegIndex++;
            if (execution.CurrentLegIndex >= execution.Route.Legs.Count)
            {
                CompleteTrip(listIndex, execution, tick, person, onTripCompleted);
            }
            else
            {
                var nextLeg = execution.Route.Legs[execution.CurrentLegIndex];
                StartLeg(execution, nextLeg);
                execution.IsQueuedInElevator = false;
                execution.IsRidingElevator = false;
            }
        }

        private void CancelTrip(int listIndex, ActiveTripExecution execution, PersonRecord person, ElevatorBank elevatorBank = null)
        {
            if (execution.IsQueuedInElevator && elevatorBank != null)
            {
                elevatorBank.TryRemoveQueuedPassenger(execution.Trip.PersonId, out _);
            }
            if (execution.Trip.State == TripState.InProgress) execution.Trip.Cancel();
            person?.UpdateActivity(ActivityKind.Idle);
            _activeTrips.RemoveAt(listIndex);
            _activeTripsByPerson.Remove(execution.Trip.PersonId);
        }

        private void CompleteTrip(int listIndex, ActiveTripExecution execution, Tick tick, PersonRecord person,
            Action<TripRecord, PersonRecord> onTripCompleted)
        {
            var trip = execution.Trip;
            if (trip.State == TripState.InProgress)
            {
                trip.Complete(tick);
            }

            if (person != null)
            {
                person.UpdateLocation(trip.Destination);
                person.UpdateActivity(PurposeToActivity(trip.Purpose));
                ReconcileArrivalActivity(person, tick);
                ResidentPurposeSystem.AdvancePerson(person, tick);
            }

            RecordOutsideArrivalCallback(trip, person, onTripCompleted);

            _activeTrips.RemoveAt(listIndex);
            _activeTripsByPerson.Remove(trip.PersonId);
        }

        private static void RecordOutsideArrivalCallback(TripRecord trip, PersonRecord person,
            Action<TripRecord, PersonRecord> onTripCompleted)
        {
            if (trip == null || onTripCompleted == null || trip.State != TripState.Completed ||
                !trip.Destination.IsOutside ||
                (trip.Purpose != TripPurpose.Food && trip.Purpose != TripPurpose.MoveOut))
                return;

            if (!trip.OutsideServiceTransactionRecorded)
            {
                onTripCompleted(trip, person);
                trip.TryMarkOutsideServiceTransactionRecorded();
            }
        }

        /// <summary>
        /// 24/7 arrival fix: a Hygiene trip lands Idle, but if the schedule block expects
        /// Sleep at home (or Work at work) the resident should rest/work immediately so
        /// needs recover instead of idling through the night (or shift) until the next
        /// block boundary. Only corrects when location already matches the block.
        /// </summary>
        public static void ReconcileArrivalActivity(PersonRecord person, Tick tick)
        {
            if (person == null) return;
            var activeLabel = person.Schedule.ActiveLabelAt(tick);
            if (activeLabel == DailySchedule.LabelSleep &&
                person.CurrentLocation.Equals(WorldLocation.InRoom(person.HomeRoomId)) &&
                person.CurrentActivity == ActivityKind.Idle)
            {
                person.UpdateActivity(ActivityKind.Sleeping);
            }
            else if ((activeLabel == DailySchedule.LabelLeisure || activeLabel == DailySchedule.LabelEat) &&
                person.CurrentLocation.Equals(WorldLocation.InRoom(person.HomeRoomId)) &&
                person.CurrentActivity == ActivityKind.Sleeping &&
                person.GetNeedSatisfaction(NeedKind.Energy) >= DynamicScheduleArbitrator.CriticalEnergyThreshold)
            {
                person.UpdateActivity(ActivityKind.Leisure);
            }
            else if (activeLabel == DailySchedule.LabelWork &&
                person.CurrentLocation.Equals(person.WorkplaceLocation) &&
                person.CurrentActivity == ActivityKind.Idle)
            {
                person.UpdateActivity(ActivityKind.Working);
            }
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
                    execution.Trip.Destination.IsOutside ? (EntityId?)null : execution.Trip.Destination.RoomId,
                    phase);
            }

            if (population != null && population.TryGetPerson(personId, out var person))
            {
                if (person.CurrentLocation.IsOutside && topology?.TransitGraph?.OutsideNode != null)
                {
                    var outside = topology.TransitGraph.OutsideNode;
                    return new ResidentSpatialPosition(personId, outside.Floor, outside.Location.X,
                        person.CurrentActivity, null, ResidentMovementPhase.Outside);
                }
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
                TripPurpose.MoveOut => ActivityKind.Idle,
                _ => ActivityKind.Idle
            };
        }
    }
}
