using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Transit
{
    public sealed class ElevatorBankCongestionProjection
    {
        public ElevatorBankCongestionProjection(
            long tick,
            int totalQueued,
            int inTransitCount,
            int deliveredCount,
            float averageWaitTicks,
            int bottleneckFloor,
            CongestionSeverity overallSeverity,
            IReadOnlyList<FloorCongestionProjection> floors,
            IReadOnlyList<ElevatorProjection> elevators)
        {
            Tick = tick;
            TotalQueued = totalQueued;
            InTransitCount = inTransitCount;
            DeliveredCount = deliveredCount;
            AverageWaitTicks = averageWaitTicks;
            BottleneckFloor = bottleneckFloor;
            OverallSeverity = overallSeverity;
            Floors = new ReadOnlyCollection<FloorCongestionProjection>(new List<FloorCongestionProjection>(floors ?? Array.Empty<FloorCongestionProjection>()));
            Elevators = new ReadOnlyCollection<ElevatorProjection>(new List<ElevatorProjection>(elevators ?? Array.Empty<ElevatorProjection>()));
        }

        public long Tick { get; }

        public int TotalQueued { get; }

        public int InTransitCount { get; }

        public int DeliveredCount { get; }

        public float AverageWaitTicks { get; }

        public int BottleneckFloor { get; }

        public CongestionSeverity OverallSeverity { get; }

        public IReadOnlyList<FloorCongestionProjection> Floors { get; }

        public IReadOnlyList<ElevatorProjection> Elevators { get; }
    }
}
