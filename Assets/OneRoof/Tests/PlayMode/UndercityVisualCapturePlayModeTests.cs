using System.Collections;
using System.IO;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class UndercityVisualCapturePlayModeTests
    {
        [UnityTest]
        public IEnumerator UndercityVisualCapture_TowerArrivalAndConnectedRoom()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A graphics device is required for visual capture; headless behavioral tests run separately.");
            var holder = new GameObject("Undercity Visual Capture");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                yield return null;
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                var output = "/tmp/one-roof-undercity-visual";
                Directory.CreateDirectory(output);
                yield return new WaitForSeconds(1.5f);
                Capture(camera, Path.Combine(output, "tower-street-01.png"));
                yield return new WaitForSeconds(.7f);
                Capture(camera, Path.Combine(output, "tower-street-02.png"));

                var session = controller.SimulationSession;
                for (var depth = 0; depth <= 3; depth++) BuildCell(session, 16, depth);
                for (var x = 17; x <= 20; x++) BuildCell(session, x, 3);
                for (var x = 18; x <= 20; x++)
                    for (var depth = 1; depth <= 2; depth++) BuildCell(session, x, depth);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCoreCommand(16, 3)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCorridorCommand(17, 3, 4)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new ZoneUndergroundRoomCommand(UndergroundRoomType.AccessHub, 18, 1, 3, 2)).Accepted, Is.True);
                for (var tick = 0; tick < 1440; tick++) session.AdvanceOneTick();
                Assert.That(session.UndergroundOperationsProjection().Rooms[0].Staff, Is.GreaterThan(0));
                controller.SyncPresenterGeometry();
                camera.GetComponent<TowerCameraController>().FocusUnderground();
                yield return new WaitForSeconds(2f);
                Capture(camera, Path.Combine(output, "undercity-room-01.png"));
                Assert.That(File.Exists(Path.Combine(output, "undercity-room-01.png")), Is.True);
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }

        private static void BuildCell(OneRoof.Application.Tower.TowerSimulationSession session, int x, int depth)
        {
            Assert.That(session.ExecuteCommand(new DigUndergroundCommand(x, depth, 1)).Accepted, Is.True);
            Assert.That(session.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth, 1)).Accepted, Is.True);
        }

        private static void Capture(Camera camera, string path)
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
