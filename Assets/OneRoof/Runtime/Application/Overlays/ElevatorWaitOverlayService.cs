using System;
using System.Collections.Generic;
using OneRoof.Application.Transit;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Service that generates the elevator wait flow overlay and names contributing causes
    /// for UI inspectors and data mode visualization (Docs/04_UX_CONTRACT.md).
    /// </summary>
    public sealed class ElevatorWaitOverlayService
    {
        public ElevatorWaitOverlayProjection CreateOverlay(ElevatorBankCongestionProjection congestion)
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
    }
}
