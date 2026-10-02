using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Dialogue;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Social;
namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class WordBattleTests
    {
        private static TowerSimulation Tower(bool allKnown = false)
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(); var data = sim.ExportSaveData();
            data.factions = new FactionSaveData { version = 5, supports = new[] { new FactionSupportSaveData { residentId = sim.Population.Persons[0].Id.Value, factionId = FactionIds.MerchantGuild, support = .8f } } };
            if (allKnown) { data.wordBattles = sim.WordBattles.ToSaveData(); var ids = new string[12]; for (int i = 0; i < 12; i++) ids[i] = WordBattleCatalog.Replies[i].Id; data.wordBattles.learnedIds = ids; }
            return TowerSimulation.RestoreFromSaveData(data);
        }
        private static void Do(TowerSimulation sim, WordBattleAction action, string reply = null, int tier = 0, bool practice = false)
        { Assert.That(sim.ExecuteCommand(new WordBattleCommand(action, FactionIds.MerchantGuild, reply, sim.WordBattles.Revision, practice, tier)).Accepted, Is.True); }
        private static string Correct(TowerSimulation sim)
        { foreach (var r in WordBattleCatalog.Replies) if (r.Challenge == sim.WordBattles.ChallengeText || r.Variant == sim.WordBattles.ChallengeText) return r.Id; return null; }
        [Test] public void LossLearnsOneReplyAndRetryWaitsUntilNextDay()
        {
            var sim = Tower(); Do(sim, WordBattleAction.Start); Do(sim, WordBattleAction.Choose); Do(sim, WordBattleAction.Choose);
            Assert.That(sim.WordBattles.Won, Is.False); Assert.That(sim.WordBattles.LearnedReplies.Count, Is.EqualTo(2));
            Do(sim, WordBattleAction.Close);
            Assert.That(sim.CanExecute(new WordBattleCommand(WordBattleAction.Start, FactionIds.MerchantGuild, expectedRevision: sim.WordBattles.Revision)).Accepted, Is.False);
            for (int i = 0; i < 1440; i++) sim.AdvanceOneTick(); Do(sim, WordBattleAction.Start);
        }
        [Test] public void VictoryIsPermanentAndPracticeCannotFarmRespect()
        {
            var sim = Tower(true); Do(sim, WordBattleAction.Start); Do(sim, WordBattleAction.Choose, Correct(sim)); Do(sim, WordBattleAction.Choose, Correct(sim));
            Assert.That(sim.WordBattles.MasteredTiers, Is.EqualTo(1));
            Do(sim, WordBattleAction.Close); Do(sim, WordBattleAction.Start, tier: 1, practice: true); Do(sim, WordBattleAction.Choose, Correct(sim)); Do(sim, WordBattleAction.Choose, Correct(sim));
            Assert.That(sim.WordBattles.MasteredTiers, Is.EqualTo(1)); Assert.That(sim.WordBattles.ActualTensionReduction, Is.Zero);
            foreach (var f in sim.Factions.Factions) if (f.Id == FactionIds.MerchantGuild) Assert.That(f.EarnedRespect, Is.EqualTo(.1f).Within(.0001f));
        }
        [Test] public void StaleCommandsCannotRepeatExchangeOrLearning()
        {
            var sim = Tower(); Do(sim, WordBattleAction.Start); var command = new WordBattleCommand(WordBattleAction.Choose, replyId: null, expectedRevision: sim.WordBattles.Revision);
            Assert.That(sim.ExecuteCommand(command).Accepted, Is.True); Assert.That(sim.ExecuteCommand(command).Accepted, Is.False); Assert.That(sim.WordBattles.LeaderScore, Is.EqualTo(1));
        }
        [Test] public void SuspendedSavePreservesChallengeScoresAndUnblocksSimulation()
        {
            var sim = Tower(true); Do(sim, WordBattleAction.Start); Do(sim, WordBattleAction.Choose, Correct(sim)); var challenge = sim.WordBattles.ChallengeText;
            long tick = sim.CurrentTick; sim.AdvanceOneTick(); Assert.That(sim.CurrentTick, Is.EqualTo(tick));
            Do(sim, WordBattleAction.Suspend); sim.AdvanceOneTick(); Assert.That(sim.CurrentTick, Is.EqualTo(tick + 1));
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData()); Assert.That(restored.WordBattles.Phase, Is.EqualTo(WordBattlePhase.Suspended)); Assert.That(restored.WordBattles.ChallengeText, Is.EqualTo(challenge));
            Do(restored, WordBattleAction.Resume); Do(restored, WordBattleAction.Choose, Correct(restored)); Assert.That(restored.WordBattles.Won, Is.True);
        }
        [Test] public void InvalidActiveSaveAndMissingSpeakerRetainLearningWithoutReward()
        {
            var sim = Tower(); Do(sim, WordBattleAction.Start); var data = sim.ExportSaveData(); data.wordBattles.stewardScore = 1;
            var restored = TowerSimulation.RestoreFromSaveData(data); Assert.That(restored.WordBattles.Phase, Is.EqualTo(WordBattlePhase.Idle)); Assert.That(restored.WordBattles.MasteredTiers, Is.Zero);
            data = sim.ExportSaveData(); data.wordBattles.speakerId = int.MaxValue; restored = TowerSimulation.RestoreFromSaveData(data); Assert.That(restored.WordBattles.Phase, Is.EqualTo(WordBattlePhase.Idle)); Assert.That(restored.WordBattles.LearnedReplies.Count, Is.EqualTo(1));
        }
        [Test] public void LoadedDefeatedTierRemainsPracticeWhenSavedFlagIsCorrupted()
        {
            var sim = Tower(true); Do(sim, WordBattleAction.Start); Do(sim, WordBattleAction.Choose, Correct(sim)); Do(sim, WordBattleAction.Choose, Correct(sim));
            Do(sim, WordBattleAction.Close); Do(sim, WordBattleAction.Start, tier: 1, practice: true); Do(sim, WordBattleAction.Suspend);
            var data = sim.ExportSaveData(); data.wordBattles.practice = false;
            var restored = TowerSimulation.RestoreFromSaveData(data);
            Assert.That(restored.WordBattles.Practice, Is.True); Do(restored, WordBattleAction.Resume); Do(restored, WordBattleAction.Choose, Correct(restored)); Do(restored, WordBattleAction.Choose, Correct(restored));
            Assert.That(restored.WordBattles.LearnedReplyId, Is.Null); Assert.That(restored.WordBattles.Transcript[restored.WordBattles.Transcript.Count - 2], Does.Contain("Practice"));
        }
        [Test] public void ResidentChatAcknowledgementTeachesAndCancellationDoesNot()
        {
            var sim = Tower(); int speaker = sim.Population.Persons[0].Id.Value;
            Assert.That(sim.ExecuteCommand(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: sim.WordBattles.Revision, speakerId: speaker)).Accepted, Is.True);
            Do(sim, WordBattleAction.Close); Assert.That(sim.WordBattles.LearnedReplies.Count, Is.EqualTo(1));
            Assert.That(sim.ExecuteCommand(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: sim.WordBattles.Revision, speakerId: speaker)).Accepted, Is.True);
            Do(sim, WordBattleAction.Choose, "wit:listen"); Assert.That(sim.WordBattles.LearnedReplies.Count, Is.EqualTo(2)); Assert.That(sim.WordBattles.MasteredTiers, Is.Zero);
        }
        [Test] public void CompletedResidentLessonLimitSurvivesSaveAndResetsNextDay()
        {
            var sim = Tower(); int speaker = sim.Population.Persons[0].Id.Value;
            Assert.That(sim.ExecuteCommand(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: sim.WordBattles.Revision, speakerId: speaker)).Accepted, Is.True);
            Do(sim, WordBattleAction.Choose, "wit:listen"); Do(sim, WordBattleAction.Close);
            sim = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            Assert.That(sim.CanExecute(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: sim.WordBattles.Revision, speakerId: speaker)).Accepted, Is.False);
            for (int i = 0; i < 1440; i++) sim.AdvanceOneTick();
            Assert.That(sim.ExecuteCommand(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: sim.WordBattles.Revision, speakerId: speaker)).Accepted, Is.True);
            Do(sim, WordBattleAction.Choose, "wit:listen"); Assert.That(sim.WordBattles.LearnedReplies.Count, Is.EqualTo(3));
        }
        [Test] public void CatalogAndOldSaveDefaultsAreValid()
        { Assert.DoesNotThrow(() => WordBattleCatalog.Validate(WordBattleCatalog.Replies)); var sim = Tower(); var data = sim.ExportSaveData(); data.wordBattles = null; Assert.That(TowerSimulation.RestoreFromSaveData(data).WordBattles.LearnedReplies.Count, Is.EqualTo(1)); }
        [Test] public void EarlyGuildSpokespersonRequiresEligibleOccupationWithoutInventingMembership()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            Assert.That(sim.Factions.Supports.Count, Is.Zero);
            Do(sim, WordBattleAction.Start);
            Assert.That(sim.WordBattles.SpeakerId, Is.EqualTo(sim.Population.Persons[0].Id.Value));
            Assert.That(sim.Factions.Supports.Count, Is.Zero);
            var data = TowerSimulation.CreateStandardFiveFloor().ExportSaveData();
            foreach (var room in data.rooms) if (room.contentType.StartsWith("commercial:") || room.contentType.StartsWith("service:")) room.contentType = "commercial:office";
            sim = TowerSimulation.RestoreFromSaveData(data);
            Assert.That(sim.CanExecute(new WordBattleCommand(WordBattleAction.Start, FactionIds.MerchantGuild, expectedRevision: 0)).Accepted, Is.False);
        }
        [Test] public void UnsupportedFactionAndAbsentGuildRepresentativeAreRejected()
        { var sim = TowerSimulation.CreateGroundFloorStart(); Assert.That(sim.CanExecute(new WordBattleCommand(WordBattleAction.Start, FactionIds.MerchantGuild, expectedRevision: 0)).Accepted, Is.False); sim = Tower(); Assert.That(sim.CanExecute(new WordBattleCommand(WordBattleAction.Start, FactionIds.TenantUnion, expectedRevision: 0)).Accepted, Is.False); }
    }
}
