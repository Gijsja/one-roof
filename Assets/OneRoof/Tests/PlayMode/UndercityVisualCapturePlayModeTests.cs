using System.Collections;
using System.IO;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Overlays;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class UndercityVisualCapturePlayModeTests
    {
        [UnityTest]
        public IEnumerator UndercityUtilitiesVisualCapture_RunningAndBrokenFlow()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A graphics device is required for visual capture.");
            var holder = new GameObject("Undercity Utility Visual Capture");
            holder.SetActive(false);
            var controller = holder.AddComponent<TowerPlayableController>();
            typeof(TowerPlayableController).GetField("_startMode",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(controller, TowerStartMode.GroundFloorStart);
            holder.SetActive(true);
            try
            {
                yield return null;
                var session = controller.SimulationSession;
                Assert.That(session.FloorCount, Is.EqualTo(1));
                BuildUtility(-14, -11, "utility:electrical_substation", 120);
                BuildUtility(-10, -9, "utility:electrical_riser", 0);
                BuildUtility(-8, -7, "utility:floor_transformer", 0);
                BuildUtility(-6, -3, "utility:water_pump", 120);
                BuildUtility(-2, -1, "utility:water_riser", 0);
                for (var depth = 0; depth <= 3; depth++) BuildCell(session, 16, depth);
                for (var x = 17; x <= 20; x++) BuildCell(session, x, 3);
                for (var x = 18; x <= 20; x++)
                    for (var depth = 1; depth <= 2; depth++) BuildCell(session, x, depth);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCoreCommand(16, 3)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCorridorCommand(17, 3, 4)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new ZoneUndergroundRoomCommand(UndergroundRoomType.AccessHub, 18, 1, 3, 2)).Accepted, Is.True);

                controller.SyncPresenterGeometry();
                controller.ModeSession.SwitchMode(InteractionMode.Data);
                controller.ShowUtilitiesOverlay();
                var network = holder.GetComponent<UtilitiesNetworkLayerPresenter>();
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                camera.GetComponent<TowerCameraController>().FocusUnderground();
                yield return null;
                Assert.That(network.ActiveLineCount, Is.GreaterThan(0));
                var output = "/tmp/one-roof-m10-utilities-visual";
                Directory.CreateDirectory(output);
                Capture(camera, Path.Combine(output, "power-running.png"));
                network.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
                yield return null;
                Capture(camera, Path.Combine(output, "water-running.png"));

                foreach (var room in session.TopologyProjection().Rooms.Values)
                    if (room.ContentType == new ContentId("utility:electrical_substation"))
                    {
                        Assert.That(session.ExecuteCommand(new DemolishRoomCommand(room.Id)).Accepted, Is.True);
                        break;
                    }
                controller.SyncPresenterGeometry();
                network.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Power);
                yield return null;
                Capture(camera, Path.Combine(output, "power-stopped.png"));
                var paths = session.UndergroundUtilityPathProjection();
                Assert.That(paths.RoomStatuses[0].IsFlowing, Is.False);
                Assert.That(File.Exists(Path.Combine(output, "power-stopped.png")), Is.True);

                void BuildUtility(int minX, int maxX, string contentId, int capacity) =>
                    Assert.That(session.ExecuteCommand(new BuildRoomCommand(0, minX, maxX,
                        new ContentId(contentId), capacity)).Accepted, Is.True, contentId);
            }
            finally
            {
                Object.Destroy(holder);
                var cameraObject = GameObject.Find("Tower Camera");
                if (cameraObject != null) Object.Destroy(cameraObject);
            }
        }

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
