using System.Collections;
using System.IO;
using NUnit.Framework;
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
                Assert.That(tower.GetComponent<NpcViewPool>().TotalInstantiatedCount, Is.LessThanOrEqualTo(60));
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
