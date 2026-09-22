using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ScrutinyStateTests
    {
        [Test]
        public void RepeatedExpansion_RaisesEventPressure()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            simulation.Scrutiny.RecordExpansion(100);

            for (var tick = 0; tick < 5; tick++)
            {
                simulation.AdvanceOneTick();
            }

            Assert.That(simulation.Scrutiny.Value, Is.GreaterThan(.12f));
            Assert.That(simulation.Scrutiny.ContributingFactors, Has.Some.Contains("construction"));
        }

        [Test]
        public void UntouchedMorningRush_NeverSaturatesScrutiny()
        {
            // Regression: ordinary commute pressure ratcheted scrutiny to a
            // permanent 100%, which would fire max-rate events forever once
            // OR-1002 consumes ExternalEventPressure. A busy-but-unexpanded
            // tower must settle below saturation.
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            for (var tick = 0; tick < 600; tick++)
            {
                simulation.AdvanceOneTick();
            }

            Assert.That(simulation.Scrutiny.Value, Is.LessThan(.80f));
        }

        [Test]
        public void SaveRoundTrip_PreservesScrutinyState()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            simulation.Scrutiny.RecordExpansion(12);
            simulation.Scrutiny.RecordAggressivePolicy(.4f);
            simulation.AdvanceOneTick();

            var restored = TowerSimulation.RestoreFromSaveData(simulation.ExportSaveData());

            Assert.That(restored.Scrutiny.Value, Is.EqualTo(simulation.Scrutiny.Value));
            Assert.That(restored.Scrutiny.RecentExpansionPressure, Is.EqualTo(simulation.Scrutiny.RecentExpansionPressure));
            Assert.That(restored.Scrutiny.RecentPolicyPressure, Is.EqualTo(simulation.Scrutiny.RecentPolicyPressure));
        }
    }
}
