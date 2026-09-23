using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Application.Overlays;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;

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
            if (!_session.TryGetResidentInspection(personId, out var person)) return null;

            var transit = _session.TransitProjection();
            var resident = FindResident(transit, residentId);
            var householdId = new EntityId(person.HouseholdId);
            var household = _session.GetHousehold(householdId);
            var dailyRent = _session.DailyResidentialRent(householdId);
            var dailyNet = household.DailyIncome - dailyRent - household.DailyServiceSpend;
            var details = new List<string>
            {
                $"Activity: {person.Activity}",
                $"Household: #{person.HouseholdId}",
                $"Home: room #{person.HomeRoomId}",
                person.WorksOutside ? "Workplace: Outside" : $"Workplace: room #{person.WorkplaceRoomId}",
                person.IsOutside ? "Current location: Outside" : "Current location: tower",
                person.Role == SpecialistRole.None
                    ? (person.IsTraining ? $"Training: {person.TrainingRole} ({person.TrainingProgress:P0})" : "Specialist role: none yet")
                    : $"Specialist role: {person.Role}",
                $"Household cash: ${household.CashBalance:N0}",
                $"Daily income: ${household.DailyIncome:N0}",
                $"Daily rent due: ${dailyRent:N0}",
                $"Food and service spend: ${household.DailyServiceSpend:N0}",
                $"Daily cash change: ${dailyNet:+#,0;-#,0;0}",
                $"Rent arrears: {household.ArrearsDays} day(s)"
            };
            foreach (var need in person.Needs) details.Add($"{need.Kind}: {need.Satisfaction:P0}");
            foreach (var trait in person.Traits) details.Add($"Trait: {trait.Kind}");
            details.Add($"Satisfaction: {person.Satisfaction:P0}");
            details.Add($"Strain: {person.Strain:P0}");
            details.Add($"Commute quality: {person.Commute:P0}");
            details.Add($"Rent burden: {person.RentBurden:P0}");
            foreach (var facet in person.PersonalityFacets) details.Add($"Personality: {facet.Kind}");
            foreach (var grievance in person.Grievances) details.Add($"Grievance: {grievance}");

            var symptom = !resident.HasValue
                ? "Resident location is not currently available."
                : $"{DescribeStatus(resident.Value.Status)} on floor {resident.Value.Floor}.";
            if (resident.HasValue && resident.Value.WaitTicks > 0) details.Add($"Elevator wait: {resident.Value.WaitTicks} ticks");

            return new InspectorDetailProjection(
                $"Resident #{residentId}", symptom, details,
                person.Grievances.Count > 0 ? "Open the Satisfaction overlay, then respond through transit capacity, services, or leasing." : "Observe needs and activity before changing tower systems.");
        }

        public InspectorDetailProjection InspectRoom(int roomId)
        {
            if (!_session.TryGetRoom(new EntityId(roomId), out var room)) return null;
            var occupants = _session.CountRoomOccupants(room.Id);

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
            var snapshot = _session.ElevatorSnapshot();
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

        /// <summary>Provides the overlay's floor-level demographic evidence as an immutable cause-chain card.</summary>
        public InspectorDetailProjection InspectPopulationFloor(int floor)
        {
            var overlay = new TowerDataOverlays(_session).Population;
            if (!overlay.TryGetFloor(floor, out var population)) return null;
            var details = new List<string>
            {
                $"Residents present: {population.ResidentCount} / {population.Capacity} capacity",
                $"Density: {population.Density:P0} ({population.DensityTier})",
                $"Age 18–29: {population.YoungAdultCount}; 30–49: {population.AdultCount}; 50+: {population.OlderAdultCount}",
                $"Household resources — limited: {population.LimitedResourceCount}; stable: {population.StableResourceCount}; comfortable: {population.ComfortableResourceCount}"
            };
            var symptom = population.DensityTier == PopulationDensityTier.Dense
                ? "This floor is densely occupied and may need more shared capacity or circulation space."
                : "This floor's population distribution is within its current space capacity.";
            return new InspectorDetailProjection($"Floor {floor} Population", symptom, details, "Use Build, leasing, and service decisions to change capacity and household conditions; residents remain autonomous.");
        }

        public InspectorDetailProjection InspectScrutiny()
        {
            var overlay = new TowerDataOverlays(_session).Scrutiny;
            var details = new List<string>
            {
                $"Current scrutiny: {overlay.Value:P0}",
                $"Trend: {overlay.Trend}",
                $"Inspection-event pressure: {overlay.ExternalEventPressure:P0} [{overlay.EventPressureBand}]",
                "Expansion status: Available (scrutiny modulates event pressure; it never blocks construction directly)"
            };
            foreach (var cause in overlay.ContributingFactors) details.Add($"Driver: {cause}");
            var symptom = overlay.ExternalEventPressure >= .80f
                ? "External attention is elevated; inspection events are more likely."
                : "External attention is being monitored; expansion remains available.";
            return new InspectorDetailProjection("Tower Scrutiny", symptom, details, "Respond through capacity and service investment and balanced household conditions; high scrutiny raises event pressure rather than vetoing builds.");
        }

        public InspectorDetailProjection InspectUtilities()
        {
            var overlay = new TowerDataOverlays(_session).Utilities;
            var details = new List<string> { $"Failed equipment: {overlay.FailedEquipmentCount}" };
            foreach (var floor in overlay.Floors) if (floor.HasDisruption) details.Add(floor.AccessibilityLabel);
            foreach (var item in overlay.Equipment) if (item.IsFailed) details.Add($"Failed: {item.ContentId} in room #{item.RoomId} on floor {item.Floor} ({item.Condition:P0} condition).");
            var symptom = overlay.FailedEquipmentCount > 0
                ? "Utility equipment has failed and is disrupting service on the listed floors."
                : "Utility network connections and equipment condition are currently stable.";
            return new InspectorDetailProjection("Tower Utilities", symptom, details, "Build connected utility capacity and maintenance training space; technicians respond autonomously when equipment fails.");
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
