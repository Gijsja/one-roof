using System.Collections;
using System.IO;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class WeatherVisibilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator GroundStart_RainAppearsInCameraCapture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A graphics device is required for visual capture.");

            var holder = new GameObject("Weather Visibility Capture");
            holder.SetActive(false);
            var controller = holder.AddComponent<TowerPlayableController>();
            typeof(TowerPlayableController).GetField("_startMode",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(controller, TowerStartMode.GroundFloorStart);
            holder.SetActive(true);
            try
            {
                yield return null;
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                var rain = controller.RainPresenter;
                Assert.That(rain, Is.Not.Null);

                var output = "/tmp/one-roof-rain-visibility";
                Directory.CreateDirectory(output);
                controller.SetWeatherOverride(0);
                // Compare weather against a settled room, not its 0.55-second construction
                // transition. A fixed 0.5-second wait can count the later backdrop/material
                // restoration as fog coverage, depending on the renderer's frame timing.
                var constructionDeadline = Time.realtimeSinceStartup + 3f;
                foreach (var transition in holder.GetComponentsInChildren<VisualEffectsPresenter>())
                {
                    while (transition != null && transition.IsTransitioning)
                    {
                        Assert.That(Time.realtimeSinceStartup, Is.LessThan(constructionDeadline),
                            "Room construction must settle before the weather reference capture.");
                        yield return null;
                    }
                }
                yield return new WaitForSeconds(0.5f);
                var clearPixels = Capture(camera, Path.Combine(output, "clear.png"));

                controller.SetWeatherOverride(2);
                yield return new WaitForSeconds(1.5f);
                Assert.That(rain.ActiveDropCount, Is.GreaterThan(0));
                Assert.That(rain.RainMeshRenderer, Is.Not.Null,
                    "World geometry creation must leave the rain mesh initialized.");
                Assert.That(rain.RainMeshRenderer.enabled, Is.True);
                var rainPixels = Capture(camera, Path.Combine(output, "rain.png"));
                var changedSkyPixels = 0;
                for (var y = 600; y < 720; y++)
                    for (var x = 0; x < 1280; x++)
                    {
                        var index = y * 1280 + x;
                        var clear = clearPixels[index];
                        var wet = rainPixels[index];
                        if (Mathf.Abs(clear.r - wet.r) > 35 ||
                            Mathf.Abs(clear.g - wet.g) > 35 ||
                            Mathf.Abs(clear.b - wet.b) > 35)
                            changedSkyPixels++;
                    }
                Assert.That(changedSkyPixels, Is.GreaterThan(500),
                    "Rain must be visible in the camera image, not only simulated offscreen.");

                controller.SetWeatherOverride(4);
                yield return new WaitForSeconds(1.5f);
                Assert.That(rain.Condition, Is.EqualTo(OneRoof.Domain.Weather.WeatherCondition.Fog));
                Assert.That(rain.ActiveFogPuffCount, Is.GreaterThan(0));
                Assert.That(rain.ActiveDropCount, Is.EqualTo(0),
                    "Fog must replace rain drops rather than layering rain over the tower.");
                var fogPixels = Capture(camera, Path.Combine(output, "fog.png"));
                var changedFogPixels = 0;
                for (var i = 0; i < clearPixels.Length; i++)
                {
                    var clear = clearPixels[i];
                    var fog = fogPixels[i];
                    if (Mathf.Abs(clear.r - fog.r) > 2 ||
                        Mathf.Abs(clear.g - fog.g) > 2 ||
                        Mathf.Abs(clear.b - fog.b) > 2)
                        changedFogPixels++;
                }
                Assert.That(changedFogPixels, Is.GreaterThan(100),
                    "Fog must affect the rendered camera image.");
                Assert.That(changedFogPixels, Is.LessThan(1280 * 720 / 10),
                    "Fog must not obscure more than ten percent of the rendered camera view.");
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }

        private static Color32[] Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(target);
                Object.Destroy(image);
            }
        }
    }
}
