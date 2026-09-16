using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.UI.Modes;
using UnityEngine;

namespace OneRoof.UI.Tests.EditMode
{
    public sealed class ModeShellBarControllerTests
    {
        private GameObject _holder;
        private ModeShellBarController _controller;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("ModeShellBarTestHolder");
            _controller = _holder.AddComponent<ModeShellBarController>();
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
    }
}
