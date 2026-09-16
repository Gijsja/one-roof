namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Flow and wait metric projection for a single floor, including non-color accessibility indicators.
    /// </summary>
    public readonly struct FloorWaitFlowProjection
    {
        public FloorWaitFlowProjection(
            int floorLevel,
            int queuedCount,
            long maxWaitTicks,
            float averageWaitTicks,
            CongestionTier severity,
            bool isBottleneck,
            float flowDirection,
            float flowIntensity,
            string nonColorBadge)
        {
            FloorLevel = floorLevel;
            QueuedCount = queuedCount;
            MaxWaitTicks = maxWaitTicks;
            AverageWaitTicks = averageWaitTicks;
            Severity = severity;
            IsBottleneck = isBottleneck;
            FlowDirection = flowDirection;
            FlowIntensity = flowIntensity;
            NonColorBadge = nonColorBadge ?? string.Empty;
        }

        public int FloorLevel { get; }

        public int QueuedCount { get; }

        public long MaxWaitTicks { get; }

        public float AverageWaitTicks { get; }

        public CongestionTier Severity { get; }

        public bool IsBottleneck { get; }

        /// <summary>Flow direction vector along the floor towards the transit node (-1 for leftward).</summary>
        public float FlowDirection { get; }

        /// <summary>Normalized flow intensity from 0.0 (idle) to 1.0 (heavily congested).</summary>
        public float FlowIntensity { get; }

        /// <summary>Textual indicator providing non-color accessibility (Docs/04_UX_CONTRACT.md).</summary>
        public string NonColorBadge { get; }
    }
}
