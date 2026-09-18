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
        /// <paramref name="previousTick"/> and <paramref name="currentTick"/>.
        /// Returns an empty list when no transitions occur (e.g. mid-block ticks).
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

                // No transition — skip.
                if (previousLabel == currentLabel) continue;

                var purpose = LabelToPurpose(currentLabel);
                if (purpose == null) continue;

                var destinationRoomId = ResolveDestinationRoom(person, purpose.Value);
                if (!destinationRoomId.HasValue) continue;

                var originRoomId = person.CurrentRoomId.Value > 0 ? person.CurrentRoomId : person.HomeRoomId;
                if (originRoomId.Equals(destinationRoomId.Value))
                {
                    person.UpdateActivity(TransitExecutionSystem.PurposeToActivity(purpose.Value));
                    continue;
                }

                var trip = BuildTrip(
                    person.Id,
                    originRoomId,
                    destinationRoomId.Value,
                    purpose.Value,
                    currentTick);

                if (trip != null)
                {
                    trips.Add(trip);
                }
            }

            return trips;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private TripRecord BuildTrip(
            EntityId personId,
            EntityId originRoomId,
            EntityId destinationRoomId,
            TripPurpose purpose,
            Tick departureTick)
        {
            var tripId = new EntityId(_nextTripId++);

            // For Work trips the origin is home; for Food trips origin is workplace;
            // for Leisure trips origin is food room; for Home trips origin is leisure room.
            // For first-playable we use the person's home room as the tracked origin for all
            // outbound trips and workplace for return legs — the route planner handles cost.
            var originNode = _graph.GetPortalNodeForRoom(originRoomId);
            var destNode   = _graph.GetPortalNodeForRoom(destinationRoomId);

            TransitRoute route = null;
            if (originNode != null && destNode != null && !originRoomId.Equals(destinationRoomId))
            {
                route = _planner.FindRoute(originNode.Id, destNode.Id);
            }

            return new TripRecord(
                tripId,
                personId,
                originRoomId,
                destinationRoomId,
                purpose,
                departureTick,
                route);
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

        private EntityId? ResolveDestinationRoom(PersonRecord person, TripPurpose purpose)
        {
            switch (purpose)
            {
                case TripPurpose.Work:
                    return person.WorkplaceRoomId;

                case TripPurpose.Home:
                    return person.HomeRoomId;

                case TripPurpose.Food:
                    // Find the first commercial room on Floor 0 in the building.
                    return FindRoomByContent(FiveFloorTopologyFixture.CommercialContentId, floorHint: 0);

                case TripPurpose.Leisure:
                    // Find the lobby on Floor 0.
                    return FindRoomByContent(FiveFloorTopologyFixture.LobbyContentId, floorHint: 0);

                default:
                    return null;
            }
        }

        private EntityId? FindRoomByContent(ContentId contentId, int floorHint)
        {
            var isCommercialFood = contentId == FiveFloorTopologyFixture.CommercialContentId;

            if (_topology.TryGetFloor(floorHint, out var floor))
            {
                foreach (var room in floor.Rooms)
                {
                    if (room.ContentType == contentId ||
                        (isCommercialFood && (room.ContentType.Value.StartsWith("commercial:") || room.ContentType.Value.StartsWith("room:diner"))))
                    {
                        return room.Id;
                    }
                }
            }

            // Fallback: search all floors.
            foreach (var f in _topology.Floors)
            {
                foreach (var room in f.Rooms)
                {
                    if (room.ContentType == contentId ||
                        (isCommercialFood && (room.ContentType.Value.StartsWith("commercial:") || room.ContentType.Value.StartsWith("room:diner"))))
                    {
                        return room.Id;
                    }
                }
            }

            return null;
        }
    }
}
