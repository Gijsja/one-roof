using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Social;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class M9IntegratedAcceptanceTests
    {
        [Test]
        public void PolicySocialPressureCivilActionAndDecisionHistorySurviveReload()
        {
            var first = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 1);
            foreach (var household in first.Population.Households) household.AdjustCashBalance(-10000);
            var policy = new PolicyDecreeState(1.3f, .2f, false, false);
            Assert.That(first.ExecuteCommand(new SetPolicyDecreeCommand(policy)).Accepted, Is.True);

            // Organizing requires actual strong local ties and sustained measured pressure.
            for (var i = 0; i < first.Population.Persons.Count; i++)
                for (var j = i + 1; j < first.Population.Persons.Count; j++)
                {
                    var a = first.Population.Persons[i]; var b = first.Population.Persons[j];
                    if (a.HomeRoomId != b.HomeRoomId) continue;
                    var edge = first.Factions.GetOrCreateEdge(a.Id, b.Id, 0, "shared household conversation");
                    if (edge != null) edge.Affinity = .8f;
                }
            for (var i = 0; i < 12 && first.CivilActions.Actions[0].Phase != CivilActionPhase.Active; i++) first.AdvanceOneTick();
            Assert.That(first.Factions.Factions.Count, Is.EqualTo(4));
            Assert.That(first.Decisions.Entries.Count, Is.GreaterThanOrEqualTo(1));
            var tenant = Find(first, FactionIds.TenantUnion);
            Assert.That(tenant.MemberCount, Is.GreaterThan(0));
            Assert.That(tenant.TopGrievance, Is.Not.EqualTo("none"));
            Assert.That(first.CivilActions.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Active));
            Assert.That(first.Decisions.Entries.Count, Is.GreaterThanOrEqualTo(2));

            var restored = TowerSimulation.RestoreFromSaveData(first.ExportSaveData(), settlementPeriod: 1);
            Assert.That(Find(restored, FactionIds.TenantUnion).Pressure, Is.EqualTo(tenant.Pressure));
            Assert.That(restored.Decisions.Entries.Count, Is.EqualTo(first.Decisions.Entries.Count));
            Assert.That(restored.CivilActions.Actions.Count, Is.EqualTo(3));
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(first.CivilActions.Actions[0].Phase));
            Assert.That(restored.Economy.CashBalance, Is.EqualTo(first.Economy.CashBalance));
            first.AdvanceOneTick(); restored.AdvanceOneTick();
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Active));
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(first.CivilActions.Actions[0].Phase));
            foreach (var household in first.Population.Households) household.AdjustCashBalance(100000);
            foreach (var household in restored.Population.Households) household.AdjustCashBalance(100000);
            var reliefPolicy = new PolicyDecreeState(.7f, .2f, false, false);
            Assert.That(first.ExecuteCommand(new SetPolicyDecreeCommand(reliefPolicy)).Accepted, Is.True);
            Assert.That(restored.ExecuteCommand(new SetPolicyDecreeCommand(reliefPolicy)).Accepted, Is.True);
            first.AdvanceOneTick(); restored.AdvanceOneTick();
            Assert.That(restored.Factions.BuildOrganizedSignals(restored.CurrentTick)[0].MemberCount, Is.LessThan(CivilActionState.MinimumMembers), "Rent cap relief must reduce the measured cause below organization readiness.");
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Recovery), "Measured rent relief stops organization and recovers the active action.");
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(first.CivilActions.Actions[0].Phase));
        }

        private static FactionRecord Find(TowerSimulation simulation, string id)
        {
            foreach (var faction in simulation.Factions.Factions) if (faction.Id == id) return faction;
            throw new System.InvalidOperationException(id);
        }
    }
}
