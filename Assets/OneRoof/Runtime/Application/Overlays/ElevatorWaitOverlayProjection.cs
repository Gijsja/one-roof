using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Complete overlay projection for elevator wait and transit congestion, exposing
    /// flow metrics, non-color indicators, and the full symptom-to-cause explanation chain.
    /// </summary>
    public sealed class ElevatorWaitOverlayProjection
    {
        public ElevatorWaitOverlayProjection(
            long tick,
            int bottleneckFloor,
            CongestionTier overallSeverity,
            IReadOnlyList<FloorWaitFlowProjection> floorFlows,
            string primaryCause,
            IReadOnlyList<string> contributingCauses,
            string recommendedAction)
        {
            Tick = tick;
            BottleneckFloor = bottleneckFloor;
            OverallSeverity = overallSeverity;
            FloorFlows = floorFlows != null
                ? new ReadOnlyCollection<FloorWaitFlowProjection>(new List<FloorWaitFlowProjection>(floorFlows))
                : new ReadOnlyCollection<FloorWaitFlowProjection>(Array.Empty<FloorWaitFlowProjection>());
            PrimaryCause = primaryCause ?? string.Empty;
            ContributingCauses = contributingCauses != null
                ? new ReadOnlyCollection<string>(new List<string>(contributingCauses))
                : new ReadOnlyCollection<string>(Array.Empty<string>());
            RecommendedAction = recommendedAction ?? string.Empty;
        }

        public long Tick { get; }

        public int BottleneckFloor { get; }

        public CongestionTier OverallSeverity { get; }

        public IReadOnlyList<FloorWaitFlowProjection> FloorFlows { get; }

        public string PrimaryCause { get; }

        public IReadOnlyList<string> ContributingCauses { get; }

        public string RecommendedAction { get; }

        public bool TryGetFloorFlow(int floor, out FloorWaitFlowProjection flow)
        {
            for (var i = 0; i < FloorFlows.Count; i++)
            {
                if (FloorFlows[i].FloorLevel == floor)
                {
                    flow = FloorFlows[i];
                    return true;
                }
            }

            flow = default;
            return false;
        }
    }
}
