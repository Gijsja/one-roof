using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Dialogue;
using OneRoof.Domain.Commands;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class WordBattleProjectionTests
    {
        [Test]
        public void ResidentLearningUsesCommandsAndInvalidatesImmutableProjection()
        {
            var session = new TowerSimulationSession();
            var initial = session.WordBattleProjection();
            Assert.That(session.WordBattleProjection(), Is.SameAs(initial));
            var residentId = session.TransitProjection().Residents[0].ResidentId;
            var start = session.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Teach, speakerId: residentId, expectedRevision: initial.Revision));
            Assert.That(start.Accepted, Is.True, start.Reason);
            var chat = session.WordBattleProjection();
            Assert.That(chat.Phase, Is.EqualTo(WordBattlePhase.ResidentChat));
            Assert.That(chat.SpeakerId, Is.EqualTo(residentId));
            Assert.That(chat.Choices.Count, Is.EqualTo(1));
            Assert.That(session.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Choose, replyId: chat.Choices[0].Id, expectedRevision: chat.Revision)).Accepted, Is.True);
            var result = session.WordBattleProjection();
            Assert.That(result.LearnedReplies.Count, Is.EqualTo(initial.LearnedReplies.Count + 1));
            Assert.That(initial.LearnedReplies.Count, Is.EqualTo(1), "Published snapshots must not change in place.");
            Assert.That(result.ActualTensionReduction, Is.Zero);
            Assert.That(session.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Choose, replyId: chat.Choices[0].Id, expectedRevision: chat.Revision)).Accepted, Is.False);
        }
    }
}
