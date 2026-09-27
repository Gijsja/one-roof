using NUnit.Framework;
using OneRoof.Domain.CivilAction;
using OneRoof.Domain.Social;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class CivilActionStateTests
    {
        private static CivilActionSignal Tenant(float pressure, int members = 3, string grievance = "rent burden") =>
            new CivilActionSignal(FactionIds.TenantUnion, pressure, members, grievance, new[] { 7, 3, 5 }, new[] { 2, 1 });

        [Test]
        public void ScrutinyAloneCannotCauseWarningOrAction()
        {
            var state = new CivilActionState();
            for (var day = 1; day <= 8; day++)
                state.Evaluate(day, new[] { Tenant(0f) }, 1f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Clear));
        }

        [Test]
        public void SustainedGrievanceTriggersAction_AndReliefRecoversWithoutStacking()
        {
            var state = new CivilActionState();
            state.Evaluate(1, new[] { Tenant(.45f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Warning));
            state.Evaluate(2, new[] { Tenant(.45f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Active));
            Assert.That(state.Actions[0].AffectedResidentIds, Is.EqualTo(new[] { 3, 5, 7 }));
            Assert.That(state.ResidentialRentCollectionMultiplier, Is.EqualTo(.75f));
            state.Evaluate(2, new[] { Tenant(.45f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Active));
            state.Evaluate(3, new[] { Tenant(.1f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Recovery));
            state.Evaluate(4, new[] { Tenant(.1f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Cooldown));
            for (var day = 5; day <= 7; day++) state.Evaluate(day, new[] { Tenant(.1f) }, 0f);
            Assert.That(state.Actions[0].Phase, Is.EqualTo(CivilActionPhase.Clear));
        }

        [Test]
        public void SaveLoadAtEveryPhasePreservesTimingAndNoDuplicateOnset()
        {
            var state = new CivilActionState();
            for (var day = 1; day <= 8; day++)
            {
                state.Evaluate(day, new[] { Tenant(day <= 2 ? .45f : .1f) }, 0f);
                var restored = CivilActionState.FromSaveData(state.ToSaveData());
                Assert.That(restored.Actions[0].Phase, Is.EqualTo(state.Actions[0].Phase));
                Assert.That(restored.Actions[0].PhaseStartedTick, Is.EqualTo(state.Actions[0].PhaseStartedTick));
                Assert.That(restored.Actions[0].OnsetCause, Is.EqualTo(state.Actions[0].OnsetCause));
                restored.Evaluate(day, new[] { Tenant(.45f) }, 1f);
                Assert.That(restored.Actions[0].Phase, Is.EqualTo(state.Actions[0].Phase));
                state = restored;
            }
        }

        [Test]
        public void RelevantFactionAndMembersAreRequiredForCapacityAndOutputEffects()
        {
            var state = new CivilActionState();
            var civic = new CivilActionSignal(FactionIds.CivicEcoCouncil, .6f, 3, "noise", new[] { 8, 9 }, new[] { 1 });
            var corporate = new CivilActionSignal(FactionIds.CorporateCoalition, .6f, 3, "commute", new[] { 10, 11 }, new[] { 2 });
            for (var day = 1; day <= 2; day++) state.Evaluate(day, new[] { civic, corporate }, 0f);
            Assert.That(state.LobbyProtestActive, Is.True);
            Assert.That(state.WorkSlowdownActive, Is.True);
            Assert.That(state.LobbyCapacityMultiplier, Is.EqualTo(.75f));
            Assert.That(state.BusinessOutputMultiplier, Is.EqualTo(.8f));

            var unrelated = new CivilActionState();
            for (var day = 1; day <= 3; day++)
                unrelated.Evaluate(day, new[] { new CivilActionSignal(FactionIds.CorporateCoalition, 1f, 1, "noise", new[] { 10 }, new[] { 2 }) }, 1f);
            Assert.That(unrelated.WorkSlowdownActive, Is.False);
        }
    }
}
