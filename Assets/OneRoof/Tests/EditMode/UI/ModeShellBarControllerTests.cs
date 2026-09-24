using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.UI.Modes;
using UnityEngine;

namespace OneRoof.UI.Tests.EditMode
{
    public sealed class ModeShellBarControllerTests
    {
        [Test]
        public void BuildPaletteRect_LeavesTheDashboardClearAtEditorGameViewHeight()
        {
            var palette = ModeShellBarController.BuildPaletteRect(718);
            Assert.That(palette.yMin, Is.GreaterThanOrEqualTo(292f));
            Assert.That(palette.yMax, Is.LessThanOrEqualTo(610f));
        }

        [Test]
        public void IsPointerOverControls_UsesScreenCoordinatesAgainstVisibleModeBar()
        {
            var projection = new ModeShellProjection(
                InteractionMode.Inspect, InteractionMode.Inspect, null, null, null, null, null, null);
            const int height = 718;
            var imguiPoint = new Vector2(30f, ModeShellBarController.ModeBarRect(height).y + 10f);
            var screenPoint = new Vector2(imguiPoint.x, height - imguiPoint.y);

            Assert.That(ModeShellBarController.IsPointerOverControls(screenPoint, height, projection), Is.True);
            Assert.That(ModeShellBarController.IsPointerOverControls(new Vector2(900f, 500f), height, projection), Is.False);
        }

        private GameObject _holder;
        private ModeShellBarController _controller;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("ModeShellBarTestHolder");
            _controller = _holder.AddComponent<ModeShellBarController>();
            _controller.Session = new ModeShellSession();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void Controller_DefaultsToInspectMode()
        {
            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Inspect));
            Assert.That(_controller.CurrentProjection.IsInspectMode, Is.True);
        }

        [Test]
        public void Controller_DoesNotAllocateSessionBeforeInjection()
        {
            _controller.Session = null;
            Assert.That(_controller.Session, Is.Null);
            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Inspect));
        }

        [Test]
        public void Controller_ExternalSessionInjection_BindsProperly()
        {
            var session = new ModeShellSession();
            session.SwitchMode(InteractionMode.Data);

            _controller.Session = session;

            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Data));
            Assert.That(_controller.CurrentProjection.IsDataMode, Is.True);
        }

        [Test]
        public void Controller_ModeTransitions_ReflectedInProjection()
        {
            _controller.Session.SwitchMode(InteractionMode.Build);
            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Build));

            _controller.Session.SelectBuildTool("transit:elevator_car");
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.EqualTo("transit:elevator_car"));

            _controller.Session.CancelOrEscape();
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.Null);
        }

        [Test]
        public void Controller_OnBuildModeRequested_OpensPaletteWithoutPreselectingTool()
        {
            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Inspect));
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.Null);

            _controller.OnBuildModeRequested();

            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.Null);
        }

        [Test]
        public void Controller_OnBuildModeRequested_ReopensPaletteByClearingExistingToolSelection()
        {
            _controller.Session.SelectBuildTool("commercial:diner");
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.EqualTo("commercial:diner"));

            _controller.OnBuildModeRequested();

            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.Null);
        }

        [Test]
        public void Controller_OnBuildModeRequested_ExitsBuildWhenPaletteAlreadyOpen()
        {
            _controller.OnBuildModeRequested();
            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(_controller.CurrentProjection.SelectedBuildTool, Is.Null);

            _controller.OnBuildModeRequested();

            Assert.That(_controller.ActiveMode, Is.EqualTo(InteractionMode.Inspect));
        }

        [Test]
        public void BuildModeStatusText_ManageMode_NamesUpcomingLeversInsteadOfDeadEnd()
        {
            var projection = new ModeShellProjection(
                InteractionMode.Manage, InteractionMode.Inspect, null, null, null, null, null, null);

            var text = ModeShellBarController.BuildModeStatusText(projection);

            Assert.That(text, Does.Contain("MODE: MANAGE"));
            Assert.That(text, Does.Contain("Steward policies are coming soon"));
        }

        [Test]
        public void BuildModeStatusText_InspectModeWithEntity_NamesSelectedEntity()
        {
            var projection = new ModeShellProjection(
                InteractionMode.Inspect, InteractionMode.Inspect, null, null, null, 42, null, null);

            Assert.That(ModeShellBarController.BuildModeStatusText(projection), Does.Contain("Entity: #42"));
        }

        [Test]
        public void BuildModeStatusText_DataModeWithOverlay_NamesActiveOverlay()
        {
            var projection = new ModeShellProjection(
                InteractionMode.Data, InteractionMode.Inspect, null, null, null, null, null, "overlay:utilities");

            Assert.That(ModeShellBarController.BuildModeStatusText(projection), Does.Contain("Overlay: UTILITIES"));
        }
    }
}
