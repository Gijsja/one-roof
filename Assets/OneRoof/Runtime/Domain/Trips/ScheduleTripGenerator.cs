using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
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

        private readonly DynamicScheduleArbitrator _arbitrator = new DynamicScheduleArbitrator();
        private int _nextTripId;

        /// <param name="topology">The building topology for room lookups.</param>
        /// <param name="graph">Pre-built transit graph for portal node lookups.</param>
        /// <param name="planner">Route planner for computing planned routes.</param>
        /// <param name="firstTripId">The starting integer for new trip entity IDs.</param>
        public ScheduleTripGenerator(
            BuildingTopology topology,
            HierarchicalTransitGraph graph,
            TransitRoutePlanner planner,
            int firstTripId = 1)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _graph    = graph    ?? throw new ArgumentNullException(nameof(graph));
            _planner  = planner  ?? throw new ArgumentNullException(nameof(planner));

            if (firstTripId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstTripId), firstTripId, "First trip ID must be positive.");
            }

            _nextTripId = firstTripId;
        }

        public void UpdateTopology(BuildingTopology topology, HierarchicalTransitGraph graph, TransitRoutePlanner planner)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _graph    = graph    ?? throw new ArgumentNullException(nameof(graph));
            _planner  = planner  ?? throw new ArgumentNullException(nameof(planner));
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
            PopulationState population)
        {
            if (population == null) throw new ArgumentNullException(nameof(population));

            var trips = new List<TripRecord>();

            foreach (var person in population.Persons)
            {
                if (person.CurrentActivity == ActivityKind.Commuting) continue;

                var previousLabel = person.Schedule.ActiveLabelAt(previousTick);
                var currentLabel  = person.Schedule.ActiveLabelAt(currentTick);
                var blockChanged  = previousLabel != currentLabel;

                TripPurpose? purpose = blockChanged
                    ? (_arbitrator.ArbitrateDestination(person, currentTick, isBlockTransition: true) ?? LabelToPurpose(currentLabel))
                    : _arbitrator.ArbitrateDestination(person, currentTick, isBlockTransition: false);

                if (purpose == null) continue;

                var destination = ResolveDestination(person, purpose.Value);
                if (!destination.HasValue) continue;

                var origin = person.CurrentLocation;
                if (origin.Equals(destination.Value))
                {
                    person.UpdateActivity(TransitExecutionSystem.PurposeToActivity(purpose.Value));
                    TransitExecutionSystem.ReconcileArrivalActivity(person, currentTick);
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
                    person.UpdateActivity(ActivityKind.Commuting);
                    trips.Add(trip);
                }
            }

            return trips;
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
                route = _planner.FindRoute(originNode.Id, destNode.Id);
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
            return BuildTrip(person.Id, WorldLocation.Outside, WorldLocation.InRoom(person.HomeRoomId), TripPurpose.Home, tick);
        }

        private static TripPurpose? LabelToPurpose(string label)
        {
            if (label == null) return null;
            if (label == DailySchedule.LabelWork)    return TripPurpose.Work;
            if (label == DailySchedule.LabelEat)     return TripPurpose.Food;
            if (label == DailySchedule.LabelLeisure) return TripPurpose.Leisure;
            if (label == DailySchedule.LabelSleep)   return TripPurpose.Home;
            return null;
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
                    // Distribute diners deterministically across all matching commercial rooms
                    // so a second diner actually relieves 24/7 meal demand.
                    return AsLocation(FindRoomByContent(FiveFloorTopologyFixture.CommercialContentId, floorHint: 0, personId: person.Id));

                case TripPurpose.Leisure:
                    // Distribute across all lobbies the same way.
                    return AsLocation(FindRoomByContent(FiveFloorTopologyFixture.LobbyContentId, floorHint: 0, personId: person.Id));

                default:
                    return null;
            }
        }

        private static WorldLocation? AsLocation(EntityId? roomId) => roomId.HasValue ? WorldLocation.InRoom(roomId.Value) : (WorldLocation?)null;

        private EntityId? FindRoomByContent(ContentId contentId, int floorHint, EntityId personId)
        {
            var isCommercialFood = contentId == FiveFloorTopologyFixture.CommercialContentId;
            var matches = new List<EntityId>();

            if (_topology.TryGetFloor(floorHint, out var floor))
            {
                foreach (var room in floor.Rooms)
                {
                    if (MatchesFoodOrLobby(room.ContentType, contentId, isCommercialFood))
                    {
                        matches.Add(room.Id);
                    }
                }
            }

            // Fallback: search all floors (skipping the hint floor to avoid duplicates).
            foreach (var f in _topology.Floors)
            {
                if (f.FloorLevel == floorHint) continue;
                foreach (var room in f.Rooms)
                {
                    if (MatchesFoodOrLobby(room.ContentType, contentId, isCommercialFood))
                    {
                        matches.Add(room.Id);
                    }
                }
            }

            if (matches.Count == 0) return null;
            if (matches.Count == 1) return matches[0];

            // Deterministic spread: stable person ID selects the room, so demand splits
            // across diners/lobbies without any per-generator state (ADR-020).
            var index = Math.Abs(personId.Value % matches.Count);
            return matches[(int)index];
        }

        private static bool MatchesFoodOrLobby(ContentId roomContent, ContentId wanted, bool isCommercialFood)
        {
            if (roomContent == wanted) return true;
            return isCommercialFood &&
                (roomContent.Value.StartsWith("commercial:") || roomContent.Value.StartsWith("room:diner"));
        }
    }
}
