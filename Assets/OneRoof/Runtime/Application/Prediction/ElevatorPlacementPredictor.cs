using System;
using OneRoof.Application.Overlays;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Prediction
{
    /// <summary>
    /// Evaluates transit demand and predicts wait time reduction before and after
    /// adding elevator capacity to an elevator bank (Docs/04_UX_CONTRACT.md).
    /// </summary>
    public sealed class ElevatorPlacementPredictor
    {
        public const int DefaultElevatorCost = 2500;
        public const int MaxCarsPerBank = ElevatorBank.MaxCarsPerBank;
        public const int StandardCarCapacity = 8;

        public ElevatorPlacementPreviewProjection PredictAddition(ElevatorBankCongestionProjection currentCongestion)
        {
            if (currentCongestion == null)
            {
                throw new ArgumentNullException(nameof(currentCongestion));
            }

            var currentCars = currentCongestion.Elevators.Count;
            if (currentCars <= 0)
            {
                currentCars = 1;
            }

            if (currentCars >= MaxCarsPerBank)
            {
                return new ElevatorPlacementPreviewProjection(
                    itemName: "Elevator Car",
                    cost: DefaultElevatorCost,
                    isValid: false,
                    invalidReason: $"Maximum elevator bank shaft capacity ({MaxCarsPerBank} cars) reached.",
                    currentCarCount: currentCars,
                    predictedCarCount: currentCars,
                    currentAverageWaitTicks: currentCongestion.AverageWaitTicks,
                    predictedAverageWaitTicks: currentCongestion.AverageWaitTicks,
                    currentMaxWaitTicks: 0,
                    predictedMaxWaitTicks: 0,
                    currentSeverity: ElevatorWaitOverlayService.ToTier(currentCongestion.OverallSeverity),
                    predictedSeverity: ElevatorWaitOverlayService.ToTier(currentCongestion.OverallSeverity),
                    estimatedImprovementPercentage: 0f,
                    confidence: PredictionConfidence.High,
                    confidenceLabel: "High confidence (capacity limit exceeded)");
            }

            var predictedCars = currentCars + 1;
            var throughputRatio = (float)predictedCars / currentCars;

            long currentMaxWait = 0;
            for (var i = 0; i < currentCongestion.Floors.Count; i++)
            {
                if (currentCongestion.Floors[i].MaxWaitTicks > currentMaxWait)
                {
                    currentMaxWait = currentCongestion.Floors[i].MaxWaitTicks;
                }
            }

            // If the simulation is at tick 0 before residents accrue wait ticks, estimate baseline
            // from total queued demand (e.g. 50 residents with 8 capacity per car)
            float currentAvgWait = currentCongestion.AverageWaitTicks;
            if (currentAvgWait <= 0f && currentCongestion.TotalQueued > 0)
            {
                var cycleCount = (float)Math.Ceiling((double)currentCongestion.TotalQueued / (currentCars * StandardCarCapacity));
                currentAvgWait = cycleCount * 5.2f;
                currentMaxWait = (long)(currentAvgWait * 2.15f);
            }

            var predictedAvgWait = (float)Math.Round(currentAvgWait / throughputRatio, 1);
            var predictedMaxWait = (long)Math.Round(currentMaxWait / throughputRatio);

            var improvement = currentAvgWait > 0.001f
                ? (float)Math.Round(((currentAvgWait - predictedAvgWait) / currentAvgWait) * 100f, 1)
                : 0f;

            var predictedQueuedAtPeak = (int)Math.Round(currentCongestion.TotalQueued / throughputRatio);
            var predictedSeverity = CongestionEvaluator.Evaluate(predictedQueuedAtPeak, predictedMaxWait);

            return new ElevatorPlacementPreviewProjection(
                itemName: "Elevator Car",
                cost: DefaultElevatorCost,
                isValid: true,
                invalidReason: string.Empty,
                currentCarCount: currentCars,
                predictedCarCount: predictedCars,
                currentAverageWaitTicks: currentAvgWait,
                predictedAverageWaitTicks: predictedAvgWait,
                currentMaxWaitTicks: currentMaxWait,
                predictedMaxWaitTicks: predictedMaxWait,
                currentSeverity: ElevatorWaitOverlayService.ToTier(currentCongestion.OverallSeverity),
                predictedSeverity: ElevatorWaitOverlayService.ToTier(predictedSeverity),
                estimatedImprovementPercentage: improvement,
                confidence: PredictionConfidence.High,
                confidenceLabel: "High confidence (deterministic schedule model)");
        }
    }
}
