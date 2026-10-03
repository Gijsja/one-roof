using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Trips
{
    /// <summary>
    /// Stateless Domain service that inspects schedule block transitions and emits
    /// <see cref="TripRecord"/> instances for residents whose activity block changed
    /// between <c>previousTick</c> and <c>currentTick</c>.
    ///
    /// <para>
    /// The service is intentionally stateless (ADR-020): it derives transitions by
    /// comparing adjacent ticks rather than caching previous block state per person.
    /// This makes it trivially testable and safe to call at any tick without session state.
    /// </para>
    /// </summary>
    public sealed class ScheduleTripGenerator
    {
        private BuildingTopology _topology;
        private HierarchicalTransitGraph _graph;
        private TransitRoutePlanner _planner;
        private TransitRoutePlanner _stairsPlanner;
        private Room[] _foodRooms;
        private Room[] _lobbyRooms;
        private readonly Dictionary<EntityId, long> _failedRouteCooldowns = new Dictionary<EntityId, long>();
        private readonly ElevatorBank _elevatorBank;
        private readonly Dictionary<(WorldLocation, WorldLocation), TransitRoute> _routineRoutes = new Dictionary<(WorldLocation, WorldLocation), TransitRoute>();

        private readonly DynamicScheduleArbitrator _arbitrator = new DynamicScheduleArbitrator();
        private int _nextTripId;
        public TransitDemandObserver DemandObserver { get; set; }

        /// <param name="topology">The building topology for room lookups.</param>
        /// <param name="graph">Pre-built transit graph for portal node lookups.</param>
        /// <param name="planner">Route planner for computing planned routes.</param>
        /// <param name="firstTripId">The starting integer for new trip entity IDs.</param>
        public ScheduleTripGenerator(
            BuildingTopology topology,
            HierarchicalTransitGraph graph,
            TransitRoutePlanner planner,
            int firstTripId = 1,
            ElevatorBank elevatorBank = null)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _graph    = graph    ?? throw new ArgumentNullException(nameof(graph));
            _planner  = planner  ?? throw new ArgumentNullException(nameof(planner));
            _elevatorBank = elevatorBank;
            RebuildRoomCandidates();
            RebuildStairsPlanner();

            if (firstTripId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstTripId), firstTripId, "First trip ID must be positive.");
            }

            _nextTripId = firstTripId;
        }

        public TripGeneratorSaveData ToSaveData()
        {
            var ids = new List<EntityId>(_failedRouteCooldowns.Keys);
            ids.Sort();
            var retries = new TripRetrySaveData[ids.Count];
            for (var i = 0; i < ids.Count; i++)
                retries[i] = new TripRetrySaveData { personId = ids[i].Value, retryAfterTick = _failedRouteCooldowns[ids[i]] };
            return new TripGeneratorSaveData { version = 1, nextTripId = _nextTripId, retries = retries };
        }

        public void RestoreFromSaveData(TripGeneratorSaveData data, int legacyNextTripId)
        {
            _failedRouteCooldowns.Clear();
            _nextTripId = Math.Max(1, legacyNextTripId);
            if (data == null || data.version == 0) return;
            if (data.version != 1 || data.nextTripId <= 0)
                throw new ArgumentException("Invalid trip generator checkpoint.", nameof(data));
            _nextTripId = Math.Max(_nextTripId, data.nextTripId);
            if (data.retries == null) return;
            foreach (var retry in data.retries)
            {
                if (retry == null || retry.personId <= 0 || retry.retryAfterTick < 0)
                    throw new ArgumentException("Invalid trip retry checkpoint.", nameof(data));
                _failedRouteCooldowns.Add(new EntityId(retry.personId), retry.retryAfterTick);
            }
        }

        public void UpdateTopology(BuildingTopology topology, HierarchicalTransitGraph graph, TransitRoutePlanner planner)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _graph    = graph    ?? throw new ArgumentNullException(nameof(graph));
            _planner  = planner  ?? throw new ArgumentNullException(nameof(planner));
            _routineRoutes.Clear();
            RebuildRoomCandidates();
            RebuildStairsPlanner();
        }

        private void RebuildStairsPlanner()
        {
            if (_graph == null)
            {
                _stairsPlanner = null;
                return;
            }
            var nonElevatorEdges = new List<TransitEdge>(_graph.Edges.Count);
            foreach (var edge in _graph.Edges)
            {
                if (edge.Mode != TransitMode.Elevator) nonElevatorEdges.Add(edge);
            }
            var stairsGraph = new HierarchicalTransitGraph(_graph.Nodes, nonElevatorEdges);
            _stairsPlanner = new TransitRoutePlanner(stairsGraph);
        }

        /// <summary>
        /// Generates trips for all residents whose schedule block changed between
        /// <paramref name="previousTick"/> and <paramref name="currentTick"/> or who
        /// have an urgent physiological or social need requiring destination arbitration (ADR-047).
        /// Returns an empty list when no transitions or need overrides occur.
        /// </summary>
        public IReadOnlyList<TripRecord> GenerateTripsForTick(
            Tick previousTick,
            Tick currentTick,
            PopulationState population,
            Func<PersonRecord, bool> canGenerateTrip = null)
        {
            if (population == null) throw new ArgumentNullException(nameof(population));

            List<TripRecord> trips = null;

            foreach (var person in population.Persons)
            {
                person.EnsureRoutineDay(currentTick);
                var currentBlock = person.Schedule.ActiveBlockAt(currentTick);
                if (currentBlock?.Label == DailySchedule.LabelWork)
                    person.PrepareWorkAppointment(currentBlock.Value,
                        currentTick.Value / DailySchedule.TicksPerDay * DailySchedule.TicksPerDay, currentTick);
                var scheduledPurpose = DynamicScheduleArbitrator.ScheduledPurpose(person.Schedule.ActiveLabelAt(currentTick));
                var changed = person.Schedule.ActiveLabelAt(previousTick) != person.Schedule.ActiveLabelAt(currentTick) ||
                    DynamicScheduleArbitrator.EpisodeStartAt(person, previousTick) != DynamicScheduleArbitrator.EpisodeStartAt(person, currentTick);
                var scheduledEpisode = DynamicScheduleArbitrator.EpisodeStartAt(person, currentTick);
                var isOutsideShift = person.CurrentPurpose == ResidentPurposeKind.WorkingOutside &&
                                     person.CurrentLocation.IsOutside;
                var permitted = canGenerateTrip == null || canGenerateTrip(person);
                var outsideCommitted = !person.Schedule.IsEmploymentRoutine && isOutsideShift && !CanInterruptOutsideWork(person, currentTick);

                // Required appointments enter the cohort before any travel, commitment or retry skip.
                // A trip already serving this purpose keeps its original observer identity.
                if (scheduledPurpose == TripPurpose.Work || scheduledPurpose == TripPurpose.Home)
                {
                    var required = changed
                        ? DemandObserver?.ObserveAttempt(person.Id, scheduledPurpose.Value, scheduledEpisode, currentTick.Value) ?? 0
                        : DemandObserver?.FindOutstandingSequence(person.Id, scheduledPurpose.Value) ?? 0;
                    var requiredDestination = ResolveDestination(person, scheduledPurpose.Value);
                    var attending = requiredDestination.HasValue && person.CurrentLocation.Equals(requiredDestination.Value) &&
                                    person.CurrentActivity != ActivityKind.Commuting;
                    if (attending)
                        DemandObserver?.Complete(required, currentTick.Value);
                    else if (changed && (!permitted || outsideCommitted) &&
                             (DemandObserver == null || !DemandObserver.HasOutstandingTrip(person.Id, scheduledPurpose.Value)))
                        DemandObserver?.Reject(required, currentTick.Value, "SuppressedCommitted");
                }
                if (!permitted) continue;
                if (person.CurrentActivity == ActivityKind.Commuting) continue;
                if (person.CurrentPurpose == ResidentPurposeKind.None)
                    ResidentPurposeSystem.AdvancePerson(person, currentTick);
                if (outsideCommitted) continue;
                if (_failedRouteCooldowns.TryGetValue(person.Id, out var cooldownUntil) && currentTick.Value < cooldownUntil) continue;

                var previousLabel = person.Schedule.ActiveLabelAt(previousTick);
                var currentLabel  = person.Schedule.ActiveLabelAt(currentTick);
                var blockChanged  = previousLabel != currentLabel;

                var departure = AnticipatedWorkDeparture(person, currentTick);
                var decision = _arbitrator.Decide(person, currentTick, isBlockTransition: blockChanged,
                    anticipatedDepartureTick: departure);
                var purpose = decision.Purpose;

                if (purpose == TripPurpose.Food && person.CurrentLocation.IsOutside &&
                    person.IsOutsideFoodRetryBlocked(currentTick))
                {
                    var blocked = DemandObserver?.ObserveAttempt(person.Id, TripPurpose.Food, decision.EpisodeStartTick, currentTick.Value) ?? 0;
                    DemandObserver?.Reject(blocked, currentTick.Value, "OutsideFoodRetryBlocked");
                    ResidentPurposeSystem.AdvancePerson(person, currentTick);
                    continue;
                }

                if (decision.Kind == RoutineDecisionKind.None || !purpose.HasValue)
                {
                    ResidentPurposeSystem.AdvancePerson(person, currentTick);
                    continue;
                }

                var episode = decision.EpisodeStartTick;
                var demand = decision.IsRequired
                    ? DemandObserver?.ObserveAttempt(person.Id, purpose.Value, episode, currentTick.Value) ?? 0 : 0;
                var destination = ResolveDestination(person, purpose.Value);
                if (!destination.HasValue)
                {
                    DemandObserver?.RejectUnreachable(demand, currentTick.Value);
                    continue;
                }

                var origin = person.CurrentLocation;
                if (origin.Equals(destination.Value))
                {
                    if (purpose == TripPurpose.Food && destination.Value.IsOutside)
                    {
                        var localOutsideMeal = BuildTrip(person.Id, origin, destination.Value, purpose.Value, currentTick);
                        if (localOutsideMeal != null)
                        {
                            person.UpdateActivity(ActivityKind.Commuting);
                            if (trips == null) trips = new List<TripRecord>();
                            trips.Add(localOutsideMeal);
                            DemandObserver?.AcceptTrip(demand, localOutsideMeal.Id.Value);
                        }
                        continue;
                    }

                    DemandObserver?.Complete(demand, currentTick.Value);
                    _failedRouteCooldowns.Remove(person.Id);
                    person.UpdateActivity(TransitExecutionSystem.PurposeToActivity(purpose.Value));
                    TransitExecutionSystem.ReconcileArrivalActivity(person, currentTick);
                    if (person.Schedule.IsEmploymentRoutine) ResidentPurposeSystem.AdvancePerson(person, currentTick);
                    continue;
                }

                var trip = BuildTrip(
                    person.Id,
                    origin,
                    destination.Value,
                    purpose.Value,
                    currentTick);

                if (trip != null)
                {
                    if (purpose == TripPurpose.Work) person.RecordWorkDeparture(currentTick);
                    _failedRouteCooldowns.Remove(person.Id);
                    person.UpdateActivity(ActivityKind.Commuting);
                    if (trips == null) trips = new List<TripRecord>();
                    trips.Add(trip);
                    DemandObserver?.AcceptTrip(demand, trip.Id.Value);
                }
                else
                {
                    DemandObserver?.RejectUnreachable(demand, currentTick.Value);
                    _failedRouteCooldowns[person.Id] = currentTick.Value + 60;
                }
            }

            return trips == null ? (IReadOnlyList<TripRecord>)Array.Empty<TripRecord>() : trips;
        }

        private static bool CanInterruptOutsideWork(PersonRecord person, Tick tick)
        {
            if (person == null || person.CurrentPurpose != ResidentPurposeKind.WorkingOutside ||
                !person.CurrentLocation.IsOutside) return false;
            return tick.Value >= person.PurposeEndsAtTick || person.CurrentActivity == ActivityKind.Eating ||
                   person.GetNeedSatisfaction(NeedKind.Hunger) < DynamicScheduleArbitrator.CriticalHungerThreshold ||
                   person.GetNeedSatisfaction(NeedKind.Energy) < DynamicScheduleArbitrator.CriticalEnergyThreshold ||
                   person.GetNeedSatisfaction(NeedKind.Hygiene) < DynamicScheduleArbitrator.CriticalHygieneThreshold;
        }

        private long AnticipatedWorkDeparture(PersonRecord person, Tick tick)
        {
            if (!person.Schedule.IsEmploymentRoutine || person.CurrentLocation.Equals(person.WorkplaceLocation) ||
                person.Schedule.ActiveLabelAt(tick) == DailySchedule.LabelWork) return -1;
            ScheduleBlock? upcoming = null;
            long upcomingDay = 0, next = long.MaxValue;
            var dayBase = tick.Value / DailySchedule.TicksPerDay * DailySchedule.TicksPerDay;
            foreach (var block in person.Schedule.Blocks)
            {
                if (block.Label != DailySchedule.LabelWork || block.EpisodeStartDayTick != block.StartTick.Value) continue;
                var start = dayBase + block.EpisodeStartDayTick;
                var candidateDay = dayBase;
                if (start <= tick.Value) { start += DailySchedule.TicksPerDay; candidateDay += DailySchedule.TicksPerDay; }
                if (start < next) { next = start; upcoming = block; upcomingDay = candidateDay; }
            }
            if (!upcoming.HasValue) return -1;
            var origin = _graph.GetNodeForLocation(person.CurrentLocation);
            var destination = _graph.GetNodeForLocation(person.WorkplaceLocation);
            if (origin == null || destination == null) return -1;
            var key = (person.CurrentLocation, person.WorkplaceLocation);
            if (!_routineRoutes.TryGetValue(key, out var route))
            {
                route = Math.Abs(origin.Floor - destination.Floor) <= 2 ? _stairsPlanner?.FindRoute(origin.Id, destination.Id) : null;
                if (route == null) route = _planner.FindRoute(origin.Id, destination.Id);
                if (_routineRoutes.Count >= 2048) _routineRoutes.Clear();
                _routineRoutes[key] = route;
            }
            if (route == null) return -1;
            if (_elevatorBank != null && _elevatorBank.Cars.Count == 0)
            {
                route = _stairsPlanner?.FindRoute(origin.Id, destination.Id);
                if (route == null) return -1;
            }
            long seconds = 0;
            for (var i = 0; i < route.Legs.Count; i++)
            {
                var leg = route.Legs[i];
                if (leg.Mode != TransitMode.Elevator || _elevatorBank == null) { seconds += leg.DurationSeconds; continue; }
                _graph.TryGetNode(leg.FromNodeId, out var fromNode);
                _graph.TryGetNode(leg.ToNodeId, out var toNode);
                var from = fromNode.Floor;
                var to = toNode.Floor;
                while (i + 1 < route.Legs.Count && route.Legs[i + 1].Mode == TransitMode.Elevator)
                {
                    _graph.TryGetNode(route.Legs[++i].ToNodeId, out toNode);
                    to = toNode.Floor;
                }
                seconds += (long)_elevatorBank.EstimateArrivalTicks(from, to) * 60;
            }
            var lead = Math.Max(1, (seconds + 59) / 60);
            var departure = Math.Max(0, next - lead);
            if (tick.Value < departure) return -1;
            person.PrepareWorkAppointment(upcoming.Value, upcomingDay, tick);
            return departure;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private TripRecord BuildTrip(
            EntityId personId,
            WorldLocation origin,
            WorldLocation destination,
            TripPurpose purpose,
            Tick departureTick)
        {
            var tripId = new EntityId(_nextTripId++);
            // Route from the resident's current endpoint, including the street edge.
            var originNode = _graph.GetNodeForLocation(origin);
            var destNode   = _graph.GetNodeForLocation(destination);

            TransitRoute route = null;
            if (originNode != null && destNode != null && !origin.Equals(destination))
            {
                if (Math.Abs(originNode.Floor - destNode.Floor) <= 2 && _stairsPlanner != null)
                {
                    route = _stairsPlanner.FindRoute(originNode.Id, destNode.Id);
                }

                if (route == null)
                {
                    route = _planner.FindRoute(originNode.Id, destNode.Id);
                }
            }

            if (route == null && !origin.Equals(destination)) return null;

            return new TripRecord(
                tripId,
                personId,
                origin,
                destination,
                purpose,
                departureTick,
                route);
        }

        public TripRecord CreateMoveInTrip(PersonRecord person, Tick tick)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (!person.CurrentLocation.IsOutside) return null;
            var demand = DemandObserver?.ObserveAttempt(person.Id, TripPurpose.Home, tick.Value, tick.Value) ?? 0;
            var trip = BuildTrip(person.Id, WorldLocation.Outside, WorldLocation.InRoom(person.HomeRoomId), TripPurpose.Home, tick);
            if (trip == null) DemandObserver?.RejectUnreachable(demand, tick.Value);
            else DemandObserver?.AcceptTrip(demand, trip.Id.Value);
            return trip;
        }

        /// <summary>Creates a one-way trip from the resident's current location to the outside city.</summary>
        public TripRecord CreateMoveOutTrip(PersonRecord person, Tick tick)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (person.CurrentLocation.IsOutside) return null;
            return BuildTrip(person.Id, person.CurrentLocation, WorldLocation.Outside, TripPurpose.MoveOut, tick);
        }

        private WorldLocation? ResolveDestination(PersonRecord person, TripPurpose purpose)
        {
            switch (purpose)
            {
                case TripPurpose.Work:
                    return person.WorkplaceLocation;

                case TripPurpose.Home:
                case TripPurpose.Hygiene:
                    return WorldLocation.InRoom(person.HomeRoomId);

                case TripPurpose.Food:
                    // Distribute diners deterministically across matching commercial rooms,
                    // prioritizing the nearest reachable diner to the resident's current floor.
                    if (person.WorkplaceLocation.IsOutside && person.CurrentLocation.IsOutside) return WorldLocation.Outside;
                    var foodFloor = GetPersonCurrentFloor(person);
                    var diner = FindRoomByContent(FiveFloorTopologyFixture.CommercialContentId, floorHint: foodFloor, personId: person.Id);
                    return diner.HasValue ? WorldLocation.InRoom(diner.Value) : WorldLocation.Outside;

                case TripPurpose.Leisure:
                    // Distribute across lobbies, prioritizing nearest floor.
                    var leisureFloor = GetPersonCurrentFloor(person);
                    return AsLocation(FindRoomByContent(FiveFloorTopologyFixture.LobbyContentId, floorHint: leisureFloor, personId: person.Id));

                default:
                    return null;
            }
        }

        private int GetPersonCurrentFloor(PersonRecord person)
        {
            if (person == null || person.CurrentLocation.IsOutside) return 0;
            return _topology.TryGetRoom(person.CurrentRoomId, out var room) ? room.Floor : 0;
        }

        private static WorldLocation? AsLocation(EntityId? roomId) => roomId.HasValue ? WorldLocation.InRoom(roomId.Value) : (WorldLocation?)null;

        private void RebuildRoomCandidates()
        {
            var foodRooms = _topology.GetRoomsByContentPrefixes("commercial:diner", "room:diner");
            _foodRooms = foodRooms.ToArray();
            _lobbyRooms = ToArray(_topology.GetRoomsByContentType(FiveFloorTopologyFixture.LobbyContentId));
        }

        private EntityId? FindRoomByContent(ContentId contentId, int floorHint, EntityId personId)
        {
            var rooms = contentId == FiveFloorTopologyFixture.CommercialContentId ? _foodRooms : _lobbyRooms;
            if (rooms == null || rooms.Length == 0) return null;

            // Preference 1: Closest candidate rooms by floor delta
            var minDelta = int.MaxValue;
            for (var i = 0; i < rooms.Length; i++)
            {
                var delta = Math.Abs(rooms[i].Floor - floorHint);
                if (delta < minDelta) minDelta = delta;
            }

            var closestCount = 0;
            for (var i = 0; i < rooms.Length; i++)
            {
                if (Math.Abs(rooms[i].Floor - floorHint) == minDelta) closestCount++;
            }

            if (closestCount > 0)
            {
                var targetIndex = Math.Abs(personId.Value % closestCount);
                var currentIndex = 0;
                for (var i = 0; i < rooms.Length; i++)
                {
                    if (Math.Abs(rooms[i].Floor - floorHint) == minDelta)
                    {
                        if (currentIndex == targetIndex) return rooms[i].Id;
                        currentIndex++;
                    }
                }
            }

            // Preference 2: Deterministic distribution across all candidate rooms
            var index = Math.Abs(personId.Value % rooms.Length);
            return rooms[index].Id;
        }

        private static Room[] ToArray(IReadOnlyList<Room> rooms)
        {
            if (rooms is Room[] roomArray) return roomArray;
            var copy = new Room[rooms.Count];
            for (var i = 0; i < rooms.Count; i++) copy[i] = rooms[i];
            return copy;
        }
    }
}
