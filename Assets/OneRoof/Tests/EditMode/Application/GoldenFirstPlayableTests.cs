using System;
using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.Application.Transit;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Application.Tests.EditMode
{
    /// <summary>
    /// Golden Acceptance Test for the First Playable slice (Docs/06_TEST_STRATEGY.md & Docs/01_GAME_VISION.md).
    ///
    /// Verifies the full explanation chain:
    ///   1. Five floors and fifty residents under morning commute demand create an observable elevator bottleneck.
    ///   2. Overlay and inspector expose the symptom, pattern, and contributing causes (non-color accessible).
    ///   3. Placement prediction estimates before/after wait reduction with high confidence.
    ///   4. Player executes the capacity intervention (adds elevator car capacity).
    ///   5. Commute and elevator wait improve by more than the 40% agreed threshold.
    ///   6. Save envelope and resident schedules remain intact and uncorrupted.
    /// </summary>
    public sealed class GoldenFirstPlayableTests
    {
        [Test]
        public void GoldenFirstPlayable_FullLoop_DemonstratesExplanationChainAndMeasurableImprovement()
        {
            // ── Step 1: Initial Tower Setup (5 Floors, 50 Residents) ─────────
            var topology = FiveFloorTopologyFixture.Create();
            var rng = new DeterministicRandomStream(42);
            var population = FiftyResidentFixture.Create(topology, rng);

            Assert.That(population.Persons.Count, Is.EqualTo(50), "First playable requires exactly 50 persistent residents.");
            Assert.That(population.Households.Count, Is.EqualTo(16), "16 households across floors 1-4.");

            var scenario1 = new CongestedElevatorScenario(carCount: 1);
            var congestionService = new TransitCongestionService();
            var overlayService = new ElevatorWaitOverlayService();
            var predictor = new ElevatorPlacementPredictor();
            var modeSession = new ModeShellSession();

            // ── Step 2: Observe Bottleneck (Symptom in Tower) ─────────────────
            var initialCongestion = congestionService.Project(scenario1.Bank, new Tick(0));

            Assert.That(initialCongestion.TotalQueued, Is.EqualTo(50));
            Assert.That(initialCongestion.BottleneckFloor, Is.EqualTo(0));
            Assert.That(initialCongestion.OverallSeverity, Is.EqualTo(CongestionSeverity.Severe));
            Assert.That(initialCongestion.Floors[0].Severity, Is.EqualTo(CongestionSeverity.Severe));

            // ── Step 3: Overlay & Inspector (Explain Pattern & Cause) ─────────
            modeSession.SwitchMode(InteractionMode.Data);
            modeSession.SetActiveOverlay("overlay:elevator_wait");

            var overlay = overlayService.CreateOverlay(initialCongestion);

            Assert.That(overlay.BottleneckFloor, Is.EqualTo(0));
            Assert.That(overlay.FloorFlows[0].IsBottleneck, Is.True);
            Assert.That(overlay.FloorFlows[0].NonColorBadge, Does.Contain("BOTTLENECK"));
            Assert.That(overlay.FloorFlows[0].NonColorBadge, Does.Contain("SEVERE"));
            Assert.That(overlay.PrimaryCause, Does.Contain("Floor 0"));
            Assert.That(overlay.ContributingCauses.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(overlay.RecommendedAction, Does.Contain("Build mode"));

            var inspector = new ElevatorCongestionInspectorProjection(
                floorLevel: 0,
                title: "Floor 0 Elevator Congestion",
                symptomDescription: "Severe morning commute delay",
                contributingCauses: overlay.ContributingCauses,
                suggestedResponseAction: overlay.RecommendedAction,
                canDirectRouteToBuild: true,
                targetBuildTool: "transit:elevator_car");

            Assert.That(inspector.CanDirectRouteToBuild, Is.True);

            // ── Step 4: Placement Prediction (Preview Before/After) ───────────
            modeSession.SwitchMode(InteractionMode.Build);
            modeSession.SelectBuildTool("transit:elevator_car");

            var preview = predictor.PredictAddition(initialCongestion);

            Assert.That(preview.IsValid, Is.True);
            Assert.That(preview.Cost, Is.EqualTo(500));
            Assert.That(preview.CurrentCarCount, Is.EqualTo(1));
            Assert.That(preview.PredictedCarCount, Is.EqualTo(2));
            Assert.That(preview.EstimatedImprovementPercentage, Is.GreaterThanOrEqualTo(40f),
                "Prediction must estimate at least a 40% improvement in wait time.");
            Assert.That(preview.Confidence, Is.EqualTo(PredictionConfidence.High));

            // ── Step 5: Run Baseline Scenario (1 Car) ─────────────────────────
            scenario1.RunToCompletion();
            Assert.That(scenario1.IsComplete, Is.True);

            var baselineDelivered = scenario1.Bank.DeliveredCount;
            var baselineAvgWait = scenario1.Bank.AverageWaitTicks;

            Assert.That(baselineDelivered, Is.EqualTo(50));
            Assert.That(baselineAvgWait, Is.GreaterThan(20f), "Baseline wait time must reflect congestion.");

            // ── Step 6: Player Intervention & Run Capacity Scenario (2 Cars) ──
            var scenario2 = new CongestedElevatorScenario(carCount: 2);
            scenario2.RunToCompletion();
            Assert.That(scenario2.IsComplete, Is.True);

            var interventionDelivered = scenario2.Bank.DeliveredCount;
            var interventionAvgWait = scenario2.Bank.AverageWaitTicks;

            Assert.That(interventionDelivered, Is.EqualTo(50));

            // ── Step 7: Measurable Improvement Verification ───────────────────
            var measuredImprovement = ((baselineAvgWait - interventionAvgWait) / baselineAvgWait) * 100f;

            Assert.That(measuredImprovement, Is.GreaterThanOrEqualTo(40f),
                $"Intervention must improve elevator wait by >= 40%. Measured: {measuredImprovement:F1}%.");
            Assert.That(interventionAvgWait, Is.LessThan(baselineAvgWait * 0.6f),
                "Intervention must reduce average wait time by more than 40%.");

            // ── Step 8: Save & Schedule Integrity Verification ────────────────
            // Schedules remain uncorrupted
            for (var i = 0; i < population.Persons.Count; i++)
            {
                var person = population.Persons[i];
                Assert.That(person.Schedule.Blocks.Count, Is.EqualTo(4), "Daily schedule must retain 4 blocks.");
                Assert.That(person.HomeRoomId.IsValid, Is.True);
                Assert.That(person.WorkplaceRoomId.IsValid, Is.True);
            }

            // Save envelope can be constructed from final simulation state
            var randomState = new RandomStreamState(42, 42, scenario2.CurrentTick.Value);
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                scenario2.CurrentTick,
                randomState,
                "2026-09-16T12:00:00Z",
                "0.1.0");

            var envelope = new SaveEnvelope<int>(metadata, scenario2.Bank.DeliveredCount);
            Assert.That(envelope.Metadata.Version.Value, Is.EqualTo(1));
            Assert.That(envelope.Metadata.SimulationTick.Value, Is.EqualTo(scenario2.CurrentTick.Value));
            Assert.That(envelope.StatePayload, Is.EqualTo(50));
        }
    }
}
