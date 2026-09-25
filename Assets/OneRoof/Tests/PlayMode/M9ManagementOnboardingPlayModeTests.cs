using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class M9ManagementOnboardingPlayModeTests
    {
        [UnityTest]
        public IEnumerator FirstManagementLessonFollowsActualTowerActions()
        {
            var holder = new GameObject("M9 Onboarding Tower");
            var controller = holder.AddComponent<TowerPlayableController>();
            controller.Initialize();
            controller.SetPaused(true);
            var lesson = controller.Onboarding;
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.NoticeQueue));

            for (var i = 0; i < 120 && lesson.CurrentStep == ManagementOnboarding.Step.NoticeQueue; i++)
            {
                controller.SimulationSession.AdvanceOneTick();
                yield return null;
            }
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.OpenOverlay));
            controller.ToggleDataOverlay();
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.InspectCause));
            controller.InspectBottleneck();
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.PreviewCapacity));
            controller.ShowPlacementPreview();
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.BuildCapacity));
            controller.OnConfirmElevatorPlacement();
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.MeasureImprovement));

            for (var i = 0; i < 300 && lesson.CurrentStep == ManagementOnboarding.Step.MeasureImprovement; i++)
            {
                controller.SimulationSession.AdvanceOneTick();
                yield return null;
            }
            Assert.That(lesson.CurrentStep, Is.EqualTo(ManagementOnboarding.Step.OpenManage));
            controller.ModeSession.SwitchMode(InteractionMode.Manage);
            Assert.That(lesson.IsComplete, Is.True);

            Object.Destroy(holder);
            var camera = GameObject.Find("Tower Camera");
            if (camera != null) Object.Destroy(camera);
            yield return null;
        }
    }
}
