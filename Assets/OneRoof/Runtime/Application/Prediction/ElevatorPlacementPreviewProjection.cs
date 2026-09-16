using OneRoof.Application.Overlays;

namespace OneRoof.Application.Prediction
{
    /// <summary>
    /// Placement preview projection showing cost, validity, and predicted transit wait times
    /// before and after adding elevator capacity (Docs/04_UX_CONTRACT.md).
    /// </summary>
    public sealed class ElevatorPlacementPreviewProjection
    {
        public ElevatorPlacementPreviewProjection(
            string itemName,
            int cost,
            bool isValid,
            string invalidReason,
            int currentCarCount,
            int predictedCarCount,
            float currentAverageWaitTicks,
            float predictedAverageWaitTicks,
            long currentMaxWaitTicks,
            long predictedMaxWaitTicks,
            CongestionTier currentSeverity,
            CongestionTier predictedSeverity,
            float estimatedImprovementPercentage,
            PredictionConfidence confidence,
            string confidenceLabel)
        {
            ItemName = itemName ?? string.Empty;
            Cost = cost;
            IsValid = isValid;
            InvalidReason = invalidReason ?? string.Empty;
            CurrentCarCount = currentCarCount;
            PredictedCarCount = predictedCarCount;
            CurrentAverageWaitTicks = currentAverageWaitTicks;
            PredictedAverageWaitTicks = predictedAverageWaitTicks;
            CurrentMaxWaitTicks = currentMaxWaitTicks;
            PredictedMaxWaitTicks = predictedMaxWaitTicks;
            CurrentSeverity = currentSeverity;
            PredictedSeverity = predictedSeverity;
            EstimatedImprovementPercentage = estimatedImprovementPercentage;
            Confidence = confidence;
            ConfidenceLabel = confidenceLabel ?? string.Empty;
        }

        public string ItemName { get; }

        public int Cost { get; }

        public bool IsValid { get; }

        public string InvalidReason { get; }

        public int CurrentCarCount { get; }

        public int PredictedCarCount { get; }

        public float CurrentAverageWaitTicks { get; }

        public float PredictedAverageWaitTicks { get; }

        public long CurrentMaxWaitTicks { get; }

        public long PredictedMaxWaitTicks { get; }

        public CongestionTier CurrentSeverity { get; }

        public CongestionTier PredictedSeverity { get; }

        /// <summary>Percentage wait-time reduction: (Current - Predicted) / Current * 100.</summary>
        public float EstimatedImprovementPercentage { get; }

        public PredictionConfidence Confidence { get; }

        public string ConfidenceLabel { get; }
    }
}
