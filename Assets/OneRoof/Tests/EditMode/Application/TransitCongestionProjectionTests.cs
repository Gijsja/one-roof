using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class TransitCongestionProjectionTests
    {
        [Test]
        public void ServiceIdentifiesFloorZeroAsBottleneckInMorningScenario()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);
            var service = new TransitCongestionService();

            var projection = service.Project(scenario.Bank, new Tick(0));

            Assert.That(projection.TotalQueued, Is.EqualTo(CongestedElevatorScenario.TotalResidents));
            Assert.That(projection.BottleneckFloor, Is.EqualTo(0));
            Assert.That(projection.OverallSeverity, Is.EqualTo(CongestionSeverity.Severe));
            Assert.That(projection.Floors[0].Severity, Is.EqualTo(CongestionSeverity.Severe));
            Assert.That(projection.Floors[1].Severity, Is.EqualTo(CongestionSeverity.Clear));
        }

        [Test]
        public void ProjectionReflectsProgressAsScenarioRuns()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);
            var service = new TransitCongestionService();

            scenario.RunToCompletion();
            var projection = service.Project(scenario.Bank, scenario.CurrentTick);

            Assert.That(projection.TotalQueued, Is.Zero);
            Assert.That(projection.DeliveredCount, Is.EqualTo(CongestedElevatorScenario.TotalResidents));
            Assert.That(projection.OverallSeverity, Is.EqualTo(CongestionSeverity.Clear));
            Assert.That(projection.AverageWaitTicks, Is.GreaterThan(0f));
        }
    }
}
