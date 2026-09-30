using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Domain.Transit;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class TowerAtmosphereAndElevatorLimitsPlayModeTests
    {
        [UnityTest]
        public IEnumerator Facade_FollowsCameraZoom_AndKeepsInteractionModesOpen()
        {
            var holder = new GameObject("Facade camera integration test");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                yield return null;
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                camera.GetComponent<TowerCameraController>().enabled = false;
                controller.ModeSession.SwitchMode(InteractionMode.Manage);
                camera.orthographicSize = 18f;
                yield return null;
                Assert.That(controller.ExteriorPresenter.FacadeEnvelopeAlpha, Is.EqualTo(1f));
                camera.orthographicSize = 8f;
                yield return null;
                Assert.That(controller.ExteriorPresenter.FrontFacadeRoot.gameObject.activeSelf, Is.False);
                camera.orthographicSize = 18f;
                foreach (var mode in new[] { InteractionMode.Build, InteractionMode.Inspect, InteractionMode.Data })
                {
                    controller.ModeSession.SwitchMode(mode);
                    if (mode == InteractionMode.Inspect) controller.ModeSession.SelectEntity(1);
                    yield return null;
                    Assert.That(controller.ExteriorPresenter.FrontFacadeRoot.gameObject.activeSelf, Is.False);
                }
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }

        [UnityTest]
        public IEnumerator Facade_FloorRemovalRetiresGeometryImmediately_AndSurvivesDeferredDestruction()
        {
            var holder = new GameObject("Facade geometry lifecycle test");
            var material = new Material(Shader.Find("OneRoof/Unlit"));
            var presenter = new BuildingExteriorPresenter();
            try
            {
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                var slabs = new System.Collections.Generic.Dictionary<int, OneRoof.Domain.Topology.CellBounds>();
                for (var f = 0; f < 3; f++) slabs[f] = new OneRoof.Domain.Topology.CellBounds(f, -14, 16);
                presenter.EnsureExteriorViews(new OneRoof.Application.Tower.TowerTopologyProjection(slabs));
                var root = presenter.Root;
                var removedWall = root.Find("Left Facade/Left Wall 2");
                slabs.Remove(2);
                presenter.EnsureExteriorViews(new OneRoof.Application.Tower.TowerTopologyProjection(slabs));
                Assert.That(removedWall.gameObject.activeSelf, Is.False);
                Assert.That(root.Find("Left Facade/Left Wall 2"), Is.Null);
                yield return null;
                Assert.That(presenter.Root, Is.SameAs(root));
                Assert.That(presenter.FrontFacadeRoot, Is.Not.Null);
                Assert.That(presenter.LeftWindowFrames.Count, Is.EqualTo(2));
            }
            finally
            {
                presenter.Dispose();
                Object.Destroy(material);
                Object.Destroy(holder);
            }
        }

        [UnityTest]
        public IEnumerator TowerController_RestoresModeRoutingAfterDisableAndReenable()
        {
            var holder = new GameObject("Tower Controller Lifecycle Test");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                yield return null;

                controller.enabled = false;
                controller.enabled = true;
                yield return null;

                controller.ModeSession.SwitchMode(InteractionMode.Data);
                controller.ModeSession.SetActiveOverlay("overlay:satisfaction");
                Assert.That(controller.SatisfactionPresenter.IsVisible, Is.True);
                Assert.That(controller.OverlayPresenter.IsVisible, Is.False);

                controller.ModeSession.SwitchMode(InteractionMode.Build);
                Assert.That(controller.SatisfactionPresenter.IsVisible, Is.False);
                Assert.That(controller.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Build));
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }

        [UnityTest]
        public IEnumerator TowerPresentation_ShowsWindowLightConesAndKeepsThreeCarsInsideTheShaft()
        {
            var holder = new GameObject("Tower Atmosphere And Elevator Limits Test");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                yield return null;

                var atmosphere = controller.AtmospherePresenter;
                Assert.That(atmosphere.WindowLightCount, Is.GreaterThan(0));
                foreach (var renderer in atmosphere.WindowLightRenderers)
                {
                    Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.transform.position.z, Is.LessThan(0.7f));
                }

                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.True);
                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.True);
                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.False);
                Assert.That(controller.TransitSession.ElevatorCarCount, Is.EqualTo(ElevatorBank.MaxCarsPerBank));

                controller.SyncPresenterGeometry();
                Assert.That(controller.ElevatorPresenter.ElevatorViews.Count, Is.EqualTo(3));
                for (var i = 0; i < controller.ElevatorPresenter.ElevatorViews.Count; i++)
                {
                    var car = controller.ElevatorPresenter.ElevatorViews[i];
                    var left = car.transform.position.x - car.transform.localScale.x * 0.5f;
                    var right = car.transform.position.x + car.transform.localScale.x * 0.5f;
                    Assert.That(left, Is.GreaterThanOrEqualTo(-2.36f));
                    Assert.That(right, Is.LessThanOrEqualTo(-1.44f));
                }
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }
    }
}
