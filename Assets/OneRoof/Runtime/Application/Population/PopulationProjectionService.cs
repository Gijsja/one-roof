using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Application.Population
{
    /// <summary>
    /// Stateless service that projects Domain population records and active trips into
    /// read-only <see cref="NpcProjection"/> instances for the presentation layer.
    /// </summary>
    public sealed class PopulationProjectionService
    {
        private readonly BuildingTopology _topology;

        public PopulationProjectionService(BuildingTopology topology)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        /// <summary>
        /// Projects all persons in <paramref name="population"/> to read-only NPC projections.
        /// </summary>
        public IReadOnlyList<NpcProjection> ProjectAll(
            PopulationState population,
            IReadOnlyList<TripRecord> activeTrips = null)
        {
            if (population == null)
            {
                throw new ArgumentNullException(nameof(population));
            }

            var tripByPerson = new Dictionary<EntityId, TripRecord>();
            if (activeTrips != null)
            {
                foreach (var trip in activeTrips)
                {
                    if (trip != null && trip.State == TripState.InProgress)
                    {
                        tripByPerson[trip.PersonId] = trip;
                    }
                }
            }

            var projections = new List<NpcProjection>(population.Persons.Count);
            for (var i = 0; i < population.Persons.Count; i++)
            {
                var person = population.Persons[i];
                projections.Add(ProjectPerson(person, tripByPerson, i));
            }

            return projections;
        }

        private NpcProjection ProjectPerson(
            PersonRecord person,
            Dictionary<EntityId, TripRecord> activeTrips,
            int residentIndex)
        {
            var hasActiveTrip = activeTrips.TryGetValue(person.Id, out var trip);

            EntityId currentRoomId;
            int floor;
            bool isInTransit;
            int? destFloor = null;
            int? destRoomId = null;
            int waitTicks = 0;

            if (hasActiveTrip)
            {
                isInTransit = true;
                currentRoomId = trip.OriginRoomId;
                waitTicks = trip.WaitTicks;
                destRoomId = trip.DestinationRoomId.Value;

                if (_topology.TryGetRoom(trip.DestinationRoomId, out var destRoom))
                {
                    destFloor = destRoom.Floor;
                }

                floor = _topology.TryGetRoom(currentRoomId, out var origRoom) ? origRoom.Floor : 0;
            }
            else
            {
                isInTransit = false;
                currentRoomId = ResolveCurrentRoom(person);
                floor = _topology.TryGetRoom(currentRoomId, out var room) ? room.Floor : 0;
            }

            // Compute a stable horizontal offset within room so multiple residents don't stack exactly on top
            var horizontalPos = ComputeHorizontalPosition(currentRoomId, residentIndex);

            return new NpcProjection(
                person.Id.Value,
                person.HouseholdId.Value,
                floor,
                currentRoomId.Value,
                person.CurrentActivity,
                isInTransit,
                destFloor,
                destRoomId,
                waitTicks,
                horizontalPos);
        }

        private EntityId ResolveCurrentRoom(PersonRecord person)
        {
            switch (person.CurrentActivity)
            {
                case ActivityKind.Working:
                    return person.WorkplaceRoomId;
                case ActivityKind.Sleeping:
                case ActivityKind.Idle:
                default:
                    return person.HomeRoomId;
            }
        }

        private float ComputeHorizontalPosition(EntityId roomId, int residentIndex)
        {
            if (_topology.TryGetRoom(roomId, out var room))
            {
                var width = room.Bounds.Width;
                var minX = room.Bounds.MinX;
                // Distributed offset inside the room cell bounds
                var slot = (residentIndex % 5) * 0.18f;
                return minX + 0.3f + slot;
            }

            return 0f;
        }
    }
}
