using System.Collections;
using System.IO;
using System.Linq;
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
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace OneRoof.Tests.PlayMode
{
    public sealed class UndercityVisualCapturePlayModeTests : OwnedPlayModeSceneFixture
    {
        [UnityTest]
        public IEnumerator UndercityConstructionPresentation_ConnectedRoomsAndUnfinishedCavity()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("A graphics device is required for construction visual acceptance.");
            var holder = new GameObject("Undercity Construction Presentation");
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
                for (var depth = 0; depth <= 3; depth++) BuildCell(session, 16, depth);
                for (var x = 10; x <= 20; x++)
                    if (x != 16) BuildCell(session, x, 3);
                for (var depth = 1; depth <= 2; depth++)
                    for (var x = 10; x <= 20; x++)
                        if (x <= 12 || x >= 18) BuildCell(session, x, depth);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCoreCommand(16, 3)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCorridorCommand(10, 3, 6)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new BuildUndergroundCorridorCommand(17, 3, 4)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new ZoneUndergroundRoomCommand(UndergroundRoomType.AccessHub, 10, 1, 3, 2)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new ZoneUndergroundRoomCommand(UndergroundRoomType.Generator, 18, 1, 3, 2)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new DigUndergroundCommand(23, 5, 3)).Accepted, Is.True);
                controller.SyncPresenterGeometry();
                // The same rig as Tower Residents makes the clearance comparison visible in the capture.
                var reference = new GameObject("Corridor Resident scale reference");
                reference.transform.SetParent(holder.transform, false);
                var rig = reference.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
                rig.Initialize(3);
                rig.SetCaption(null);
                rig.SetBubbleVisible(false);
                var corridor = holder.GetComponentsInChildren<Transform>()
                    .First(t => t.name == "Underground Corridor 14_3");
                reference.transform.position = new Vector3(corridor.position.x,
                    corridor.Find("Walkway").GetComponent<MeshRenderer>().bounds.max.y, 0.10f);
                var projection = session.UndergroundProjection();
                Assert.That(projection.Rooms.Count, Is.EqualTo(2));
                foreach (var room in projection.Rooms) Assert.That(room.IsReachable, Is.True);
                Assert.That(projection.IsExcavated(24, 6), Is.True);
                Assert.That(projection.IsFloored(24, 6), Is.False);
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                camera.GetComponent<TowerCameraController>().FocusUnderground();
                yield return new WaitForSeconds(1f);
                var output = "/tmp/one-roof-undercity-refresh";
                Directory.CreateDirectory(output);
                Capture(camera, Path.Combine(output, "undercity-overview.png"));
                AssertCavityReadability(camera, session, Path.Combine(output, "undercity-overview.png"));
                camera.orthographicSize = 5.7f;
                camera.transform.position = new Vector3(-0.9f, TowerStructurePresenter.FloorY(0) - 4.8f, -10f);
                yield return null;
                Capture(camera, Path.Combine(output, "undercity-construction-close.png"));
