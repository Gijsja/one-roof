using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Social;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.CivilAction;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class FactionWordBattleRespectTests
    {
        [Test]
        public void AwardsAreUniqueBoundedAndPersistAcrossDailyEvaluation()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 1);
            sim.AdvanceOneTick();
            var support = sim.Factions.Supports[0];
            var priorSupport = support.Support;
            var priorMember = support.IsMember;
            Assert.That(sim.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 1), Is.True);
            Assert.That(sim.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 1), Is.False);
            Assert.That(sim.Factions.AwardWordBattleRespect("missing", 1), Is.False);
            Assert.That(sim.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 4), Is.False);
            Assert.That(support.Support, Is.EqualTo(priorSupport));
            Assert.That(support.IsMember, Is.EqualTo(priorMember));
            sim.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 2);
            sim.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 3);
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            restored.AdvanceOneTick();
            var faction = restored.Factions.Factions[0];
            Assert.That(faction.EarnedRespect, Is.EqualTo(.30f).Within(.0001f));
            Assert.That(faction.EffectivePressure, Is.EqualTo(System.Math.Max(0f, faction.Pressure - .30f)).Within(.0001f));
            Assert.That(restored.Factions.AwardWordBattleRespect(FactionIds.TenantUnion, 3), Is.False);
        }

        [TestCase(.55f, CivilActionPhase.Clear)]
        [TestCase(1f, CivilActionPhase.Active)]
        public void RespectChangesRealCivilActionThresholdsWithoutRemovingOrganization(float rawPressure, CivilActionPhase expected)
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var first = sim.Population.Persons[0].Id.Value;
            var second = sim.Population.Persons[1].Id.Value;
            var state = FactionState.FromSaveData(new FactionSaveData
            {
                version = 4,
                edges = new[] { new RelationshipEdgeSaveData { first = first, second = second, affinity = .8f, lastContactTick = 100 } },
                supports = new[] {
                    new FactionSupportSaveData { residentId = first, factionId = FactionIds.TenantUnion, support = .8f, sustainedDays = 3, grievanceDays = 3, directPressure = rawPressure, driver = "arrears", homeFloor = 1 },
                    new FactionSupportSaveData { residentId = second, factionId = FactionIds.TenantUnion, support = .8f, sustainedDays = 3, grievanceDays = 3, directPressure = rawPressure, driver = "arrears", homeFloor = 1 }
                }
            }, sim.Population);
            for (var tier = 1; tier <= 3; tier++) state.AwardWordBattleRespect(FactionIds.TenantUnion, tier);
            var civil = new CivilActionState();
            civil.Evaluate(100, state.BuildOrganizedSignals(100), 0f);
            civil.Evaluate(101, state.BuildOrganizedSignals(101), 0f);
            Assert.That(civil.Actions[0].Phase, Is.EqualTo(expected));
            Assert.That(state.BuildOrganizedSignals(101)[0].MemberCount, Is.EqualTo(2));
            Assert.That(state.Supports[0].DirectPressure, Is.EqualTo(rawPressure));
        }

        [Test]
        public void RestoreIgnoresUnprovenNumericRespectAndInvalidDuplicateTiers()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var data = new FactionSaveData { version = 5, factions = new[] {
                new FactionRecordSaveData { id = FactionIds.TenantUnion, pressure = .8f, earnedRespect = float.PositiveInfinity },
                new FactionRecordSaveData { id = FactionIds.MerchantGuild, pressure = .8f, earnedRespect = 99f, wordBattleVictoryTiers = new[] { 1, 1, 0, 4, -1 } }
            } };
            var restored = FactionState.FromSaveData(data, sim.Population);
            Assert.That(restored.Factions[0].EarnedRespect, Is.Zero);
            Assert.That(restored.Factions[2].EarnedRespect, Is.EqualTo(.1f));
            data.version = 4;
            Assert.That(FactionState.FromSaveData(data, sim.Population).Factions[2].EarnedRespect, Is.Zero);
        }
    }
}
