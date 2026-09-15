using NUnit.Framework;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class CongestedElevatorScenarioTests
    {
        [Test]
        public void ScenarioStartsCompletelyQueuedAtFloorZero()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);

            Assert.That(scenario.Bank.GetQueueLength(0), Is.EqualTo(CongestedElevatorScenario.TotalResidents));
            Assert.That(scenario.Bank.DeliveredCount, Is.Zero);
            Assert.That(scenario.IsComplete, Is.False);
        }

        [Test]
        public void SingleCarScenarioProducesDeterministicDeliveryAndMetrics()
        {
            var scenarioA = new CongestedElevatorScenario(carCount: 1);
            scenarioA.RunToCompletion();

            var scenarioB = new CongestedElevatorScenario(carCount: 1);
            scenarioB.RunToCompletion();

            Assert.That(scenarioA.IsComplete, Is.True);
            Assert.That(scenarioA.Bank.DeliveredCount, Is.EqualTo(CongestedElevatorScenario.TotalResidents));
            Assert.That(scenarioA.Bank.AverageWaitTicks, Is.GreaterThan(0f));

            // Must produce identical metrics across runs
            Assert.That(scenarioA.Bank.AverageWaitTicks, Is.EqualTo(scenarioB.Bank.AverageWaitTicks));
            Assert.That(scenarioA.CurrentTick.Value, Is.EqualTo(scenarioB.CurrentTick.Value));
        }

        [Test]
        public void AddingElevatorCapacityMeasurablyImprovesAverageWaitTicks()
        {
            var singleCarScenario = new CongestedElevatorScenario(carCount: 1);
            singleCarScenario.RunToCompletion();

            var twoCarScenario = new CongestedElevatorScenario(carCount: 2);
            twoCarScenario.RunToCompletion();

            Assert.That(twoCarScenario.IsComplete, Is.True);
            Assert.That(twoCarScenario.Bank.DeliveredCount, Is.EqualTo(CongestedElevatorScenario.TotalResidents));

            // 2 cars must reduce average wait ticks
            Assert.That(twoCarScenario.Bank.AverageWaitTicks, Is.LessThan(singleCarScenario.Bank.AverageWaitTicks));
            Assert.That(twoCarScenario.CurrentTick.Value, Is.LessThan(singleCarScenario.CurrentTick.Value));
        }
    }
}
