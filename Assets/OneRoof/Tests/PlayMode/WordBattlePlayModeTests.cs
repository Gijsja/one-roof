using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Domain.Dialogue;
using OneRoof.Domain.Commands;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class WordBattlePlayModeTests : OwnedPlayModeSceneFixture
    {
        [UnityTest]
        public IEnumerator ResidentChatOwnsModalInputAndRestoresPauseAfterLearning()
        {
            yield return LoadTestScene("Assets/Scenes/Tower_GoldStandard30.unity");
            yield return null;
            var tower = Object.FindFirstObjectByType<GoldStandardPlayground>().Tower;
            tower.SetPaused(false);
            var panel = tower.WordBattlePanel;
            var session = tower.SimulationSession;
            var residentId = session.TransitProjection().Residents[0].ResidentId;
            var initialMode = tower.ModeSession.CurrentMode;
            var learned = session.WordBattleProjection().LearnedReplies.Count;
            Assert.That(panel.TeachResident(residentId), Is.True);
            panel.SuspendOrClose();
            Assert.That(session.WordBattleProjection().LearnedReplies.Count, Is.EqualTo(learned), "Canceling an unfinished lesson must grant nothing.");
            Assert.That(tower.IsPaused, Is.False);
            Assert.That(panel.TeachResident(residentId), Is.True);
            Assert.That(tower.IsPaused, Is.True);
            Assert.That(tower.CaretakerInputBlocked, Is.True);
            tower.CaretakerPresenter.ToggleContact();
            Assert.That(tower.CaretakerPresenter.ContactOpen, Is.False, "A second modal cannot open over a Resident chat.");
            if (System.Environment.GetEnvironmentVariable("ONEROOF_CAPTURE_WORD_BATTLE") == "1")
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "word-battle-chat.png"));
                yield return null;
                yield return null;
            }
            tower.ModeSession.SwitchMode(InteractionMode.Build);
            Assert.That(tower.ModeSession.CurrentMode, Is.EqualTo(initialMode));
            var chat = session.WordBattleProjection();
            Assert.That(panel.Choose(chat.Choices[0].Id), Is.True);
            Assert.That(session.WordBattleProjection().Phase, Is.EqualTo(WordBattlePhase.Result));
            panel.SuspendOrClose();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(tower.IsPaused, Is.False);
            Assert.That(tower.ModeSession.IsInputBlocked, Is.False);
            Assert.That(panel.TeachResident(residentId), Is.False, "A completed lesson uses the daily learning allowance.");
            Assert.That(panel.StartBattle(), Is.True);
            if (System.Environment.GetEnvironmentVariable("ONEROOF_CAPTURE_WORD_BATTLE") == "1")
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "word-battle-active.png"));
                yield return null;
                yield return null;
            }
            for (var exchange = 0; exchange < 2; exchange++)
            {
                var battle = session.WordBattleProjection();
                string correct = null;
                foreach (var reply in WordBattleCatalog.Replies)
                    if (reply.Challenge == battle.ChallengeText || reply.Variant == battle.ChallengeText) correct = reply.Id;
                Assert.That(correct, Is.Not.Null);
                Assert.That(panel.Choose(correct), Is.True);
            }
            Assert.That(session.WordBattleProjection().Won, Is.True);
            Assert.That(session.WordBattleProjection().MasteredTiers, Is.EqualTo(1));
            if (System.Environment.GetEnvironmentVariable("ONEROOF_CAPTURE_WORD_BATTLE") == "1")
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "word-battle-result.png"));
                yield return null;
                yield return null;
            }
            panel.enabled = false;
            Assert.That(session.WordBattleProjection().Phase, Is.EqualTo(WordBattlePhase.Idle), "Disabling the result panel must release domain input ownership.");
            Assert.That(tower.IsPaused, Is.False);
            panel.enabled = true;
            tower.SetPaused(true);
        }
    }
}
