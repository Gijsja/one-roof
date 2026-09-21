using System;
using NUnit.Framework;
using OneRoof.Application.Prediction;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class ElevatorPlacementPredictorTests
    {
        [Test]
        public void MaxCarsPerBank_MatchesTheDomainThreeCarShaftLimit()
        {
            Assert.That(ElevatorPlacementPredictor.MaxCarsPerBank, Is.EqualTo(3));
            Assert.That(ElevatorPlacementPredictor.MaxCarsPerBank, Is.EqualTo(ElevatorBank.MaxCarsPerBank));
        }

        [Test]
        public void PredictAddition_SingleCarCongestedScenario_PredictsSignificantWaitReduction()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);
            var congestionService = new TransitCongestionService();
            var predictor = new ElevatorPlacementPredictor();

            var congestion = congestionService.Project(scenario.Bank, new Tick(0));
            var prediction = predictor.PredictAddition(congestion);

            Assert.That(prediction.IsValid, Is.True);
            Assert.That(prediction.CurrentCarCount, Is.EqualTo(1));
            Assert.That(prediction.PredictedCarCount, Is.EqualTo(2));
            Assert.That(prediction.Cost, Is.EqualTo(ElevatorPlacementPredictor.DefaultElevatorCost));
            Assert.That(prediction.EstimatedImprovementPercentage, Is.GreaterThanOrEqualTo(40f),
                "Doubling car count from 1 to 2 must predict at least a 40% reduction in wait time.");
            Assert.That(prediction.PredictedAverageWaitTicks, Is.LessThan(prediction.CurrentAverageWaitTicks));
            Assert.That(prediction.Confidence, Is.EqualTo(PredictionConfidence.High));
            Assert.That(prediction.ConfidenceLabel, Does.Contain("High confidence"));
        }

        [Test]
        public void PredictAddition_AtMaximumShaftCapacity_ReturnsInvalidWithReason()
        {
            var scenario = new CongestedElevatorScenario(carCount: ElevatorPlacementPredictor.MaxCarsPerBank);
            var congestionService = new TransitCongestionService();
            var predictor = new ElevatorPlacementPredictor();

            var congestion = congestionService.Project(scenario.Bank, new Tick(0));
            var prediction = predictor.PredictAddition(congestion);

            Assert.That(prediction.IsValid, Is.False);
            Assert.That(prediction.InvalidReason, Does.Contain("Maximum elevator bank shaft capacity"));
            Assert.That(prediction.EstimatedImprovementPercentage, Is.EqualTo(0f));
        }

        [Test]
        public void PredictAddition_NullCongestion_ThrowsArgumentNullException()
        {
            var predictor = new ElevatorPlacementPredictor();
            Assert.Throws<ArgumentNullException>(() => predictor.PredictAddition(null));
        }
    }
}
