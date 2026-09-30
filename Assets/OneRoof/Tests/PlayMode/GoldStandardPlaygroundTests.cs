using System.Collections;
using System.IO;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Transit;
using OneRoof.Presentation.Tower;
using OneRoof.Presentation.Population;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace OneRoof.Tests.PlayMode
{
    public sealed class GoldStandardPlaygroundTests
    {
        [UnityTest]
        public IEnumerator AuthoredScene_FacadeToggleAndLodRemainConnectedAcrossZoomLevels()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Tower_GoldStandard30.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var playground = Object.FindFirstObjectByType<GoldStandardPlayground>();
            var tower = playground.Tower;
            var camera = Camera.main;
            try
            {
                tower.SetPaused(true);
                camera.GetComponent<TowerCameraController>().enabled = false;
                var activeCount = 0;
                foreach (var resident in tower.SimulationSession.Projection().Residents)
                    if (resident.Status != TransitResidentStatus.Outside) activeCount++;
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.Auto);
                camera.GetComponent<TowerCameraController>().FocusOverview();
                yield return null;
                Assert.That(tower.ExteriorPresenter.FacadeEnvelopeAlpha, Is.EqualTo(1f));
                Assert.That(tower.RoomPresenter.ClutterAlpha, Is.Zero);
                Assert.That(tower.FloorDeckPresenter.Decks[0].gameObject.activeSelf, Is.True);
                Assert.That(tower.FloorDeckPresenter.Decks[29].gameObject.activeSelf, Is.False);
                Assert.That(tower.ResidentPresenter.VisibleResidentCount, Is.EqualTo(activeCount));
                Assert.That(tower.ResidentPresenter.VisibleMacroCount, Is.EqualTo(activeCount));
                var directory = "/tmp/one-roof-lod-visuals";
                Directory.CreateDirectory(directory);
                Capture(camera, Path.Combine(directory, "facade-overview.png"));
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.LockedCutaway);
                yield return null;
                Assert.That(tower.ExteriorPresenter.FrontFacadeRoot.gameObject.activeSelf, Is.False);
                Assert.That(tower.RoomPresenter.ClutterAlpha, Is.EqualTo(1f));
                Assert.That(tower.FloorDeckPresenter.Decks[29].gameObject.activeSelf, Is.True);
                Assert.That(tower.ResidentPresenter.VisibleResidentCount, Is.EqualTo(activeCount));
                Capture(camera, Path.Combine(directory, "cutaway-overview.png"));
                playground.FrameStreet();
                camera.orthographicSize = 5f;
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.LockedFacade);
                yield return null;
                Assert.That(tower.ExteriorPresenter.FacadeEnvelopeAlpha, Is.EqualTo(1f));
                Assert.That(tower.ResidentPresenter.VisibleRigCount, Is.LessThanOrEqualTo(tower.GetComponent<NpcViewPool>().MaxCapacity));
                Capture(camera, Path.Combine(directory, "facade-street-close.png"));
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.LockedCutaway);
                yield return null;
                Capture(camera, Path.Combine(directory, "cutaway-street-close.png"));
                camera.orthographicSize = 13f;
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.Auto);
                yield return null;
                Assert.That(tower.ExteriorPresenter.FacadeEnvelopeAlpha, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(tower.RoomPresenter.ClutterAlpha, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(tower.ResidentPresenter.VisibleSpriteCount, Is.GreaterThan(0));
                Capture(camera, Path.Combine(directory, "crossfade-middle.png"));
                camera.GetComponent<TowerCameraController>().FocusOverview();
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.LockedFacade);
                for (var frame = 0; frame < 30; frame++) yield return null;
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    var elapsed = 0f; var drawCalls = 0;
                    var performanceTarget = new RenderTexture(1920, 1080, 24);
                    camera.targetTexture = performanceTarget;
                    try
                    {
                        for (var frame = 0; frame < 150; frame++)
                        {
                            yield return null;
                            if (frame < 30) continue;
                            elapsed += Time.unscaledDeltaTime;
                            drawCalls += UnityEditor.UnityStats.drawCalls;
                        }
                    }
                    finally { camera.targetTexture = null; Object.Destroy(performanceTarget); }
                    var rendererReport = new System.Text.StringBuilder();
                    var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                    foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        if (renderer.enabled && renderer.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                        {
                            var meshFilter = renderer.GetComponent<MeshFilter>();
                            rendererReport.AppendLine(renderer.name + " | " + (renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "none") + " | " + (meshFilter != null && meshFilter.sharedMesh != null ? meshFilter.sharedMesh.name : "sprite/text"));
                        }
                    File.WriteAllText(Path.Combine(directory, "renderers.txt"), rendererReport.ToString());
                    var tickTimer = System.Diagnostics.Stopwatch.StartNew();
                    for (var tick = 0; tick < 300; tick++) tower.SimulationSession.AdvanceOneTick();
                    tickTimer.Stop();
                    File.WriteAllText(Path.Combine(directory, "performance.txt"),
                        "Average presentation frame ms: " + (elapsed / 120f * 1000f) +
                        "\nAverage draw calls: " + (drawCalls / 120f) +
                        "\nVisible residents: " + tower.ResidentPresenter.VisibleResidentCount +
                        "\nAverage simulation tick ms (300 ticks): " + tickTimer.Elapsed.TotalMilliseconds / 300d +
                        "\nGPU: " + SystemInfo.graphicsDeviceName);
                    yield return null;
                    Capture(camera, Path.Combine(directory, "facade-after-simulation.png"));
                }
            }
            finally
            {
                Object.Destroy(tower.gameObject); Object.Destroy(playground.gameObject);
                if (camera != null) Object.Destroy(camera.gameObject);
            }
