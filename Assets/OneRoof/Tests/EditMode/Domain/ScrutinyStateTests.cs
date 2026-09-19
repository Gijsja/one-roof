using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ScrutinyStateTests
    {
        [Test]
        public void RepeatedExpansion_RaisesPressureAndCanConstrainFurtherExpansion()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor();
            simulation.Scrutiny.RecordExpansion(100);

            simulation.AdvanceOneTick();

            Assert.That(simulation.Scrutiny.Value, Is.GreaterThan(.10f));
            Assert.That(simulation.Scrutiny.ContributingFactors, Has.Some.Contains("construction"));
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
