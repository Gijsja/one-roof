using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Overlays
{
    /// <summary>One read-only seam for the Tower's diagnostic overlays and their cause chains.</summary>
    public sealed class TowerDataOverlays
    {
        private readonly TowerSimulationSession _session;
        private ElevatorBankCongestionProjection _cachedCongestion;
        private ElevatorWaitOverlayProjection _cachedWait;

        public TowerDataOverlays(TowerSimulationSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public ElevatorWaitOverlayProjection ElevatorWait
        {
            get
            {
                var congestion = _session.CongestionProjection();
                if (!ReferenceEquals(congestion, _cachedCongestion))
                {
                    _cachedCongestion = congestion;
                    _cachedWait = ProjectElevatorWait(congestion);
                }
                return _cachedWait;
            }
        }
        public SatisfactionOverlayProjection Satisfaction => CreateSatisfaction();
        public PopulationOverlayProjection Population => CreatePopulation();
        public ScrutinyOverlayProjection Scrutiny => CreateScrutiny();
        public FootTrafficOverlayProjection FootTraffic => CreateFootTraffic();
        public BusinessHealthOverlayProjection BusinessHealth => CreateBusinessHealth();
        public UtilitiesOverlayProjection Utilities => CreateUtilities();

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
                if (!accumulators.TryGetValue(room.Floor, out var values)) accumulators.Add(room.Floor, values = new float[3]);
                values[0] += person.Wellbeing.Satisfaction; values[1]++; values[2] += person.Wellbeing.Grievances.Count;
            }
            var floors = new List<SatisfactionFloorProjection>(); var total = 0f; var count = 0;
            foreach (var entry in accumulators)
            {
                var values = entry.Value; var satisfaction = values[1] == 0 ? 1f : values[0] / values[1];
                floors.Add(new SatisfactionFloorProjection(entry.Key, satisfaction, (int)values[1], (int)values[2]));
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

                var budget = _session.GetHousehold(person.HouseholdId).Budget;
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
            foreach (var business in _session.Businesses)
            {
                if (!_session.TryGetRoom(business.RoomId, out var room)) continue;
                if (!floors.TryGetValue(room.Floor, out var value)) floors.Add(room.Floor, value = new BusinessHealthAccumulator());
                value.Tenants++;
                if (business.IsInsolvent) value.Insolvent++;
                value.NetCash += business.CashBalance;
            }
            var result = new List<BusinessHealthFloorProjection>();
            foreach (var floor in floors) result.Add(new BusinessHealthFloorProjection(floor.Key, floor.Value.Tenants, floor.Value.Insolvent, floor.Value.NetCash));
            return new BusinessHealthOverlayProjection(result);
        }

        private sealed class BusinessHealthAccumulator { public int Tenants; public int Insolvent; public long NetCash; }


        private UtilitiesOverlayProjection CreateUtilities()
        {
            var power = _session.ElectricalGridProjection(); var waterWaste = _session.WaterWasteNetworkProjection(); var operations = _session.UtilityOperationsProjection();
            var failedByFloor = new Dictionary<int, int>();
            foreach (var item in operations.Equipment) if (item.IsFailed) failedByFloor[item.Floor] = failedByFloor.TryGetValue(item.Floor, out var count) ? count + 1 : 1;
            var floors = new List<UtilitiesFloorProjection>();
            for (var floor = 0; floor < _session.FloorCount; floor++)
            {
                var electrical = power.Floors[floor]; var plumbing = waterWaste.Floors[floor];
                failedByFloor.TryGetValue(floor, out var failures);
                floors.Add(new UtilitiesFloorProjection(floor, electrical.Voltage, electrical.BrownoutReason.ToString(), plumbing.WaterPressure, plumbing.WaterFailure.ToString(), plumbing.WasteFailure.ToString(), failures));
            }
            return new UtilitiesOverlayProjection(floors, operations.Equipment);
        }

    }
}
