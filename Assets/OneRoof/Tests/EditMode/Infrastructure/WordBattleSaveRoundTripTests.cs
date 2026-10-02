using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Dialogue;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Social;
using OneRoof.Infrastructure.Persistence;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    public sealed class WordBattleSaveRoundTripTests
    {
        private static void Execute(TowerSimulation sim, WordBattleAction action, string reply = null, int speaker = 0)
        {
            var result = sim.ExecuteCommand(new WordBattleCommand(action, FactionIds.MerchantGuild,
                reply, sim.WordBattles.Revision, speakerId: speaker));
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        private static TowerSimulation RoundTrip(TowerSimulation sim)
        {
            var serializer = new JsonSaveSerializer();
            var metadata = new SaveEnvelopeMetadata(new SchemaVersion(1), sim.Clock.CurrentTick,
                new RandomStreamState(1337, 1337, 0), "2026-10-02T00:00:00Z", "learned-wit-v1");
            var json = serializer.Serialize(new SaveEnvelope<TowerSaveData>(metadata, sim.ExportSaveData()));
            var loaded = serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loaded.IsSuccess, Is.True);
            return TowerSimulation.RestoreFromSaveData(loaded.Value.StatePayload);
        }

        [Test]
        public void JsonRoundTrip_ActiveBattleResumesSameChallengeWithoutDuplicateLearning()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            Execute(sim, WordBattleAction.Start);
            Execute(sim, WordBattleAction.Choose);
            var challenge = sim.WordBattles.ChallengeText;
            var revision = sim.WordBattles.Revision;
            var restored = RoundTrip(sim);
            Assert.That(restored.WordBattles.Phase, Is.EqualTo(WordBattlePhase.Suspended));
            Assert.That(restored.WordBattles.Revision, Is.EqualTo(revision));
            Assert.That(restored.WordBattles.ChallengeText, Is.EqualTo(challenge));
            Execute(restored, WordBattleAction.Resume);
            Execute(restored, WordBattleAction.Choose);
            Assert.That(restored.WordBattles.LeaderScore, Is.EqualTo(2));
            Assert.That(restored.WordBattles.LearnedReplies.Count, Is.EqualTo(2));
            var stale = restored.ExecuteCommand(new WordBattleCommand(WordBattleAction.Choose,
                expectedRevision: revision));
            Assert.That(stale.Accepted, Is.False);
            Assert.That(RoundTrip(restored).WordBattles.LearnedReplies.Count, Is.EqualTo(2));
        }

        [Test]
        public void JsonRoundTrip_VictoryPreservesRepertoireAndRespect()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = sim.Population.Persons[0].Id.Value;
            for (var i = 0; i < 3; i++)
            {
                Execute(sim, WordBattleAction.Teach, speaker: resident);
                Execute(sim, WordBattleAction.Choose, "wit:listen");
                Execute(sim, WordBattleAction.Close);
                if (i < 2) for (var tick = 0; tick < 1440; tick++) sim.AdvanceOneTick();
            }
            Execute(sim, WordBattleAction.Start);
            for (var i = 0; i < 2; i++)
            {
                WordBattleReply matching = null;
                foreach (var reply in WordBattleCatalog.Replies)
                    if (reply.Challenge == sim.WordBattles.ChallengeText || reply.Variant == sim.WordBattles.ChallengeText)
                        matching = reply;
                Assert.That(matching, Is.Not.Null);
                Execute(sim, WordBattleAction.Choose, matching.Id);
            }
            Assert.That(sim.WordBattles.Won, Is.True);
            var restored = RoundTrip(sim);
            Assert.That(restored.WordBattles.MasteredTiers, Is.EqualTo(1));
            Assert.That(restored.WordBattles.Practice, Is.False, "A saved real victory must not be relabeled as practice.");
            Assert.That(restored.WordBattles.LearnedReplies.Count, Is.EqualTo(5));
            Assert.That(restored.Factions.Factions[2].EarnedRespect, Is.EqualTo(.10f).Within(.0001));
            Execute(restored, WordBattleAction.Close);
            Execute(restored, WordBattleAction.Start);
            Assert.That(restored.WordBattles.Tier, Is.EqualTo(2));
            Assert.That(restored.Factions.Factions[2].EarnedRespect, Is.EqualTo(.10f).Within(.0001));
        }

        [Test]
        public void JsonRoundTrip_OldSaveDefaultsToStarterLineAndZeroRespect()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var data = sim.ExportSaveData();
            data.wordBattles = null;
            data.factions.version = 4;
            var restored = TowerSimulation.RestoreFromSaveData(data);
            restored = RoundTrip(restored);
            Assert.That(restored.WordBattles.Phase, Is.EqualTo(WordBattlePhase.Idle));
            Assert.That(restored.WordBattles.LearnedReplies.Count, Is.EqualTo(1));
            foreach (var faction in restored.Factions.Factions)
                Assert.That(faction.EarnedRespect, Is.Zero);
        }
    }
}
