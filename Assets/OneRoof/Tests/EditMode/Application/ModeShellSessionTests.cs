using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Modes.Commands;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class ModeShellSessionTests
    {
        [Test]
        public void InitialState_DefaultsToInspectModeWithNoSelection()
        {
            var session = new ModeShellSession();
            var projection = session.Projection();

            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Inspect));
            Assert.That(projection.IsInspectMode, Is.True);
            Assert.That(projection.IsBuildMode, Is.False);
            Assert.That(projection.IsDataMode, Is.False);
            Assert.That(projection.SelectedBuildTool, Is.Null);
            Assert.That(projection.ActiveOverlayId, Is.Null);
            Assert.That(projection.SelectedEntityId, Is.Null);
        }

        [Test]
        public void SwitchMode_TransitionsModeAndStoresPrevious()
        {
            var session = new ModeShellSession();

            session.SwitchMode(InteractionMode.Build);
            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(session.Projection().PreviousMode, Is.EqualTo(InteractionMode.Inspect));

            session.SwitchMode(InteractionMode.Data);
            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Data));
            Assert.That(session.Projection().PreviousMode, Is.EqualTo(InteractionMode.Build));
        }

        [Test]
        public void SelectBuildTool_SetsToolAndBuildMode()
        {
            var session = new ModeShellSession();

            session.SelectBuildTool("transit:elevator_car");
            var projection = session.Projection();

            Assert.That(projection.CurrentMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(projection.SelectedBuildTool, Is.EqualTo("transit:elevator_car"));
        }

        [Test]
        public void SelectEntity_SetsSelectedEntityAndFloor()
        {
            var session = new ModeShellSession();

            session.SelectEntity(42, floor: 2);
            var projection = session.Projection();

            Assert.That(projection.SelectedEntityId, Is.EqualTo(42));
            Assert.That(projection.SelectedFloor, Is.EqualTo(2));
            Assert.That(projection.IsInspectMode, Is.True);
        }

        [Test]
        public void SetActiveOverlay_SetsOverlayAndDataMode()
        {
            var session = new ModeShellSession();

            session.SetActiveOverlay("overlay:elevator_wait");
            var projection = session.Projection();

            Assert.That(projection.CurrentMode, Is.EqualTo(InteractionMode.Data));
            Assert.That(projection.ActiveOverlayId, Is.EqualTo("overlay:elevator_wait"));
        }

        [Test]
        public void CancelOrEscape_ClearsToolFirst_ThenReturnsToInspect()
        {
            var session = new ModeShellSession();
            session.SelectBuildTool("transit:elevator_car");

            // First escape: clears tool, remains in Build mode
            session.CancelOrEscape();
            Assert.That(session.Projection().SelectedBuildTool, Is.Null);
            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Build));

            // Second escape: returns to Inspect mode
            session.CancelOrEscape();
            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Inspect));
        }

        [Test]
        public void ModeChangedEvent_FiresOnStateTransition()
        {
            var session = new ModeShellSession();
            var eventCount = 0;
            ModeShellProjection lastProjection = null;

            session.ModeChanged += p =>
            {
                eventCount++;
                lastProjection = p;
            };

            session.SwitchMode(InteractionMode.Data);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(lastProjection, Is.Not.Null);
            Assert.That(lastProjection.CurrentMode, Is.EqualTo(InteractionMode.Data));
        }

        [Test]
        public void ExecuteCommand_NullCommand_ThrowsArgumentNullException()
        {
            var session = new ModeShellSession();
            Assert.Throws<ArgumentNullException>(() => session.ExecuteCommand(null));
        }
    }
}
