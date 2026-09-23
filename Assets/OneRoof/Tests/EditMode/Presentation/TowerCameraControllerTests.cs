using NUnit.Framework;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerCameraControllerTests
    {
        [Test]
        public void EnsureTowerCamera_GroundStartFramesTheSingleFloor()
        {
            Camera cam = null;
            try
            {
                cam = TowerCameraController.EnsureTowerCamera(1, null, resetView: true);
                Assert.That(cam.orthographicSize, Is.EqualTo(3.2f).Within(0.001f));
                Assert.That(cam.transform.position.x, Is.EqualTo(-4.6f).Within(0.001f));
            }
            finally
            {
                if (cam != null) Object.DestroyImmediate(cam.gameObject);
            }
        }

        [Test]
        public void CameraController_PanBy_ClampsWithinConfiguredBounds()
        {
            var go = new GameObject("TestCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                var ctrl = go.AddComponent<TowerCameraController>();
                ctrl.Camera = cam;
                ctrl.SetBounds(-10f, 10f, 0f, 20f);
                go.transform.position = new Vector3(0f, 5f, -10f);

                // Pan within bounds
                ctrl.PanBy(new Vector2(3f, 2f));
                Assert.That(go.transform.position.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(go.transform.position.y, Is.EqualTo(7f).Within(0.001f));

                // Pan exceeding max bounds
                ctrl.PanBy(new Vector2(20f, 50f));
                Assert.That(go.transform.position.x, Is.EqualTo(10f).Within(0.001f));
                Assert.That(go.transform.position.y, Is.EqualTo(20f).Within(0.001f));

                // Pan exceeding min bounds
                ctrl.PanBy(new Vector2(-50f, -50f));
                Assert.That(go.transform.position.x, Is.EqualTo(-10f).Within(0.001f));
                Assert.That(go.transform.position.y, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CameraController_ZoomBy_ClampsBetweenMinAndMax()
        {
            var go = new GameObject("TestCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.8f;

                var ctrl = go.AddComponent<TowerCameraController>();
                ctrl.Camera = cam;
                ctrl.MinOrthographicSize = 3.2f;
                ctrl.MaxOrthographicSize = 15.0f;

                // Zoom in exceeding min
                ctrl.ZoomBy(-10.0f);
                Assert.That(cam.orthographicSize, Is.EqualTo(3.2f).Within(0.001f));

                // Zoom out exceeding max
                ctrl.ZoomBy(30.0f);
                Assert.That(cam.orthographicSize, Is.EqualTo(15.0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CameraController_FocusOverview_RestoresDefaults()
        {
            var go = new GameObject("TestCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;

                var ctrl = go.AddComponent<TowerCameraController>();
                ctrl.Camera = cam;
                ctrl.SetOverviewDefaults(new Vector3(-1.6f, 5f, -10f), 7.5f);

                // Mutate position and zoom
                go.transform.position = new Vector3(8f, 18f, -10f);
                cam.orthographicSize = 3.5f;

                // Focus overview
                ctrl.FocusOverview();
                Assert.That(go.transform.position.x, Is.EqualTo(-1.6f).Within(0.001f));
                Assert.That(go.transform.position.y, Is.EqualTo(5.0f).Within(0.001f));
                Assert.That(cam.orthographicSize, Is.EqualTo(7.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnsureTowerCamera_PreservesPositionAndZoom_WhenResetViewIsFalse()
        {
            Camera cam = null;
            try
            {
                cam = TowerCameraController.EnsureTowerCamera(5, null, resetView: true);
                var ctrl = cam.GetComponent<TowerCameraController>();
                var initialBoundsY = ctrl.BoundsY.y;

                // User pans and zooms
                cam.transform.position = new Vector3(4.0f, 6.0f, -10f);
                cam.orthographicSize = 4.2f;

                // Subsequent call (e.g. per-frame or building expansion) with resetView: false
                TowerCameraController.EnsureTowerCamera(7, null, resetView: false);

                // View must remain intact
                Assert.That(cam.transform.position.x, Is.EqualTo(4.0f).Within(0.001f));
                Assert.That(cam.transform.position.y, Is.EqualTo(6.0f).Within(0.001f));
                Assert.That(cam.orthographicSize, Is.EqualTo(4.2f).Within(0.001f));

                // But bounds must have expanded for 7 floors
                Assert.That(ctrl.BoundsY.y, Is.GreaterThan(initialBoundsY));
            }
            finally
            {
                if (cam != null)
                {
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
        }

        [Test]
        public void EnsureTowerCamera_ResetsPositionAndZoom_WhenResetViewIsTrue()
        {
            Camera cam = null;
            try
            {
                cam = TowerCameraController.EnsureTowerCamera(5, null, resetView: true);

                // User pans and zooms
                cam.transform.position = new Vector3(4.0f, 6.0f, -10f);
                cam.orthographicSize = 4.2f;

                // Reset view (e.g. simulation reset or focus)
                TowerCameraController.EnsureTowerCamera(5, null, resetView: true);

                Assert.That(cam.transform.position.x, Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(cam.orthographicSize, Is.EqualTo(Mathf.Max(6.8f, 6 * 1.15f)).Within(0.001f));
            }
            finally
            {
                if (cam != null)
                {
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
        }

        [Test]
        public void EnsureTowerCamera_AddsAnAudioListenerForSpatialTowerSound()
        {
            Camera cam = null;
            try
            {
                cam = TowerCameraController.EnsureTowerCamera(5, null, resetView: true);

                Assert.That(cam.GetComponent<AudioListener>(), Is.Not.Null);
            }
            finally
            {
                if (cam != null)
                {
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
        }
    }
}
