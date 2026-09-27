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

            for (var i = 0; i < 3; i++) first.AdvanceOneTick();
            Assert.That(first.Factions.Factions.Count, Is.EqualTo(4));
            Assert.That(first.Decisions.Entries.Count, Is.GreaterThanOrEqualTo(1));
            var tenant = Find(first, FactionIds.TenantUnion);
            Assert.That(tenant.MemberCount, Is.GreaterThan(0));
            Assert.That(tenant.TopGrievance, Is.Not.EqualTo("none"));
            Assert.That(first.CivilActions.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Active));
            Assert.That(first.Decisions.Entries.Count, Is.GreaterThanOrEqualTo(2));

            var restored = TowerSimulation.RestoreFromSaveData(first.ExportSaveData());
            Assert.That(Find(restored, FactionIds.TenantUnion).Pressure, Is.EqualTo(tenant.Pressure));
            Assert.That(restored.Decisions.Entries.Count, Is.EqualTo(first.Decisions.Entries.Count));
            Assert.That(restored.CivilActions.Actions.Count, Is.EqualTo(3));
            Assert.That(restored.CivilActions.Actions[0].Phase, Is.EqualTo(first.CivilActions.Actions[0].Phase));
            Assert.That(restored.Economy.CashBalance, Is.EqualTo(first.Economy.CashBalance));
        }

        private static FactionRecord Find(TowerSimulation simulation, string id)
        {
            foreach (var faction in simulation.Factions.Factions) if (faction.Id == id) return faction;
            throw new System.InvalidOperationException(id);
        }
    }
}
