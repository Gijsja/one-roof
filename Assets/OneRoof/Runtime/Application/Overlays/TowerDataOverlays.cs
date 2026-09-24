using System;
using System.Collections.Generic;
using OneRoof.Application.Economy;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Overlays
{
    /// <summary>One read-only seam for the Tower's diagnostic overlays and their cause chains.</summary>
    public sealed class TowerDataOverlays
    {
        private readonly TowerSimulationSession _session;
        private long _cachedTick = long.MinValue;
        private long _cachedVersion = long.MinValue;
        private ElevatorWaitOverlayProjection _cachedWait;
        private SatisfactionOverlayProjection _cachedSatisfaction;
        private PopulationOverlayProjection _cachedPopulation;
        private ScrutinyOverlayProjection _cachedScrutiny;
        private FootTrafficOverlayProjection _cachedFootTraffic;
        private BusinessHealthOverlayProjection _cachedBusinessHealth;
        private UtilitiesOverlayProjection _cachedUtilities;

        public TowerDataOverlays(TowerSimulationSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public TreasuryFlowProjection TreasuryFlow => _session.TreasuryFlow;

        public ElevatorWaitOverlayProjection ElevatorWait
        {
            get
            {
                EnsureCurrentState();
                if (_cachedWait == null) _cachedWait = ProjectElevatorWait(_session.CongestionProjection());
                return _cachedWait;
            }
        }
        public SatisfactionOverlayProjection Satisfaction
        {
            get
            {
                EnsureCurrentState();
                if (_cachedSatisfaction == null) _cachedSatisfaction = CreateSatisfaction();
                return _cachedSatisfaction;
            }
        }
        public PopulationOverlayProjection Population
        {
            get
            {
                EnsureCurrentState();
                if (_cachedPopulation == null) _cachedPopulation = CreatePopulation();
                return _cachedPopulation;
            }
        }
        public ScrutinyOverlayProjection Scrutiny
        {
            get
            {
                EnsureCurrentState();
                if (_cachedScrutiny == null) _cachedScrutiny = CreateScrutiny();
                return _cachedScrutiny;
            }
        }
        public FootTrafficOverlayProjection FootTraffic
        {
            get
            {
                EnsureCurrentState();
                if (_cachedFootTraffic == null) _cachedFootTraffic = CreateFootTraffic();
                return _cachedFootTraffic;
            }
        }
        public BusinessHealthOverlayProjection BusinessHealth
        {
            get
            {
                EnsureCurrentState();
                if (_cachedBusinessHealth == null) _cachedBusinessHealth = CreateBusinessHealth();
                return _cachedBusinessHealth;
            }
        }
        public UtilitiesOverlayProjection Utilities
        {
            get
            {
                EnsureCurrentState();
                if (_cachedUtilities == null) _cachedUtilities = CreateUtilities();
                return _cachedUtilities;
            }
        }

        private void EnsureCurrentState()
        {
            var tick = _session.CurrentTick;
            var version = _session.Version;
            if (_cachedTick == tick && _cachedVersion == version) return;

            _cachedTick = tick;
            _cachedVersion = version;
            _cachedWait = null;
            _cachedSatisfaction = null;
            _cachedPopulation = null;
            _cachedScrutiny = null;
            _cachedFootTraffic = null;
            _cachedBusinessHealth = null;
            _cachedUtilities = null;
        }

        public static ElevatorWaitOverlayProjection ProjectElevatorWait(ElevatorBankCongestionProjection congestion)
        {
            if (congestion == null)
            {
                throw new ArgumentNullException(nameof(congestion));
            }

            var floorFlows = new List<FloorWaitFlowProjection>(congestion.Floors.Count);
            FloorCongestionProjection bottleneckFloorData = default;
            var hasBottleneckFloor = false;

            for (var i = 0; i < congestion.Floors.Count; i++)
            {
                var floor = congestion.Floors[i];
                var isBottleneck = floor.FloorLevel == congestion.BottleneckFloor && floor.QueuedCount > 0;
                if (isBottleneck)
                {
                    bottleneckFloorData = floor;
                    hasBottleneckFloor = true;
                }

                var intensity = Math.Min(1.0f, floor.QueuedCount / 20.0f);
                var flowDirection = -1.0f; // leftward towards elevator lobby

                var badge = $"FL {floor.FloorLevel}: {floor.QueuedCount} QUEUED [{floor.Severity.ToString().ToUpperInvariant()}]";
                if (isBottleneck)
                {
                    badge += " • BOTTLENECK";
                }

                floorFlows.Add(new FloorWaitFlowProjection(
                    floor.FloorLevel,
                    floor.QueuedCount,
                    floor.MaxWaitTicks,
                    floor.AverageWaitTicks,
                    ToTier(floor.Severity),
                    isBottleneck,
                    flowDirection,
                    intensity,
                    badge));
            }

            var (primaryCause, contributingCauses, recommendedAction) = DiagnoseCauses(congestion, hasBottleneckFloor ? bottleneckFloorData : default);

            return new ElevatorWaitOverlayProjection(
                congestion.Tick,
                congestion.BottleneckFloor,
                ToTier(congestion.OverallSeverity),
                floorFlows,
                primaryCause,
                contributingCauses,
                recommendedAction);
        }

        public static CongestionTier ToTier(CongestionSeverity severity)
        {
            switch (severity)
            {
                case CongestionSeverity.Severe:
                    return CongestionTier.Severe;
                case CongestionSeverity.Heavy:
                    return CongestionTier.Heavy;
                case CongestionSeverity.Moderate:
                    return CongestionTier.Moderate;
                default:
                    return CongestionTier.Clear;
            }
        }

        private static (string primary, List<string> contributing, string action) DiagnoseCauses(
            ElevatorBankCongestionProjection congestion,
            FloorCongestionProjection bottleneckFloor)
        {
            var contributing = new List<string>();
            var totalCapacity = 0;
            for (var i = 0; i < congestion.Elevators.Count; i++)
            {
                totalCapacity += congestion.Elevators[i].Capacity;
            }

            string primary;
            string action;

            if (congestion.OverallSeverity >= CongestionSeverity.Severe)
            {
                primary = $"Severe queue bottleneck on Floor {congestion.BottleneckFloor}: elevator throughput is insufficient for current demand surge.";
                contributing.Add($"Queue at Floor {congestion.BottleneckFloor}: {bottleneckFloor.QueuedCount} waiting residents.");
                contributing.Add($"Elevator bank throughput: {congestion.Elevators.Count} active car(s) with total single-trip capacity {totalCapacity} passengers.");
                contributing.Add($"Average wait time: {congestion.AverageWaitTicks:F1} ticks (Max observed: {bottleneckFloor.MaxWaitTicks} ticks).");
                contributing.Add($"Total queued across building: {congestion.TotalQueued} residents.");
                action = "Add elevator car capacity in Build mode to increase passenger throughput.";
            }
            else if (congestion.OverallSeverity == CongestionSeverity.Moderate)
            {
                primary = $"Moderate elevator queue detected on Floor {congestion.BottleneckFloor}.";
                contributing.Add($"Floor {congestion.BottleneckFloor} queue length: {bottleneckFloor.QueuedCount} residents.");
                contributing.Add($"Active cars: {congestion.Elevators.Count} (Capacity {totalCapacity}).");
                contributing.Add($"Average wait: {congestion.AverageWaitTicks:F1} ticks.");
                action = "Monitor commute traffic or add capacity if morning queues persist.";
            }
            else
            {
                primary = "Transit flow is nominal; no significant elevator delays detected.";
                contributing.Add($"Elevator bank operating with {congestion.Elevators.Count} car(s).");
                contributing.Add($"All floors reporting clear queues.");
                action = "No intervention needed. Current capacity is optimal.";
            }

            return (primary, contributing, action);
        }

        private SatisfactionOverlayProjection CreateSatisfaction()
        {
            var accumulators = new SortedDictionary<int, float[]>();
            foreach (var person in _session.Persons)
            {
                if (!_session.TryGetRoom(person.CurrentRoomId, out var room)) continue;
                if (!accumulators.TryGetValue(room.Floor, out var values)) accumulators.Add(room.Floor, values = new float[4]);
                values[0] += person.Wellbeing.Satisfaction;
                values[1]++;
                values[2] += person.Wellbeing.Grievances.Count;
                values[3] += person.Wellbeing.RentBurden;
            }
            var floors = new List<SatisfactionFloorProjection>(); var total = 0f; var count = 0;
            foreach (var entry in accumulators)
            {
                var values = entry.Value; var satisfaction = values[1] == 0 ? 1f : values[0] / values[1];
                var averageRentBurden = values[1] == 0 ? 0f : values[3] / values[1];
                floors.Add(new SatisfactionFloorProjection(entry.Key, satisfaction, (int)values[1], (int)values[2], averageRentBurden));
                total += values[0]; count += (int)values[1];
            }
            return new SatisfactionOverlayProjection(count == 0 ? 1f : total / count, floors);
        }

        private PopulationOverlayProjection CreatePopulation()
        {

            var accumulators = new SortedDictionary<int, PopulationAccumulator>();
            for (var floor = 0; floor < _session.FloorCount; floor++)
            {
                var capacity = 0;
                foreach (var room in _session.GetRoomsOnFloor(floor)) capacity += room.Capacity;
                accumulators.Add(floor, new PopulationAccumulator(capacity));
            }

            var locations = new Dictionary<int, int>();
            foreach (var resident in _session.TransitProjection().Residents) locations[resident.ResidentId] = resident.Floor;
            foreach (var person in _session.Persons)
            {
                if (!locations.TryGetValue(person.Id.Value, out var floor) || !accumulators.TryGetValue(floor, out var values)) continue;
                values.ResidentCount++;
                var age = DeriveAge(person.Id.Value);
                if (age < 30) values.YoungAdultCount++;
                else if (age < 50) values.AdultCount++;
                else values.OlderAdultCount++;

                var household = _session.GetHousehold(person.HouseholdId);
                var budget = household != null ? household.Budget : 0.5f;
                if (budget < .55f) values.LimitedResourceCount++;
                else if (budget < .8f) values.StableResourceCount++;
                else values.ComfortableResourceCount++;
            }

            var floors = new List<PopulationFloorProjection>(accumulators.Count);
            foreach (var item in accumulators)
            {
                var value = item.Value;
                floors.Add(new PopulationFloorProjection(item.Key, value.ResidentCount, value.Capacity, value.YoungAdultCount, value.AdultCount, value.OlderAdultCount, value.LimitedResourceCount, value.StableResourceCount, value.ComfortableResourceCount));
            }

            return new PopulationOverlayProjection(_session.ResidentCount, floors);
        }

        private static int DeriveAge(int residentId) => 18 + (int)Math.Abs(((long)residentId * 17) % 53);

        private sealed class PopulationAccumulator
        {
            public PopulationAccumulator(int capacity) { Capacity = capacity; }
            public int Capacity { get; }
            public int ResidentCount { get; set; }
            public int YoungAdultCount { get; set; }
            public int AdultCount { get; set; }
            public int OlderAdultCount { get; set; }
            public int LimitedResourceCount { get; set; }
            public int StableResourceCount { get; set; }
            public int ComfortableResourceCount { get; set; }
        }

        private ScrutinyOverlayProjection CreateScrutiny()
        {
            var scrutiny = _session.Scrutiny;
            return new ScrutinyOverlayProjection(scrutiny.Value, scrutiny.Trend, scrutiny.ExternalEventPressure, scrutiny.ContributingFactors);
        }

        private FootTrafficOverlayProjection CreateFootTraffic()
        {
            var counts = new SortedDictionary<int, int>();
            foreach (var resident in _session.TransitProjection().Residents)
                if (resident.Status == TransitResidentStatus.Walking || resident.Status == TransitResidentStatus.Queued || resident.Status == TransitResidentStatus.Riding)
                    counts[resident.Floor] = counts.TryGetValue(resident.Floor, out var count) ? count + 1 : 1;
            var flows = new List<FootTrafficFloorProjection>();
            foreach (var entry in counts) flows.Add(new FootTrafficFloorProjection(entry.Key, entry.Value));
            return new FootTrafficOverlayProjection(flows);
        }

        private BusinessHealthOverlayProjection CreateBusinessHealth()
        {
            var floors = new SortedDictionary<int, BusinessHealthAccumulator>();
            var tenants = new List<BusinessTenantProjection>(_session.Businesses.Count);
            foreach (var business in _session.Businesses)
            {
                if (!_session.TryGetRoom(business.RoomId, out var room)) continue;
                if (!floors.TryGetValue(room.Floor, out var value)) floors.Add(room.Floor, value = new BusinessHealthAccumulator());
                value.Tenants++;
                if (business.IsInsolvent) value.Insolvent++;
                value.NetCash += business.CashBalance;
                tenants.Add(new BusinessTenantProjection(
                    business.Id.Value,
                    business.RoomId.Value,
                    room.Floor,
                    business.ContentType.Value,
                    business.CashBalance,
                    business.EmployeeIds.Count,
                    business.LastCustomerRevenue,
                    business.LastContractRevenue,
                    business.LastWages,
                    business.LastOperatingCost,
                    business.LastRentPaid,
                    business.LastTaxPaid,
                    business.ArrearsDays,
                    business.WageArrears,
                    business.IsInsolvent,
                    business.IsVacantForReLease));
            }
            var result = new List<BusinessHealthFloorProjection>();
            foreach (var floor in floors) result.Add(new BusinessHealthFloorProjection(floor.Key, floor.Value.Tenants, floor.Value.Insolvent, floor.Value.NetCash));
            return new BusinessHealthOverlayProjection(result, tenants);
        }

        private sealed class BusinessHealthAccumulator { public int Tenants; public int Insolvent; public long NetCash; }


        private UtilitiesOverlayProjection CreateUtilities()
        {
            var power = _session.ElectricalGridProjection(); var waterWaste = _session.WaterWasteNetworkProjection(); var operations = _session.UtilityOperationsProjection();
            var failedByFloor = new Dictionary<int, int>();
            foreach (var item in operations.Equipment) if (item.IsFailed) failedByFloor[item.Floor] = failedByFloor.TryGetValue(item.Floor, out var count) ? count + 1 : 1;
            var floors = new List<UtilitiesFloorProjection>();
            var plumbingByFloor = new Dictionary<int, OneRoof.Domain.Infrastructure.WaterWasteFloorProjection>();
            foreach (var plumbing in waterWaste.Floors) plumbingByFloor[plumbing.Floor] = plumbing;
            foreach (var electrical in power.Floors)
            {
                var floor = electrical.Floor;
                if (!plumbingByFloor.TryGetValue(floor, out var plumbing)) continue;
                failedByFloor.TryGetValue(floor, out var failures);
                floors.Add(new UtilitiesFloorProjection(floor, electrical.Voltage, electrical.BrownoutReason.ToString(), plumbing.WaterPressure, plumbing.WaterFailure.ToString(), plumbing.WasteFailure.ToString(), failures,
                    electrical.RiserColumn, plumbing.WaterRiserColumn, plumbing.WasteChuteColumn, electrical.IsConnected, plumbing.HasWaterService, plumbing.HasWasteCollection));
            }
            return new UtilitiesOverlayProjection(floors, operations.Equipment);
        }

    }
}
