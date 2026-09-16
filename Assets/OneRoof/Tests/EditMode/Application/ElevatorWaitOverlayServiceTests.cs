using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class ElevatorWaitOverlayServiceTests
    {
        [Test]
        public void CreateOverlay_SevereMorningCongestion_IdentifiesBottleneckAndExplainsCauses()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);
            var congestionService = new TransitCongestionService();
            var overlayService = new ElevatorWaitOverlayService();

            var congestion = congestionService.Project(scenario.Bank, new Tick(0));
            var overlay = overlayService.CreateOverlay(congestion);

            Assert.That(overlay.BottleneckFloor, Is.EqualTo(0));
            Assert.That(overlay.OverallSeverity, Is.EqualTo(CongestionTier.Severe));
            Assert.That(overlay.FloorFlows.Count, Is.EqualTo(5));

            var foundFloor0 = overlay.TryGetFloorFlow(0, out var floor0Flow);
            Assert.That(foundFloor0, Is.True);
            Assert.That(floor0Flow.IsBottleneck, Is.True);
            Assert.That(floor0Flow.QueuedCount, Is.EqualTo(CongestedElevatorScenario.TotalResidents));
            Assert.That(floor0Flow.NonColorBadge, Does.Contain("BOTTLENECK"));
            Assert.That(floor0Flow.NonColorBadge, Does.Contain("SEVERE"));
            Assert.That(floor0Flow.FlowIntensity, Is.GreaterThan(0.5f));

            var foundFloor1 = overlay.TryGetFloorFlow(1, out var floor1Flow);
            Assert.That(foundFloor1, Is.True);
            Assert.That(floor1Flow.IsBottleneck, Is.False);
            Assert.That(floor1Flow.QueuedCount, Is.Zero);

            Assert.That(overlay.PrimaryCause, Does.Contain("Floor 0"));
            Assert.That(overlay.ContributingCauses.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(overlay.RecommendedAction, Does.Contain("Build mode"));
        }

        [Test]
        public void CreateOverlay_CompletedTransit_ReflectsNominalState()
        {
            var scenario = new CongestedElevatorScenario(carCount: 1);
            var congestionService = new TransitCongestionService();
            var overlayService = new ElevatorWaitOverlayService();

            scenario.RunToCompletion();
            var congestion = congestionService.Project(scenario.Bank, scenario.CurrentTick);
            var overlay = overlayService.CreateOverlay(congestion);

            Assert.That(overlay.OverallSeverity, Is.EqualTo(CongestionTier.Clear));
            Assert.That(overlay.PrimaryCause, Does.Contain("nominal"));
        }
    }
}