#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
                // Exercise the real pointer consumer and include IMGUI, which Camera.Render omits.
                camera.GetComponent<TowerCameraController>().FocusUnderground();
                controller.SetPaused(true);
                controller.ModeSession.SelectBuildTool(null);
                yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "undercity-build-palette.png"));
                yield return null;
                controller.ModeSession.SelectBuildTool("underground:floor_3");
                var previousMouse = Mouse.current;
                var mouse = InputSystem.AddDevice<Mouse>();
                var settings = InputSystem.settings;
                var previousBehavior = settings.editorInputBehaviorInPlayMode;
                var previousBackground = settings.backgroundBehavior;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                try
                {
                    var placement = holder.GetComponent<GridPlacementController>();
                    Assert.That(session.TryGetFloorSlab(0, out var slab), Is.True);
                    var earthCenter = placement.CellOriginX + (slab.MinX + slab.MaxX + 1) * placement.CellWidth * 0.5f;
                    var targetWorld = new Vector3(earthCenter - UndergroundDigState.GridWidthCells * GridPlacementController.UndergroundCellWidth * 0.5f + 23.1f * GridPlacementController.UndergroundCellWidth,
                        placement.FloorOriginY - 0.74f - 5.1f * GridPlacementController.UndergroundCellHeight, 0f);
                    // Pan the work site into the unobstructed center of the actual GameView.
                    camera.GetComponent<TowerCameraController>().PanBy(new Vector2(targetWorld.x - camera.transform.position.x, 0f));
                    var targetScreen = camera.WorldToScreenPoint(targetWorld);
                    mouse.MakeCurrent();
                    for (var frame = 0; frame < 4; frame++)
                    {
                        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(targetScreen.x, targetScreen.y) });
                        InputState.Change(mouse.position, new Vector2(targetScreen.x, targetScreen.y));
                        yield return null;
                    }
                    placement.TryGetUndergroundCellFromScreen(targetScreen, camera, out var targetCell, out var targetDepth);
                    var diagnostic = $"Screen {Screen.width}×{Screen.height}, pointer {targetScreen}, current {Mouse.current == mouse}/{Mouse.current?.position.ReadValue()}, enabled {placement.enabled}, blocked {controller.ModeSession.IsInputBlocked}, mode {controller.ModeSession.CurrentMode}, UI {GridPlacementController.IsPointerOverUI(targetScreen, false, Screen.height, Screen.width)}, cell {targetCell}/{targetDepth}, hint {placement.LastPlacementHint}";
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "undercity-build-diagnostic.png"));
                    Assert.That(placement.GhostPresenter.IsVisible, Is.True, diagnostic);
                    Assert.That(placement.LastPlacementValid, Is.True, placement.LastPlacementHint);
                    Assert.That(placement.GhostPresenter.CurrentSize.x, Is.EqualTo(3f * GridPlacementController.UndergroundCellWidth * .98f).Within(.01f));
                    Assert.That(placement.GhostPresenter.CurrentSize.y, Is.EqualTo(3f * GridPlacementController.UndergroundCellHeight * .98f).Within(.01f));
                    Assert.That(placement.LastPlacementHint, Does.Contain("3×3 m"));
                    Assert.That(session.UndergroundProjection().IsFloored(24, 6), Is.False, "Hover must not construct.");
                    yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "undercity-build-preview.png"));
                    yield return null;
                }
                finally
                {
                    InputSystem.RemoveDevice(mouse);
                    if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
                    settings.editorInputBehaviorInPlayMode = previousBehavior;
                    settings.backgroundBehavior = previousBackground;
                }
#endif
            }
            finally { Object.Destroy(holder); }
        }

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
            }
        }

        private static void BuildCell(OneRoof.Application.Tower.TowerSimulationSession session, int x, int depth)
        {
            Assert.That(session.ExecuteCommand(new DigUndergroundCommand(x, depth, 1)).Accepted, Is.True);
            Assert.That(session.ExecuteCommand(new BuildUndergroundFloorCommand(x, depth, 1)).Accepted, Is.True);
        }

        private static void AssertCavityReadability(Camera camera, OneRoof.Application.Tower.TowerSimulationSession session, string path)
        {
            Assert.That(session.TryGetFloorSlab(0, out var slab), Is.True);
            var center = GridPlacementController.DefaultCellOriginX + (slab.MinX + slab.MaxX + 1) * 0.25f;
            var left = center - UndergroundDigState.GridWidthCells * GridPlacementController.UndergroundCellWidth * 0.5f;
            var y = TowerStructurePresenter.FloorY(0) - 0.74f - 6.5f * GridPlacementController.UndergroundCellHeight;
            var image = new Texture2D(2, 2);
            var previousAspect = camera.aspect;
            try
            {
                Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True);
                camera.aspect = image.width / (float)image.height;
                float Brightness(float x)
                {
                    var viewport = camera.WorldToViewportPoint(new Vector3(left + x * GridPlacementController.UndergroundCellWidth, y, 0f));
                    Assert.That(viewport.x, Is.InRange(0f, 1f));
                    var color = image.GetPixel(Mathf.RoundToInt(viewport.x * (image.width - 1)),
                        Mathf.RoundToInt(viewport.y * (image.height - 1)));
                    return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                }
                Assert.That(Brightness(24.5f), Is.LessThan(Brightness(22.5f) * 0.65f),
                    "An unfloored excavation must read as a recessed cavity, not earth with an outline.");
            }
            finally { camera.aspect = previousAspect; Object.Destroy(image); }
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