#else
            Assert.Ignore("Editor scene validation only.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator AuthoredScene_StartsFilledResetsAndRendersTheLivingCity()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Tower_GoldStandard30.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var playground = Object.FindFirstObjectByType<GoldStandardPlayground>();
            Assert.That(playground, Is.Not.Null);
            var tower = playground.Tower;
            try
            {
                Assert.That(tower.SimulationSession.FloorCount, Is.EqualTo(30));
                Assert.That(tower.SimulationSession.ResidentCount, Is.EqualTo(300));
                Assert.That(tower.StructurePresenter.RenderedFloorCount, Is.EqualTo(30));
                Assert.That(playground.Environment.TrafficCount, Is.EqualTo(12));
                Assert.That(playground.Environment.GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(tower.GetComponent<NpcViewPool>().TotalInstantiatedCount, Is.LessThanOrEqualTo(tower.GetComponent<NpcViewPool>().MaxCapacity));
                var clock = playground.Environment.AnimationClock;
                yield return new WaitForSeconds(.1f);
                Assert.That(playground.Environment.AnimationClock, Is.GreaterThan(clock));
                tower.SetPaused(true);
                clock = playground.Environment.AnimationClock;
                yield return null;
                Assert.That(playground.Environment.AnimationClock, Is.EqualTo(clock));
                tower.SetWeatherOverride(0);
                yield return new WaitForSeconds(1f);
                var camera = Camera.main;
                camera.GetComponent<TowerCameraController>().FocusOverview();
                Assert.That(camera.orthographicSize, Is.GreaterThan(30));
                var directory = "/tmp/one-roof-gold-city";
                Directory.CreateDirectory(directory);
                Capture(camera, Path.Combine(directory, "overview.png"));
                camera.transform.position = new Vector3(2, 1, -10);
                camera.orthographicSize = 9;
                Capture(camera, Path.Combine(directory, "street.png"));
                while (tower.SimulationSession.CurrentTick < 1230) tower.SimulationSession.AdvanceOneTick();
                yield return null;
                Capture(camera, Path.Combine(directory, "street-night.png"));
                camera.GetComponent<TowerCameraController>().FocusOverview();
                Capture(camera, Path.Combine(directory, "overview-night.png"));
                tower.ResetCommuteSimulation();
                yield return null;
                Assert.That(tower.SimulationSession.FloorCount, Is.EqualTo(30));
                Assert.That(tower.SimulationSession.ResidentCount, Is.EqualTo(300));
                Assert.That(Object.FindObjectsByType<GoldCityEnvironment>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(tower.gameObject);
                Object.Destroy(playground.gameObject);
                if (Camera.main != null) Object.Destroy(Camera.main.gameObject);
            }
#else
            Assert.Ignore("Editor scene validation only.");
            yield break;
#endif
        }

        private static void Capture(Camera camera, string path)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var target = new RenderTexture(1920,1080,24);
            var image = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                camera.Render();
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0);
                image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;
                RenderTexture.active=previousActive;
                Object.Destroy(target);Object.Destroy(image);
            }
        }
    }
}
