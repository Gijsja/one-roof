using OneRoof.Domain.Transit;

namespace OneRoof.Application.Transit
{
    public readonly struct FloorCongestionProjection
    {
        public FloorCongestionProjection(
            int floorLevel,
            int queuedCount,
            long maxWaitTicks,
            float averageWaitTicks,
            CongestionSeverity severity)
        {
            FloorLevel = floorLevel;
            QueuedCount = queuedCount;
            MaxWaitTicks = maxWaitTicks;
            AverageWaitTicks = averageWaitTicks;
            Severity = severity;
        }

        public int FloorLevel { get; }

        public int QueuedCount { get; }

        public long MaxWaitTicks { get; }

        public float AverageWaitTicks { get; }

        public CongestionSeverity Severity { get; }
    }
}
