using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;

namespace OneRoof.Application.Inspectors
{
    /// <summary>Builds immutable drill-down cards from the authoritative tower simulation.</summary>
    public sealed class TowerInspectionService
    {
        private readonly TowerSimulationSession _session;

        public TowerInspectionService(TowerSimulationSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public InspectorDetailProjection InspectResident(int residentId)
        {
            var personId = new EntityId(residentId);
            if (!_session.Population.TryGetPerson(personId, out var person)) return null;

            var transit = _session.TransitProjection();
            var resident = FindResident(transit, residentId);
            var details = new List<string>
            {
                $"Activity: {person.CurrentActivity}",
                $"Household: #{person.HouseholdId.Value}",
                $"Home: room #{person.HomeRoomId.Value}",
                $"Workplace: room #{person.WorkplaceRoomId.Value}"
            };
            foreach (var need in person.Needs) details.Add($"{need.Kind}: {need.Satisfaction:P0}");
            foreach (var trait in person.Traits) details.Add($"Trait: {trait.Kind}");
            details.Add($"Satisfaction: {person.Wellbeing.Satisfaction:P0}");
            details.Add($"Strain: {person.Wellbeing.Strain:P0}");
            details.Add($"Commute quality: {person.Wellbeing.Commute:P0}");
            details.Add($"Rent burden: {person.Wellbeing.RentBurden:P0}");
            foreach (var facet in person.PersonalityFacets) details.Add($"Personality: {facet.Kind}");
            foreach (var grievance in person.Wellbeing.Grievances) details.Add($"Grievance: {grievance}");

            var symptom = !resident.HasValue
                ? "Resident location is not currently available."
                : $"{DescribeStatus(resident.Value.Status)} on floor {resident.Value.Floor}.";
            if (resident.HasValue && resident.Value.WaitTicks > 0) details.Add($"Elevator wait: {resident.Value.WaitTicks} ticks");

            return new InspectorDetailProjection(
                $"Resident #{residentId}", symptom, details,
                person.Wellbeing.Grievances.Count > 0 ? "Open the Satisfaction overlay, then respond through transit capacity, services, or leasing." : "Observe needs and activity before changing tower systems.");
        }

        public InspectorDetailProjection InspectRoom(int roomId)
        {
            if (!_session.Topology.TryGetRoom(new EntityId(roomId), out var room)) return null;
            var occupants = 0;
            foreach (var person in _session.Population.Persons)
                if (person.CurrentRoomId.Value == roomId) occupants++;

            var details = new List<string>
            {
                $"Type: {room.ContentType}",
                $"Floor: {room.Floor}",
                $"Footprint: cells {room.Bounds.MinX}–{room.Bounds.MaxX}",
                $"Occupancy: {occupants} / {room.Capacity}",
                $"Portals: {room.PortalIds.Count}",
                $"Interaction points: {room.InteractionPoints.Count}"
            };
            var symptom = occupants > room.Capacity
                ? "Room occupancy exceeds its authored capacity."
                : $"Room is operating at {occupants} of {room.Capacity} capacity.";
            return new InspectorDetailProjection($"Room #{roomId}", symptom, details, "Use Build mode to change capacity or add supporting rooms.");
        }

        public InspectorDetailProjection InspectElevatorBank()
        {
            var snapshot = _session.ElevatorBank.Snapshot();
            var details = new List<string>
            {
                $"Cars in service: {snapshot.Cars.Count}",
                $"Queued residents: {snapshot.TotalQueuedCount}",
                $"Average wait: {snapshot.AverageWaitTicks:F1} ticks"
            };
            for (var floor = snapshot.MinFloor; floor <= snapshot.MaxFloor; floor++)
                details.Add($"Floor {floor} queue: {snapshot.GetQueueLength(floor)}");
            foreach (var car in snapshot.Cars)
                details.Add($"Car #{car.Id.Value}: floor {car.CurrentFloor}, {car.Passengers.Count}/{car.Capacity}, {car.Phase}");

            var symptom = snapshot.TotalQueuedCount > 0
                ? $"{snapshot.TotalQueuedCount} resident(s) are waiting for elevator service."
                : "No residents are currently waiting for elevator service.";
            return new InspectorDetailProjection("Elevator Bank", symptom, details, "Open Build mode to add elevator capacity, then compare the wait-time overlay.");
        }

        private static TransitResidentProjection? FindResident(TowerProjection projection, int residentId)
        {
            foreach (var resident in projection.Residents)
                if (resident.ResidentId == residentId) return resident;
            return null;
        }

        private static string DescribeStatus(TransitResidentStatus status)
        {
            switch (status)
            {
                case TransitResidentStatus.Queued: return "Waiting for an elevator";
                case TransitResidentStatus.Riding: return "Riding an elevator";
                case TransitResidentStatus.Walking: return "Walking through the tower";
                case TransitResidentStatus.InRoom: return "Inside an assigned room";
                default: return "At their destination";
            }
        }
    }
}
