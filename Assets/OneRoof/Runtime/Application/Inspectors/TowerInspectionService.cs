using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Application.Overlays;
using OneRoof.Application.Social;
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
            var dailyNet = household.DailyBudgetNetFlow;
            var details = new List<string>
            {
                $"Activity: {person.Activity}",
                $"Purpose: {(resident.HasValue ? resident.Value.PurposeLabel ?? "Unassigned" : "Unassigned")}",
                $"Household: #{person.HouseholdId}",
                $"Home: room #{person.HomeRoomId}",
                person.WorksOutside ? "Workplace: Outside" : $"Workplace: room #{person.WorkplaceRoomId}",
                person.IsOutside ? "Current location: Outside" : "Current location: tower",
                person.Role == SpecialistRole.None
                    ? (person.IsTraining ? $"Training: {person.TrainingRole} ({person.TrainingProgress:P0})" : "Specialist role: none yet")
                    : $"Specialist role: {person.Role}",
                $"Household cash: ${household.CashBalance:N0}",
                $"Net cash after outside credit: ${household.NetCashPosition:N0}",
                $"Position after rent arrears: ${household.NetFinancialPosition:N0}",
                $"Daily income: ${household.DailyIncome:N0}",
                $"Daily rent due / paid: ${household.DailyRentDue:N0} / ${household.DailyRentPaid:N0}",
                $"Tower food and service spend: ${household.DailyServiceSpend:N0}",
                $"Outside essentials / quality / care: ${household.DailyOutsideEssentialSpend:N0} / ${household.DailyOutsideQualitySpend:N0} / ${household.DailyCareSpend:N0}",
                $"Outside credit outstanding: ${household.OutsideCreditBalance:N0}; repaid today: ${household.DailyCreditRepayment:N0}",
                $"Budget flow: ${dailyNet:+#,0;-#,0;0}; trailing 7/30 days: ${household.Rolling7DayNetFlow:+#,0;-#,0;0} / ${household.Rolling30DayNetFlow:+#,0;-#,0;0}",
                $"Unmet essentials today: ${household.DailyEssentialShortfall:N0}; accumulated exposure: ${household.UnderprovisionExposure:N0}",
                $"Rent arrears: ${household.RentArrearsBalance:N0} for {household.RentArrearsDays} day(s)"
            };
            if (household.RentArrearsDays > 30) details.Add("Prolonged rent arrears are causing household stress.");
            var housing = _session.HouseholdHousing(household.Id);
            if (housing.HasValue)
            {
                details.Add($"Home room condition: {housing.Value.RoomCondition}");
                details.Add($"Lease phase: {housing.Value.LeasePhase}");
                if (housing.Value.HasNotice)
                    details.Add($"Move-out notice started on day {housing.Value.NoticeStartedDay}; eligible: {(housing.Value.IsMoveOutEligible ? "yes" : "no")}");
            }
            foreach (var need in person.Needs) details.Add($"{need.Kind}: {need.Satisfaction:P0}");
            foreach (var trait in person.Traits) details.Add($"Trait: {trait.Kind}");
            details.Add($"Satisfaction: {person.Satisfaction:P0}");
            details.Add($"Strain: {person.Strain:P0}");
            details.Add($"Commute quality: {person.Commute:P0}");
            details.Add($"Rent burden: {person.RentBurden:P0}");
            foreach (var facet in person.PersonalityFacets) details.Add($"Personality: {facet.Kind}");
            foreach (var grievance in person.Grievances) details.Add($"Grievance: {grievance}");
            var social = FactionProjectionService.Capture(_session.Simulation);
            foreach (var support in social.ResidentSupports)
                if (support.ResidentId == residentId)
                    details.Add($"Faction {support.FactionId}: {support.Support:P0} support{(support.IsMember ? " (member)" : "")}; driver: {support.Driver}.");

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
            var tenant = _session.FindHouseholdByHomeRoom(room.Id);
            var housing = tenant == null ? (HouseholdHousingLifecycleProjection?)null : _session.HouseholdHousing(tenant.Id);
            if (housing.HasValue)
            {
                details.Add($"Housing condition: {housing.Value.RoomCondition}");
                details.Add($"Lease phase: {housing.Value.LeasePhase}; rent arrears: {housing.Value.RentArrearsDays} day(s)");
            }
            var symptom = occupants > room.Capacity
                ? "Room occupancy exceeds its authored capacity."
                : housing.HasValue && housing.Value.RoomCondition != HousingConditionStage.Maintained
                    ? $"Room is under deferred upkeep while the tenant has {housing.Value.RentArrearsDays} day(s) of rent arrears."
                    : $"Room is operating at {occupants} of {room.Capacity} capacity.";
            return new InspectorDetailProjection($"Room #{roomId}", symptom, details, "Use Build mode to change capacity or add supporting rooms.");
        }

        public InspectorDetailProjection InspectFactionFloor(int floor)
        {
            var overlay = new TowerDataOverlays(_session).FactionTension;
            foreach (var region in overlay.Floors)
            {
                if (region.Floor != floor) continue;
                var details = new List<string>
                {
                    $"Support signals: {region.SupporterCount}",
                    $"Average support pressure: {region.AveragePressure:P0} ({region.Tier})",
                    $"Leading faction: {region.LeadingFactionName}",
                    $"Top grievance: {region.TopGrievance}",
                    $"Trend: {region.Trend}"
                };
                return new InspectorDetailProjection($"Floor {floor} Faction Tension",
                    region.SupporterCount == 0 ? "No faction support is recorded on this floor." : region.AccessibilityLabel,
                    details,
                    "Review affected households, transit, services, and the decree panel; then compare the next daily settlement.");
            }
            return null;
        }

        public InspectorDetailProjection InspectBusiness(int businessId)
        {
            foreach (var business in _session.Simulation.Businesses.Businesses)
            {
                if (business.Id.Value != businessId) continue;
                var details = new List<string>
                {
                    $"Room: #{business.RoomId.Value}; type: {business.ContentType}",
                    $"Staff: {business.EmployeeIds.Count}",
                    $"Cash: ${business.CashBalance:N0}",
                    $"Last revenue: ${business.LastCustomerRevenue + business.LastContractRevenue:N0}",
                    $"Last wages: ${business.LastWages:N0}; rent: ${business.LastRentPaid:N0}; tax: ${business.LastTaxPaid:N0}",
                    $"Arrears: {business.ArrearsDays} day(s); wage arrears: {(business.WageArrears ? "yes" : "no")}",
                    $"Insolvent: {(business.IsInsolvent ? "yes" : "no")}; re-lease eligible: {(business.IsVacantForReLease ? "yes" : "no")}."
                };
                return new InspectorDetailProjection($"Business #{businessId}",
                    business.IsInsolvent ? "This business cannot cover its obligations." : "This business is operating.",
                    details, "Review foot traffic, commercial rent, tax, staffing capacity, and service access through tower-level controls.");
            }
            return new InspectorDetailProjection($"Business #{businessId}", "This business has closed or moved out.",
                new[] { "Its dated decision record remains available as historical evidence." }, "Review current business demand and leasing in Manage mode.");
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
