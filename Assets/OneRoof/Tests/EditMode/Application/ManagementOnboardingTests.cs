using NUnit.Framework;
using OneRoof.Application.Modes;

namespace OneRoof.Tests.EditMode.Application
{
    public sealed class ManagementOnboardingTests
    {
        [Test]
        public void AdvancesOnlyOnObservedActionsAndMeasuredImprovement()
        {
            var lesson = new ManagementOnboarding();
            lesson.ObserveOverlay("overlay:elevator_wait");
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.NoticeQueue));

            lesson.ObserveQueue(8, 20f);
            lesson.ObserveOverlay("overlay:elevator_wait");
            lesson.ObserveInspector(true);
            lesson.ObservePreview(true, 25f);
            lesson.ObserveCapacityBuilt(true);
            lesson.ObserveMeasuredWait(20f);
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.MeasureImprovement));
            lesson.ObserveMeasuredQueue(5);
            lesson.ObserveManageMode(true);
            Assert.That(lesson.IsComplete, Is.True);
        }

        [Test]
        public void SkipAndRestartDoNotAdvanceTheLesson()
        {
            var lesson = new ManagementOnboarding();
            lesson.Skip();
            lesson.ObserveQueue(8, 20f);
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.NoticeQueue));
            lesson.Restart();
            lesson.ObserveQueue(8, 20f);
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.OpenOverlay));
        }
    }
}
