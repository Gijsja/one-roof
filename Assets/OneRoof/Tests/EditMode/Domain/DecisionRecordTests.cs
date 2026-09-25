using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Social;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class DecisionRecordTests
    {
        [Test]
        public void EffectiveDecreeRecordsOnceAndSurvivesSaveLoadWithDailyObservation()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var lower = new PolicyDecreeState(.7f, 0f, false, false);
            Assert.That(sim.ExecuteCommand(new SetPolicyDecreeCommand(lower)).Accepted, Is.True);
            Assert.That(sim.Decisions.Entries.Count, Is.EqualTo(1));
            Assert.That(sim.Decisions.Entries[0].observations.Length, Is.EqualTo(1));
            Assert.That(sim.Decisions.Entries[0].householdIds.Length, Is.GreaterThan(0));
            Assert.That(sim.Decisions.Entries[0].observations[0].householdBalances.Length, Is.GreaterThan(0));
            Assert.That(sim.Decisions.Entries[0].immediateEffect, Does.Contain("No immediate treasury"));
            sim.ExecuteCommand(new SetPolicyDecreeCommand(lower));
            Assert.That(sim.Decisions.Entries.Count, Is.EqualTo(1), "A no-op must not create a second decision.");
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            Assert.That(restored.Decisions.Entries.Count, Is.EqualTo(1));
            Assert.That(restored.Decisions.Entries[0].id, Is.EqualTo(sim.Decisions.Entries[0].id));
            for (var tick = 0; tick < 1440; tick++) restored.AdvanceOneTick();
            Assert.That(restored.Decisions.Entries[0].observations.Length, Is.EqualTo(2));
            Assert.That(restored.Decisions.Entries[0].observations[1].tick, Is.EqualTo(1440));
            Assert.That(restored.Decisions.Entries[0].observations[1].householdBalances.Length,
                Is.EqualTo(restored.Decisions.Entries[0].observations[0].householdBalances.Length));
        }

        [Test]
        public void ActiveCivilActionGetsOneHistoricalEntryAndPhaseUpdates()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var signals = new[] { new CivilActionSignal(FactionIds.TenantUnion, .8f, 3, "rent burden", new[] { 11, 12 }, new[] { 2 }) };
            sim.CivilActions.Evaluate(1440, signals, 0f);
            sim.Decisions.RecordCivilPhases(sim);
            Assert.That(sim.Decisions.Entries.Count, Is.Zero);
            sim.CivilActions.Evaluate(2880, signals, 0f);
            sim.Decisions.RecordCivilPhases(sim);
            Assert.That(sim.Decisions.Entries.Count, Is.EqualTo(1));
            var entry = sim.Decisions.Entries[0];
            Assert.That(entry.kind, Is.EqualTo("civil_action"));
            Assert.That(entry.residentIds, Is.EquivalentTo(new[] { 11, 12 }));
            sim.Decisions.RecordCivilPhases(sim);
            Assert.That(sim.Decisions.Entries.Count, Is.EqualTo(1));
            Assert.That(TowerSimulation.RestoreFromSaveData(sim.ExportSaveData()).Decisions.Entries[0].id, Is.EqualTo(entry.id));
        }

        [Test]
        public void HistoryEvictsOldestCompletedEntryAtBound()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var low = new PolicyDecreeState(.7f, 0f, false, false);
            var normal = PolicyDecreeState.Default;
            for (var i = 0; i < 70; i++)
                sim.ExecuteCommand(new SetPolicyDecreeCommand(i % 2 == 0 ? low : normal));
            Assert.That(sim.Decisions.Entries.Count, Is.EqualTo(64));
            Assert.That(sim.Decisions.Entries[0].id, Is.EqualTo(7));
            Assert.That(sim.Decisions.Entries[63].id, Is.EqualTo(70));
        }
    }
}
